# Configuration and the Options Pattern

> **Provenance.** The Options pattern, validation and secrets guidance is
> `from-kit` (`dotnet-claude-kit` at `cd83d31`, TRIAGE row A10) — framework fact,
> independent of architecture. The *per-capability configuration files* section is
> `from-my-code` and describes an actual convention in this codebase. Kept apart on
> purpose.
>
> **Dated content.** Accurate as of **2026-07-26**.

Binding and validating settings is composition-root work, which is why it lives in
this skill rather than in the skill for whatever consumes the setting.

## Core Principles

1. **Options pattern always.** Never inject `IConfiguration` into a service. Bind a
   section to a class and inject that.
2. **Fail at startup, not at the first request.** `ValidateOnStart()` turns a
   mis-set value into a boot failure with a clear message, instead of a
   `NullReferenceException` under load an hour later.
3. **Secrets never live in source control.** Structure and defaults belong in the
   committed file; the value does not.
4. **Later sources override earlier ones.** Environment variables come last so a
   deployment can override anything without a rebuild.

## Patterns

### Bind and validate

```csharp
public sealed class CacheSettings
{
    [Required]
    public required string ConnectionString { get; init; }

    [Range(1, 3600)]
    public int DefaultTtlSeconds { get; init; } = 300;
}
```

```csharp
services.AddOptions<CacheSettings>()
    .BindConfiguration(nameof(CacheSettings))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Binding by `nameof(T)` keeps the section name and the class name in sync — renaming
the class in an IDE renames the binding, and a mismatch becomes a compile error
rather than a silently empty options object.

For rules that data annotations cannot express:

```csharp
services.AddOptions<JwtSettings>()
    .BindConfiguration(nameof(JwtSettings))
    .Validate(o => o.SigningKey.Length >= 32, "JWT signing key must be at least 32 characters")
    .Validate(o => o.ExpirationMinutes > 0, "JWT expiration must be positive")
    .ValidateOnStart();
```

One `Validate` call per rule, each with its own message. A single combined predicate
tells you something is wrong but not what.

### Choosing the injection interface

| Interface | Lifetime | Use when |
|---|---|---|
| `IOptions<T>` | Singleton, bound once at startup | The value never changes while the process runs. **The default.** |
| `IOptionsSnapshot<T>` | Scoped, re-read per request | Config genuinely changes at runtime and each request should see the current value. |
| `IOptionsMonitor<T>` | Singleton, with change notifications | A singleton or background service needs the current value. The **only** correct choice inside a singleton. |

```csharp
public sealed class TokenService(IOptions<JwtSettings> options)
{
    private readonly JwtSettings _jwt = options.Value;
}

public sealed class QueueWorker(IOptionsMonitor<WorkerSettings> options)
{
    private void Poll() => _ = options.CurrentValue.BatchSize;
}
```

Injecting `IOptionsSnapshot<T>` into a singleton throws at resolution — it is the
captive-dependency bug wearing a configuration costume. See
`references/dependency-injection.md`.

### Reading configuration before the container exists

The one legitimate exception to "never read configuration directly". Logging must be
configured before `builder.Build()`, so there is no service provider to resolve from:

```csharp
// Register for injection, as normal
builder.Services.AddOptions<LoggerSettings>()
    .BindConfiguration(nameof(LoggerSettings))
    .ValidateOnStart();

// AND read eagerly, because the logger is configured before the container is built
LoggerSettings settings = builder.Configuration
    .GetRequiredSection(nameof(LoggerSettings))
    .Get<LoggerSettings>()!;
```

`GetRequiredSection` rather than `GetSection` — a missing section should stop the
boot, not produce an object full of defaults. Use this pattern only for bootstrap
concerns; anywhere else it is the anti-pattern below.

### Secrets

Committed files carry **structure and non-secret defaults**. The value arrives from
somewhere else:

| Environment | Source |
|---|---|
| Local development | `dotnet user-secrets` |
| Deployed | Environment variables — the portable default, and what `AddEnvironmentVariables()` already reads |
| Deployed, with a managed secret store | A configuration provider for that store, e.g. Azure Key Vault |

A managed secret store is **one option among several**, not the assumed default;
adding a cloud-specific provider ties the app to that cloud. Environment variables
work everywhere and need no extra package.

Nested keys map to environment variables with `__` as the separator:
`CacheSettings__ConnectionString`.

## Per-capability configuration files

*This section is `from-my-code` — the convention used in these services.*

Rather than one large `appsettings.json`, configuration is split into **one file per
capability**, each with an optional environment overlay:

```
Web/Configurations/
├── appsettings.json          appsettings.Production.json
├── cache.json                cache.Production.json
├── database.json             …
├── logger.json
├── security.json
└── openapi.json
```

```csharp
internal static WebApplicationBuilder AddConfigurations(this WebApplicationBuilder builder)
{
    string environmentName = builder.Environment.EnvironmentName;

    builder.Configuration
        .AddJsonFiles(environmentName, "appsettings")
        .AddJsonFiles(environmentName, "logger")
        .AddJsonFiles(environmentName, "cache")
        .AddJsonFiles(environmentName, "database")
        .AddJsonFiles(environmentName, "security")
        .AddEnvironmentVariables();

    return builder;
}

private static IConfigurationBuilder AddJsonFiles(
    this IConfigurationBuilder builder, string environmentName, string fileName)
{
    const string configurationsDirectory = "Configurations";

    return builder
        .AddJsonFile($"{configurationsDirectory}/{fileName}.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"{configurationsDirectory}/{fileName}.{environmentName}.json", optional: true, reloadOnChange: true);
}
```

Three details that carry the weight:

- **The base file is `optional: false`.** A missing base file fails the boot. This is
  what makes the split safe — you cannot lose a capability's configuration silently.
- **The overlay is `optional: true`.** Not every environment needs to override every
  capability.
- **`AddEnvironmentVariables()` is last**, so a deployment can override any value
  from any file.

**Adding a capability means adding a file and a line here.** Name the file after the
facade, lowercase. The section inside it is named after the settings class, so
`BindConfiguration(nameof(CacheSettings))` finds it without further configuration.

## Anti-patterns

### Don't inject IConfiguration into a service

```csharp
// BAD — stringly typed, unvalidated, untestable, fails at call time
public sealed class CacheService(IConfiguration config)
{
    private int Ttl => int.Parse(config["CacheSettings:DefaultTtlSeconds"]!);
}

// GOOD
public sealed class CacheService(IOptions<CacheSettings> options)
{
    private int Ttl => options.Value.DefaultTtlSeconds;
}
```

### Don't commit real secret values

```json
// BAD — now in git history, effectively forever
{ "JwtSettings": { "SigningKey": "s3cr3t-signing-key" } }

// GOOD — shape and defaults only; the value comes from the environment
{ "JwtSettings": { "SigningKey": "", "Issuer": "myapp", "ExpirationMinutes": 60 } }
```

Committed once is committed permanently — rewriting history does not reach clones,
forks or backups. Treat an accidentally committed secret as compromised and rotate
it; do not just delete the line.

### Don't use Configure<T> when you meant to validate

```csharp
// BAD — binds, never validates; a missing value surfaces at first use
services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

// GOOD
services.AddOptions<JwtSettings>()
    .BindConfiguration(nameof(JwtSettings))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### Don't rely on ValidateDataAnnotations for nested objects

The built-in `ValidateDataAnnotations()` validates only the **top-level** properties
of the options class. Attributes on a nested settings object are not evaluated, so a
required field one level down passes validation while being empty.

Either validate the nested rules explicitly with `.Validate(...)`, or use a recursive
validation extension if the project already has one. Do not assume the nesting is
covered — check.

## Decision Guide

| Scenario | Recommendation |
|---|---|
| Binding a settings section | `AddOptions<T>().BindConfiguration(nameof(T))` |
| Value fixed for the process lifetime | `IOptions<T>` |
| Value changes per request | `IOptionsSnapshot<T>` |
| Reading settings inside a singleton or worker | `IOptionsMonitor<T>` |
| Simple field rules | `[Required]`, `[Range]` + `ValidateDataAnnotations()` |
| Cross-field or conditional rules | One `.Validate(predicate, message)` per rule |
| Nested settings objects | Explicit `.Validate(...)` — the built-in check does not recurse |
| Needed before `builder.Build()` | `GetRequiredSection(...).Get<T>()`, and register for injection too |
| Local secrets | `dotnet user-secrets` |
| Deployed secrets | Environment variables; a managed store if one is already in use |
| New capability's settings | New `Configurations/<capability>.json` + one line in `AddConfigurations` |
