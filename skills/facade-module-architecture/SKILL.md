---
name: facade-module-architecture
description: >
  Use when creating a .NET solution or adding a project, deciding where a new file
  belongs, adding a cross-cutting technical capability, wiring the composition root,
  or reviewing project references and dependency direction. Covers the
  Core/Infrastructure/Web layer chain, the Facades (technical) x Modules (business)
  split inside Infrastructure, self-registering Startup.cs extension methods,
  solution-level files (.sln, Directory.Build.props, package versions), the Options
  pattern, and service lifetimes.
  NOT for writing the handler, request or service inside one feature — use
  cqrs-feature-slice. NOT for DbContext, entities, migrations or queries — use
  ef-core-data-access. NOT for controllers, routes, DTOs or OpenAPI — use
  api-surface. NOT for JWT, policies or how secrets are stored — use
  auth-and-security. NOT for Serilog sinks, tracing or health endpoints — use
  observability. NOT for bootstrapping a brand-new repository from nothing — use
  project-scaffolding.
---

# Facade / Module Architecture

The layering used across these .NET services. It is **not** Clean Architecture and
**not** Vertical Slice Architecture. Do not describe it as either, and do not
"correct" a codebase toward either one.

## Core Principles

1. **Three layers, one direction.** `Core` → `Infrastructure` → `Web`. Every
   project reference points one way. There is no `Domain` project and no
   `Application` project.
2. **Business logic lives in `Infrastructure`.** This is deliberate. Clean
   Architecture would call it a violation; here it is the design. `Core` holds
   primitives only.
3. **Two axes inside `Infrastructure`.** `Facades/` holds technical capabilities
   (horizontal). `Modules/` holds business capabilities (vertical). A file belongs
   to exactly one of them.
4. **Every capability wires itself.** A facade owns a `Startup.cs` exposing
   `AddX()` / `UseX()` extension methods. The composition root is one flat fluent
   chain — never a pile of registrations in `Program.cs`.
5. **Registration order in `UseX` is the middleware pipeline order.** It is
   load-bearing behaviour, not formatting. Reordering it changes what the app does.

## The layer chain

```
Core  ←  Infrastructure  ←  Web
              ↖  Migrators.<Provider>  ←  Web
```

| Project | References | Holds |
|---|---|---|
| `Core` | nothing | Base types, shared interfaces, exception types, helpers. **No entities, no business rules.** A shared kernel of primitives. |
| `Infrastructure` | `Core` | Everything else: technical capabilities *and* business modules. |
| `Migrators.<Provider>` | `Infrastructure` | EF Core migration assemblies, one project per database provider. Nothing else. |
| `Web` | `Infrastructure`, `Migrators.*` | The host: `Program.cs`, `Controllers/`, `Configurations/`, and host assets. No business logic and no service registration of its own. Deliberately thin. |

`Web` referencing the migrator projects is intentional — the host must carry the
provider assemblies so migrations can be discovered and applied at startup.

## Where does this file belong?

| What you are writing | Where it goes |
|---|---|
| A base class, marker interface, custom exception, or generic helper | `Core/` |
| Anything talking to an external system (DB, cache, search, mail, storage, broker) | `Infrastructure/Facades/<Capability>/` |
| Cross-cutting request handling (middleware, validation, mapping, auth) | `Infrastructure/Facades/<Capability>/` |
| An entity, request/response model, service, validator or seeder for a business area | `Infrastructure/Modules/<BusinessArea>/` |
| The DI or pipeline wiring for a capability | `Infrastructure/Facades/<Capability>/Startup.cs` |
| An HTTP endpoint | `Web/Controllers/` |
| A configuration file | `Web/Configurations/<capability>.json` |

**If a file needs a reference that would reverse an arrow, the file is in the
wrong place.** Move the file; do not add the reference.

## Patterns

### Facade layout

A facade is a folder with a `Startup.cs` and whatever it needs. Names are technical,
never business — `Auth`, `Cache`, `Logging`, `Persistence`, `Validations`, `Mapping`,
`Middleware`, `Mailing`, `FileStorage`, `HealthChecks`, `BackgroundJobs`, `Cors`,
`OpenAPI`, `Common`.

```csharp
namespace Infrastructure.Facades.Auth;

internal static class Startup
{
    internal static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddCurrentUser()
            .AddPermissions()
            .AddJwtAuth(configuration);

    internal static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app) =>
        app.UseMiddleware<CurrentUserMiddleware>();

    private static IServiceCollection AddPermissions(this IServiceCollection services) =>
        services
            .AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>()
            .AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
}
```

`Startup` is `internal` by default. Make it `public` only when the host must call it
before `AddInfrastructure` runs — logging bootstrap is the usual reason.

### Module layout

A module is a business area, decomposed by technical role:

```
Infrastructure/Modules/<BusinessArea>/
├── Entities/
├── Requests/
├── Responses/
├── Mappings/
├── Validations/
├── Services/
└── Seeders/
```

**Modules have no `Startup.cs`.** They are registered by a convention-scanning
`AddServices()` living in `Facades/Common`. This asymmetry is intentional: a facade
decides its own wiring because it owns external resources; a module has nothing to
decide.

### The composition root

One file, two methods, one flat chain each.

```csharp
namespace Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddFluentValidation()
            .AddAutoMapper(cfg => cfg.AddCollectionMappers(), typeof(MappingProfile))
            .AddAuth(configuration)
            .AddPersistence()
            .AddCache()
            .AddServices();

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app, IConfiguration configuration) =>
        app
            .UseStaticFiles()
            .UseRouting()
            .UseCorsPolicy()
            .UseExceptionHandlerMiddleware()
            .UseAuthentication()
            .UseCurrentUser()
            .UseAuthorization()
            .UseHealthCheck();
}
```

`Program.cs` calls exactly these, plus the logging bootstrap and controller setup.
Adding a capability means adding one line here and one folder under `Facades/` — it
never means editing `Program.cs`.

## Anti-patterns

### Don't let target frameworks drift

Every project in the solution targets the same framework. A single project left
behind is a defect, not a detail — it silently changes which APIs and analyzers apply,
and it surfaces as confusing restore failures rather than as a clear error.

```xml
<!-- BAD — one test project left a version behind -->
<!-- src/Web/Web.csproj                       --> <TargetFramework>net8.0</TargetFramework>
<!-- tests/App.IntegrationTests/*.csproj      --> <TargetFramework>net7.0</TargetFramework>

<!-- GOOD — one value, and set it once in Directory.Build.props -->
<TargetFramework>net8.0</TargetFramework>
```

`references/solution-layout.md` carries the check that catches this.

### Don't register services in `Program.cs`

```csharp
// BAD — Program.cs grows with every capability
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

// GOOD — the facade owns its wiring; the root gains one line
builder.Services.AddInfrastructure(builder.Configuration);
```

### Don't put business logic in `Core`

`Core` is referenced by everything and depends on nothing. Business rules placed
there become unremovable coupling, and they are invisible to anyone reading
`Modules/`.

### Don't create a `Domain` or `Application` project

Both belong to Clean Architecture, which this is not. Introducing one splits the
layering across two incompatible models and leaves no honest answer to "where does
this file belong?".

## Decision Guide

| Scenario | Recommendation |
|---|---|
| New technical capability | New folder under `Facades/`, with its own `Startup.cs` |
| New business area | New folder under `Modules/`, no `Startup.cs` |
| Capability needs startup wiring | `AddX()` in the facade's `Startup.cs`, one line in `AddInfrastructure` |
| Capability needs middleware | `UseX()` in the facade's `Startup.cs`, placed by pipeline order in `UseInfrastructure` |
| Capability needs settings | See `references/configuration-and-options.md` |
| Choosing a service lifetime | See `references/dependency-injection.md` |
| Adding a project, package or solution-level file | See `references/solution-layout.md` |
| New database provider | New `Migrators.<Provider>` project referencing `Infrastructure` |
| Type used by two modules | `Core/` if it is a primitive; `Facades/Common/` if it has behaviour |

## References

- `references/solution-layout.md` — solution and project files, package versions,
  and the six solution-hygiene checks.
- `references/configuration-and-options.md` — the Options pattern, validation on
  startup, and the per-capability configuration file convention.
- `references/dependency-injection.md` — lifetimes, keyed services, decorators, and
  the captive-dependency bug.
