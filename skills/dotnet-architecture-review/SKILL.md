---
name: dotnet-architecture-review
description: >-
  This skill should be used when reviewing a .NET solution's architecture: an
  architecture or layering review, dependency direction and project references
  across Core, Infrastructure, Migrators and Web, a layer or namespace leak, a
  file placed in the wrong project, facade or module, or an endpoint or
  registration sitting outside the composition root. Not for: blast radius,
  severity, slop — dotnet-code-review; secrets, injection, data exposure —
  dotnet-security-review; N+1, allocation, blocking — dotnet-performance-review;
  the placement and wiring rules themselves — facade-module-architecture; service
  and handler internals — module-feature; the review process —
  superpowers:requesting-code-review, superpowers:receiving-code-review;
  executing cleanup — /simplify.
---

## Overview

This is a **rubric, not a pipeline.** It says what to check when the question is
*where code sits and what it is allowed to reference*, in what order, and how to
rank what it finds. It does not run the review — gathering the diff, dispatching
the reviewer, receiving and triaging the feedback belong to
`superpowers:requesting-code-review` and `superpowers:receiving-code-review`. It
does not apply fixes either: moving a file across projects is an ordinary change,
and only the cleanup such a move leaves behind goes to `/simplify`.

**One baseline, not four.** This stack has exactly one architecture, and no step
below asks which one is in play or infers it from the code:

```
Core  ←  Infrastructure  ←  Migrators.<Provider>  ←  Web
```

with `Infrastructure` split on two axes — `Facades/` for technical capabilities,
`Modules/` for business ones — and a single flat composition root. **This is not
Clean Architecture and not Vertical Slice; never audit it against either
template.** There is no domain ring to protect and no slice boundary to police,
so the findings those templates generate are not merely unwelcome here, they are
wrong: the repository abstraction is house law (it lives in the `Persistence`
facade), controllers rather than auto-registered endpoint groups are house law,
and a facade that looks business-shaped is house law too — *business-shaped is
not business-specific*.

**It checks conformance; it does not define it.** The shape above is defined by
`facade-module-architecture` and its six `references/` files; the lines quoted
here are the target being compared against, not a second statement of the rule.
Two consequences, and they are the whole difference between a useful report and a
noisy one:

- **A finding must trace to a shipped skill's body, or be a defect in any
  codebase.** Same law as `dotnet-code-review` Principle 5, *The rubric cites the
  owning skill; it never re-teaches it*. A rubric that re-teaches doctrine becomes
  a second source of truth, and the day the two disagree the reader cannot tell
  which one is stale.
- **Cite by number and name** — every finding points at the rule it breaks, so the
  author can argue with the rule rather than with the reviewer.

**Every check is a manual instruction.** There is no code-analysis server, no
Roslyn tooling and no project-graph tool in this stack, and no step below may
assume one. Each check is written as something a reader executes: open a `.csproj`
and read its `ProjectReference` lines, `grep -rn` a namespace or a symbol under a
named folder, or build and read the diagnostics. A step that cannot be written
that way is not a check. This is stated once and holds for this body and its
references.

**Scope: the shape, not the contents.** Reach for this rubric when placement and
layering *are* the change — a new project, facade or module, a moved file, an
edited `.csproj`, a touched composition root, or an inherited codebase whose
conformance is unknown. Whether the code *inside* a correctly placed file is
right is the breadth pass, `dotnet-code-review`, which routes here when layering
is what the change is mostly about.

## Core Principles

1. **The compiler enforces the chain; nothing enforces the two axes.** `Core`,
   `Infrastructure`, `Migrators.<Provider>` and `Web` are separate projects, so
   MSBuild rejects a cycle and a missing reference before a reviewer sees them —
   which is why there is no cycle audit here. `Facades/` and `Modules/` are
   folders *inside one project*: every type there can name every other type, and
   the only thing standing between the two axes is this review. Spend the
   attention accordingly — the boundary that compiles is the one that rots.

2. **A placement finding is about the folder, not the code in it.** "This file is
   in the wrong project" and "this method is wrong" are different reviews. Report
   where the file belongs and stop; the contents are `dotnet-code-review`'s pass,
   and the rule for the contents belongs to the owning skill. A review that
   follows a misplaced file into its implementation never finishes the sweep it
   started.

3. **Name the move, not the principle.** Every finding ends in a concrete
   destination — *"move to `src/Infrastructure/Facades/Common/<Capability>/`"*,
   *"delete the reference; the type arrives transitively"*, *"invoke it from the
   owning module's `Startup.cs`"*. "Violates layering" is unactionable, and an
   architecture finding with no destination is reliably deferred forever.

## Two modes, and say which

| Mode | When | Scope rule |
|---|---|---|
| **Diff** | A change is under review | Score what the change introduced or moved. A pre-existing misplacement in a touched file is INFO — unless the change adds to it, and then it scores normally (`dotnet-code-review` Principle 6, *Report what changed, not what was already there*). |
| **Sweep** | An inherited or unaudited codebase, or a release gate | Everything is in scope; score normally. Group findings by audit, not by file, or the report reads as a list of complaints rather than a shape. |

State the mode in the report Summary. The same misplaced file is INFO in one mode
and MEDIUM in the other, and a reader who does not know which mode ran cannot
tell a clean pass from a narrow one. Diff mode is the default; a sweep scored
under Principle 6 would grade every finding INFO and empty the report.

## The audit, in order

Five audits, each narrowing the unit of inspection: solution → assembly →
project → folder → file. **Run all five, in order, and report coverage.** The
order matters because a finding at one level explains the findings below it — but
stopping at the first CRITICAL only forces a second full pass after a one-line
fix. When audit 1 produces a CRITICAL, say in the Summary that the findings below
it may be its consequences, and re-audit once it is gone.

| # | Audit | Unit | Answers |
|---|---|---|---|
| 1 | Project graph | the `.csproj` set | May these projects reference each other this way? |
| 2 | Namespace leaks | `using` lines and package lists | Does a type cross a boundary the references left open? |
| 3 | Presentation boundary | `Web` | Is anything defined or registered in `Web` that `Web` does not own? |
| 4 | Facades / Modules conformance | folders under `Infrastructure` | Is this file on the right axis, in a sanctioned folder? |
| 5 | Composition root | three files | Is wiring visible, single, and in the right order? |

Every `Find:` below is the whole instruction — there is no tool that does it for
you. Paths are written for the standard layout (`src/Core/`,
`src/Infrastructure/`, `src/Migrators/Migrators.<Provider>/`, `src/Web/`,
`tests/`); **resolve the solution's real roots from the `.sln` once, before the
first grep.** A path that does not exist returns nothing, and an empty result
reads exactly like a clean pass. Patterns assume `grep -rn --include=*.cs` unless
stated.

### 1 — Project graph

`Find:` `grep -rn "ProjectReference" --include=*.csproj src/ tests/`

One command prints the whole graph. Compare every arrow against the reference
table in `facade-module-architecture`, *Solution & project graph* — that table is
the target, not a paraphrase of it.

| # | Finding | Severity |
|---|---|---|
| 1.1 | A reference the table forbids: `Core` referencing any project; `Infrastructure` → `Web` or a migrator; a migrator → another migrator or `Web`. Nothing is broken yet — that is the point: it compiles today, it legalises every leak written underneath it, and it is the only finding here that gets strictly more expensive every day it stands | **CRITICAL** |
| 1.2 | `Web` → `Core` **directly**. One link up, never two; `Core`'s types arrive transitively. It compiles, which is exactly why it is a review finding and not a build error | **HIGH** |
| 1.3 | A migrator directory that `Web` does not reference. `Web` references `Infrastructure` **and every** migrator, so a missing edge surfaces as a runtime failure on the provider nobody tested. `Find:` compare `ls src/Migrators/` against `Web`'s reference list | **HIGH** |
| 1.4 | A project outside the sanctioned set. The base is a floor: growth happens inside `Infrastructure` and `Web/Controllers/`, and the only sanctioned new projects are another migrator sibling and another test project. The finding is a question — which existing project should have absorbed it | **HIGH** |
| 1.5 | A migrator's folder name that does not match the configured provider key. The name is a runtime contract — persistence startup passes `Migrators.{dbProvider}` as the migrations assembly, so a mismatch is a startup failure, not a style point. `Find:` `grep -rn "MigrationsAssembly" src/Infrastructure/` and read the provider key in the database configuration topic | **CRITICAL** |
| 1.6 | Csprojs disagreeing on `<TargetFramework>`. Every csproj declares the same one and they are upgraded in one commit. `Find:` `grep -rn "<TargetFramework>" --include=*.csproj src/ tests/`, then read the distinct values | **MEDIUM** |

**When the graph looks wrong but you cannot tell whether an edge is
load-bearing:** comment the `ProjectReference` out, `dotnet build`, and read the
diagnostics. The `CS0246` list *is* the leak inventory, ranked by file, for the
cost of one build — this is the "build and read the diagnostics" leg of the
manual-instruction rule doing real work.

Migration *contents* are not this rubric's business. A migrator holding
hand-written code is a finding here (MEDIUM — it holds generated migrations for
one provider and nothing else); whether a migration is safe belongs to
`ef-core-data-access`. Solution-file and analyzer hygiene is in
`references/conformance-checks.md`.

### 2 — Namespace leaks

References can be clean while types still cross. The axis boundary lives inside
one assembly, so this audit is the only enforcement it has. Direction: **`Modules/`
may name `Facades/` types freely** — business capabilities consume technical ones,
which is the design. The reverse is the finding.

| # | Finding | Severity |
|---|---|---|
| 2.1 | A facade naming a module type. `Find:` `grep -rn "using .*\.Modules\." src/Infrastructure/Facades/`. It fails the question that defines the axis — *would this code still make sense in a project with a completely different business domain?* **Escalate to CRITICAL when the naming is mutual:** a facade and a module that reference each other are a type-level cycle MSBuild cannot see, and neither can be moved, extracted or reused without the other coming along · `common-extensions`, *A base `Common/` file never names a module* + `list-query-pipeline`, `references/anti-patterns.md`, *Domain knowledge welded into the reflection walk* | **HIGH** |
| 2.2 | `Core` carrying a package beyond `Humanizer` and `NewId`, or a `using` reaching into `Infrastructure`. `Core` is the contract layer, not a utility bag. `Find:` open `src/Core/Core.csproj` and read every `PackageReference`; `grep -rn "using " src/Core/` | **HIGH** |
| 2.3 | A type in `Core` that only one layer names. The rule is **"must two or more layers name this type?"**, not "is it small?". `Find:` for each type added to `Core` in the diff, `grep -rn "<TypeName>" src/` and count the layers. Same MEDIUM from the other direction: a type in `Core` or in a facade whose **name states a business concept** | **MEDIUM** |

**The standard repair for 2.1's commonest shape.** The module type a facade
reaches for is usually the principal — the user entity behind auth, permissions
or auditing. The repair is neither moving the entity nor living with the leak:
point the facade at the principal abstraction under `Facades/Identity/Base`,
which the module's entity already implements. The facade keeps the concept, the
module keeps the type, and the cycle unwinds without adding an edge. Name that
destination in the finding — a 2.1 with no destination is deferred forever.

**A shared `Common/` extension has a different repair, and it is not always a move.**
A feature member on a base extension moves to `Modules/<Feature>/Expressions/`; a
module type welded into a generic reflection walk moves nowhere at all — it becomes a
parameter or an attribute the caller passes in.

**Three things that look like findings here and are not.** Each is house law, and
reporting one burns the author's trust in the whole report:

- **A `using` in `Web` naming a `Core` type.** `Web` uses `Core` types constantly —
  the wrappers, the exceptions. What it must not do is declare the reference
  *edge*, which is check 1.2.
- **A module naming another module's type.** No shipped body forbids it. That rule
  belongs to the Modular-Monolith template, which this stack does not use.
- **A facade that reads as business logic.** *Business-shaped is not
  business-specific*: a capability every product with the same need would reuse
  whole is a facade however domain-like it looks.

### 3 — Presentation boundary

| # | Finding | Severity |
|---|---|---|
| 3.1 | A controller outside `src/Web/Controllers/`. The HTTP surface stops being enumerable from one folder, and a project that must not know about HTTP now does. `Find:` `grep -rln "\[ApiController\]\|: BaseController" src/` and discard the hits under `src/Web/Controllers/` | **HIGH** |
| 3.2 | Anything under `Web/Controllers/` that is not `BaseController.cs` or a folder named after a module that exposes endpoints. A helper, filter or attribute parked there is a technical capability — `src/Infrastructure/Facades/Common/`. `Find:` `ls src/Web/Controllers/` | **MEDIUM** |
| 3.3 | A registration inside `Web`. `Web` registers nothing itself; the only two decisions it owns are controller JSON behaviour and the shape of the invalid-model response. Everything else belongs in a facade's or module's `AddX()`. `Find:` `grep -rn "Services.Add" src/Web/` | **HIGH** |
| 3.4 | A facade `Startup` that is not `internal static class Startup`. Only `Infrastructure/Startup.cs` composes facades; a `public` one invites composition from anywhere. `Find:` `grep -rn "class Startup" src/Infrastructure/Facades/`. **Escalate to HIGH** when something outside `Infrastructure` already calls it | **MEDIUM** |
| 3.5 | A controller named for two modules — `OrderShipmentsController`. `Find:` `ls src/Web/Controllers/*/`; a class name that concatenates two module names from audit 4's list is the hit. **Then read that controller's route templates to name the destination — do not guess it from the class name.** Every action's route opening with the parent's `{id:guid}` means the parent owns the family: it moves to `OrdersController` as a suffix part (`OrdersController.Shipments.cs`), its operations to that module's service part whose only foreign reach is a `Send`. **Renaming it to the child (`ShipmentsController`) is the wrong fix while the routes still nest**, and a full CRUD surface on the sub-resource does not make it a top-level controller. **Grade HIGH, not a naming nit:** the route family currently sits outside both modules' surfaces, so neither module's controller enumerates it · `api-surface`, *Route shapes* and *Controller partials* + `module-feature`, *Call the service, or send a message?* | **HIGH** |

Route shapes, casing, attributes and `ProducesResponseType` are **not** this
rubric's — `api-surface` owns what a controller looks like; this audit asks only
where it lives and what it registers.

### 4 — Facades / Modules conformance

`Find:` `ls src/Infrastructure/Facades/` and, per module,
`ls src/Infrastructure/Modules/<Feature>/`

Apply `facade-module-architecture`'s two placement questions to every added or
moved file: would this code still make sense in a project with a completely
different business domain (yes → `Facades/`), and does it have the reach of a
top-level facade or of `Facades/Common/`?

| # | Finding | Severity |
|---|---|---|
| 4.1 | A file on the wrong axis — business meaning under `Facades/`, or a reusable technical mechanism under `Modules/` | **HIGH** |
| 4.2 | A module folder outside the sanctioned tier list, or a subfoldered `Services/` (`Requests/` and `Responses/` may be themed; `Services/` never subfolders). A folder created **in advance** of its trigger is **INFO** — empty scaffolding teaches the next author a shape the module has not earned | **MEDIUM** |
| 4.3 | A `Mappings/` folder anywhere. There is none: a response's profile lives in the same file as the response, below it, so the contract and its projection cannot drift apart silently. `Find:` `find src/Infrastructure -type d -name "Mappings"` | **MEDIUM** |
| 4.4 | A centralized settings folder, or a facade's settings class parked in `Web/`. Settings follow their service: a top-level facade's at the facade root, a `Common` capability's beside its `Startup.cs`, a module's under `Modules/<Feature>/Settings/`. `Find:` `find src/Infrastructure -type d -name "Settings"` and check each one's parent | **MEDIUM** |
| 4.5 | An enum declared inside an entity, response or service file. Every enum a capability owns lives in its `Enums/` folder. `Find:` `grep -rn "enum " src/Infrastructure/Modules/` and discard the hits under `/Enums/` | **MEDIUM** |
| 4.6 | A facade-style `Startup.cs` at a module root. A module has none — the marker scan in `Facades/Common` registers its services. The single exception is `Settings/Startup.cs` | **MEDIUM** |
| 4.7 | A request-shaped envelope filed in the notification folder, or the reverse. The interface decides the folder, not the name, and the folder is all a reader sees before opening the file. `Find:` `grep -rn "IRequest\|INotification" src/Infrastructure/Modules/<Feature>/` and compare each hit's folder · `mediatr-messaging` | **MEDIUM** |
| 4.8 | A **newly created** `Events/` folder. `DomainEvents/` is the convention; `Events/` is the older name for the same thing, and having both means every search for a module's events runs twice. **An existing `Events/` folder is not a finding** — `mediatr-messaging` rules the ones already there out of scope. Do not report them | **MEDIUM** |

**4.9 — A file in `Services/` that is not a service part** — *MEDIUM* ·
`facade-module-architecture`, *Infrastructure — the Modules axis* +
`module-feature`, *When a service outgrows one file*

`Find:` `ls src/Infrastructure/Modules/<Feature>/Services/`

`Services/` holds services and nothing else. Every file is `<Name>Service.cs` or
`<Name>Service.<Role>.cs`, and every type it declares is `<Name>Service` or
`I<Name>Service`. Flag a file breaking that pattern; a part named after a layer —
`Helper`, `Logic`, `Extensions`; a private-helpers part that declares a partial
*interface*. A part named after a layer is a dumping ground with the `partial`
keyword on it — it attracts everything with no obvious home, and by the time it
needs subfolders the module has lost its shape; a private part declaring a partial
interface has published a helper as API. `<Name>` is this module's own capability:
a `<Name>` concatenating two modules — `OrderShipmentService` — is the two-module
controller's sibling defect (3.5), and those operations are a suffix part of the
owning module's service, foreign data arriving by `Send`.

The destination is specific, and naming it is what makes the finding actionable: a
genuine business rule goes inside the service or on the entity that owns it, a
computed value in the module's `Expressions/`, a reusable technical mechanism on
the Facades axis, a bag of records into `Requests/`/`Responses/` or `Settings/`,
and logic extracted "to keep the service small" is a suffix-named partial, not a
new type.

**4.10 — A module folder that does not name the capability inside it** —
*HIGH* · `facade-module-architecture`, *the split test*

`Find:` run this for **every** module folder in scope, and write the answer
down before moving on:

1. `ls src/Infrastructure/Modules/<Module>/Entities/` — list the entity files.
2. Singularise the folder name (`Devices` → `Device`, `Orders` → `Order`).
3. Compare that word against every entity file name in step 1.

Exactly two comparisons are findings, and both are mechanical:

| Shape | What you will see | Verdict |
|---|---|---|
| **(a) The folder names a concept that has no entity** | `Modules/Devices/Entities/` holds only `DeviceType.cs` — no `Device.cs` | The module is named for something it does not implement, and the aggregate it *does* hold is a capability with no module of its own. **Finding** |
| **(b) Two or more aggregate entities, each with its own request/response/validator family** | `Modules/Orders/Entities/` holds `Order.cs` **and** `Carrier.cs`, with `Requests/Carriers/`, `Validations/Carriers/` beside the order's own | The second one is a capability living in the first one's folder. **Finding** |

Then state the split test in the finding — *what exists only because of X and
is only ever created because of X stays with X; anything created or consulted
in its own right is its own module* — and name the destination:
`Modules/<Aggregate>/`, a module named for the aggregate itself.

**A type catalogue with its own CRUD surface is its own capability** however
much one consumer dominates it: "it is only used by devices" is not the test,
"it is only ever created because a device is created" is, and a catalogue
administered on its own screens fails that.

**Not a finding — the settings shapes.** Typed classes that exist only to be
persisted by one aggregate (a JSON settings column's shape, one per enum
member) pass the split test and stay beside that aggregate. Say so under
*Audit coverage* rather than reporting it.

**Two more things that are not findings.** A **big `Facades/Common/` capability** —
reach decides, not size; a niche integration can grow to dozens of files and still
belong in `Common`, because the next project will not take that dependency. And a
**module with no facade-style `Startup.cs`** — that is correct, and 4.6 is the
finding in the other direction.

For a **new** facade or module, record the answers to the two placement questions
in the report as INFO even when they pass. An axis decision made silently is the
one nobody can revisit.

### 5 — Composition root

Three files boot the system: `src/Web/Program.cs`,
`src/Web/Configurations/Startup.cs`, `src/Infrastructure/Startup.cs`. Open all
three; none is long enough to skim.

| # | Finding | Severity |
|---|---|---|
| 5.1 | `AddInfrastructure` is not one flat fluent chain in a single statement, or a line in it is not a call into a facade or module — an `AddScoped<>` for a concrete business type, an inline `AddOptions<T>()`. Framework plumbing with no owner is the only exception. `Find:` `grep -n "\.Add\(Scoped\|Singleton\|Transient\|Options\)<" src/Infrastructure/Startup.cs` | **HIGH** |
| 5.2 | A duplicate registration hiding in the chain. The chain is ordered for readability, not semantics, which is exactly why a duplicate survives review. `Find:` `grep -o "\.Add[A-Za-z]*(" src/Infrastructure/Startup.cs \| sort \| uniq -d` — it prints each repeated call and **nothing at all when the chain is clean**, so empty output is the pass; use `uniq -cd` if you want the counts | **MEDIUM** |
| 5.3 | A `UseX()` appended at the end of `UseInfrastructure` by default. **The order of the `UseX()` calls IS the middleware pipeline** — a new one goes at the position its middleware must occupy. A diff that *moves* a line here is a behavioural change, not a cleanup: **HIGH** when the moved middleware is exception handling, authentication, authorization or CORS, MEDIUM otherwise, and the finding asks for the intended order, not for a revert. `Find:` in diff mode, `git diff <base>...HEAD -- src/Infrastructure/Startup.cs` and read the `UseInfrastructure` hunk; in a sweep, read the current order and judge it against the pipeline it implies | **HIGH** |
| 5.4 | `InitializeDatabasesAsync` not awaited **before** `UseInfrastructure()` and `Run()`. Out of order it serves traffic against an unmigrated schema for the length of the migration. `Find:` `grep -n "InitializeDatabasesAsync\|UseInfrastructure\|\.Run()" src/Web/Program.cs` | **HIGH** |
| 5.5 | A configuration topic grown into `appsettings.json` instead of its own `<topic>.json` plus one `AddJsonFiles` line, or a topic file named for a vendor rather than a concern. Declaration order is load order, later wins, and `AddEnvironmentVariables()` is last. `Find:` `ls src/Web/Configurations/` and open its `Startup.cs` | **MEDIUM** |
| 5.6 | An environment overlay with no base file. `<topic>.json` is required and `<topic>.<Environment>.json` optional — the base is what the overlay overlays | **HIGH** |
| 5.7 | A settings **class** under `Web/Configurations/`. Only the JSON topic lives here; the class stays with its owner (see 4.4) | **MEDIUM** |

## Severity

The four words and their general meanings are `dotnet-code-review`'s — Principle
3, *One severity vocabulary, four words*, and its *Severity ladder* section. This
rubric does not restate them; it calibrates them, because architecture findings
are consequence-poor on the day they are written and consequence-rich later, and
left uncalibrated almost every one of them argues its way to HIGH.

| Severity | In an architecture finding |
|---|---|
| **CRITICAL** | The layering cannot be enforced from here, or it breaks at runtime: a wrong-direction project reference, a migrator name that does not match its provider key. Nothing is broken yet in the first case — that is what makes it critical. Everything downstream is now possible and no reviewer will catch it twice. |
| **HIGH** | A boundary is actually crossed. A reference, a type or a registration is somewhere the shipped rules say it must never be, and something compiles that should not — but no behaviour is wrong and no data is at risk. |
| **MEDIUM** | Shape or naming inside a correct boundary. The file is in the right project and on the right axis but the wrong folder, the wrong filename, or the wrong visibility. It costs a reader, not a caller. |
| **INFO** | A pre-existing placement noticed in diff mode, a boundary question no shipped body settles, or a placement done unusually well. |

Three calibrations settle the arguments this rubric actually gets:

- **Severity is consequence, not effort** — and not how far a file would have to
  move. A wrong-direction reference is a one-line delete and stays CRITICAL; a
  three-week extraction of a module that leaked into a facade stays HIGH.
- **A boundary crossing that compiles is still a crossing.** `Web` → `Core`
  compiles and ships, and nothing catches it but this rubric. "The compiler allows
  it" is never a reason to lower a severity — it is the reason the check exists.
- **Placement alone is never CRITICAL.** If a placement finding feels CRITICAL,
  the real finding is the reference edge or the leak the placement enabled —
  report that one instead.

## The report

If this review produces a report, write it to a file under `docs/code-review/`
in the reviewed repository (create the folder if absent) — the file, not the
chat copy, is the deliverable.

One report, the severity words as headings, always in this order. **Every section
appears every time**; write `None.` when a section is empty, because an absent
section is ambiguous between *checked, found nothing* and *did not check* — and in
an audit that ambiguity is the whole distinction.

**Report language.** Write the report in the language the reviewed project's
`CLAUDE.md` sets for talking to the user. If it sets none, write in the language
the user is using in this session. Identifiers, paths, commands, file names and
quoted code stay in English. **The field labels below are English because this
skill is written in English — they are field names, not fixed strings. Translate
them.** A report in the wrong language is a defect even when every finding in it
is correct.

**The header table is not optional and every row appears.** A row that cannot
apply carries `—`; a blank cell is a defect.

```markdown
# Architecture review: <scope> — <one line on what it covers>

| | |
|---|---|
| **Date** | <yyyy-MM-dd> |
| **Branch** | `<branch>` (<n> commits) |
| **Base** | `<sha>` on `<base branch>`, or `the empty tree (standing code)` for a path scope |
| **Worktree** | `<path>`, or `—` |
| **Scope** | <n> files — <what they are> |
| **Excluded** | <paths or shapes left out, or `—`> |
| **Method** | <which audits ran; and that every CRITICAL/HIGH was re-checked at the cited `file:line`> |

### Summary
<mode (diff or sweep) · what was audited · PASS / FAIL and the findings that decide it>

### Conformance
PASS / FAIL — one line per audit: project graph · namespace leaks ·
presentation boundary · facades and modules · composition root

### CRITICAL
**<title>** — `<file>:<line>` · check <n.n>
- **Defect:** <what crosses what>
- **Failure:** <what the crossing lets happen, or what it stops being possible>
- **Fix:** <the move> · <owning skill>

### HIGH
**<title>** — `<file>:<line>` · check <n.n> … (same three lines)

### MEDIUM
**<title>** — `<file>:<line>` · check <n.n> … (same three lines)

### INFO
**<title>** — `<file>:<line>` · check <n.n> … (same three lines)

### Audit coverage
1 Project graph · 2 Namespace leaks · 3 Presentation boundary · 4 Facades/Modules · 5 Composition root
<ran / skipped and why, per audit>

### What's Good
- <the placement and wiring decisions worth repeating>
```

**Every finding is a header line and at most three labelled lines, and each of
those lines is at most two sentences.** A finding has no fourth line. Anything
longer is an appendix — put it under `## Notes` at the end and reference it by the
finding's title. A `Failure:` line that cannot be written is not a finding: drop
it to INFO or cut it.

**Three things never appear in a finding**, because each is process narration
rather than information:

- **How the review reached it** — no *"the reviewer asked for…"*, no *"verified by
  running…"*. State the conclusion; evidence the reader must re-check goes in
  `## Notes`.
- **A paragraph arguing the severity** — the severity is the heading the finding
  sits under, and the `Failure:` line is its whole argument.
- **Anything already in the header table** — the branch, the base and the scope
  are stated once, at the top, and never again.

Three rules for the findings themselves:

1. **Name both ends of the boundary.** "Misplaced" is not a finding; "`<Type>` sits
   in `Modules/<Feature>/Services/` but declares no service type" is. Evidence is
   `file:line`, or the exact `ls` or csproj line — an architecture finding whose
   evidence is "the structure feels off" cannot be acted on or argued with.
2. **Cite the check number and the owning skill.** The author argues with the rule,
   not with the reviewer, and a rule that turns out to be stale surfaces as a
   contradiction rather than as a second opinion. A finding citing nothing is this
   rubric inventing doctrine, which it may not do.
3. **`FAIL` is decided by CRITICAL and HIGH only.** MEDIUM and INFO do not fail a
   conformance verdict, or the verdict is always FAIL and stops meaning anything.
   A MEDIUM-only report is `PASS`, and the Summary says how many drift findings it
   carries — that is a real distinction and it does not need a new word for it.

**Audit coverage is not optional.** A review that ran only audits 1 and 5 is a
useful report; one that ran only audits 1 and 5 and does not say so is a
misleading one.

## Routing

**Deep dives — sibling rubrics.** This rubric owns the shape. When the change's
risk lives somewhere else, load that one instead of stretching this.

| The change is mostly about | Load |
|---|---|
| The code inside a correctly placed file; blast radius, severity of behavioural findings, slop | `dotnet-code-review` |
| Secrets, injection, authorization gates, mass assignment, data exposure | `dotnet-security-review` |
| Query cost, allocation, blocking, missing indexes | `dotnet-performance-review` |

**Doctrine — the owning knowledge skill.** This rubric notices the disagreement;
the rule itself lives here.

| The finding is about | Owning skill |
|---|---|
| Project, folder, facade or module placement; the project graph; the composition root | `facade-module-architecture` |
| Service structure, partial parts, guards, what may sit in `Services/` | `module-feature` |
| What a controller or endpoint looks like once it is in the right folder — routes, DTO chains, attributes, OpenAPI | `api-surface` |
| What a migration *contains*, versus which project it sits in | `ef-core-data-access` |
| Which folder a message envelope belongs in; handler registration; pipeline behaviours | `mediatr-messaging` |
| Profile placement and mapping mechanics once the file is placed | `automapper-mapping` |
| What may sit in a base `Facades/Common/` file, versus which of `Extensions/`, `Services/`, `Attributes/` it sits in | `common-extensions` |
| Unsure which of the above owns a boundary question | `choosing-a-dotnet-skill` |

**Process.** Requesting the review and triaging what comes back belong to
`superpowers:requesting-code-review` and `superpowers:receiving-code-review`.
**Execution:** moving a file across projects is an ordinary change, made normally;
only the cleanup a move leaves behind — the orphaned folder, the now-unused
`using`, the reference no longer needed — goes to `/simplify`.

## References

**Read `references/conformance-checks.md` when** running a sweep, or when a finding
needs the exact grep and the exact rule text — it holds the long tail behind the
five audits: solution-file and csproj hygiene, the full per-project reference
matrix as a tick-list, the `Core` contract details, facade `Startup` anatomy and
the `Facades/Common` fractal test, the facade base set and module tier list to
compare a folder against, the settings-placement cases, and the configuration
topic list and load order.

## Decision Guide

| Situation | Do this |
|---|---|
| Asked to "check the architecture" with no scope | Ask whether this is the change or the solution, and declare the mode in the Summary. The mode decides every severity. |
| Audit 1 finds an inverted edge | Report it, finish the remaining audits, and say in the Summary that findings below it may be its consequences — then re-audit once it is gone. |
| A diff touches one module only | Run 4 and 5; record 1–3 as skipped under Audit coverage, with the reason. |
| Unsure whether a project reference is load-bearing | Comment it out, `dotnet build`, read the `CS0246` list — that list is the leak inventory. |
| A file is misplaced *and* its contents look wrong | Report the placement, route the contents to `dotnet-code-review`. Do not follow it in. |
| A move is the whole change | Verify both ends: the new folder is sanctioned **and** nothing still references the old namespace (`grep -rn` it). |
| A facade names a module type | HIGH, or CRITICAL if the naming is mutual — that is the only cycle this stack can actually have. |
| A `using` in `Web` names a `Core` type | Not a finding. Check the `.csproj` for the reference edge instead. |
| An existing `Events/` folder | Not a finding — `mediatr-messaging` rules the existing ones out of scope. Flag only a newly created one. |
| A file is in the wrong place but moving it is out of scope | Score it normally, then name the follow-up owner. Never soften a severity to match the budget. |
| Two shipped skills disagree about a placement | INFO stating the contradiction and both citations. Do not pick a winner inside a review. |
| A convention the code breaks is stated in no shipped body | INFO with the question stated. This rubric has no doctrine of its own. |
| Kit or generic advice suggests a finding — a domain ring, dropping the repository wrapper, endpoint groups, feature folders instead of controllers | Not a finding here. Check the rule exists in a shipped body first. |
| A formatter or analyzer already owns it | Say nothing — `dotnet-code-review` Principle 4, *Style is reviewed last, or not at all*. |
| Everything passes | Say PASS, write `None.` into each empty section, and keep *Audit coverage* honest. |
