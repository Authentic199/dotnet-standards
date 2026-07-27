# Scaffolding the lock capability

Use this file only when the project has no mutual-exclusion capability and you are
creating one. If it already has one, extend it in place — the guard below is how you
tell the difference.

## Pre-scaffold guard

Before creating anything, check both:

1. **Does any folder already own `IDistributedLockFactory`, `IRedLock`, or a static
   `SemaphoreSlim` registry?** A lock capability may sit under a different name than
   the one this file scaffolds, including inside a module.
2. **Does the `AddInfrastructure` chain already contain a lock registration** — an
   `AddConcurrencyHandler()`, `AddRedLock()` or similarly named line?

Either hit means the capability exists: stop scaffolding and use it in place. The
chain is ordered for readability rather than semantics, which is precisely why a
duplicate survives review in it — **search it by method name, do not skim it.**

A duplicated lock capability is worse than a duplicated cache. Two handlers, each with
its own semaphore registry or its own factory, hand out *different* locks for the same
key string. Both callers acquire successfully, both proceed, and the guarded work runs
twice. Nothing throws and nothing logs; the only evidence is the duplicate record the
lock existed to prevent.

## Three prerequisites this scaffold never creates

All three are shared substrate with an owner elsewhere in the solution. Verify each
exists before writing any file.

**1. The `RedisSettings` section.** The distributed provider is built from a Redis
connection, and that connection already has an owner: the cache capability in
`Infrastructure/Facades/Common/RedisCaches/`, which binds `RedisSettings` as its own
configuration section. This scaffold reads that same section. It does **not** declare a
connection string of its own — locks and cache entries must reach the same Redis, and
two sections holding "the same" connection drift the day one is updated.

**2. The validation helper.** `ConcurrencySettings` calls `validationContext.Required()`,
an extension in `Infrastructure/Facades/Common/Extensions/` (`ValidatorExtension`):

```csharp
public static IEnumerable<ValidationResult> Required(
    this ValidationContext validationContext, params string[] ignoreProperties)
```

It reflects over every public property and reports any that is empty or still at its
type default, so a settings class opts into whole-object validation with one line
instead of per-property attributes.

**3. `LockedException` and its HTTP exception family.** In `Core/Common/Exceptions/`.
This is not a soft dependency: `ConcurrencyHandler` throws it on a failed acquisition
and **does not compile without it**. The exception family and the middleware that maps
it are introduced and owned by the `error-handling` skill — do not define a local
exception type here, and do not fall back to a bare `Exception`. A failed acquisition
that does not arrive as `LockedException` reaches the caller as a server fault, and the
retry that would have succeeded a second later never happens.

**If any is missing: stop, report, and let the caller choose.** Do not create them, do
not declare a second Redis connection string, do not substitute `[Required]` attributes
for the helper, do not invent an exception type.

| Option | Consequence |
|---|---|
| Scaffold the missing piece first, as its own task | Lands in its correct home, owned by the skill that owns it, reviewed on its own merits. Lock work waits. |
| Point at an equivalent already in the project | No new file. Requires confirming it matches — a second connection string that happens to be identical today, or a locally-defined exception the middleware does not recognise, is a silent divergence, not a detail. |
| Proceed without it | Not offered, and not actually possible. All three are types the files below reference, so the scaffold does not compile without them; if the settings type exists but its section does not, the registration-time read throws during composition. What *is* possible is the workaround — a second connection string, a local exception type — and that is the silent failure this table exists to prevent. |

**Package quick-check, not a stop:** `RedLock.net` (namespaces `RedLockNet`,
`RedLockNet.SERedis`, `RedLockNet.SERedis.Configuration`) and `StackExchange.Redis`
(for `ConnectionMultiplexer`). Verify they resolve before writing files; a missing
package reference is not a decision to escalate.

## Checklist

Work in this order. Each step is done only when the named artifact exists.

1. Create `Infrastructure/Facades/Common/Services/ConcurrencyHandlers/`.
2. Add `RedLock.net` to the `Infrastructure` csproj — the canonical pins `2.3.2`.
   `StackExchange.Redis` arrives transitively, but the canonical pins it explicitly
   (`2.6.122`) because this capability constructs `ConnectionMultiplexer` itself; pin
   it too rather than depending on whatever the transitive graph resolves to.
3. Write `ConcurrencySettings.cs` — settings class and the provider enum, one file.
4. Write `ConcurrencyHandlerOptions.cs` — the per-call options object.
5. Write `ConcurrencyHandler.cs` — interface and implementation, one file.
6. Write `Startup.cs` — `internal static class Startup`, one `AddConcurrencyHandler()`.
7. Add the `ConcurrencySettings` section to the base `appsettings.json`. There is **no
   dedicated configuration topic file** for this capability.
8. Append `.AddConcurrencyHandler(configuration)` to the `AddInfrastructure` chain,
   after the duplicate search from the guard above.

Steps 7 and 8 are where scaffolds get abandoned half-done. A capability that compiles
but is never composed is not finished — and here the failure is quiet: every call site
still compiles, and the first `LockedAsync` fails to resolve `IConcurrencyHandler` at
runtime.

## `ConcurrencySettings.cs`

```csharp
using Infrastructure.Facades.Common.Extensions;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Facades.Common.Services.ConcurrencyHandlers;

public class ConcurrencySettings : IValidatableObject
{
    public ConcurrencyProvider Provider { get; set; } = ConcurrencyProvider.SemaphoreSlim;

    public int WaitTime { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => validationContext.Required();
}

public enum ConcurrencyProvider
{
    /// <summary>
    /// In-memory locking over a semaphore. <para/>
    /// The lock is held within a single process only, so it excludes nothing across
    /// instances. <para/>
    /// No wait bound and no expiry: a competing caller queues until the holder releases.
    /// </summary>
    SemaphoreSlim = 1,

    /// <summary>
    /// Distributed locking over RedLock. <para/>
    /// Requires the Redis connection configured in the RedisSettings section.
    /// </summary>
    RedLock = 2,
}
```

Three properties of this shape are contract, not accident:

- **The enum is colocated with the settings class**, beside the property that names it.
  It is not a type consumers browse for on its own, and a separate file buys nothing.
- **The enum members carry explicit values, and they stay explicit.** The value is what
  a bound configuration string or number resolves to; renumbering silently repoints an
  existing deployment's configuration at the other provider.
- **`WaitTime` is an `int` in seconds here, and a `TimeSpan` on the options object.**
  They are not the same member and the conversion is the call site's job — see the
  configuration section below.

**`Provider` is bound, validated, and read by nothing.** No code path consults
`ConcurrencySettings.Provider`; the provider always comes from the per-call
`ConcurrencyHandlerOptions`. It is kept because it is canonical and because it records
the project's intent, but changing it in configuration changes no runtime behaviour.
**Do not add a fallback that makes it live** — that would silently change the provider
at every call site that omitted one, which is exactly the set of call sites nobody
audited.

## `ConcurrencyHandlerOptions.cs`

```csharp
namespace Infrastructure.Facades.Common.Services.ConcurrencyHandlers;

public class ConcurrencyHandlerOptions
{
    /// <summary>
    /// Lifetime of the distributed lock session. <para/>
    /// If this is set too short and the session expires while the guarded work is still
    /// running, the lock session is released automatically — another request then sees
    /// IsAcquired = true and can enter. <para/>
    /// That is, two requests may end up processing the same resource. <para/>
    /// Set it generously. When the work finishes before the session expires, disposing
    /// the lock releases the session anyway.
    /// </summary>
    public TimeSpan ExpiryTime { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long competing requests wait to acquire the lock. <para/>
    /// When the wait elapses, IsAcquired = false, and a request still waiting for the
    /// lock receives <see cref="Core.Common.Exceptions.LockedException"/>.
    /// </summary>
    public TimeSpan WaitTime { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long to wait before retrying an unsuccessful acquisition.
    /// </summary>
    public TimeSpan RetryTime { get; set; } = TimeSpan.FromMilliseconds(200);

    public ConcurrencyProvider Provider { get; set; } = ConcurrencyProvider.SemaphoreSlim;
}
```

*Normalized:* the doc comments are translated from the canonical file's own comments.
The wording is the canonical's, not a new claim.

**On the `ExpiryTime` comment specifically.** It states the canonical project's
documented intent for that option — *set the expiry above the worst case, because an
expiry reached mid-work is a second holder* — and this skill teaches it on that basis.
It is **not** a measured statement about what any particular client library does: some
distributed-lock clients renew a held lock in the background, and that behaviour was
not verified for the version pinned here. The engineering rule is unaffected either
way, because sizing the expiry above the worst-case duration is correct under both
behaviours; only the mechanism differs. Do not restate the comment elsewhere as library
behaviour.

Two more properties are contract:

- **`Provider` defaults to the in-memory value**, so an omitted options object silently
  selects the provider that excludes nothing across instances. This is why every call
  site passes the options object explicitly.
- **All three timings apply to the distributed provider only.** The in-memory path never
  receives this object — see below.

## `ConcurrencyHandler.cs`

```csharp
using Core.Common.Exceptions;
using RedLockNet;
using System.Collections.Concurrent;

namespace Infrastructure.Facades.Common.Services.ConcurrencyHandlers;

public interface IConcurrencyHandler
{
    Task<TResult> LockedAsync<TResult>(List<string> keys, Func<Task<TResult>> action, ConcurrencyHandlerOptions? options = null, CancellationToken cancellationToken = default);

    Task<TResult> LockedAsync<TResult>(string key, Func<Task<TResult>> action, ConcurrencyHandlerOptions? options = null, CancellationToken cancellationToken = default);
}

public class ConcurrencyHandler : IConcurrencyHandler
{
    private static readonly ConcurrentDictionary<string, Lazy<SemaphoreSlim>> SemaphoreLocker = new();
    private readonly IDistributedLockFactory redLockFactory;

    public ConcurrencyHandler(IDistributedLockFactory redLockFactory)
    {
        this.redLockFactory = redLockFactory;
    }

    public Task<TResult> LockedAsync<TResult>(List<string> keys, Func<Task<TResult>> action, ConcurrencyHandlerOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        return options.Provider switch
        {
            ConcurrencyProvider.SemaphoreSlim => SemaphoreAsync(keys, action, cancellationToken),
            ConcurrencyProvider.RedLock => RedLockAsync(keys, action, options, cancellationToken),
            _ => throw new NotSupportedException($"Concurrency provider {options.Provider} is not supported."),
        };
    }

    public Task<TResult> LockedAsync<TResult>(string key, Func<Task<TResult>> action, ConcurrencyHandlerOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        return options.Provider switch
        {
            ConcurrencyProvider.SemaphoreSlim => SemaphoreAsync(new List<string>() { key }, action, cancellationToken),
            ConcurrencyProvider.RedLock => RedLockAsync(key, action, options, cancellationToken),
            _ => throw new NotSupportedException($"Concurrency provider {options.Provider} is not supported."),
        };
    }

    private static async Task<TResult> SemaphoreAsync<TResult>(List<string> keys, Func<Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        List<string> sortedKeys = keys.OrderBy(key => key).ToList();
        List<SemaphoreSlim> semaphores = sortedKeys.ConvertAll(key => SemaphoreLocker.GetOrAdd(key, _ => new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(1, 1))).Value);

        try
        {
            foreach (SemaphoreSlim semaphore in semaphores)
            {
                await semaphore.WaitAsync(cancellationToken);
            }

            return await action();
        }
        finally
        {
            for (int i = 0; i < semaphores.Count; i++)
            {
                semaphores[i].Release();

                if (semaphores[i].CurrentCount == 1)
                {
                    SemaphoreLocker.TryRemove(sortedKeys[i], out _);
                }
            }
        }
    }

    private async Task<TResult> RedLockAsync<TResult>(string key, Func<Task<TResult>> action, ConcurrencyHandlerOptions options, CancellationToken cancellationToken = default)
    {
        await using IRedLock redLock = await redLockFactory.CreateLockAsync(key, options.ExpiryTime, options.WaitTime, options.RetryTime, cancellationToken);

        if (redLock.IsAcquired)
        {
            return await action();
        }

        throw new LockedException("Resource is locked right now. Try again later!");
    }

    private async Task<TResult> RedLockAsync<TResult>(List<string> keys, Func<Task<TResult>> action, ConcurrencyHandlerOptions options, CancellationToken cancellationToken = default)
    {
        List<string> sortedKeys = keys.OrderBy(key => key).ToList();
        List<IRedLock> redLocks = new();

        try
        {
            foreach (string key in sortedKeys)
            {
                IRedLock redLock = await redLockFactory.CreateLockAsync(key, options.ExpiryTime, options.WaitTime, options.RetryTime, cancellationToken);
                if (!redLock.IsAcquired)
                {
                    await redLock.DisposeAsync();
                    throw new LockedException("Resource is locked right now. Try again later!");
                }

                redLocks.Add(redLock);
            }

            return await action();
        }
        finally
        {
            for (int i = redLocks.Count - 1; i >= 0; i--)
            {
                await redLocks[i].DisposeAsync();
            }
        }
    }
}
```

*Normalized*, at two spots: the single-key distributed method is named `RedLockAsync`
(the canonical drops the `Async` suffix on that one method only — the two are
overloads, distinguished by parameter type), and a third provider option has been
removed entirely, along with its dispatch branch and its dependency.

Seven properties of this shape are contract, not accident:

- **`SemaphoreLocker` is `static` while the service is scoped.** That is the only reason
  the in-memory provider excludes anything: a per-instance registry would give every
  request its own semaphore, and every caller would acquire immediately. If you ever
  make this instance state, the provider stops working and nothing reports it.
- **The value is `Lazy<SemaphoreSlim>`, not `SemaphoreSlim`.** `GetOrAdd`'s factory may
  run more than once under contention and the loser's object is discarded; wrapping it
  in `Lazy` guarantees that whichever entry wins the race, exactly one semaphore is ever
  *constructed and used* per key.
- **Both multi-key paths sort the keys before acquiring, and the single-key call reuses
  the list path.** A single global ordering is what makes multi-key locking
  deadlock-free; two callers requesting the same pair in opposite orders would otherwise
  each hold what the other needs. It is also why callers pass a list rather than nesting
  two `LockedAsync` calls — and why there is one acquisition algorithm per provider
  rather than two.
- **The distributed multi-key path disposes the failed lock before throwing**, and
  releases the acquired ones in reverse order in `finally`. A partial acquisition never
  leaks: the attempt that failed is handed back explicitly rather than left to a
  finalizer, and the locks already taken are always released.
- **`SemaphoreAsync` takes no options object at all.** No `WaitTime`, no `ExpiryTime`,
  no `RetryTime` — a caller waits on the semaphore for as long as it takes, and this
  path therefore **never throws `LockedException`**. The same call site produces two
  different observable behaviours depending on the provider: a `423` under one, an
  unbounded queue under the other.
- **Both `LockedAsync` overloads are non-`async`.** The switch returns the provider's
  task directly, so the dispatch adds no state machine and no extra allocation. Keep it
  that way; adding `async`/`await` here buys nothing.
- **The `default` arm throws `NotSupportedException`.** It is unreachable today and it is
  the guard that makes adding an enum member a loud failure instead of a silent
  fall-through.

**One honest note on the semaphore registry cleanup.** The `finally` block removes a
key's entry once `CurrentCount` reads `1`, which bounds the dictionary's growth —
without it, every key ever locked stays resident for the life of the process. The check
is not atomic with `GetOrAdd`: a caller can retrieve a semaphore an instant before it is
removed, after which a later caller for the same key adds a fresh one, and the two hold
different semaphores for the same key. The canonical code is kept as-is and this is not
presented as a defect to fix in passing — it affects only the in-memory provider, which
is not the one production call sites select, and any repair (never removing, or
reference-counting) is a design decision with its own costs. **Know it is there before
you rely on the in-memory provider for correctness.**

## `Startup.cs`

```csharp
using Infrastructure.Facades.Common.RedisCaches;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Infrastructure.Facades.Common.Services.ConcurrencyHandlers;

internal static class Startup
{
    internal static IServiceCollection AddConcurrencyHandler(this IServiceCollection services, IConfiguration configuration)
    {
        services
        .AddOptions<ConcurrencySettings>()
        .BindConfiguration(nameof(ConcurrencySettings))
        .ValidateDataAnnotationsRecursively()
        .ValidateOnStart();

        services.AddRedLock(configuration);
        services.AddScoped<IConcurrencyHandler, ConcurrencyHandler>();
        return services;
    }

    private static IServiceCollection AddRedLock(this IServiceCollection services, IConfiguration configuration)
    {
        // Needed at registration time, before anything can resolve IOptions<RedisSettings>.
        // This is the one sanctioned direct read of IConfiguration in this capability.
        RedisSettings settings = configuration
            .GetRequiredSection(nameof(RedisSettings))
            .Get<RedisSettings>()!;

        List<RedLockMultiplexer> multiplexer = new()
        {
            ConnectionMultiplexer.Connect(settings.ConnectionStrings.DefaultConnection),
        };

        return services.AddSingleton<IDistributedLockFactory>(RedLockFactory.Create(multiplexer));
    }
}
```

*Normalized:* the connection is read from the **extracted `RedisSettings` section** owned
by the cache capability. The canonical reads it back out of the persistence facade's
nested settings, which is where that project keeps it. Everything else here is canonical.

Five properties of this shape are contract, not accident:

- **`IDistributedLockFactory` is a singleton, and it is constructed eagerly.**
  `RedLockFactory.Create` runs during registration, not on first resolve, and the
  multiplexer it wraps connects at the same moment. One multiplexer per process is the
  point; a per-request one exhausts connections under load.
- **Composition therefore blocks on Redis**, and an unreachable server surfaces inside
  `AddInfrastructure` rather than at the first lock. That is the right shape here and the
  deliberate opposite of the cache's `AbortOnConnectFail = false` policy: a cache has a
  correct degraded mode — slower but right — and **a lock has none**. An application that
  starts without its lock server is an application whose guarded work is unguarded.
  Expect this line when you are debugging a startup hang.
- **This multiplexer is separate from the cache's connection** — same server, two
  connections, each owned by its capability. Do not rework either capability to share
  one; each keeps its own connection lifecycle, and the coupling would tie a cache
  restart to the lock's availability.
- **The options block and the direct read both stay, because they fail at different
  moments.** `GetRequiredSection` throws during registration when the section is absent
  entirely; `ValidateOnStart` throws at host start when a value inside it is missing,
  naming the property.
- **The handler is registered explicitly with `AddScoped`.** `IConcurrencyHandler` carries
  no lifetime marker interface, so an assembly scan will not find it — capabilities
  register their own services.

## The `ConcurrencySettings` section

This capability has **no dedicated configuration topic file**. Its section lives in the
base `appsettings.json`, and the section name equals the settings type name because
`BindConfiguration(nameof(ConcurrencySettings))` says so.

```json
{
  "ConcurrencySettings": {
    "Provider": "RedLock",
    "WaitTime": 10
  }
}
```

- **`WaitTime` is the one live value**: an `int` in seconds, read by a call site that
  needs an operationally tunable wait and converted there with `TimeSpan.FromSeconds(...)`.
  Most call sites do not read it and should not.
- **`Provider` is inert.** It binds and validates, and nothing reads it. Writing
  `"RedLock"` here does not make any call site use the distributed provider — only the
  per-call options object does that. It is kept for parity with the canonical and as a
  record of intent; treat it as documentation.
- Both properties carry non-default initializers, and `Required()` compares each
  property against its type default — so validation passes even when the section is
  absent entirely and the binding leaves the defaults in place. **Write the section
  anyway:** an absent section means the defaults are in force by accident rather than by
  decision, and there is no startup error to tell you which.
- The Redis connection is **not** configured here. It comes from the cache capability's
  `RedisSettings` section.

## The wiring line

```csharp
// Infrastructure/Startup.cs — appended to the same single fluent chain
services
    // … existing facades …
    .AddConcurrencyHandler(configuration);
```

`configuration` is passed because this capability reads it at registration time; most
lines in that chain take no argument. Its position in the chain is free — nothing in the
chain depends on it and it depends on nothing in the chain. Do know that it is one of
the few lines that opens a network connection while the chain runs.

## Normalizations at a glance

| Spot | Canonical | This scaffold | Reason |
|---|---|---|---|
| Settings file name | misspelled, does not match the type it declares | `ConcurrencySettings.cs` | A file is found by the type's name; the typo is a transcription slip, not a convention. |
| XML doc comments | Vietnamese | English | Artifact language, applied without changing what the comments say. |
| Provider options | three, one of which is a second in-memory option backed by an extra third-party package | two: in-memory and distributed | Two in-memory options is one decision nobody has to make, and it carried a dependency for a path production never selects. Removed with its dispatch branch and its package reference. |
| Single-key distributed method | no `Async` suffix, unlike its list counterpart | `RedLockAsync`, an overload of the list version | Two methods doing the same job under two naming rules; overloading by parameter type is what the pair already is. |
| Connection source | read back out of the persistence facade's nested settings | the extracted `RedisSettings` section | Settings follow their service. The cache capability owns that section and this capability shares its Redis; one connection string, one owner. |
