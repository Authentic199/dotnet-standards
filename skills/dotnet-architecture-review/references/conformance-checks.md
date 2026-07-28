# Architecture conformance — the check catalogue

The long tail behind the five audits in `SKILL.md`. The body carries each audit's
decisive checks; this file carries the rest, plus the comparison data an audit is
run against — the reference matrix, the facade set, the module tiers, the
settings homes, and the configuration topics and boot order.

**Numbering continues the body's.** Audit 1 resumes at 1.7, audit 2 at 2.4,
audit 4 at 4.10, audit 5 at 5.8. A number is never reused, so `check 4.13` needs
no file named beside it. **Audit 3 has no tail** — what a controller or endpoint
looks like is `api-surface`'s, not a placement question.

**Scope, tool and severity are the body's and are not repeated here.** Diff mode
versus sweep, `grep -rn --include=*.cs` as the default instrument, and the
CRITICAL/HIGH/MEDIUM/INFO ladder all hold unchanged.

**How to read a check.**

> **<n.n> <title>** — *SEVERITY* · owner
> `Find:` the grep to run, the file to open, or the listing to compare.
> Why it is a finding, and the move that fixes it.

The owner is the skill and section that legislates the rule — cite it in the
finding rather than re-deriving it. Anything presented as a table or a listing is
**comparison data, not a check**: it carries no number and no severity, because it
is what you compare against.

## Contents

- [Audit 1 — solution and csproj hygiene](#audit-1--solution-and-csproj-hygiene) — 1.7–1.13
- [Comparison data — the reference matrix](#comparison-data--the-reference-matrix)
- [Audit 2 — the Core contracts](#audit-2--the-core-contracts) — 2.4–2.8
- [Audit 4 — facade anatomy](#audit-4--facade-anatomy) — 4.10–4.15
- [Comparison data — the facade set and the module tiers](#comparison-data--the-facade-set-and-the-module-tiers)
- [Comparison data — settings placement](#comparison-data--settings-placement)
- [Audit 5 — configuration and boot order](#audit-5--configuration-and-boot-order) — 5.8–5.12

## Audit 1 — solution and csproj hygiene

**1.7 More than one solution file, or a `.slnx`** — *MEDIUM* ·
`facade-module-architecture` › *solution-layout*
`Find:` `ls *.sln *.slnx` at the repository root.
One classic `.sln` for the repository. A second solution file splits the meaning
of "the solution" — a build, a review and an IDE can each be looking at a
different set of projects, and nothing reports the divergence.

**1.8 Solution folders that do not mirror the disk** — *INFO* ·
`facade-module-architecture` › *solution-layout*
`Find:` open the `.sln` and read its solution-folder entries against
`ls src/ src/Migrators/ tests/`. The set is `src`, `Migrators`, `tests`, plus
`Solution Items` for the root files.
The solution view is the map most readers navigate by; when it disagrees with the
disk, the disk is still the truth and the map quietly teaches a structure that
does not exist. INFO because nothing downstream depends on it.

**1.9 A root build file missing or relocated** — *MEDIUM* ·
`facade-module-architecture` › *solution-layout*
`Find:` `ls Directory.Build.props Directory.Build.targets dotnet.ruleset stylecop.json`
at the repository root.
`.props` carries what is true of every project — the ruleset pointer,
documentation generation, implicit usings, nullable, the analyzer
`PackageReference`s. `.targets` carries what can only be computed **after** a
csproj is evaluated (a documentation path built from `$(OutputPath)`, which is
unknown at props time), so a property moved from targets into props silently
produces an empty value. Moving either file down into one project makes the rule
apply to that project and nobody notices the others lost it.

> **Not a finding: `stylecop.json` appearing to do nothing.** It sits at the root
> as a shared settings file and **is not wired into the build** — no project
> declares it as an `AdditionalFiles` item, so StyleCop does not read it, and
> editing it changes nothing. Do not report the file as unused, and do not report
> the missing `AdditionalFiles` item as a defect. Wiring it is a decision to raise
> with the owner, not a conformance repair.

**1.10 An analyzer `Include`d outside `Directory.Build.props`** — *MEDIUM* ·
`facade-module-architecture` › *solution-layout*
`Find:` `grep -rn "PackageReference Include=" --include=*.csproj src/ tests/` and
look for analyzer packages.
`Directory.Build.props` is the only place that `Include`s an analyzer; a project
needing a newer one writes `PackageReference Update="…" Version="…"` — `Update`,
never a second `Include`. **Consequence worth carrying into the finding:** the
version in props is a floor, not the effective version, so read the csproj before
trusting it.

**1.11 A rule suppression inside a csproj** — *MEDIUM* ·
`facade-module-architecture` › *solution-layout*
`Find:` `grep -rn "NoWarn\|WarningsNotAsErrors" --include=*.csproj src/ tests/`
Suppressions belong in `dotnet.ruleset`, the one shared ruleset, where the whole
solution can see what was turned off. A csproj-local suppression is invisible to
everyone reading the ruleset to learn what the solution tolerates.

**1.12 Central package management, or a pinned SDK** — *MEDIUM* ·
`facade-module-architecture` › *solution-layout*
`Find:` `ls Directory.Packages.props global.json`
Neither exists here: each csproj declares its own `PackageReference` versions and
the SDK version is not pinned. This check earns its place mostly as the reply to
framework or version drift — introducing a central property is the tempting "fix"
for body check 1.6, and it replaces the convention instead of repairing the defect
the drift reported. Say so in the finding. If central management is genuinely
wanted, that is the owner's decision, raised as INFO, not applied inside a review.

**1.13 The wrong SDK attribute on a project** — *MEDIUM; HIGH once ASP.NET types
have arrived* · `facade-module-architecture` › *solution-layout*
`Find:` `grep -rn "<Project Sdk=" --include=*.csproj src/ tests/`
`Web` alone is `Microsoft.NET.Sdk.Web`; `Core`, `Infrastructure`, every migrator
and every test project are plain `Microsoft.NET.Sdk`. A class library on the
`.Web` SDK gains the ASP.NET Core framework reference implicitly — nothing breaks,
and every HTTP type becomes reachable from a project the chain says must not know
about HTTP. That is a latent capability, which is why it starts at MEDIUM.
Escalate once it has been used: `grep -rln "Microsoft.AspNetCore" src/Infrastructure/ src/Core/`.

## Comparison data — the reference matrix

Open every `.csproj` and tick every row. Anything off this table is body check
1.1, 1.2 or 1.3.

| Project | SDK | References | Referenced by | Never references |
|---|---|---|---|---|
| `Core` | `Microsoft.NET.Sdk` | no project — NuGet only | `Infrastructure` | any project |
| `Infrastructure` | `Microsoft.NET.Sdk` | `Core` | `Web`, every migrator, test projects | `Web`, any migrator |
| `Migrators.<Provider>` | `Microsoft.NET.Sdk` | `Infrastructure` | `Web` | another migrator, `Web` |
| `Web` | `Microsoft.NET.Sdk.Web` | `Infrastructure` **+ every** migrator | nothing | `Core` directly |
| test project, under `tests/` | `Microsoft.NET.Sdk` | `Infrastructure` | nothing | anything else |

Four rules the table encodes, each its own tick:

- [ ] **One link up the chain, never two.** `Core`'s types reach `Web` transitively.
- [ ] **No upward edges.** Nothing points back up the chain.
- [ ] **Migrators are siblings.** `Migrators.<A>` never references `Migrators.<B>`.
- [ ] **A test project references `Infrastructure` and nothing else**, from `tests/`.

**Migrator projects, in detail.** A migrator holds the generated migration files
for one provider and no hand-written code; its csproj is a `<TargetFramework>` and
one `ProjectReference` to `Infrastructure`. **Its name is a runtime contract** —
persistence startup hands EF Core the migrations assembly by name, built from the
configured provider key, so a mismatch fails at runtime on the first migration and
never at build time (body check 1.5). `Web` references *all* migrators so every one
ships in the output; **configuration, not the reference graph, decides which is
used** (body check 1.3).

## Audit 2 — the `Core` contracts

`Core` is the contract layer, and the test for a type belonging here is **"must two
or more layers name this type?"** — not "is it small?". Its folders are exactly
`Bases/`, `Common/Interfaces/` and `Common/Exceptions/`. The body's audit 2 covers
`Core`'s purity; these are the shape rules for what is already inside it.

> **Two things in `Core` that look wrong and are not.** `using MassTransit;` in the
> entity base — `NewId`'s types live in the `MassTransit` namespace even though the
> package is `NewId`; expected, not a stray dependency and not a third package. And
> an analyzer `PackageReference Update=` entry in `Core.csproj` — analyzers come
> from the solution-level build props, and an `Update` pins a version rather than
> adding a dependency. Only an `Include` of a third *runtime* package is body
> check 2.2.

**2.4 A new top-level folder in `Core`** — *MEDIUM* ·
`facade-module-architecture` › *core-contracts*
`Find:` `ls -R src/Core/`
A fourth folder is a claim that `Core` has acquired a fourth kind of contract.
Sometimes it has; usually the type belongs to the one facade or module that names
it. Hold every type in it against the three disqualifiers: it needs a package
beyond `Humanizer` and `NewId`, only one layer will ever name it, or its name
states a business concept — any one and it belongs in a facade or module.

**2.5 A third lifetime marker, or a proposed singleton marker** — *HIGH* ·
`facade-module-architecture` › *core-contracts*
`Find:` `ls src/Core/Common/Interfaces/`
There are exactly two, both empty: `IScopedService` and `ITransientService`.
**There is deliberately no singleton marker** — a shared instance is a decision
with real consequences, so every singleton is one explicit line in the owning
facade's `Startup.cs`, where its configuration and ordering are visible at the call
site. A third marker reshapes a contract every layer already depends on — see 2.8.

**2.6 A lifetime marker on the implementation instead of the interface** —
*MEDIUM* · `facade-module-architecture` › *core-contracts* + `module-feature`
`Find:` `grep -rn ": IScopedService\|: ITransientService" src/Infrastructure/`, then
confirm each hit is an **interface** declaration.
Implementing the marker *is* the lifetime decision, and the interface is what the
scan binds. On the class it still resolves, which is exactly why it survives: the
lifetime is now stated in the one place a reader of the contract does not look.
The grep is a starting point, not a proof — it misses a multi-line base list and a
marker inherited through an intermediate interface.

**2.7 A new exception leaf that is off-shape** — *MEDIUM* ·
`facade-module-architecture` › *core-contracts*
`Find:` `grep -rn ": HttpCustomException" src/Core/`, then open each new one.
The house shape is `sealed`, deriving from `HttpCustomException`, with two
constructors — `(message)` and `(message, innerException)` — and no
`[Serializable]` / `SerializationInfo` ceremony, which is obsolete on modern .NET
and is not carried by new exceptions. **Placement and shape are all this check
claims:** whether every constructor pins its status, and whether a leaf carries a
payload or sits on the non-HTTP base, are `dotnet-code-review` checks 5.1 and 5.2.
Route them; do not re-grade them here.

**2.8 A `Core` contract reshaped rather than extended** — *HIGH* ·
`facade-module-architecture` › *core-contracts*
`Find:` in diff mode, `git diff <base>...HEAD -- src/Core/` and flag any
*modification* to `BaseEntity`, `BaseEntity<TId>`, `IEntity`, `HttpCustomException`,
`SuccessResultWrapper<TData>`, `ErrorResultWrapper` or the marker interfaces, as
opposed to a new file beside them. **In a sweep there is no diff:** read those types
against the shapes in *core-contracts* and flag any member the reference does not
carry.
Grow `Core` by adding a leaf under an existing contract. A leaf is free — a new
sealed exception is one file the middleware handles the day it is written, because
it matches on `HttpCustomException` and reads its status and message. A reshape
forces every layer to re-agree: an audit-user or soft-delete field on the entity
base, a `Data` property on `ErrorResultWrapper` (an error response carries
diagnostics, not a payload), a payload constructor, extra members on the empty
`IEntity` marker.

## Audit 4 — facade anatomy

**4.10 A facade without exactly one `Startup.cs`, or one not named `Startup`** —
*MEDIUM* · `facade-module-architecture` › *facades*
`Find:` `ls src/Infrastructure/Facades/*/Startup.cs` compared against
`ls src/Infrastructure/Facades/`.
Every facade owns exactly one `Startup.cs` declaring `internal static class Startup`
— always that name — exposing one `AddX()`. A registration file under another name
is invisible to the reader who has learned where to look; two of them mean the
facade has two entry points and the composition root can compose half of it. The
sanctioned exception is a facade with independent sub-capabilities, each with its
own `Startup` composed at the facade's root (4.14). **Visibility is body check 3.4.**

**4.11 An options binding that is not the four calls** — *MEDIUM* ·
`facade-module-architecture` › *facades*
`Find:` `grep -rn -A3 "AddOptions<" src/Infrastructure/`
Always the same four, in order: `AddOptions<T>()` → `BindConfiguration(nameof(T))`
→ `ValidateDataAnnotationsRecursively()` → `ValidateOnStart()`. Two specific misses
to look for. A **string literal** in `BindConfiguration` instead of `nameof(T)` —
the section name is the type name, and a literal is how the two drift apart, after
which it binds nothing, silently. And a **missing `ValidateOnStart()`**, which moves
a configuration failure from process start to the first request that needs the
value: a different environment, a different person, and a much worse error.

**4.12 A `UseX()` on a facade that never touches the request pipeline** —
*MEDIUM* · `facade-module-architecture` › *facades*
`Find:` `grep -rn "IApplicationBuilder Use" src/Infrastructure/Facades/`
`UseX()` exists only where the facade contributes middleware, CORS or OpenAPI, and
stays a one-liner in the same `Startup`. A `UseX()` that only resolves something at
startup belongs in `AddX()`, and it puts the facade into an ordered chain where
order is behaviour (body check 5.3) for no reason. **The mirror is the real
finding:** read each facade's `Startup` for a middleware registration with no
`UseX()` beside it — that facade has hidden its pipeline contribution somewhere the
ordered chain cannot show it.

**4.13 A second entry point on a facade** — *MEDIUM* ·
`facade-module-architecture` › *facades*
`Find:` `grep -rn "static IServiceCollection Add" src/Infrastructure/Facades/*/Startup.cs`
A named registration block becomes a **`private static`** extension in the same
file, not a second surface for the composition root to call. One entry point is
what lets a facade rearrange internally without a caller noticing; a second one is
a second order it can be composed in, and eventually is. **Not a finding:** the
sub-capability `Startup`s of a composite facade, each of which legitimately exposes
its own `AddX()` to that facade's root.

**4.14 A sub-capability composed from the composition root instead of its facade** —
*MEDIUM* · `facade-module-architecture` › *facades*
`Find:` read the composite facade's `AddX()`, then `grep -n "\.Add" src/Infrastructure/Startup.cs`
for its sub-capability names.
A facade with independent sub-capabilities gives each its own `Startup` and composes
them **at its own root**. When the composition root calls them directly it has taken
on knowledge of a facade's internals, and the facade can no longer add or reorder a
sub-capability without editing the root. **Except capabilities under
`Facades/Common/`, which the composition root composes directly by design** — the
shipped chain calls `Common`'s marker scan and its individual capabilities as
first-class lines. Do not report those.

**4.15 A `Facades/Common/` subfolder that is not shaped like a miniature facade** —
*MEDIUM* · `facade-module-architecture` › *facades*
`Find:` `ls src/Infrastructure/Facades/Common/`
`Common` is shared substrate (`Extensions/`, `Attributes/`, `Converters/`,
`Filters/`, shared `Requests/`/`Responses/`) **plus** a nursery of capabilities, each
a subfolder with its own folders, its own settings and its own `Startup.cs` if it
needs registration, composed upward like any facade. A micro-capability and a full
integration have the identical shape; only the number of leaves differs. The failing
shape is a folder at `Common`'s root that is neither substrate nor a capability —
most often one holding a single settings class, which is a leaf of the feature that
reads it, not a capability.

> **Two things on the Facades axis that look wrong and are not.** A **large
> `Facades/Common/` capability** — reach decides, not size; a niche integration can
> grow to dozens of files across ten subfolders and still belong in `Common`,
> because the next project will not take that dependency. And **`Auth` and
> `Identity` as two facades** — they answer different questions, and `Identity`
> holding entities and services does not make it a module: every project that needs
> an account system reuses it whole. *Business-shaped is not business-specific.*

`Facades/Common`'s root `Startup` owns the assembly scan that makes the `Core`
lifetime markers real. That is why a module needs no facade-style `Startup` — see
the body's suppression list, and body check 4.6 for the finding in the other
direction.

## Comparison data — the facade set and the module tiers

Neither list is a whitelist; a solution adds capabilities. They are what you hold an
unfamiliar folder against, with the two placement questions.

**The base facade set — 21.** A production service keeps the set and grows *inside*
it, so a new top-level facade is a decision worth naming in the report:

`Apm` · `Auth` · `BackgroundJobs` · `Cache` · `Common` · `Cors` · `Definitions` ·
`ElasticSearch` · `FileStorage` · `HealthChecks` · `Identity` · `Logging` ·
`Mailing` · `Mapping` · `Medias` · `Middleware` · `MQTT` · `Notification` ·
`OpenAPI` · `Persistence` · `Validations`

**The two placement questions**, run in order, are what the set is compared with:
would this code still make sense in a project with a completely different business
domain (yes → `Facades/`), and is it a technology many projects reuse (yes → a
top-level facade; shared substrate, or a capability only some projects need →
`Facades/Common/`).

**The module tiers.** Create a folder when its trigger is real, never in advance —
an empty `Commands/` is noise. A folder outside this set is body check 4.2.

| Tier | Folder | Trigger |
|---|---|---|
| 1 | `Entities/` `Requests/` `Responses/` `Services/` | every module |
| 2 | `Seeders/` | the module ships reference data |
| 2 | `Validations/` | a rule must hit the database |
| 3 | `Commands/` `Queries/` `DomainEvents/` | the module is driven through MediatR |
| 3 | `Settings/` | it binds its own options section |
| 3 | `Enums/` | the capability owns enums |
| 3 | `Expressions/` | write-once reusable expressions |
| 3 | `ElkEntities/` | `Elk`-prefixed search documents |

> **On the event folder's name — precedence, stated because two skills differ.**
> `facade-module-architecture`'s own tier list still prints this folder as
> `Events/`. `mediatr-messaging` rules that **`DomainEvents/` is the name** and
> `Events/` the older one for the same thing, never to be newly created — and it is
> the skill that owns the message folder, so it governs and the table above follows
> it. Do not read the two names as a distinction. **An `Events/` folder that already
> exists is explicitly not a defect either skill flags:** report a new one (body
> check 4.8) and leave an old one alone.

`Requests/` and `Responses/` may be subfoldered by theme once they grow.
**`Services/` never subfolders**, and every file in it is a service part (body
check 4.9).

## Comparison data — settings placement

There is no centralized settings folder. A settings class lives with the code that
reads it, beside the `Startup` that binds it. Body check 4.4 is the sweep; this is
what it compares against.

| Setting belongs to | Lives in | Bound by |
|---|---|---|
| A top-level facade | the facade root, named for **the concern it configures**, not for the facade | that facade's `Startup`, or one of its sub-`Startup`s |
| A `Common` sub-capability | beside the capability's `Startup.cs`, moving into `<Capability>/Settings/` as it grows | that capability's own `Startup.cs` |
| A business module | `Modules/<Feature>/Settings/<Feature>Settings.cs` | a tiny `Startup.cs` in that same folder |

The naming half is the half that drifts: a facade's settings file named after the
facade rather than the concern stops matching its configuration section, which is
the type name (4.11).

Two shapes fail the table, and both are folders rather than files — which is why
`find … -type d -name "Settings"` finds them:

- **A one-feature settings folder at the root of `Common/`.** A folder there is a
  claim that a new shared capability exists; one settings class is not a capability
  (4.15).
- **A centralized `Common/Settings/` holding unrelated capabilities' settings.** The
  superseded pattern: it detaches configuration from the code that consumes it and
  can only grow by pulling more settings away from their owners. Older services
  carry one. The fix is directional, not a rewrite — do not add to it, and move a
  setting out when its owner is next touched.

**The mirror rule.** Only the JSON *topic* lives in `Web/Configurations/`; the
settings *class* stays with its owner. A settings class found there is body
check 5.7.

## Audit 5 — configuration and boot order

**The base set — 13 topics, in load order:**

`appsettings` · `logger` · `apm` · `hangfire` · `healthcheck` · `openapi` · `cors` ·
`filestorage` · `mail` · `security` · `database` · `httpclient` · `cache`

Each topic is a **pair**: `<topic>.json` required, `<topic>.<Environment>.json`
optional and overlaying it, both with `reloadOnChange: true`. Declaration order is
load order and later wins. A topic named for a vendor rather than a concern, and a
section grown into `appsettings.json`, are body check 5.5.

**5.8 A topic file with no load line, or a load line with no file** — *MEDIUM* ·
`facade-module-architecture` › *composition-root*
`Find:` `ls src/Web/Configurations/*.json`, then read every `AddJsonFiles` line in
`src/Web/Configurations/Startup.cs` and compare the two lists.
A file nobody loads is dead configuration that reads as live — the most expensive
kind, because someone will edit it and wait for an effect. (A load line whose *base*
file is missing is the louder failure and is body check 5.6.)

**5.9 `AddEnvironmentVariables()` not last, or absent** — *HIGH* ·
`facade-module-architecture` › *composition-root*
`Find:` open `src/Web/Configurations/Startup.cs` and read the end of the chain.
Later wins, so environment variables last is what lets a container or CI deployment
override any file without editing one — and it is where deployment secrets come
from. Move it up and a checked-in file silently beats the environment: the service
runs on the repository's values while the platform reports the variables as set.

**5.10 The `optional` flags inverted** — *HIGH* ·
`facade-module-architecture` › *composition-root*
`Find:` read the `AddJsonFiles` helper in the same file.
The base file is `optional: false` and the overlay `optional: true`. Inverted, a
missing base becomes a silently empty section instead of a startup failure, and a
missing overlay takes the process down in every environment that does not ship one.

**5.11 `RegisterSerilog()` reached before `AddConfigurations()`** — *MEDIUM* ·
`facade-module-architecture` › *composition-root*
`Find:` `grep -n "AddConfigurations\|RegisterSerilog" src/Web/Program.cs`
Serilog reads its own topic file, which is not in configuration until
`AddConfigurations()` has run. Reversed, logging silently falls back to defaults —
and the first thing you lose is the log that would have told you.

**5.12 The `Program.cs` boot order altered** — *MEDIUM; HIGH when a step is
dropped* · `facade-module-architecture` › *composition-root*
`Find:` open `src/Web/Program.cs` and read it top to bottom — it is short enough.
Four orderings carry a reason, and a diff that changes any of them is behavioural:

- **`StaticLogger.EnsureInitialized()` first, outside the `try`.** A configuration or
  DI failure happens before the logger is configured from its topic file; without the
  bootstrap logger that crash is silent. `EnsureInitialized()` is idempotent and is
  called again in `catch` and `finally` **by design** — that repetition is not the
  finding.
- **One flush point, in `finally`.** The process has exactly one
  `Log.CloseAndFlushAsync()`; a second one elsewhere splits the guarantee.
- **`catch (Exception ex) when (ex is not HostAbortedException)`.** That exception is
  how EF Core design-time tooling stops the host after building the service provider.
  Without the filter, every `dotnet ef` command logs a fatal error.
- **`AddControllers()` carries the two `Web`-owned decisions and nothing else** —
  controller JSON behaviour and the invalid-model response shape (body check 3.3).

`InitializeDatabasesAsync()` awaited before `UseInfrastructure()` and `Run()` is body
check 5.4 — the same read of the same file answers both. It creates a scope, because
the root provider cannot resolve the scoped context; applies pending migrations when
the auto-migration flag is on; and seeds through the `IDbInitializer` abstraction, so
the root knows the interface and never the seeding logic. A seeding call written
directly into the root is body check 5.1.
