# Integration testing: the fixture, the envelope and flows

The integration tier proves everything only a running host and a real database
can answer: the HTTP contract, the SQL a projection generates, and the effects
that cross a module boundary.

- [The integration fixture](#the-integration-fixture)
- [Sharing containers and resetting state](#sharing-containers-and-resetting-state)
- [Authenticating a test request](#authenticating-a-test-request)
- [Asserting through the settled envelope](#asserting-through-the-settled-envelope)
- [Seeding](#seeding)
- [Flow test cases](#flow-test-cases)
- [Packages](#packages)

## The integration fixture

One fixture boots the real host against a real database in a container. It is
the only place a test touches infrastructure configuration.

```csharp
// tests/<ProjectName>.IntegrationTests/Fixtures/ApiFixture.cs
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder().Build();
    private Respawner respawner = default!;

    public string ConnectionString => database.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:SqlSettings:DbProvider"] = "PostgreSql",
                ["DatabaseSettings:SqlSettings:ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["DatabaseSettings:SqlSettings:UseAutoMigration"] = "false",
            }));

        builder.ConfigureTestServices(services =>
            services.AddAuthentication(TestAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName, _ => { }));
    }

    public async ValueTask InitializeAsync()
    {
        await database.StartAsync();

        using IServiceScope scope = Services.CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>()
            .Database.MigrateAsync();

        await using DbConnection connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("public", "__EFMigrationsHistory")],
        });
    }

    public async Task ResetAsync()
    {
        await using DbConnection connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await respawner.ResetAsync(connection);
    }

    public override async ValueTask DisposeAsync()
    {
        await database.DisposeAsync();
        await base.DisposeAsync();
    }
}
```

- **Point the connection string at the container; do not re-register the
  `DbContext`.** Overwriting three configuration values hands the container's
  database to the registration the application actually ships — provider
  selection through `DbProviderKeys` and the matching migrations assembly
  included. Re-registering is also the fragile route: the context is **pooled**,
  and removing `DbContextOptions<T>` does not dislodge a pooled registration.
  A test whose host is wired differently from production is not an integration
  test of production.
- **`ConfigureTestServices`, not `ConfigureServices`, for the auth scheme.** It
  runs *after* the application's own registrations, so replacing a scheme
  actually wins; anything registered in `ConfigureServices` is overwritten by
  the app's own wiring.
- **Migrate explicitly and leave the auto-migration flag off.** The fixture then
  knows the schema exists before the first test, instead of depending on a
  production startup flag a configuration change could flip. Applying migrations
  — never `EnsureCreated` — is also what proves the migration chain still runs;
  a schema built any other way is not the schema that deploys.
- **xUnit v3 signatures are `ValueTask`.** `IAsyncLifetime.InitializeAsync`
  returns `ValueTask` (v2 returned `Task`), and because the interface inherits
  `IAsyncDisposable` you `override` the `ValueTask DisposeAsync()` that
  `WebApplicationFactory` already declares rather than adding a second method.
  A `Task InitializeAsync` copied from a v2 sample does not implement the
  interface, and the container never starts.
- **The container builder follows whichever provider the solution's `Migrators`
  projects target** — the examples use PostgreSQL; a solution migrating another
  engine builds that engine's container and sets `DbProvider` to match.
- **A Redis container is added the same way, and only when the path under test
  touches the cache.** Every container is start-up time paid by every run of the
  suite, including the runs that never use it.

`WebApplicationFactory<Program>` needs `Program` to be a nameable type, so the
host project's `Program.cs` ends with the sentinel `public partial class Program
{ }`. (An `[assembly: InternalsVisibleTo]` on the host works too, but it is
invisible at the call site; prefer the sentinel.)

## Sharing containers and resetting state

**Start the containers once for the whole suite and empty the database between
tests.** A container start plus migrations costs seconds; deleting the rows
costs milliseconds.

```csharp
[CollectionDefinition(nameof(ApiCollection))]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;

[Collection(nameof(ApiCollection))]
public abstract class IntegrationTestBase(ApiFixture fixture) : IAsyncLifetime
{
    protected ApiFixture Fixture { get; } = fixture;

    protected HttpClient Client { get; } = fixture.CreateClient();

    public ValueTask InitializeAsync() => new(Fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

- **`ICollectionFixture<T>`, not `IClassFixture<T>`.** A class fixture starts a
  container per test class. A collection fixture starts one for the collection
  and serializes the classes in it — which is required, not incidental: they
  share one database, and parallel tests resetting it would delete each other's
  rows.
- **Reset before a test, not after it.** xUnit builds a new instance per test
  method, so the base class's `InitializeAsync` runs before each one — a test
  that fails leaves its rows in place to be inspected, and a test that crashed
  before its own cleanup cannot poison the next.
- **The connection must be open before `Respawner.CreateAsync`.** It reads the
  foreign-key graph at construction to compute a delete order, which is why the
  reset is fast and why it needs no list of your tables.
- **Ignore the migrations history table.** Respawn excludes nothing by default,
  and deleting that table leaves a migrated schema that reports itself
  unmigrated to everything that asks afterwards.
- **Reset beats re-create.** Re-creating means dropping and re-migrating —
  seconds per test, and it throws away the warm connection pool. Respawn also
  does not reset identity or sequence values: never assert a generated key's
  *value*, assert the row.

## Authenticating a test request

**A test scheme replaces token verification with a principal the test states
outright.** The handler exists so a request reaches the endpoint; the real
schemes, how permissions are granted and how the policy reads them belong to
`auth-and-security`.

```csharp
public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string PermissionsHeader = "X-Test-Permissions";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        List<Claim> claims = [new(ClaimTypes.NameIdentifier, TestUsers.DefaultId.ToString())];

        if (Request.Headers.TryGetValue(PermissionsHeader, out StringValues permissions))
        {
            claims.AddRange(permissions.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(permission => new Claim(PermissionClaimType, permission)));
        }

        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, SchemeName));

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
```

```csharp
Client.DefaultRequestHeaders.Add(
    TestAuthenticationHandler.PermissionsHeader,
    Permissions.Orders + Operations.Create);
```

- **This replaces token *verification* only — it does not replace
  authorization.** `[HasPermission]` still evaluates the policy against the
  claims this handler issued, which is the point: a test that omits a permission
  must be denied, and that denial is a real one.
- **The permissions come from the request, not from a static field.** A mutable
  static that tests assign before acting is shared state, and the failure it
  produces is order-dependent and unreproducible.
- **Use the same permission constants the endpoint's attribute uses.** A test
  spelling the permission as a literal passes while the endpoint requires
  something else.
- **Take the constructor without `ISystemClock`.** That parameter is obsolete on
  .NET 8 and the three-parameter form is current — most examples online still
  show the old shape.
- **The claim type and the permission format must match what the real policy
  handler reads.** That pairing belongs to `auth-and-security`; read it there
  rather than reverse-engineering it from this handler.

## Asserting through the settled envelope

Four response shapes reach a test and **they are not the same body**. Reading the
wrong one turns a real failure into a deserialization error that names nothing.

| The outcome | Status | Body |
|---|---|---|
| Success | 200 | `SuccessResultWrapper<T>` — the wrapper the controller returned |
| A thrown application exception | the exception's pinned status | `ErrorResultWrapper` |
| A request the validator or model binding rejected | 400 | a plain `{ message }` object — **not** `ErrorResultWrapper` |
| A permission denial, or a route id that is not a GUID | 403 / 404 | **no body to assert** — status only |

**Success — the wrapper envelope** (`api-surface`):

```csharp
HttpResponseMessage response = await Client.PostAsJsonAsync("api/Orders", request);

response.StatusCode.ShouldBe(HttpStatusCode.OK);
SuccessResultWrapper<OrderResponse>? body =
    await response.Content.ReadFromJsonAsync<SuccessResultWrapper<OrderResponse>>();
body.ShouldNotBeNull();
body.Data.Id.ShouldNotBe(Guid.Empty);
```

Deserializing into the real wrapper type — rather than reading loose JSON — is
what makes the test fail if the envelope ever changes shape.

**Business not-found — 400 with `ErrorResultWrapper`, never 404**
(`error-handling`):

```csharp
HttpResponseMessage response = await Client.GetAsync($"api/Orders/{Guid.NewGuid()}");

response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
ErrorResultWrapper? error = await response.Content.ReadFromJsonAsync<ErrorResultWrapper>();
error.ShouldNotBeNull();
error.Message.ShouldBe(Messages<Order>.NotFound());
```

A test asserting 404 for a well-formed id that matches no row is asserting the
wrong contract. 404 belongs to routing — a *malformed* id dies on the
`{id:guid}` constraint before any code runs, which is a genuinely different test
and worth having.

**Automatic validation failure — the `{ message }` carve-out, not the middleware
envelope** (`error-handling`):

```csharp
HttpResponseMessage response = await Client.PostAsJsonAsync("api/Orders", new CreateOrderRequest());

response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
body.GetProperty("message").GetString()
    .ShouldBe(Messages<CreateOrderRequest>.Required(x => x.Code));
```

A rejected request never reaches the action and never throws, so the exception
middleware is not involved and there is no `ErrorResultWrapper` — same status,
different body. Deserializing this one as `ErrorResultWrapper` "succeeds",
reads `null` into every field and asserts nothing. Compose the expected string
the same request-typed way as the unit-level validator test (`unit-testing.md`),
so both suites break together when a key changes rather than drifting apart.

**`[HasPermission]` denial — bare 403, status only.** A policy denial
short-circuits the pipeline without throwing, so nothing envelopes it:
`response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);` and stop. Asserting a
body is asserting a response the stack does not produce.

**Never assert `Source`, `Method`, `Exception` or `Line`.** Production redaction
blanks them and default-valued members are omitted from the payload entirely, so
a test depending on them passes in one environment and fails in another. Assert
`Message` and the status.

## Seeding

**Seed through the fixture's own scope, using the real repository.**

```csharp
    protected async Task<T> SeedAsync<T>(T entity) where T : BaseEntity
    {
        using IServiceScope scope = Fixture.Services.CreateScope();
        IRepositoryWrapper repositoryWrapper =
            scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();

        await repositoryWrapper.Repository<T>().AddAsync(entity, CancellationToken.None);
        return entity;
    }

    protected async Task<T?> ReadAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
    {
        using IServiceScope scope = Fixture.Services.CreateScope();
        IRepositoryWrapper repositoryWrapper =
            scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();

        return await repositoryWrapper.Repository<T>()
            .Find(predicate, isAsNoTracking: true)
            .FirstOrDefaultAsync();
    }
```

`AddAsync` persists on its own — there is no separate save
(`ef-core-data-access`). Build the entity with a builder from `unit-testing.md`
and let the database assign the key.

- **Never seed by calling another endpoint over HTTP.** A create test that
  depends on the create endpoint cannot fail cleanly: one broken endpoint reds
  the whole suite, and the arrange step silently asserts a second contract.
- **Never seed through a substitute.** The point of this suite is that the row
  really exists with the real constraints applied; a double removes exactly what
  the test proves.
- **Read back in a *different* scope from the one that wrote.** The seeding
  context still tracks the graph it inserted, so asserting through it can pass on
  state that never reached the database.
- **`ReadAsync` is the read counterpart of `SeedAsync`**, resolving the
  repository in its own fresh scope so the assertion sees committed state rather
  than the change tracker's memory of it. `isAsNoTracking: true` because a test
  reads to assert, never to mutate.
- **A test running as a JWT-user principal must also seed that user's row** —
  the verification middleware re-checks the caller against the database and
  rejects a principal whose row is missing, so an unseeded caller fails before
  the endpoint runs. Which principals are subject to that check belongs to
  `auth-and-security`.

## Flow test cases

**A flow test is one `[Fact]` that walks a real sequence and asserts the state
the sequence accumulated.** It is ordinary test-class code — the same
`IntegrationTestBase`, the same reset, the same envelope shapes — that simply
lives longer inside one method.

### One module, one lifecycle

```csharp
[Fact]
public async Task Order_ConfirmedAfterCreation_AdvancesThroughEachState()
{
    // Create
    HttpResponseMessage created = await Client.PostAsJsonAsync("api/Orders", new CreateOrderRequest { Code = "ORD-0001" });
    created.StatusCode.ShouldBe(HttpStatusCode.OK);
    Guid orderId = (await created.Content.ReadFromJsonAsync<SuccessResultWrapper<OrderResponse>>())!.Data.Id;

    // The state the create left behind
    SuccessResultWrapper<OrderResponse>? fetched = await Client
        .GetFromJsonAsync<SuccessResultWrapper<OrderResponse>>($"api/Orders/{orderId}");
    fetched!.Data.Status.ShouldBe(FulfilmentStatus.Pending);

    // Confirm — legal only because the create above happened
    HttpResponseMessage confirmed = await Client.PostAsync($"api/Orders/{orderId}/Confirm", null);
    confirmed.StatusCode.ShouldBe(HttpStatusCode.OK);

    Order? persisted = await ReadAsync<Order>(x => x.Id == orderId);
    persisted.ShouldNotBeNull();
    persisted.Status.ShouldBe(FulfilmentStatus.Confirmed);
}
```

- **A flow test does not have one Arrange/Act/Assert — it has several**, and the
  step comments are what keep that readable. Assert at every step, not only at
  the end: both the envelope the step returned and the state it left behind. A
  flow whose middle steps are unchecked reports the last failure it happened to
  reach, and the step that actually broke is a guess.
- **A flow test earns its place only when the sequence *is* the behaviour** — a
  status that may move in only one direction, an operation legal only after
  another, an id minted by one step and consumed by the next, a total that
  accumulates. **Anything a seeded precondition can set up is a single-endpoint
  test**, which runs faster, fails at one place and names that place.
- **Never split a flow across three tests that depend on running in order.** The
  reset empties the database between tests and the runner promises no order, so
  the second test would find nothing — the machinery makes the mistake
  impossible, which is why the whole flow belongs in one method.
- **Name it for the behaviour** — `Order_ConfirmedAfterCreation_…` — never
  `CreateGetUpdate_Works`, which names the steps and says nothing about what is
  supposed to be true at the end.

### Crossing a module boundary

Capability crosses a module boundary as a message (`module-feature`), so
**trigger one module over HTTP and assert the other module's observable
outcome** — a row that appeared, a status that changed.

```csharp
[Fact]
public async Task Order_Confirmed_CreatesPendingShipment()
{
    // Arrange
    Order order = new OrderBuilder().WithStatus(FulfilmentStatus.Pending).Build();
    await SeedAsync(order);

    // Act
    HttpResponseMessage response = await Client.PostAsync($"api/Orders/{order.Id}/Confirm", null);

    // Assert — the observable outcome in the other module
    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    Shipment? shipment = await ReadAsync<Shipment>(x => x.OrderId == order.Id);
    shipment.ShouldNotBeNull();
    shipment.Status.ShouldBe(ShipmentStatus.Pending);
}
```

- **This is the only tier that proves the link runs at all.** The unit tier
  substitutes the mediator away, so an unregistered handler, an envelope nothing
  resolves, or a notification no one consumes passes every unit test and fails
  only here.
- **Assert the effect, never the dispatch.** A row that now exists, a status that
  changed — not that a message was sent. Dispatch is the implementation, and it
  is precisely what the two modules are allowed to change without breaking their
  contract.
- **Messaging is in-process, so the effect has already happened when the response
  returns** — no polling, no retry loop, and above all no `Task.Delay` in a test.
  If an effect is genuinely deferred to a background job, its assertion belongs
  with that job — `background-worker`.
- Which capability travels as a message rather than a direct call is
  `module-feature`'s law; envelope registration and pipeline behaviours are
  `mediatr-messaging`'s. This section only says how to see the result.

## Packages

```
tests/
  <ProjectName>.UnitTests/
  <ProjectName>.IntegrationTests/
```

Unit tests concentrate on the Infrastructure layer's services and validators;
integration tests target the Web host. Add packages by name and let `dotnet add
package` resolve the current stable version — a version pinned from memory is a
version that was current once.

**`tests/<ProjectName>.IntegrationTests`**

| Package | For |
|---|---|
| `xunit.v3` | the framework |
| `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` | discovery and the runner |
| `Shouldly` | assertions |
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory` |
| `Testcontainers.PostgreSql` | the database container — the module matching the provider |
| `Testcontainers.Redis` | only when a tested path uses the cache |
| `Respawn` | the between-test reset |
| `coverlet.collector` | coverage |
