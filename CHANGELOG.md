# Changelog

All notable changes to `dotnet-standards`.

Versions follow semantic versioning. The version in `.claude-plugin/plugin.json`
is the only signal an installed copy is stale, so it is bumped whenever
components change materially — not only on releases.

---

## [0.3.73] — the skill said both things, and the agent picked the wrong one, 2026-08-08

**Field report from a consumer repository**, three incidents in one session,
**after the skill was loaded**, and the third one message after the agent had
itself written that the behaviour was a violation. Every claim in the report was
re-checked against the files before anything changed here; all six held.

**The headline is a textual contradiction, twelve lines apart.** `api-surface`,
*Controller partials*, said this in a bullet list:

> *"…it does not mean exactly one service. A controller whose route family spans
> modules injects each module's service, and two or three is normal."*

and this in the paragraph below it:

> *"…whose only reach into the foreign module is a `Send` of that module's
> envelope"*

Same situation, two mutually exclusive shapes. The agent followed the bullet —
imperative, in a list, in the section it was already reading — and injected a
second module's service into a controller. `module-feature` settles it and
explains why the loser is not merely unstylish: **envelopes are `internal
sealed`, so a controller physically cannot `Send` one**, which is what makes the
direct-call rule enforceable rather than advisory. The bullet taught the exact
shortcut that design exists to block.

**The bullet was a leftover.** 0.3.32 shipped the two-module-name rule as a user
ruling — *"whose only reach into the foreign module is a `Send`"* — and added the
ownership paragraph without deleting the sentence twelve lines above it. Removing
it now executes that ruling rather than making a new one. **Corpus note, checked
before acting:** three controllers in the canonical project do inject two or
three modules' services, so the deleted sentence described the tree accurately —
it was accurate about the corpus and wrong about the rule, which is precisely the
confusion this release is about. Those sites became defects the day 0.3.32
shipped; no new anti-example is labelled here.

**Second finding, and the more general one: the anti-imitation guard was keyed to
cosmetics.** `api-surface`'s *Pre-convention files* opened by defining its scope
as files that *"typically break four at once: block-scoped `namespace { }`,
block-bodied endpoints, bare `{id}`, and no `<summary>` or
`ProducesResponseType`"*. The file that misled the session had **none of the
four** — file-scoped namespace, expression-bodied actions, constrained id
parameters, complete attributes and XML summaries — while breaking three
structural rules: its own `[Route]`, a two-module name, and a foreign module's
service in its constructor. It read as modern, so the one section that says *do
not copy your neighbour* never fired. **A file can be spelled correctly and
structured illegally, and the structural breaches are the expensive ones.**

**Third: the router sent the deciding question to the wrong skill.** The
*a controller* row assigned **"action body"** to `api-surface` — but the body of a
cross-module action is `module-feature`'s *Call the service, or send a message?*
decision. Asked which skills to load, the session named `api-surface` and
`facade-module-architecture` and not `module-feature`, the skill holding the rule
it was about to break. The *one row, then stop* law actively enforced that: one
row fitted, and its only exception was for planning rather than writing.

**What changed:**

- **`api-surface`** — the contradicting bullet replaced: the constructor injects
  the services of the controller's **own** module, several are fine when a module
  publishes several, and a second module's service in a constructor is a defect to
  report rather than a norm to copy, with the `internal sealed` mechanism named so
  the rule reads as enforcement and not preference.
- **`api-surface`, *Pre-convention files* → *Non-conforming files*** — the four
  symptoms are now stated as *common*, not definitional, followed by: **structural
  breaches wear no costume**; **a file cited as precedent enters scope whether or
  not you are editing it** (*"X already does it this way"* is a claim about the
  tree, never evidence about the rule); and **a non-conforming file you decline to
  fix gets written down**, because silence is indistinguishable from conformance.
  A new line under the anti-pattern table closes the specific trap: a separate
  controller for a nested route family **cannot exist without its own `[Route]`**,
  which *Routes* forbids outright — so it is not a choice between two legal shapes,
  and citing one as *"the pattern we already use"* is citing a violation as a rule.
- **`choosing-a-dotnet-skill`** — the *a controller* row now splits body *shape*
  (`api-surface`) from **which service the body calls** (`module-feature`); a new
  row covers `api/<Parents>/{id}/<Children>` and names both skills as mandatory;
  and *one row, then stop* gains a **second exception: boundary work always loads
  `module-feature`**, with the trap named — *do not settle "this task does not
  touch a service" before loading the skill that defines what touching one means.*
- **`dotnet-review-flow` spawn contract and the four rubrics** — **a file the
  change cites as precedent is in scope even when it is not in the diff**, and
  **where existing code contradicts a loaded skill, the contradiction is the
  finding**. In the incident, one lens graded the shape HIGH and the breadth lens
  graded it INFO on the reasoning that *"the existing controller already does it,
  so this is one decision for both"* — a reviewer reading only the diff cannot see
  the file the diff is imitating.

**This lands direction C** from the 2026-08-07 field report, which the user had
queued third and which arrived with its own evidence: *a precedent in the
repository is not a row in these tables — it answers a question about the tree's
history, not about which skill owns the decision, and a wrong neighbour is copied
exactly as readily as a right one.* It sits in `choosing-a-dotnet-skill`, loaded
first, as that report proposed.

**Honest limits.** All four changes are text; **none is verified behaviourally.**
The report ships three eval cases with a fixture spec — a scratch solution seeded
with a *modern-looking* non-conforming controller — designed to fail before the
change and pass after. They have not been run. Until they are, this release rests
on the same evidence the last three did: the contradiction is provably in the
files, and the fix provably removes it.

---

## [0.3.72] — the report is the deliverable, and it was unreadable, 2026-08-08

**Evidence: 12 real reports** in a consumer repository's `docs/code-review/`,
produced by this plugin's flow over five weeks. The owner named three defects.
All three verified, and the third had a cause inside this plugin's own text.

**1 — No header block.** A flow-produced report opens `Mode: … · Base: …` and a
prose `Scope:` paragraph. No date except in the filename, no branch as a field, no
link to the spec, plan or test report. The owner's two hand-shaped reports carry
seven to ten labelled fields.

**2 — Mixed language: 9 of 12 English, 3 Vietnamese.** The consumer's own
`CLAUDE.md` line 76 already required otherwise — *"Talk to the user in Vietnamese:
chat replies, questions, summaries, **and any prose document written for the
user**"*. **This plugin had no language rule for reports anywhere**; the only
mention of Vietnamese in the whole tree was in `claude-md-builder`, which teaches
a project to *write* that rule. So the plugin taught the rule and then ignored it,
because the English skeleton in the skill body is a stronger signal to a model
than a line in a project file.

**3 — Findings written as essays, and the cause was a shipped rule.**
`dotnet-review-flow`'s report rule 3 read *"Nothing a subagent learned is dropped.
The user paid for six fresh-context passes; a summary that keeps only the blockers
throws away most of what was bought."* It forbids dropping and says nothing about
length, and the `### CONFIRMED findings` placeholder was one uncapped line — so
the way to comply was to write more. A real finding ran eight lines including
*"The reviewer asked for confirmation that…"* and *"This is a widening of a
decision the owner already took with full information"* — process narration, not
information.

**What changed, in all six report templates** (`dotnet-review-flow`,
`dotnet-feature-flow`, `dotnet-code-review`, `dotnet-architecture-review`,
`dotnet-security-review`, `dotnet-performance-review`):

- **A report-language rule**, identical in all six: the report is written in the
  language the reviewed project's `CLAUDE.md` sets for talking to the user, or
  failing that the language the user is using in the session. Identifiers, paths
  and quoted code stay English. It says explicitly that **the skeleton's labels are
  field names to be translated, not fixed strings** — that sentence is the fix for
  the modelling problem, not the rule itself.
- **A seven-row header table** — Date, Branch, Base, Worktree, Scope, Excluded,
  Method — every row present, `—` where inapplicable, blank never. Field set and
  two-column layout chosen by the project owner against their own two exemplars;
  **Mode and Status were considered and cut by them.** `dotnet-feature-flow` takes
  the language rule but no table: its wrapper already carries branch and merge
  state, and the block report it embeds brings its own header.
- **A capped finding shape** — a header line plus at most three labelled lines
  (`Defect:` / `Failure:` / `Fix:`), each at most two sentences, no fourth line.
  Longer material becomes a `## Notes` appendix referenced by id. **A finding with
  no writable `Failure:` line is not a finding** — drop it to INFO or cut it. The
  caps are countable on purpose; "be concise" is advice a weak model discards.
- **Three named bans**, each of them process narration: how the review reached the
  finding, a paragraph arguing the severity, and anything already in the header
  table.
- **Rule 3 rewritten** to close the loophole that produced the essays: *"Nothing a
  subagent learned is dropped — **and nothing is padded either** … Dropping a
  finding and inflating one are both failures of this rule, and the second is the
  one that actually happens."*

**Also: `dotnet-review-flow` 601 → 561**, and it now has `references/` where it had
none. `NO-SIGNAL` and `When a subagent fails` (~100 lines) moved to
`references/failure-branches.md`. Both are contingency branches, and 0.3.71's
criterion is at its strongest here — a contingency is entered on an unmistakable
event (`RED — environment`, a subagent that returned nothing), which is the
opposite of the false-certainty case that keeps a reference closed. Three rules
that change what you do *before* reaching the branch stay in the body.

**Verification found and fixed a real defect, which is the point of running it.**
The pointer-reachability check — *for every moved rule, which token in the body
makes an agent open the file at the right moment?* — showed the two TEST-LOOP
table rows that say **"Enter NO-SIGNAL"** never named the file, and the pointer sat
50 lines further down. Fixed at all three entry points, including the Decision
Guide row. A second catch during the move: the `verification-before-completion`
paragraph closes the *whole shared block* and had been carried into the reference
with the section above it; returned to the body.

**Honest limits.** The token-survival check that worked for 0.3.71 is weak here —
the moved block holds 6 backticked tokens because it is prose, not API names, so
it proves almost nothing. And **no behavioural test was run**: whether a report
produced under these rules is actually shorter and in the right language is
unknown until `/dotnet-review` runs against a real branch. That comparison is
direction H, still queued.

**Cost, stated rather than buried.** Four review skills grew:
`dotnet-security-review` 508 → 548, `dotnet-performance-review` 504 → 544,
`dotnet-architecture-review` 465 → 505, `dotnet-code-review` 270 → 310. The report
rules are duplicated in six places instead of pointed at from one, deliberately —
a report format behind a pointer is the shape that was already being ignored. The
project owner made that trade explicitly: the line-count heuristic loses to the
document they read every day.

**Process note.** The three-way loop was waived by the user for this change, with
the wording approved directly. The boundary proposed and accepted alongside it,
recorded because it will recur: **the loop applies where text is authored or a
rule's meaning changes, not where settled text is relocated verbatim** — running
two independent authors over shipped doctrine invites exactly the relitigation
`CLAUDE.md`'s SETTLED section forbids.

---

## [0.3.71] — split a skill body by trigger, not by topic, 2026-08-08

`ef-core-data-access/SKILL.md` had reached **567 lines** against a sibling norm of
117–450 and skill-creator's <500 bar. Measured first, and two of the three
obvious framings turned out to be wrong.

**It was not the largest.** Five skills sit at or above 500: `dotnet-review-flow`
601 (**with no `references/` at all**), `ef-core-data-access` 567,
`dotnet-security-review` 508, `dotnet-feature-flow` 508,
`dotnet-performance-review` 504. Only `ef-core-data-access` is touched here;
`dotnet-review-flow` is a process skill loaded on every `/dotnet-review` and the
criterion below may not apply to it — logged, not fixed.

**The obvious cut was the wrong cut.** `## Soft delete` was the largest section
(152 lines) and already had a 374-line `references/soft-deletes.md` beside it, so
it read as duplication. It is not: the reference is **scaffolding** — `ISoftDelete.cs`,
`IHidden.cs`, `GlobalQueryFilterExtension.cs`, the node visitor, wiring, checklist
— and the body is **operating doctrine**: the filter belongs to the repository,
deleting is an update, uniqueness must ignore deleted rows, the filter covers the
root set only. Different readers. Moving it down would have rebuilt the exact gap
0.3.70 closed.

**The criterion actually used, and it comes from 0.3.70's field report rather
than from a line count:**

> **To `references/`: what has a trigger that announces itself.** You know when
> you are adding a migration. A token-shaped pointer (direction A) is enough.
> **Kept in the body: what is needed by someone who does not know they need it.**
> That is the real failure mode — false certainty — and no pointer reaches it,
> because nobody goes looking for what they believe they already know.

**What moved** — `references/schema-lifecycle.md` (new, 130 lines): the two
`ApplicationDbContext` overrides, `DatabaseSettings` binding and provider
selection, the full migration workflow, initialization and seeding.
`references/entity-configuration.md` (new, 80 lines): `BaseEntity` vs
`BaseEntity<TId>` and its cross-layer rule, the `HasBaseEntity().UnderscoreTable()`
opening, the explicit foreign-key pair, the `OnDelete` decision table, composite
uniqueness, enum storage.

**What deliberately stayed** — the whole repository/wrapper section and the whole
soft-delete section, untouched; and out of the moved areas, the four rules that
bite when unlooked-up: a committed migration is never deleted (the repair is a
new forward migration), the EF CLI builds first so both commands need a generous
timeout, no `HasMaxLength`/`IsRequired()` in a configuration, and human-read text
is `citext`. The canonical entity + configuration example stays in the body per
direction B.

**Result: 567 → 419**, inside the sibling norm. Both new pointers are
token-shaped — *"before typing `dotnet ef migrations add` …"*, *"before typing
`HasOne`, `OnDelete`, `HasIndex` …"* — not *"when working with migrations"*,
which is the phrasing the field report identifies as unactionable.

**Verification, not eyeballing.** Every one of the 74 backticked tokens in the
removed 256-line block was checked to still exist somewhere in the skill
directory; all 74 do. Two inbound cross-skill citations named sections that moved
and were repointed at the new files: `dotnet-performance-review`'s
`references/performance-checks.md` 1.11 (*DbInitializer seeding*) and its
`SKILL.md` 1.5 (*Entity configuration*).

**Deliberately not done:** splitting the skill in two. That costs a router edit,
a new description and a `Not for:` line in every sibling — too much blast radius
for a line count. **No doctrine changed in this release**; every rule that existed
before still exists, in the body or in a reference.

---

## [0.3.70] — a convention with no check is documentation, 2026-08-08

**Field report** (`docs/field-reports/2026-08-07-search-fields-and-missed-references.md`).
A consumer repository on 0.3.68 shipped three call sites that passed a field
array written at the call site as `ApplySearch`'s second argument instead of
`request.SearchFields`. **No convention was wrong.** The rule was already in
`ef-core-data-access`'s `references/query-conventions.md`, correctly stated. What
was missing was anything that would fail.

**The measurement that makes this release.** Nine per-task reviews plus four
whole-branch lenses — breadth on the strongest available model — read those three
call sites and reported nothing; several quoted the hard-coded array back as a
valid description of the endpoint. Not a model failure: **there was no check, so
there was nothing to fail.** Generalized: a convention that lives only in a
knowledge skill, with no corresponding rubric check, has nothing enforcing it.

**Corpus evidence gathered before writing the check.** Across the four projects
under `reference/projects/`, worktree duplicates excluded: 96 `ApplySearch(`
occurrences; the only ones not passing a request's own `SearchFields` are
`QueryExpressionExtension.cs` itself, where the `IEnumerable` overload forwards
its parameter, and one unit test of the extension supplying a literal set on
purpose. Zero counter-examples in production code across four projects — which is
why the new check is scoped to `src/` and carries both of those as stated
non-findings.

**What changed:**

- **`dotnet-code-review` rubric 1.12** — *A search stage that names its own
  fields*, MEDIUM, owned by `ef-core-data-access` + `list-query-pipeline`.
  `grep -rn -A2 --include=*.cs "\.ApplySearch(" src/`, then read the second
  argument; conforming values are `SearchFields` and `null`. The finding carries
  its own fix — `[NotSearchable]` for the derived set, `searchFieldExcepts` for
  one call site — and requires a deliberate narrowing to *say so* at the site,
  because silence leaves a restriction and an accident indistinguishable. Grep
  smoke-tested against two real projects before shipping, per the 0.3.58 rule.
  `SKILL.md`'s area table and its `1: 1.1-1.11` coverage example updated.
- **`ef-core-data-access` body** now states the `ApplySearch` second-argument rule
  directly, instead of only pointing at `references/query-conventions.md`. First
  application of the field report's direction B — canonical shape in the body,
  rationale in the reference — shipped here as a single-rule probe, not a policy.
- **`list-query-pipeline` decision guide** — the row reading *"must **never** be
  swept by free-text search → `[NotSearchable]`"* was an overclaim, and the
  skill's own `references/property-info-extension.md` already said so: the
  attribute is consulted only where `ApplySearch` derives the field set, and a
  caller naming the property in `SearchFields` still reaches it. In the consumer
  repository that row came close to closing a credential-probing concern that was
  still open. Split into two rows — the derived-set case keeps the attribute; the
  *must never be reachable at this gate* case routes to a decision in that gate's
  own service.

**Recorded, not decided.** The field report's part 2 is a first-person account of
*why* a `references/` file goes unopened — a precedent already in context, a
trigger sentence at the end of a long section, the need arising mid-writing, and
a summary good enough to feel sufficient. Eight directions (A–H) are logged in
the report. Only B is touched here, and only once.

---

## [0.3.69] — a suite that varies is where real failures hide, 2026-08-05

**Version note.** Authored as 0.3.68 in parallel with the session that shipped
`test-report-nudge`'s 0.3.68 below; renumbered at merge time per the
same-version-collision rule — `main` had already moved.

**Field report, third in three releases** (`docs/field-reports/2026-08-05-feedbacl-dotnet-testing.md`).
A consumer repository merged four integration fixtures into one, dropped Respawn
and moved to per-test disjoint data: a tier whose failure count moved 13/59/87 on
one commit became 146/146 in 11–16 seconds, stable across repeated runs. Six
defects in this plugin came out of the exercise. Item 5 is the one that matters
most, and it is not about fixtures.

**The headline: instability is an epistemic failure, not a reliability
annoyance.** Every word this plugin had about flaky tests treated them as
friction. The heavier consequence went unstated — when the total moves, no one
can tell a code failure from a fixture failure, triaging each one costs more than
it returns, and the whole cluster is rationalized into *known noise*. After
stabilization the 13 split cleanly: 8 were fixture artifacts that went green with
no source change, 5 were real, and 2 of those were production defects days old —
a committed migration deleted in a later refactor, taking a unique index and the
one-session-per-device rule out of the model, and a column switched from `citext`
to `text` under a uniqueness guard whose comment still explained that it
deliberately did not lower-case *because* the column was `citext`. Both had a
test failing correctly the whole time, inside a tier nobody believed any more.

**What changed:**

- **`dotnet-testing` Principle 7** — *A tier that varies is broken worse than a
  tier that is red.* The mechanism, plus three operating rules: whole-suite
  totals are not evidence while a tier varies (only a filtered run of a named
  test is); the proof that a variance is fixed is **several consecutive runs on
  one commit**, never a single green; and after stabilizing, **re-triage every
  outstanding failure one at a time** — never carry the old list forward. That
  last step is what surfaced both production defects, and no flow had it.
- **`dotnet-review-flow`, TEST-LOOP** — *A tier that varies is not a tier that
  failed.* Compare the failing **set** across rounds, not the count; confirming
  means re-spawning the tester two or three times against the unchanged tree, and
  **those re-spawns consume no round**; stabilizing is that round's fix; then the
  re-triage, including of any list inherited as "known" or "pre-existing". The
  tier's report row now carries `non-deterministic` beside its verdict.
- **New `dotnet-testing/references/test-isolation.md`** — the option space items
  1–3 of the report say the skill never described. **One factory per assembly**:
  a second `WebApplicationFactory` subclass is a race, because the override route
  samples reach for (`Environment.SetEnvironmentVariable`, which wins by being
  last) belongs to the *process* while xUnit runs collections in parallel inside
  one; `DisableTestParallelization` does not repair it and made it worse in the
  field (`HostFactoryResolver`: *the entry point exited without ever building an
  IHost*); the repair is to parameterise the one factory or use
  `WithWebHostBuilder`. **Three fixture scopes, not two**: `IClassFixture` per
  class, `ICollectionFixture` **per collection** — which is where four containers
  came from, and the reading of it as "once per suite" is the whole defect — and
  xUnit v3's `[assembly: AssemblyFixture(typeof(T))]` for once per assembly.
  **Three isolation strategies**, with Respawn's hidden price named: it forces
  every class sharing the database to serialize. Disjoint data is the only one of
  the three that permits parallel classes, and it ships as a contract (every
  unique-index value generated; every aggregate read filtered to this test's own
  rows) plus the two shapes a generated id cannot fix — a ceiling asserted over a
  whole table, and a write into a fixed configured row, whose repair is a
  **fixture-less `[CollectionDefinition]`** that serializes only those classes.
- **`ef-core-data-access`, Migrations workflow** — a committed migration is never
  deleted, renamed or rewritten, and **restoring the file is not the repair**: a
  database whose `__EFMigrationsHistory` already names that id skips it, so the
  object stays missing there. Redeclare in the entity configuration and generate
  a new migration that tolerates both populations.
- **`dotnet-code-review` check 1.11**, *A committed migration deleted or renamed*
  — CRITICAL. A deletion in the file list is enough for a diff (the file existed
  at the base); the sweep form is `git log --all --diff-filter=D --name-only --
  "*Migrations/*"` with `git merge-base --is-ancestor`. The finding must state
  **what the deleted migration contained**, and recommend the forward repair.
- **`claude-md-builder` R33** — never edit source through the Windows shell.
  PowerShell 5.1 reads a BOM-less file as Windows-1252 and writes UTF-8,
  re-encoding every non-ASCII byte in the file; in the field a six-file
  `Set-Content` pass turned one test red **deterministically, 4 runs of 4**, which
  reads exactly like a regression. Ships with the tell (`git diff --stat` far
  larger than the edit) and the lossless reversal. Exposure is near-total in
  repositories whose comments are not in English.
- **`dotnet-integration-tester`** — its parallelism ban no longer asserts that the
  collection fixture serializes; it says the suite's own fixture and isolation
  strategy decide, and the flag is still not the tester's to set. One
  rationalization row added: a run contradicting an earlier one is evidence, and
  the cross-run comparison belongs to the flow.

**Verified, not recalled.** `Xunit.AssemblyFixtureAttribute(Type)` was checked
against `xunit.v3.core` 3.2.2's own metadata and XML docs in the local NuGet
cache: the instance is created before any test in the assembly runs,
`IAsyncLifetime.InitializeAsync` is awaited on it, it needs a **public
parameterless constructor**, and a test reaches it by declaring a constructor
parameter of exactly the fixture type. There is **no `IAssemblyFixture<T>`
interface** — the report's one inaccuracy, and the reason no interface appears in
the shipped shape. v2 has no equivalent at all, which is why every fixture sample
copied from a v2 codebase stops at the collection fixture.

**Standing lesson.** Two releases running, the lesson was *the worked example is
the doctrine*. This one is its sibling: **describing one correct configuration
without its option space is an instruction to clone it.** The four fixtures, the
four containers and the serialization nobody wanted were all written by someone
following the documentation exactly.

---

## [0.3.68] — test-report-nudge: one file per run, Vietnamese ĐẠT/HỎNG, 2026-08-04

**User-directed change to the report-rule wording (report-rule changes need
approval — shown before shipping).** The rule the hook nudges the model
toward was replaced outright: a single `test-report.md` at the repository
root, overwritten on every settled run, threw away the progression between
runs — the part the user actually wanted kept.

**What changed in `hooks/test-report-nudge`:**

- Report path is now `docs/test-report/YYYY-MM-DD-test-<what-was-tested>.md`
  — a new file per settled run, never an overwrite. The model chooses the
  `<what-was-tested>` slug from the run's scope.
- Report body is fixed Vietnamese prose — command plus pass/fail/skip
  totals, one section per test class, one plain line per test case, marked
  **ĐẠT** or **HỎNG** (each HỎNG with one short reason) — replacing "written
  in the language the user is conversing in" and the PASS/FAIL marks.
- The subagent branch (added 0.3.63) is unchanged in mechanism: a subagent
  still never writes under `docs/test-report/`, only hands ĐẠT/HỎNG lines
  back to the dispatching session — the wording was updated to match, not
  the ownership split itself.
- Nothing else in the script (gates, per-context marker, marker sweep)
  changed — this was a report-rule wording edit only, per explicit scope
  from the user.

---

## [0.3.67] — `Trim()` was in the teaching example, so it was in the output, 2026-08-04

**Field report: generated code calls `Trim()` everywhere.** The source is this
plugin. `module-feature`'s one worked service — the example every agent copies
when writing a create operation — opened with
`AnyAsync(x => x.Code == request.Code!.Trim(), …)`, and `references/service-growth.md`
repeated the same line. Nothing else in the plugin said a word about string
normalization, so the example *was* the doctrine.

**The corpus does not support it.** 16 `.Trim()` sites in the canonical project,
worktrees excluded. Every one outside a single module is a **parsing** call —
the `Bearer ` header slice, the order-by clause split, a comma-separated recipient
list. The module sites are all one file, and that file also shows the defect the
shape produces: the uniqueness guard compares `request.Code!.Trim()` while the
map that follows stores `request.Code`, so the check answers about a string the
row never holds — then repairs it with `request.Name = request.Name!.Trim()` a few
lines down, which is the same call at a second site, not a fix.

**What changed:**

- **`module-feature`** — `.Trim()` is gone from both worked examples, and *The
  service file* gains the rule: a service never normalizes an input string; no
  `Trim()`, `ToLower()` or `Replace` on a request property. Whitespace that must
  not arrive is a validator rule (`.NotWhiteSpace()`, `.NotEmpty()`) with a
  `Messages<T>` message. `Trim()` is a parsing call and belongs next to the split
  or slice that produced the string.
- **`dotnet-code-review` check 5.23**, *A string normalized at a call site* —
  MEDIUM, HIGH where a guard and the write it protects normalize differently. The
  `Find:` grep drops parsing hits first, then reports which of the two harms
  applies: the comparison disagreeing with the write, or nothing declaring the
  rule.
- **`dotnet-performance-review` check 1.12**, *A predicate that calls a function
  on the column* — the other half. `x.Name.ToLower() == …` transforms the stored
  value once per candidate row and the column's index stops answering; the
  parameter-side call routes to 5.23 instead. Graded against the entity's
  configuration, and noted as doubly redundant on a `citext` column.
- **`claude-md-builder` R32** — the rule enters the static catalogue, so a
  generated `CLAUDE.md` carries it. No analyzer flags this shape, which is why it
  propagates: it reads as defensive hygiene.

Both new greps were smoke-tested against the canonical project before shipping —
5.23 returns 64 solution-wide hits that the parsing filter thins to the real ones,
1.12 returns exactly the two column-side predicates.

## [0.3.66] — One graph, one `AddAsync`: the save example taught the long way round, 2026-08-04

**Field report from a consumer repository, and the skill's own entity example
convicts it.** `ef-core-data-access` showed exactly one way to write a parent
and its children in one operation: map each entity separately, set the foreign
key by hand, then call `AddAsync`/`AddRangeAsync` once per entity type. The
example was `Order` plus `OrderLine` — and thirty lines further down the same
skill declares `public ICollection<OrderLine> Lines` on `Order`, configured
through `HasOne(...).WithMany(...)`. So the single worked example was the one
case where the pattern it teaches is unnecessary, and an agent working from it
reproduced the hand-set-FK form across a real service, including at sites whose
parent already carried the navigation.

**What changed in `## Saving is the repository's job`:**

- The transaction example's second mutation is now
  `Repository<Customer>().UpdateAsync(...)` instead of
  `Repository<OrderLine>().AddRangeAsync(...)`. Two genuinely separate
  aggregates, so the block still teaches what it always taught — the wrapper's
  transaction is what makes independent saves one unit — without incidentally
  teaching the graph case wrong.
- A new **One graph, one `AddAsync`** paragraph: when the parent declares a
  navigation to the child, assign the children to it and add the *parent* once.
  `AddAsync` calls `DbContext.Add`, which cascades across every untracked
  entity reachable through navigations and fixes up each foreign key itself;
  `BaseEntity` assigns `Id` in its constructor, so the parent key exists before
  anything is saved and there is nothing to sequence. It reaches as deep as the
  graph does — a join entity holding the far side of a many-to-many travels in
  the same add.
- The selection rule, both arms named. Hand-set foreign keys plus a call per
  entity type is the fallback for when there is **no** navigation to assign:
  the child points at a row that existed before this operation, or the parent
  declares no collection for the relationship. Keys that are not part of the
  relationship — a tenant discriminator the mapping profile ignores — stay the
  caller's to set.

**Why it is worth a release rather than a note.** The hand-set form is not
wrong; the transaction still makes it atomic. It is longer than it needs to be,
and it is the form that silently drops one half of a composite foreign key.
Teaching only that form is what produced the repetition in the field.

No H4 heading was introduced — no skill in the tree uses one, so the new
material leads with a bold sentence like its neighbours.

---

## [0.3.65] — Codex has hooks and subagents after all; 0.3.64 said otherwise, 2026-08-02

**0.3.64 claimed the three lost components had no Codex equivalent. That was
wrong, and the correction is this release.** The evidence for the claim was the
plugin manifest validator, which does reject `hooks`, `commands` and `agents` —
but a rejected *manifest field* is not an absent *feature*. `codex features list`
on the CLI in hand (0.144.1) settles it: `hooks` **stable/true**,
`multi_agent` **stable/true**, `plugin_hooks` **removed/false**. Only the
plugin-bundled delivery is gone. Codex reads each component from outside the
plugin:

| Component | Where Codex reads it |
|---|---|
| Hooks | `~/.codex/hooks.json` or `<repo>/.codex/hooks.json` — same event names, same matchers, same stdin payload, same `hookSpecificOutput.additionalContext` reply |
| Agents | `~/.codex/agents/*.toml` or `<repo>/.codex/agents/*.toml` |
| Commands | `~/.codex/prompts/*.md` — user scope only, top level only |

**So `codex/` ships the kit**, and `agents/*.md` and `commands/*.md` stay the
single source of truth: `codex/sync-from-plugin.py` projects them into the two
shapes Codex reads and `--check` fails on drift, because a silently stale
projection simply teaches an older rule. `codex/install.sh` merges the hooks
into an existing `hooks.json` (identifying its own entries by command path, not
by an ownership key — Codex's hook schema is fixed and an unknown field risks the
whole file), installs the agents and prompts, and uninstalls exactly what it
added.

**The Windows fact that cost the first attempt, and would have shipped as a
silent no-op.** Codex launches a hook command **without a shell**, so a bare
`.cmd` path is not executable and every hook never ran — with nothing reporting
it. Every entry now carries a `commandWindows` override through `cmd /c`. Found
by planting a canary hook that wrote a file: the canary fired and ours did not,
which is the difference between "Codex does not run hooks" and "Codex could not
run *this* command".

**Measured, not assumed** (Codex CLI 0.144.1, 2026-08-02): a `codex exec` turn in
a `.csproj` directory created this plugin's own session marker and the router
text reached the model — found in the session rollout. All 26 skills are visible
to the model as `dotnet-standards:<skill>` with their descriptions
(`codex debug prompt-input`). **Not verified: the six agents.** They install and
their TOML parses, but `codex exec` exposes no spawn tool at all, and upstream
reports custom agents resolving differently across Codex surfaces — so
`dotnet-review-flow` preflight #3 keeps checking the roster rather than the
install, and its sequential-lens fallback stands. `codex/README.md` records the
split between what was measured and what was not.

**Also.** `hooks/superpowers-check` read only the Claude registry, so under Codex
it would have warned that Superpowers was missing while it sat installed and
enabled in the running session; it now accepts either registry. The router's
harness section and `hooks/README.md` were rewritten from "absent" to "installed
elsewhere, and here is what decides which you have".

---

## [0.3.64] — the same plugin, installable on Codex, 2026-08-02

**The ask.** Ship this plugin so Codex can install it too, and where Codex wants
its own instructions file, point that file at the project's `CLAUDE.md` rather
than writing a second rule set.

**What Codex's plugin contract actually is** — read off the `plugin-creator`
skill and the `validate_plugin.py` that Codex installs at
`~/.codex/skills/.system/`, not inferred. A plugin is `.codex-plugin/plugin.json`
plus a marketplace descriptor at `.agents/plugins/marketplace.json`. The manifest
accepts exactly `id`, `name`, `version`, `description`, `skills`, `apps`,
`mcpServers`, `interface`, `author`, `homepage`, `repository`, `license`,
`keywords` — **`hooks`, `commands` and `agents` are rejected fields**, and
`interface` requires `displayName`, `shortDescription`, `longDescription`,
`developerName`, `category`, `capabilities` and `defaultPrompt`. The validator
also walks every `skills/*/SKILL.md` frontmatter. This repo passes it.

**So the knowledge layer ships whole and three things do not**, each compensated
where the loss lands rather than in a note nobody reads:

- **Hooks** — nothing announces the router on a Codex first prompt.
  `choosing-a-dotnet-skill` gained *When the harness is not Claude Code*, which
  states the compensations as the session's own work.
- **Commands** — `/dotnet-feature` and `/dotnet-review` were only thin entries;
  the flow skills load by name.
- **Agents** — the six specialist types are absent **together**, which is the
  signal. `dotnet-review-flow` preflight #3 now separates that harness fact from
  a stale install (six absent vs. one or two) and runs the four lenses
  sequentially, one report each, with the shared context declared in *Not run*.
  Never a merged pass — the merge is what Principle 1 forbids, and it is exactly
  the shortcut this fallback exists to refuse. `dotnet-feature-flow` inherits
  the exception through its compact restatement of the same preflight.

**The instructions file — one file, one copy.** `claude-md-builder` PHASE 7 now
writes `AGENTS.md` as a **pointer** at `CLAUDE.md` after writing `CLAUDE.md`
itself, and a new hard constraint forbids the skill from putting rules into any
other memory file. Three rulings inside that step: not a copy (a duplicated rule
set drifts, and the copy is what gets read when it does); not a symlink (Windows
checks it out as a one-line text file, which the harness then reads as the entire
instruction set — the shape Superpowers ships and the reason this repo does not
copy it); and not an overwrite of an `AGENTS.md` a human maintains with real
content — that is a contradiction to report, PHASE 1c shape. PHASE 0 gained the
matching rule: only `CLAUDE.md` decides the mode, so an `AGENTS.md`-only
repository is a **create-mode** repository whose stranded rules get folded into
the new `CLAUDE.md`. Update mode gained step 7, which repairs a missing or
rule-carrying pointer.

**Files.** New `.codex-plugin/plugin.json`, `.agents/plugins/marketplace.json`,
and this repo's own `AGENTS.md` pointer. Router, `claude-md-builder`, both flow
skills, `README.md` (Codex install section) and `hooks/README.md` updated.

---

## [0.3.63] — a hook's "once per session" was starving the session it was written for, 2026-08-02

**Found by a user question, not by review: "does the test-report hook still run
when the phases are Superpowers'?"** It does — and answering it properly exposed
that it had been running in the wrong place since 0.3.44.

**The defect.** `test-report-nudge` emitted once per session, keyed by
`session_id`. Under Superpowers' `subagent-driven-development` every implementer
is a `general-purpose` subagent that runs its own red-green loop, so **the first
`dotnet test` of a whole run reliably fires inside a throwaway subagent
context.** That context consumed the session's only emit and then vanished. Every
later run — including the coordinating session's own final full-suite run — got
nothing. The standing instruction *"for the rest of this session"* had been
handed to the one context with no rest of a session to apply it to, and the
symptom is silent: `test-report.md` simply never gets written, or gets written
once for one task and never updated.

**Verified against the CLI, not inferred.** The `PreToolUse`/`PostToolUse`
payload carries `agent_id`, documented in the 2.1.220 schema as *"present only
when the hook fires from within a subagent … absent for the main thread"*, with
the explicit instruction to use that field and not `agent_type` to tell the two
apart. Hooks do fire inside subagents; the tool matcher does not care who called.

**The fix, in two halves.** The marker is now keyed by `session_id` **plus**
`agent_id`, so a subagent and the main thread each get their own single emit and
neither starves the other. And the two contexts are told **different things**:
letting a subagent write `test-report.md` is worse than letting it write nothing,
because the file is overwritten per task and the last implementer to finish
leaves a report that names the whole run while covering one task. A subagent is
now told to put its plain-language lines in the report it hands back and leave
the file alone; the main thread owns the file and folds those lines in.

**The main-thread report rule is unchanged to the character**, as its own
approval rule requires. It gains one clause — *"Where subagents ran tests, fold
the lines they reported back into this file rather than re-deriving them"* — and
the subagent branch is new wording, both flagged to the user at ship time so
either can be reversed in one edit.

**The same defect was found in a hook shipped hours earlier.**
`process-handback` was keyed by session alone, and `dotnet-feature-flow:210`
orders **every** implementer subagent to load its skills with the Skill tool — so
an implementer told to follow TDD would load `superpowers:test-driven-development`,
match the gate, and consume the emit the coordinating session needed.
`fleet-nudge` gets the same keying for the nested-spawn case. All three hooks now
say "once per context" and mean it.

**Smoke tests: 13 new for `test-report-nudge`, 6 added to the pair from
0.3.62 (29 total), all green** — including the exact failure sequence: subagent
runs tests first, a second subagent runs tests, then the main thread, and all
three are served.

---

## [0.3.62] — the plugin was skipped twice in one consumer session, at both ends of the same feature, 2026-08-02

**The failure.** A session building an access-control module on
`feature/access-control-core` wrote a MediatR architecture specification with
**no knowledge skill loaded** — wrong handler placement, wrong `AddMediatR`
anchor, a `Contracts/` folder outside the house vocabulary — and then ran **more
than twenty subagent review rounds, the final whole-branch review among them,
without loading one of the five review skills or spawning one of the six
specialist agents.** Every round was `general-purpose` carrying a constraint
block the session wrote by hand. The performance lens was therefore never
applied at all; architecture and security ran on improvised criteria. Field
report, written by the session that made both mistakes:
`docs/field-reports/2026-08-02-skill-routing-failure.md`.

**Verified against Superpowers 6.2.0 before anything was designed** —
`brainstorming:132` states *"Do NOT invoke any other skill"* with no qualifier
(`:13` and `:61` scope the same ban to *implementation* skills); all three
`subagent-driven-development` templates hard-code `Subagent (general-purpose)`;
its final reviewer is hard-coded to `requesting-code-review/code-reviewer.md`;
its rubric slot is `[GLOBAL_CONSTRAINTS]`, *"copied verbatim"* by hand; and
`grep -ri 'dotnet|domain skill|domain plugin|other plugin'` over
`subagent-driven-development/` and `writing-plans/` returns **zero hits**.

**Two findings the field report did not reach, and they reshaped the remedy.**
(1) Its Superpowers-side fixes cannot be executed: no Superpowers file may be
modified, and a marketplace update would erase a local patch anyway — so a
design resting on one silently expires. (2) It proposed re-firing `router-nudge`
when a prompt mentions review; **that cannot work**, because the twenty review
rounds ran inside one autonomous `subagent-driven-development` turn. The
write→review transition was decided by the model, not typed by the user, so no
`UserPromptSubmit` ever fired. Our only injection channel was structurally deaf
to the exact moment it was needed.

**Nothing about this plugin's content failed.** `dotnet-feature-flow:200–216`
already forces every task prompt to name the owning knowledge skills *and order
the Skill-tool load*; the router's planning section already governs spec, plan
and subagent-prompt writing (moved above the tables at 0.3.59); the flow that
owned the entire task sat in the session's skill list all day and was never
opened. **The failure is entry and hand-back, not doctrine** — so this release
changes no rubric, no convention, and adds no skill.

**The legitimacy chain this release rests on.** `using-superpowers` states its
own precedence: *"User instructions (CLAUDE.md …) take precedence over skills."*
A rule in the consumer repository's `CLAUDE.md` therefore outranks a process
skill that is already holding the wheel — and `claude-md-builder` already owns
that file's generation.

**Layer 1 — four self-gating static rules, `claude-md-builder`.** New
*Process ownership* group (R28–R31), self-gating exactly like R23: R28 the two
flow entry points and *do not assemble that sequence by hand*; R29 review and
test subagents are `dotnet-review-flow`'s, never `general-purpose`, and the
criteria are the four rubrics, never a hand-written constraint block — **naming
no agent, because that flow owns the roster**; R30 a *"do not invoke any other
skill"* line bars implementation skills and does not suspend the knowledge
layer; R31 re-route on every phase change, the router is a lookup table and not
a briefing. `template.md` §8 places them as `### Process`, immediately after the
hard constraints; `checklist.md` makes them uncuttable, because their absence is
the one that cannot be noticed from inside the file. **0.3.57's update-mode
reconciliation carries all four into every `CLAUDE.md` this plugin has already
generated.**

**Layer 2 — two `PreToolUse` hooks, the first per-tool-call hooks this plugin
has admitted.** `fleet-nudge` (matcher `Task|Agent`) fires at a subagent spawn
that looks like review or test work; `process-handback` (matcher `Skill`) fires
when one of six Superpowers process skills is loaded. Both are gated to .NET
solutions and emit once per session. **The mechanism was measured, not assumed**
— read out of the CLI binary at `versions/2.1.220`: `PreToolUse` carries
`additionalContext` alongside `permissionDecision`, `permissionDecisionReason`
and `updatedInput`; the payload carries `tool_name` and `tool_input`; the
subagent tool answers to both `Agent` and `Task`; `Skill` is an ordinary tool
name. **Gate order is the reverse of `router-nudge`'s** and deliberately so:
these fire per call, so the session marker is checked first and the .NET verdict
is memoised on the first invocation — every later call is one `test -e`.
23 synthetic-payload smoke tests pass before ship, covering both directions of
every gate plus missing `session_id`, unwritable `TMPDIR` and empty stdin.

**Two refusals recorded so they are not reintroduced as conveniences.**
`updatedInput` would let a hook rewrite `subagent_type` — refused: it makes the
transcript disagree with what was spawned, and it inverts this repository's
*under-fire, never over-fire* hook doctrine. `permissionDecision: ask|deny`
would make a nudge into a gate — deferred behind a measurement, with the
escalation ladder written down.

**Layer 3 — text.** `dotnet-feature-flow`'s description now says what it
*replaces* (hand-assembling brainstorming + plan + subagent-driven development)
and carries the phrasings people type — *"execute this plan"*, *"implement the
plan with subagents"*; `dotnet-review-flow`'s adds *"final review before merge"*
and *"review each task as it lands"*; both stay under 100 words (97 and 90).
`choosing-a-dotnet-skill` gains *Composing with Superpowers process skills* and
a *spawning a subagent* row.

**A written doctrine was amended, not worked around.** `router-nudge`'s header
said *IT NAMES THE ROUTER AND NOTHING ELSE*. That holds for **table rows** and
still does — the emit names none. It does not hold for the choice the tables
cannot express: a row routes a question to a skill, while both incidents were
failures to choose a **process for the whole session**. The emit now names
`/dotnet-feature` and `/dotnet-review`; the amendment and its reason are
recorded in the script header and in `hooks/README.md`, per the precedent set at
0.3.27.

**Known limit, stated rather than buried:** `dotnet-feature-flow` has still
never been run end to end in the field — the only trial was `/dotnet-review` on
one commit. This release routes more traffic at it. The trial is parked on the
board, and it is the only thing that settles whether any of this worked.

Design: `docs/superpowers/specs/2026-08-02-process-handback-design.md` ·
plan: `docs/superpowers/plans/2026-08-02-process-handback.md`.

---

## [0.3.61] — `message-keys` taught the wrong form, and it had spread to four skills, 2026-08-02

**The worst defect this field exercise has found, and it was caught only because
it made *me* break a working skill.** Reviewing a consumer module, I read
`message-keys` and "corrected" `module-feature`'s validator examples from
`Messages<Order>.Required(x => x.Name)` to `Messages<OrderRequest>...`. The user
stopped it: the entity-typed form is the house rule, and I had just rewritten
correct doctrine into incorrect doctrine by trusting a skill.

**The false premise.** `message-keys` principle 4 argued *"a property selector
can only compile against the type being validated, so a validator message must
be typed to the request."* That is simply not true — the selector is an
expression over `Messages<T>`'s own `T`, chosen at the call site, and
`WithMessage` receives a finished string. `Messages<Order>.Required(x => x.Code)`
inside `OrderRequestValidator` compiles and is what the corpus does
(`Messages<UserRefreshToken>.Invalid(x => x.Token)` in a request validator).

**What `[MessageDisplay]` actually does**, settled by reading the implementation
rather than the skill: `Messages<T>.GetMessageBase()` reads the attribute off
`typeof(T)` and falls back to `type.Name`. With `T` an entity the fallback *is*
the intended path. The attribute earns its place in exactly one situation — a
**Facades-tier request with no entity behind it**, where `T` must be the request
(`MediaUploadRequest` + `[MessageDisplay(nameof(Media))]`, the only such call
site in the consumer repo). On the twelve module requests that carry it, nothing
ever reads it. `message-keys` had generalised that one exception into the
universal rule.

**Blast radius — the wrong form had propagated into four skills:**

- `message-keys` — principles 3, 4 and 5, the *Which form where* table, the
  Patterns block, the "superseded" paragraph (now an inline correction notice),
  the string-overload example, and the `[MessageDisplay]` anti-pattern.
- `dotnet-code-review` — **check 5.5 was inverted**: titled *"A validator message
  typed to the entity — MEDIUM"*, it would have raised a finding against
  conforming code. Retitled, rewritten, and carrying a note that reports citing
  the old wording are void.
- `dotnet-testing` — three assertion examples (SKILL.md, `unit-testing.md`,
  `integration-testing.md`).
- `api-surface` — the base-request bullet claiming `[MessageDisplay]` "renames the
  key prefix for every derived request".

**Kept from the aborted edit, because it was right and the user confirmed it:** a
rule whose check is the existence of a *different* entity speaks as that entity
and takes **no selector** — `Messages<Category>.NotFound()`, never
`Messages<Order>.NotFound(x => x.CategoryId)`. Stated now in `module-feature`
SKILL.md and `references/validation-rules.md`, and the examples in both follow it.

**Also fixed — the nullable law was unreachable.** `api-surface`'s body never
carried it; it lived only in `references/request-response-dtos.md`. Real
consequence in real code: `EvaluateAccessRequest.OccurredAt` shipped as a
non-nullable `DateTimeOffset` with no validator rule, so an omitted value binds
to `0001-01-01` and is written to the decision row as the edge's clock. The law
now sits in the SKILL body where the reader actually is. Same shape as 0.3.59
fix #3: a rule placed where nobody reaches it is not shipped.

**Process note.** R7 exists for this. I verified the *examples* against the
corpus but took the *rule* from a sibling skill on faith, and the sibling was
wrong. Reading `Messages.cs` — thirty seconds — would have settled it before the
first edit.

---

## [0.3.60] — a shipped contradiction between two skills, 2026-07-31

Found the same day and the same way as 0.3.59: a consumer session rewriting its
spec against the skills hit two shipped rules that cannot both be satisfied, and
had to adjudicate the conflict itself in a *Xung đột giữa hai skill* section.
Having to do that is the defect.

- `ef-core-data-access:222` — *"One entity lives in one file, together with its
  `IEntityTypeConfiguration<T>` **and any enums it owns**"*, and its example
  declared `public enum FulfilmentStatus` inside the `Order` file.
- `facade-module-architecture:215` — *"**Every enum the capability owns lives in
  `Enums/`** — never declared inside an entity, response or service file."*

**Resolved toward `facade-module-architecture`**, which owns placement — the
same reading the consumer session reached unaided, and the one the router's base
map assigns. `ef-core-data-access` no longer claims the enum, states the rule and
its owner explicitly, and its example carries an `// Enums/FulfilmentStatus.cs`
comment marking the enum as a separate file shown inline only so the
`HasDefaultValue` line reads.

**Second instance of the 0.3.59 family, and the reason to look for more.** In
both cases the skill's *prose* deferred correctly or said nothing, while its
*example* demonstrated the opposite of another skill's rule — and the example is
what gets copied. 0.3.59 fixed it for MediatR envelope accessibility across
three skills; this fixes it for enum placement. A sweep for further
example-versus-rule conflicts across skill pairs is worth a session.

---

## [0.3.59] — three activation defects found by a field failure, 2026-07-31

Field evidence, not prediction. A session in a consumer .NET repository designed
a MediatR surface for a reusable access-control core and produced a spec with
five convention violations, having loaded neither owning skill. The session's own
account of *why* is the useful part, and all three of its causes turned out to be
plugin defects rather than model error. The fourth cause it identified — no skill
owns a module whose public surface **is** its MediatR commands — is a genuine
gap and is **not** fixed here; it is in the PENDING log awaiting a user ruling.

### `claude-md-builder` — R27: an absence is not an exemption

The generated *Where this repository differs from what a skill assumes* section
listed `mediatr-messaging — neither MediatR nor a ConcurrencyHandler exists in
this solution; services are called directly`. The session read that as *this
skill does not apply here* and designed from memory. The bullet was accurate;
what it lacked was any statement that the skill still governs — and it is read
most often by exactly the session that is **introducing** the capability, which
is when the skill binds hardest.

- **R27 added to `references/static-rules.md`**, in *Architecture and placement*
  beside R9. It is not a `Rules` bullet: it is the standing **preamble of
  section 6b**. Placing it in the catalogue is what makes 0.3.57's update-mode
  reconciliation carry it into `CLAUDE.md` files that already exist — a
  template-only change would have reached new files only.
- **`references/template.md` §6b** now requires the preamble verbatim, and
  requires every capability-absent bullet to carry its own *load it first*
  clause. Prefer *not yet* over *does not exist*: the second reads as permanent.
- **`references/checklist.md`** — both are `Never cut` (they read as rationale,
  so cut-items 6 and 10 reach for them first), plus a final-gate checkbox.
- **`SKILL.md` PHASE 1c** — the finding is recorded as absence *plus*
  instruction, not absence alone.

### `mediatr-messaging` — the examples contradicted the owning skill

All six envelope declarations in the skill shipped as `public record`, while
`module-feature/references/mediatr-envelopes.md` rules every envelope `internal
sealed` — and explains that `internal` is the *enforcement mechanism* for
"controllers call services", because the HTTP project is a separate assembly.
The skill did disclaim ownership of envelope shape in prose, but **a disclaimer
naming no rule loses to an example demonstrating the opposite**. 0.3.31
normalized these examples to `public` on the grounds of that disclaimer; that
was the wrong direction and is reversed here.

- Six envelopes → `internal sealed record`.
- `ProcessEntityBatchHandler<TData>` → `internal sealed` with its envelope. Not
  cosmetic: a `public` class cannot implement `IRequestHandler<T>` where `T` is
  internal (CS0061), so the two only move together.
- The bare `.Send(...)` snippet — which read as controller code — now names its
  holder (*service, facade or worker; never a controller*) and states why the
  controller cannot reach it, matching the `Publish` snippet above it.
- Core Principle 4's deferral now carries the rule it defers to, so a reader who
  never opens `module-feature` still knows the answer.

### `choosing-a-dotnet-skill` — the router told the reader to stop too early

*"If exactly one base-map row fits, load it and stop"* sat 100 lines above *When
the work is being planned, not yet written* — the section that governs
spec-writing. The failing session did not skip that section; it obeyed the stop
instruction. A rule placed after the instruction to stop reading is not shipped.

- The planning section **moved above the tables**, with a pointer to it in the
  opening lines and an explicit carve-out in the one-row rule.
- Two additions: a plan touching four areas loads four skills (the one-row rule
  governs a question in front of you, never a document that decides many), and
  **a capability the repository does not have yet is the strongest reason to
  load its skill** — the router-side statement of R27, so the two reinforce
  rather than depend on each other.

Solo session, no three-way loop: every change is a correction traceable to
shipped text plus field evidence, not new doctrine. R7/R8 carve-outs untouched.

---

## [0.3.58] — the five new skills become reachable from inside, and visible to review, 2026-07-31

**A measured gap, raised by the user: does anything but a description trigger
these skills?** Measurement, after the batch shipped: router base-map rows
present for all five; reciprocal `Not for:` entries present — but those live in
descriptions; **body pointers from the 21 pre-existing skills: 1 of 5**; **rubric
citations: 0 of 5**; **agent mentions: 0 of 5**. A description fires at
skill-selection time only, so a session already inside `module-feature` or
`api-surface` was never told these skills existed — and the review layer, which
loads the knowledge skills its rubric cites (0.3.32), could not see any of them,
including the 32 anti-examples shipped at 0.3.54.

Two coordinators, two three-way loops, disjoint scopes.

**Package A — body pointers: 1 of 5 → 4 of 5.** Five pointers, **eleven
candidate sites rejected**, under the repo's standing discipline that *a pointer
earns its place only when it restates a boundary a shipped `Not for:` itself
draws*. `api-surface` → `list-query-pipeline` (the contract is ours, the binder
and operator set are theirs); `ef-core-data-access` `query-conventions.md` →
`list-query-pipeline` (call sites ours, extensions theirs); `module-feature`
`validation-rules.md` → `common-extensions` (the reuse→promote→inline ladder and
the canonical `ValidatorExtension` to recreate); `automapper-mapping` →
`file-storage` (the storage-key wrapper in `MapFrom`, closing a reciprocal banked
at 0.3.50); and one site nobody proposed — `dotnet-testing`
`references/unit-testing.md` → `http-client-factory`, for substituting the sender
in a double, carrying the corpus-verified fact that the sender returns a `500`
rather than throwing, which is what a faithful double must reproduce.
**`excel-miniexcel` stays at zero pointers by ruling:** no pre-existing body sits
on its path, and adding one would be coverage for its own sake.

**Package B — the review layer.** Existing checks now cite the new owners, and
three checks were added where a shipped anti-example had no reviewer at all:
code-review **3.11** (a task started and never awaited), **5.14** (a suspicious
range in a regex character class), **7.2** (a helper written where one already
exists). The four reviewer agents' load instructions now say that **any** skill a
check cites is loaded before a finding citing it is written — **and equally
before a citation is relied on to suppress one**, closing the direction nobody
audits: doctrine graded from memory to justify silence is the same defect as
doctrine graded from memory to justify a finding.

**Every new `Find:` grep was smoke-tested against a real project first** (the
0.3.56 lesson, now standing practice). 3.11's returned two hits, both true
positives — one of them the exact shape of `file-storage`'s anti-pattern 9,
live. **A fourth candidate check was dropped rather than shipped**: an
undisposed `CreateScope()` grep returned seven hits of which four needed
case-by-case reading, and a check whose hits are judgement calls trains
reviewers to skim.

## [0.3.57] — update mode reconciles against the rule catalogue, 2026-07-31

**Field report, and the defect is the skill's, not the session's.** A
`claude-md-builder` update run on a consumer repository added exactly one
factual line and no rules — while three static rules had shipped in the same
day's releases (R16's house-pattern exception, R25 lookup-first over
`Common/Extensions`, R26 timestamps-are-UTC) and all three applied to that
project. Inspection of the file confirmed it: the old R16 wording present, R25
and R26 absent.

**Cause: update mode had six steps and none of them re-opened the catalogue.**
It diffed the file against the *project* — stale facts, analyzer duplicates,
provisional content — and never against the *rules*. So a `CLAUDE.md` was frozen
against whatever the catalogue held on the day it was written, and every rule
shipped afterwards was invisible to it, permanently.

**Added: update-mode step 3, "Reconcile against the current rule catalogue"**
(the old steps 3–6 renumber to 4–7). It re-runs PHASE 3 selection against
`references/static-rules.md` as it stands now and diffs **both** directions: a
rule whose `Applies when` the scan satisfies but which the file lacks — offered,
never asserted, because **absence is not rejection** and a rule the user rejected
before must not return wearing a new hat; and a rule the file carries whose
canonical text has since changed — both wordings shown, the user chooses,
because a silently stale rule reads as current and is enforced as written. The
file's line budget still binds. PHASE 3 gained the reciprocal sentence: the
catalogue moves, which is why update mode cannot trust an existing file.

**Known seam:** generated files carry no provenance marker naming the catalogue
version they were built against, so reconciliation is a full re-selection and a
text diff rather than a version comparison. Adding such a marker would put a
line in the output that is neither project-specific nor an approved static rule,
which the skill's own hard constraints forbid — so the cheap fix is deliberately
not taken.

## [0.3.56] — rubric checks 5.21 and 5.22 close the two orphans, 2026-07-31

The two rules shipped at 0.3.55 had no reviewer. Both now do, in
`dotnet-code-review/references/review-rubric.md`, area 5.

**5.21 A request property that cannot express "not sent"** — *MEDIUM*, raised
to HIGH for a foreign key, a money or quantity amount, or a state flag. A
non-nullable value type on a request breaks the convention in both directions
at once: it is filled with its own default before any rule runs, so
`Guid.Empty`, `0` or `false` arrives indistinguishable from a value the caller
chose — and a caller who genuinely means `false` has no way to say so. Cites
`api-surface` *The request chain* and `module-feature` *Every required property
is asserted, never assumed*.

**5.22 A timestamp converted by hand** — *MEDIUM*, INFO when merely redundant.
Each surviving hit is one of two findings: a `DateTimeOffset` whose conversion
the model already performs, written a second time; or a `DateTime`, which no
convention converts and whose type is the fix.

**Both `Find:` greps were smoke-tested against a real project before shipping,
and both were wrong on the first pass.** 5.21's matched `public interface I…`
on the `int` inside `interface` — the check now carries the `` and says why
to keep it. 5.22's flagged a JSON converter and the `DbContext`'s own value
converter, i.e. the two places the conversion is supposed to live; the check now
filters converters first and says that reporting them costs the check its
credibility. The original text asserted every hit "is one of two findings and
never a pass" — that absolutism was falsified by the first project it met.

**Also fixed:** the `### Check coverage` example in `dotnet-code-review/SKILL.md`
still read `5: 5.1-5.19` after 5.20 shipped; it now reads `5.1-5.22`.

## [0.3.55] — two user rules: nullable requests with `NotEmpty()`, and UTC without help, 2026-07-31

**Rule 1 — every request property is nullable, every required one carries
`NotEmpty()`.** Stated by the user as law. The reason is the model binder: a
non-nullable `Guid`, `int`, `bool`, `DateTimeOffset` or enum on a request
arrives already filled with a default the binder invented, indistinguishable
from a value the caller sent, and nothing downstream can tell the difference.
Nullability is what makes *absent* distinguishable from *sent*; optionality is
the validator's statement, not the property's. Corpus-confirmed on strings
(`public string? Code` + `NotEmpty().WithMessage(...)`), and the corpus also
carries the violations the rule exists to kill — required foreign keys and
counts declared as non-nullable value types.

**Added, both halves:** `api-surface/references/request-response-dtos.md` (*The
request chain*) carries the DTO side with the binder trap spelled out;
`module-feature/references/validation-rules.md` gains a section, a TOC entry and
a checklist line for the validator side — including the consequence that a rule
following `NotEmpty()` may dereference with `!`, and that an optional property
is the one with **no** `NotEmpty()`.

**Rule 2 — `claude-md-builder` R26: timestamps are UTC without your help.** The
`DbContext` converts every `DateTimeOffset` in and out, so `ToUniversalTime()`
or `ToLocalTime()` on a value going to or coming from the repository is a
double conversion. **Written narrower than dictated, deliberately:** the user
said "every DateTime/DateTimeOffset", but the corpus convention is
`Properties<DateTimeOffset>().HaveConversion<...>()` — `DateTime` is *not*
covered by it. Telling a session that `DateTime` is handled automatically would
be false, so the rule says to use `DateTimeOffset` and names a `DateTime`
property as the thing to change.

**Known seam:** no review rubric checks either rule. Following the precedent set
for the `Guid.NewGuid()` sequential-key rule, this is logged as a verified
orphan rather than fixed by an ad-hoc check in the wrong format.

## [0.3.54] — the R8 labelling pass: 32 anti-examples across six skills, 2026-07-31

**A user-run labelling pass, then a delegated one.** The batch's six coordinator
reports had banked **63 verified anti-example candidates** — real defects found
in the corpus while extracting canon — none labelled, because R8 reserves
labelling to the user. The user ruled on groups 1–2 candidate by candidate, then
delegated the rest ("từ nay cứ theo bạn đề xuất đi"), waiving the carve-out for
this pass only. Every decision is recorded with its reason in
`docs/ext-batch-2026-07-31/r8-decisions.md`, one row per candidate, so any label
can be vetoed independently.

**Verdict: 32 LABEL, 31 BỎ.** Three exclusion rules, generalized from the user's
own group-1 calls, account for most of the drops: a misspelling is never an
anti-example; a variant that merely lost a canonical pick is never one; and
dead-but-harmless code is not one either. Two more were dropped on merit — the
`Any()` probes (shipped canon and `dotnet-performance-review`'s cost model both
rest on them; labelling would force a two-skill change) and `Service<T>()`
(already taught as anti-pattern 4). One was dropped for lack of evidence: a
reported `Current`-under-`PageSize` violation could not be reproduced.

**Implementation:** six three-way loops, one per owning skill, run by delegated
headless coordinators. **All 32 labels re-verified against the corpus and
shipped; none dropped at implementation.** One was re-framed weaker than the
decision table implied and that is the pass's most valuable outcome: the
`TrimEnd(character-set)` defect was traced through all ten operator templates —
every composed predicate ends in `)`, which is in neither trim set, so nothing
is corrupted today. It ships as a **latent** defect with the mechanism, never as
a live failure.

**Where they live:** `ef-core-data-access` (3, woven into `## Soft delete` +
`references/soft-deletes.md`); the other five skills were at or near the 500-line
bar, so their sets ship in a new `references/anti-patterns.md` each —
`list-query-pipeline` (6), `common-extensions` (9), `excel-miniexcel` (5),
`file-storage` (5), `http-client-factory` (4) — with a short pointer in the
SKILL.md's existing Anti-patterns section. No description changed, so the router
needed no edit. No existing anti-pattern was renumbered.

**Coordinator catches worth keeping:** both `http-client-factory` authors
converged on a *broken* remedy for the builder-state label — passing an empty
header dictionary — which the shipped `WithHeaders` ignores (it assigns pairs,
never clears), so the fix fixed nothing; cut in all three places, replaced with
"only a fresh instance is clean." `list-query-pipeline` proved its dead paged
`Data` member is `internal set` and never assigned in any lineage, and that the
`GetType().IsGenericType` guard is wrong in all six lineages while the very next
branch spells the same test correctly.

**Preserved:** the decision table, the implementation brief, and all six R8
reports under `docs/ext-batch-2026-07-31/`.

## [0.3.53] — the soft-delete section stops overstating `HasQueryFilter`, 2026-07-31

**Caught while pulling evidence for the R8 labelling pass, one release after
0.3.47 shipped it.** The new `## Soft delete` section claimed *"No
`HasQueryFilter` is registered anywhere in this standard"*. A full corpus
census falsifies it: two entities register one (staged-import exclusion), just
never for a soft-delete stamp. Both authors and the arbiter missed it because
the survey grepped the SoftDeletes folders and the repository, not the whole
corpus — a shared blind spot the loop's own shared-claim rule exists to catch.

**Changed:** the claim is narrowed to what is true (no *stamp* is registered
that way; `HasQueryFilter` exists for a different job), and the
documentation-derived block now tells the reader to check whether the entity
registers one at all — where none is registered `IgnoreQueryFilters()` is dead
weight, where one is it clears *that*, which is a different intention than
reading past a soft delete. The R8 candidate that exposed this (a live no-op
`IgnoreQueryFilters()` call in a project with zero `HasQueryFilter`
registrations) is unaffected and still banked.

## [0.3.52] — `common-extensions`: the lookup-first doctrine and the utility canon, 2026-07-31

**Batch deliverable 6/6 — the batch is complete.** (Three-way loop, delegated
headless coordinator; every arbiter spawn loaded `skill-creator` live. Five
shared-false author claims caught across the rounds — non-canonical wrappers,
a wrong ValidatorService census, R7 member-averaging, a false leak census
(round 3a NEITHER — the corrected four-shape census shipped), and the
password-shuffle security rationale, refuted against the code.)

New skill `common-extensions`: SKILL.md 480 lines + 9 references/ files
(1,868 lines of recreate-ready canon: regex, expression, serializer, random,
password, action-context, validation, validator-extension, validator-service).

- **The doctrine half:** lookup-first over `Infrastructure/Facades/Common/`
  (Extensions first) — reuse → promote-to-extension → inline only the
  genuinely one-off; recreate a missing house extension from canon; attributes
  centralized. The regex law ruled absolute within shipped canon — the
  validator references hoist inline literals into `RegexExtension` fields as a
  second visibly-marked corrected-canon fix (banked for user review).
- **Corrected canon shipped under the pre-authorization:** the
  ValidatorService scope-disposal synthesis (the non-disposing `Service<T>()`
  resolution replaced by the disposing shape; the catalogue names the leaking
  variant as anti-pattern 4). The arbiter overruled the coordinator once —
  the misspelled exception message stays verbatim under one coherent rule
  (file/type name corrected, nothing inside the type) — accepted as
  better-reasoned.
- **Reconciliations executed per the ownership map:** `PropertyInfoExtension`
  catalogue row now points at `list-query-pipeline`'s full listing (the
  dual-listing question from 0.3.51, closed); zip/import-template →
  excel-miniexcel; `RepositoryBaseExtentions` → ef-core-data-access pointer;
  Crypto/ parked; `JsonNamingPolicyExtension` silent.
- **Router:** one base-map row (helper/utility/attribute triggers + the
  Common/ address + the missing-extension reflex). **Sibling `Not for:`**:
  module-feature gains `reusable rule methods, existence-check extensions —
  common-extensions` (description re-trimmed to 98 words).
- Banked, unlabelled (R8): 16 verified anti-example candidates — the batch's
  largest bank; detail in the coordinator report.

---

## [0.3.51] — `list-query-pipeline`: the list-API extensions as recreatable source, 2026-07-31

**Batch deliverable 5/6** (three-way loop, delegated headless coordinator;
piece 1 MERGE, body MERGE, references MERGE per file, every verdict
spot-verified). New skill `list-query-pipeline`: SKILL.md 462 lines (90-word
description) + 3 references/ files — QueryExpressionExtension (454),
PaginationExtension (250), PropertyInfoExtension (195), each with a
Deviations-from-corpus table. Fires on **authorship** (writing/porting/
repairing the extensions), never on usage — usage stays with
`ef-core-data-access`/`api-surface`.

- **The `Any()` probes were KEPT** — the shipped five-trips cost model in
  `dotnet-performance-review` stays true; **no perf follow-up needed**. The
  probe-drop optimization is banked as a named follow-up with exact edit
  locations.
- Both authors' independently-added `CancellationToken` on the `ApplyQuery`
  bundle was overruled (citation verified overstated; the settled "bundle is
  the short form" identity corroborated); the shared unverified
  `new ParsingConfig()` construction replaced with corpus tokens, flag
  placement fixed.
- **Router:** one base-map row (authorship + does-not-resolve triggers); the
  `pagination` disambiguation row gains an authorship arm. **Sibling
  `Not for:`** — ef-core-data-access (`query-extension internals`) and
  api-surface (`pipeline implementation`), both descriptions re-trimmed to 99
  words by cutting nouns.
- **Incident recovered mid-run:** a concurrent lane overwrote un-namespaced
  reference drafts; the arbiter refused wrong-skill content, both drafts were
  re-emitted verbatim from transcript, and the other lane's orphans were
  safety-copied (`*-RESCUED.md` in the scratchpad).
- Vetoable ruling recorded: the `$null` `it.`-prefix drop (revert instruction
  in the report). Open at the last batch merge: the
  `common-extensions`/PropertyInfoExtension dual listing must reconcile.

---

## [0.3.50] — `file-storage`: the S3 facade, `S3FilePath`, keys, media downloads, 2026-07-31

**Batch deliverable 4/6** (three-way loop, delegated headless coordinator;
seven verdicts, all MERGE, drafts relayed verbatim). New skill `file-storage`:
SKILL.md 460 lines + 4 references/ files (implementation 860, key-generation
191, media-downloads 539, usage-patterns 315) — full sanitized implementations
with a "Normalizations at a glance" table auditing every divergence from
corpus.

- The facade file set with pre-scaffold guard, csproj-verified package
  prerequisites, and a 12-step checklist; `S3FilePath` + its `JsonConverter`
  as the mandatory response pattern (13 corpus mapping sites grounded it);
  `S3AwsExtensions` key law with the argument-order trap defended in code;
  the five-file media-download pipeline with the caller-dispose contract and
  a corrected `AddHttpClient` registration.
- **Corpus defect corrected under the pre-authorization:** the converter's
  `Read` is broken verbatim in all five projects that carry it — shipped
  fixed, marked. The arbiter also fixed two defects in its own settled
  fragments, and one finding legitimately reached back into locked text
  (`BeginTransactionAsync` — mandated by all six corpus upload+transact
  sites) and was authorized explicitly rather than silently diverged from.
- Config examples placeholder-only (every corpus `filestorage.json` holds
  real credentials). `KeysGenerationExtension` stays excluded (RSA, not S3).
- **Router:** one base-map row. The queued reciprocal obligation from 0.3.49
  is satisfied — the description's `Not for:` names `http-client-factory`.
  **Banked for owning sessions** (S17 precedent): reciprocal `Not for:`
  additions on api-surface (`file fields, pre-signed URLs, S3FilePath`) and
  automapper-mapping (`S3FilePath in MapFrom, IsSystem`).
- Ten R8 anti-example candidates banked, none labelled.

---

## [0.3.49] — `http-client-factory`: the mandatory outbound-HTTP facade, 2026-07-31

**Batch deliverable 3/6** (three-way loop, delegated headless coordinator;
arbiter-first ordering — its process lesson from the wasted first-run author
rounds). New skill `http-client-factory`: SKILL.md 491 lines + 3 references/
files (sender-and-result, content-extensions, registration-and-settings) —
full sanitized implementations for verbatim recreation.

- Canonical sender lineage: the three-project byte-identical modern variant
  with the `IFormFile` `ToFormDataContent` fix; the sync-over-async eager-read
  lineage shipped nowhere (divergent corpus code presented unlabelled — R8
  bank held).
- Doctrine grounded in numbers: zero `new HttpClient(` outside the facade
  corpus-wide; the partial-class `HttpClientSettings` mechanism (5 of 6
  projects); settings examples placeholder-only (live-looking secrets existed
  in every corpus `httpclient.json`).
- Resilience deliberately narrowed: retry exists only in one project's
  modules, never the facade — the skill teaches the boundary and refuses
  doc-recall resilience advice (the parked `http-resilience` roadmap name is
  superseded by this narrower corpus-grounded scope).
- **Router:** one base-map row + two disambiguation rows (`HTTP`
  inbound/outbound; `a file over the wire` vs `file-storage`). The proposed
  third row (retry/timeout) was declined — it would route a topic the skill
  itself declares module-owned. The arbiter-flagged reciprocal
  `Not for:` on `file-storage` ("the outbound HTTP call itself") is queued for
  that skill's own merge; the optional api-surface reciprocal is banked.
- Nothing was compiled; every code block verified by transcription diff
  against the corpus (noted honestly in the report).

---

## [0.3.48] — `excel-miniexcel`: MiniExcel both directions, 2026-07-31

**Batch deliverable 2/6** (three-way loop, delegated headless coordinator; 7
verdicts — 1×A, 6×MERGE — plus a budget pass, all coordinator-verified). New
skill `excel-miniexcel`: SKILL.md 472 lines + 4 references/ files carrying full
sanitized implementations for verbatim recreation.

- **Export:** the six-project byte-identical `ExcelExtension`, reproduced
  untouched.
- **Zip helper canonical:** the modern corpus variant made public; `Archive`
  check adopted into `IsImages` (defect fix); `SaveImage` decoupled from the
  S3 statics (temp root parameterized, corpus `FormatFileName` shape kept
  privately); cross-type `nameof` corrected; the second corpus variant
  presented neutrally (R8 label question banked, not taken).
- **Templates SHIP CORRECTED:** the three-way-verified template-name defect
  (extension passed as the name argument saves the replacement as ".xlsx",
  unfindable by the `StartsWith` lookup) fixed under the pre-authorization,
  marked visibly in body and reference; the working-directory anchor
  divergence surfaced with a marked documentation-derived note, not silently
  fixed.
- **Import:** no corpus extension exists — `references/import-service-pattern.md`
  distills the canon: direct + zip flows, optional staged/confirm lifecycle
  with session-scoped `FindStaged` (a security catch — one draft lost the
  session scoping), set-based uniqueness grounded in the staging query filter,
  per-row structural media guard, bounded upload gate canonical with the
  unbounded corpus shape recorded neutrally; row-validation rule content
  routes to `module-feature`.
- **Provenance refusals:** un-rewound-stream consequence claims, a Hangfire
  transaction-coupling claim, a MIME literal.
- **Router:** one base-map row (export/import/template/staging triggers).
  **Sibling `Not for:` additions:** api-surface (`Excel export/import
  streams`), module-feature (`Excel parsing, import flows`) — both
  descriptions re-trimmed to 99/98 words by cutting nouns, no entry dropped.
- **Banked (user's call, non-blocking):** eight unlabelled R8 candidates, the
  `ExcelExtension` rename question (R7), the `EntryExcelCastTo` no-seek
  question, the 472-vs-450 budget call — detail in the coordinator report.

---

## [0.3.47] — soft delete joins `ef-core-data-access`, 2026-07-31

**First deliverable of the common-extensions batch** (three-way loop, run by a
delegated background coordinator; first run blocked on the skill-creator roster
defect, re-run clean — arbiter loaded `skill-creator:skill-creator` live).
Verdicts: SECTION — MERGE · REFERENCES — MERGE · DESCRIPTION DELTA — B (99
words, roster untouched).

**Added:** `## Soft delete` section (SKILL.md, final section) — the two
independent stamp axes (`ISoftDelete.DeleteAt` permanent, `IHidden.HiddenAt`
reversible via the entity's fluent setter), repository-level automatic
injection into `Find`/`Count`/`CountAsync`/`Any`/`AnyAsync` (no EF
`HasQueryFilter` anywhere — "global query filter" names the composed
predicate), by-key/raw-SQL members outside the filter by construction, delete
= stamp + `UpdateAsync`, `IgnoreGlobalQueryFilter` escape-hatch semantics
(property-matched — it clears caller-written conditions on the same stamp
too), partial unique indexes via `ISoftDelete.SqlFilter`, root-set-only scope.
New `references/soft-deletes.md`: all four files verbatim + repository wiring
+ opt-in entity example + checklist. Description gains the soft-delete nouns
(99 words). Router: new `soft delete / hidden rows` disambiguation row.
Cross-reference sentence added in `### The surface` (arbiter + both authors
recommended). SKILL.md lands at 493 lines — inside the <500 bar.

**Rulings recorded:** automatic injection ratified as the canonical shape
under the standing pick-the-best delegation (the only functional wiring in the
corpus). Both authors' shared ungrounded restore claim (`DeleteAt = null`)
REFUSED per provenance law; migration/backfill guidance deliberately absent
(not corpus-demonstrable). Banked, unlabelled (R8): seven anti-example
candidates, strongest the live no-op `.IgnoreQueryFilters()` call; detail in
the coordinator report.

---

## [0.3.46] — review reports become files; the NuGet rule gains its exception; R25, 2026-07-31

Three user rulings from the same conversation, wordings shown before editing:

**Review reports are files.** `dotnet-review-flow`'s "Three rules for the
report" became four: the report is written to
`docs/code-review/<yyyy-MM-dd>-<scope-label>.md` inside the reviewed
repository (folder created if absent) — the chat copy is not the deliverable.
Each of the four review rubric skills (`dotnet-code-review`,
`dotnet-architecture-review`, `dotnet-security-review`,
`dotnet-performance-review`) carries the standalone one-sentence form under its
`## The report` heading.

**R16 exception (claude-md-builder).** The no-new-NuGet rule was over-tight:
when a house pattern or skill the project follows requires a specific library
and the project lacks it, adding that library is permitted — named with the
pattern it serves, no approval stall.

**R25 (claude-md-builder, new).** The lookup-first law over
`Infrastructure/Facades/Common/` — reuse an existing extension; promote
reusable logic to a new extension there; inline only the genuinely one-off;
recreate a missing house extension from its canonical form instead of inlining
a bespoke copy. Applies when the scan found `Infrastructure/Facades/Common/`;
noted as R24's search-first move with the address filled in. Catalogue now
holds 23 rules; no count enumeration existed to go stale.

---

## [0.3.45] — the integration tier cannot be quietly narrowed to subcutaneous, 2026-07-31

**Renumbered at merge time from 0.3.44** — a parallel session shipped
`test-report-nudge` as 0.3.44 first, and identical manifest strings merged
silently (the known same-number hazard, second occurrence). Entry otherwise
unchanged below.

**Field failure, user-reported.** A consumer-repo session built 152 unit + 14
"integration" tests where the integration tests were subcutaneous only — real
components, service-layer calls, transport skipped — after rationalizing that
the real host drags in heavy externals. The session's own security-relevant
changes (authorization attributes, merged permission handlers, response-shape
change, binding, status mapping, nested routes) were therefore never proven at
the layer where they live. The tier definition named WebApplicationFactory, but
nothing forbade counting the tier done without it.

**Added (three files, six surgical edits):**
- `dotnet-testing/SKILL.md` Core Principle 6: the integration tier is not
  satisfiable without the factory host; subcutaneous is a complement, never a
  substitute — counting one as the tier is scope-narrowing. Names the five
  pipeline-only change classes (authorization attributes/permission handlers,
  model binding, routing, exception-middleware status mapping, wire JSON shape)
  for which a transport-skipping tier is INCOMPLETE, never green; routes an
  unbootable host to the flows' `RED — environment` / *Not run* machinery. Plus
  a read-trigger line and two Decision Guide rows.
- `dotnet-testing/references/integration-testing.md`: the escape recipe as
  imperative instructions — settings to the containers, the test auth scheme,
  hosted services disabled (a new fixture bullet shows where) — try all three
  before concluding the host cannot boot; then report blocked, never narrow.
- `agents/dotnet-integration-tester.md`: the one-line mirror of the rule, and
  the rationalization table's **first observed-in-the-field row** ("the real
  host drags in heavy externals → the service-layer tests are the tier") — all
  prior rows were predicted; this one is baselined from a real run.

**Flagged, not changed:** the skill description lacks the noun "subcutaneous";
the observed failure happened with the taxonomy already loaded, so triggering
was not the defect — candidate insertion parked in the hardening report.

---

## [0.3.44] — `test-report-nudge`: a human-readable test report after `dotnet test`, 2026-07-31

**The parked `dotnet-test-report` hook (PENDING log / roadmap row added at
S15's close) shipped, redesigned per the user's 2026-07-31 direction.** The
roadmap sketch said "parses TRX/console output"; the user's actual requirement
— a persistent report **a human reads**, one plain-language line per test case
("tên user không vượt quá 200 ký tự — PASS"), short, no filler — is exactly
what a shell parser cannot produce and the model can. So the hook follows the
`router-nudge` precedent instead of the kit's `post-test-analyze.sh`: it
parses nothing and nudges the model.

**Added: `hooks/test-report-nudge`** — `PostToolUse`, matcher `Bash`. Gate 1:
`.tool_input.command` contains a `dotnet test` invocation (jq, sed fallback;
extraction failure falls back to matching the raw payload — over-firing is the
benign direction). Gate 2: once per session (session-keyed marker under
`${TMPDIR:-/tmp}/dotnet-standards/`, 7-day sweep — router-nudge's mechanism).
Emits one standing `additionalContext` instruction: whenever a test run
settles (not between red-green iterations), write `test-report.md` at the
repository root, **overwriting** the previous version — date/time, command,
pass/fail/skip totals, one section per test class, one plain-language line per
test case with PASS/FAIL and a one-line reason on FAIL, in the language the
user is conversing in.

**User approvals (2026-07-31):** the report format sample and the
overwrite-not-append behaviour were both shown and approved before the build,
per the standing report-rule ruling (2026-07-28). The wording inside the emit
is a report rule; changing it needs approval.

**R5 conflict check (Group B, all five):** (1) hook events — Superpowers
registers only `SessionStart`; no other plugin here registers `PostToolUse`
`Bash`; (2) no slash-command name; (3) no skill name; (4) instructions — does
not contradict brainstorm → plan → TDD → review: the emit explicitly defers to
the red-green loop and asks for the report only when a run settles; (5) no
agent name.

**Changed:** `hooks/hooks.json` (second `PostToolUse` entry, matcher `Bash`);
`hooks/README.md` (four hooks now; new section; the `post-test-analyze`
refusal row carries both verdicts and what changed between them — the S6
reason was never falsified, the deliverable changed); roadmap row and PENDING
log entry closed.

**Verified pre-ship:** six-payload gate matrix by hand (non-test command
silent; `dotnet test` emits; same-session repeat silent; `dotnet test`
appearing only in `tool_response.stdout` silent — the sed extraction isolates
the command field; `dotnet.exe test` emits; missing `session_id` silent), plus
an end-to-end run through `run-hook.cmd` under `cmd.exe` with both JSON
outputs parse-checked.

---

## [0.3.43] — the id-list request base is `RangeItemRequest<T>` with `Items`, 2026-07-31

**User ruling.** The canonical base for every id-list request (delete-range
above all) is the class/property naming of the newest corpus tree —
`RangeItemRequest<T>` with an `Items` collection — combined with the validator
chain the older trees carry (`NotEmpty` + `NotDuplicate` + `IsExistByIds` with
the optional ownership filter). Older variants (`Ids` property, legacy
`DeleteRangeRequest<TId>` base) are named as drift.

**Changed:** `api-surface/references/request-response-dtos.md` "Bulk requests"
— the section now carries the full recreatable base implementation
(request + `RangeItemValidator` + Guid convenience pair, property renamed
`Items` throughout) and the rule that any request whose body is a list of ids
derives from it; the module-level example is unchanged apart from the prose.
`module-feature`'s families file needed no edit — it names no property and its
hand-rolled-`List<Guid>` drift warning still stands.

---

## [0.3.42] — MediatR anchors on the root Startup, not an invented marker, 2026-07-31

**User ruling (reverses part of 0.3.16).** A consumer-repo report showed
`mediatr-messaging` teaching an invented `internal sealed class
MessagingAssemblyMarker;` as the `AddMediatR` scan anchor. The user ruled this
wrong: the architecture already prescribes a root `Infrastructure/Startup.cs`,
and the registration call lives inside it — so the anchor is
`cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly)`, the corpus form
(apsp `Infrastructure/Startup.cs:78`).

**Changed:** `mediatr-messaging/SKILL.md` "Registering handlers" — the marker
class is deleted; the example is now the Startup-anchored corpus form, marked
as living in `Infrastructure/Startup.cs`. The two corpus-verified hazards that
motivated the marker at S17 are kept, inverted into placement rules: `Startup`
is declared dozens of times across one assembly, so the anchor is only safe
written inside the root `Startup.cs` itself (self-referential, no `using` can
rebind it); and a wrong-assembly scan fails silently (zero-handler
notifications no-op).

---

## [0.3.41] — a long command is waited for, not abandoned, 2026-07-30

The feature-flow trial's fourth attempt ended mid-run, and this time the cause
was the flow's own gap rather than the host: the session issued `dotnet ef
migrations add`, found it slow on a cold tree, and **ended its turn intending to
check back** — which in a non-interactive run ends the run, with a half-written
migration in the tree.

**Added:**

- **`ef-core-data-access`, migrations workflow** — both EF CLI commands build the
  solution first, so they take an explicit generous timeout, and a run abandoned
  mid-command leaves a partial migration pair the next `add` builds on top of.
- **`dotnet-feature-flow`, PHASE 3** — a long-running command (cold build, EF
  migration, first container pull) is waited for with an explicit timeout and
  never abandoned; ending a turn mid-command ends a non-interactive run. The two
  tester agents have carried this rule since they shipped; the implement phase
  never did.

Recorded from the same trial, not a code change: **`/dotnet-feature` is a
single-feature flow** — its own description says *idea to commit in one run* —
so a trial asking one session for three features was testing a shape the flow
does not claim. Features are chained one session each, and the trial harness
now does that.

---

## [0.3.40] — the generation side gets a conformance sweep, 2026-07-30

The feature-flow trial finally ran far enough to grade: on 0.3.37 it built the
Events capability end to end, took it through a review round, applied the
confirmed fixes and reverted its own out-of-scope ones — then the print-mode
session hit its 600-second wait ceiling mid-way through review round two.

Measured against the five rule violations the 0.3.34 attempt produced, **0.3.36's
load-the-knowledge-layer fix repaired two**: enum members now start at 1, and
validator messages use `Messages<TRequest>.X(x => x.Prop)` instead of the
superseded extension form. **Three survived** — an enum declared inside its
entity file, a `Global`-prefixed validation type, and `IsRequired()` on
non-nullable properties (now joined by `HasMaxLength`). All three are decisions
taken in the first minute of creating a file, and all three sit in bodies the
task prompt named.

**Added — PHASE 3's conformance sweep.** Before the testers and reviewers are
spawned, the session runs eight commands over the files the phase created and
fixes every hit. Each command is `dotnet-code-review`'s own — cited by check
number, adding no rule here — chosen because each of these defects has now
shipped from a task whose prompt named the owning skill: they are invisible to a
passing test suite and cost a full review round each when a lens finds them
instead. A deliberate hit is stated in the report rather than left silent.

This is the generation-side counterpart of the lesson the review side has been
teaching all trial: a rule fires when it is a command, not when it is a
pointer.

---

## [0.3.39] — round-5 readout: the last check that asked for unbounded work, 2026-07-30

Round 5 (Sonnet, 0.3.38) scored **14 of 15 again, and missed the same single
criterion** — which makes it a defect, not variance. `Check coverage` reached a
report for the first time: the merged *Not run* section now names a check by
number (*code-review check 5.13 — requires a build, reported not-run rather than
clean*), which is exactly what the section was added to force.

The round was the strongest yet on defects no rule anticipates — four CRITICAL,
including two nobody had found in four previous rounds: a validator that
dereferences `x.EndAt!.Value` and so **crashes on an ordinary partial update**,
and per-request principal verification that **fails open** when the principal
type does not resolve by name.

**Fixed:**

- **5.17's `Find:` was still unbounded work in disguise.** 0.3.38 replaced
  *read each class top-to-bottom* with two line-numbered greps — but *per
  changed class file*, across a fifty-file scope, which is a hundred commands
  and so, in practice, none. It is now **one repo-wide grep** over the two
  folders where these types live, followed by a second grep on **only the
  handful of files that first grep names**, and a comparison of two integers.
  Files the first grep does not name cannot hold the defect, so the work is
  bounded by the result rather than by the scope.

The rule this trial keeps re-teaching, now in its sharpest form: a check fires
when its cost is bounded by *what it finds*, and gets skipped when its cost is
bounded by *how big the scope is* — regardless of how mechanical each individual
step looks.

---

## [0.3.38] — round-4 readout: 14 of 15, and the section that never shipped, 2026-07-30

Round 4 (Sonnet, 0.3.37) caught **fourteen of the fifteen held-back criteria**,
including all four the 0.3.37 fixes targeted, each on its first outing: the
hand-rolled regex duplicated across two modules (5.16), the redundant audit-field
`Ignore`s (5.19), the undocumented contract properties (5.18), and the responses
re-closing the generic base by hand (5.20). Both hard architecture criteria held
from round 3 and landed at HIGH with verified evidence — the two-module
controller identified by reading its route templates (*every action opens with
the parent's `eventId`*, and a full CRUD surface does not change that), and the
module folder named for a concept it does not implement, confirmed with a
solution-wide `find` for the entity that does not exist.

**Fixed:**

- **`Check coverage` never reached a report.** 0.3.37 added the section to the
  rubric's template, but `agents/dotnet-code-reviewer.md` enumerates the report
  sections in its own sentence and that list is what the agent follows — it
  still said *Summary, CRITICAL, HIGH, MEDIUM, INFO, Architecture compliance,
  Test coverage, Cleanup candidates, What's Good*. The section is now in the
  agent's enumeration too. A template and an agent that disagree resolve in
  favour of the agent, silently.
- **5.17 was the last check still asking for unbounded reading**, and it is the
  one criterion round 4 missed — after round 3 had caught it, so it is variance
  a command removes. *Read each class top-to-bottom* became two line-numbered
  greps and a comparison: `grep -n "static "` against `grep -n "public
  <TypeName>("`, and any static line number below the constructor's is the
  finding. This completes the sweep 0.3.37 started; no check in area 5 now asks
  for judgement where a command will do.

---

## [0.3.37] — round-3 readout: the effortful checks are the ones that vanish, 2026-07-30

Round 3 (Sonnet, 0.3.36) **passed the two criteria round 2 failed** — the split
test fired on a module folder whose only entity names something else, correctly
proposing `Modules/DeviceTypes/`, and the two-module controller was sent to the
right destination this time (a suffix partial of the parent's controller, not a
rename to the child). It also kept every beyond-rule catch: the FluentValidation
`.When()` composition bug, the unauthenticated controller family, the reachable
package advisory, the missing indexes.

But four criteria still missed, and **two of them are regressions** — property
XML documentation (5.18) and the hand-closed generic base were both reported in
earlier rounds and absent here. Reading the four together gives the shape: 5.16
required opening the facade's extension file first, 5.19 required opening each
source type, 5.18 said "check every public property" with no command at all, and
the base-class rule had no check number of its own. **The checks that vanish are
the ones that cost a second lookup**, and a rubric that has grown to nineteen
checks in one area gives a mid-tier model room to lose them quietly.

**Fixed — every change makes a check cheaper or its absence visible:**

- **The code rubric gains a `Check coverage` section**, the one thing its three
  sibling rubrics all had and it did not (*Audit*, *Layer* and *Area coverage*).
  One row per area, naming the check numbers actually run and any skipped with
  the reason. A silently skipped check now has to be written down or the report
  is malformed.
- **5.16 raises the finding from one grep.** A regex literal inside a module
  validator is the finding on its own — the facade lookup that reviewers kept
  skipping now names the *fix*, not the *finding*, so skipping it can no longer
  turn the check into a pass.
- **5.19 is two greps and no file-by-file reading.** Step 1 is the
  `AssertConfigurationIsValid` probe that closes the check when the
  configuration test exists; step 2 greps the `Ignore`s for member names a
  request never carries (`CreatedBy`, `UpdatedAt`, `DeletedAt`, `Id`…), so the
  certain hits come straight out of the grep.
- **5.18 gains a command** — `grep -rL "<summary>"` to list the files that lack
  it — replacing "check every public property", which is an instruction to do
  unbounded manual work and therefore an instruction to skip.
- **New check 5.20** — `grep -rn "BaseEntity<Guid>" src/`. The rule existed as
  doctrine in two knowledge skills and as no check anywhere, which is why it
  fired in two rounds and not the third. It also states outright that a
  pre-existing count sizes the fix and never withholds the finding: round 2 had
  named it and then excused it as an established convention.

---

## [0.3.36] — the generation side never loaded the knowledge layer, 2026-07-29

The second trial loop — `/dotnet-feature` building three features from scratch
on a branch where they do not exist — was killed by the host partway through
its first feature, but the Events module it had already committed is enough to
grade, and it fails on its own plugin's rules. The brainstorming was strong:
the session read the BA sheet, self-answered eleven scoped questions with
reasons, and recorded them in the design document. The code it then wrote
contains, in one small module:

- an enum declared inside the entity file (`facade-module-architecture`:
  every enum lives in `Enums/`, never inside an entity);
- enum members numbered from `0` (`ef-core-data-access`: int-backed, explicit,
  starting at 1);
- `Validations/GlobalEventValidation.cs` — the `Global`-prefixed validation
  type that `module-feature`'s `references/validation-rules.md` carries as its
  named anti-example;
- `IsRequired()` on a non-nullable string and an enum (0.3.31 doctrine, check
  1.10);
- `WithMessage(MessagesType.X)` written new, the form `message-keys` calls
  superseded and the rubric says to recognise but never write.

Each is settled in a shipped body. None of those bodies was open.

**Fixed:**

- **PHASE 3 now orders the load, not just the name.** The flow already required
  every task prompt to carry *pointers to the owning knowledge skills*; a
  pointer is a name, and a name is what an implementer recognises without ever
  opening the file. Task prompts must now carry the instruction verbatim —
  *before writing a single line of code, load `dotnet-standards:<skill>` with
  the Skill tool, and every `references/` file that skill tells you to open;
  write nothing from memory of .NET conventions* — with the failure it prevents
  spelled out, since the output of a memory-written task compiles and passes
  tests while violating house law. The requirement binds this session on the
  TDD route too: the route decides who implements, never whether the
  conventions get read.
- **PHASE 2 records the `references/` file, not only the skill.** The rules the
  Events module broke live in reference files, not in skill bodies — a step
  naming `module-feature` alone sends the implementer to the summary and past
  the file where the anti-example is actually written.

This is the same defect class 0.3.32 fixed for the four reviewers, on the half
of the plugin that writes code rather than grading it.

---

## [0.3.35] — round-2 readout: four rules that could not fire, 2026-07-29

Round 2 of the blind re-trial ran on **Sonnet** (the trial's model floor from
here on) against 0.3.34 and failed four held-back criteria. Every one traced to
a rule that was present but unfirable as written — the recurring lesson of this
trial, and the reason each fix below replaces judgement with a mechanical step.

What the round got right, for the record: the unauthenticated controller
family, a package advisory traced from an unauthenticated route to its
dynamic-LINQ sink, the FluentValidation `.When()` composition bug behind the
one red unit test, `ToLower()` defeating a citext index, a synchronous `Count()`
on every list endpoint, plus 1.10 (redundant EF configuration), 5.15 (dangling
`RuleFor`), 5.17 (member order) and 5.18 (property docs) all firing on their
first outing.

**Fixed:**

- **Check 4.10 could not match the case it was written for.** It required *a
  second aggregate entity*; the real defect was a module folder named `Devices`
  whose `Entities/` holds only `DeviceType.cs` — one entity, so nothing to
  compare. Rewritten as a three-step mechanical procedure (list the entities,
  singularise the folder name, compare) with **two** matching shapes: (a) the
  folder names a concept that has no entity, (b) two aggregates each with their
  own request/response family. The settings-shapes exception is now stated as
  *Audit coverage* material rather than a finding.
- **`api-surface`'s sub-resource rule sent the reviewer to the wrong
  destination.** "Unless it has its own module and its own full CRUD surface"
  plus "the controller stays the module's" read, on a nested CRUD surface, as
  *rename it to the child* — which is what round 2 recommended, against check
  3.5. Rewritten: the class hosting a nested route is **the owner's controller,
  the module whose resource the leading `{id:guid}` identifies**, as a suffix
  partial; a full CRUD surface does not change that; the child earns a
  top-level controller only when its **routes stop nesting**, which is a
  routing decision read off the templates, never an action count. Check 3.5
  now orders the reviewer to read the route templates before naming a
  destination, and says outright that renaming to the child is the wrong fix
  while the routes nest — plus **grade HIGH, not a naming nit**, since round 2
  filed it MEDIUM.
- **Check 5.19 graded backwards, and the security lens credited the defect.**
  The decisive fact was nowhere in the plugin: **AutoMapper copies only members
  it finds on the source**, so `Ignore` on a member the request never declared
  changes no behaviour and prevents no mass assignment. Round 2 praised those
  lines under *What's Good* as an anti-mass-assignment measure. 5.19 is now a
  three-step `Find:` whose **step 2 decides the grade** — grep the *test
  sources* for `AssertConfigurationIsValid` (with an explicit warning that a
  `bin/`/`obj/` hit is the AutoMapper DLL and proves nothing): a hit makes every
  `Ignore` load-bearing and closes the check; no hit means grade it. The same
  fact lands in `automapper-mapping` and as a *do not credit this under What's
  Good* clause in `dotnet-security-review` 6.1(b).
- **Check 5.16 never told the reviewer how to find the helpers.** "Compare each
  hit against the facade's `ValidatorExtension.cs`" assumes the reviewer goes
  looking; round 2 did not, and a live hand-rolled `.Matches(...)` duplicating a
  shipped helper went unreported. The `Find:` is now two required commands, the
  second locating the extension file by signature
  (`grep -rln "static.*IRuleBuilder" src/Infrastructure/Facades/`) with the
  instruction to read its method names **before** grading any hit.

---

## [0.3.34] — round-1 readout of the self-evaluating re-trial, 2026-07-29

The blind re-run of the field trial (round 1, against 0.3.33) was evaluated
against the held-back criteria. The run's beyond-rule catches were strong — a
FluentValidation `.When()` composition bug proven by a failing unit test, an
authorization-free controller family, verified page-size and index findings,
one PLAUSIBLE correctly demoted after tracing an index through a shared
extension. Two criteria failed deterministically and two soft spots showed;
all four are fixed here.

**Fixed:**

- **The split test had no grader.** The 0.3.31 doctrine (a concept that fails
  *exists-only-because-of-X* owns its own module) lived in
  `facade-module-architecture` with no rubric check asking the question, so
  the round-1 architecture lens never asked it. New
  `dotnet-architecture-review` check **4.10 — two capabilities in one module
  folder**: a second aggregate entity with its own request/response/validator
  family (themed subfolders are the tell) triggers the split test; settings
  shapes that pass the test are explicitly not the finding.
- **Check 5.19 grepped a path that does not exist.** `src/Modules/` — the
  canonical layout roots modules under `src/Infrastructure/`, so the redundant-
  `Ignore` sweep returned empty and read as clean. 5.16 carried the same
  defect. Both greps re-rooted to `src/`; and the code-reviewer agent gains
  the **resolve-the-roots-from-the-`.sln`** scope bullet its three siblings
  already had — the round-1 miss is exactly the failure mode that bullet
  names.
- **Check 3.5's owner was ambiguous.** Round 1 caught the two-module
  controller but leaned toward naming it for the child concept. The rule now
  states the owner outright: **the module whose resource roots the route — the
  parent** (`api-surface` wording aligned).
- **Standalone + a RED test tier shipped no review.** The flow's *Who fixes*
  said "report the failures and stop", so a standalone audit of code with one
  failing test would deliver zero lens output — round 1 only produced its
  review because the invocation overrode the flow, and said so. Standalone now
  records the RED tier and continues to REVIEW-LOOP; the after-report offer
  covers failures alongside findings. Principle 2's economy argument does not
  apply when nobody fixes until the user answers.
- **The diff file must be host-readable.** Round 1 wrote the scratch diff to a
  POSIX `/tmp` path; two reviewers could not open it and fell back to the file
  list. Diff preparation now requires a path every agent on the host can open,
  and names an unreachable diff a flow defect, not a fallback.

---

## [0.3.33] — the delegation reversal, and the trial's remaining deterministic gaps, 2026-07-29

Preparation for the self-evaluating re-run of the field trial. **The user
reversed one 0.3.31 ruling**: member order and property documentation are no
longer delegated to the analyzer — the review must name them as findings. The
other rule families (formatting, using ordering, nullable flow) stay delegated,
and the analyzer-enforcement preflight readout stays for them; the flow's
example list is corrected accordingly.

**Added:**

- **Member order is a house rule now** — fields, the constructor, then members;
  a static member may sit above the non-private methods, never above the
  constructor. Doctrine in `module-feature` (*The service file*), graded by new
  rubric check **5.17**.
- **Property documentation is a house rule now** — every public property of a
  request, response and entity carries an XML `<summary>`;
  `IncludeXmlComments` publishes it into the schema. Doctrine in `api-surface`
  (*Request DTOs*, with an entity pointer in `ef-core-data-access`), graded by
  new rubric check **5.18**.
- **Rubric check 1.10** — a configuration restating what the model or the
  validator already says (`IsRequired()` under `Nullable`, `HasMaxLength`,
  `varchar(n)`, business check constraints): the 0.3.31 doctrine now has a
  grep-able check, not only a knowledge-skill paragraph.
- **Rubric check 5.15** — a `RuleFor(x => x.P);` ending at the selector: reads
  as validated, enforces nothing.
- **Rubric check 5.16** — a hand-rolled regex or predicate the facade's
  `ValidatorExtension` already ships (the 0.3.31 helper-first doctrine's
  check), carrying the fixed warn → approve → migrate order.
- **Rubric check 5.19 and `automapper-mapping` doctrine** — an `Ignore` for a
  destination member nothing would map is noise that buries the deliberate
  ignores; with the explicit exception that where the configuration-validation
  test enforces destination coverage the `Ignore` is load-bearing — check
  before calling it redundant.

**Fixed:** the 0.3.31 insertion of check 5.14 accidentally swallowed the
`## 6. Tests` heading in the rubric — restored.

---

## [0.3.32] — cross-module ownership, and the reviewers' knowledge layer, 2026-07-29

Second batch from the same field-trial conversation as 0.3.31; both items are
user rulings.

**Added:**

- **The two-module-name rule**, extending 0.3.31's split test to the HTTP and
  service surfaces: there is no `OrderShipmentsController` and no
  `OrderShipmentService`. A route family about another module's concept — an
  order's shipments — belongs to the **owning module's controller as a suffix
  part** (`OrdersController.Shipments.cs`); its operations to that module's
  service part (`OrderService.Shipments.cs`), whose **only reach into the
  foreign module is a `Send`** of the foreign module's envelope — the foreign
  logic stays in the foreign service, exactly the shipped *Call the service,
  or send a message?* law, now with its partial-part face. Landed in
  `api-surface` (*Controller partials*: the ownership paragraph plus a fourth
  anti-pattern row), `module-feature` (*When a service outgrows one file*: a
  `<Role>` may be a foreign concept, with the Send constraint), and
  `dotnet-architecture-review` (new check **3.5**, plus 4.9's `<Name>` clause
  making `OrderShipmentService` its sibling defect).
- **The four reviewers now load the knowledge layer their rubric grades
  against.** After the rubric, each agent loads the knowledge skills its
  rubric cites most — chosen by citation count over the rubric bodies, not by
  guess: code — `ef-core-data-access`, `module-feature`, `error-handling`,
  `message-keys`; architecture — `facade-module-architecture`,
  `module-feature`, `api-surface`, `mediatr-messaging`; security —
  `auth-and-security`, `api-surface`, `error-handling`; performance —
  `ef-core-data-access`, `distributed-caching`, `distributed-lock`,
  `elasticsearch-search`. Every other skill a check cites is loaded **before a
  finding citing it is written** — house doctrine is never graded from memory,
  which generalizes the verify-against-the-body clause single checks (5.5)
  already carried. The 0.3.31 cache-read ban and stop-on-load-failure rule now
  cover every one of these loads, not the rubric alone.

---

## [0.3.31] — the first field trial's feedback lands, 2026-07-29

Every change in this version traces to one source: the first production run of
the review fleet — `/dotnet-review` at v0.3.29 (plugin commit f5c5419), run
standalone with a path scope over the consumer repository this plugin is
installed into (its commit `416e128`, .NET 8, EF Core with PostgreSQL and MySQL
migrators, 2026-07-29). Two waves (76 module files, then 10 controller files),
12 agent spawns, 24 CRITICAL/HIGH findings all verified CONFIRMED, 0 PLAUSIBLE.
The trial's feedback report catalogued 15 items; 11 shipped here directly and 4
more after the user answered the five open questions it posed. One item was
**declined on the user's answer**: XML `<summary>` on DTO/entity properties
(C4) stays delegated to the analyzer — the remedy is severity raised to
`error`, not a plugin rule.

**Fixed:**

- **E1 — all six agents commanded a Skill-tool load their `tools` list did not
  grant.** Measured in the trial: 2 of 12 spawns failed outright with
  `No such tool available: Skill`; the other 10 "succeeded" by reading rubric
  bodies straight from the plugin cache on disk — one tester self-reported it
  could not verify which of five cached versions was the enabled one, and one
  failed reviewer falsely reported the plugin absent from the machine. `"Skill"`
  is now first in every agent's `tools`, and every *First action* section gains
  a uniform paragraph — **a load failure is never worked around**: no reading
  rubrics from the plugin cache or any disk path, report the load error
  verbatim, the defect is fixed in the install or the agent definition. The
  paragraph makes explicit what "stop and say exactly that" already required.
- **E2 — `dotnet-review-flow` retried deterministic failures.** *When a
  subagent fails* now classifies before retrying: a deterministic environment
  failure (missing tool, unloadable skill, an agent that cannot start) is
  **never retried** — STOP and surface it, since a "successful" retry only
  means the agent improvised around the defect invisibly, which the trial
  observed twice; a transient failure (timeout, API error, wrong report shape)
  retries once with the identical prompt, as before. Matching Decision Guide
  row added.

**Added:**

- **`ef-core-data-access`** — three rulings under *Entities and
  configurations*: (1) length and business conditions live in the validator,
  not the schema — no `HasMaxLength`, no `HasColumnType("varchar(n)")`, no
  business check constraint; keys, FKs, uniqueness (`HasCitextUnique`) and
  defaults stay (user ruling, questions 1–2: house-wide, no exception clause);
  (2) with `Nullable` enabled, `IsRequired()` is never written for a value type
  or non-nullable reference — EF already derives it; (3) the closed-`BaseEntity`
  rule binds **every deriving type, not entities alone** — a response written
  `: BaseEntity<Guid>` is the same defect in a different layer (question 4: no
  trade-off note). `api-surface/references/request-response-dtos.md` carries
  the reciprocal sentence so the DTO layer's reviewer sees it too. This closes
  the trial's C1 gap, where a reviewer saw the generic form on a response and
  logged it under *Suppressions applied* as correct.
- **`facade-module-architecture`** — **the split test** under the Modules axis
  (question 3: a general principle, not a project opinion): *what exists only
  because of X and is only ever created because of X stays with X; anything
  else is its own module.* Also the tool a reviewer uses to answer placement
  questions instead of leaving them open — the trial's architecture reviewer
  found the right question and was correctly stopped by the no-invented-
  conventions rule; the missing piece was this rule.
- **`module-feature/references/validation-rules.md`** — new section *The
  facade's rule helpers come first*: check `ValidatorExtension` before writing
  a `.Matches(...)` regex or one-off predicate into a rule chain, and **the
  fixed order when the helper itself is wrong** — warn, fix on approval, only
  then migrate call sites (migrating first launders the defect into every
  caller). Review checklist line added.
- **`dotnet-code-review/references/review-rubric.md`** — check **5.14, a
  suspicious range in a regex character class** (`A-z`, `a-Z`): grep-able,
  HIGH, graded at the pattern's reach. The trial found a live one — a
  character-class helper admitting the six ASCII characters between `Z` and
  `a` that it was named to block — and no lens had a check that could see it.
- **`dotnet-review-flow`** — two additions. At the pre-build gate: **read the
  warning count while it is on the screen** — when analyzer warnings run to
  the hundreds and neither `TreatWarningsAsErrors` nor `.editorconfig`
  severity enforces them, the rule families the knowledge skills deliberately
  delegate to analyzers are enforced by nobody; tell the user with the count,
  recommend `error` severity one rule group at a time (question 5), and record
  it under *Not run*. At target determination: **a path scope covering one
  side of the HTTP boundary warns about the other** — the trial's first wave
  scoped modules only and missed the three severest defects, all living in
  controllers. Decision Guide rows for both.
- **`dotnet-architecture-review`** — the standard repair for check 2.1's
  commonest shape, promoted from a trial reviewer's own proposal: when a
  facade reaches for the principal, point it at the abstraction under
  `Facades/Identity/Base` — the facade keeps the concept, the module keeps the
  type, the type-level cycle unwinds without a new edge.
- **`dotnet-security-review` + `auth-and-security`** — check 4.2 and
  `permission-internals.md` §3 now carry the conjunction's true spelling, in a
  marked framework-documentation block: **stacked `[HasPermission]` attributes
  mean ALL, one attribute listing several codes means ANY** — and the two
  refactors between the shapes are never cosmetic: merging silently widens
  access, splitting silently narrows it. The shipped "a conjunction is not
  expressible" sentence was true of one attribute and false of the action;
  the trial's reviewers derived the stacked semantics unaided (all four
  lenses), which earned it a place in the rubric.
- **`dotnet-security-review`** — check 3.3's grep and prose now cover
  **identifier generators minted as one-time credentials**: `NewId`,
  `Guid.NewGuid()`, COMB and ULID produce unique values, not unpredictable
  ones. Promoted from the trial's severest finding — a sequential id used as a
  password-recovery code, caught by the security lens alone, which is also the
  standing argument for the never-drop-a-lens rule.

Process notes for the record: version read off `main` at merge time — another
lane shipped 0.3.30 mid-session, exactly the collision the numbering rule
exists for. The four `[CẦN XÁC NHẬN]` items shipped only after the user
answered the report's five questions (recorded above beside each). The trial
also confirmed three mechanisms worth keeping verbatim: *Suppressions applied*
(it is what made C1 traceable — seen-and-suppressed is not missed), *Not run* /
layer coverage (the package layer has never run; nobody mistook that for
clean), and the CONFIRMED/PLAUSIBLE verify discipline (24/24 verified, zero
phantom findings).

---

## [0.3.30] — write only the minimum the task needs, 2026-07-29

Implements the decision doc
`docs/superpowers/specs/2026-07-29-write-simple-code-ownership-design.md` in
full: option (B), the ponytail ruleset distilled into house pieces — one grader,
two carriers, one offered call site. Installing ponytail (A) and doing nothing
(C) stay refused; the (A) refusal is now mirrored into `hooks/README.md`'s
refusal table with its reversal conditions.

**Changed:**

- **`dotnet-code-review`** — new priority area **7 Simplicity / over-build**
  (rubric `## 7. Simplicity and over-build`, checks 7.1–7.3); Cleanup/slop
  renumbers to row 8. The cap is MEDIUM and **absolute** — the rubric header's
  escalation clause does not reach the area, and at Low blast radius the area is
  not reached at all. Findings are severity-ranked with *candidate for
  `/simplify`* as the fix; the unranked `Cleanup candidates` section stays the
  checklist's. Where the simpler shape is itself a shipped convention, the area
  cites the existing grader (3.9, 6.8, the capability reuse laws, R16) and never
  re-grades. Description gains the trigger nouns (99 words, no `Not for:` entry
  dropped); Decision Guide gains three rows; `cleanup-checklist.md` renumbers,
  fixes its own mis-citation (the priority order lives in `SKILL.md`, not the
  rubric), and gains the grade-once boundary: code this diff *added* that
  nothing calls is 7.1's, code this diff *orphaned* is category 3's.
- **`dotnet-feature-flow`** (369 → 442 lines) — PHASE 2 closes with the
  simplicity ladder applied to steps as they are drafted: the three questions
  are the trigger, the rule lives in the rubric's area 7, and the instruction
  **trims the plan's own inventions, never the user's scope** (a step the user
  asked for goes to GATE 1, not into the bin). New section **The cleanup offer —
  after PHASE 5, before PHASE 6**: once per run, on an explicit yes only —
  silence, "up to you" and a yes to a different question are declines — then
  redo diff preparation and the pre-build gate (this flow's side of the seam)
  and re-enter the shared block from TEST-LOOP, same block, same caps. Not a
  gate; GATE 2 unchanged behind both answers; `:234` never-chased untouched;
  explicitly not `dotnet-review-flow`'s standalone offer reopened. Phase map,
  Outstanding, the Review-block caption (the carried report is the *last*
  block's), Routing and the Decision Guide all follow.
- **`claude-md-builder/references/static-rules.md`** — new rule **R24** (after
  R21; R7/R8/R14 are burned numbers from user-rejected rules, never reused):
  search-first with a checkable negative (*call it instead, or say why it does
  not fit*), and add nothing the task does not need beyond what the generated
  file's own conventions already require — the scope clause that makes
  sanctioned structure unreachable rather than exempted.
- **Router** (`choosing-a-dotnet-skill`, body rows only — the description stays
  under its 0.3.29 trial): the `dotnet-code-review` base-map row gains the new
  nouns; new shared-token row *"this is over-built" / "simplify this"* splitting
  grading, execution, plan-time and every-session carriers.
- **`NOTICE`** — obligation 3: `DietrichGebert/ponytail`, MIT (© 2026), covering
  the ladder structure and the two verbatim lines that head area 7.
- **`hooks/README.md`** — the ponytail refusal row: two independent grounds
  (session-start injection measured ignored at 0.3.27; a generic YAGNI voice
  cannot tell sanctioned structure from slop), and the two conditions under
  which the refusal stops holding.

**Rulings — three-way loop, three pieces, MERGE × 3:**

- **A shared claim at the doctrine's center was retreated, not shipped.** Both
  authors (and the design doc's own direction) stated *"an abstraction earns its
  place when something must substitute it"* as `dotnet-testing`'s criterion. The
  shipped sentences support less: an extension method cannot be substituted, and
  unit tests substitute at the facade boundary. Area 7.3 ships a two-armed
  admission test (a shipped skill mandates it, or a test must stand in for it)
  citing only what the source states. The S13b class, caught again.
- **`/simplify` provenance, ruled twice the same way.** No house file defines
  `/simplify`'s constraints (one tree-wide hit, in the design doc); Author B
  asserted its "no-behaviour-change constraint" in pieces 1 and 2 and was
  refused both times. The house hedge — *executes with its own verification*
  (`cleanup-checklist.md`) — ships instead. Author A's refusal to assert the
  external behaviour was the correct call under the S16 precedent.
- **Arbiter factual catches:** A cited 4.7 as a simplicity grader (it is stale
  cache/index — cut); A's greps used `src/Infrastructure/Facades/` against the
  rubric's own stated-once path convention (`src/Facades/`); A's
  reached-vs-unreachable dead-code boundary contradicted its own 7.1 `Find:`
  (B's added-vs-orphaned boundary ships, with the reciprocal checklist edit);
  B's offer sentence located the code lens's candidates "inside" `Unfixed
  MEDIUM and INFO` (they survive via the block's *nothing a subagent learned is
  dropped* rule, not inside that section — final wording names both real
  sources); the R24 insertion dispute settled on file evidence (every group's
  rules ascend internally, so after R21 — A's cross-group precedent refuted by
  A's own examples).
- **Coordinator catches:** the arbiter's self-declared addition *"the
  repository wrapper is substituted in every unit test"* overclaimed the source
  and shipped as *"what unit tests substitute at the facade boundary"*; the
  dispatch prompts' `references/architecture-rubric.md` path was the
  coordinator's error, not the design doc's (both authors located the real
  4.2 at `dotnet-architecture-review/SKILL.md:213`); the block-report shape was
  verified independently before the piece-2 verdict was accepted.
- **B-Q1 ruled without touching the sibling:** a `Cleanup candidates` slot in
  `dotnet-review-flow`'s report would fix the offer's input cleanly, but the
  file sits at 495 lines among design test 9's "not touched" three; option (a)
  wording shipped instead.
- **The user's Facades ruling holds at full strength in three places** (rubric
  boundary list, 7.1's *does not apply to*, the SKILL.md Decision Guide row):
  a finding against sanctioned structure is **wrong**, not low-value. B's
  drafted "if it is worth naming at all, INFO" was rejected as permissive drift.

**Verified live before merge.** A fresh-context subagent given only area 7 and a
synthetic diff flagged the unused `bool` parameter (7.1, MEDIUM) and the
one-strategy interface (7.3, MEDIUM), refused to flag an ahead-of-need
`Infrastructure/Facades/` capability — citing the carve-out's own "wrong, not
merely low-value" — and left the module file family alone. Both directions of
the user ruling survived a cold read.

**Known seams:**

- The design doc named one cross-reference for the renumber; five sites needed
  edits (`cleanup-checklist.md` 3/21/27 and the rubric's opening + ToC). Its
  §5.3 path shorthand for check 4.2 was accurate; the coordinator's dispatch
  expansion of it was not.
- Two cheap live checks logged, not run: B's R24 micro-test (a generated
  `CLAUDE.md` with and without the rule, against a task inviting a speculative
  abstraction) and the offer's soft-yes test (is *"sounds good"* treated as the
  explicit yes the decline enumeration exists to reject?).
- The cleanup offer's fuller input depends on the code lens's candidate list
  surviving block aggregation under the *nothing dropped* rule — no named slot
  exists. If a real run shows it dropped, the offer degrades to `Unfixed MEDIUM
  and INFO` alone; the fix would live in `dotnet-review-flow` (at the budget
  bar) and needs a user ruling.
- The Facades carve-out is structural in R24 — a generated `CLAUDE.md` never
  *displays* it. Making it visible would be a PHASE 3 project-specific line,
  not a static rule.
- The 0.3.28 seam (blast-radius re-rank for standing audits) is unchanged —
  same file, different problem, still parked.

## [0.3.29] — the router triggers on entry, not on confusion, 2026-07-29

The third and last defect from the 2026-07-29 observation. 0.3.27 got the plugin
entered by a hook; 0.3.28 gave the flow something to do when it arrives; this
repairs the router's own front door.

**The defect.** `choosing-a-dotnet-skill`'s description gated its first arm on
*"no skill self-triggered on the convention"* — **a non-event**. A session that
never considered this plugin cannot observe that nothing self-triggered, because
it never asked the question. The signal could never fire for the reader who most
needed it. The description's second arm — brainstorming, spec and plan writing,
composing subagent prompts — was already observable, already worked, and is
untouched.

**Changed — the first arm only, and at zero cost:**

- **The confusion framing is gone.** The trigger is now a **state of the session**
  a reader can check against its own transcript: *working in a .NET codebase with
  no dotnet-standards skill chosen*.
- **A new observable moment: `before reading, searching or listing files`.** That
  is the instant the measured failure occupied — the session ran `find`. It is
  keyed to an action about to be taken, never to a feeling.
- **`chosen`, not `loaded`.** `loaded` stays true right up until the Skill tool
  fires, so it would capture the correctly-matched reader in the window between
  deciding and acting. `chosen` also matches the body's own verb at `:26-28` —
  *"enough to choose, never enough to act on. Having chosen, load the skill"* —
  so description and body now describe one state with one word.
- **`nothing to load`, the body's own phrase** at `:90-92`, replaces *"a skill
  that does not load"*: plainer, shorter, and identical to the section it points
  at.
- **97 words in, 97 words out.** Net zero against a cap of under 100, one column
  narrower than shipped. Both `Not for:` entries and the whole second arm are
  byte-identical.

**How the line is drawn — and it is the point of the piece.** A description
pushy enough to fire on every .NET question inserts a hop before every sibling
and contradicts this skill's own `Not for:` (*"a question already matched to one
skill — load that skill directly"*). Three things hold that line: the predicate
excludes the matched reader by its own terms; the new trigger nouns are
tool-shaped (`reading`, `searching`, `listing`) where every sibling triggers on
domain nouns, so the router competes with none of them; and **no .NET domain
verb was added to buy pushiness** — `review` in particular was refused, since it
is contested between four rubrics and two flows.

**Rulings — three-way loop, one piece, MERGE:**

- **The decisive finding was the arbiter's, and neither author nor the
  coordinator raised it.** Author A put its guard in front of **all four**
  signals — including *"a `Not for` pointer led to a skill that does not load"*.
  **You can only follow a `Not for:` pointer from a skill you have already
  loaded**, so under A's guard that trigger is false by construction exactly when
  it would be true in fact. A defended keeping the clause because dropping it
  would orphan the body's `## Not yet covered` section, and then orphaned it by
  another route.
- **The same structure kills A's headline property.** A presented
  "self-extinguishes on success" as the repair's virtue. It is the bug: a session
  that loads one skill on turn 3 has permanently silenced *two skills seem
  plausible* and *the task spans several areas*, which are post-load signals by
  nature.
- **Author B's `or` hinge is what makes its version correct — and B never argued
  it.** The guard governs only the action-keyed arm; the three post-load signals
  sit outside it. B defended the umbrella on exhaustiveness grounds; the
  structural reason is stronger.
- **Author A produced the single best line in either draft**, and found it by
  auditing its own text: the observed session ran `find` **twice**, so *"the
  first read"* had already expired when the user interrupted. `before reading,
  searching or listing files` is two words cheaper and closes the hole. **Both
  drafts shared the lapse; only A saw it.** B's objection — the moment must stay
  bounded — is answered by the guard both drafts carry.

**Anti-example candidates, forwarded unlabelled (R8 is the user's alone):**

1. **A description trigger keyed to a non-event can never fire for the reader who
   most needs it** — the clause this release removed, with a measured failure and
   a documented repair behind it.
2. **A guard clause that silences the very trigger it was written to protect** —
   A's structure is a textbook instance, and the subtler of the two.

**Known seams:**

- **Plausible and unmeasured.** A description only participates in matching once
  installed, so no draft could be probed before merge. **Two description trials
  are now outstanding at once** — 0.3.28's standing-code triggers and this router
  repair — and the next real session in the consumer repository exercises both.
  If this one still misses, the next lever is the strength of the `Not for:`
  backstop, not the trigger wording: the trigger half now leans on it harder than
  the shipped version did.
- **The body does not yet state the entry condition the description carries.**
  `## How to use these tables` (`:14-18`) opens at *"Find the row that matches"*.
  Nothing is unreachable — the description is the matching surface and the body is
  read only after a match — but the two tiers would agree better with one
  sentence. Follow-up for a session that owns the body.

---

## [0.3.28] — `dotnet-review-flow` reviews standing code, not only diffs, 2026-07-29

The second defect from the 2026-07-29 observation, and the one the user actually
asked about. 0.3.27 got the plugin entered; this gets the request served.

**The flow only reviewed diffs.** PHASE 0 STOPped without a resolvable base, the
spawn contract derived every subagent input from a computed diff, and the
changed-file list came from `git diff --name-only`. So *"review these modules,
excluding these three"* — against code written long before the plugin existed —
could not be served at all. A perfectly-triggered session stopped at an empty
diff. Both PHASE 0 and the `/dotnet-review` argument hint already promised *"a
named scope"*; the machinery behind the promise did not exist.

**Scope, narrowed by the user before drafting.** *"Write the report to a file,
change nothing"* was a point-in-time need — validate the plugin's review quality
against pre-plugin code while keeping that code intact as evidence — **not a
permanent read-only mode**. Two apparent gaps were therefore not gaps: standalone
mode already changes nothing until the user accepts the offer, and the
never-write-inside-the-repository rule binds the **diff file**, not the report.
Exactly one thing was missing: **a scope that is a set of paths instead of a
diff.** No third mode was added.

**Changed:**

- **A path scope.** Paths with optional exclusions expand through `git ls-files
  -- <paths> :(exclude)<excluded>` to every **tracked** file, reviewed whether or
  not anything changed. Tracked-only is a stated guarantee, not an accident:
  build output and untracked scratch cannot enter a review.
- **Standing code reaches the fleet as a diff against the empty tree**, so the
  four spawn-contract inputs keep their exact shape and **the six agent files
  were not touched**. Chosen over a second input shape because of the contract's
  own rule — two lenses handed different inputs produce two reports nobody can
  compare.
- **The empty tree comes from `git hash-object -t tree /dev/null`, never the
  literal `4b825dc…`**, which is the SHA-1 empty tree and wrong in a SHA-256
  repository. Both authors reached this independently.
- **Count, state, then chunk.** PHASE 0 counts the expansion and states the
  count, the scope label, every exclusion **and any path that expanded to
  nothing** before anything is spawned. **An empty expansion is a STOP.**
- **Chunking bounds on files *or* bytes, whichever comes first** — ~100 files or
  ~250 KB of patch. The count is what a user can defer by name; the bytes are
  what actually breaks. **More than one chunk means state the plan and wait** —
  one chunk is the ordinary run, several is a bill the user did not name.
- **Chunking splits the fleet, not the suite.** TEST-LOOP is not chunked, so only
  REVIEW-LOOP's cap counts per chunk. Findings merge into one report; `### Run`
  now carries `Chunks <n>`, where an unchunked run is `1`.
- ***Not run* is now tagged**, because it carries three different kinds of thing:
  **not examined** (excluded paths, declined chunks, a layer no coverage line
  reached) versus **attempted, no result** (a lens that failed twice, a tier with
  no signal, a chunk a cap halted). Untagged, "chose not to look" reads as "found
  nothing".
- **`Base:` never goes blank.** For a path scope it reads `the empty tree
  (standing code)`, reusing the spawn contract's string rather than inventing a
  second one.
- **New Decision Guide row: "Every line in the diff reads as added."** An
  artifact of the empty-tree base, not a fact about the code. A reviewer's
  "newly introduced" is wording, and *"outside this change's scope"* has no
  referent when the scope **is** the file set — never move a severity for either.
- **The description was rebalanced, not extended.** It was already at 90 words
  against a 100-word cap, and it said *"over one diff"* — narrow, and for a path
  scope simply false. It now carries the failing sentence nearly verbatim plus a
  second verb (`audit`), and it is **shorter** than what it replaced: 88 words,
  same 9 lines.
- **Router rows `:58` and `:85` rebalanced in the same commit**, per the
  alignment rule. Both said "diff"; both now carry the scope shape.

**Measured, in real repositories, before any of it was written:**

| | |
|---|---|
| The motivating scope, 76 files | 149,071 bytes of patch — one run, no chunking |
| The same repository, whole | 4,782,492 bytes — **32×**, must chunk |
| Bytes per file across three sibling modules | 1659 / 2042 / 1756 — tight, so a file count is a serviceable proxy **only** for hand-written code |
| `git ls-files` vs `git diff <empty-tree> HEAD`, same pathspec | 76 and 76 — the two consumers of one pathspec agree |
| `git diff --name-only <empty-tree>...HEAD` | **exit 128**, *"is a tree, not a commit"* — see below |

**Rulings — the three-way loop, three pieces, three MERGE verdicts:**

- **The three-dot landmine.** The shipped contract wrote `git diff --name-only
  <base>...HEAD`. Substituting the empty tree into that form **errors on first
  use**. The arbiter found it before the drafts arrived and the coordinator
  reproduced it; neither author stepped on it, but the file would have invited
  the next reader to.
- **Author A's file-list relocation was rejected on verified evidence.** A moved
  the derivation into PHASE 0 check 4. `dotnet-feature-flow:88` states that
  embedded mode **skips check 4 entirely**, while `:207` names *"derive the
  changed-file list"* as a step of the section A was emptying — so embedded mode
  would have reached the spawn contract with no file list. This decided the
  structure.
- **A's confirmation-gate rationale rested on a false premise.** A argued a
  universal gate would deadlock embedded mode "where `dotnet-feature-flow` drives
  PHASES 4–5 unattended". `dotnet-feature-flow:238-240` already stops and waits
  on the user at a cap halt. The gate survives, **keyed to cost rather than to
  scope shape** — more than one chunk — which needs no exemption clause.
- **The cap-multiplication question was dissolved, not compromised.** One author
  asserted per-chunk caps without addressing the total; the other computed 25
  possible TEST-LOOP rounds and declined to resolve it. The suite is not scoped
  to a chunk, so re-running it per chunk buys nothing.
- **`references/` was NOT overturned, unlike 0.3.27's refusal.** `CHANGELOG.md`
  0.3.21 settled single-body for both flow skills — *"one control-flow graph has
  no long tail"*. That reason is about the **shape** of the content, not its
  size, and a scope mechanism creates no long tail. A refusal is reversible only
  when its stated reason stops holding; this one still holds, so the entire
  change was funded by compression. The file lands at **496 lines**, four under
  the hard bar, having started at 483.
- **Angle-bracket placeholders were rejected from the description.** One author
  proposed `"review the modules under <path>"`. The arbiter scanned every
  sibling description: **zero use the form.** And it fails that author's own
  test — a description matches literal user phrasings, and `<path>` is not a
  literal anybody types.
- **Two independent methodologies condemned the same 23 words.** Both authors cut
  *spawning the tester and reviewer agents in parallel*, *verifying findings
  against the code before any fix*, and *or looping tests and review to green* —
  one from the house description law, one from `superpowers:writing-skills`'
  *"never summarize the skill's process"*. That convergence funded the whole
  rebalance.

**Coordinator catches, recorded because two of them corrected the arbiter:**

- **A deleted Decision Guide row took a rule with it.** Cutting *"A cap is hit |
  … Never raise the cap …"* removed the only statement of that rule reaching
  REVIEW-LOOP: TEST-LOOP carries "Never a sixth" in its own paragraph,
  REVIEW-LOOP carried nothing equivalent. Folded back at zero line cost —
  *"Never a fourth, never lower a severity to clear the gate."* The arbiter
  endorsed the correction and withdrew the cut.
- **An arbiter instruction said "replace" where its own arithmetic said
  "append".** Replacing the chunking paragraph's last sentence would have deleted
  the merge-into-one-report and *Not run* rules. Appended instead; the arbiter
  confirmed that was its intent, and paid back the redundancy the append created.
- **The coordinator garbled the user's own decision and said so.** The user chose
  *"count, **ask**, then chunk"*; the context package rendered it as "state the
  count", and the arbiter — reading what it was given — rejected the gate
  entirely. Modality drift introduced by the coordinator, caught by the
  coordinator, returned to the user, who chose the cost-keyed form.

**Known seams:**

- **`git ls-files` reads the index; `git diff <empty-tree> HEAD` reads HEAD.** A
  file staged but never committed appears in the file list and not in the patch,
  so a reviewer would see a path with no content. Narrow, and the trade is
  deliberate: the count the user approves must be the count that runs.
- **`dotnet-code-review` still ranks by blast radius, which assumes a change.**
  The new Decision Guide row forbids moving a severity because the code is old,
  but nothing yet re-ranks a standing audit, and pre-plugin code violates
  conventions it never knew about. Expect volume. Belongs to a
  `dotnet-code-review`-owning session.
- **The description is untested.** A description only participates in matching
  once installed, so neither draft could be probed before merge. **The real trial
  is the original failing sentence, retyped in the same consumer repository
  against the installed 0.3.28** — and if it still misses, the next lever is the
  `existing`/`audit` coverage, not further wordsmithing of the clause structure.
- **`or unchanged code` is the arbiter's own phrase**, proposed by neither
  author, and the arbiter flagged it as the line to challenge.
- **The em-dash word-count convention was ruled** — count them, `wc -w`, because
  a convention a future session cannot re-derive with one command is not a
  convention — but it is **not yet recorded** in `02-repo-structure.md` §5. Solo
  chore.

---

## [0.3.27] — mechanism E: a hook names the router, once per session, 2026-07-29

The first hard evidence that skill descriptions alone do not get this plugin
entered — and the reason a component refused in S6 ships anyway.

**What was observed.** A session in a consumer .NET repository was asked, in
Vietnamese, to review a set of modules and write the report to a file. It went
straight to `find`. The user interrupted, said *"không load skill gì à"*, and the
session agreed and then did the same thing again. It loaded no skill, no command
and no agent, across two attempts.

Nothing was misinstalled. `installed_plugins.json` recorded the plugin at
**project** scope bound to that exact repository, version 0.3.26, `gitCommitSha`
equal to the merge commit; the repository's own `.claude/settings.json` enabled
it; the cache carried all 21 skills, 2 commands and 6 agents. The descriptions
were loaded and were declined. Superpowers' own emphatic `SessionStart` block
was present in that session and was ignored on turn 1 — which is the measurement
that decided where this hook attaches.

**Two defects were found. This release fixes one.**

*Defect 1 — the plugin had no way in.* The only entry mechanisms were a skill
description matching, or the user typing a slash command. There was no hook on
the prompt, and a consumer repository has no `CLAUDE.md` unless someone writes
one. Every path into the plugin depended on the model spontaneously choosing it.

*Defect 2 — the router could only be found by someone already looking for it.*
`choosing-a-dotnet-skill`'s description triggers on *"it is unclear which
dotnet-standards skill owns the question — two skills seem plausible, no skill
self-triggered on the convention"*. That is a condition about the reader's own
confusion. A session that never considered this plugin is not confused; it is
oblivious, and obliviousness matches nothing. **Not fixed here** — see Known
seams.

**Changed:**

- **New hook `router-nudge` (`UserPromptSubmit`).** On the first prompt of a
  session whose `cwd` looks like a .NET solution, it emits one
  `additionalContext` line naming `dotnet-standards:choosing-a-dotnet-skill` and
  nothing else. Registered in `hooks/hooks.json`; extensionless and invoked
  through `run-hook.cmd`, per the Windows rule.
- **It names the router and no destination.** A hook script that named a
  concrete skill would become a second source of truth for routing the day the
  router's tables move. The router routes; the hook only points at the router.
- **Once per session, not once per prompt** — a marker keyed by `session_id`
  under `${TMPDIR:-/tmp}/dotnet-standards/`, swept after seven days. Emitted
  context persists in the conversation, so re-emitting buys nothing and costs
  every turn. This is what answers S6's token objection instead of dismissing it.
- **Gated on a solution file, by glob and never by `find`.** A recursive walk of
  an arbitrary repository on every prompt is precisely the tax S6 was right
  about. Root `*.sln`/`*.slnx`/`*.csproj` first, then `*.csproj` at depth 2–3.
- **Every failure direction is silence.** No `session_id`, no writable temp
  directory, no bash, a solution nested past the cap — each yields no output and
  the session behaves exactly as it did before this release. The hook guards
  nothing, so it cannot fail open; that is what lets it pass the wrapper rule
  that killed the guard candidates.
- **`hooks/README.md`: the refusal row for this exact component is rewritten,
  not deleted.** It now carries the S6 verdict verbatim, the observation that
  falsified it, and how the token objection was answered. The two-hook counts
  (four places), the section heading, the files table and the `ships one hook`
  line are corrected; `README.md`'s roster row and `run-hook.cmd`'s stale
  one-hook comment are corrected with them.

**Ruling — a refusal is reversible, and reversing it belongs in the file that
recorded it.** S6 refused this component for a stated reason. The reason was
falsified by observation, not out-argued. Shipping the component while leaving
`hooks/README.md` declaring it refused would have put a contradiction into the
tree; deleting the row would have destroyed the record of why it was ever
refused. Both verdicts and the evidence between them now sit in one row.

**Verified before ship**, by observation rather than inspection — the failure
mode here is silent by design: five stdin payloads (.NET repo first call → JSON;
same `session_id` again → nothing; non-.NET `cwd` → nothing; missing
`session_id` → nothing; empty stdin → nothing) plus both halves of
`run-hook.cmd`, POSIX and `cmd.exe`, and a parse check of `hooks.json` showing
all three events registered.

**Known seams:**

- **Defect 2 is untouched.** `choosing-a-dotnet-skill`'s description still
  triggers on confusion rather than on entry. The hook now names the router
  directly, so entry no longer depends on that description — but the description
  is still wrong on its own terms. Next in the queue, and it is a skill piece, so
  it needs the three-way loop.
- **The whole review surface is diff-anchored, and the request that exposed all
  of this cannot be served by it.** `dotnet-review-flow` hard-stops without a
  diffable base (`SKILL.md:99-101`) and derives every subagent input from the
  diff; `dotnet-code-review`'s description says *"reviewing **changed** .NET or
  C# code … before merge"*. "Audit these folders of standing code, change
  nothing, write the report to a file" has no owner in this plugin. Even a
  perfectly-triggered session would have stopped at an empty diff. Parked for a
  `dotnet-review-flow`-owning session.
- **"23 skills" versus 21 — both numbers are right, about different things.**
  The board header read "23 skills" while `skills/` holds 21. The 23 comes from
  `claude plugin details` itself: its inventory line lists the two commands
  (`dotnet-feature`, `dotnet-review`) among the skills. The header now carries
  both and names which tool reports which, because the prove-it rule tells a
  session to read that count off `details` — a session comparing 23 against a
  directory count of 21 would conclude the install had failed.

---

## [0.3.26] — `claude-md-builder`: contradictions are reported, never cut, 2026-07-29

Second real run, same consumer repository, this time in update mode. It worked —
and it also deleted something it should have kept, which exposed the flaw behind
both failures so far.

The repository has one controller declaring its own `[Route]` prefix
(`api/Events/{eventId:guid}/Booths`). `api-surface` rules the opposite: the
prefix is declared once on `BaseController` and **no other controller declares a
`[Route]`** (`SKILL.md:41-42`); nesting is expressed by the verb attribute's tail
(`SKILL.md:51`, `72-75`). So the repository *contradicts* the skill. The compare
step saw a routing topic, matched it to the skill that owns routing, and cut the
line as a restatement.

The step was only asking *"does a skill own this?"*. It never asked *"does the
repository agree with it?"* — and a contradiction is the single most valuable
thing a scan can find, because it is the one thing no skill can tell you.

The user classified this instance as a defect to fix in code, not a deliberate
choice — which is exactly the ruling the skill should have asked for instead of
deciding alone.

**Changed:**

- **PHASE 1's comparison is now two questions, not one:** does a skill own this,
  and does the repository match or contradict it. Four outcomes, only one of
  which is "drop it": match → pointer; contradiction → PHASE 1c; skill assumes
  something absent → content; skill silent → content.
- **New PHASE 1c — report every contradiction, decide nothing alone.** The scan
  cannot distinguish a deliberate local choice from a defect; both look identical
  on disk. Every contradiction is listed for the user — what the skill requires,
  what the repository does, where — and the user classifies it. Deliberate goes
  into the file with its reason; a defect goes into the report and **nowhere in
  the file**, because a line describing a bug expires when the bug is fixed. This
  is a report, not a question, and does not count against the PHASE 2 cap.
  Silence is the one forbidden outcome.
- **Template section 6b — *Where this repository differs from what a skill
  assumes* — is now a required section**, 10 lines. The first run invented it ad
  hoc; making it part of the template is what makes it repeatable. It holds three
  things: a capability a skill assumes and the repository lacks, a confirmed
  deliberate contradiction, and a specified-but-unbuilt capability.
- **Source marking now applies in every mode, to any line describing something
  not yet true** — not only to greenfield and document-derived lines. Trigger for
  the change: a generated rule read *"event-scoped data (booths, devices,
  customers, gifts) is always bound to an event"* while exactly one controller
  did that, with no mark to say it was intent. Preference stated: narrow the
  claim to what is built; failing that, keep it broad and mark it. Never leave a
  broad claim unmarked.
- **Checklist item 2 narrowed** — cut only what the repository does *the same way*
  as the skill; an opposite is the finding, never the bin. Two new final-gate
  checks cover marks and contradiction handling.

`api-surface` is unchanged and needed no change — it already owns sub-resource
nesting. The earlier claim in session that it did not was wrong.

**Version note:** 0.3.25 was taken by a concurrent session, so this is 0.3.26.
Two sessions choosing the same number produce no git conflict — identical strings
in both manifests merge silently — so the number was read off `main` at merge
time rather than off this branch.

---

## [0.3.25] — `dotnet-review-flow`: the NO-SIGNAL branch, 2026-07-29

A real run of `/dotnet-review` against an external repository produced no
deliverable at all: both testers hit `RED — environment` (Windows Smart App
Control blocked the test host from loading locally built assemblies) and the
flow halted before the four review lenses — which never depended on the test
tiers — ever spawned. This bugfix closes that gap. Built outside the three-way
drafting loop, waived by the user for this session. A final review pass before
ship caught three further gaps in the report rule and a regression this same
branch had introduced in `dotnet-feature-flow`; all four are folded into this
entry.

### Fixed

- **A new doctrine paragraph precedes TEST-LOOP: halting a loop is never
  halting the deliverable.** Every stop condition below it — a cap, a
  timeout, an unanswered question — ends a loop and still owes the final
  report. Stating this once means the report rule can no longer be
  relitigated verdict by verdict.
- **`skills/dotnet-review-flow/SKILL.md` gains a NO-SIGNAL section**, a third
  named unit between TEST-LOOP and REVIEW-LOOP. It unifies two situations the
  skill used to treat differently — a tester returning `RED — environment`,
  and a tier that is absent — because both mean the same thing to a reader:
  **no evidence about the code under review, and nothing in the code to
  fix.** Only the absent-tier case used to still deliver a report; the
  environment case halted before the report was ever produced. The invariant
  now stated at the top of the section: **NO-SIGNAL may end in a question, it
  may never end in nothing delivered** — REVIEW-LOOP runs and the report is
  produced either way. Four steps: state the gap in words the user can act
  on; measure it in numbers (counts of untested types, which tiers are empty,
  whether the missing tier needs infrastructure stood up); repair under a
  capped ladder; then offer options built from the measurement, never a bare
  yes/no.
- **`RED — timed out` now halts the loop while still owing the report, and
  stays out of NO-SIGNAL.** Retry once with a larger budget, then halt — but
  unlike a blocked or absent tier, a timeout can be the code's own fault, so
  it never joins the unified treatment above.
- **NO-SIGNAL that changes the tree re-enters TEST-LOOP before REVIEW-LOOP.**
  If repair wrote tests or edited a project file, the diff is recomputed
  first, so reviewers are never handed a diff that predates the tests just
  written.
- **The repair ladder's single classifier: does the action acquire something
  over the network?** If yes, ask the user first; anything irreversible,
  needing administrator rights, or governed by policy the user does not own
  is never done unasked. Capped at two attempts, where one attempt is one
  repair pass plus one re-spawn of the testers — the cap governs the reruns,
  not the individual actions inside a pass. The re-run rung itself is
  narrowed to diagnostic commands and reading configuration, never a test
  suite — the tiers are re-run only by re-spawning the testers, so repair
  cannot collide with TEST-LOOP's own ban on this session re-running a suite.
- **`Never scaffold a tier` deleted at both of its sites** (the `tier absent`
  verdict row and the Decision Guide's "no test projects at all" row) — a
  user ruling of 2026-07-28, replaced by measure-then-offer: a fabricated test
  project would make the flow's next round measure something it invented.
- **PHASE 0's "install nothing" narrowed to the preflight it was written
  for**, so it no longer contradicts the repair ladder. Standing up a test
  environment later, under NO-SIGNAL, is a different act with its own rules.
- **The report rule now reads "There is no path through the shared block that
  ends without the report,"** carved out from the whole flow: PHASE 0 and the
  pre-build gate stop *before* the block and hand back diagnostics instead,
  and everything after them owes this report. The Tests section of the
  report template now handles a `RED — environment` tier identically to an
  absent one — its row carries the verdict and dashes and reappears under
  *Not run* with what NO-SIGNAL attempted. The `Run` line still records what
  NO-SIGNAL attempted and what the user chose, with a deferred choice going
  into the existing *Not run* section rather than a new one.
- **`agents/dotnet-unit-tester.md` gains an `### Environment` section**, a
  reporting slot only, so the unit tier has somewhere to carry a blocking
  message verbatim — mirroring the integration tester, and matching the tier
  that actually failed in the originating incident. **Neither tester gained
  any power to repair anything**; every prohibition on repairing, writing
  files, or managing containers by hand is unchanged.
- **`skills/dotnet-feature-flow/SKILL.md` fixes a caller regression this same
  branch had introduced.** Once NO-SIGNAL could complete the shared block
  with a tier recorded under *Not run* — neither a cap nor a red suite — the
  caller had no stop condition left before PHASE 6, so it would have
  proceeded to commit a feature for which no test ever ran. Closed with an
  explicit stop-and-ask rule in the shared-block section, a matching Decision
  Guide row, and the ownership-table cell renamed from "the two loops" to
  "the three named units" now that NO-SIGNAL sits alongside TEST-LOOP and
  REVIEW-LOOP.

---

## [0.3.24] — `claude-md-builder` records the delta, not the doctrine, 2026-07-29

First real run, against a live consumer repository, exposed a design hole: the
user had rejected R7 (dependency direction) and R8 (marker-interface DI
registration) as static rules **because `facade-module-architecture` already owns
them** — and the scan then rediscovered both from source and wrote them into the
generated file's *Project structure* and *Architecture and layering* sections. A
rejected rule returned as a "scan finding". The rejection was recorded at one
layer; the skill had a second, unguarded path.

Verified before acting: `facade-module-architecture` teaches both, in more
detail than the generated file did — `SKILL.md:21` carries the same dependency
diagram, `references/solution-layout.md:16-21` adds the trap that a redundant
`Core` reference in `Web.csproj` still compiles, and `SKILL.md:95-100` rules that
there are exactly two lifetime markers and deliberately no singleton one. The
generated file had flattened that to a single marker. The failure mode is not
inaccuracy but confidence: a reader who believes they know the rule stops opening
the source that states its exceptions.

**Changed:**

- **New core principle 8 — *Record the delta, not the doctrine*.** Where a
  `dotnet-standards` skill owns a convention, `CLAUDE.md` points at it and says
  nothing more; a line is written only where the repository *differs*, or where
  the skill has no answer. Includes the owner table. The skill can assume the
  plugin is installed — it ships inside it, so if it runs, the plugin is there.
- **PHASE 1 gains a comparison step.** Layout and convention findings are checked
  against the owning skill before reaching the draft: match → pointer, differ →
  content, no answer → content.
- **Template sections 4 and 5 are now deviations-only**, budgets cut 25 → 8 and
  15 → 8, both omitted entirely when the repository conforms. Directory trees are
  banned outright: one that mirrors the canonical shape is doctrine restated, and
  one of a repository still taking shape reads as the intended final layout and
  stops Claude creating what is missing. Section 6 now carries skill pointers
  alongside document pointers.
- **Checklist gains cut item 2** — doctrine an owning skill already covers, with
  an explicit instruction to check twice for rules the user rejected. Later items
  renumbered.
- **R9 narrowed to a pointer**; **R10 narrowed to its guard arm** — *do not create
  a new top-level folder or project without asking* — which survives because it
  is a guard, not a convention, and no skill states it.
- **New hard constraint:** never restate a convention a `dotnet-standards` skill
  owns.

Expected effect on the run that triggered this: the two sections collapse from
27 lines to about 8, and *Architecture and layering* disappears entirely.

---

## [0.3.23] — `claude-md-builder` speaks Vietnamese, 2026-07-28

Language is now settled at both levels, on user direction.

- **New rule group in `static-rules.md` — Communication and language**, ungated,
  shipping in every generated file. **R22:** the user is addressed in Vietnamese
  — chat, questions, summaries and prose documents; code, identifiers, commands
  and paths stay English. **R23:** with Superpowers, brainstorming output is
  Vietnamese and the plan stays English — a plan is an artifact other agents
  execute, so it stays in the language of the tooling and the codebase. R23 is
  self-gating: it states its own condition, so no scan detection is needed.
- **New template section 7b — Communication**, required, 4 lines, placed near
  the top of the rules so a reader who stops early has still seen it.
- **The generated `CLAUDE.md` stays in English** (user ruling), and the static
  rules ship in their canonical English form. The split is the one R23 already
  draws: the conversation is Vietnamese, the artifact agents execute against
  stays in the language of the codebase.
- **The skill itself converses in Vietnamese**, added to its hard constraints.

---

## [0.3.22] — `claude-md-builder`, the tier-3 generator, 2026-07-28

The plugin gains a skill that writes the **per-project `CLAUDE.md`** — tier 3 in
the README's three-tier model. Built to a user-directed phased spec (research →
approved static rules → approved discovery design → build → dry run), **not**
through the three-way drafting loop: the user ruled the loop off for this
session.

**Shipped:**

- **`skills/claude-md-builder/`** — `SKILL.md` plus four `references/` files:
  `scan-map.md` (13-row scan table, three user questions), `static-rules.md`
  (the approved rule set, each gated by an `Applies when` condition),
  `template.md` (section skeleton, ordering, per-section line budgets summing to
  165 with a 200 ceiling), `checklist.md` (anti-pattern list in cutting order).
- **Router alignment, same commit** — one base-map row (`claude-md-builder`),
  one shared-token row (*a convention or a rule*), and the build-sequence line
  extended with *project memory*.

**Rulings recorded:**

- **18 static rules approved, 3 rejected.** Rejected: dependency-direction and
  marker-interface DI registration (user: out of scope for tier 3), and the
  config-precedence rule. Every candidate was checked against a live
  StyleCop + SonarAnalyzer + Roslynator configuration first — formatting, naming,
  using-ordering, XML-doc and nullable rules were excluded by construction,
  because a rule an analyzer enforces must not spend session context.
- **Blocking-async (R11) ships despite Sonar `S4462` covering it** (user ruling).
  Evidence: `TreatWarningsAsErrors=false` in the corpus, and three surviving
  violation sites — an advisory analyzer did not stop them.
- **`CancellationToken` (R6) is prescriptive, not descriptive.** The corpus is at
  roughly 37% adoption (63 of 172 `Task`-returning controller methods); the rule
  imposes the convention going forward. `CA2016` does not cover it — that rule
  forwards an existing token and never asks for the parameter to exist.
- **Committed credentials are not assumed to be leaks.** The user's repositories
  are hosted in a private registry, so real values in tracked config can be
  deliberate. R12 became a question with two opposite outcomes (R12a forbid /
  R12b deliberate); R13 — never letting a secret reach a transcript, log or
  commit message — is absolute in both arms.
- **`/init` is not used and its output is not trusted** (user direction,
  following HumanLayer over the official "run `/init`, then refine" advice). It
  generates precisely the derivable content the trim step exists to remove.
- **Test policy is always asked, never inferred.** Decisive evidence: a corpus
  repository ships two fully-built test projects while its own `CLAUDE.md`
  forbids writing tests.
- **Static rules are conditional.** Each carries an `Applies when` gate, so a
  repository without migrations receives no EF rules. This is what keeps two
  generated files from converging on the same generic text.
- **Greenfield branch** (user-raised gap, same session). A repository with no
  business code yet has nothing to scan, so the skill takes documents the user
  names — spec, design note, ERD, API contract — as a source, and only those.
  The boundary is hard: *a document states intent; only the codebase states
  fact.* Documents may produce the project's purpose, a domain glossary,
  intended boundaries and agreed constraints; they may never produce a command,
  a path, a framework or a package, and nothing from them may be phrased as
  already existing. Document-derived lines carry a stripped-at-load HTML comment
  naming their source, so the next update knows what to re-verify. A capped
  10-line `Planned, not yet built` section holds the rest and is the one part of
  the file with an expiry rule — surviving two updates unchanged makes it the
  *historical archive* anti-pattern, and it is cut. The greenfield branch also
  swaps PHASE 6's three probes, since build commands and migration projects do
  not exist to be asked about.

**Not done this session:** the dry run against the reference projects (user
deferred it — the plugin ships first, feedback comes from real use on a
consumer repository).

---

## [0.3.21] — Process-integration layer v1 (Lane D), 2026-07-28

The "Knowledge only" promise is deliberately broken, per the approved design
`docs/superpowers/specs/2026-07-27-process-integration-design.md` (user ruling,
S14/D0). The plugin now has two layers: knowledge (unchanged) and process
integration — closed-loop workflows that CALL Superpowers, never copy it.

**Shipped (five groups):**

- **Two flow skills** (three-way loop, one MERGE verdict each):
  `dotnet-review-flow` — the shared TEST-LOOP → REVIEW-LOOP block, standalone
  (`/dotnet-review`, ends at the report, fixing OFFERED) or embedded (called by
  the feature flow as its PHASES 4–5; caller must state "embedded", default is
  standalone — the safe direction). `dotnet-feature-flow` — PHASE 0 → brainstorm
  → plan → GATE 1 (human) → implement (≤3 use-cases: TDD in-session; >3:
  subagent-driven-development; plan-step skill pointers mandated via the
  router's planning section) → the sibling's shared block, embedded → git →
  GATE 2 (human, before any push).
- **Six specialist agents** (three-way loop, two MERGE verdicts): four
  read-only reviewers (`dotnet-code-reviewer`, `dotnet-architecture-reviewer`,
  `dotnet-security-reviewer`, `dotnet-performance-reviewer`) bound to the four
  rubrics, `tools: ["Read","Grep","Glob"]` — read-only by configuration; and
  two testers (`dotnet-unit-tester`, `dotnet-integration-tester`) bound to
  `dotnet-testing`'s two tiers, `+Bash` for build/test only, file mutation via
  shell forbidden by instruction.
- **Two commands**: `/dotnet-feature`, `/dotnet-review` — thin deterministic
  entries; plugin-command namespacing verified against current docs before
  naming (spec §9.1): always `/dotnet-standards:<name>`, bare form as fallback;
  plugins CAN shadow built-ins, hence the `dotnet-` prefix.
- **SessionStart `superpowers-check` hook** (warn-only; PHASE 0 of each flow is
  the hard stop) + `hooks/README.md` justification against the wrapper's
  silent-absence rule; `.gitattributes` pins LF on the new script.
- **Description rewrite** in BOTH manifests (two layers; requires Superpowers).
  The rewrite also drops two claims for skills that do not ship
  (`observability`, `worker services` — both user-PENDING) and corrects "CQRS
  pipeline" to "in-process messaging pipeline" per the S8 ruling (0.3.3) that
  renamed `cqrs-feature-slice`; the same correction is applied to README's tier
  table.

**Key rulings (arbiter + coordinator, recorded):**

- Severity vocabulary: the spec's "blocking/major" is superseded by the shipped
  ladder CRITICAL/HIGH/MEDIUM/INFO (`dotnet-code-review` owns it; spec §9.3
  anticipated this). Loop exit = no CONFIRMED CRITICAL/HIGH remain.
- Test taxonomy verified: exactly two tiers (unit, integration; flow tests live
  inside integration) — two tester agents, adding nothing (spec §9.2/§4).
- Closed tester verdict vocabulary (arbiter addition): GREEN · RED — tests
  failed · RED — build failed · RED — environment · RED — timed out · tier
  absent — nothing run. The flow branches on exactly these; `RED — environment`
  halts immediately without consuming a round; `tier absent` never blocks the
  loop and never reads as a pass.
- PHASE 0 checks Superpowers by ACTUALLY LOADING a Superpowers skill, never by
  reading `installed_plugins.json` (a disabled install keeps its registry key);
  plugin completeness checked against the session's skill/agent roster, not by
  loading five rubrics (contamination + cost).
- Reviewer tools allow-list is the sole read-only mechanism (`disallowedTools`
  rejected as drift); agent files are NOT bound by the §5 skill-description law
  (different selection mechanism) but keep <100 words + anti-triggers; agent
  `Not for:` entries name sibling AGENTS, not skills (the reader is choosing
  what to spawn).
- Both piece-1 authors shared a blind spot the arbiter corrected: the Low-radius
  report-collapse exception (`dotnet-code-review` L185) was missing from the
  code reviewer. Unverified runner-flag claims (`--logger` verbosity) cut per
  the S16/S17 provenance precedent.
- Spec §9.4 resolved: both flow skills are single-body (398 and 362 lines); no
  references/ split — one control-flow graph has no long tail.
- `superpowers:writing-plans` has no "use-case" unit — its unit is `### Task N`
  (verified). The spec's `≤ 3 use-cases` threshold is kept as the user's own
  approved concept and defined in `dotnet-feature-flow` as units of delivered
  behaviour grouped from the plan's tasks, with a count-as-two tie-break. It is
  deliberately NOT the task count: a three-use-case feature routinely spans six
  tasks, so re-expressing the threshold in tasks would have silently moved
  almost every run onto the subagent route.
- GATE 2 moved INSIDE PHASE 6, binding `finishing-a-development-branch`'s
  integration-option choice (spec §6.1 draws it after PHASE 6; the verified
  fact that the push option lives inside that skill makes the after-placement a
  race — the spec's constraint "no push without GATE 2" is preserved, the
  mechanism refined).
- `superpowers:finishing-a-development-branch` presents push as a user-chosen
  option (verified) — GATE 2 is positioned at the option choice, not after the
  skill returns.
- Router alignment (same session): two base-map rows (the two flows), a "review"
  shared-token row, the base-map order line gains "→ flows", and the router
  description's `Not for:` narrowed from "process-layer workflow — Superpowers"
  to "the process skills themselves" (the flows are now in-plugin process
  layer). README.md minimally corrected where this lane falsified it
  ("commands deliberately absent", "does not own workflow", stale status
  table) — a coordinator boundary call, logged for veto.
- Known maintenance cost (recorded, not engineered around): the four reviewer
  bodies share ~50% structure with no include mechanism — a future edit to the
  shared report-discipline wording is made four times.

**v1.5 next (Lane D's next session):** the `bugfix` flow (spec §6.3 —
systematic-debugging → fix → the same shared blocks). Banked: rewrite the
agents' rationalization tables from observed smoke-test behaviour if it
diverges from the predictions.

---

## [0.3.20] — Review rubric #4: `dotnet-performance-review`, 2026-07-28

### Added
- **`skills/dotnet-performance-review/`** — the fourth and last review rubric
  (SKILL.md 499 lines; `references/performance-checks.md` 510). Five areas
  ordered by where the money is: query shape and round trips, blocking and
  async cost, cache and staleness cost, lock and contention,
  search-infrastructure cost. Body checks 1.1–5.4 as table rows; references
  continues each area's numbering (1.7–1.11, 2.5–2.10, 3.6–3.10, 4.6–4.10,
  5.5–5.9) plus a round-trip comparison table ("a floor, not a budget") and a
  12-row `Refused — and why` table (rate limiting with the inbound route from
  `dotnet-security-review` named and body 1.3 as the one house-grounded
  mitigation; Hangfire/background jobs; benchmark/profiling methodology;
  HybridCache version-neutral; compiled queries + query-construction; Span/
  pooling/ValueTask; TimeProvider; sealing; DbContext-direct; ClockSkew-Zero;
  pool-sizing/context-pooling; projection-safety routed to its owner).
- **Honesty rule, verbatim in every report** (Principle 1 = template
  blockquote, word-identical, checked at the final pass): *static inspection
  of code shape, not a measurement … it cannot tell you which of them your
  traffic actually reaches*. No finding may carry a number not read out of the
  code — countable (round trips, hold times, page bounds, configured seconds)
  vs not-countable (percentages, milliseconds, allocation sizes) stated as a
  two-list rule.
- **Severity calibration** citing rubric #1's ladder, never restating: HIGH is
  the home rung ("fails predictably under load or on a second request");
  CRITICAL = stopped-not-sluggish (thread-pool exhaustion, unbounded fetch,
  transaction-before-lock — with the stated reconciliation that the lock's own
  queue is not a P1-style precondition); degrade-to-correct de-escalates and
  says so; one cross-area rule (unbounded growth = HIGH, code-bounded =
  MEDIUM) replacing per-row qualifiers.
- **Grade-once enforced structurally**: fifteen graded-by rows carry the
  owning `dotnet-code-review` check in the severity cell and no severity of
  their own (deepening 1.2, 1.3, 3.1, 3.2, 3.3, 3.4, 3.6, 3.8, 3.10, 4.7,
  4.8; 2.10 is the one split-cell row — HTTP arm graded by 4.8,
  capability-singleton arm HIGH here). The routing table names the eleven
  deepened checks and is the single authority (Principle 6 no longer
  duplicates the list — the assembly-time D4 lesson: enumerate a cross-skill
  list exactly once and name the authority).
- **Suppression discipline**: per-area *Not a finding* blocks (sync validator
  predicates; the four connection values; RetryTime; the outbound-spanning
  lock "correct, a throughput ceiling"; terminateAfter's permitted default;
  first-resolve; eager lock-factory connect; separate multiplexers; the
  non-async dispatch) + report-level `Suppressions applied` (not optional when
  an area ran) + `Area coverage`.
- **Router alignment (same commit)**: the `dotnet-performance-review`
  reservation row DELETED from *Not yet covered* (0.3.18 planted it for this
  session); a base-map review row added; one disambiguation row added
  ("this is slow"/performance cost — the shape's owner vs grading in review,
  the same owner-vs-review split as the architecture and security rows).

### Rulings (the loop worked — the densest catch series of any session)
- **Six grade-once violations caught before ship, none shipped**: A's 1.7
  (tracking = #1's 1.2) and 2.5 (async void = #1's 3.3); 4.6 (nested
  single-key locks = #1's 3.8 — missed by BOTH authors AND the arbiter,
  caught by the coordinator's full-title-list sweep); 4.8 (pre-lock-read key
  = #1's 3.6, the arbiter's own re-sweep); body 4.1 and 4.3/4.4/4.5 seams vs
  #1's 3.6/3.7 (final pass, fixed with zero-line disambiguation clauses).
  **Root cause systematic and recorded: the brief handed authors seven check
  numbers instead of the sibling's complete check-title list. Durable fix:
  future rubric/lane briefs carry the full sibling check-title inventory in
  the context package.**
- **Verified-false claims cut before ship**: A's P1 CRITICAL example fused
  two distributed-lock shapes (outbound-span is shipped-*correct*;
  pool-pressure belongs to transaction-before-lock); B's 5.2 graded
  "unnecessary `terminateAfter: null`" — the shipped sentence constrains
  keeping the default, it does not oblige it (S13b permission-into-obligation
  drift, mirror image); B's `AbortOnConnectFail = true` library-default claim
  (API recall, S16 precedent) — the check survives on the shipped consequence
  sentence; B's disclaimer third sentence bounding provenance where the
  precedent bounds capability; A's "get shape = one round trip" comparison
  row (no shipped sentence numbers it — dropped, compositions marked as
  compositions).
- **Citation corrections**: A cited permission-internals §7 for the §4
  *Implied permissions* rule; A declared the ToPagedList overload sentence
  nonexistent (it lives at api-surface request-response-dtos.md:328); B's
  grade-once list was short by 3.10; B's routing list short by 1.2; B's 1.3
  guide row used its own unshipped title. The arbiter's "4.4 does not exist"
  alarm was itself refuted (layer-4 body checks are SKILL.md table rows;
  references only carries 4.10+).
- **Description**: 94 words; `permission cache internals — auth-and-security`
  added on the roster rule set this session (*a check target or a suppression
  creates the Not-for obligation; routes and refusals do not*), paid for by
  dropping the `a dead Include` noun; `automapper-mapping` stays out (its own
  description routes ProjectTo to ef-core-data-access — S15 dangle rule);
  `mediatr-messaging` is a routing-table destination, no roster entry.
- **Form rulings**: the novel-form rule ("a novel form ships only when the
  sibling form demonstrably fails at something") — B's numbered suppression
  block rejected under it, content kept unnumbered; FAIL lives in the
  template + its own paragraph (architecture precedent), not as findings rule
  5; body 1.3 hosts the DoS landing (half-sentence naming the security
  inbound route).
- **Budget**: assembled body measured 515 by actual `wc -l` (the arbiter's
  ~451 projection was once again an estimate — the 0.3.19 defect class, this
  time caught pre-ship). Fixed per the adopted priority: Principle 6
  de-duplication (−2), five redundant Decision Guide rows deleted (−5),
  prose rewrap at wider columns, content untouched (−9). Final 499/500 —
  margin one line; the next addition to this body must be paid for by a cut.
- **Final consistency pass**: FAIL→PASS. D1 references "How to read a check"
  clause widened for the one split-cell row; D2/D3 zero-line seam cites in
  4.1/4.5; D4 duplicate-enumeration fix; blockquote-vs-Principle-1
  word-identical (the 0.3.18 D1 class did NOT recur); 15/15 graded-by
  numbers+names verified; teasers ↔ references reconciled; sanitization
  clean.

### Process notes
- Coordinator committed the S16-class verbatim violation TWICE (P3 and P4
  forwarded as compressed indexes); the arbiter refused to rule on summaries
  both times and the full texts were re-sent — the safeguard held from the
  reviewer side. Lesson: forwarding VERBATIM means the draft prose itself, in
  the message body, every round, however long.
- Banked, still unowned (unchanged from 0.3.18/0.3.19 logs): ClockSkew-Zero
  clock-drift (refused a second time, as predicted); Hangfire/background
  scheduling (waits for `background-worker`); the S11 CompileQueryAsync
  pagination extension (R7, user must name the file; user re-confirmed
  banked this session). Area 4 ships with no BAD/GOOD pair — no labelled
  lock anti-example exists in any bank (R8; recorded beside the S17
  seventh-anti-pattern gap). Two shipped shapes recorded as
  never-labellable-without-the-user: the outbound-spanning lock (owner says
  "Correct") and the semaphore registry window (owner: "not a defect to fix
  in passing").
- **Lane D is UNLOCKED**: all four review rubrics have shipped
  (0.3.15 / 0.3.17 / 0.3.18 / 0.3.20).

---

## [0.3.19] — Rubric session #3 budget fix, 2026-07-28

### Changed
- **`dotnet-security-review/SKILL.md` compressed 804 → 498 lines** (bar: <500;
  siblings 246/411). Root cause was structural, not prose: the body carried all
  29 checks in prose form where the shipped sibling convention
  (dotnet-architecture-review, 28 of 29 checks) is table rows. 26 checks
  converted to table rows; 3 keep prose form for sub-structure a cell cannot
  hold (3.1 four bullets, 6.1 two lettered shapes, 6.2 the R8 BAD/GOOD blocks —
  the 4.9 precedent). Nothing dropped: every check number, title, severity,
  owner and `Find:` survives; all suppressions, the report template, the
  calibration table, routing tables and Decision Guide untouched.
  `references/security-checks.md` needed no edit (all ten body-check titles it
  cites survive verbatim). Body-tables + references-prose confirmed as the
  shipped house pattern, not a divergence.
- **Defect class recorded: "an estimate presented as a count."** The 0.3.18
  final pass reported "P2 ~470 lines" without measuring; the real figure was
  804 assembled. Caught by the coordinator with `wc -l` post-assembly; the
  S17-precedent budget pass (553→450) applied at larger scale. Standing rule:
  line counts are reported only from an actual count.
- **Margin warning:** the body ships at 498/500. The next addition to it must
  be paid for by a cut.

## [0.3.18] — Rubric session #3 (solo), 2026-07-28

### Added
- **`dotnet-security-review`** — review rubric #3 of four, built under the
  three-way process (verdicts: P1 MERGE, P2 MERGE, P3 MERGE, P4 router MERGE;
  final whole-skill consistency pass FAIL→PASS, 3 defects fixed). Six layers —
  packages, secrets, injection and unsafe input, auth posture, CORS, data
  protection and exposure — checked against the shipped skill bodies; it cites,
  never re-teaches. Body: Overview + 5 Core Principles + six layers with
  numbered checks (1.1, 2.1–2.5, 3.1–3.5, 4.1–4.9, 5.1–5.3, 6.1–6.6) + severity
  calibration (cited from dotnet-code-review, calibrated per the
  0.3.17 precedent) + report template + Routing + Decision Guide (~500 lines
  rendered). `references/security-checks.md` continues the numbering (1.2,
  2.6–2.7, 3.6–3.8, 4.10–4.17, 5.4, 6.7–6.8) plus three comparison-data blocks
  (shipped pipeline order; the five-site principal-type list restored to the
  shipped enumeration after BOTH authors corrupted it identically; which gate
  answers what) and an in-file `Refused — and why` table (457 lines).
- Router alignment, same commit: base-map row for `dotnet-security-review`;
  disambiguation row `secrets / tokens / authorization gates` (write-the-rule →
  `auth-and-security` vs audit-what-exists → `dotnet-security-review`, twinned
  with the 0.3.17 placement row's structure); **flagged extra beyond strict
  alignment**: a `dotnet-performance-review` reservation row added to *Not yet
  covered* — this session raised its dangling-pointer count to three, and the
  section's stated purpose is resolving exactly that dead end (Author B's
  stricter merge-time reading recorded; rubric #4 deletes the row).

### Rulings and process notes
- **House-over-kit at the JWT center (kit divergences recorded per the 0.3.17
  convention):** the kit anchor `security-scan` (A32) demands
  `ValidateIssuer/ValidateAudience = true` and a 1-minute ClockSkew; the rubric
  checks HOUSE doctrine (per-family signing key is the boundary; ClockSkew
  Zero) and ships the suppressions as first-class *Not a finding* blocks. Kit's
  Minimal-API `[Authorize]` samples re-expressed as
  `[HasPermission]`/`[AllowAnonymous]`/`[ApiKey]` on controllers. Kit's 6-layer
  taxonomy and severity-with-context kept; kit's Critical/High/Medium/Low
  re-expressed in the house CRITICAL/HIGH/MEDIUM/INFO ladder; the honesty rule
  ("static analysis, not a penetration test") kept VERBATIM in every report and
  canonicalised to one wording (final-pass defect D1: Principle 1 and the
  template had drifted apart). Kit's per-layer MCP steps (endpoint map,
  find-references) degrade to grep, stated in the body.
- **Partition vs rubric #1:** dotnet-code-review's 2.1–2.7 and 1.7 keep their
  home and numbers; this rubric deepens and cites (2.2 called "the
  highest-value single grep in either rubric"). No claiming.
- **CORS limited to what has an owner:** pipeline position + registration
  pairing + the one universal defect (5.3, SPLIT: reflected origin
  `SetIsOriginAllowed(_ => true)`+credentials = CRITICAL; literal
  `AllowAnyOrigin()`+credentials = MEDIUM misunderstanding — browsers reject
  it). Origin/header/method policy REFUSED — no shipped owner.
- **R8 (user-labelled, sanitized):** the response-DTO property documented
  "internal only, not returned in JSON" while remaining a plain public property
  ships as the BAD/GOOD block under 6.2 — the first C# code block in any rubric
  body (deliberate divergence, both authors independent). Username enumeration
  at login (user-banked at S9b) ships as 4.17, MEDIUM,
  decision-not-defect framing — the shipped login flow itself distinguishes the
  branches, so the check surfaces a trade-off, never grades the house example.
- **Provenance rulings:** mass assignment = universal reinforced by
  api-surface *Binding sources* (description promise from both shipped rubrics'
  routing tables honoured by check 6.1); `SaveToken = true` deepening stamped
  "universal consequence of a shipped setting" (the setting is corpus-grounded
  at jwt-and-tokens.md:130); 6.5 redaction-verifiability check ships — the
  "observed behaviour, not a pattern to copy" text necessitates the check, the
  check asks nobody to copy it. REFUSED for lack of a shipped owner: XSS (no
  view layer), CORS origin policy, a numbered test-posture check ("a test
  scheme reachable from a deployed composition" — banked; the routing row to
  dotnet-testing ships), rate limiting/lockout, password-hashing primitives,
  security response headers, token-lifetime ceilings, a prescribed secret
  store.
- **Coordinator catches on the arbiter (recorded per the honest-log norm):**
  (1) the "two independent gates" sentence was cut as unverifiable but is
  shipped verbatim at jwt-and-tokens.md:452-455 — restored; (2) the arbiter
  twice misread `Required(...)`'s exclusion semantics (called Author A's
  optionality clause false at P3, then claimed an auth-and-security intra-skill
  contradiction at the final pass) — the shipped file documents the semantics
  at jwt-and-tokens.md:89-92, NO seam exists, the false Known-seam note was
  withdrawn and A's clause restored into 2.7. Also caught by the loop: Author
  A's fabricated `[AllowAnonymous]` stale-principal mechanism (the shipped
  mechanism is principal-unset-by-design), A's four miscited body-check titles,
  Author B's abridged pipeline order (omitted APM and CORS) and inverted 5.1
  rationale, and the shared five-site corruption — shared-blind-spot instances
  5+ of the S13b/S15/S17 series.
- **Severity calibration precedents set:** refresh-token replay response =
  HIGH not CRITICAL (attacker must already hold a superseded token — first
  mechanical application of the ladder's precondition test to overrule an
  author); fail-closed defects de-escalate (3.6 MEDIUM, 5.4 MEDIUM);
  availability defects on the key path are MEDIUM and say so (2.6).
- Body forecloses tail lesson: layer-1 and layer-5 preambles written before
  the references file asserted the layer's total size — both softened at the
  final pass. A body written before its references file over-claims
  completeness in exactly the layers with the smallest tails.

### Known seams (logged, not fixed here — outside this session's ownership)
- `dotnet-performance-review` remains the only unshipped rubric; its
  reservation row ships in the router this commit and rubric #4 deletes it.
- Banked for rubric #4 or later: the ClockSkew-Zero clock-drift trade-off (no
  shipped body mentions it); a test-posture security check pending a user-named
  shipped sentence; the `[ApiKey]`+`[HasPermission]` pairing as a BAD/GOOD
  anti-example candidate (not user-labelled); `GetFallbackPolicyAsync` null as
  an R8 hazard-label candidate (user's call).

## [0.3.17] — Rubric session #2 (solo), 2026-07-28

### Added
- **`dotnet-architecture-review`** — review rubric #2 of four, built under the
  three-way process (verdicts: P1 MERGE, P2 MERGE, P3 MERGE; final consistency
  pass PASS on the skill files + one defect in a coordinator edit fixed, one
  router disambiguation arm added on arbiter recommendation). Checks conformance
  to the ONE house architecture (`Core ← Infrastructure ← Migrators.<Provider> ←
  Web`, `Facades/` × `Modules/`) against the shipped skill bodies — it cites,
  never re-teaches (dotnet-code-review Principle 5 precedent). Body: Overview +
  3 Core Principles + diff/sweep mode table + five audits with numbered checks
  (1.1–1.6 project graph, 2.1–2.3 namespace leaks, 3.1–3.4 presentation
  boundary, 4.1–4.9 facades/modules, 5.1–5.7 composition root) + severity
  calibration + report template + Routing + Decision Guide (411 lines).
  `references/conformance-checks.md` continues the numbering (1.7–1.13, 2.4–2.8,
  4.10–4.15, 5.8–5.12; audit 3 has no tail) plus unnumbered comparison data
  (reference matrix, 21-facade set, module tiers, settings homes, 13 config
  topics) — 436 lines, H1 + TOC.
- Session rulings recorded:
  - **Severity ladder reused** from 0.3.15 (CRITICAL/HIGH/MEDIUM/INFO), cited
    not restated, with architecture calibrations: boundary *crossed* = HIGH,
    shape *inside* a correct boundary = MEDIUM, placement alone never CRITICAL;
    inverted project reference and migrator-name/provider-key mismatch are the
    two CRITICALs. Verdict = PASS/FAIL decided by CRITICAL+HIGH only ("PASS
    (N drift findings)" replaces a proposed fifth vocabulary word).
  - **Kit-anchor divergences (A02 `arch-check`), recorded explicitly:** Step 1's
    four-baseline table collapsed to the one fixed architecture; Step 3's
    standalone cycle audit DROPPED (project cycles cannot survive MSBuild in
    the fixed chain — the real risk, a type-level Facades↔Modules cycle,
    folds into the namespace-leak audit as check 2.1's mutual-naming
    escalation); every Roslyn-MCP step replaced by a manual instruction (C01),
    incl. the comment-out-the-reference → `dotnet build` → read-`CS0246`
    dependency-graph substitute and a RUN-verified `grep -o … | sort | uniq -d`
    duplicate-registration probe.
  - **Banked check 5.8 claimed from `dotnet-code-review`** (per the 0.3.15
    bank): ships here as check 4.9; rubric #1's 5.8 row slimmed to a pointer
    (number kept, `Find:` kept, owner column = the legislating skill
    `module-feature` — arbiter-corrected coordinator edit). Check 5.9 NOT
    claimed (intra-type structure, stays with rubric #1). B's proposed
    entity-base-response check cut as a duplicate of rubric #1's 2.7.
  - **Six false-positive suppressions** shipped as "not a finding" blocks
    (Web-using-Core, module-naming-module, business-shaped facade, big
    `Facades/Common/`, module-without-Startup, existing `Events/` folders) +
    three more in the catalogue (unwired `stylecop.json`, `using MassTransit;`
    in Core, analyzer `Update=` in Core.csproj) — each traced to a shipped
    sentence; the kit's Clean/VSA/Modular-Monolith baselines generate exactly
    these false positives.
  - **Refused for lack of a shipped owner** (provenance law): facade-hosting-a-
    background-worker (banked for `background-worker`); namespace-must-match-
    folder (no shipped sentence — candidate rule for a future fma session);
    `Guid.NewGuid()` on entity keys (verified ORPHAN: `core-contracts.md:40`
    states it, no rubric checks it — banked for rubric #1's data-access area).
  - Shared-blind-spot catches this session: both authors printed the fenced
    chain and severity laws correctly (verified), but Author A miscited three
    body check numbers and Author B one — all caught by cross-reference
    verification; body 4.9's own citation erratum (*When a service grows* →
    *When a service outgrows one file*) was a coordinator catch.
- **Router alignment (same commit, alignment rule 0.3.10):** base-map row for
  `dotnet-architecture-review` (review group; order-note unchanged — "review"
  already covers both rubrics) + NEW disambiguation row "placement / project
  references / the composition root" splitting deciding-where-it-goes
  (`facade-module-architecture`) from checking-conformance
  (`dotnet-architecture-review`) — arbiter-recommended: those tokens now appear
  in two base-map rows.

### Known seams (logged, not fixed here — outside this session's ownership)
- `facade-module-architecture` still prints `Events/` in its module tier list
  (SKILL.md:197, `references/modules.md:26`) — stale against
  `mediatr-messaging`'s `DomainEvents/` ruling; the catalogue ships an explicit
  precedence note. Queued for an fma-owning session.
- `Guid.NewGuid()` sequential-key rule remains unchecked by any rubric — queued
  for a `dotnet-code-review`-owning session (area 1).

---

## [0.3.16] — S17 (Lane C), 2026-07-28

### Added
- **`mediatr-messaging`** — the messaging pipeline: Send/Publish dispatch,
  notification vs request semantics, event/handler folder and naming
  conventions, AddMediatR registration analysis, open-generic handler
  registration, pipeline behaviours (documentation-derived, marked). Built
  under the three-way process; verdicts: P1 MERGE, P2 MERGE, P3 MERGE
  (arbiter-corrected), P4 MERGE; final pass PASS + 1 blocking defect fixed
  (envelope accessibility normalized to `public record` in examples — the
  skill disclaims envelope shape to `module-feature`) + second budget pass
  (553 → 450 lines).
- Session rulings recorded:
  - **`DomainEvents/` is the canonical event folder** going forward (user
    ruling); `Events/` is the legacy name — never create new ones; the only
    shipped instruction about existing ones is that leaving them is not a
    defect (both authors' rename inferences were refused as beyond the
    ruling).
  - **Naming law, three arms** (user ruling, corpus-verified): request →
    replace kind suffix with `Handler` (12 conforming sites); single-handler
    notification → `<EventName>Handler`; multi-handler notification →
    descriptive names (mandatory — 3 corpus events carry 2 handlers each;
    the derived name would collide). The fan-out arm is an arbiter
    correction of both authors' unconditional `<EventName>Handler` rule —
    shared blind spot of the S13b/S16 class.
  - **Controller dispatch is a house default, not a ban** (S15 modality
    precedent; census: 0 of ~20 dispatch sites in controllers).
  - **Handler classes `internal sealed` = recommendation** (canonical
    project 20-vs-6; second project uniformly `public`; the anti-pattern is
    the mix, not either form).
  - **AddMediatR recommendation**: `RegisterServicesFromAssemblyContaining<T>`
    anchored by a dedicated empty marker type — grounded in the verified
    fact that `class Startup` is declared 43 times in the canonical
    Infrastructure assembly, making `typeof(Startup)` binding fragile.
    `Lifetime` analyzed and declined. Corpus call recorded as the
    starting point, per user direction, not the standard.
  - **Open-generic registration (arbiter correction, user-notified, no
    veto)**: the corpus generic handler's type parameter is NESTED inside
    the message type (`Handler<TData> : IRequestHandler<Message<TData>>`);
    the built-in container substitutes positionally and cannot resolve that
    shape — both authors' MS.DI translation was refused; the shipped pattern
    keeps a unifying container (module defines, root invokes). "Arity is the
    trap" also refused — indirection is the trap.
  - **Refused claims** (S16 precedent, API recall unverifiable in corpus):
    Send-with-multiple-handlers behaviour (replaced by "a second
    registration does not give you a second handler"); behaviour
    execution-order sentence (both authors). `Publish` sequential /
    stop-at-first-exception shipped ONLY inside documentation-provenance
    markers; `RegisterGenericHandlers` (12.4+) is an existence note with a
    version caution, not a recommendation.
- Anti-patterns shipped (all six user-labelled): request type in the event
  folder (a 4-type family in the corpus); legacy `Events/` name; descriptive
  name on a single-handler message; suffix-kept handler name
  (`...CommandHandler`); log-and-rethrow in a notification handler (framed
  inside this skill's fence; exception flow routed to `error-handling`);
  mixed handler accessibility in one folder. Declined by user (banked for
  rubrics): dead `params Assembly[]` on a registration extension;
  generic handler branching on `typeof(TData)`.

### Changed
- **`choosing-a-dotnet-skill`** (router alignment, same commit): messaging
  row deleted from *Not yet covered*; new base-map row after
  `module-feature` ("Dispatching a message in-process through MediatR…");
  order-note gains "messaging"; `"message"` row gains a third arm
  (dispatching the envelope and its handler); `a query` row gains a fourth
  arm (dispatch — arbiter-flagged asymmetry, fixed).
- Both manifests bumped to 0.3.16 together.

## [0.3.15] — Rubric session #1 (solo), 2026-07-28

### Added
- **`dotnet-code-review`** — review rubric #1 of four, built under the
  three-way process (verdicts: P1 MERGE, P2 MERGE, P3 MERGE; final consistency
  pass PASS — three defects fixed: dangling nullability cross-reference closed
  by new check 5.13, tool-path inconsistency, build-diagnostics clarification).
  A rubric, not a workflow: Superpowers owns the review process, `/simplify`
  owns cleanup execution; this skill supplies the .NET-specific review
  knowledge both consume. Decision-layer body + `references/review-rubric.md`
  (53 per-area checks: data access, security posture, concurrency,
  integration, correctness, tests) + `references/cleanup-checklist.md`
  (five-category slop taxonomy + the four safe-delete checks).
- **Severity ladder set for all four rubrics** (first rubric session sets it,
  the next three reuse it): CRITICAL / HIGH / MEDIUM / INFO, consequence-based,
  with the calibration "a dropped `CancellationToken` is HIGH by default,
  CRITICAL only when the un-cancelled work corrupts or exposes".
- Session rulings recorded:
  - **"Seal non-inherited classes" is kit doctrine, NOT house law** (user
    ruling) — excluded from the slop taxonomy with a standing note; both
    authors independently imported it from de-sloppify Step 6, the third
    shared-blind-spot catch in the S13b/S15 series.
  - Every check is a manual instruction — "grep X under Y", "open file Z", or
    "build and read the diagnostics"; no analysis server assumed (C01
    degradation stated in Principle 2 and honored in both references).
  - Report shape: every section always appears, `None.` when empty; blast
    radius sets depth; style last or never.
  - Checks cut as unverifiable against any shipped body: `= default` on a
    controller-action token parameter; `request.X!` nullable-by-convention.
    Both banked as anti-example candidates ("plausible .NET instinct dressed
    as house law").
  - Cross-references cite number **and** name so stale numbers self-correct.
- **Router alignment** (mandatory per CHANGELOG 0.3.10; S9b hotfix precedent):
  base-map row for `dotnet-code-review` added to `choosing-a-dotnet-skill`.
- **P2 check 2.1 aligned with the just-shipped `auth-and-security` v0.3.13**:
  `[ApiKey]` recognized as the third explicit access decision so machine-caller
  actions are not false-flagged.
- Flagged to Lane A (not fixed here — ownership): `module-feature/SKILL.md:187`
  and its validator examples (lines 165–172) still carry the superseded
  entity-typed `Messages<T>` form — second instance of the
  `validation-rules.md:322` drift family flagged at S15.
---

## [0.3.14] — S9b hotfix (Lane A), 2026-07-28

### Fixed
- **Router alignment for `auth-and-security`** — the 0.3.13 ship skipped the
  mandatory router merge-time edits (alignment rule, CHANGELOG 0.3.10); found
  by Lane C's post-close audit, fixed by the same Lane A session. Five edits,
  arbiter-reviewed per the S16 precedent:
  - Base map: new row "Authentication and authorization: schemes and tokens,
    permission grants and checks, the current principal, API keys, auth
    secrets" (capabilities group; order-note unchanged).
  - `401 / 403` disambiguation: third arm now routes to `auth-and-security`
    (was *not yet covered*).
  - `## Not yet covered`: the "Permission and identity" reservation row
    deleted.
  - "a Settings class": `SecuritySettings`, `JwtSettings` arm added.
  - NEW disambiguation row "a cache that went stale" (arbiter addition):
    a Redis value not invalidated — `distributed-caching`; a permission check
    still passing after a grant changed — `auth-and-security` — routes the
    revoke-no-evict hazard away from the Redis-flavoured cache row.
  - Recorded non-edits: `ApiKeySettings` arm and "a middleware" token row
    considered and declined (brevity; description matching resolves them).
- Lane-A lane file and the LANE BOARD now carry the alignment rule so no
  future lane ship skips it.

---

## [0.3.13] — S9b (Lane A), 2026-07-27

### Added
- **`auth-and-security`** — Lane A's deliverable (reassigned from Lane B's queue
  at S9 close), built under the three-way loop, coordinator-only main session
  (verdicts: P1 MERGE, P2 MERGE, P3 MERGE + five-patch delta, P4 MERGE,
  P5 MERGE). Body: Overview + 7 Core Principles + Decision Guide (13 rows) +
  4 user-labelled Anti-patterns; three references
  (`jwt-and-tokens`, `permission-internals`, `principal-and-secrets`), all with
  TOCs (580/391/344 lines).
  Session rulings:
  - **JwtSettings divergence (user decree, R7):** canonical settings-class
    shape = ops (`double` expirations + the four `Get*` helpers, UTF-8);
    options multiplicity = apsp (`Default` + one property per client scheme).
    String expirations are the superseded form. Scheme family taught FROM CODE
    (no `User` scheme exists in code despite stale project docs).
  - `Required(params ignoreProperties)` semantics verified: arguments are
    EXCLUSIONS — Issuer/IsAudience are optional; they are stamped into tokens
    (iss/aud) but never validated; the per-scheme signing key is the boundary.
  - `ValidateDataAnnotationsRecursively` provenance: NuGet
    `ReHackt.Extensions.Options.Validation` 7.0.1.
  - Authorization is a DB read: handler takes only the principal id; grant
    tables + IMemoryCache (sliding, per-key eviction on sync verbs only).
    Permission catalogue: code = Resource+Action; implication one level deep,
    expanded after the cache; Guards = single-family seeding presets.
  - Verify middleware reads the established principal, re-checks the row
    per request (not-found/blocked/installation), runs after
    UseCurrentUser and before UseAuthorization
    (order verified at Infrastructure/Startup.cs:103-110).
  - Taught-form departures from canonical code, all declared in honesty notes:
    shared `Configure(JwtBearerOptions, JwtSettings)` extension; generator
    keys via settings helpers; inert `ValidIssuer`/`ValidAudience` and
    `RequireExpirationTime = false` dropped; catalogue lookup dictionary as
    `static readonly` (corpus: computed property rebuilt per access);
    `DefaulTokenGenerate`/`isUser` renamed; neutral catalogue names
    (`AppPermissions`/`PermissionDefinition`/`AppResource`/`AppAction`).
  - Anti-example ledger: 37 candidates recorded in the Lane A log; user
    labelled FOUR for embedding (type-name-as-data with fail-open
    verification; call-site key encoding; revoked-grant-never-lapses;
    committed key material). Security findings held for the rubrics:
    username enumeration at login; `userPermissions` dead const;
    `PermissionsValue` hot-path rebuild; sync-over-async in the auth path.
  - Process: the three-way loop ran with hot-loaded agents; arbiter message
    races produced overlapping verdict outputs (S13b-class lesson recorded:
    quote held text to agents, never cite prior verdicts); one author draft
    reproduced real committed key values from memory — caught by the
    coordinator, contaminated block withheld from the arbiter, final grep
    verified zero real-key matches.

---

## [0.3.12] — S16 (Lane C), 2026-07-27

### Added
- **`automapper-mapping`** — Lane C's S16 deliverable, built under the three-way
  authoring process, coordinator-only main session per `three-way-skill-loop`
  (verdicts: P1 MERGE, P2 MERGE, P3 MERGE, P4 MERGE). Single SKILL.md, no
  `references/` (both authors and the arbiter independently concluded the
  depth is unconditional; future candidates recorded in the Lane C log).
  Session rulings:
  - Placement law (user doctrine, generalized): a profile lives in the file
    that declares the map's SOURCE type; exception — entity→response maps live
    in the response file; never a mapping folder (the facade's empty
    `MappingProfile` is an assembly-scan anchor only). The generalized form
    also covers maps whose source is a type (e.g. an enum) declared inside a
    request file.
  - Naming: `<DtoTypeName>Mapping` (corpus 13 conforming vs 2 abbreviated +
    1 mismatched; the DTO is the source for request maps — Author B drifted
    this three times, arbiter-corrected each time).
  - Projection safety (user doctrine, confirmed BROAD): a map REACHABLE from a
    query projection — transitively via `IncludeAllDerived`/`IncludeMembers` —
    must not use `AfterMap`/`ConvertUsing`; a never-reached map MAY (bare
    permission; two dilution attempts and one widening to `PreCondition` cut).
  - Inheritance (arbiter-corrected shared blind spot): `IncludeAllDerived` at
    EVERY level with configuration to hand down, not only the root; leaf maps
    omit it (four-level corpus chain, two `IncludeAllDerived` sites).
  - Static shared computation: `internal static readonly Expression<Func<T,R>>`
    FIELD on the entity (both authors taught a public expression-bodied
    property — corrected against six corpus declarations).
  - `ConvertUsing` teaches the clean `(src, dest) => src switch` form; the
    corpus's `dest = src switch` assignment is a verified no-op quirk, shipped
    as an anti-pattern.
  - `ReverseMap`: no house ruling (1 canonical site; Author A's
    placement-collision argument disproved at that site) — one Decision Guide
    line, no Patterns section.
  - `PreCondition`: extension-project-only (0 canonical sites) — mentioned,
    downgraded, never prohibited.
  - Anti-examples user-confirmed: profile name pointing at a different type /
    abbreviating the DTO suffix; `ForMember` on a computed get-only property.
  - references/ NOT needed; recorded future candidates: troubleshooting
    catalogue, `IncludeMembers` precedence semantics (deliberately not
    asserted — unverifiable offline), value/type-converter material.

### Changed
- **`choosing-a-dotnet-skill`** (router alignment, same commit): mapping
  disambiguation third arm now routes `automapper-mapping`; `Mapping
  mechanics` row removed from *Not yet covered*; base-map row added for
  `automapper-mapping`; performed Lane B's pre-written testing swap (Testing
  row removed from *Not yet covered*, base-map row added for `dotnet-testing`,
  order note extended `… → mapping → … → capabilities → tests`).
- **`.claude-plugin/marketplace.json`** version aligned (was left at 0.3.10 by
  the 0.3.11 ship — "both manifests agree" rule).

### Known seams (queued, not fixed here — outside Lane C ownership)
- `api-surface`'s description claims "colocated validator and mapping profile"
  but its `Not for:` does not route `automapper-mapping`; reciprocal edit
  queued for an api-surface-owning session.

---

## [0.3.11] — S15 (Lane B), 2026-07-27

### Added
- **`dotnet-testing`** — Lane B's B4 deliverable under the reprioritized queue
  (research variant: no living exemplar — both projects' test projects are dead
  scaffolding per S7b; distilled from the kit's testing/tdd skills + web
  research, adapted to the stack). Three-way process verdicts: P1 MERGE,
  P2 MERGE, P3 MERGE, P3b MERGE (user-directed flow-test addition), P4 MERGE;
  final consistency pass PASS (3 optional notes: note 2 applied — package-table
  row alignment; notes 1, 3 recorded, no action). Body + two references
  (`unit-testing.md`, `integration-testing.md`) — the split the references
  mechanism prescribes, decided at P4. Session rulings:
  - Toolchain settled: xUnit v3 (net8.0 in support; AOT-assert caveat waived),
    Shouldly (FluentAssertions v8+ commercial — banned), NSubstitute (Moq not
    chosen, not banned — modality preserved), Testcontainers + Respawn,
    `UseInMemoryDatabase` banned, Verify snapshots OUT, WireMock declined
    (fake `HttpMessageHandler` instead), MockQueryable considered-and-declined
    (projecting reads route to the integration tier).
  - Fixture points the host at the container via THREE CONFIG KEYS, never
    re-registration (pooled context; `UseDatabase`/`DbProviderKeys` must stay
    the shipped path). Kit's own fixture uses the rejected shape — anti-example
    candidate bank.
  - Test auth: handler mechanism verified against the real middleware (reads
    the established principal; DB re-check means a JWT-user principal needs its
    seeded row). Internals routed to `auth-and-security`.
  - Validator test assertions are REQUEST-TYPED (`Messages<TRequest>`) per
    message-keys v0.3.7 — both authors initially wrote the superseded
    entity-typed form (S13b mirror); root cause: cross-skill drift in
    `module-feature/references/validation-rules.md:322` (stale "T is the
    entity" for rules) — flagged to Lane A, not corrected here.
  - `Received/DidNotReceive` sanctioned in exactly two shapes (guard-rejects,
    catch-must-rollback); banned on happy paths.
  - Unruled candidates banked for the rubrics: message-keys "Which form where"
    table lacks a row for selector-bearing entity-typed service throws;
    validation-rules drift above.
- **Process (user-directed, mid-S15):** Author A drafting delegated from the
  main session to the new `skill-writer-a` agent (`.claude/agents/`); the
  three-way loop codified as project skill `three-way-skill-loop`
  (`.claude/skills/`). Main session is coordinator-only from S15 on.
- **Roadmap row added:** `dotnet-test-report` hook (Group B, post-rubrics) —
  PostToolUse on `dotnet test`, TRX/console parse, auto-report of cases
  run/passed; precedent: kit's `post-test-analyze.sh`; needs the Windows
  polyglot wrapper (02-repo-structure §6).

## [0.3.10] — S15 (Lane C), 2026-07-27

### Added
- **`choosing-a-dotnet-skill`** — the ROUTER (mechanism D from brainstorm §3),
  Lane C's deliverable under the three-way authoring process (verdicts: P1
  MERGE, P2 MERGE, P3 MERGE; arbiter final consistency pass PASS — one defect
  fixed: a falsely-justified H1 removed, 8/9 siblings open at `##`). A single
  decision-table SKILL.md, no `references/`. Session rulings:
  - Two-user goal: sibling disambiguation AND process-phase coverage — the
    router fires when Superpowers brainstorming/plan-writing/subagent-dispatch
    runs on a generic .NET task whose trigger nouns have not surfaced yet.
  - Description uses meta-shaped triggers (uncertainty situations, plan/spec/
    subagent-prompt authoring), not domain nouns — deliberate §5-letter
    tension, spirit-compliant, so the router never competes with the nine
    siblings it routes to. Collapsed two-entry `Not for:` (process layer →
    Superpowers; confidently matched → load directly) — vacuously satisfies
    "name every excluded sibling" since the router owns no content area.
  - Routes ONLY to skills existing on `main` (nine at ship time). One
    `## Not yet covered` section, ten uniform topic-noun rows, two populations
    undistinguished in the artifact: six names dangled by shipped `Not for:`
    lists printed as reservations (`auth-and-security`, `observability`,
    `background-worker`, `automapper-mapping`, `mediatr-messaging`,
    `project-scaffolding` — "nothing to load"), four name-less roadmap areas
    (testing, HTTP resilience, domain modelling, modern C#).
  - The body-scoped obligation (user-approved "must"): each spec/plan/subagent
    step touching an area a SHIPPED skill owns must name that skill inside the
    step; uncovered areas have nothing to name (permission clause: say so in
    the step if it helps the plan). Deliberately absent from the description —
    unscopable at that tier.
  - Single entry-point per row, never sequences; whole-feature conditional:
    `facade-module-architecture` if the module does not exist yet, else
    `module-feature` (corrected against fma's literal description).
  - Disambiguation table may source tokens from skill BODIES
    (`ConcurrencySettings`, `Repository<T>()`) — it exists for tokens
    description-matching cannot resolve; base-map rows stay strictly sparser
    than their target descriptions (compose, never amplify).
  - No section-heading pointers into targets (headings drift). Disclaimer
    principle: a `Not for:` entry is a disclaimer, not an ownership assignment;
    when two shipped pointers differ in grain, the finer one from the area's
    owner governs (`[HasPermission]` usage → `api-surface`, internals →
    not yet covered).
  - `dotnet-testing` merge-time swap pre-written: delete the Testing row from
    Not yet covered; append base-map row "Writing or changing tests: unit,
    integration, fixtures, test doubles → dotnet-testing"; extend the order
    note to "… → capabilities → tests". Three mechanical edits.

---

## [0.3.9] — S9 (Lane A), 2026-07-27

### Added
- **`ef-core-data-access`** — Lane A's second deliverable, built under the
  three-way authoring process (verdicts: P1 NEITHER→merge twice — re-arbitrated
  cold after a parent-session restart so `skill-creator` could be invoked live;
  P2–P5 MERGE). The data-access gateway: repository-over-EF-Core with the
  wrapper as the law (`IRepositoryWrapper.Repository<T>()`, scoped, the kit's
  no-wrapper stance overruled by the codebase), save-per-operation with wrapper
  transactions (`catch (Exception)` + rollback + rethrow — 22/22 real sites),
  `Find(isAsNoTracking:)` as the query gate, thin `ApplicationDbContext`
  (no DbSets, citext extension, global DateTimeOffset→UTC converter),
  options-first `DatabaseSettings` (SqlSettings only; Redis/ES routed to
  Lane C), provider switch with `MigrationsAssembly($"Migrators.{provider}")`,
  the migrations workflow (dev: `dotnet ef` with `-p`/`-s`/`-c`; prod:
  `UseAutoMigration` at boot), seeding (`IDataSeedContributor` as the only
  public seam; bail-out and reconcile idempotency strategies, order between
  contributors not guaranteed), entities & configurations (sequential-GUID
  `BaseEntity`, one-file co-location, `HasBaseEntity().UnderscoreTable()`
  opener, explicit FK pairs, `OnDelete` as a decision question with census
  Cascade 27 / Restrict 14 / SetNull 3), and
  `references/query-conventions.md` (the QueryContainer search shape,
  operator grammar verified against the parser, the get shape, and the honest
  five-round-trip cost of the canonical search chain).

### Rulings recorded for reuse
- `ICode`/`HasCode` ruled OUT of the skill entirely — citext taught via
  `HasCitextUnique` directly; hand-rolled citext-unique without the interface
  is explicitly allowed.
- Collection navigations: non-nullable `ICollection<X> Xs { get; set; } =
  default!`; reference navigations stay nullable.
- Single-style opener: `HasBaseEntity().UnderscoreTable()` (7:4 census, the
  minority order confined to one module).
- `GetByIdAsync(params object[])` does not exist in this skill's world
  (ops-service repository shape is the source of truth).
- Entity-static `Expression` members and entity-file AutoMapper Profiles are
  omitted entirely (module-feature's Expressions/ and the future
  automapper-mapping own them).
- Get-single is taught without `Include` — `ProjectTo` ignores it; the real
  call site's Include chain is a labelled anti-example.
- Anti-examples labelled for future review rubrics (real paths in the lane
  log): transaction-ct drops (12/29 sites), BrandService rollback-cleanup-wrap,
  sync `Any()` in async seeders, the three `entities.Any()` probes in the
  query helpers, ApplyFilter's silent catch + Console.WriteLine, sync
  `Count()` dropping its ct in ToPagedListAsync, QueryContainer.Validate
  blaming PageSize for Current, response DTOs inheriting `BaseEntity<Guid>`.

---

## [0.3.8] — S14 (Lane C), 2026-07-27

### Added
- **`distributed-lock`** — Lane C's third deliverable, built under the three-way
  authoring process (A/B independent drafts per piece; `skill-arbiter` invoked
  the installed `skill-creator` live after a parent-session restart; verdicts:
  P1 MERGE, P2–P4 MERGE B-dominant; user adjudicated through P3, standing
  delegation applied from P4). Owns distributed mutual exclusion: the
  `ConcurrencyHandlers` capability (`IConcurrencyHandler`, two `LockedAsync`
  overloads, `ConcurrencyHandlerOptions`, `ConcurrencySettings`), provider
  choice (SemaphoreSlim honestly framed single-instance-only vs RedLock — every
  production call site passes RedLock explicitly), lock-key discipline
  (`{Noun}:{id}`, private static helpers, no central factory, no CachePrefix),
  the ExpiryTime/WaitTime/RetryTime doctrine, and `LockedException` (423) as the
  cited contract routed to `error-handling`. Decision-layer body plus two
  references (`implementation.md` with the full scaffold bodies;
  `usage-patterns.md` with the three production patterns).
- **Rulings recorded for reuse:** the third in-memory provider option is
  scrubbed entirely (enum member, dispatch branch, package — no mention
  anywhere); the scaffold reads the EXTRACTED `RedisSettings` section owned by
  `distributed-caching` (deviation from the canonical `DatabaseSettings`
  nesting; that section + `Required()` + the `LockedException` family are STOP
  prerequisites); `ConcurrencySettings.Provider` is dead config — honest note,
  no normalization, no invented fallback; the semaphore-registry cleanup race
  (`TryRemove`/`GetOrAdd`) is an honest note, canonical code kept, not a
  BAD/GOOD pair; authorized normalizations: settings filename typo, Vietnamese →
  English XML docs, single-key `RedLock` → `RedLockAsync` rename, and — in
  usage-patterns only — the Pattern 3 catch filter gains
  `and not LockedException` (the canonical filter compensates work that never
  started and downgrades a retryable 423 to a 500); the ExpiryTime mid-work
  release is taught as the canonical's documented intent with an explicit
  non-assertion about client auto-renewal (unverified for the pinned version);
  placement asymmetry named once (lock at `Common/Services/`, cache at
  `Common/` — both canonical, don't move existing folders); lock keys may carry
  two ids when the guarded resource is the pair; drift noted once (one canonical
  call site passes a bare Guid key).

## [0.3.7] — S13b (Lane B), 2026-07-27

### Added
- **`message-keys`** — third Lane B deliverable, built under the three-way
  authoring process (A/B independent drafts per piece; `skill-arbiter` ran with a
  LIVE skill-creator invocation after a mid-session plugin install plus parent
  session restart; verdicts: P1 MERGE, P2 NEITHER with arbiter-corrected
  doctrine, P3 MERGE). Owns the `Messages<T>` key grammar: key anatomy
  (`Mes.{Module}.{Rest}`), the success/action helper family, the `Action`
  overload family and its no-default trap, the 15-member `MessagesType` matrix,
  `[MessageDisplay]`, and which form is used where. Decision-layer body plus
  `references/key-grammar.md`.
- Session rulings recorded:
  - Single-style doctrine: `Messages<T>.X(selector)` is THE validator-message
    law; the `WithMessage(MessagesType.X)` extension is legacy — recognised when
    reading, never written new.
  - `[MessageDisplay]` and the selector lambda are complementary, not competing:
    every request class carries `[MessageDisplay(nameof(Entity))]`; validator
    messages are request-typed; cross-entity existence checks entity-typed.
  - Two generics, two jobs (arbiter-discovered against BOTH authors' drafts):
    requests type validator messages, entities type outcome messages —
    corpus-verified (zero request-typed success calls in either project; the
    written convention's request-typed worked example ruled drift against its
    own repo's code).
  - Growth-by-reuse: the `MessagesType` enum is closed; the action family may
    grow — an unnamed action starts as `Action("X", true)`, and promotion to a
    dedicated facade helper is permitted (not required) once the action recurs
    across modules (Approve/Reject/Cancel named as typical).
  - The enum is authoritative at 15 members; the written convention's 14-item
    list is stale. Overload coverage is non-uniform and a missing shape is
    final (the absence pattern tracks the enum's own resource/value split).
  - Older entity-typed validation of a request's own properties acknowledged in
    one clause as superseded.
  - Sanctioned anti-example (generic form only): a request class without
    `[MessageDisplay]` leaks its type name into every key.

## [0.3.6] — S11 (Lane C), 2026-07-26

### Changed
- **`distributed-caching`** — `references/usage-patterns.md` now names the real search
  facade type `IElasticSearchWrapper` in the pipeline-handoff producer example instead
  of the sanitized `ISearchWrapper`, per the cross-skill vocabulary ruling made when
  `elasticsearch-search` shipped.

## [0.3.5] — S11 (Lane C), 2026-07-26

### Added
- **`elasticsearch-search`** — Lane C's second deliverable, built under the
  three-way authoring process (A/B independent drafts per piece, `skill-arbiter`
  file-verified verdicts: P1–P4 all MERGE; user adjudication through P2, delegated
  auto-approval from P3 onward). Owns the Elasticsearch facade
  (`IElasticSearchWrapper`, `ElasticSearchRepositoryBase<T>`, `AddElasticsearch`,
  `IndexSettingsMapper<T>`) and the `ElkEntities/` convention — `Elk*` documents,
  never index a DB entity, the root-vs-embedded document distinction, projection
  profiles, query and re-index patterns. Decision-layer body plus two references
  (`implementation.md` with the full corrected scaffold bodies; `usage-patterns.md`
  with document authoring, read/write-back patterns and the single authorized
  blocking-pair anti-example).
- Session rulings recorded: `ElasticsearchSettings` stays nested in `DatabaseSettings`
  (deliberate divergence from the Redis extraction); two-folder facade anatomy taught
  as-is; `ElkBaseEntity` normalized into the facade; canonical registration split kept
  (wrapper registered in the persistence facade's `Startup`); `Querry` spellings
  corrected with an honest note; blocking `Search(out)`/`BulkAll` omitted from the
  scaffold and treated as the sole BAD/GOOD pair.

## [0.3.4] — S13 (Lane B), 2026-07-26

### Added
- **`error-handling`** — fifth skill, second Lane B deliverable, built under the
  three-way authoring process (A/B independent drafts per piece, `skill-arbiter`
  file-verified verdicts: P1 MERGE, P2 MERGE, P3 MERGE B-dominant; user delegated
  adjudication after P1). Owns: when to throw which of the four sealed exceptions,
  catch/bubble/wrap doctrine, the exception middleware's envelope contract
  (`ErrorResultWrapper`), and the growth sanction's boundary. Decision-layer body
  plus one reference (`middleware-behavior.md`: full handler walkthrough, the three
  shaping paths, diagnostics fields, `HiddenProperties` redaction mechanics,
  pipeline position and its consequences, the dedicated-catch anti-example, and a
  symptom→cause troubleshooting table).
- Session rulings recorded: **business not-found = `BadRequestException` 400** (no
  `NotFoundException`, ever; 404 stays routing's answer per `{id:guid}`);
  current-principal-not-found → `UnAuthorizedException` 401; `ForbiddenException`
  documented honestly (zero throw sites — real 403s are bare authorization
  short-circuits, never enveloped; reserved leaf for a domain-decided enveloped
  403); **bubble by default** — wrap into `InternalServerException(message, inner)`
  only when the catch adds context, `(ex.Message, ex)` is not house style; growth
  sanction boundary — a status-pinning sealed leaf is free, a payload-carrying
  exception demanding middleware compensation is outside the sanction; the
  "middleware is the only producer" doctrine scoped to *thrown* exceptions, with
  the invalid-model-state `{ message }` carve-out named (Web's
  `InvalidModelStateResponseFactory`, consistent with the architecture skill's
  Web-owns line; validator rules → `module-feature`).
- Labelled anti-examples (user-confirmed): a leaf constructor that fails to pin
  `StatusCode` (latent status-0 defect); the middleware's dedicated file-upload
  `catch` that compensates but never writes a response — framed as a defect of the
  shared shape (present, identical, in both reference codebases).
- Roadmap: **`distributed-lock` row added** at S13's open by user direction
  (lane-ownership exception) — it owns `ConcurrencyHandlers` and `LockedException`
  (423); `error-handling` cites 423 only as the growth worked example and routes
  lock semantics there.

### Changed
- Merge-time alignment with S8 (which landed on `main` mid-session): every
  `cqrs-feature-slice` route in `error-handling` (description `Not for:`, body
  carve-out, `Not this skill`, reference) ships as **`module-feature`**, per the
  S8 rename ruling.

## [0.3.3] — S8 (Lane A), 2026-07-26

### Added
- **`module-feature`** — fourth skill, Lane A's refounding of the `cqrs-feature-slice`
  charter under its new name (user ruling: MediatR is in-process messaging, not CQRS,
  so the old name lied). Built under the three-way authoring process (A/B independent
  drafts per piece, `skill-arbiter` file-verified verdicts: P0–P6 all MERGE; user
  adjudication through P3, then blanket delegation). Owns writing one feature inside a
  module: the service file (one file interface+implementation, `IScopedService` on the
  interface, suffix partials, `Services/` purity with two authorized dumping-ground
  inventories), request/response files (one-file law, tiers, facade bases),
  `<X>Validation.cs` (IsExist… predicates / ThrowIf… guards, symmetric boundary), and
  thin MediatR envelopes (`internal sealed`, handler-delegates-to-service absolute).
  Decision-layer body (282 lines) + four references (`service-growth.md`,
  `request-response-families.md`, `validation-rules.md`, `mediatr-envelopes.md`).
- Session rulings recorded: ct mandatory on every service operation (`= default`,
  last parameter; private helpers required-no-default); XML `<summary>` law (English,
  no TODOs); response tier suffix naming with strict chain; `DeleteRange<X>Request`
  standard; Expressions/-mandatory for business-computed members; `IsExist…` prefix
  law; envelope visibility law with the `internal`-blocks-Web (not module-vs-module)
  mechanism; `GetByIdAsync`-token trap documented.

### Changed
- **Rename ripple:** `cqrs-feature-slice` → `module-feature` across
  `facade-module-architecture` (description + Not-this-skill) and `api-surface`
  (description Not-for, Overview, body, `request-response-dtos.md`) — routing text
  otherwise untouched; api-surface's "validation rules" hand-back preserved.
- Cross-lane alignment: `module-feature`'s request/response piece amended to the
  shipped api-surface DTO chain law (base request Profile only when customized);
  open `[MessageDisplay]` vs `Messages<T>`-lambda conflict logged for `message-keys`.

## [0.3.2] — S12 (Lane B), 2026-07-26

### Added
- **`api-surface`** — third skill, first Lane B deliverable, built under the three-way
  authoring process (A/B independent drafts per piece, `skill-arbiter` file-verified
  verdicts: P1 MERGE, P2 MERGE + user-approved errata, P3–P5 MERGE, user adjudication
  throughout). Claims routes, request/response DTO base-class chains, versioning stance
  (**none**), Swashbuckle/OpenAPI, and controller endpoint-writing conventions.
  Decision-layer body plus three references (`endpoint-anatomy.md` with two worked
  controllers and two authorized anti-examples; `request-response-dtos.md` with both
  DTO chains, the conditional base-Profile rule and pagination contracts;
  `openapi-swashbuckle.md` with the full facade walkthrough and a debugging table).
- Session rulings recorded: expression-bodied endpoints only (body-style hand-off from
  `facade-module-architecture` explicitly claimed); signature wrapping counts every
  parameter including the token; strict binding sources on new endpoints; `{id:guid}`
  always; suffix-partial base-list law with three named anti-patterns; request
  inheritance law (base-first, lookup before defining) and response ladder rooted at
  `BaseEntity`/`ElkBaseEntity`; base request `Profile` only when customized
  (`.IncludeAllDerived()`), response base rungs always carry it;
  `PaginationResponse` as the only list envelope, `MoreInfo` = companion computed by
  the same search; `[HasPermission]` single constructor, three call shapes, positional
  trap documented; `Messages<T>` text conventions assigned to a **dedicated future
  `message-keys` skill** (neither api-surface nor error-handling).

## [0.3.1] — S10 (Lane C), 2026-07-26

### Added
- **`distributed-caching`** — second skill, first Lane C deliverable, built under the
  three-way authoring process (A/B independent drafts per piece, `skill-arbiter`
  file-verified verdicts: P1 MERGE, P2 NEITHER-redraft, P3 MERGE, P4 MERGE, user
  adjudication throughout). Canonical source: the user-designated
  `Facades/Common/RedisCaches` cache exemplar (RedisCache only). Decision-layer body
  plus two references (`implementation.md` scaffold with pre-scaffold guard and two
  STOP prerequisites; `usage-patterns.md` with the handoff read-once and cache-aside
  patterns, one authorized anti-example, and the HybridCache
  considered-not-adopted ruling).
- Session rulings recorded: cache facade taught at `Facades/Common/RedisCaches/`
  (scaffold-if-missing); normalized anatomy (`internal` Startup, Options four calls,
  `RedisSettings` extracted from `DatabaseSettings` per settings-follow-their-service,
  entry point `AddRedisCache`); named key legal for singleton rows; Redis queue
  scrubbed from the skill entirely; canonical `IValidatableObject` +
  `validationContext.Required()` validation with the helper as a STOP prerequisite.

## [0.3.0] — S7b, 2026-07-26

### Changed
- **`facade-module-architecture` rebuilt from scratch** under the three-way authoring
  process (independent A/B drafts, `skill-arbiter` verdicts with file-verified reasons,
  user adjudication per piece). Evidence base re-founded on the user's per-area canonical
  designations (`ops-service` as the base project for solution/Core/facade-startup/
  composition-root conventions; `apsp-backend` as the production source for modules and
  controllers; `be-booking` for one anti-example only). All six recorded defects of the
  0.2.0 version are fixed, plus rulings made this session: two lifetime markers (no
  singleton marker), four sealed exceptions with no serialization ceremony on new ones,
  unified suffix-partial law (base list only in the suffix-less core file — services and
  controllers alike), `Enums/` unconditional, `<X>Validation.cs` naming, settings follow
  their service, `Facades/Common` fractal growth with reach-not-size placement,
  Expressions write-once, no `Mappings/` folder.
- **New shape:** a ~300-line decision-layer body plus six verbatim-approved
  `references/` files (`solution-layout`, `core-contracts`, `facades`, `modules`,
  `composition-root`, `web-controllers`), replacing the three 0.2.0 references.
  Nine authorized anti-examples live in the references, none in the body.
- **Description voice settled** by the arbiter loading Anthropic's official
  `skill-creator`: third person, under 100 words, trigger-noun "pushy", `Not for:`
  routing list. `docs/02-repo-structure.md` §5 rewritten to match; the shipped
  description is the reference example.

## [Unreleased] — process change, 2026-07-26

**No version bump, deliberately.** No plugin component changed — the version is the only
signal an installed copy is stale, and bumping it would mint a fresh 41 MB cache directory
for a docs-and-tooling change. Everything here is process.

### Added
- `.claude/agents/skill-writer-sp.md` and `.claude/agents/skill-arbiter.md` — the second and
  third authors in the new three-way skill authoring process. **Project tooling, not plugin
  content**: they live in `.claude/agents/`, never in the plugin's `agents/`, because triage
  settled that exactly one agent ships (`ef-core-specialist`, B18). Neither agent may write a
  file; both return draft text so nothing is written before the user approves. **Amended same
  day at the user's direction: all three participants — main session, writer, arbiter — read
  the user-named exemplar files in `reference/projects/` directly, with equal access.** The
  first design fed the agents only material pre-digested by the main session, which made every
  draft inherit one reading of the code and left the arbiter judging two drafts that shared
  one pair of eyes. Equal capability, differing only in loaded methodology; the reading
  discipline (user names the files, widening requires asking, no bulk scans, R7, Bash not
  Glob) binds all three identically.
- `docs/03-session-roadmap.md` — the **three-way authoring process**, mandatory from S7b
  onward. Main session drafts A from the repo's own rules; `skill-writer-sp` drafts B from
  Superpowers' `writing-skills`; `skill-arbiter` decides using Anthropic's official
  `skill-creator`. Piece by piece, explained in Vietnamese, user-approved before any write.

### Changed
- **R7 canonical source re-designated: `apsp-backend`, not `ops-service`.** The user
  identified `ops-service` as a base project rather than production. `apsp-backend`
  **confirms** the Facade/Module architecture — identical project graph, identical `Core`
  shape, identical two-axis split, identical 13-file configuration load order — so **Q1's
  answer stands**. Six details change, and `facade-module-architecture` is queued for rebuild
  in S7b.
- `docs/02-repo-structure.md` §5 — the description **voice** is now marked contested and
  explicitly undecided. Second person (§5) versus third person (the user's own
  `skill-creator` convention). Assigned to the arbiter, to be settled with reasons *after*
  drafts exist. Anti-triggers remain the settled part of the rule.

### Known issues in the shipped skill, pending the S7b rebuild
- `references/dependency-injection.md` documents two DI marker interfaces; `apsp-backend` has
  **three** (`ISingletonService` is missing).
- The principle *"`Core` holds primitives only"* is **wrong**. `Core` also holds an exception
  hierarchy and result wrappers.
- The one shipped anti-example — target-framework drift — **does not reproduce in
  `apsp-backend`**, where every project including both test projects targets `net7.0`. It was
  an `ops-service`-only defect and loses its standing unless re-authorised on new evidence.

### Notes
- **Definitions do not load in the session that creates them.** A newly written agent type is
  undispatchable until restart — the same constraint S6 measured for hooks, now confirmed to
  generalise to project-level agents. This is why S7 stopped at defining the process rather
  than exercising it.
- `apsp-backend` ships **eleven skills of its own**, now designated the highest-tier
  `from-my-code` source. `dotnet-standards` generalises an existing personal convention rather
  than writing on a blank page.
- **S8's blocking question is answered by the user's own written rule:** MediatR is
  *in-process messaging, not CQRS read/write separation*. The `cqrs-feature-slice` gateway as
  named describes a pipeline the user does not run.

---

## [0.2.0] — 2026-07-26

The first knowledge session. Q1 — open since S0 — is answered from real code, and
the first skill ships on top of the answer.

### Q1 resolved — the architecture has a name

The architecture is a **three-project chain** — `Core` → `Infrastructure` → `Web`,
with `Migrators.<Provider>` between the last two — whose `Infrastructure` project is
split on **two axes**: `Facades/` for technical capabilities and `Modules/` for
business ones. Every facade wires itself through a `Startup.cs` exposing
`AddX()`/`UseX()`, composed into a single flat fluent chain.

**It is not Clean Architecture and not Vertical Slice Architecture.** `Core` holds no
entities, business logic lives inside `Infrastructure`, and there is no `Domain` or
`Application` project — so it is not "close to" Clean Architecture either. Three
skipped triage rows (A03, A08, A35) are confirmed by this answer rather than reopened.

### Added
- `skills/facade-module-architecture/SKILL.md` — the first skill in the plugin, and
  the gateway that answers "where does this file belong?". Its description carries
  **anti-triggers** naming six sibling skills to use instead.
- `skills/facade-module-architecture/references/solution-layout.md` — solution and
  build files, package-version discipline, and the six solution-hygiene checks
  (TRIAGE A28 + D07 + B28).
- `skills/facade-module-architecture/references/configuration-and-options.md` — the
  Options pattern, startup validation, and the per-capability configuration-file
  convention (TRIAGE A10).
- `skills/facade-module-architecture/references/dependency-injection.md` — lifetimes,
  the captive-dependency bug, keyed services, and where registration lives
  (TRIAGE A14).

### Changed
- Gateway renamed **`solution-architecture ⚠️` → `facade-module-architecture`**
  across TRIAGE rows A10, A14, A28, B28 and D07, and in `00-brainstorm.md` §4.
  Historical decision-log entries keep the old name — the log is append-only.
- `00-brainstorm.md` §8: **Q1 closed**. Roadmap S7 row marked complete.
- TRIAGE **A05** and **A33**: setup sites traced from named-but-unverified to
  concrete paths, and confirmed to exist. Traced only — neither is distilled here.

### Fixed
- **TRIAGE A33 carried a wrong configuration path.** The row claimed Serilog is
  configured from a `Serilog` section in `appsettings*.json`. No such section exists;
  configuration is a strongly-typed POCO bound from a per-capability `logger.json`
  and applied imperatively in code. Corrected in the row and logged.

### Notes
- **One anti-example ships**, adjudicated by the user rather than assumed:
  target-framework drift. Four further divergences from the kit (no central package
  management, no `global.json`, a rules-free `.editorconfig`, classic `.sln`) are
  recorded as **observed conventions — neither endorsed nor faulted**, per R7's
  "label, don't blend".
- All version-specific content is dated **2026-07-26**. The stack targets **.NET 8**,
  not the kit's .NET 10. Next R10 trigger: **.NET 11 GA, 2026-11-10**.
- `NOTICE` unchanged — no new *kind* of artifact carries kit material; the three
  `references/` files are derived components already covered.

---

## [0.1.0] — 2026-07-26

The scaffold. No .NET knowledge ships in this version; this is the plumbing that
makes everything else installable.

### Added
- `.claude-plugin/plugin.json` and `.claude-plugin/marketplace.json` — the plugin
  manifest and the local development marketplace (`dotnet-standards-dev`).
- `hooks/post-edit-format` — the one hook that survived triage. Runs
  `dotnet format` scoped to the nearest `.csproj` after every `.cs` edit.
  Extensionless by necessity: Claude Code on Windows prepends `bash` to any
  command containing `.sh`.
- `hooks/run-hook.cmd` — polyglot CMD/POSIX wrapper. Copied in pattern from
  Superpowers, never referenced across plugins.
- `hooks/hooks.json` — rebuilt manifest, one `PostToolUse` entry. Auto-loaded, so
  it is deliberately **not** declared under `plugin.json`'s `manifest.hooks`.
- `hooks/README.md` — the three-kinds hook taxonomy, the Windows cost, and the
  rule that a hook may ship only if its silent absence is benign.
- `NOTICE` — two MIT attributions: `codewithmukesh/dotnet-claude-kit` at the
  pinned commit, and the wrapper pattern from Superpowers.
- Empty `skills/` and `agents/` directories for the components S7–S8 will build.

### Fixed
- `post-edit-format` — the reference kit's `dotnet format "$PROJECT" --include "$FILE"` call
  formats **nothing** on Windows, silently. Two independent causes, both measured on
  .NET SDK 10.0.301: an absolute project path with forward slashes triggers
  *"Skipping referenced project"*, and `--include` only matches paths relative to
  the current working directory. Now runs from the project directory with
  relative paths. See `hooks/README.md`.
- `post-edit-format` — the project walk now recognises `.slnx`, the `dotnet new sln`
  default since .NET 10, alongside `.sln`.

### Verified
- **Live confirmation, closing the one gap S6 could not close itself.** S6 proved
  the hook worked by running `run-hook.cmd` directly; it could not prove the
  hook fires *inside a live Claude Code session*, because the session that
  installs a plugin predates its hooks. Confirmed 2026-07-26 in a separate test
  project, project-scoped install (`--scope local`), fresh session after
  restart: writing a `.cs` file with irregular indentation through Claude's
  Write tool triggered `post-edit-format` and the file came back re-indented
  to 4-space / brace-on-own-line convention. **The hook fires end to end.**

### Notes
- **Installing this plugin copies the whole source directory and ignores
  `.gitignore`** — including `reference/`, which holds the kit clone and the
  author's real projects. First install copied 39 MB against a ~330 KB plugin.
  No exclusion mechanism exists for a `directory` marketplace source. Two
  candidate fixes are recorded in `docs/02-repo-structure.md` §4; neither is
  chosen yet because both change what §1 specifies. Until then, delete
  `reference/` from the cache copy after each install.
- **Install copies, it does not link.** Editing this repository changes nothing in
  the installed plugin until uninstall → install → restart.
- **No `mcpServers` block in `plugin.json`.** `CWM.RoslynNavigator` is kept as an
  externally installed dotnet tool, not bundled. Its install command and
  `.mcp.json` shape become a `references/` file in a later version.
- **No `commands/` directory**, by design — see `README.md`.
- **This plugin's knowledge layer carries dated content.** .NET/C# version
  guidance, breaking-change notes, package versions and commercial-licence
  boundaries all expire. The nearest known expiry is **.NET 11 GA,
  2026-11-10**. Treat a stale "current as of" line in `README.md` as a defect,
  not as cosmetics.
