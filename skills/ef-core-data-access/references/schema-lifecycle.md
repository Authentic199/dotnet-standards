# Schema lifecycle — context, provider, migrations, seeding

The four parts of the data layer you touch at a known moment rather than while
writing a query: the context itself, how the provider and connection are
configured, how a migration is produced and applied, and how seed data reaches a
fresh database. Each has an unambiguous trigger — you know when you are adding a
migration — which is why they live here and the query and soft-delete rules do
not.

## `ApplicationDbContext`

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

**A migration that has been committed is never deleted, renamed or rewritten.**
`dotnet ef migrations remove` is for a migration that exists only in your working
tree; once the file is in a shared history it has almost certainly been applied
somewhere, and `__EFMigrationsHistory` in that database still names it. Deleting
it does not undo it — it produces two populations that no longer share a schema:
databases that ran it keep the object, databases created afterwards never get it,
and nothing reports the divergence. Observed in the field: a migration adding a
unique index was committed in one change and deleted in a later "align module
boundaries" refactor, and the one-session-per-device constraint it enforced left
the model and the new databases without a single error.

**The repair is forward, and restoring the deleted file is not it** — a database
whose history table already carries that id will skip it, so the object it
created is still missing there. Redeclare the intent in the entity configuration,
generate a **new** migration, and make that migration tolerate both populations:
guard the create so it is a no-op where the object already exists. Then check
what else the deletion took with it — a migration removed to "clean up" is
usually removed along with the model change it belonged to.

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
