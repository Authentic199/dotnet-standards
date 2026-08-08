---
name: ef-core-data-access
description: >-
  This skill should be used when working the data layer of a .NET API:
  query or save through IRepositoryWrapper/RepositoryBase,
  Find(isAsNoTracking:), ProjectTo, includes, pagination, SaveChangesAsync per
  operation, Begin/Commit/RollbackTransactionAsync, ApplicationDbContext,
  an entity or IEntityTypeConfiguration — BaseEntity,
  HasBaseEntity, UnderscoreTable, HasCitextUnique, citext, OnDelete, soft
  delete via ISoftDelete/IHidden, DeleteAt/HiddenAt, IgnoreGlobalQueryFilter —
  DatabaseSettings, connection strings, a Migrators.PostgreSql/Migrators.MySql migration, or DbInitializer seeding. Not for:
  services, validators, computed-value expressions — module-feature; file
  placement — facade-module-architecture; endpoints, DTOs — api-surface;
  exceptions, status codes — error-handling; message text — message-keys;
  mapping profiles — automapper-mapping; Redis — distributed-caching;
  locks, ConcurrencyHandlers — distributed-lock; Elasticsearch indexing —
  elasticsearch-search; query-extension internals — list-query-pipeline.
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
    await repositoryWrapper.Repository<Customer>().UpdateAsync(customer, cancellationToken);

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

**One graph, one `AddAsync`.** Two separate calls are the right shape only
when the two entities are separate graphs. When the parent declares a
navigation to the child — the
`HasOne(...).WithMany(...)` pair configured in `IEntityTypeConfiguration`, such
as `Order.Lines` — assign the children to that navigation and add the *parent*
once. `AddAsync` calls `DbContext.Add`, which cascades the add across every
untracked entity reachable through navigations and fixes up each foreign key
itself; `BaseEntity` has already assigned the parent's `Id` in its constructor,
so there is nothing to wait for:

```csharp
Order order = mapper.Map<Order>(request);
order.Lines = mapper.Map<List<OrderLine>>(request.Lines);

await repositoryWrapper.Repository<Order>().AddAsync(order, cancellationToken);
```

That reaches as deep as the graph does: a child that itself carries a
navigation to a grandchild — a join entity holding the other side of a
many-to-many, say — travels in the same add, so wire it up rather than
inserting a level at a time.

Setting the foreign key by hand and calling `AddAsync`/`AddRangeAsync` per
entity type is what to fall back to when there is **no** such navigation to
assign: the child points at a row that already existed before this operation,
or the parent simply declares no collection for the relationship. Hand-setting
a key the navigation would have filled is redundant, and it is the form that
quietly drops half of a composite foreign key. Keys that are not part of the
relationship — a tenant discriminator the mapping profile ignores, for
instance — are still yours to set.

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

The one that is got wrong most often, stated here so it survives a session that
never opens the reference: **`ApplySearch`'s second argument is
`request.SearchFields`** — the field set the client chose. A set written at the
call site is a defect, not a shortcut: `QueryContainer` publishes `SearchFields`
on every list endpoint, so a hard-coded array makes that parameter silently do
nothing, and it turns each later change to the searchable set into a code change
and a deploy. The two supported narrowings live elsewhere — `[NotSearchable]` on
the response property keeps it out of the set `ApplySearch` derives when the
client sends none, and the third argument, `searchFieldExcepts`, drops a field at
one call site.

Read `references/query-conventions.md` before building a list or search
endpoint, or when touching `QueryContainer`, the `$`-prefixed filter
operators, `ApplyFilter`/`ApplySearch`/`ApplySort` or `ToPagedListAsync`.

## Context, provider, migrations, seeding

`ApplicationDbContext` stays thin: options in the constructor, two overrides,
and **no `DbSet` properties** — entities reach the model through
`ApplyConfigurationsFromAssembly`, so adding an entity means adding its
configuration and this file never changes. `ConfigureConventions` routes every
`DateTimeOffset` through a UTC converter in both directions, and no entity can
opt out. The provider is a configuration value rather than a compile-time
choice, and each arm points at its own `Migrators.<Provider>` assembly.

Two rules from this area bite hardest when nobody thought to look them up, so
they are here rather than in the reference. **A committed migration is never
deleted, renamed or rewritten** — deleting it does not undo it, and the repair is
always a *new*, forward migration that tolerates both populations
(`dotnet-code-review` 1.11 is the check). And **the EF CLI builds the solution
first, so both commands need an explicit, generous timeout**: a run killed
halfway leaves a partial migration pair that the next `add` builds on top of.

Read `references/schema-lifecycle.md` **before typing `dotnet ef migrations add`,
`dotnet ef database update`, `AddOptions<DatabaseSettings>`, `UseAutoMigration`
or `IDataSeedContributor`** — it carries the two context overrides, the settings
and provider wiring, the whole migration workflow including the forward-repair
procedure, and why every seeder must decide what is already there.

## Entities and configurations

One entity lives in one file, together with its `IEntityTypeConfiguration<T>`.
The configuration sits beside the class it configures, so a reviewer reads the
shape and its mapping without opening a second file, and
`ApplyConfigurationsFromAssembly` finds it with no registration step.

**An enum the entity uses is not in that file** — every enum a capability owns
lives in `Enums/`, one per file, never inside an entity, response or service
file; that placement is `facade-module-architecture`'s rule. Every public
property carries an XML `<summary>`, and that documentation law is
`api-surface`'s, covering entities alongside the DTOs they feed.

```csharp
public class Order : BaseEntity
{
    public string? Code { get; set; }

    public Guid CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = default!;

    // FulfilmentStatus lives in Enums/FulfilmentStatus.cs, not here
    public FulfilmentStatus Status { get; set; } = FulfilmentStatus.Pending;

    public Order SetCustomer(Guid customerId)
    {
        CustomerId = customerId;
        return this;
    }
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

Every `Configure` opens with `HasBaseEntity().UnderscoreTable()` — primary key on
`Id`, then the table name snake-cased from the type (`OrderLine` becomes
`order_line`).

Three rules the schema gets wrong when nobody thinks to check. Each produces a
line a reviewer deletes, not a lookup:

- **Length and business conditions live in the validator, not the schema.** No
  `HasMaxLength`, no `HasColumnType("varchar(n)")`, and no business check
  constraint — a `CK_*` comparing one column against another is a validator rule
  wearing schema clothes. A limit declared twice drifts twice, and the schema
  copy is the one that diverges silently. What the schema keeps is structure the
  database itself needs: keys, foreign keys, uniqueness, defaults.
- **With `Nullable` enabled, requiredness is the type's job.** EF derives
  required from a non-nullable property — a value type or a non-nullable
  reference — so `IsRequired()` restates what the model already says and is never
  written. The property's own `?` is the single source of optionality.
- **Text a human reads** — codes, names, plates — **is `citext`**, so lookups and
  uniqueness ignore case. `HasCitextUnique` sets the column type and its unique
  index together; `HasColumnType("citext")` alone leaves the column searchable
  and unindexed.

Small fluent setters like `SetCustomer` that assign and `return this` are as
far as entity behaviour goes here; anything that makes a decision belongs to
domain-modeling.

Read `references/entity-configuration.md` **before typing `HasOne`, `OnDelete`,
`HasIndex`, `HasDefaultValue` or `BaseEntity<`** — it carries the base-class
choice and the cross-layer rule that binds responses to it, the explicit
foreign-key pair, the `OnDelete` decision table, composite uniqueness and how
enums are stored.

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
`references/soft-deletes.md` — recreate the files from it when the project
lacks them; the near-misses it names all compile; do not write a local variant.

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
soft-delete stamp is ever registered through `HasQueryFilter`, so within this
pattern "global query filter" names this composed predicate and nothing else.
`HasQueryFilter` does appear in the standard, but for a different job —
excluding staged import rows from every read of an entity being imported into
— and never for `DeleteAt` or `HiddenAt`.

> **Documentation-derived** — not corpus-verified. EF Core's own
> `IgnoreQueryFilters()` clears filters registered through `HasQueryFilter`.
> It therefore does **not** reach the stamp check, which is an ordinary
> `Where` — `IgnoreGlobalQueryFilter` below is the escape hatch for this
> pattern. Check first whether the entity registers a `HasQueryFilter` at all:
> where none is registered, `IgnoreQueryFilters()` clears nothing and the call
> is dead weight; where one is (a staging filter, say), it clears *that*, which
> is a different intention than reading past a soft delete.

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
