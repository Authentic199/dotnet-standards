---
name: distributed-lock
description: >-
  This skill should be used when two requests must not process one resource at
  once in a .NET solution: scaffolding ConcurrencyHandlers, injecting
  IConcurrencyHandler, wrapping a read-check-write in LockedAsync, composing a
  lock key or multi-key list, choosing ConcurrencyHandlerOptions (ExpiryTime,
  WaitTime, RetryTime), picking SemaphoreSlim vs RedLock, wiring
  IDistributedLockFactory and AddConcurrencyHandler, throwing or handling
  LockedException, or reviewing a double-processing or race-condition fix. Not
  for: cache keys, TTL, IRedisCacheService — distributed-caching; indexing,
  search descriptors — elasticsearch-search; background jobs, Hangfire —
  background-worker; DbContext, transactions, RowVersion tokens —
  ef-core-data-access; exception family, error envelope, middleware —
  error-handling; folder placement, composition root — facade-module-architecture.
---

## Overview

One capability owns mutual exclusion:
`Infrastructure/Facades/Common/Services/ConcurrencyHandlers/`. It exposes a single
service — `IConcurrencyHandler` — that runs a delegate while a named lock is held,
over one of two providers. Nothing else in the solution creates a lock, a semaphore
or an `IDistributedLockFactory` directly.

**Everything the lock protects runs inside the delegate.** The check that decides
whether the work may happen, the mutation, and the write all go into the
`Func<Task<TResult>>` passed to `LockedAsync`. A check performed before the call, or
a write issued after it returns, sits outside the lock and races exactly the way the
unlocked code did.

Answer "does this need a lock, which provider, and what key?" from this file alone.
Open a `references/` file only under the conditions stated below.

## Before scaffolding: find the lock capability that may already be there

Search the loading project for **any** existing mutual-exclusion capability before
creating one:

- a folder whose files own `IDistributedLockFactory`, `IRedLock` or a static
  `SemaphoreSlim` dictionary, under `Facades/`, `Facades/Common/` or a module;
- an `AddConcurrencyHandler`-, `AddRedLock`- or `AddDistributedLock`-shaped line in
  the `AddInfrastructure` chain, or in a facade `Startup.cs` it calls.

**If you find one, use it in place** — call the handler that already exists. Two lock
capabilities beside each other are worse than two caches: they hold *different* locks
under the same key names, so both callers acquire successfully and both proceed. The
symptom is the exact double-processing the lock was added to prevent, and nothing
logs it. This skill describes the capability it scaffolds; it makes no claim about
what an existing folder contains, so read that folder before extending it.

Only when the search finds nothing, scaffold — and scaffold **first**, before the
consuming code. Do not put a `SemaphoreSlim` field on a service, do not take a lock
straight from the factory at a call site, do not lock on a `static readonly object`,
and do not "temporarily" guard with a `ConcurrentDictionary` of in-flight ids. Each
of those is invisible to the next instance of the process.

## Placement & anatomy

Mutual exclusion is a **`Common` sub-capability** — substrate only some projects
take. Per the fractal rule it is shaped like a miniature facade: own folder, own
settings, own `Startup.cs`.

```
Facades/Common/Services/ConcurrencyHandlers/
├── ConcurrencyHandler.cs          # IConcurrencyHandler + ConcurrencyHandler, one file
├── ConcurrencySettings.cs         # ConcurrencySettings + the ConcurrencyProvider enum
├── ConcurrencyHandlerOptions.cs   # the per-call options object
└── Startup.cs                     # internal static class Startup → AddConcurrencyHandler(configuration)
```

- **It sits under `Common/Services/`, not directly under `Common/`**, beside the
  other cross-cutting services. *(Named once so nobody "fixes" it: this plugin's
  cache capability sits one level up, at `Facades/Common/RedisCaches/`. The two
  depths are inconsistent, and each is the canonical path for its own capability.
  Keep both as they are; moving an existing folder to make them agree is churn that
  breaks every namespace beneath it.)*
- **The `ConcurrencyProvider` enum stays in the settings file**, beside the property
  that names it. It is not a type consumers browse for on its own, and splitting it
  out buys a file for nothing.
- **Two settings types, and they are not interchangeable.** `ConcurrencySettings` is
  the bound configuration section, read at startup. `ConcurrencyHandlerOptions` is a
  plain object constructed per call and passed to `LockedAsync`. Configuration never
  reaches a call site by itself — see *Timing* for the one value that bridges them.
- Bound with the standard **four calls** — `AddOptions<ConcurrencySettings>()` →
  `BindConfiguration(nameof(ConcurrencySettings))` →
  `ValidateDataAnnotationsRecursively()` → `ValidateOnStart()`. `ConcurrencySettings`
  implements `IValidatableObject` and validates by returning
  `validationContext.Required()`.
- **The Redis connection comes from the cache capability's `RedisSettings` section.**
  *(Deviation, deliberate: the canonical project reads the connection back out of the
  persistence facade's `DatabaseSettings.RedisSettings` at this point. This plugin's
  `distributed-caching` skill extracts `RedisSettings` into the capability that owns
  it, so this scaffold reads the extracted section. Lock and cache share one Redis and
  must therefore share one connection string; a second copy in a second section drifts
  the day either is edited, and from that day every lock still "works" while excluding
  nothing.)*
- `internal static class Startup` exposing `AddConcurrencyHandler(configuration)`,
  composed by `Common`'s root `Startup`. It takes `IConfiguration` because the
  multiplexer is built during registration — see below. Composition is one line,
  `.AddConcurrencyHandler(configuration)`, in the `AddInfrastructure` chain.

| Registration | Lifetime, and why |
|---|---|
| `IDistributedLockFactory` | **Singleton**, built from a connection multiplexer. One multiplexer per process is the point; a per-request one exhausts connections under load. |
| `IConcurrencyHandler` | **Scoped**, registered explicitly with `AddScoped`. It carries no lifetime marker interface. |

**Composition is not free and it is not lazy.** `AddConcurrencyHandler` connects to
Redis *while the service collection is being built*, so a Redis problem surfaces
during composition rather than at first lock. Worth knowing before you debug a hang
in `AddInfrastructure` — and it is the right shape: unlike the cache, which is
deliberately configured to degrade to slower-but-correct, a lock has no correct
degraded mode, so it should not start pretending to hold one.

**Read `references/implementation.md` when** you are scaffolding the capability, or
writing or reviewing `ConcurrencyHandler.cs`, `ConcurrencySettings.cs`,
`ConcurrencyHandlerOptions.cs` or this capability's `Startup.cs` — it carries the full
file bodies, the normalization table and the acquisition/release details.

## Prerequisites — stop if any is missing

This capability is not self-contained. It needs three pieces that already have an
owner elsewhere in the solution:

| Prerequisite | Where it lives | Used for |
|---|---|---|
| the `RedisSettings` section owned by the cache capability | `Facades/Common/RedisCaches/` | the connection the distributed lock factory is built from |
| the `Required()` validation helper | `Facades/Common/Extensions/` (`ValidatorExtension`) | `ConcurrencySettings.Validate` |
| `LockedException` and the HTTP exception family it belongs to | `Core/Common/Exceptions/` | what a failed acquisition throws; the family and its middleware are `error-handling`'s |

**If any is absent from the loading project, stop. Report what is missing, propose
options — introduce the shared piece first, or narrow the task — and wait for a
decision.** Do not stand up a second Redis settings section, do not swap `Required()`
for hand-written attributes, and above all **do not invent a local exception for a
failed acquisition**: a lock timeout that surfaces as a `500`, or as a plain
`Exception` the middleware does not recognise, tells the caller "we are broken" when
the truth is "try again in a second" — and the retry that would have succeeded never
happens.

**Package quick-check, not a stop:** `RedLock.net` (`RedLockNet.SERedis`) and
`StackExchange.Redis`. Verify they resolve before writing files.

## Service surface

Interface **and** implementation in one file. Two overloads, one shape.

| Member | Use it for |
|---|---|
| `Task<TResult> LockedAsync<TResult>(string key, Func<Task<TResult>> action, ConcurrencyHandlerOptions? options = null, CancellationToken ct = default)` | One resource. |
| `Task<TResult> LockedAsync<TResult>(List<string> keys, Func<Task<TResult>> action, ConcurrencyHandlerOptions? options = null, CancellationToken ct = default)` | Several resources that must be held together. |

- **There is no `void`/`Task`-only overload, and do not add one.** The delegate
  returns a value, so the locked region is an expression whose result leaves the lock
  with you. A caller with nothing meaningful to return returns an id or a flag. The
  alternative — assigning to a captured local inside the delegate and reading it after
  the call — reads the variable outside the lock, which is the bug the lock existed to
  stop.
- `options` is optional and defaults to a new `ConcurrencyHandlerOptions`, whose
  provider default is the in-memory one. **Pass it explicitly.** Relying on the
  default is how a single-instance-correct lock ships to a multi-instance deployment.
- The **multi-key overload sorts the keys before acquiring** and releases in reverse.
  That global ordering is what stops two callers holding one another's keys, and it is
  why you pass a list rather than nesting calls.
- **Do not substitute nested single-key calls for the multi-key overload.** Nesting
  acquires in whatever order each call site happened to write, so two call sites that
  disagree block each other. What that looks like depends on the provider, and neither
  outcome is acceptable: on the in-memory provider, which has no timeout, it is a
  permanent deadlock; on the distributed provider both callers exhaust `WaitTime` and
  answer `423`. Either way it appears only under concurrency and never under test.
- Every overload takes a trailing `CancellationToken` and passes it down.

## Choosing the provider

| Provider | Scope of the lock | Correct when |
|---|---|---|
| `SemaphoreSlim` | one process | the application provably runs as a single instance, and always will |
| `RedLock` | every process sharing the Redis | anything else — including "we run one instance today" |

**Pass `Provider = ConcurrencyProvider.RedLock` explicitly at every call site.** This
is neither the enum's default nor a configured default; it is what every production
call site does, and the reason is that the in-memory provider's failure mode is
silent. Two instances behind a load balancer each acquire their own semaphore, each
believe they hold the lock, and both run the guarded work. Nothing throws, nothing
logs, and the duplicate shows up later as duplicate data.

**The in-memory provider ignores the options object entirely.** It takes no
`WaitTime`, no `ExpiryTime` and no `RetryTime` — a caller waits on the semaphore for
as long as it takes. It therefore **never throws `LockedException`**: under contention
it queues instead of rejecting, so a request that would have returned `423` in one
deployment simply blocks in the other. Two providers, two different observable
behaviours from the same call site. Choose deliberately; they are not a swappable
implementation detail.

`LockedException` pins **423 Locked** and is thrown when the distributed provider's
wait is exhausted. It bubbles like any other domain exception — the family it belongs
to, the response envelope and the middleware that shapes it are `error-handling`'s,
not this skill's. Do not catch it at the call site to retry: the waiting and retrying
already happened inside the acquisition attempt.

`ConcurrencySettings.Provider` exists, is bound and is validated — **and no code path
reads it.** The provider always comes from the per-call options object. Treat the
configured value as documentation of intent, not as a switch: changing it in
`appsettings` changes nothing at runtime. Do not add a fallback that makes it live
without first deciding, per call site, what that would change.

## Key discipline

A lock key is an interpolated string of the form **`{Noun}:{id}`** — a noun naming the
protected operation or resource, a colon, and the identifier. Nothing else. A key may
carry more than one id when the guarded resource genuinely is the combination.

- **Build it at the call site, or in a `private static` helper on the consuming
  service** when the same key is composed more than once in that file. There is no
  central key factory and none should be introduced: a lock key is meaningful only to
  the code that guards the resource, and a shared factory invites a second call site to
  reuse a key whose invariant it does not share.
- **The noun is the whole namespace.** Lock keys live in the same Redis as cache keys
  but receive **none** of the cache's prefix — that prefix is applied by the cache
  client, not by this capability. A bare identifier as a key is a genuine collision
  risk against every other key in the server.
- **Name the operation, not just the entity.** `OrderPayment:{orderId}` and
  `OrderCancellation:{orderId}` are two locks; `Order:{id}` is one lock serialising
  both, which is either too coarse or — worse — accidentally shared with a third
  feature that meant something else by it.
- **A key must be derivable by every caller that needs to be excluded.** If two paths
  guard the same resource by two different ids, they do not exclude each other. This is
  the most common real failure and it is invisible: both calls succeed.

*(Drift, noted once: one canonical call site passes a bare `Guid.ToString()` with no
noun. It works only because nothing else happens to use that guid as a key. Write the
noun.)*

**Read `references/usage-patterns.md` when** you are writing or reviewing a call site,
composing a multi-key lock, wrapping a transaction in a lock, or deciding what belongs
inside the delegate — it carries the single-key and multi-key patterns end to end.

## Timing: ExpiryTime, WaitTime, RetryTime

Three values on `ConcurrencyHandlerOptions`, each answering a different question. They
reach the distributed provider only.

| Value | Question it answers | Default |
|---|---|---|
| `ExpiryTime` | How long may the holder hold it before the lock is considered abandoned? | 30 s |
| `WaitTime` | How long does a competing caller wait before giving up? | 10 s |
| `RetryTime` | How often does a waiting caller re-attempt? | 200 ms |

**Size `ExpiryTime` against the worst-case duration of the guarded work, not its
typical duration.** The expiry exists so a crashed holder does not wedge the resource
forever, which makes it a deadline as well as insurance. The capability's own options
documentation warns what it expects when work outruns it: the session expires, a
second caller acquires legitimately, and two callers process one resource with no
error anywhere. *(That is the documented intent of the setting, not a measured
property of the client library — some distributed-lock clients renew a held lock while
its owner is alive. Size the expiry the same way either way; do not rely on renewal
you have not verified in the version you ship.)* If the honest worst case is
uncomfortably large, the fix is moving work out of the locked region, not shortening
the expiry.

**Exhausting `WaitTime` throws `LockedException`, and that is a feature.** It converts
unbounded queueing into a fast, retryable answer. Do not raise it to "reduce errors": a
caller waiting a minute for a lock has already blown its own timeout, and the queue
behind it keeps growing. `RetryTime` is the polling interval inside that window —
lower means faster pickup and more Redis chatter; the default is not a tuning target.

**One configured value reaches a call site: `ConcurrencySettings.WaitTime`, an `int` in
seconds**, converted with `TimeSpan.FromSeconds(...)` and assigned to `options.WaitTime`.
Do that only where the wait genuinely needs to be operationally tunable — a payment
path, say. Everywhere else the default is the right answer, and a config lookup at
every call site is noise that hides the one place it matters.

## Not this skill

Cache keys, TTL, `IRedisCacheService` and the cache's own `RedisSettings` binding →
`distributed-caching`. Indexing, search documents and query descriptors →
`elasticsearch-search`. Recurring and queued jobs, and Hangfire → `background-worker`.
`DbContext`, transactions and `RowVersion` optimistic-concurrency tokens →
`ef-core-data-access`. The exception family `LockedException` belongs to, the error
envelope and the middleware that maps it to `423` → `error-handling`. Where the
capability folder sits, facade anatomy and the composition root →
`facade-module-architecture`.
