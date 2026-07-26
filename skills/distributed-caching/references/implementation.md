# Scaffolding the cache capability

Use this file only when the project has no distributed-cache capability and you are
creating one. If it already has one, extend it in place — the guard below is how you
tell the difference.

## Pre-scaffold guard

Before creating anything, check both:

1. **Does any folder already own `IDistributedCache`?** A cache capability may sit
   under a different name than the one this file scaffolds.
2. **Does the `AddInfrastructure` chain already contain a cache registration** — an
   `AddCache()`, `AddRedisCache()` or similarly named line?

Either hit means the capability exists: stop scaffolding and use it in place. The
chain is ordered for readability rather than semantics, which is precisely why a
duplicate survives review in it — **search it by method name, do not skim it.** A
second registration fails silently: both stay in the container, a single resolve
gets the last one, and anything the registration does internally runs twice.

## Two prerequisites this scaffold never creates

Both are shared substrate with an owner elsewhere in the solution. Verify each
exists before writing any file.

**1. The JSON serializer.** `RedisCacheService` stores values as JSON strings and
delegates every conversion to `IJsonSerializerService` in
`Infrastructure/Facades/Common/Services/`. It needs two members:

```csharp
string Serialize<T>(T obj, Action<JsonSerializerOptions>? configs = null);

T? Deserialize<T>(string text, Action<JsonSerializerOptions>? configs = null);
```

The interface carries a solution-wide `DefaultOptions` — naming policy, reference
handling, encoder, case sensitivity — that every consumer inherits. Read it; do not
assume its contents. A cache that serializes under a different naming policy than
the rest of the solution changes the shape of every cached payload, and nothing
fails until something else reads one.

**2. The validation helper.** The settings classes below call
`validationContext.Required()`, an extension in
`Infrastructure/Facades/Common/Extensions/`:

```csharp
public static IEnumerable<ValidationResult> Required(
    this ValidationContext validationContext, params string[] ignoreProperties)
```

It reflects over every public property and reports any that is empty or still at its
type default, so a settings class opts into whole-object validation with one line
instead of per-property attributes. `ignoreProperties` is the escape hatch for a
genuinely optional property.

**If either is missing: stop, report, and let the caller choose.** Do not create
them, do not inline a `JsonSerializer.Serialize` call as a stand-in, do not
substitute `[Required]` attributes for the helper.

| Option | Consequence |
|---|---|
| Scaffold the missing piece first, as its own task | Lands in its correct home with its correct lifetime, reviewed on its own merits. Cache work waits. |
| Point at an equivalent already in the project | No new file. Requires confirming the behavior matches — a different serializer policy or a narrower validation rule is a silent divergence, not a detail. |
| Proceed without it | Not offered. The cache would own a policy that belongs to the whole solution. |

## Checklist

Work in this order. Each step is done only when the named artifact exists.

1. Create `Infrastructure/Facades/Common/RedisCaches/`.
2. Add `Microsoft.Extensions.Caching.StackExchangeRedis` to the `Infrastructure`
   csproj, on the version line matching the project's target framework. It supplies
   `AddStackExchangeRedisCache` and brings in `StackExchange.Redis`, which
   `RedisSettings` needs for `ConfigurationOptions`; projects that use that type
   directly commonly pin it explicitly as well.
3. Write `RedisCacheService.cs` — interface and implementation, one file.
4. Write `RedisSettings.cs` — beside the `Startup` that binds it.
5. Write `Startup.cs` — `internal static class Startup`, one `AddRedisCache()`.
6. Add `Web/Configurations/cache.json`, and its `.AddJsonFiles(environmentName,
   "cache")` line **only if that line is not already present** — `cache` is one of
   the base configuration topics, so in most projects it already is. Optionally add
   `cache.<Environment>.json`.
7. Append `.AddRedisCache(configuration)` to the `AddInfrastructure` chain, after the
   duplicate search from the guard above.

Steps 6 and 7 are where scaffolds get abandoned half-done. A capability that compiles
but is never composed, or is composed but has no configuration topic, is not finished.

## `RedisCacheService.cs`

```csharp
using Infrastructure.Facades.Common.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Facades.Common.RedisCaches;

public interface IRedisCacheService
{
    static string CacheKey<T>(string suffix) => $"{typeof(T).Name}:{suffix}";

    Task RemoveAsync(string key, CancellationToken ct = default);

    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task<T?> GetRemoveAsync<T>(string key, CancellationToken ct = default);

    Task<T> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);

    Task<T> SetAsync<T>(string key, T value, DistributedCacheEntryOptions options, CancellationToken ct = default);
}

public class RedisCacheService : IRedisCacheService
{
    private readonly IDistributedCache distributedCache;
    private readonly IJsonSerializerService serializerService;

    public RedisCacheService(IDistributedCache distributedCache, IJsonSerializerService serializerService)
    {
        this.distributedCache = distributedCache;
        this.serializerService = serializerService;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        string? value = await distributedCache.GetStringAsync(key, ct);
        if (string.IsNullOrEmpty(value))
        {
            return default;
        }

        return serializerService.Deserialize<T>(value);
    }

    public async Task<T?> GetRemoveAsync<T>(string key, CancellationToken ct = default)
    {
        T? value = await GetAsync<T>(key, ct);
        if (value is not null)
        {
            await RemoveAsync(key, ct);
        }

        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
        => await distributedCache.RemoveAsync(key, ct);

    public async Task<T> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        DistributedCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = expiration,
        };

        return await SetAsync(key, value, options, ct);
    }

    public async Task<T> SetAsync<T>(string key, T value, DistributedCacheEntryOptions options, CancellationToken ct = default)
    {
        string serialized = serializerService.Serialize(value);
        await distributedCache.SetStringAsync(key, serialized, options, ct);
        return value;
    }
}
```

Four properties of this shape are contract, not accident:

- **`SetAsync` returns the value it stored**, so a mutating method can end with
  `return await cache.SetAsync(key, entity);` instead of storing and returning
  separately.
- **A miss returns `default` and never throws**, which is what lets callers compose
  with `??` rather than guard every read.
- **`static CacheKey<T>` lives on the interface**, so callers build keys without
  resolving anything and there is exactly one definition of the convention.
- **`GetRemoveAsync` is read-then-delete, not atomic.** Two callers can both observe
  the value before either removes it. It is a convenience for one-shot reads, not a
  lock and not a token claim — if only one caller may win, this is the wrong tool.

One more thing the signatures hide: a `TimeSpan?` of `null` in the two-argument
`SetAsync` sets no absolute expiration, so the entry lives until it is removed or
evicted under memory pressure. Passing `null` is a decision, not a default.

## `RedisSettings.cs`

```csharp
using Infrastructure.Facades.Common.Extensions;
using StackExchange.Redis;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Facades.Common.RedisCaches;

public class RedisSettings : IValidatableObject
{
    public RedisConnectionStrings ConnectionStrings { get; set; } = new();

    public string CachePrefix { get; set; } = default!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required();

    internal ConfigurationOptions ConnectionConfiguration()
    {
        ConfigurationOptions configuration = ConfigurationOptions.Parse(ConnectionStrings.DefaultConnection);
        configuration.AbortOnConnectFail = false;   // a dead cache degrades the app, never kills it
        configuration.ConnectTimeout = 5000;
        configuration.ConnectRetry = 3;
        configuration.KeepAlive = 180;
        return configuration;
    }

    // Ensure the prefix ends with a colon
    internal string CleanedCachePrefix() => CachePrefix.EndsWith(':') ? CachePrefix : $"{CachePrefix}:";
}

public class RedisConnectionStrings : IValidatableObject
{
    public string DefaultConnection { get; set; } = default!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required();
}
```

*Normalized:* the canonical source nests both classes inside the persistence facade's
`DatabaseSettings`. They are extracted here because settings follow their service —
the class belongs beside the `Startup` that binds it, in its own `RedisSettings`
section. Everything inside the classes is canonical and unchanged.

**Both classes implement `IValidatableObject`, and they have to.** `Required()`
inspects one object's own properties; the outer call proves `CachePrefix` is set, and
the nested class's own `Validate` is what proves `DefaultConnection` is set. Drop the
inner one and a missing connection string passes validation and fails at first
connect. The nested `Validate` runs only because the binding chain in `Startup` uses
`ValidateDataAnnotationsRecursively()` — the non-recursive variant stops at the root.

`AbortOnConnectFail = false` is the load-bearing one of the four connection values:
left at its default, an unreachable cache turns a degradation into a failed startup.
The other three bound the connect attempt and keep idle connections alive through NAT
and load-balancer timeouts.

`CleanedCachePrefix()` is not cosmetic. The prefix is a namespace separator, and
without the trailing colon it glues onto the first character of the next segment.

## `Startup.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Facades.Common.RedisCaches;

internal static class Startup
{
    internal static IServiceCollection AddRedisCache(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RedisSettings>()
            .BindConfiguration(nameof(RedisSettings))
            .ValidateDataAnnotationsRecursively()
            .ValidateOnStart();

        // Needed at registration time, before anything can resolve IOptions<RedisSettings>.
        // This is the one sanctioned direct read of IConfiguration here.
        RedisSettings redisSettings = configuration
            .GetRequiredSection(nameof(RedisSettings))
            .Get<RedisSettings>()!;

        services.AddStackExchangeRedisCache(opt =>
        {
            opt.ConfigurationOptions = redisSettings.ConnectionConfiguration();
            opt.InstanceName = redisSettings.CleanedCachePrefix();
        });

        // IRedisCacheService carries no lifetime marker, so the assembly scan will not
        // find it. Capabilities register their own services explicitly.
        services.AddScoped<IRedisCacheService, RedisCacheService>();

        return services;
    }
}
```

*Normalized*, at three spots:

1. **`internal static class Startup`** — the canonical class is `public`. Only the
   composition root composes capabilities, so `internal` keeps the capability free to
   rearrange behind its one entry point.
2. **The Options four calls are added** — the canonical binds nothing and reads
   settings only at registration. Binding lets any consumer inject
   `IOptions<RedisSettings>`, and makes a bad `cache.json` fail at startup.
3. **The entry point is renamed.** The canonical names the method `AddRedisService`;
   the normalized name is `AddRedisCache`, named for the concern it registers.

**The options block and the direct read both stay, because they fail at different
moments.** `GetRequiredSection` throws during registration when the section is absent
entirely; `ValidateOnStart` throws at host start when the section is present but a
value is missing, naming the property. Delete either and a class of misconfiguration
moves from startup to the first cache hit in production.

## `Web/Configurations/cache.json`

The section name equals the settings type name, because `BindConfiguration(nameof(
RedisSettings))` says so. Shape only, placeholder values:

```json
{
  "RedisSettings": {
    "ConnectionStrings": {
      "DefaultConnection": "<host>:<port>"
    },
    "CachePrefix": "<application-key>"
  }
}
```

- `DefaultConnection` is parsed by `ConfigurationOptions.Parse`, so extra options
  append comma-separated: `<host>:<port>,<option>=<value>`.
- **Credentials never go in this file.** Environment variables load last and beat
  every JSON file — that is where a deployed password or TLS setting comes from.
- The base `cache.json` is required and must exist even when every value is overlaid.
  `cache.<Environment>.json` is the optional overlay.

## The two wiring lines

```csharp
// Web/Configurations/Startup.cs — only if this line is not already present
builder.Configuration
        // … existing topics, in load order …
        .AddJsonFiles(environmentName, "cache")
        .AddEnvironmentVariables();
```

```csharp
// Infrastructure/Startup.cs — appended to the same single fluent chain
services
    // … existing facades …
    .AddRedisCache(configuration);
```

`configuration` is passed because this capability reads it at registration time; most
lines in that chain take no argument.

## What a stored key looks like

Three segments combine, and no single file shows the result:

```
{CachePrefix}:{TypeName}:{suffix}
└─ InstanceName ─┘└─ IRedisCacheService.CacheKey<T> ─┘
```

`InstanceName` is set to `CleanedCachePrefix()`, so `IDistributedCache` prepends it to
every key the service passes. `CacheKey<T>("42")` produces `TypeName:42`. A caller
that hand-writes a raw string key skips the type segment and lands in a different
namespace from everything else cached for that type, where it will neither be found
nor invalidated — build keys through `IRedisCacheService.CacheKey<T>()`.

## Normalizations at a glance

| Spot | Canonical | This scaffold | Reason |
|---|---|---|---|
| `Startup` visibility | `public static class` | `internal static class` | Only the composition root composes capabilities. |
| Entry point | `AddRedisService` | `AddRedisCache` | Named for the concern it registers. |
| Settings home | nested in the persistence facade's `DatabaseSettings` | own file in `RedisCaches/` | Settings follow their service. |
| Bound section | `DatabaseSettings` → `.RedisSettings` | `RedisSettings` | Section name equals type name. |
| Options binding | none; registration-time read only | four-call options block **plus** the registration-time read | Fail at startup, not at first use. |
