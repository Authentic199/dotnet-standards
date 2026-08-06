---
name: dotnet-testing
description: >-
  This skill should be used when writing or reviewing tests in a .NET
  solution: adding a unit test or a WebApplicationFactory integration test, a
  test project under tests/<ProjectName>.UnitTests or .IntegrationTests, xUnit
  v3, Shouldly, NSubstitute doubles for IRepositoryWrapper, Testcontainers
  fixtures, fixture scope, Respawn resets or disjoint test data, a flaky or
  non-deterministic suite, a test authentication handler,
  FluentValidation.TestHelper validator tests, AutoMapper
  AssertConfigurationIsValid, or a test data builder. Not for: red-green-refactor process —
  superpowers:test-driven-development; validation rules — module-feature;
  endpoints, DTOs — api-surface; repositories, DbContext, migrations —
  ef-core-data-access; JWT, policies, permission internals —
  auth-and-security; exception shapes — error-handling; message text —
  message-keys; project placement — facade-module-architecture.
---

## Overview

Tests here split by tier, and the split is the doctrine: **an integration test
proves behaviour against a running host and a real database, and a unit test
proves decisions with the facade boundary substituted.** Nothing in between —
no in-memory provider standing in for the database, no double standing in for
the thing under test. Which tier a question belongs to is usually the whole
answer, and the Decision Guide below settles the cases that are not obvious.

This skill covers what to test and how to write it. The red-green-refactor
discipline of writing the test first is a process, not a stack convention, and
lives in `superpowers:test-driven-development`.

## Core Principles

1. **Integration tests are the highest-value tests.** One `WebApplicationFactory`
   test exercises routing, model binding, the validator, the service, the
   repository, the middleware pipeline and the response wrapper envelope in a
   single pass — so it can fail for every reason the endpoint can actually fail.
   Start there; add unit tests for the logic that is awkward to reach through HTTP.

2. **Integration tests run against a real database in a container, and
   `UseInMemoryDatabase` is banned.** The in-memory provider is not the database:
   it does not enforce a unique index, does not honour a transaction, does not run
   the SQL the real provider would generate, and does not know what `citext` is —
   so it passes exactly the tests that matter most and hides the bugs they exist to
   catch. Start a container for the provider this solution migrates against, and
   one for the cache when the path under test touches it.

3. **A unit test substitutes the facade boundary the service already declares.**
   A module service takes two or three interfaces through its constructor —
   `IRepositoryWrapper`, `IMapper`, another module's service — so the seam exists
   whether or not a test uses it; substitute it with NSubstitute and assert the
   observable outcome. This deliberately inverts the common "never mock what you
   own" advice, which assumes a hand-rolled fake is cheap — here it would mean
   reimplementing a generic repository per test class. The trade is explicit:
   doubles buy fast tests of *decisions* — guards, branches, what gets thrown —
   while real query and persistence behaviour is proven by the integration tier,
   never by a double.

4. **Every test is Arrange / Act / Assert, named
   `MethodName_Scenario_ExpectedResult`, and asserts the outcome rather than the
   steps.** Separate the three sections with blank lines. Assert what the caller
   can see — the response body, the thrown exception, the row now in the database —
   never which internal method was called, so the test survives a refactor of the
   code it covers.

5. **The toolchain is xUnit v3, Shouldly and NSubstitute — each a decision, not a
   default.** xUnit v3 is the current line and supports .NET 8, so a new test
   project has no reason to start on v2. Shouldly, because FluentAssertions v8 and
   later require a paid commercial licence — v7 is the last free release and
   receives critical fixes only, which makes "just upgrade FluentAssertions" a
   purchasing decision rather than a package bump; do not introduce it here.
   NSubstitute, because Moq once shipped a build-time component that sent a hash of
   the developer's git email address to a remote service, and a package with that
   history stays flagged in dependency review regardless of later removal.

6. **The integration tier is not satisfiable without the factory host.** A suite
   of subcutaneous tests — real components wired together, called at the service
   layer, transport skipped — is a legitimate *complement*, never a substitute:
   counting one as the integration tier is scope-narrowing, not completion. Five
   change classes live only in the ASP.NET pipeline and cannot be proven
   subcutaneously: authorization attributes and permission handlers, model
   binding (`[FromBody]`/`[FromQuery]`), routing and route templates,
   exception-middleware status-code mapping, and response serialization shape —
   what the wire JSON actually contains, and leaks. For a change in any of these
   classes, a tier that skipped the transport is INCOMPLETE and is reported as
   such, never green. "The real host drags in heavy externals" is a solved
   problem, not a dead end — `references/integration-testing.md` gives the
   escape: point the settings at the containers, swap in the test authentication
   scheme, disable the hosted services the tests do not exercise. If the host
   genuinely cannot boot after that, the honest verdict is a blocked tier,
   reported through the flows' existing not-run machinery (`RED — environment`,
   *Not run*) — never a tier quietly narrowed to the service layer and reported
   done.

7. **A tier that varies is broken worse than a tier that is red, because a
   varying tier hides real failures.** Instability is usually filed as a
   reliability annoyance — slow, irritating, hard to know when green. The heavier
   consequence is epistemic: when the total moves between runs on one commit,
   nobody can tell which red is the code and which is the fixture, the cost of
   triaging each one exceeds its expected value, and the whole cluster gets
   rationalized into *known noise*. That cluster is where a genuine failure sits
   undisturbed for as long as it likes. Measured in the field: 13 failures filed
   as known noise, totals moving 13/59/87 on one commit; after the fixtures were
   merged, 8 were fixture artifacts that went green untouched and 5 were real —
   two of them production defects days old, each with a test that had been failing
   correctly the whole time. Three operating rules follow, and the third is the
   one no process had:
   - **Stabilize before interpreting.** While a tier varies, a whole-suite total
     is not evidence of anything. A filtered run of the named test is; the
     summary line is not.
   - **The evidence that a variance is fixed is repetition** — several
     consecutive runs on the same commit producing the same result, not one green
     run. Three to four runs per state is what caught a residual flake firing
     once in three.
   - **After stabilizing, re-triage every outstanding failure, one at a time.**
     Never carry the old list forward. The list was assembled under conditions
     that made it unreliable, and re-triaging it is the step that finds what was
     hiding in it.

## Patterns

The patterns live in three files — two split by tier, because a unit test and an
integration test share almost no machinery, and one for the isolation decisions
that sit underneath the integration tier.

**Read `references/unit-testing.md` when** you are constructing a service directly
in a test, configuring an NSubstitute double for `IRepositoryWrapper`, `IMapper`
or another module's service interface, reaching for `Received`/`DidNotReceive`,
testing a validator with `TestValidate`, adding or changing the mapping
configuration test, writing an `<X>Builder`, or working out why an `IQueryable`
returned by `Find` throws on `FirstOrDefaultAsync`.

**Read `references/integration-testing.md` when** you are creating or changing
`ApiFixture`, starting a container, wiring Respawn or the collection fixture,
getting a test request past authentication, deciding which body shape a response
carries, seeding or reading state through the host, writing a flow test that
walks a lifecycle or crosses a module boundary, deciding what to do when the real
host seems too heavy to boot, or adding packages to either test project.

**Read `references/test-isolation.md` when** the suite has — or is about to grow
— a second `WebApplicationFactory` subclass or a second collection, when you are
choosing between `IClassFixture`, `ICollectionFixture` and xUnit v3's assembly
fixture, when a container starts more than once per run, when Respawn's
serialization is what the suite is paying for, when writing a test that must run
in parallel with its siblings, and **whenever the tier's pass count moves between
runs on an unchanged commit** — that symptom is nearly always one of the three
scopes, not the code under test.

## Anti-patterns

### Don't put the in-memory provider in the fixture

```csharp
// BAD — enforces no unique index, honours no transaction, generates no SQL
services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("Tests"));

// GOOD — the container's connection string, handed to the registration the application ships
["DatabaseSettings:SqlSettings:ConnectionStrings:DefaultConnection"] = database.GetConnectionString(),
```

The bugs an integration test exists to catch — a unique index, a cascade, a
projection that will not translate — are exactly the ones the in-memory provider
cannot have. It passes the tests that matter most.

### Don't re-register the DbContext to point it at the container

```csharp
// BAD — the context is registered pooled, and a pooled registration cannot have
// its options swapped underneath it; this also pins the migrations assembly by hand
services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(database.GetConnectionString()));

// GOOD — three configuration keys; provider selection and the migrations
// assembly stay the shipped ones
configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["DatabaseSettings:SqlSettings:DbProvider"] = "PostgreSql",
    ["DatabaseSettings:SqlSettings:ConnectionStrings:DefaultConnection"] = database.GetConnectionString(),
    ["DatabaseSettings:SqlSettings:UseAutoMigration"] = "false",
});
```

Re-registering means the suite exercises a context the deployed application never
builds.

### Don't read a validation failure as the error envelope

```csharp
// BAD — a validator failure is the plain { message } object, so every field
// deserializes to null and this assertion passes
ErrorResultWrapper? error = await response.Content.ReadFromJsonAsync<ErrorResultWrapper>();
error!.Message.ShouldNotBe("something else");

// GOOD — the shape that was actually sent
response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
body.GetProperty("message").GetString().ShouldBe(Messages<Order>.Required(x => x.Code));
```

A request rejected before the action never throws, so the exception middleware
never runs and there is no envelope — same status, different body. A test that
asserts against fields that were never sent is a test that cannot fail.

### Don't verify calls on a path that has a real outcome

```csharp
// BAD — asserts the service called a method; the row is the thing that matters
await orders.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());

// BAD — an integration test runs the real dispatcher; there is nothing to interrogate
await mediator.Received(1).Publish(Arg.Any<OrderConfirmedEvent>(), Arg.Any<CancellationToken>());

// GOOD — the guard's whole promise is that nothing was written, and no other
// observable exists
await orders.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());

// GOOD — the effect, read back
Shipment? shipment = await ReadAsync<Shipment>(x => x.OrderId == order.Id);
shipment.ShouldNotBeNull();
```

Verify a call only when the call's absence or presence is the entire observable.
On a path that produced state, assert the state — and the test survives the
refactor that changes how it got there.

### Don't write a test with no assertion

```csharp
// BAD — "it did not throw, so it works"
[Fact]
public async Task CreateAsync_Works()
    => await sut.CreateAsync(request, CancellationToken.None);

// GOOD — the name states an outcome and the body asserts it
[Fact]
public async Task CreateAsync_WhenCodeAlreadyExists_ThrowsBadRequestException()
{
    // Act
    Func<Task> act = () => sut.CreateAsync(request, CancellationToken.None);

    // Assert
    BadRequestException exception = await act.ShouldThrowAsync<BadRequestException>();
    exception.Message.ShouldBe(Messages<Order>.AlreadyExist(x => x.Code));
}
```

A method that returns without throwing has proved nothing about what it wrote.

### Don't spread one flow across ordered tests

```csharp
// BAD — hidden shared state, and an ordering the runner is under no obligation to honour
private static Guid _orderId;

[Fact] public async Task Step1_Create() { /* assigns _orderId */ }
[Fact] public async Task Step2_Confirm() { /* reads _orderId */ }

// GOOD — one test method owns the whole sequence
[Fact]
public async Task Order_ConfirmedAfterCreation_AdvancesThroughEachState() { /* create, get, confirm */ }
```

Each test gets a fresh class instance, and nothing carries its state to the next
one — an emptied database under a reset, rows the next test never generated under
disjoint data — so the state the second step expects is gone before it runs. Ordering attributes such as
`[TestCaseOrderer]` and static fields are two ways of asking a runner to
guarantee something it does not.

## Decision Guide

| Scenario | Recommendation |
|---|---|
| What an endpoint does — status, envelope, persisted result | Integration test through the fixture |
| A service's decision logic — a branch, a guard, what it throws | Unit test, NSubstitute at the constructor boundary |
| A validator rule over the request's own values | `FluentValidation.TestHelper` — `TestValidate`, asserting the request-typed message |
| A validator rule that reads the database | Substitute the wrapper member the predicate calls; integration test when the predicate does more than delegate |
| A read that composes `Find(...)` and `ProjectTo` | Integration test — nothing else proves the projection translates to SQL |
| Every AutoMapper profile in the solution | The one `AssertConfigurationIsValid` test — not a test per profile |
| A lifecycle or state transition whose steps depend on each other | Flow test — one `[Fact]` walking the sequence |
| An effect another module must show | Cross-module flow test — assert the row or status, never the dispatch |
| An effect handed to a background job | Not here — `background-worker` |
| Time-dependent logic | `FakeTimeProvider` when the type takes a `TimeProvider`; otherwise assert what the timestamp is used for |
| A third-party HTTP dependency | An `HttpMessageHandler` stub on the typed client |
| Arranging state for an integration test | `SeedAsync` through the host's own scope — never by calling another endpoint |
| Snapshot testing a response | **Not used in this stack.** Responses are a versionless DTO ladder, so additive properties — the normal change here — would churn every snapshot |
| An authorization attribute, model binding, a route template, exception status mapping, or the response JSON shape changed | Integration test through the fixture — these live only in the pipeline, and a service-layer test cannot prove them (Principle 6) |
| The tier's pass count moves between runs on one commit | Stop reading the totals — they are not evidence while it varies. Fix the variance first (`references/test-isolation.md`), prove the fix with several consecutive runs, then re-triage every outstanding failure one at a time (Principle 7) |
| A failure is "known noise" / "pre-existing" / "out of scope" | Only after the tier is deterministic. Before that, the label is a guess about a test nobody could classify — and it is where a real defect lives |
| A second group of tests needs different host configuration | Parameterise the one factory, or `WithWebHostBuilder` — never a second `WebApplicationFactory` subclass in the same assembly |
| A test must run in parallel with its siblings | Disjoint data: every value on a unique index generated, every aggregate read filtered to this test's own rows — `references/test-isolation.md` |
| A test asserts a ceiling over a whole table, or writes a fixed configured row | Neither converts to disjoint data by generating ids. Push a per-test filter into the query, or serialize just those classes in a fixture-less `[CollectionDefinition]` |
| The real host looks too heavy to boot for the fixture | The escape in `references/integration-testing.md`: settings to the containers, the test auth scheme, hosted services disabled. Still will not boot → a blocked tier, reported not-run — never narrowed to service-layer tests |
