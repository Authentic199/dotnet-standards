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

**R26** — `Timestamps reach the database as UTC without your help: the DbContext converts every DateTimeOffset on the way in and back on the way out. Never call ToUniversalTime() or ToLocalTime() on a value going to or coming from the repository. Use DateTimeOffset, not DateTime — a DateTime property is outside that conversion and is the thing to change.`
**Applies when** the scan found a `ConfigureConventions` override converting
`DateTimeOffset` (or an equivalent global value converter).
*Prevents:* the double conversion — a value shifted by hand and again by the
convention — and the silent local-time drift when a `DateTime` slips into a
model everything else treats as UTC.

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

Both rules below were **narrowed to pointers** after the first real run, which
showed them restating what `facade-module-architecture` and `module-feature`
already teach in more detail. Ship them as written; do not re-expand them into a
description of the layout.

**R9** — `Placement is owned by facade-module-architecture (where a file, project or registration goes) and module-feature (service and validator internals). Load the skill rather than inferring the rule from the current tree.`
*Prevents:* a flattened copy of a rule the skill states with its exceptions
intact, which a reader then trusts instead of the real source.

**R10** — `Do not create a new top-level folder or a new project without asking.`
*Prevents:* an invented project or top-level folder that the solution file, CI
and every sibling repository then disagree with.
*Why this arm survived:* it is a guard, not a convention. The owning skill says
where things go; it does not say *stop and ask before inventing a new place*,
and that is the part a scan cannot supply either.

**R27** — `Every bullet below records what is in the tree today, not that the skill is void. "X does not exist here" is never a licence to skip X's skill — and it is at its most misleading when the task is to introduce X, which is exactly when that skill governs. Load the owning skill before designing, while the spec or plan is still being written, not when you start typing code.`
**Applies when** section 6b ships — that is, whenever the scan found any
divergence at all. It is not a bullet in the `Rules` section: it is the
**standing preamble of section 6b**, written directly under that heading and
above the first bullet.
*Prevents:* the failure this rule was written from. A session designing a
feature read `mediatr-messaging — neither MediatR nor a ConcurrencyHandler
exists in this solution` as *this skill does not apply here*, and designed the
MediatR surface from memory — inventing a folder outside the house vocabulary,
a handler layout the owning skill forbids, and a registration anchor that fails
silently. The task was *introducing* the capability, which is the moment the
skill is most binding, and the section as written read as a licence to skip it.
*Note:* R20 bars reconstructing a convention from memory; this bars the
reasoning that makes a reader think no convention applies. A section-6b bullet
describes a **tree**, never a **rule** — see `template.md` §6b, which requires
every capability-absent bullet to carry its own *load it first* clause on top of
this preamble.

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

## Request data

**Applies when** the scan found request or DTO classes bound from HTTP — true
wherever this stack's controllers are present.

**R32** — `Never normalize a string at a call site: no Trim(), TrimStart/TrimEnd, ToLower() or ToUpper() on a request property, in a guard's comparison, in a validator predicate or in a mapping. Whitespace or casing that must not arrive is one rule in the request's validator, rejected there. Trim() is a parsing call — it is allowed only next to the split or slice that produced the string.`
*Prevents:* the split-brain a scattered `Trim()` creates — a uniqueness guard
comparing a trimmed value while the write stores the untrimmed one, and the
"repair" that assigns the trimmed value back onto the request, spreading the same
call to a second site. Also stops a case-folding call landing on an entity property
inside a query predicate, where the database computes it per row and the column's
index stops answering.
*Note:* no analyzer flags this. It reads as defensive hygiene, which is why it
propagates through a file untouched by review.

---

## Communication and language

**Applies when** always. This group ships in every generated file — it is the one
group that costs almost nothing and is wrong in a way nobody notices until a
whole document comes back in the wrong language.

**R22** — `Talk to the user in Vietnamese: chat replies, questions, summaries, and any prose document written for the user. Code, identifiers, commands and paths stay in English.`
*Prevents:* a long report or a design document delivered in a language the reader
did not ask for, which then has to be produced twice.

**R23** — `When using Superpowers: brainstorming output is written in Vietnamese. The plan stays in English.`
*Prevents:* the split being guessed. Brainstorming is a conversation with the
user and follows R22; a plan is an artifact other agents execute, so it stays in
the language the tooling and the codebase use.
*Note:* self-gating — the rule states its own condition, so it ships whether or
not the scan can detect Superpowers in the repository.

---

## Process ownership

**Applies when** always — **self-gating, exactly like R23**: each rule states its
own condition, so the group ships whether or not the scan can detect Superpowers
in the repository.

**This group exists because `CLAUDE.md` is the only place these four can bind.**
Superpowers states its own precedence — *"User instructions (CLAUDE.md, AGENTS.md,
direct requests) take precedence over skills"* — so a rule written here outranks
a process skill that is already holding the wheel. A rule stating the same thing
inside a plugin skill does not, and that is the difference the failure below
turned on.

*The failure, measured 2026-08-02.* A consumer session building a feature
skipped this plugin at both ends of the same branch: it wrote a MediatR
architecture spec with no knowledge skill loaded, then ran more than twenty
subagent review rounds — the final whole-branch review among them — without
loading one review skill or spawning one specialist agent. Full report:
`docs/field-reports/2026-08-02-skill-routing-failure.md`.

**R28** — `The .NET process in this repository has two entry points: /dotnet-feature to take a change from idea to reviewed commit, /dotnet-review to review a branch, a diff or a set of paths. Superpowers brainstorming, plan writing, TDD and subagent-driven development are phases those flows call — do not assemble that sequence by hand.`
*Prevents:* a whole session running a hand-assembled Superpowers pipeline that no
.NET flow ever enters. The flow that owned the entire observed task sat in the
session's skill list all day and was never opened.

**R29** — `Subagents that review or test .NET code are the ones dotnet-review-flow names, never general-purpose, and their criteria are the four rubric skills — do not hand-write a constraint block in place of a rubric.`
*Prevents:* a review whose coverage equals whatever the coordinating session
happened to think of. In the observed failure the performance lens was never
applied once, and the architecture and security lenses ran on improvised
criteria.
*Note:* names no agent on purpose. The roster lives in `dotnet-review-flow`;
repeating it here creates a second list to keep in step.

**R30** — `A Superpowers process skill saying "do not invoke any other skill" is barring implementation skills. It does not suspend the knowledge layer: before any brainstorm answer, plan step or subagent prompt states a .NET convention, load the dotnet-standards skill that owns it.`
*Prevents:* a specification written from memory during brainstorming, which is
where conventions get decided with no skill looking.
*Note:* this is ordinary reading, not an override of another plugin. Two
statements of that ban scope it to implementation skills by name; the third is
the one-line summary of the same rule with the qualifier dropped. Reading the
summary in the light of what it summarises is the correct reading — and it has
to be stated here, because the moment it matters is the moment the process skill
holds the wheel.

**R31** — `Re-route when the work changes phase — design, code, test, review. choosing-a-dotnet-skill is a lookup table consulted at each phase, not a file read once at the start of a session.`
*Prevents:* the shape both observed incidents took. The router was in context all
day, was consulted once for a placement question, and was never revisited when
the work changed nature.

---

## Scope, workflow and verification

**Applies when** always, except where noted.

**R15** — `Ask before committing or pushing. Do not wait to be told: propose the commit, state what it contains, and get an explicit yes first.`
*Prevents:* a self-started commit that skips review, or a push that cannot be
taken back.

**R16** — `Do not add a new NuGet package without first explaining why the packages already referenced cannot do the job. Exception: when a house pattern or skill this project follows requires a specific library and the project lacks it, add that library — name the pattern it serves in the same message; do not stall the task waiting for approval.`
*Prevents:* dependencies accumulating by convenience, widening the security and
licensing surface — without the over-tight reading that stalls a house pattern
because its named library is missing (user ruling, 2026-07-31).

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

**R24** — `Before writing a new helper, service or extension, search the solution for one that already does the job: call it instead, or say why it does not fit. Add nothing the current task does not need — no extra parameter, branch, type or file — beyond what this file's conventions already require.`
*Prevents:* the three shapes over-build takes in this stack — code written for a
need that has not arrived, a second implementation of something the solution
already has, and a shape more elaborate than the task called for. All three were
observed in real sessions, including sessions where the plugin was installed and
never entered — the context no skill and no flow reaches, and the reason this
line carries the constraint instead of a skill.
*Note:* R17 bounds the diff outward — do not touch what the task did not name.
This one bounds it inward — do not inflate what it did. R16 is the same
search-first move at package scale.

**R25** — `Before writing a helper yourself, look in Infrastructure/Facades/Common/ — Common/Extensions/ first: reuse what is there; if the logic is reusable and no extension exists, add it there as an extension so other modules can call it; write inline code only when it is genuinely one-off. If a house extension this situation calls for is missing from this project, recreate it in Common/Extensions/ from its canonical form instead of inlining a bespoke copy.`
**Applies when** the scan found `Infrastructure/Facades/Common/`.
*Prevents:* the accumulated extension layer being bypassed — the same utility
rewritten per module; regexes and serializer settings scattered instead of
centralized.
*Note:* R24's search-first move, with the address filled in.
