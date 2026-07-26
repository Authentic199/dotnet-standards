# Dependency Injection

> **Provenance.** Lifetimes, keyed services, decorators, factories and the
> captive-dependency bug are `from-kit` (`dotnet-claude-kit` at `cd83d31`, TRIAGE
> row A14) — framework fact, independent of architecture. *Where registration lives*
> and *Registering modules by convention* are `from-my-code`. Kept apart on purpose.
>
> **Dated content.** Accurate as of **2026-07-26**. Keyed services require .NET 8 or
> later.

## Core Principles

1. **Constructor injection only.** No service locator, no property injection. If a
   type needs `IServiceProvider`, that is a signal — see *Factories* below for the
   cases where it is legitimate.
2. **Match lifetimes.** A longer-lived service must never hold a shorter-lived one.
   This is the most common DI bug in .NET, and it fails in production rather than in
   development.
3. **Registration lives with the thing being registered** — in the facade's
   `Startup.cs`, not in `Program.cs`.
4. **Register the interface, resolve the interface.** Registering the concrete type
   makes it impossible to decorate or substitute later.

## Where registration lives

*This section is `from-my-code`.*

Two mechanisms, split by what is being registered:

| What | How |
|---|---|
| A facade's services (technical capability) | Explicit registration in that facade's `Startup.cs` |
| A module's services (business capability) | Convention scanning — see below |

Explicit, because a facade's wiring is a decision worth reading:

```csharp
namespace Infrastructure.Facades.Auth;

internal static class Startup
{
    internal static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddCurrentUser()
            .AddPermissions()
            .AddJwtAuth(configuration);

    private static IServiceCollection AddPermissions(this IServiceCollection services) =>
        services
            .AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>()
            .AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
}
```

Return `IServiceCollection` from every helper so registrations chain. Keep the
public surface to the one `AddX` the composition root calls; everything else is
`private`.

### One instance, two interfaces

When two interfaces must resolve to the **same** instance within a scope, forward the
second to the first:

```csharp
internal static IServiceCollection AddCurrentUser(this IServiceCollection services) =>
    services
        .AddScoped<ICurrentUser, CurrentUser>()
        .AddScoped(sp => (ICurrentUserInitializer)sp.GetRequiredService<ICurrentUser>());
```

This matters more than it looks. The obvious version is wrong:

```csharp
// BAD — two separate CurrentUser instances per scope. Whatever the initializer
// writes, the consumer of ICurrentUser never sees.
services.AddScoped<ICurrentUser, CurrentUser>();
services.AddScoped<ICurrentUserInitializer, CurrentUser>();
```

Use the forwarding form whenever one object is written through one interface and read
through another — request-scoped context types are the usual case.

### Registering modules by convention

Module services are not registered individually. They implement a marker interface
from `Core`, and are scanned in:

```csharp
namespace Infrastructure.Facades.Common;

internal static class Startup
{
    internal static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
            .AddClasses(filter => filter.AssignableTo<ITransientService>())
                .AsImplementedInterfaces()
                .WithTransientLifetime()
            .AddClasses(filter => filter.AssignableTo<IScopedService>())
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        return services;
    }
}
```

**To add a module service, implement `IScopedService` or `ITransientService`.** There
is no registration line to write and no file to edit — that is the point of the
convention. The lifetime is chosen by which marker you implement, so picking the
marker *is* the lifetime decision; `IScopedService` is the right default for anything
touching the database.

The marker interfaces live in `Core/Common/Interfaces/` — that is why `Core` is
referenced by everything and depends on nothing.

Two characteristics of assembly scanning worth knowing: it only sees assemblies that
are **already loaded**, and a type is registered against *all* the interfaces it
implements (`AsImplementedInterfaces`). A module service implementing a second,
unrelated interface gets a registration for that one too.

## Lifetimes

| Lifetime | One instance per | Use for |
|---|---|---|
| `Singleton` | Application | Stateless helpers, caches, configuration holders, policy providers |
| `Scoped` | Request | Anything touching `DbContext`, per-request context, most business services |
| `Transient` | Resolution | Cheap, stateless, short-lived types |

**The rule:** a service may depend on its own lifetime or a longer one, never a
shorter one. Singleton → Scoped is the violation that matters.

### The captive dependency

```csharp
// BAD — a singleton holding a scoped DbContext. The context is never disposed,
// its change tracker grows forever, and every request sees stale data.
public sealed class LookupCache(AppDbContext db)
{
    public Task<Region?> GetAsync(int id) => db.Regions.FindAsync(id).AsTask();
}
services.AddSingleton<LookupCache>();

// GOOD — the singleton creates a scope per operation
public sealed class LookupCache(IServiceScopeFactory scopeFactory)
{
    public async Task<Region?> GetAsync(int id, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Regions.FindAsync([id], ct);
    }
}
```

The default container validates scopes in Development and throws on resolution, which
is why this reaches production: it is caught only if someone ran the path in
Development first. Background services are the classic escape route — a
`BackgroundService` is a singleton, so it must create a scope for every unit of work,
never inject a scoped service directly.

`IOptionsSnapshot<T>` is scoped and hits this same rule. Inside a singleton, use
`IOptionsMonitor<T>` — see `references/configuration-and-options.md`.

## Patterns

### Keyed services (.NET 8+)

Several implementations of one interface, selected by key. Replaces hand-written
factories and, in a MediatR pipeline, lets one handler pick a strategy without a
switch statement.

```csharp
services.AddKeyedScoped<INotificationSender, EmailSender>("email");
services.AddKeyedScoped<INotificationSender, SmsSender>("sms");

// Injected directly when the key is known at compile time
public sealed class WelcomeHandler([FromKeyedServices("email")] INotificationSender sender);

// Resolved at runtime when the key is data
public sealed class NotificationRouter(IServiceProvider provider)
{
    public INotificationSender For(string channel) =>
        provider.GetRequiredKeyedService<INotificationSender>(channel);
}
```

The router is one of the legitimate uses of `IServiceProvider` — the key is not known
until runtime, so there is nothing to inject.

### Decorator

Cross-cutting behaviour around an existing service, without touching it:

```csharp
services.AddScoped<IOrderService, OrderService>();
services.Decorate<IOrderService, CachingOrderService>();
```

Order matters: each `Decorate` wraps whatever is currently registered, so the last
call is the outermost layer.

For behaviour that should apply to *every* request rather than one service, a MediatR
pipeline behaviour is the better tool — see `cqrs-feature-slice`.

### Factory

When the implementation depends on configuration or runtime state:

```csharp
services.AddScoped<IStorageClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<StorageSettings>>().Value;
    return settings.Provider switch
    {
        "s3"    => ActivatorUtilities.CreateInstance<S3StorageClient>(sp),
        "local" => ActivatorUtilities.CreateInstance<LocalStorageClient>(sp),
        _ => throw new InvalidOperationException($"Unknown storage provider: {settings.Provider}")
    };
});
```

`ActivatorUtilities.CreateInstance<T>(sp)` resolves the constructor dependencies from
the container, so the concrete type keeps normal constructor injection. Throw on the
unknown case — returning a null or no-op client turns a configuration typo into
silent data loss.

## Anti-patterns

### Don't register in Program.cs

```csharp
// BAD — Program.cs grows one line per service, forever
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IOrderService, OrderService>();

// GOOD — the facade owns its wiring
builder.Services.AddInfrastructure(builder.Configuration);
```

### Don't make something a singleton to "save allocations"

```csharp
// BAD — holds a scoped dependency, and now holds request state across requests
services.AddSingleton<IOrderService, OrderService>();

// GOOD
services.AddScoped<IOrderService, OrderService>();
```

Scoped resolution is cheap. Choose the lifetime from what the service *holds*, not
from a guess about performance.

### Don't inject IServiceProvider to avoid a constructor

```csharp
// BAD — dependencies now invisible; failures move from startup to runtime
public sealed class OrderHandler(IServiceProvider provider)
{
    public Task Handle() => provider.GetRequiredService<IOrderService>().RunAsync();
}
```

Legitimate uses are narrow: keyed resolution by a runtime value, factory delegates,
and `IServiceScopeFactory` inside a singleton. Everything else is a service locator.

## Decision Guide

| Scenario | Recommendation |
|---|---|
| New facade service | Explicit registration in that facade's `Startup.cs` |
| New module service | Implement `IScopedService` or `ITransientService`; scanning handles it |
| Anything using `DbContext` | `Scoped` |
| Stateless helper, cache, policy provider | `Singleton` |
| Singleton needs a scoped service | Inject `IServiceScopeFactory`, create a scope per operation |
| Singleton needs settings | `IOptionsMonitor<T>` |
| Two interfaces, one instance per scope | Register one; forward the second with `sp => (I2)sp.GetRequiredService<I1>()` |
| Several implementations, key known at compile time | `[FromKeyedServices("key")]` |
| Several implementations, key known at runtime | `GetRequiredKeyedService<T>(key)` |
| Implementation chosen by configuration | Factory delegate + `ActivatorUtilities.CreateInstance<T>(sp)` |
| Wrapping one service with behaviour | Decorator |
| Wrapping every request with behaviour | MediatR pipeline behaviour — see `cqrs-feature-slice` |
| Auditing existing registrations | Read the facade `Startup.cs` files — the composition root is one chain. The Roslyn MCP `get_di_registrations` tool does it in one call if available, but is not required. |
