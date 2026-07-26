# Usage patterns

How to consume `IRedisCacheService` from a module. Two patterns cover nearly every
real case; a use that fits neither is a design question, not a caching question, and
the default answer is **don't cache it yet** — the cache is never the source of truth.

Modules inject `IRedisCacheService`. Never `IDistributedCache`, never a Redis client
directly.

| Situation | Pattern | Key | TTL | Invalidation |
|---|---|---|---|---|
| Value handed from one pipeline stage to the next | Pipeline handoff (read-once) | `CacheKey<T>(suffix)` | Short, e.g. 5 minutes | Automatic — the read removes it |
| Single configuration-like row read on most requests | Cache-aside | `typeof(T).Name`, no suffix | None | Explicit, on every mutation |
| Per-user or per-tenant data, many keys, unbounded growth | Neither — reconsider | — | — | — |

Every value stored must survive a JSON round-trip — see *What may be cached*.

---

## Pattern 1 — pipeline handoff, read-once

One stage produces a document; the next stage needs it seconds later, and the source
of truth is slow to query (a search index, an external system). The producer writes to
the source of truth **and** to the cache, in parallel, with a short TTL. The consumer
reads the cache first and falls through.

The fallback is not error handling. It is the normal path whenever the entry expired,
was never written, or is being read a second time.

**Producer** — both writes together, short TTL.

```csharp
public static class ProcessingPipelineExtensions
{
    private static readonly TimeSpan HandoffExpiration = TimeSpan.FromMinutes(5);

    public static async Task UpsertRange<TDocument>(
        this IElasticSearchWrapper searchWrapper,
        IRedisCacheService cacheService,
        IEnumerable<TDocument> documents)
        where TDocument : IProcessingDocument
    {
        List<Task> tasks = new()
        {
            CacheRange(cacheService, documents),
            UpsertRange(searchWrapper, documents),
        };

        await Task.WhenAll(tasks);
    }

    private static async Task CacheRange<TDocument>(
        IRedisCacheService cacheService,
        IEnumerable<TDocument> documents)
        where TDocument : IProcessingDocument
    {
        // Only sessions still in flight, and only the newest version of each —
        // an older version would be read back as if it were current.
        IEnumerable<(string SessionId, TDocument Document)> latest = documents
            .Where(x => x.EndTime == null)
            .GroupBy(x => x.SessionId)
            .Select(group => (group.Key, group.OrderByDescending(x => x.Version).First()));

        List<Task> tasks = new();
        foreach ((string sessionId, TDocument document) in latest)
        {
            tasks.Add(cacheService.SetAsync(
                IRedisCacheService.CacheKey<TDocument>(sessionId),
                document,
                HandoffExpiration));
        }

        await Task.WhenAll(tasks);
    }
}
```

Note that the parallel `Task.WhenAll` couples the two writes: a Redis outage faults
the whole upsert, and the durable write is lost because a best-effort cache write
failed. When the durable write must not be lost, decouple them — durable first,
cache best-effort:

```csharp
await UpsertRange(searchWrapper, documents);
try
{
    await CacheRange(cacheService, documents);
}
catch
{
    // A failed handoff write costs only the fast path — the consumer's
    // fallback query still succeeds.
}
```

**Consumer** — `GetRemoveAsync`, with the source of truth as the `??` fallback.

```csharp
private async Task<ProcessingDocument?> GetDocumentAsync(string sessionId)
{
    return await cacheService.GetRemoveAsync<ProcessingDocument>(
            IRedisCacheService.CacheKey<ProcessingDocument>(sessionId))
        ?? await searchWrapper.Repository<ProcessingDocument>()
            .FirstOrDefaultAsync(x => x.SessionId == sessionId);
}
```

Why this shape holds:

- **`GetRemoveAsync`, not `GetAsync`.** A handoff entry has exactly one legitimate
  reader. Removing on read frees the memory immediately instead of holding it for the
  rest of the TTL, and a second reader falls through to the source of truth — correct
  by construction, because the cached value is a snapshot of a stage that has moved on.
- **The TTL is the janitor, not the contract.** Five minutes only bounds the entries
  whose consumer never arrived. Do not lengthen it to "improve the hit rate": that
  widens the window in which a superseded snapshot can be served.
- **Losing Redis loses nothing.** Every read degrades to the authoritative query —
  slower, not wrong.
- **The fallback must return the same logical value the cache would have.** If the two
  paths can disagree, the flow is non-deterministic and no test will reliably catch it.

---

## Pattern 2 — cache-aside for a configuration-like row

A single mutable row read on most requests, written rarely, where **every mutation
passes through one service**.

- **Key:** the type name alone, no suffix — there is only one row, so there is nothing
  to disambiguate. Declare it as a static member on the service's own interface so
  producer and consumer cannot disagree about it.
- **TTL:** none. Correctness comes from invalidation, not expiry.
- **Invalidation:** `RemoveAsync` then `SetAsync`, in the same method that mutated.

```csharp
public interface IIntegrationSettingService : IScopedService
{
    static string CacheKey => typeof(IntegrationSetting).Name;

    Task<IntegrationSetting> GetAsync(CancellationToken ct = default);

    Task<IntegrationSetting> UpsertAsync(UpsertIntegrationSettingRequest request);
}
```

```csharp
// IRepositoryWrapper, IRedisCacheService and IMapper are constructor-injected.

public async Task<IntegrationSetting> GetAsync(CancellationToken ct = default)
{
    IntegrationSetting? cached = await cacheService.GetAsync<IntegrationSetting>(
        IIntegrationSettingService.CacheKey, ct);

    if (cached is not null)
    {
        return cached;
    }

    IntegrationSetting? entity = await repositoryWrapper
        .Repository<IntegrationSetting>()
        .Find()
        .FirstOrDefaultAsync(ct);

    if (entity is null)
    {
        entity = new IntegrationSetting();
        await repositoryWrapper.Repository<IntegrationSetting>().AddAsync(entity, ct);
    }

    return await Cached(entity);
}

public async Task<IntegrationSetting> UpsertAsync(UpsertIntegrationSettingRequest request)
{
    IntegrationSetting? entity = await repositoryWrapper
        .Repository<IntegrationSetting>()
        .Find()
        .FirstOrDefaultAsync();

    if (entity is null)
    {
        entity = mapper.Map<IntegrationSetting>(request);
        await repositoryWrapper.Repository<IntegrationSetting>().AddAsync(entity);
    }
    else
    {
        entity = mapper.Map(request, entity);
        await repositoryWrapper.Repository<IntegrationSetting>().UpdateAsync(entity);
    }

    // Every mutation path ends here. That is what makes the no-TTL entry safe.
    return await Cached(entity);
}

private async Task<IntegrationSetting> Cached(IntegrationSetting entity)
{
    await cacheService.RemoveAsync(IIntegrationSettingService.CacheKey);
    await cacheService.SetAsync(IIntegrationSettingService.CacheKey, entity);
    return entity;
}
```

`SetAsync` returns the value it stored, which is what keeps both public methods ending
in a single `return await Cached(entity);`.

### No TTL is earned, not assumed

All three conditions must hold:

1. **One row** — the cache holds one entry that is overwritten, never accumulated.
2. **One writer** — every mutation goes through this service. No background job, no
   migration, no admin script, no sibling service writes that table directly.
3. **Unconditional invalidation** — not behind an `if`, not in a `catch`, not opt-in
   per call site. The mutation method cannot return without it having run.

If any of them fails, add a TTL. A stale no-TTL entry is served *forever* — not for
five minutes, but until someone restarts Redis or notices. The bug surfaces long after
the change, somewhere else, with no error anywhere. A TTL turns "wrong forever" into
"wrong for at most N seconds", which is a bug you can survive.

Note that the cache write is not part of the database transaction. If a surrounding
transaction rolls back after `Cached` ran, the cache holds a value the database never
accepted; when the mutation participates in a transaction, invalidate after it commits.

---

## Key discipline

One factory, on the facade interface, callable from anywhere:

```csharp
string key = IRedisCacheService.CacheKey<ProcessingDocument>(sessionId);
```

- Never build a key with string interpolation at a call site.
- Never define a second helper that formats keys (see below).
- The only decision left to you is the **suffix** — the identifier distinguishing one
  entry from another. The type parameter does the namespacing, so two different types
  may safely share a suffix.
- A single-row cache uses a **named key** instead: `static string CacheKey =>
  typeof(T).Name` on the owning service's interface. That is a named constant for one
  specific entry, not a second key factory.
- The configured cache prefix is applied as the Redis `InstanceName`, so it namespaces
  every key transparently — callers never concatenate it themselves.

### Anti-pattern: a module redefining the facade's key helper

A module declares its own generic `CacheKey<T>` formatting keys exactly the way the
facade already does, then uses it on both the producer and the consumer side.

**BAD**

```csharp
public static class ProcessingUtility
{
    // Byte-for-byte the same format as IRedisCacheService.CacheKey<T>.
    public static string CacheKey<T>(string sessionId)
        => $"{typeof(T).Name}:{sessionId}";
}

await cacheService.SetAsync(ProcessingUtility.CacheKey<TDocument>(sessionId), document, HandoffExpiration);
```

**GOOD**

```csharp
await cacheService.SetAsync(
    IRedisCacheService.CacheKey<TDocument>(sessionId), document, HandoffExpiration);
```

Identical today is not the problem — **divergence tomorrow is.** Two factories drift
independently, and the day one gains a tenant segment or a different separator,
producers and consumers silently stop meeting at the same key. The failure is not an
error but a miss: the handoff falls back to the source of truth and keeps returning
correct answers, just slowly and under higher load. The cache appears to work while
being entirely useless, and nothing in the logs says so.

Delete the module helper — the facade member is public, static and reachable from every
module, so there is nothing to keep.

**Red flag:** you are about to write `$"{typeof(T).Name}:` outside the cache facade.

---

## What may be cached

Values round-trip the shared JSON serializer, configured with camelCase names,
`ReferenceHandler.IgnoreCycles`, case-insensitive property matching and relaxed
escaping. What comes back is a **new object reconstructed from JSON**, never the
instance you stored.

Cache DTO- and entity-shaped types: public properties with public setters (or records),
values, and collections of them.

Do not cache:

- Anything holding a delegate, a stream, a connection or another live handle.
- Types whose cycles must survive. `IgnoreCycles` **drops** back-references rather than
  failing, so a parent → child → parent graph returns with the inner parent `null`,
  silently. Flatten the type first if a back-reference matters.
- Anything without a public setter — computed-only and private state is gone on the way
  back.

Two behaviours worth knowing:

- A miss returns `default`. For a value type that is `0`, indistinguishable from a
  cached zero — cache reference types, or wrap the value in a small DTO.
- Case-insensitive matching plus missing-properties-default means cached payloads
  survive a rolling deploy where two versions of a type coexist. It also means a
  renamed or removed property reads back as `null`/`0` with no error. Treat a cached
  payload as a wire format: changing its shape is a compatibility decision, not a
  refactor.

---

## HybridCache — considered, not adopted

`Microsoft.Extensions.Caching.Hybrid` (L1/L2 layering, stampede protection, tag-based
invalidation) was evaluated and ruled out: it requires .NET 9 or later, and the
canonical stack is .NET 7 on `IDistributedCache` with the StackExchangeRedis provider,
where every pattern above is verified against production code. Revisit on a framework
upgrade. Until then this facade is the standard, and HybridCache is not a reason to
deviate from it.
