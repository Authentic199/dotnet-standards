# Solution Layout, Build Properties and Package Versions

> **Provenance.** Recommendations in *Solution files* and *Package versions* are
> `from-kit` (`dotnet-claude-kit` at `cd83d31`, TRIAGE rows A28, D07, B28), adjusted
> for this stack. The *Observed conventions* section is `from-my-code` and is
> recorded, not recommended. The two are kept apart on purpose — never blend them
> into one voice.
>
> **Dated content.** Version-specific guidance below is accurate as of
> **2026-07-26**. .NET 11 GA is **2026-11-10**; re-check the version rows after that.

## Core Principles

1. **Set shared settings once.** `Directory.Build.props` at the solution root, not
   the same six properties repeated in every `.csproj`.
2. **Never hardcode a package version from memory.** Training data is full of
   outdated versions. Resolve the real one, always.
3. **One target framework per solution.** Drift between projects is a defect.
4. **`src/` and `tests/` are separate trees.** A test project under `src/` is
   misfiled.

## Solution files

| File | Purpose |
|---|---|
| `<Name>.slnx` | Solution. The XML format is cleaner and merges without conflict markers. Needs a recent SDK — on .NET 8 the classic `.sln` is still the correct choice. |
| `Directory.Build.props` | Shared MSBuild properties and solution-wide analyzer packages. |
| `Directory.Packages.props` | Central package management, when used. |
| `global.json` | Pins the SDK so every machine and CI runner builds with the same compiler. |
| `.editorconfig` | Code style. |

### Directory.Build.props

Two jobs: shared properties, and analyzers that every project should carry.

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <CodeAnalysisRuleSet>$(MSBuildThisFileDirectory)dotnet.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Roslynator.Analyzers">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="SonarAnalyzer.CSharp">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="StyleCop.Analyzers">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

`PrivateAssets=all` keeps analyzers out of the published output and stops them
flowing transitively to consumers. Both attributes are required — omitting
`IncludeAssets` silently disables the analyzer in referencing projects.

Set `TreatWarningsAsErrors` deliberately. Turning it on across a codebase that
already has warnings converts a working build into a broken one; turn it on for new
projects, or after the existing warnings are cleared.

### global.json

```json
{
  "sdk": {
    "version": "8.0.400",
    "rollForward": "latestFeature"
  }
}
```

`latestFeature` allows patch and feature-band upgrades while refusing a major jump —
the usual balance between reproducibility and not blocking developers on an exact
patch.

## Package versions

### Never write a version from memory

This is the single most common way an assistant corrupts a project file. Model
training data contains superseded versions, and a plausible-looking version number is
indistinguishable from a correct one until restore fails — or worse, until it
succeeds against something old.

```bash
# DO — resolves the current stable version from NuGet
dotnet add package MediatR
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package Serilog.AspNetCore

# DON'T — a remembered version is a guess wearing a number
dotnet add package MediatR --version 12.0.0
dotnet add package Serilog.AspNetCore --version 8.0.0
```

Rules that follow from this:

- **`Microsoft.*` and `System.*` packages track the runtime major.** On `net8.0`,
  use 8.x for `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.*` and
  `Microsoft.AspNetCore.*`. Mixing majors is how you get a load failure that reads
  like an unrelated bug.
- **Never downgrade a package already in the project** unless explicitly asked, or
  to resolve a documented incompatibility. Say which, and why.
- **Prefer stable over preview** unless the project deliberately targets a preview
  feature.
- **Unsure?** `dotnet package search <name>`, or ask. Do not guess.

### Central package management

When `Directory.Packages.props` exists:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Resolve real versions with `dotnet add package` — do not copy these -->
    <PackageVersion Include="MediatR" Version="" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="" />
    <PackageVersion Include="AutoMapper" Version="" />
  </ItemGroup>
</Project>
```

**Invariant:** with central management on, individual `.csproj` files must **not**
specify `Version=` on a `<PackageReference>`. Half-migrated solutions — some projects
central, some pinned locally — produce restore behaviour nobody can predict.

Central package management is a solution-wide choice. Do not introduce it into a
solution that has not adopted it as a side effect of some other change; it is its own
migration.

## The six solution-hygiene checks

Run these before a first build of an unfamiliar solution, or when a build breaks for
structural rather than code reasons. Two globs and a read — no script needed.

| # | Check | Why it matters |
|---|---|---|
| 1 | A `.sln` or `.slnx` exists at the root | Without one, tooling guesses which projects belong together |
| 2 | `Directory.Build.props` exists when there are more than two projects | Otherwise shared settings drift project by project |
| 3 | `global.json` exists | Unpinned SDK means "works on my machine" is literally true |
| 4 | `.editorconfig` exists **and carries rules** | A file containing only `root = true` passes a presence check while enforcing nothing |
| 5 | At least one test project exists | |
| 6 | **All projects resolve to the same target framework** | See below — this is the one that finds real bugs |

Check 6 needs care to run correctly. Compare the **base** framework only: split
`<TargetFrameworks>` on `;`, drop `$(...)` MSBuild expansions, and strip platform
suffixes (`-windows`, `-android`, `-ios`) so legitimate multi-targeting is not
reported as drift. Read both `<TargetFramework>` and `<TargetFrameworks>` — checking
only the singular tag misses multi-targeted projects entirely.

Checks 1–5 are advisory: report them, do not block on them. Check 6 is different — a
mixed target framework is a defect and should be raised as one.

### Anti-example — target framework drift

Observed in a real solution in this codebase: six projects on `net8.0` and one
integration-test project left on `net7.0`. Nothing failed loudly. The test project
silently ran against a different BCL surface and a different analyzer set from the
code it was testing.

```xml
<!-- BAD -->
<!-- src/*/*.csproj                    --> <TargetFramework>net8.0</TargetFramework>
<!-- tests/App.IntegrationTests.csproj --> <TargetFramework>net7.0</TargetFramework>

<!-- GOOD — declare it once, in Directory.Build.props, and let every project inherit -->
<TargetFramework>net8.0</TargetFramework>
```

When a project genuinely needs a different framework, say so in a comment in that
`.csproj`. An unexplained difference reads as an oversight because it usually is one.

## Naming

| Element | Convention | Example |
|---|---|---|
| Layer project | Bare layer name, no application prefix | `Core`, `Infrastructure`, `Web` |
| Provider-specific project | `<Role>.<Provider>` | `Migrators.MySql`, `Migrators.PostgreSql` |
| Namespace | Matches the folder path from the project root | `Infrastructure.Facades.Auth` |
| Facade folder | PascalCase, technical noun | `Facades/Persistence/` |
| Module folder | PascalCase, plural business noun | `Modules/Customers/` |
| Test project | `<ProjectName>.UnitTests` / `.IntegrationTests` | `Infrastructure.UnitTests` |
| Configuration file | lowercase, one per capability | `Configurations/cache.json` |

Bare project names work because there is one host per repository. The kit's
`AppName.Layer` convention (`MyApp.Api`, `MyApp.Domain`) belongs to solutions that
host several applications side by side — do not apply it here, and do not rename
existing projects to match it.

## Observed conventions — recorded, not recommended

These describe how the existing services are actually built. They are neither
endorsed nor flagged as faults; they are here so that reading an existing solution
does not read as a list of problems, and so nothing "fixes" them unasked.

| Area | Kit recommendation | What the existing services do |
|---|---|---|
| Solution format | `.slnx` | Classic `.sln`. Consistent with the `net8.0` target. |
| Package versions | `Directory.Packages.props` | Declared per project in each `.csproj`. No central management. |
| SDK pinning | `global.json` | Not pinned. |
| Code style | `.editorconfig` carries the rules | `.editorconfig` holds `root = true` only; style is enforced by `stylecop.json` and `dotnet.ruleset`, wired through `CodeAnalysisRuleSet`. |
| Target framework | `net10.0` | `net8.0` across the solution. |

**Do not migrate any of these as a side effect of unrelated work.** Each is a
solution-wide change with its own review. If one is wanted, it is its own task.
