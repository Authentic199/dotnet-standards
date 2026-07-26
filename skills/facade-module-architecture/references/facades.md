## Infrastructure — the Facades axis

`Infrastructure` owns every piece of business logic and every integration, split on
two axes: **`Facades/` — technical capabilities** and **`Modules/` — business
capabilities**. Two questions place every new file:

1. **Would this code still make sense in a project with a completely different
   business domain?** If yes it is a technical capability and goes in `Facades/`.
   If it only means something in *this* product, it is a feature — `Modules/`.
2. **Is this capability a technology that many projects reuse?** Then it is a
   top-level facade (`Persistence`, `Cache`, `Auth`, `Mailing`, `MQTT`…). If it is
   shared substrate, or a capability only some projects need, it lives inside
   `Facades/Common/`. **Reach decides, not size** — a niche integration can grow to
   dozens of files across ten subfolders and still belong in `Common`, because the
   next project will not take that dependency.

The base facade set is these 21, and a production service keeps the set and grows
inside it: `Apm`, `Auth`, `BackgroundJobs`, `Cache`, `Common`, `Cors`,
`Definitions`, `ElasticSearch`, `FileStorage`, `HealthChecks`, `Identity`,
`Logging`, `Mailing`, `Mapping`, `Medias`, `Middleware`, `MQTT`, `Notification`,
`OpenAPI`, `Persistence`, `Validations`.

### Anatomy of a facade

Every facade owns exactly one `Startup.cs` — `internal static class Startup`, always
that name — exposing `AddX()`:

```csharp
namespace Infrastructure.Facades.Persistence;

internal static class Startup
{
    internal static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddOptions<DatabaseSettings>()
            .BindConfiguration(nameof(DatabaseSettings))
            .ValidateDataAnnotationsRecursively()
            .ValidateOnStart();

        services.AddDbContextPool<ApplicationDbContext>((provider, options) => { /* … */ });

        services.AddTransient<IDbInitializer, DbInitializer>()
            .AddRepositories();

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepositoryWrapper), typeof(RepositoryWrapper));

        return services;
    }
}
```

The rules this shape encodes:

- **`internal`, never `public`.** Only `Infrastructure/Startup.cs` composes facades;
  nothing outside the assembly calls them, so the facade is free to rearrange inside.
- **Options pattern, always the same four calls:** `AddOptions<T>()` →
  `BindConfiguration(nameof(T))` → `ValidateDataAnnotationsRecursively()` →
  `ValidateOnStart()`. Section name == type name, and bad configuration fails at
  startup instead of at first use.
- **Add `UseX()` only when the facade touches the request pipeline** (middleware,
  CORS, OpenAPI). It stays a one-liner in the same `Startup`:

  ```csharp
  internal static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app) =>
      app.UseMiddleware<CurrentUserMiddleware>();
  ```

- **Private helper extensions keep one entry point per facade.** When a registration
  block needs a name (`AddRepositories`), it becomes a `private static` extension in
  the same file, not a new public surface.
- **Singletons are registered explicitly here**, in the owning facade's `Startup` —
  see *What Core contains* for why they have no marker interface.

**Composite facades.** A facade with several independent sub-capabilities gives each
its own `Startup` and composes them at the root:

```csharp
internal static IServiceCollection AddCustomIdentity(this IServiceCollection services)
{
    services.AddGrantPermission();
    services.AddPasswordService();
    services.AddJwtTokenService();
    return services;
}
```

The `Persistence` facade also holds a repository abstraction over EF Core
(`RepositoryBase`, `IRepositoryWrapper`); placement is all this section claims —
usage belongs to the `ef-core-data-access` skill.

### `Facades/Common` in depth

`Common` is the facade every project has: shared substrate, plus a nursery for
capabilities that are not their own technology domain. Its base inventory and one
grown capability, side by side:

```
Facades/Common/
  Startup.cs            <- AddServices(): the marker scan
  ApplicationInfos.cs   <- the few genuinely global constants
  Extensions/  Attributes/  Converters/  Filters/
  Requests/    Responses/   Services/                  <- shared substrate
  HttpClients/                                         <- micro-capability
    Startup.cs   HttpClientSender.cs   HttpClientSettings.cs
  ChatPlatform/                                        <- same shape, more leaves
    Startup.cs
    Settings/  Models/  Entities/  Services/  Requests/  Workers/
```

**The fractal rule.** A new capability in `Common` is a subfolder structured like a
miniature facade — its own folders, its own settings, and its own `Startup.cs` if it
needs registration (`HttpClients/Startup.cs` exposes `AddHttpClientSender()`),
composed upward by `Infrastructure/Startup.cs` like any facade. A micro-capability
and a full third-party integration have the identical shape; only the number of
leaves differs. A capability keeps its settings class flat beside its `Startup` while
it is small and moves it into `Settings/` once the folder grows.

**`Common` owns the registration engine.** Its root `Startup` is the scan that makes
the Core lifetime markers real — which is why modules need no facade-style `Startup`:

```csharp
internal static IServiceCollection AddServices(this IServiceCollection services)
{
    services.Scan(scan => scan
        .FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
        .AddClasses(filter => filter.AssignableTo<ITransientService>())
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        .AddClasses(filter => filter.AssignableTo<IScopedService>())
            .AsImplementedInterfaces()
            .WithScopedLifetime()
    );

    return services;
}
```

### Settings follow their service

There is **no centralized settings folder.** A settings class lives with the code
that reads it, next to the `Startup` that binds it:

| Setting belongs to | Lives in | Bound by |
|---|---|---|
| A top-level facade | the facade root, named for the concern it configures — `Persistence/DatabaseSettings.cs`, `Auth/SecuritySettings.cs` — not after the facade | that facade's `Startup`, or one of its sub-`Startup`s |
| A `Common` sub-capability | beside the capability's `Startup.cs`, moving into `<Capability>/Settings/` as it grows | that capability's own `Startup.cs` |
| A business module | `Modules/<Feature>/Settings/`, holding `<Feature>Settings.cs` | a tiny `Startup.cs` in that same folder |

### `Auth` and `Identity` are two facades, not one

- **`Auth`** answers *is this caller allowed?* — JWT schemes, the permission policy
  provider / requirement / handler, the `HasPermission` attribute, current-user
  middleware.
- **`Identity`** answers *how do identities and their permissions work?* — token
  generation, password and recovery services, grant-permission (role and permission
  entities and their services).

`Identity` holds entities and services and looks like a business module. It is a
facade anyway, because every project that needs an account system reuses it whole —
it survives a change of business domain. This is the placement rule in its sharpest
form: **business-shaped is not the same as business-specific.** Only domain-specific
code goes in `Modules/`.

### Common mistakes

**A one-feature settings folder at the root of `Common/`**

```
❌ Facades/Common/<Feature>Settings/
       <Feature>Settings.cs        <- one class, alone, at Common's root

✅ Facades/Common/<Feature>/Settings/<Feature>Settings.cs
   (or Modules/<Feature>/Settings/ if the setting is business-specific)
```

A folder at `Common`'s root is a claim that a new shared capability exists. One
settings class is not a capability — it is a leaf of the feature that reads it. If
you are about to create a folder in `Common/` for one feature's configuration, stop:
it goes next to the feature.

**A centralized `Common/Settings/` folder**

```
❌ Facades/Common/Settings/          <- superseded: settings for unrelated
       Startup.cs                       capabilities collected in one place
       StaticFileSettings.cs

✅ each settings class inside the facade, capability or module that reads it
```

Older services carry this folder. It is the superseded pattern: it detaches
configuration from the code that consumes it and can only grow by pulling more
settings away from their owners. Do not add to one, and move a setting out when you
next touch its owner.
