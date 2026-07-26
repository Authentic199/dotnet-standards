## Project graph & solution layout

Four kinds of project, one direction of reference. Every edge points down the
chain; nothing points back up.

| Project | SDK | References | Referenced by |
|---|---|---|---|
| `Core` | `Microsoft.NET.Sdk` | no project (NuGet only) | `Infrastructure` |
| `Infrastructure` | `Microsoft.NET.Sdk` | `Core` | `Web`, every `Migrators.<Provider>`, test projects |
| `Migrators.<Provider>` | `Microsoft.NET.Sdk` | `Infrastructure` | `Web` |
| `Web` | `Microsoft.NET.Sdk.Web` | `Infrastructure` + **every** `Migrators.<Provider>` | nothing |

### Reference rules

- **Reference one link up the chain, never two.** `Web` does not reference `Core`;
  `Core`'s types arrive transitively through `Infrastructure`. Adding
  `<ProjectReference Include="..\Core\Core.csproj" />` to `Web.csproj` compiles
  fine and changes nothing at build time — which is why it is a review finding
  rather than a build error. It lets a controller reach `Core` directly while the
  layer chain only appears to hold.
- **No upward edges.** `Core` never references `Infrastructure`; `Infrastructure`
  never references `Web` or a migrator.
- **Migrators are siblings.** `Migrators.<A>` never references `Migrators.<B>`.
- **A test project references `Infrastructure` and nothing else**, and lives under
  `tests/`.

### Solution root

```
MyApp.sln
Directory.Build.props
Directory.Build.targets
dotnet.ruleset
stylecop.json
src/
  Core/
  Infrastructure/
  Migrators/
    Migrators.<Provider>/
  Web/
tests/
  Infrastructure.UnitTests/
  Infrastructure.IntegrationTests/
```

(Container, CI and editor files sit alongside these; they are outside this skill.)

| File | What it does |
|---|---|
| `MyApp.sln` | One classic `.sln` for the repository — not `.slnx`. Solution folders mirror the disk: `src`, `Migrators`, `tests`, plus `Solution Items` for the root files. |
| `Directory.Build.props` | Everything true of every project: `CodeAnalysisRuleSet` → `dotnet.ruleset`, `GenerateDocumentationFile`, `ImplicitUsings`, `Nullable`, and the analyzer `PackageReference`s with `PrivateAssets=all`. |
| `Directory.Build.targets` | Properties that can only be computed after the csproj is evaluated — e.g. `DocumentationFile` → `$(OutputPath)$(AssemblyName).xml`, since `$(OutputPath)` is not known at props time. |
| `dotnet.ruleset` | The one shared ruleset. Rule suppressions belong here, not in individual csproj files. |
| `stylecop.json` | Carried at the root as a shared settings file. It is not wired into the build — no project declares it as an `AdditionalFiles` item, so StyleCop does not read it. Editing it changes nothing until someone wires it. |

Conventions to preserve when you add a project:

- **Analyzers are included once, versioned per project.** `Directory.Build.props`
  is the only place that `Include`s the analyzer packages. A project that needs a
  newer one overrides it with
  `<PackageReference Update="Roslynator.Analyzers" Version="..." />` — `Update`,
  never a second `Include`. Consequence worth knowing: the version in props is a
  floor, not the effective version. Read the csproj before you trust it.
- **No central package management.** There is no `Directory.Packages.props`; each
  csproj declares its own `PackageReference` versions.
- **No `global.json`.** The SDK version is not pinned.

### What a `Migrators.<Provider>` project is for

A migrator project holds exactly one thing: the generated EF Core migration files
for one database provider. Its csproj is a `TargetFramework` and a single
`ProjectReference` to `Infrastructure`. No hand-written code goes there.

It exists because the `DbContext` is provider-neutral and lives in
`Infrastructure`, while generated migrations are not. One assembly per provider
keeps two providers' migration histories from colliding.

**The project name is a runtime contract.** Persistence startup reads the provider
from configuration and hands EF Core the migrations assembly by name:

```csharp
options => options.MigrationsAssembly($"Migrators.{dbProvider}")
```

So the project must be named `Migrators.<ProviderKey>` using the exact configured
key. A mismatch fails at runtime on the first migration, not at build time.
`Web` references *all* migrators so every one ships in the output; configuration —
not the reference graph — decides which is used.

Adding a provider: new `src/Migrators/Migrators.<Provider>/`, reference
`Infrastructure`, add it to `Web`. Nothing else in the graph changes.

### One TargetFramework for the whole solution

Every csproj — `src/` and `tests/` alike — declares its own `<TargetFramework>`,
and every one must declare the same value. There is no central TFM property to
inherit from, so the rule holds only if you upgrade every csproj in one commit.

**Anti-example (from a real base solution), one project missed in a framework
upgrade:**

```xml
<!-- every src/ project and the unit-test project -->
<TargetFramework>net8.0</TargetFramework>

<!-- tests/Infrastructure.IntegrationTests -->
<TargetFramework>net7.0</TargetFramework>   <!-- drifted -->
```

Nothing failed loudly — the drifted project still built. But its language version,
analyzer behaviour and package resolution all differed from the code it was
supposed to test, so the tests exercised a different framework than production
runs on. Drift in a test project is the easiest to miss for exactly this reason.

**Fix the offending csproj.** Do not "solve" drift by introducing a central TFM
property or central package management — that replaces the convention instead of
repairing the defect.

### The base is deliberately minimal

This graph is a floor, not a finished system. A base solution ships with a thin
`Core`, a handful of facades and one or two modules precisely so a project can grow
into it.

Growth happens *inside* `Infrastructure` — more facades, more modules, more
registrations — and inside `Web`'s controllers. It does not add projects or edges.
A mature production solution built on this base has the same four project kinds,
the same reference direction and the same root files as the day it was scaffolded.
The only sanctioned new projects are another `Migrators.<Provider>` sibling and
another test project under `tests/`. So when you meet a solution whose
`Infrastructure/Startup.cs` registers thirty components, that is this architecture
grown up, not a different one.

If a change seems to need a new edge — `Core` reaching for `Infrastructure`, `Web`
reaching past it — that is a signal something belongs in a different layer, not
that the graph needs an exception.
