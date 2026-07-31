# Unit testing: services, validators and test data

The unit tier proves decisions — what a service guards against, what a
validator rejects, what the mapping configuration must satisfy — and stops at
the line where the method leaves C# and becomes SQL.

- [A service unit test](#a-service-unit-test)
- [Where a unit test stops: the projecting read](#where-a-unit-test-stops-the-projecting-read)
- [A validator unit test](#a-validator-unit-test)
- [The one mapping configuration test](#the-one-mapping-configuration-test)
- [Test data builders](#test-data-builders)
- [Time](#time)
- [An outbound third-party dependency](#an-outbound-third-party-dependency)
- [Packages](#packages)

## A service unit test

A module service takes its whole world through one constructor —
`IRepositoryWrapper`, `IMapper`, and at most a cross-module service interface.
Substitute those, construct the service directly, and assert what the caller
would see: the response, or the thrown sealed exception and its message.

```csharp
public class OrderServiceTests
{
    private readonly IRepositoryWrapper repositoryWrapper = Substitute.For<IRepositoryWrapper>();
    private readonly IRepositoryBase<Order> orders = Substitute.For<IRepositoryBase<Order>>();
    private readonly IMapper mapper = Substitute.For<IMapper>();
    private readonly OrderService sut;

    public OrderServiceTests()
    {
        repositoryWrapper.Repository<Order>().Returns(orders);
        sut = new OrderService(repositoryWrapper, mapper);
    }

    [Fact]
    public async Task CreateAsync_WhenCodeAlreadyExists_ThrowsBadRequestException()
    {
        // Arrange
        orders.AnyAsync(Arg.Any<Expression<Func<Order, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);
        CreateOrderRequest request = new() { Code = "ORD-0001" };

        // Act
        Func<Task> act = () => sut.CreateAsync(request, CancellationToken.None);

        // Assert
        BadRequestException exception = await act.ShouldThrowAsync<BadRequestException>();
        exception.Message.ShouldBe(Messages<Order>.AlreadyExist(x => x.Code));
        await orders.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }
}
```

- **`Repository<T>()` is the seam you configure first.** The wrapper hands out one
  repository per entity type, so a substituted wrapper returns a substituted
  `IRepositoryBase<T>` and every later `Repository<Order>()` call in the method
  under test resolves to the same object.
- **Match an expression argument with `Arg.Any<Expression<Func<T, bool>>>()` and
  assert the outcome instead.** Expression trees do not compare by value, so
  pinning the predicate means either string-matching a compiler-generated shape or
  asserting the code you just wrote. Whether the predicate *selects the right rows*
  is a question only a database can answer.
- **Compose the expected message from the same `Messages<T>` call the code under
  test makes, never a literal.** A literal turns a key change into a failure in a
  file that has nothing to do with keys. Which key an operation uses is
  `message-keys`' law; which exception pins which status is `error-handling`'s.
- **`Received`/`DidNotReceive` earns its place in exactly two shapes, and neither
  is a happy path.** A guard's whole promise is that it rejects *before* touching
  state, and `DidNotReceive().AddAsync(...)` is the only way to see that at this
  tier. A `catch` that must unwind a transaction promises
  `RollbackTransactionAsync` ran and the original exception kept travelling — same
  reasoning. Never write `Received(1).AddAsync(...)` to prove a save happened: that
  asserts the service called a method, and the integration test proves the row by
  reading it back.

### Where a unit test stops: the projecting read

Every operation returns a response type, and a write returns by re-reading through
the projection — so the success path of nearly every service method ends in the
same construct:

```csharp
    .Find(x => x.Id == orderId, isAsNoTracking: true)
    .ProjectTo<OrderResponse>(mapper.ConfigurationProvider)
    .FirstOrDefaultAsync(cancellationToken);
```

**That line cannot be unit tested, and two independent things break on it.** `Find`
hands back an `IQueryable<T>`; if a substitute returns `list.AsQueryable()`, the
LINQ-to-Objects provider has no `IAsyncQueryProvider` and `FirstOrDefaultAsync`
throws at runtime. Separately, a substituted `IMapper` returns `null` for
`ConfigurationProvider`, so `ProjectTo` fails before the provider is even reached.

**Route every projecting path to the integration tier.** A package such as
MockQueryable can supply a real async provider and was considered and declined:
making the provider work would only prove that LINQ-to-Objects can run the
expression, not that EF Core can translate the projection to SQL — which is the
failure that actually ships. A hand-rolled `TestAsyncQueryProvider` mirrors EF Core
internals and drifts with them. Unit tests therefore cover what happens *before*
the query composes — guards, branches, what is thrown, what is rolled back — and
stay on the Task-returning members where decisions live: `AnyAsync`, `CountAsync`,
`GetByIdAsync`, and the mutation members. The read itself is proved once, against a
real database — `integration-testing.md`.

## A validator unit test

`FluentValidation.TestHelper` runs a validator against a request and gives the
error set a fluent surface. Substitute whatever the validator's constructor
declares.

```csharp
public class OrderRequestValidatorTests
{
    private readonly IRepositoryWrapper repositoryWrapper = Substitute.For<IRepositoryWrapper>();
    private readonly IActionAccessorService actionAccessorService = Substitute.For<IActionAccessorService>();

    [Fact]
    public void Validate_WhenNameIsEmpty_HasRequiredError()
    {
        // Arrange
        actionAccessorService.GetAction().Returns("Create");
        OrderRequestValidator validator = new(repositoryWrapper, actionAccessorService);

        // Act
        TestValidationResult<OrderRequest> result = validator.TestValidate(new OrderRequest { Name = null });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(Messages<OrderRequest>.Required(x => x.Name));
    }
}
```

- **A validator message is typed to the request, not the entity** — the request
  carries `[MessageDisplay]`, so the key still reads as the entity. Compose the
  expectation from the same call the validator makes; a literal defeats the point.
- **`ShouldNotHaveValidationErrorFor(x => x.Name)` is the other half of the pair**,
  and it is the assertion that a rule did *not* fire — use it on the valid-input
  case rather than asserting an empty error set.
- **A rule fenced by `.When(_ => action == "Create")` is testable only because the
  action accessor is injected.** Stub `GetAction()` to each action the rule
  distinguishes; a validator whose conditional rules are never exercised under both
  actions is half-tested.
- **A `.Must(...)` predicate that reads the database is an extension method on the
  repository abstraction, and an extension method cannot be substituted.**
  NSubstitute configures only members the interface declares, so the predicate's
  real body runs against your substituted wrapper. Configure the wrapper member it
  calls underneath — typically `Repository<T>().AnyAsync(...)` — and if the
  predicate does more than delegate, test that rule at the integration tier
  instead — `integration-testing.md`.
- **The test helper carries its own assertions for the error set; Shouldly covers
  everything else.** Which checks belong in a validator versus a `ThrowIf…` guard
  is `module-feature`'s law; this section only proves the declared rules fire when
  they should.

## The one mapping configuration test

```csharp
[Fact]
public void MapperConfiguration_ForEveryProfileInTheAssembly_IsValid()
{
    MapperConfiguration configuration = new(cfg => cfg.AddMaps(typeof(OrderResponse).Assembly));

    configuration.AssertConfigurationIsValid();
}
```

**One test suffices because profiles are found by assembly scan, not by
registration.** Every request and response declares its `Profile` in its own file
and the scan picks all of them up, so this test covers the profile somebody adds
next month without anyone remembering to extend it. It walks every `CreateMap` and
fails on any destination member that no source member and no `ForMember` accounts
for, naming the map and the member. A test per profile would be one file per DTO
and would still miss the map nobody wired up. The type in `typeof(...)` is only a
marker for the assembly the DTOs live in.

## Test data builders

Requests are initializer-friendly POCOs — build them inline. Reach for a builder
when an entity with required fields repeats across tests, so a test can override
only the field it is about.

```csharp
public class OrderBuilder
{
    private string code = "ORD-0001";
    private Guid customerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private FulfilmentStatus status = FulfilmentStatus.Pending;

    public OrderBuilder WithCode(string value)
    {
        code = value;
        return this;
    }

    public OrderBuilder WithStatus(FulfilmentStatus value)
    {
        status = value;
        return this;
    }

    public Order Build() => new()
    {
        Code = code,
        CustomerId = customerId,
        Status = status,
    };
}

Order shipped = new OrderBuilder().WithStatus(FulfilmentStatus.Shipped).Build();
```

- **The line a test writes is the whole reason the test exists.** `WithStatus` in
  the arrange section says *this test is about a shipped order*; the fields it does
  not name are noise the builder absorbs.
- **Defaults are fixed values, not generated ones.** A failing test must be
  reproducible from its own source: a build that fails once and passes on the retry
  is worse than no test, and a value nobody chose cannot be reasoned about from a
  CI log.
- **A builder never sets `Id`.** `BaseEntity` assigns a sequential GUID in its
  constructor, so a built entity has its identity before anything is saved — and a
  test that pins `Id` is pinning a value the production path generates.
- One builder per type, named `<X>Builder`, living in the test project beside the
  tests that use it.

## Time

`BaseEntity` defaults `CreatedAt` to `DateTimeOffset.UtcNow`, so an entity's own
timestamp is not something a test can control — assert it with a tolerance,
`created.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5))`, rather than
pinning it. If the type under test takes a `TimeProvider` through its constructor,
substitute `FakeTimeProvider`, move the clock, and assert exact instants:

```csharp
FakeTimeProvider clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
clock.Advance(TimeSpan.FromDays(31));
```

Where a type reads the clock directly, assert what the timestamp is *used for* — a
status, a decision, an inclusion in a result — rather than the value itself. When
time drives a decision a test needs to control, taking `TimeProvider` through the
constructor is the change worth making; it is a recommendation, not a requirement.

## An outbound third-party dependency

**When the path under test calls a third-party HTTP API, hand its typed client an
`HttpMessageHandler` stub that returns the response the test needs** — no
additional package, and no container for something that is not yours to run.

Where the path reaches that API through the house's sender facade rather than holding
a client of its own, the seam is the facade: substitute `IHttpClientSender` and return
the `HttpResult` the path expects. `http-client-factory` describes that result —
including that the sender catches transport failures and returns a `500` instead of
throwing, which is the behaviour a faithful double reproduces.

## Packages

Add packages by name and let `dotnet add package` resolve the current stable
version — a version pinned from memory is a version that was current once.

**`tests/<ProjectName>.UnitTests`**

| Package | For |
|---|---|
| `xunit.v3` | the framework |
| `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` | discovery and the runner |
| `Shouldly` | assertions |
| `NSubstitute` | substitutes at the facade boundary |
| `coverlet.collector` | coverage on `dotnet test --collect:"XPlat Code Coverage"` |

`FluentValidation.TestHelper` is a namespace inside `FluentValidation` — the
validator tests need no package beyond the reference the project already has.
