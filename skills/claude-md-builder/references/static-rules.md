# Approved static rules

Read at PHASE 3. These are the rules that hold across every .NET repository this
plugin serves — .NET 8 / C#, ASP.NET Core with controllers, EF Core, PostgreSQL,
Redis, Elasticsearch, Hangfire.

**How to use this file.** Ship a rule only when its **Applies when** condition
was proved by the scan. Fill every `<slot>` from scan findings; a slot that
cannot be filled means the rule does not ship — a placeholder must never reach
the output. Copy the **rule line only**; the *Prevents* line exists to help you
choose, not to be pasted.

None of these duplicates an analyzer. That was checked against a live
StyleCop + SonarAnalyzer + Roslynator configuration before the set was approved.

---

## EF Core, schema and migrations

**Applies when** the scan found EF Core packages **and** at least one file under
a `Migrations/` folder. No migrations → skip this entire group.

**R1** — `Never hand-edit files under Migrations/ or the ...ModelSnapshot.cs. Generate them with dotnet ef migrations add, and repair a wrong migration with dotnet ef migrations remove followed by a fresh generate.`
*Prevents:* a hand-patched migration drifting from the snapshot, so the next
generate produces a corrupt diff.

**R2** — `Ask before changing database schema — columns, tables, relations, indexes. Never leave an entity or DbContext mapping change without a matching migration in the same change set.`
*Prevents:* silent model-versus-database drift that only surfaces at deploy.

**R3** — `Never delete, rename or rewrite a migration that is already committed or applied to a shared environment. Correct it with a new migration instead.`
*Prevents:* a rewritten migration desynchronising `__EFMigrationsHistory`, so
other environments can no longer migrate.

**R4** — `Run EF Core commands with all three switches, never bare:`
```
dotnet ef migrations add <Name> -p <MigrationsProject> -s <StartupProject> -c <DbContext>
dotnet ef database update      -p <MigrationsProject> -s <StartupProject> -c <DbContext>
```
*Slots:* `<MigrationsProject>` from scan row 7, `<StartupProject>` from row 1,
`<DbContext>` from row 8. Drop `-c` if exactly one context exists and the scan
found no ambiguity.
*Prevents:* a bare `dotnet ef` writing the migration into the wrong project, or
failing to find a `DbContext` at all.

---

## Async and cancellation

**Applies when** the solution targets ASP.NET Core (always true for this stack).

**R6** — `Every method that performs I/O — EF Core, HTTP, Redis, Elasticsearch — must declare a CancellationToken parameter and pass it to every awaited call. Controller actions take it from the request; do not swallow it at a layer boundary.`
*Prevents:* work continuing after the client disconnected — connections held,
transactions kept open, long jobs that cannot be stopped.
*Note:* `CA2016` only forwards a token that is already in scope. It never asks
for the parameter to exist, which is exactly the gap this rule covers.

**R11** — `Never block on async: no .Result, no .Wait(), no GetAwaiter().GetResult(). Await the call, or make the caller async.`
*Prevents:* deadlock on a synchronisation context, and exceptions buried inside
`AggregateException`.
*Note:* Sonar `S4462` flags this, but only as a warning — and warnings are not
errors in this stack, so violations survive. The rule ships anyway, by ruling.

---

## Architecture and placement

**Applies when** the scan found a modular layout — a folder holding domain
modules, or facades for integrations.

**R9** — `Business logic lives in <ModulesPath>. Controllers only validate the route, delegate to a service, and return; never put a query, a mapping or a business branch in a controller.`
*Slot:* `<ModulesPath>` from the project layout in scan row 2.
*Prevents:* logic stranded in a controller — untestable, unreusable, and a second
home for business rules.

**R10** — `Add a new capability inside the existing skeleton — a folder under <ModulesPath> for a domain, under <FacadesPath> for an integration. Do not create a new top-level folder or a new project without asking.`
*Slots:* both from scan row 2. Ship only the arms whose paths were found.
*Prevents:* an invented project or top-level folder that the solution file, CI
and every sibling repository then disagree with.

---

## Secrets and configuration

**R12a** — ships when question 2 was answered *forbid*:
`Config files under <ConfigPath> are tracked by git and hold placeholders only. Supply every real credential through environment variables using the Section__Key form.`

**R12b** — ships when question 2 was answered *deliberate*:
`Real credentials in <ConfigPath> are intentional — this repository is hosted in a private registry. Do not replace them with placeholders, do not "fix" them, and do not report them as a leak.`

*Applies when* scan row 6 tripped and question 2 was answered. Unanswered → ship
neither. `<ConfigPath>` is the tracked config directory the scan found.
*Prevents:* R12a — a real credential written into a tracked file on request;
R12b — an agent breaking a working environment by "fixing" a deliberate one.

**R13** — `Never read, print, echo or copy a secret value, and never let one reach a transcript, a log line, or a commit message. To learn what configuration exists, read the key structure, not the values.`
**Applies when** always. Ships even alongside R12b — the ban on leaking a value
outward is absolute regardless of what the repository chooses to commit.
*Prevents:* a secret pulled into a transcript, a log or a commit message just to
check something.

---

## Build and analyzers

**R5** — `Do not silence analyzer warnings by editing <RulesetFile>, Directory.Build.props, or adding #pragma warning disable. Fix the code, or ask.`
**Applies when** scan row 3 found an active analyzer configuration.
*Slot:* `<RulesetFile>` — the ruleset or `.editorconfig` actually present.
*Prevents:* a build "fixed" by lowering severity instead of changing code, which
also drifts the shared standard.
*Note:* this is a rule **about** the linter, not a copy of one — the thing an
analyzer cannot protect on its own.

---

## Scope, workflow and verification

**Applies when** always, except where noted.

**R15** — `Ask before committing or pushing. Do not wait to be told: propose the commit, state what it contains, and get an explicit yes first.`
*Prevents:* a self-started commit that skips review, or a push that cannot be
taken back.

**R16** — `Do not add a new NuGet package without first explaining why the packages already referenced cannot do the job.`
*Prevents:* dependencies accumulating by convenience, widening the security and
licensing surface.

**R17** — `Do not refactor code outside the scope of the current task. Note improvements you spot; act on them only when explicitly asked.`
*Prevents:* a diff that outgrows its task and can no longer be reviewed.

**R18** — `When a requirement is ambiguous or two designs are both defensible, stop and ask. Do not guess and do not pick silently.`
*Prevents:* three hundred lines written against a misread requirement.

**R19** — `Verify with the project's own commands and show the output. Never report a change as working from reading code alone.`
*Slot:* name the actual commands inline when the scan verified them; otherwise
ship the rule without them.
*Prevents:* "done" claimed from reading code, with the failure handed to the
reviewer or to production.

**R20** — `Read the convention and workflow documents this file points to before writing code. Do not reconstruct a convention from memory.`
**Applies when** scan row 12 found at least one such document.
*Prevents:* the pointers being ignored — which is what keeps `CLAUDE.md` short
enough to be followed at all.

**R21** — `Do not change the tech stack: do not swap, remove or replace an existing library, framework or storage engine. Propose it and wait.`
*Prevents:* a runtime broken by a substitution nobody asked for. Distinct from
R16, which is about *adding*.
