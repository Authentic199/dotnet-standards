# Test isolation: one factory, one container, disjoint data

Three scopes decide whether integration tests collide, and they are chosen
together: how many host factories the assembly declares, what the container
fixture is attached to, and how two tests stay off each other's rows. A wrong
pairing does not fail loudly — it produces failures that move between runs on an
unchanged commit, which is the condition Principle 7 in the skill body calls
worse than a red tier.

- [One factory per assembly](#one-factory-per-assembly)
- [The three fixture scopes](#the-three-fixture-scopes)
- [Two coherent configurations](#two-coherent-configurations)
- [Keeping tests off each other's rows](#keeping-tests-off-each-others-rows)
- [The two tests a generated id does not fix](#the-two-tests-a-generated-id-does-not-fix)

## One factory per assembly

**A test assembly declares exactly one class deriving from
`WebApplicationFactory<TEntryPoint>`.** A second one is not "another fixture", it
is a race — and the code that causes it looks entirely reasonable, because
nothing in a second fixture's own file says the first exists.

Three mechanisms, and the first is the one that bites:

- **A configuration override that goes through the process loses every guarantee
  the fixture thought it had.** `AddEnvironmentVariables()` is the application's
  last configuration source, so `Environment.SetEnvironmentVariable` is the
  override that always wins — which is exactly why samples reach for it. But an
  environment variable belongs to the **process**, not to the fixture, and xUnit
  runs collections in parallel inside one process: two fixtures overwrite each
  other's connection string, and one fixture migrates the other's container.
  **Never set an environment variable from a fixture.** Override through
  `ConfigureAppConfiguration` + `AddInMemoryCollection`, as
  `integration-testing.md` does — that source is per host, so the value cannot
  leak sideways into another fixture.
- **Two factories are two hosts, and usually two containers.** Every container
  start plus its migration is paid by every run of the suite, and the second copy
  proves nothing the first did not.
- **Building a second `WebApplicationFactory<Program>` in one process is not
  reliably supported.** Observed in the field, on a repository that tried it:
  constructing the second one — even serially — reached `HostFactoryResolver` and
  threw `InvalidOperationException: The entry point exited without ever building
  an IHost`.

**`[assembly: CollectionBehavior(DisableTestParallelization = true)]` does not
repair a second factory.** It is the reflex fix and it made things worse in the
field: serializing the collections removes the config race and leaves the
second-host failure above, so a suite that was unstable becomes one that cannot
start. Reach for it and you have diagnosed the symptom, not the cause.

**When two groups of tests genuinely need different host configuration —
a different environment name, a different scheme, a registration swapped —
parameterise the one factory; do not clone it.** Either give the factory state
the test sets before the host builds, or take the framework's own per-test
escape:

```csharp
// One factory class, a host configured for this test only
WebApplicationFactory<Program> factory = Fixture.WithWebHostBuilder(builder =>
    builder.ConfigureTestServices(services => services.AddSingleton<IClock>(frozenClock)));

HttpClient client = factory.CreateClient();
```

`WithWebHostBuilder` still builds a host, so it is not free — but it declares no
second factory type, starts no second container, and cannot be picked up by a
third test class that only meant to reuse "the other fixture".

## The three fixture scopes

The container is expensive and the fixture attachment decides how many of them
exist. There are three levels, not two, and the middle one is where four
containers come from.

| Attachment | Instances | Parallelism | Cost |
|---|---|---|---|
| `IClassFixture<T>` | one per **test class** | classes still run in parallel | a container per class — never used for a host + database fixture |
| `ICollectionFixture<T>` | one per **collection** | classes inside a collection are serialized; collections run in parallel with each other | a container per collection. One collection → one container; four collections → **four** |
| `[assembly: AssemblyFixture(typeof(T))]` | one per **assembly** | none imposed — every class is its own collection again | one container for the whole suite |

**`ICollectionFixture` is not the top of the ladder, and reading it as "once for
the suite" is how a suite ends up with a container per collection.** It is once
per *collection*, and a suite that grew a second collection quietly doubled its
infrastructure without a line of the fixture changing.

**The assembly fixture is xUnit v3 only.** There is no equivalent in v2, so every
fixture sample copied from a v2 codebase or article stops at the collection
fixture — the absence is in the sample, not in the framework.

*Verified against `xunit.v3.core` 3.2.2:* the attribute is
`Xunit.AssemblyFixtureAttribute(Type)`; the instance is created before any test
in the assembly runs and `IAsyncLifetime.InitializeAsync` is awaited on it; it is
disposed after the last test through `IAsyncDisposable`, falling back to
`IDisposable`. Two consequences the shape depends on: **the fixture must have a
public parameterless constructor**, and **a test reaches it by declaring a
constructor parameter of exactly the fixture type** — there is no interface to
implement, which is the one place the assembly fixture does not look like its two
siblings.

```csharp
// tests/<ProjectName>.IntegrationTests/AssemblyFixtures.cs
[assembly: AssemblyFixture(typeof(ApiFixture))]
```

## Two coherent configurations

Scope and reset are one decision, not two: **a reset needs serialization, and the
assembly fixture provides none.** Pick a row, not a cell.

| | **A — collection fixture + reset** | **B — assembly fixture + disjoint data** |
|---|---|---|
| Container | one per collection — **the assembly must have exactly one collection** | one, for the whole assembly |
| Between tests | Respawn or a TRUNCATE, before each test | nothing runs |
| Test classes | serialized inside the collection | run in parallel |
| Holds because | one test at a time owns the whole database | no two tests name the same rows |
| Breaks when | a second collection appears — a second container, and the reset now deletes rows a test in the other collection is using | a new test writes a literal into a unique column, or asserts over a whole table |

**Configuration A is the safe default while the assembly has one collection and
tests that assert over whole tables.** Its price is that every class in the suite
is serialized, whether or not it needed to be — `integration-testing.md` ships
it, and the reason its collection definition is singular is this table.

**Configuration B is what a suite adopts to run its classes in parallel**, and it
is the only one of the two that can: both reset strategies force serialization by
construction. The trade is that its guarantee lives in every test rather than in
the fixture — see the contract below, which is what has to hold.

**Do not mix them by halves.** An assembly fixture with a `ResetAsync` still in
the base class is the worst of both: parallel classes deleting each other's rows,
failing in a different place each run.

## Keeping tests off each other's rows

Three strategies, and the skill's older *reset beats re-create* line ranks only
the first two against each other.

| Strategy | What runs | What it costs | Choose it when |
|---|---|---|---|
| **Respawn** | `Respawner.ResetAsync` before each test, deleting every row in FK order | serialization of everything sharing the database; **no** identity/sequence reset | tests assert over whole tables, or the data cannot be made disjoint |
| **Manual `TRUNCATE`** | a fixed statement listing the tables | the same serialization, plus a table list that goes stale — a new table is a silent leak | Respawn cannot be added, or the suite genuinely touches two or three tables |
| **Disjoint data** | nothing at all | a contract every future test must keep, and two shapes it cannot cover (below) | the suite wants parallel classes, and its tests can own their rows |
| **Drop and re-migrate** | the schema, per test | seconds per test, and it throws away the warm connection pool | never — this is the one *reset beats re-create* already refused |

**Disjoint data is not "no isolation" — it is isolation moved into the test, and
it is a contract.** Two rules, both mechanical enough to review:

- **Every value that reaches a unique index is generated, never a literal.**
  `$"CODE-{Guid.NewGuid():N}"`, not `"CODE-0001"`. A literal is a collision
  waiting for the second test that liked the same name.
- **Every aggregate read narrows to this test's own rows.** `CountAsync`,
  `ToListAsync`, `SingleAsync`, `ShouldBeEmpty` — each carries a predicate that
  filters to what this test created. A count over the whole table is an assertion
  about every other test that happens to be running.

The payoff is measured, not theoretical: in the repository this pattern was
reported from, 20 of 25 integration classes were already written disjointly, the
group already running without a reset had been stable for months, and removing
Respawn took the tier from a pass count that moved between runs to 146/146 in
11–16 seconds, repeatable.

## The two tests a generated id does not fix

Both were real in that repository, and both look like ordinary tests until the
reset is taken away.

**1 — A test asserting a ceiling over the whole table.** A page-size cap, "the
list returns at most N", a total. Generating ids changes nothing: the assertion
is about rows the test did not create. **The repair is in the query, not in the
data** — the test filters on a prefix it owns and the endpoint's filter carries
it. Where no such filter exists and adding one is out of scope, this test keeps a
reset, which means it keeps configuration A.

**2 — A test writing into a fixed row read from configuration.** A tenant, an
owner, a settings singleton that every call must ensure exists. A generated id
separates the child rows and cannot separate the shared parent, because every one
of these tests must use the same one.

**The repair is serialization for those classes only, with no fixture attached:**

```csharp
// Serialization is the entire purpose — this definition carries no ICollectionFixture<T>.
[CollectionDefinition(nameof(SharedTenantCollection))]
public sealed class SharedTenantCollection;

[Collection(nameof(SharedTenantCollection))]
public sealed class BookingSharedTenantTests(ApiFixture fixture) : IntegrationTestBase(fixture);
```

A collection definition with no fixture is legal and does exactly one thing: the
classes in it never run at the same time as each other. The rest of the assembly
stays parallel, and the container is still the assembly's one container. This
only works in configuration B — in A the base class already carries the
collection, and a second `[Collection]` on a derived class has nothing to say.
