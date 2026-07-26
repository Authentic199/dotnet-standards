## The composition root & configuration

Three files boot the entire system: `Web/Program.cs`, `Web/Configurations/Startup.cs`,
and `Infrastructure/Startup.cs`. Everything else is reached from them.

**`Web` registers nothing itself.** Every service line in `Program.cs` delegates to
Infrastructure. The only two decisions `Web` owns are controller JSON behavior and the
shape of the invalid-model response — both are properties of the HTTP surface, not of
any facade.

### `Program.cs`

```csharp
StaticLogger.EnsureInitialized();
Log.Information("Server Booting Up...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddConfigurations().RegisterSerilog();

    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.Converters.Add(new DateTimeOffsetConverter());
            options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
        })
        .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
            new BadRequestObjectResult(new
            {
                message = context.ModelState?
                    .FirstOrDefault(x => x.Value.ValidationState is ModelValidationState.Invalid)
                    .Value?.Errors[0].ErrorMessage
            }));

    var app = builder.Build();

    await app.Services.InitializeDatabasesAsync();

    app.UseInfrastructure(builder.Configuration);

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    StaticLogger.EnsureInitialized();
    Log.Fatal(ex, "Unhandled Exception");
}
finally
{
    StaticLogger.EnsureInitialized();
    Log.Information("Server Shutting Down...");
    await Log.CloseAndFlushAsync();
}
```

- **Static logger first, outside the `try`.** A configuration or DI failure happens before
  Serilog is configured from its topic file; without the bootstrap logger that crash is
  silent. `EnsureInitialized()` is idempotent, so `catch` and `finally` call it again.
  `finally` is the single flush point for the process — do not add a second one elsewhere.
- **`when (ex is not HostAbortedException)`.** That exception is how EF Core design-time
  tooling stops the host after building the service provider. Without the filter, every
  `dotnet ef` command logs a fatal error.
- **`AddConfigurations()` runs before `RegisterSerilog()`** — Serilog reads its own topic file,
  which does not exist in configuration until the first call has run.
- **`InitializeDatabasesAsync()` is awaited before `UseInfrastructure()` and `Run()`,** so a
  failed migration or seed stops startup instead of serving traffic against a bad schema.
- **The review test:** a new `builder.Services.Add…` line appearing in `Program.cs` is the
  smell. It belongs in a facade's or module's `AddX()`, reached through `AddInfrastructure`.

### `Web/Configurations/` — one topic, one file pair

Configuration is split by concern, never accumulated into one `appsettings.json`. The base
set is 13 topics, in load order:

`appsettings` · `logger` · `apm` · `hangfire` · `healthcheck` · `openapi` · `cors` ·
`filestorage` · `mail` · `security` · `database` · `httpclient` · `cache`

```csharp
internal static class Startup
{
    internal static WebApplicationBuilder AddConfigurations(this WebApplicationBuilder builder)
    {
        string environmentName = builder.Environment.EnvironmentName;
        builder.Configuration
                .AddJsonFiles(environmentName, "appsettings")
                .AddJsonFiles(environmentName, "logger")
                // … one line per topic, in the order listed above …
                .AddJsonFiles(environmentName, "cache")
                .AddEnvironmentVariables();

        return builder;
    }

    private static IConfigurationBuilder AddJsonFiles(
        this IConfigurationBuilder builder, string environmentName, string fileName)
    {
        const string configurationsDirectory = "Configurations";

        return builder
            .AddJsonFile($"{configurationsDirectory}/{fileName}.json",
                optional: false, reloadOnChange: true)
            .AddJsonFile($"{configurationsDirectory}/{fileName}.{environmentName}.json",
                optional: true, reloadOnChange: true);
    }
}
```

- **Each topic is a pair.** `<topic>.json` is **required** — a missing base file is a startup
  failure, not a silently empty section. `<topic>.<Environment>.json` is **optional** and
  overlays it. Both use `reloadOnChange: true`.
- **Declaration order is load order; later wins.** `AddEnvironmentVariables()` is last, so an
  environment variable beats every file — that is what makes container and CI deployment work
  without editing files, and where deployment secrets come from.
- **A facade with its own configuration gets a new topic file plus one `AddJsonFiles` line.**
  Name the file for the concern, not the vendor. Never grow `appsettings.json` instead. The
  settings *class* stays with its owner (see *Infrastructure — the Facades axis* and
  *Infrastructure — the Modules axis*); only the JSON topic and its one load line live here.

### `Infrastructure/Startup.cs` — the table of contents

`AddInfrastructure` composes every facade in one flat fluent chain — a single statement:

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services, IConfiguration configuration)
{
    services
        .AddFluentValidation()
        .AddAutoMapper(cfg => cfg.AddCollectionMappers(), typeof(MappingProfile))
        .AddAuth(configuration)
        .AddBackgroundJobs(configuration)
        .AddCorsPolicy(configuration)
        .AddHealthCheck(configuration)
        .AddOpenApiDocumentation(configuration)
        .AddS3AwsFileStorage()
        .AddMailing()
        .AddPersistence()
        .AddCustomIdentity()
        .AddServices()              // the marker-interface scan
        .AddHttpClientSender()
        .AddCache()
        .AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        // … one AddX() per additional facade or module that owns registrations

    return services;
}
```

- **Every line is a call into a facade or module** — no `AddScoped<…>()` for a concrete
  business type, no `AddOptions<T>().BindConfiguration(…)`. The only exception is framework
  plumbing that has no owner, such as the accessor singleton above.
- **Pass `configuration` only to the facades that need it** at registration time.
- **Unlike `UseInfrastructure` below, this chain is ordered for readability, not for
  semantics.** That is exactly why a duplicated line hides here — see the first mistake below.

A mature project appends more lines to the *same* chain. The file gets longer; the shape does
not change — still one `AddInfrastructure` statement, one `UseInfrastructure` statement.

### `UseInfrastructure` — the order IS the pipeline

```csharp
public static IApplicationBuilder UseInfrastructure(
    this IApplicationBuilder app, IConfiguration configuration)
{
    app
        .UseStaticFiles()
        .UseRouting()
        .UseApm(configuration)
        .UseCorsPolicy()
        .UseExceptionHandlerMiddleware()
        .UseAuthentication()
        .UseCurrentUser()
        .UseVerifyJwtUserMiddleware()
        .UseAuthorization()
        .UseHealthCheck()
        .UseBackgroundJobs(configuration)
        .UseOpenApiDocumentation();

    return app;
}
```

**The order of these calls is the middleware pipeline order** — this chain is not a list, it
is an execution sequence. The exception handler sits ahead of everything whose exceptions it
should convert into the standard error response; anything reading the authenticated principal
follows `UseAuthentication()` and `UseCurrentUser()`; anything depending on the authorization
result follows `UseAuthorization()`.

So append a new `UseX()` at the position its middleware must occupy, never at the end by
default — and treat a diff that moves a line here as a behavioral change, not a cleanup. It
still compiles either way.

### `InitializeDatabasesAsync`

The same file owns database startup. It creates a scope — the root provider cannot resolve
scoped services such as the DbContext — then applies pending EF Core migrations when the
auto-migration configuration flag is on (logging a warning and continuing when it is off, so
deployments that migrate out-of-band still work), and finally runs seeding through the
`IDbInitializer` abstraction. The root knows the interface, never the seeding logic.

### Common mistakes

**1. The same `AddX()` twice in one chain**

```csharp
❌  services
        .AddCache()
        .AddSearchIndex()          // ← here
        .AddNotifications()
        // … ten more lines …
        .AddCryptographic()
        .AddSearchIndex()          // ← and again
        .AddApiKeySettings();
```

Nothing fails loudly. Both registrations stay in the container: a single resolve gets the
last one, an `IEnumerable<T>` resolve gets both, and any option binding or hosted service
inside that `AddX()` runs twice. In a 35-line chain no reviewer sees it.

**Fix:** search the chain for the method name before appending. A long chain is the reason to
read it, not the excuse for not reading it — if the `AddX()` is already there, your work is
done.

**2. The chain broken apart around an inline `AddOptions<T>()`**

```csharp
❌  services
        .AddCache()
        // … long chain …
        .AddApiKeySettings();

    services.AddOptions<SomeFeatureSettings>()          // ← does not belong here
        .BindConfiguration(nameof(SomeFeatureSettings))
        .ValidateDataAnnotationsRecursively()
        .ValidateOnStart();

    services
        .AddMessageStreams(configuration)
        .AddCleanupJob();

✅  services
        .AddCache()
        // … long chain …
        .AddApiKeySettings()
        .AddSomeFeatureSettings()   // the binding lives with its owner
        .AddMessageStreams(configuration)
        .AddCleanupJob();
```

Two failures at once. The composition root is a table of contents, not a registration site —
the moment an `AddOptions<T>()` block appears here, its owner has lost track of it; bind it in
the owning facade's or module's `Settings/Startup.cs` and compose that instead. And splitting
the chain into three statements destroys the single-glance table of contents, which is what
lets the first mistake survive.
