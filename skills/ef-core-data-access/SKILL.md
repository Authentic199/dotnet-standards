---
name: ef-core-data-access
description: >-
  This skill should be used when working the data layer of a .NET API:
  querying or saving through IRepositoryWrapper/RepositoryBase,
  Find(isAsNoTracking:), ProjectTo, includes, pagination, SaveChangesAsync per
  operation, Begin/Commit/RollbackTransactionAsync, ApplicationDbContext,
  adding an entity or IEntityTypeConfiguration — BaseEntity,
  HasBaseEntity, UnderscoreTable, HasCitextUnique, citext, OnDelete, soft
  delete via ISoftDelete/IHidden, DeleteAt/HiddenAt, IgnoreGlobalQueryFilter —
  DatabaseSettings, connection strings, a migration under
  Migrators.PostgreSql or Migrators.MySql, or DbInitializer seeding. Not for:
  services, validators, computed-value expressions — module-feature; file
  placement — facade-module-architecture; endpoints, DTOs — api-surface;
  exceptions, status codes — error-handling; message text — message-keys;
  mapping profiles — automapper-mapping; Redis — distributed-caching;
  locks, ConcurrencyHandlers — distributed-lock; Elasticsearch indexing —
  elasticsearch-search.
---

## Repository and wrapper

All data access goes through `IRepositoryWrapper`. Inject it into a service —
never `ApplicationDbContext`, never a `DbSet`, never a hand-written
`IOrderRepository`. The wrapper hands out one cached `IRepositoryBase<T>` per
entity type from `Repository<T>()`, so adding an entity gives you its
repository for free: nothing to write, nothing to register. Wrapping EF Core
this way is the deliberate contract of this standard — the wrapper is where
transactions live, the repository is where saves happen, and every module's
call sites look the same.

### The surface

| Member | Use it for |
|---|---|
| `Find(expression?, isAsNoTracking:)` | Every composed query — returns `IQueryable<T>` |
| `GetById<TId>(id)` / `GetByIdAsync<TId>(id, ct)` | Tracked fetch by primary key, nothing composed |
| `AddAsync` / `AddRangeAsync` | Insert — saves |
| `UpdateAsync` / `UpdateRangeAsync` | Update — saves |
| `DeleteAsync` / `DeleteRangeAsync` | Delete — saves |
| `AnyAsync(expression?, ct)` / `CountAsync(expression?, ct)` | Existence and counts without materializing |
| `FromSqlRaw` / `ExecuteSqlRawAsync` | Escape hatch when LINQ cannot express it |

Every async member above takes the cancellation token last — pass the one your
service received rather than letting it default, including on
`BeginTransactionAsync`. The raw-SQL pair is the exception in both senses: the
parameter array comes *first* and neither takes a token, so a call to it is a
deliberate, reviewed choice.

When `T` implements a soft-delete interface, the repository composes the stamp
check into `Find`, `Count`, `CountAsync`, `Any` and `AnyAsync` before EF sees
the query — see `## Soft delete`.

### Saving is the repository's job

Each mutation calls `SaveChangesAsync` itself, so one repository call is one
committed change. There is no batching step to forget, and no reason for
`SaveChanges` to appear anywhere else — there would be nothing left to flush.

```csharp
Order order = mapper.Map<Order>(request);
await repositoryWrapper.Repository<Order>().AddAsync(order, cancellationToken);
```

The cost of that convenience is that a multi-step operation is **not** atomic
by default. When two or more mutations must succeed or fail together, the
wrapper's transaction is what makes the individual saves one unit:

```csharp
await repositoryWrapper.BeginTransactionAsync(cancellationToken);

try
{
    await repositoryWrapper.Repository<Order>().AddAsync(order, cancellationToken);
    await repositoryWrapper.Repository<OrderLine>().AddRangeAsync(lines, cancellationToken);

    await repositoryWrapper.CommitTransactionAsync(cancellationToken);
}
catch (Exception)
{
    await repositoryWrapper.RollbackTransactionAsync(cancellationToken);
    throw;
}
```

Rolling back belongs in the `catch`, and the failure keeps travelling — how it
is shaped from there is error-handling's call, not this skill's.

### Find is the query gate

`Find` returns `IQueryable<T>`, so composition happens at the call site:
filter through the expression, then `Include`, `ProjectTo`, ordering,
pagination, and a terminal `FirstOrDefaultAsync(cancellationToken)` or
`ToListAsync(cancellationToken)`.

```csharp
OrderResponse? response = await repositoryWrapper.Repository<Order>()
    .Find(x => x.Id == orderId && x.CustomerId == customerId, isAsNoTracking: true)
    .ProjectTo<OrderResponse>(mapper.ConfigurationProvider)
    .FirstOrDefaultAsync(cancellationToken);
```

- Pass `isAsNoTracking: true` on read paths, and pass it named — a bare `true`
  at the call site tells the next reader nothing. Leave tracking on only when
  the entity you load will be mutated and saved.
- Reach for `Find(...).FirstOrDefaultAsync(ct)` whenever the fetch composes
  anything at all: no-tracking, an `Include`, a projection, a second
  predicate. `GetByIdAsync<TId>` is the tracked by-key fetch for
  load-modify-save, where there is nothing to compose.
- `Include` before `ProjectTo` is redundant — the projection generates the
  joins it needs; reach for `Include` only when materializing entities with
  their children.

Read `references/query-conventions.md` before building a list or search
endpoint, or when touching `QueryContainer`, the `$`-prefixed filter
operators, `ApplyFilter`/`ApplySearch`/`ApplySort` or `ToPagedListAsync`.

## DbContext

`ApplicationDbContext` stays thin on purpose: options in the constructor, and
nothing in the model but two overrides.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    modelBuilder.HasPostgresExtension("citext");
}
```

There are no `DbSet` properties. Entities reach the model through
`ApplyConfigurationsFromAssembly`, which picks up every
`IEntityTypeConfiguration<T>` in the assembly — so adding an entity means
adding its configuration, and this file never changes. The second override,
`ConfigureConventions`, routes `Properties<DateTimeOffset>()` through a
`ValueConverter<DateTimeOffset, DateTimeOffset>` calling `ToUniversalTime()`
in both directions: every timestamp is stored and read as UTC, and no entity
can opt out.

## Settings and provider

Configuration binds once and fails at startup rather than at first query:

```csharp
services.AddOptions<DatabaseSettings>()
    .BindConfiguration(nameof(DatabaseSettings))
    .Configure(x => x.SqlSettings.ConnectionStrings.OverrideConnection())
    .ValidateDataAnnotationsRecursively()
    .ValidateOnStart();
```

`SqlSettings` carries `DbProvider`, `ConnectionStrings.DefaultConnection` and
the `UseAutoMigration` flag, and implements `IValidatableObject` so a missing
key is reported by its full configuration path instead of as a null reference
later. `OverrideConnection()` appends `ApplicationName=<machine name>;` when
the connection string lacks one, so a database session is traceable to the
host that opened it. The same `DatabaseSettings` root also carries the cache
and search sections, which belong to distributed-caching and
elasticsearch-search.

The provider is a configuration value, not a compile-time choice: the pooled
context is registered from those options, and `UseDatabase` switches on
`DbProviderKeys`, points each arm at its own migrations assembly with
`options.MigrationsAssembly($"Migrators.{dbProvider}")`, and throws on an
unrecognised value.

## Migrations workflow

Create migrations locally with the EF CLI — `-p` is the migrator project for
the provider you run, `-s` the host project, `-c` the context:

```bash
dotnet ef migrations add <Name> -p src/Migrators/Migrators.PostgreSql -s src/Web -c ApplicationDbContext
dotnet ef database update -p src/Migrators/Migrators.PostgreSql -s src/Web -c ApplicationDbContext
```

**Both commands build the solution first, so give them an explicit, generous
timeout** — on a cold tree the build alone outruns a default one, and the
command is then killed halfway through writing a migration. Wait for it rather
than moving on: a run abandoned mid-command leaves a partial migration pair
that the next `add` builds on top of.

Deployed environments never run the CLI. With `SqlSettings.UseAutoMigration`
set, startup asks `GetPendingMigrationsAsync` and applies anything outstanding
with `MigrateAsync`; with the flag off it applies nothing and logs a warning.
Both branches log, so the startup log — not the database — answers "did this
deployment migrate?".

## Initialization and seeding

After migrations settle, startup resolves the initializer, which guards on
`Database.CanConnectAsync(cancellationToken)` and runs the seeders only if the
database answers. Seed data ships as `IDataSeedContributor` implementations,
placed in the module that owns the data:

```csharp
public class OrderStatusSeeder : IDataSeedContributor
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await repositoryWrapper.Repository<OrderStatus>().AnyAsync(cancellationToken: cancellationToken))
        {
            return;
        }

        await repositoryWrapper.Repository<OrderStatus>().AddRangeAsync(statuses, cancellationToken);
    }
}
```

A Scrutor scan registers every contributor as transient and the runner awaits
each in turn with the boot token, so ordering between contributors is not
something to rely on. Seeders run on **every** start, which is why each one
decides what is already there — bail out when the table is populated, as
above, or reconcile row by row when the seed set grows.

`IDataSeedContributor` is the only public type here, by design: modules
contribute seed data, and the initializer and runner stay `internal` because
nothing outside persistence should drive initialization.

## Entities and configurations

One entity lives in one file, together with its `IEntityTypeConfiguration<T>`
and any enums it owns. The configuration sits beside the class it configures,
so a reviewer reads the shape and its mapping without opening a second file,
and `ApplyConfigurationsFromAssembly` finds it with no registration step.
Every public property carries an XML `<summary>` — the documentation law lives
in `api-surface` and covers entities alongside the DTOs they feed.

```csharp
public class Order : BaseEntity
{
    public string? Code { get; set; }

    public Guid CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = default!;

    public FulfilmentStatus Status { get; set; } = FulfilmentStatus.Pending;

    public Order SetCustomer(Guid customerId)
    {
        CustomerId = customerId;
        return this;
    }
}

public enum FulfilmentStatus
{
    /// <summary>
    /// Accepted, not yet shipped.
    /// </summary>
    Pending = 1,
    /// <summary>
    /// Shipped to the customer.
    /// </summary>
    Shipped = 2,
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasBaseEntity().UnderscoreTable();

        builder.HasCitextUnique(x => x.Code);

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Status).HasDefaultValue(FulfilmentStatus.Pending);
    }
}
```

**`BaseEntity`** closes `BaseEntity<TId>` over `Guid` and assigns `Id` in its
constructor from a sequential GUID generator, so the id exists the moment you
`new` the entity — children can be wired up, and the id returned, before
anything is saved. The generic base contributes `CreatedAt`, defaulted to
`DateTimeOffset.UtcNow`. Reach for `BaseEntity<TId>` only if a key genuinely
cannot be a GUID — **and that rule binds every type deriving from the base, not
entities alone.** Response families root at `BaseEntity` too (`api-surface`,
`references/request-response-dtos.md`), so a response written `: BaseEntity<Guid>`
where the closed `BaseEntity` exists is the same defect in a different layer —
the generic form is never a choice when the key is a `Guid`. A reviewer of
either layer flags it.

**Text a human reads** — codes, names, plates — is `citext`, so lookups and
uniqueness ignore case. `HasCitextUnique(x => x.Code)` sets the column type
and the unique index together and takes an optional filter;
`HasColumnType("citext")` alone fits a case-insensitive column that need not
be unique.

**Length and business conditions live in the validator, not the schema.** The
request's FluentValidation rules own maximum length and every business-shaped
condition, so the configuration does not restate them: no `HasMaxLength`, no
`HasColumnType("varchar(n)")`, and no business check constraint — a `CK_*`
comparing one column against another is a validator rule wearing schema
clothes. A limit declared twice drifts twice, and the schema copy is the one
that diverges silently. What the schema keeps is structure the database itself
needs: keys, foreign keys, uniqueness (`HasCitextUnique`), defaults.

**With `Nullable` enabled, requiredness is the type's job too.** EF derives
required from a non-nullable property — a value type or a non-nullable
reference — so `IsRequired()` on one restates what the model already says and
is never written. The property's own `?` is the single source of optionality;
a configuration line duplicating it is the line a reviewer deletes.

**Open every `Configure` with `HasBaseEntity().UnderscoreTable()`** — primary
key on `Id`, then the table name snake-cased from the type (`OrderLine`
becomes `order_line`). The two are independent, so one order across the
solution is a convention worth keeping rather than a requirement.

**Foreign keys are explicit pairs**: a `Guid CustomerId` — nullable when the
link is optional — beside a nullable reference navigation, declared with
`HasOne`/`WithMany`/`HasForeignKey` rather than left to convention, and
finished with an `OnDelete` chosen on purpose. Collection navigations are
non-nullable and initialized `= default!`, as `Lines` above. The question
`OnDelete` answers is *what should happen to this row when its target is
deleted?*

| Answer | Behaviour |
|---|---|
| It cannot outlive the target | `Cascade` — child rows, membership and log tables |
| The target must not be deletable while this points at it | `Restrict` — shared catalogue or configuration rows |
| It survives, having kept what it needs | `SetNull` — optional FK on a history row |

`SetNull` needs a nullable FK, only makes sense when the row still carries a
usable snapshot of what it pointed at, and is by a wide margin the rarest of
the three. Composite uniqueness is a plain index over an anonymous type —
`builder.HasIndex(x => new { x.OrderId, x.LineNumber }).IsUnique()` — which is
how a natural key gets enforced when the surrogate `Id` is not the real one.

**Enums are int-backed with explicit values starting at 1**, so stored numbers
never shift when a member is inserted later, and each member carries an XML
doc line. Pin a default with a property initializer, `HasDefaultValue`, or
both: the initializer covers entities created in code, `HasDefaultValue` rows
inserted around it.

Small fluent setters like `SetCustomer` that assign and `return this` are as
far as entity behaviour goes here; anything that makes a decision belongs to
domain-modeling.

## Soft delete

Some rows are marked as removed rather than actually deleted. Two interfaces
carry the mark, and an entity opts in by implementing one or both:

| Interface | Property | Means |
|---|---|---|
| `ISoftDelete` | `DateTimeOffset? DeleteAt` | Deleted. Nothing clears the stamp. |
| `IHidden` | `DateTimeOffset? HiddenAt` | Withheld for now — set and cleared as a workflow moves. |

They are independent axes. Reach for `ISoftDelete` when the row is gone but its
history is still wanted, and for `IHidden` when concealment is a state the same
workflow will reverse. Only hiding has a reversal, and it is written as a fluent
setter on the entity rather than at a call site. Both stamps are nullable
timestamps rather than booleans — null is live, a value is the flag and the
moment at once.

**`BaseEntity` carries neither stamp, and it never gains one.** Deletability is
declared per entity, so an entity that is genuinely removable stays removable
and no table pays for a column it does not use. That boundary belongs to
facade-module-architecture (`references/core-contracts.md`); what follows is
what an entity does once it opts in.

**A project that soft-deletes at all recreates the canonical implementation** —
four files under `Infrastructure.Facades.Common.SoftDeletes` plus the repository
wiring that applies them. A `bool IsDeleted` on the entity, or an
`x.DeleteAt == null` typed by hand at a call site, is the drift this pattern
exists to remove: the first is unfilterable without touching every query, the
second is correct exactly until the query someone forgets. Read
`references/soft-deletes.md` and recreate the files from it when the project
lacks them; do not write a local variant.

### The filter belongs to the repository

`Find`, `Count`, `CountAsync`, `Any` and `AnyAsync` each compose both stamps
into the predicate before touching the `DbSet`, so every read is filtered
whether or not its author thought about deletion:

```csharp
public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? expression = default, CancellationToken cancellationToken = default)
    => await dbContext.Set<T>().CountAsync(ApplySoftDelete(expression).HiddenObject(), cancellationToken);
```

Both helpers test `typeof(T)` against the interface and hand the predicate
straight back when it does not implement it, so the wiring is written once in
the generic base and costs nothing for entities that never opted in.
`IRepositoryBase<T>` does not change — the signatures are identical and the
filtering is invisible from the outside. `ExpressionExtension.Join` is what ANDs
the stamp check onto the caller's predicate; `common-extensions` owns it.

**The injected condition is an ordinary `Where`, not an EF query filter.** No
`HasQueryFilter` is registered anywhere in this standard, so "global query
filter" names this composed predicate and nothing else.

> **Documentation-derived** — not corpus-verified. EF Core's own
> `IgnoreQueryFilters()` clears filters registered through `HasQueryFilter`.
> With none registered it has nothing to clear, so it is not the escape hatch
> for this pattern and a call to it here changes nothing.

**By-key and raw-SQL members sit outside the filter by construction.**
`GetById`/`GetByIdAsync` go through `DbSet.Find`, and `FromSqlRaw` /
`ExecuteSqlRawAsync` reach the database directly; none of them composes a
predicate, so each returns marked rows. Reach for `Find(x => x.Id == id)`
whenever the stamp has to be honoured.

### Deleting is an update

Stamp the entity, then save it the ordinary way. `DeleteAsync` still issues a
real `Remove`, so it is not the delete path for a stamped entity:

```csharp
order.DeleteAt = DateTimeOffset.UtcNow;
await repositoryWrapper.Repository<Order>().UpdateAsync(order, cancellationToken);
```

For a set, stamp each entity and call `UpdateRangeAsync` once; when the delete
also touches rows other modules own, it belongs in the wrapper's transaction
like any other multi-step mutation.

Hiding is reversed through a fluent setter on the entity — `Hidden(bool enable)`
assigning `HiddenAt = enable ? DateTimeOffset.UtcNow : null` and returning
`this`, the `SetCustomer` shape above — so both directions live on the entity
and neither is spelled out at a call site.

### Reading past the filter

`IgnoreGlobalQueryFilter(params Type[])` walks the query built so far and
replaces every comparison on a property the named interface declares with
`true`:

```csharp
Order? order = await repositoryWrapper.Repository<Order>()
    .Find(x => x.Id == orderId)
    .IgnoreGlobalQueryFilter(typeof(IHidden))
    .Include(x => x.Lines)
    .FirstOrDefaultAsync(cancellationToken);
```

**Compose it directly after `Find`, before includes and projections.** It
rewrites the tree that exists at the moment it runs, so anything composed after
it is untouched — and anything composed before it is fair game, including a
condition on `DeleteAt` or `HiddenAt` you wrote yourself in the `Find`
predicate: the rewrite matches the property, not who put it there. Pass the
interface type, never the entity type, and name only the axis you need.

Reaching for it is the deliberate, reviewed choice that raw SQL is, and the axis
it is reached for is the hidden one — the ordinary reason to want a withheld row
is to finish the workflow that withheld it. `typeof(ISoftDelete)` is the shape
to argue about before writing: a deleted row is meant to stay gone.

### Uniqueness must ignore deleted rows

`ISoftDelete.SqlFilter` is a constant on the interface holding the predicate
`"DeleteAt" is null`, so every partial index is written the same way:

```csharp
builder.HasBaseEntity().UnderscoreTable();

builder.HasCitextUnique(x => x.Code, ISoftDelete.SqlFilter);

builder.HasIndex(x => new { x.CustomerId, x.Number })
    .IsUnique()
    .HasFilter(ISoftDelete.SqlFilter);
```

Without the filter the unique index still counts deleted rows, so a code
released by a delete can never be reused and the next attempt fails against a
row nobody can see — a soft delete that is a hard one from the client's side.
This is what `HasCitextUnique`'s optional filter parameter is for, and it
applies to plain and composite indexes alike; keeping the SQL in one `const`
means one place to check when the provider changes. `IHidden` has no such
constant and no index excludes `HiddenAt`, because hiding is reversible and the
value has to still be there when it comes back.

### The filter covers the root set only

It is built for `T` and joined onto the predicate over `T`, so it reaches no
`Include`, no child collection aggregated inside a computed expression, and no
navigation filtered by hand. Those write the stamp check themselves:

```csharp
.Include(x => x.Lines.Where(line => line.DeleteAt == null))

public static Expression<Func<Order, long>> LineTotalExpr =>
    src => src.Lines.Where(x => x.DeleteAt == null).Sum(x => x.Quantity);
```
