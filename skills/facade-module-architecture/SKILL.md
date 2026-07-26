---
name: facade-module-architecture
description: >-
  This skill should be used when a placement or wiring question arises in a
  .NET solution: which project or folder a new file belongs in — Core,
  Infrastructure, Migrators or Web; Facades (technical) or Modules (business) —
  adding a project, facade or module, where a controller, settings file or
  solution-level build file goes, wiring the composition root, or reviewing
  project references and dependency direction. Not for: feature handlers —
  module-feature; DbContext, entities and migrations — ef-core-data-access;
  routes, DTOs and OpenAPI — api-surface; JWT, policies and secrets —
  auth-and-security; Serilog sinks and tracing — observability; a brand-new
  repository — project-scaffolding.
---

## Overview

Four kinds of project, one direction of reference:

```
Core  ←  Infrastructure  ←  Migrators.<Provider>  ←  Web
```

`Infrastructure` owns all business logic and all integrations, split on two axes:
**`Facades/` — technical capabilities** (code that would still make sense in a
project with a completely different business domain) and **`Modules/` — business
capabilities** (code that only means something in *this* product).

**This is not Clean Architecture and not Vertical Slice Architecture. Do not map
it onto either template** — there is no application/domain ring and no
use-case-per-folder slice; the rules below are the whole contract.

Answer placement questions from this file alone; open a `references/` file only
when you are writing or reviewing the files that section covers.

### Placement quick reference

| You are adding | It goes in |
|---|---|
| A type two or more layers must name (entity base, lifetime marker, exception, result wrapper) | `Core/` |
| A technical capability many projects reuse | `Infrastructure/Facades/<Capability>/` |
| Shared substrate, or a technical capability only some projects need | `Infrastructure/Facades/Common/<Capability>/` |
| Code that only means something in *this* product | `Infrastructure/Modules/<Feature>/` |
| Generated EF Core migrations for one provider | `src/Migrators/Migrators.<Provider>/` |
| An HTTP endpoint | `Web/Controllers/<Module>/` |
| A JSON configuration topic + its one load line | `Web/Configurations/` |
| A registration line | the owning facade's or module's `Startup.cs` — never `Program.cs` |

## Solution & project graph

| Project | SDK | References | Never references |
|---|---|---|---|
| `Core` | `Microsoft.NET.Sdk` | no project (NuGet only) | any project |
| `Infrastructure` | `Microsoft.NET.Sdk` | `Core` | `Web`, `Migrators.*` |
| `Migrators.<Provider>` | `Microsoft.NET.Sdk` | `Infrastructure` | other migrators, `Web` |
| `Web` | `Microsoft.NET.Sdk.Web` | `Infrastructure` + **every** migrator | `Core` directly |
| test project (under `tests/`) | `Microsoft.NET.Sdk` | `Infrastructure` | anything else |

- **One link up, never two.** `Web` does not reference `Core`; `Core`'s types
  arrive transitively. Adding that reference compiles fine, so it is a review
  finding rather than a build error.
- Layout: `src/Core/`, `src/Infrastructure/`,
  `src/Migrators/Migrators.<Provider>/`, `src/Web/`, `tests/`; one classic `.sln`
  (not `.slnx`) whose solution folders mirror the disk;
  `Directory.Build.props`/`.targets`, `dotnet.ruleset`, `stylecop.json` at the root.
- `Directory.Build.props` is the **only** place that `Include`s an analyzer — a
  project needing a newer version uses `PackageReference Update`. Suppressions go
  in `dotnet.ruleset`, not in a csproj. **No central package management, no
  `global.json`:** each csproj declares its own package versions and its own
  `<TargetFramework>`, and **every csproj must declare the same framework** —
  upgrade them in one commit.
- A migrator holds generated migrations for one provider and no hand-written code.
  **Its name is a runtime contract** — persistence startup passes
  `Migrators.{dbProvider}` as the migrations assembly, so the name must match the
  configured provider key exactly.
- **The base is a floor.** Growth happens inside `Infrastructure` and `Web`'s
  controllers, not by adding projects or edges. The only sanctioned new projects
  are another migrator sibling and another test project.

**Read `references/solution-layout.md` when** creating a solution or project,
editing a csproj or a solution-level file, adding a database provider, or
reviewing project references.

## Core — the contract layer

`Core` is the **contract layer**, not a domain layer and not a bag of primitives.
The test is **"must two or more layers name this type?"** — not "is it small?". It
references no project and carries exactly two packages: `Humanizer` and `NewId`.
Its folders are `Bases/`, `Common/Interfaces/` and `Common/Exceptions/`.

- `BaseEntity` gives `Id` + `CreatedAt` with a **sequential** Guid
  (`NewId.Next().ToGuid()`, never `Guid.NewGuid()`); non-Guid keys derive from
  `BaseEntity<TId>`; `IEntity` is an empty marker. No audit user, no soft-delete
  flag, no domain-event list in the base.
- **Exactly two lifetime markers**, both empty: `IScopedService` and
  `ITransientService`. The marker goes on the **service interface**, not the
  implementation — implementing it *is* the lifetime decision, and an assembly
  scan does the registration. **There is deliberately no singleton marker:**
  singletons are registered explicitly in the owning facade's `Startup.cs`, where
  the configuration and ordering are visible. Do not add a third marker.
- `ICode` marks types carrying a unique business code, so one shared persistence
  configuration can be constrained to all of them.
- `CustomException` → `HttpCustomException` (+ `StatusCode`, `Value`) → exactly
  four **sealed** concrete types: `BadRequestException` (400),
  `UnAuthorizedException` (401), `ForbiddenException` (403),
  `InternalServerException` (500). Each takes `(message)` and
  `(message, innerException)` — **no constructor takes a data payload.**
- The wrappers live beside them: `SuccessResultWrapper<TData>`, produced by the
  Web base controller, and `ErrorResultWrapper`, produced by the exception
  middleware — **it has no `Data` property.**
- **Grow `Core` by adding a leaf under an existing contract, never by reshaping
  one.** A new exception is one `sealed` file with two constructors and no
  serialization ceremony; the middleware handles it the day it is written.

Any one of these means the type belongs in a facade or module instead: it needs a
package beyond `Humanizer` and `NewId`; only one layer will ever reference it; its
name states a business concept.

**Read `references/core-contracts.md` when** adding any type to `Core`, or writing
a new exception or result wrapper.

## Infrastructure — the Facades axis

Two questions place every new file:

1. **Would this code still make sense in a project with a completely different
   business domain?** Yes → `Facades/`. No → `Modules/`.
2. **Is it a technology many projects reuse?** Yes → a top-level facade. Shared
   substrate, or needed by only some projects → `Facades/Common/`. **Reach
   decides, not size** — a niche integration can grow to dozens of files and still
   belong in `Common`, because the next project will not take that dependency.

Base facade set (21; a production service keeps the set and grows inside it):
`Apm`, `Auth`, `BackgroundJobs`, `Cache`, `Common`, `Cors`, `Definitions`,
`ElasticSearch`, `FileStorage`, `HealthChecks`, `Identity`, `Logging`, `Mailing`,
`Mapping`, `Medias`, `Middleware`, `MQTT`, `Notification`, `OpenAPI`,
`Persistence`, `Validations`.

**Anatomy.** Every facade owns exactly one `Startup.cs` — `internal static class
Startup`, always that name — exposing `AddX()`:

- **`internal`, never `public`.** Only `Infrastructure/Startup.cs` composes facades.
- **Options pattern, always the same four calls:** `AddOptions<T>()` →
  `BindConfiguration(nameof(T))` → `ValidateDataAnnotationsRecursively()` →
  `ValidateOnStart()`. Section name == type name, and bad configuration fails at
  startup instead of at first use.
- **Add `UseX()` only when the facade touches the request pipeline** (middleware,
  CORS, OpenAPI); it stays a one-liner in the same `Startup`.
- A named registration block becomes a **`private static` extension in the same
  file** — one entry point per facade. Singletons are registered explicitly here.
- A facade with independent sub-capabilities gives each its own `Startup` and
  composes them at its root.

**`Facades/Common` is fractal.** It is the shared substrate (`Extensions/`,
`Attributes/`, `Converters/`, `Filters/`, shared `Requests/`/`Responses/`) plus a
nursery of capabilities, each a subfolder shaped like a miniature facade — own
folders, own settings, own `Startup.cs` if it needs registration, composed upward
like any facade; only the number of leaves differs. `Common`'s root `Startup` owns
the marker scan that makes the `Core` lifetime markers real — which is why modules
need no facade-style `Startup`.

**Settings follow their service — there is no centralized settings folder.**

| Setting belongs to | Lives in |
|---|---|
| A top-level facade | the facade root, named for the concern it configures, not for the facade |
| A `Common` sub-capability | beside the capability's `Startup.cs`, moving into `<Capability>/Settings/` as it grows |
| A business module | `Modules/<Feature>/Settings/<Feature>Settings.cs`, bound by a tiny `Startup.cs` in that same folder |

**`Auth` and `Identity` are two facades, not one:** `Auth` answers *is this caller
allowed?*; `Identity` answers *how do identities and their permissions work?*.
`Identity` holds entities and services and looks like a business module — it is a
facade anyway, because every project that needs accounts reuses it whole.
**Business-shaped is not business-specific.**

The repository abstraction (`RepositoryBase`, `IRepositoryWrapper`) lives in the
`Persistence` facade; using it belongs to the `ef-core-data-access` skill.

**Read `references/facades.md` when** adding a technical capability, writing or
reviewing a facade `Startup.cs`, placing a settings class, or growing
`Facades/Common/` — it carries the authorized anti-examples for misplaced settings
folders.

## Infrastructure — the Modules axis

A module is **one business capability** and the only place business meaning lives.
It has **no facade-style `Startup.cs`** — the marker scan picks up its services.
The single exception is `Settings/Startup.cs`, which binds that module's options.

Create a folder when its trigger is real, never in advance:

```
Modules/<Feature>/
├── Entities/  Requests/  Responses/  Services/      # tier 1 — every module
├── Seeders/        # + it ships reference data      ┐ tier 2
├── Validations/    # + a rule must hit the database ┘
├── Commands/  Queries/  Events/  # + driven through MediatR  ┐
├── Settings/       # + it binds its own options section      │ tier 3
├── Enums/          # + the capability owns enums             │
├── Expressions/    # + write-once reusable expressions       │
└── ElkEntities/    # + Elk-prefixed search documents         ┘
```

- `Requests/` and `Responses/` may be subfoldered by theme once they grow.
  **`Services/` never subfolders.**
- **Every enum the capability owns lives in `Enums/`** — never declared inside an
  entity, response or service file.
- `Validations/` holds global validations, one `<X>Validation.cs` per concern:
  static extension methods on `IRepositoryWrapper` that FluentValidation rules
  call when a check needs the database. The rule stays in the validator.

**One service file = interface + implementation**, with the lifetime marker on the
**interface** — that is what the scan binds. Do not split the interface into its
own file and do not put it in `Core`.

**When a service grows: suffix-named partials in one folder** —
`<Name>Service.cs`, `<Name>Service.<Role>.cs`. The suffix-less core file declares
the marker and the base lists **and nowhere else does**; a part adding public
operations declares both the partial interface and the partial class, without base
lists; a private-helpers part declares only the partial class.

**`Services/` holds services and nothing else.** Every file is `<Name>Service.cs`
or `<Name>Service.<Role>.cs`, and every type it declares is `<Name>Service` or
`I<Name>Service`. A policy, calculator, builder, mapper, helper, resolver or model
bag is not a service: a genuine business rule belongs inside the service or on the
entity that owns it, backed by `Expressions/`; a reusable technical mechanism
belongs on the Facades axis. If you are extracting logic "to keep the service
small", make it a suffix-named partial instead.

**MediatR here is in-process messaging, not CQRS.** `Commands/` and `Queries/` are
named message envelopes — no write model, no read model, no separate stores, no
read/write separation. Each message is thin: a `sealed record` plus a handler that
delegates straight to the module's service, both in one file. `Events/` is the
same shape with `INotification` / `INotificationHandler<T>`.

**`Expressions/`** holds values and predicates that are computed, not stored, as
`public static Expression<Func<TEntity, TResult>>` on a static class. One
definition serves three call sites — a mapping `Profile`, an entity method and a
query predicate — so the three can never disagree.

**There is no `Mappings/` folder.** The AutoMapper `Profile` for a response lives
in the same file as the response class, below it, so the contract and its
projection cannot drift apart silently.

**`ElkEntities/`** holds `Elk`-prefixed search documents when a module is
projected into Elasticsearch; never index a database entity (how the projection is
built belongs to the `elasticsearch-search` skill).

**Read `references/modules.md` when** creating a module, adding or moving any file
inside one, splitting a service, or reviewing a `Services/` folder — it carries the
authorized anti-examples of `Services/` drift.

## The composition root & configuration

Three files boot the system: `Web/Program.cs`, `Web/Configurations/Startup.cs`,
`Infrastructure/Startup.cs`.

**`Web` registers nothing itself.** Every service line delegates to
Infrastructure; the only two decisions `Web` owns are controller JSON behavior and
the shape of the invalid-model response. **The review test:** a new
`builder.Services.Add…` line in `Program.cs` is the smell — it belongs in a
facade's or module's `AddX()`.

**`Web/Configurations/` — one topic, one file pair.** Base set of 13, in load
order: `appsettings` · `logger` · `apm` · `hangfire` · `healthcheck` · `openapi` ·
`cors` · `filestorage` · `mail` · `security` · `database` · `httpclient` ·
`cache`. `<topic>.json` is **required**; `<topic>.<Environment>.json` is
**optional** and overlays it. Declaration order is load order, later wins, and
`AddEnvironmentVariables()` is last. A facade with its own configuration gets a
new topic file named for the concern, not the vendor, plus one `AddJsonFiles`
line — never grow `appsettings.json`. **The settings class stays with its owner;
only the JSON topic lives here.**

**`AddInfrastructure` is the table of contents:** every facade composed in **one
flat fluent chain, a single statement**. Every line is a call into a facade or
module — no `AddScoped<…>()` for a concrete business type, no inline
`AddOptions<T>()`; the only exception is framework plumbing with no owner. Pass
`configuration` only to the facades that need it. A mature project appends to the
*same* chain; the shape never changes. Before appending, **search the chain for
the method name** — it is ordered for readability, not semantics, which is exactly
why a duplicate hides in it.

**`UseInfrastructure` is the opposite: the order of the `UseX()` calls IS the
middleware pipeline.** Append a new `UseX()` at the position its middleware must
occupy, never at the end by default, and treat a diff that moves a line here as a
behavioral change, not a cleanup.

**`InitializeDatabasesAsync`** lives in the same file — it creates a scope,
applies pending migrations when the auto-migration flag is on, and seeds through
the `IDbInitializer` abstraction, so the root knows the interface and never the
seeding logic. It is awaited **before** `UseInfrastructure()` and `Run()`.

**Read `references/composition-root.md` when** editing `Program.cs`, adding a
configuration topic, or registering anything into the `AddInfrastructure` chain or
the `UseInfrastructure` pipeline — it carries the full `Program.cs` boot order and
the authorized anti-examples for both chains.

## Web — Controllers

`Web/Controllers/` holds controllers and nothing else: `BaseController.cs` at the
root, then one folder per module that exposes HTTP endpoints, named after the
module.

- Every controller inherits **`BaseController`**, never `ControllerBase` directly,
  and **none declares its own `[Route]`** — the URL shape is decided in one file.
- `OkWrapper` / `CreatedWrapper` / `AcceptedWrapper` are the only success path:
  **controllers wrap successes; the exception middleware shapes failures.** A
  controller never builds an error response — it throws.
- The constructor injects the module's service interface and nothing else, and
  there is **no business logic in a controller**: no repository, no `DbContext`,
  no rule evaluation.
- Each endpoint carries an XML `<summary>`, its routing attribute,
  `[HasPermission]`, and two `ProducesResponseType` lines — the second with
  `typeof(ErrorResultWrapper)` for 400. Every endpoint takes a `CancellationToken`
  and passes it down, and the body is a single delegating call wrapped in
  `OkWrapper` with a message from `Messages<T>`.
- **When a controller grows, split into suffix-named partials under the same law
  as a module service:** the suffix-less core file is the only one that declares
  `: BaseController`; every other part declares only
  `public partial class <Name>Controller`.

**Read `references/web-controllers.md` when** adding a controller or endpoint, or
splitting a controller that has grown.

## Not this skill

The handler, request or service internals of one feature → `module-feature`.
`DbContext`, entities, migrations, queries → `ef-core-data-access`. Route shapes,
DTOs, versioning, OpenAPI → `api-surface`. JWT, policies, permission internals,
secrets → `auth-and-security`. Serilog sinks, tracing, health endpoints →
`observability`. Bootstrapping a brand-new repository → `project-scaffolding`.
