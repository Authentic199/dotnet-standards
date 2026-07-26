---
name: distributed-caching
description: >-
  This skill should be used when distributed caching enters a .NET solution:
  adding it to a project that has none, scaffolding the cache facade under
  Facades/Common/RedisCaches, injecting IRedisCacheService or IDistributedCache,
  naming a cache key or CachePrefix, choosing a TTL or
  DistributedCacheEntryOptions, reading once with GetRemoveAsync, invalidating an
  entry after a mutation, wiring RedisSettings and AddStackExchangeRedisCache,
  reviewing a cache-aside read and its fallback, or deciding whether to cache at
  all. Not for: distributed locks, ConcurrencyHandlers, LockedException —
  distributed-lock; search indexing, ElasticsearchSettings — elasticsearch-search;
  background jobs, Hangfire — background-worker; DbContext, repositories —
  ef-core-data-access; project placement, facade wiring —
  facade-module-architecture.
---

## Overview

One capability owns distributed caching: `Infrastructure/Facades/Common/RedisCaches/`.
It exposes a single service — `IRedisCacheService` over `IDistributedCache` — with
one key factory and one JSON codec. Nothing else in the solution touches
`IDistributedCache` or a Redis client directly.

**The cache is never the source of truth.** Every read path must still be correct
with the cache empty: a miss loads from the database or the search index and the
request succeeds. A path that needs a hit to be correct is a bug, not an
optimization.

Answer "should this be cached, and how do I call it?" from this file alone. Open a
`references/` file only under the conditions stated below.

## Before scaffolding: find the cache that may already be there

Search the loading project for **any** existing cache capability before creating
one:

- a folder whose files own `IDistributedCache`, under `Facades/` or `Facades/Common/`;
- an `AddCache`-, `AddRedisCache`- or `AddStackExchangeRedisCache`-shaped line in
  the `AddInfrastructure` chain, or in a facade `Startup.cs` it calls.

**If you find one, use it in place** — add the operation you need to the service
that already exists. A second cache capability beside the first gives the project
two key conventions and two connection policies, and nothing warns you. This skill
describes the capability it scaffolds; it makes no claim about what an existing
cache folder contains, so read that folder before extending it.

Only when the search finds nothing, scaffold — and scaffold **first**, before the
consuming code. Do not inline `IDistributedCache` into a module, do not write a
module-local cache wrapper, and do not "temporarily" cache through a dictionary
while the capability is missing.

## Placement & anatomy

Distributed caching is a **`Common` sub-capability**, not a top-level facade — it
is substrate only some projects take. Per the fractal rule it is shaped like a
miniature facade: own folder, own settings, own `Startup.cs`.

```
Facades/Common/RedisCaches/
├── RedisCacheService.cs     # IRedisCacheService + RedisCacheService, one file
├── RedisSettings.cs         # settings follow their service
└── Startup.cs               # internal static class Startup → AddRedisCache(configuration)
```

- `RedisSettings` lives **in this capability** with its own configuration section
  and its own `cache.json` topic in `Web/Configurations/`. *(Deviation, deliberate:
  the canonical project nests `RedisSettings` inside the persistence facade's
  `DatabaseSettings` and reads it back out of that section. Settings follow their
  service, so this capability carries its own.)*
- Bound with the standard **four calls** — `AddOptions<RedisSettings>()` →
  `BindConfiguration(nameof(RedisSettings))` → `ValidateDataAnnotationsRecursively()`
  → `ValidateOnStart()`. `RedisSettings` implements `IValidatableObject` and
  validates by returning `validationContext.Required()`.
- Reading `IConfiguration` directly is allowed at **exactly one point**: the
  `AddStackExchangeRedisCache` registration, which runs before the options container
  exists. Everywhere else injects `IOptions<RedisSettings>`.
- `internal static class Startup` exposing `AddRedisCache(configuration)`, composed
  by `Common`'s root `Startup`. *(Deviation, deliberate: the canonical method is
  named `AddRedisService`.)* Registration is explicit —
  `AddScoped<IRedisCacheService, RedisCacheService>()`; this service carries no
  lifetime marker.

**Read `references/implementation.md` when** you are scaffolding the capability, or
writing or reviewing `RedisCacheService.cs`, `RedisSettings.cs` or this
capability's `Startup.cs` — it carries the full file bodies and the settings split.

## Prerequisites — stop if either is missing

This capability is not self-contained. It needs two pieces of shared substrate:

| Prerequisite | Where it lives | Used for |
|---|---|---|
| `IJsonSerializerService` | `Facades/Common/Services/` | every value it stores |
| the `Required()` validation helper | `Facades/Common/Extensions/` (`ValidatorExtension`) | `RedisSettings.Validate` |

**If either is absent from the loading project, stop. Report what is missing,
propose options — introduce the shared piece first, or narrow the task — and wait
for a decision.** Do not scaffold either silently, do not substitute a local
`JsonSerializer.Serialize` call, and do not swap `Required()` for hand-written
attributes: the cache would then encode values, or fail configuration, differently
from every other subsystem, and the divergence surfaces months later as a
deserialization or startup failure nobody connects to this change.

## Service surface

Interface **and** implementation in one file.

| Member | Use it for |
|---|---|
| `Task<T?> GetAsync<T>(key, ct)` | Plain read. Returns `default` on miss — a miss is not an exception. |
| `Task<T?> GetRemoveAsync<T>(key, ct)` | Read-once handoff. Removes only when a value was found. |
| `Task<T> SetAsync<T>(key, value, TimeSpan? expiration, ct)` | Write with an absolute expiry relative to now. `null` (the default) means no expiry. |
| `Task<T> SetAsync<T>(key, value, DistributedCacheEntryOptions options, ct)` | Write needing sliding expiry or an absolute date. |
| `Task RemoveAsync(key, ct)` | Explicit invalidation. |

- The `TimeSpan?` overload **delegates** to the options overload, which is the only
  one that serializes and writes — one code path, one place a bug can live.
- Both `SetAsync` overloads **return the stored value**, so a write can be the tail
  of an expression instead of a statement followed by a bare `return`.
- Every method takes a trailing `CancellationToken ct = default` and passes it down.
- Values cross the wire as strings through the shared serializer, which is camelCase
  and ignores reference cycles. **Cache values, never behavior:** cache DTOs and
  entity-shaped records that survive serialize → deserialize; never a delegate, a
  stream, an open handle, or anything whose identity matters.

## Key discipline

The full key is `{CachePrefix}:{TypeName}:{suffix}`. `{CachePrefix}` is **not** in
your string — it comes from settings, is forced to end with `':'`, and is handed to
Redis as `InstanceName`, which prepends it to every key this cache writes. Your code
produces `{TypeName}:{suffix}` and nothing more.

**`IRedisCacheService.CacheKey<T>(suffix)` is the only key factory.**

- No literal key strings, no interpolation at a call site, no hand-written
  `$"{typeof(T).Name}:{id}"`.
- **No module-local key helper.** Re-declaring a generic `CacheKey<T>(suffix)` in a
  module utility compiles, produces identical strings today, and diverges silently
  the day the convention changes — one call site then reads keys the other never
  wrote.
- **Named key, not a second factory:** a value with exactly one instance and no
  suffix (pattern B below) is keyed by the type name alone. Declare it once as
  `static string CacheKey => typeof(T).Name` on the **owning service's interface**,
  so the read, the write and the invalidation all name one constant.

## When to cache

| Situation | Pattern | Key | Expiry | Invalidation |
|---|---|---|---|---|
| A value produced by one pipeline stage and consumed by the next, correlated by an id, read exactly once | **A — handoff** | `CacheKey<T>(correlationId)` | short absolute, ~5 min | `GetRemoveAsync` consumes it |
| A configuration-like row read on nearly every request and written rarely | **B — cache-aside** | named key (type name) | none | `RemoveAsync` then `SetAsync` on every write |
| Anything else | **don't cache yet** | — | — | — |

**Pattern A — pipeline handoff.** The producing stage writes the newest version per
correlation id with a short TTL, in parallel with the durable write. The consuming
stage reads once and falls back:

```csharp
private async Task<Snapshot?> GetSnapshotAsync(string correlationId)
    => await cacheService.GetRemoveAsync<Snapshot>(IRedisCacheService.CacheKey<Snapshot>(correlationId))
       ?? await store.Repository<Snapshot>().FirstOrDefaultAsync(ByCorrelation(correlationId));
```

The `??` is the contract, not defensive noise. The TTL is a leak bound, not a
freshness policy: the handoff either happens within seconds or it does not happen.
Do not stretch it to hours to "improve the hit rate" — a stale handoff is worse than
a miss.

**Pattern B — cache-aside for a configuration-like row.** Read: a hit returns; a
miss loads the single row, creating a validated default if none exists, and writes
it back. Write: `RemoveAsync` then `SetAsync`, in that order, in one private helper
that every mutating path funnels through. **No TTL is earned, not assumed** — it is
safe only because there is a single row, a single writer, and that writer always
invalidates. If any path can mutate the row without going through the service, give
it a TTL.

**Read `references/usage-patterns.md` when** you are writing or reviewing a call
site, choosing between the two patterns, or asked about `HybridCache` — it carries
both patterns end to end, the authorized anti-example for module-local key helpers,
and why `HybridCache` was considered and not adopted.

## Connection policy

The connection is built from the settings type with four values that are policy, not
tuning knobs:

| Option | Value | Why |
|---|---|---|
| `AbortOnConnectFail` | `false` | An unreachable Redis must not stop the app starting or serving. |
| `ConnectTimeout` | `5000` | Fail a connect attempt fast rather than holding a request. |
| `ConnectRetry` | `3` | Ride out a restart or failover without operator action. |
| `KeepAlive` | `180` | Survive NAT and load-balancer idle timeouts. |

Leaving `AbortOnConnectFail` at its default turns a cache outage into an application
outage. Together with *never the source of truth*, these four make the system
degrade to slower-but-correct instead of down. Do not override them per call site.

## Not this skill

Mutual exclusion, `ConcurrencyHandlers`, lock expiry and `LockedException` →
`distributed-lock`. Indexing, search documents and query descriptors →
`elasticsearch-search`. Recurring jobs and Hangfire → `background-worker`.
`DbContext`, entities and the repository abstraction the cache falls back to →
`ef-core-data-access`. Where the capability folder sits, facade anatomy and the
composition root → `facade-module-architecture`.
