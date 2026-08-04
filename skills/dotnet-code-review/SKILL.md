---
name: dotnet-code-review
description: >-
  This skill should be used when reviewing changed .NET or C# code: a code review
  or PR review, "review my changes", scoring blast radius, ranking findings by
  severity, or listing cleanup, slop and simplification candidates — dead code,
  stale TODOs, dropped CancellationToken, over-build, unnecessary complexity —
  before merge. Not for: the review process — superpowers:requesting-code-review,
  superpowers:receiving-code-review; executing cleanup or simplification —
  /simplify; layering, dependency direction — dotnet-architecture-review; secrets,
  injection, data exposure — dotnet-security-review; N+1, allocation, blocking —
  dotnet-performance-review; JWT, policies, permission internals —
  auth-and-security; profiles, CreateMap — automapper-mapping; the conventions
  checked — ef-core-data-access, module-feature, api-surface, error-handling,
  message-keys, dotnet-testing, facade-module-architecture.
---

## Overview

This is a **rubric, not a pipeline.** It says what to check in changed .NET
code, in what order, and how to rank what it finds. It does not run the review:
the process — gathering the diff, dispatching the reviewer, receiving and
triaging the feedback — belongs to `superpowers:requesting-code-review` and
`superpowers:receiving-code-review`. Executing cleanup belongs to `/simplify`.
This skill supplies the .NET-specific judgement those steps consume.

It also does not teach doctrine. Every convention it checks is owned by a
knowledge skill, and the rubric's job is to compare the diff **against** that
skill and cite it — never to restate its rules from memory. When a finding
needs the rule spelled out, load the owning skill (see *Routing*).

## Core Principles

1. **Depth follows blast radius, not line count.** A one-line change to the
   exception middleware can break every error response in the service; a
   300-line rename cannot break anything the compiler would not catch. Score
   the change first, then decide how much reading it earns — otherwise review
   effort tracks diff size, which is uncorrelated with risk.

2. **Every check is a manual instruction.** There is no code-analysis server in
   this stack, and no rule here may assume one. A check that cannot be written
   as "grep X under Y", "open file Z and look at W", or "build and read the
   diagnostics" is not a check — it is a wish. This is stated once and holds
   for this body and both references. Three consequences worth naming:

   - **"Who calls this?" is a grep for the symbol name across the solution**,
     plus a second grep for string-based use — reflection, convention-based DI
     registration, serialization, configuration files — because a name can be
     used without ever appearing in a call.
   - **"Is this new?" is a diff question**, answered by comparing against the
     base branch, not by scanning the file.
   - **Read the whole file, not just the hunk**, for anything scored Critical
     or High. The diff hides the constructor, the base class and the attribute
     that decide whether the hunk is correct.

3. **One severity vocabulary, four words.** CRITICAL / HIGH / MEDIUM / INFO,
   used in the findings, in the report headings and in any follow-up issue. Two
   parallel vocabularies (`Warnings`, `Suggestions`, `nits`) make findings
   incomparable across reviews and let a reader mistake a HIGH for a nit
   because it landed under a soft heading.

4. **Style is reviewed last, or not at all.** Formatting, naming casing and
   using-ordering are owned by the formatter and the analyzers; a human raising
   them spends the reader's attention on the cheapest possible finding. Never
   let a naming nit appear above a data-corruption finding in the same report.

5. **The rubric cites the owning skill; it never re-teaches it.** A finding
   reads "the read is not `isAsNoTracking:` — see `ef-core-data-access`", not a
   paragraph re-deriving why tracking costs. This keeps the rubric short, keeps
   one source of truth per convention, and makes a stale rubric row visible as
   a contradiction rather than as a second opinion. A check that cannot be
   traced to a shipped skill's body — or to a defect that is a defect in any
   codebase — is not a finding.

6. **Report what changed, not what was already there.** Pre-existing issues in
   a touched file are INFO at most, unless the change makes one reachable or
   worse. A review that relitigates the whole file is a review nobody finishes.

## Blast radius sets depth

Identify what changed (`git diff main...HEAD --stat`, or the files named), score
every changed file, take the highest, and review the whole change at that depth.

| Blast radius | Changed here | Depth |
|---|---|---|
| **Critical** | Middleware (exception, authentication, request pipeline); authentication/authorization wiring; EF migrations; anything in `Core` (base entity, exception hierarchy, response wrappers, shared contracts); the composition root | Thorough — read every code path, and every consumer of what changed |
| **High** | A public HTTP contract (route, request or response DTO on an existing endpoint); `DbContext` or an `IEntityTypeConfiguration`; the repository wrapper; a new module or a new facade | Focused — behaviour plus every caller of the changed surface |
| **Medium** | A new feature inside an existing module following the existing shape; a bug fix inside one service; a new action on an existing controller | Standard — one full pass of the priority order below |
| **Low** | Docs, renames, formatting, log statements, XML summaries, test-only edits | Glance — build and tests green, no behaviour hidden in the diff |

Three rules make the table hold. A change is scored by **what it touches, not
by who wrote it or how routine it feels**: a migration is Critical the tenth
time as much as the first. **A Low-radius diff containing one Critical-radius
file is a Critical review** — the highest row wins, always. And **state the
score in the report Summary**: a reader who disagrees with the score is a much
faster correction than a reader who disagrees with the findings.

## Severity ladder

| Severity | Meaning |
|---|---|
| **CRITICAL** | Must fix before merge. Exploitable, corrupting, or already wrong: data loss, a missing authorization gate, a transaction that cannot roll back, a response leaking what it must not, a change that breaks an existing consumer as written. |
| **HIGH** | Must fix before merge, or merge with a named owner and a filed follow-up. Breaks a settled house convention, or fails predictably under load or on a second request. |
| **MEDIUM** | Should fix; acceptable to defer with a tracked issue. Real tech debt with no imminent incident — duplication, a missing test for a branch that exists, a convention drift with a contained blast radius. |
| **INFO** | Observation, no action required. Context, a pre-existing issue noticed in passing, or something done well. |

Assign severity from **consequence, not from effort**: a one-character fix to a
missing authorization check is CRITICAL, and a large refactor that would merely
tidy naming is MEDIUM. If a finding cannot be given a severity, it is not yet a
finding — say what breaks, or drop it.

Calibration, because this one is argued every time: **a dropped
`CancellationToken` is HIGH by default.** It wastes server work on a
disconnected client, which is a predictable failure under load, not corruption.
Raise it to CRITICAL only when the concrete consequence is corrupting or
exploitable — an uncancellable write inside a transaction, say.

## Priority order

Work the areas in this order and stop descending when the review budget set by
blast radius runs out. The order is by incident frequency: the top four are
where production breaks, the last two produce candidates for `/simplify` rather
than merge blockers, and below them is where taste lives.

| # | Area | The rubric checks | Owning skill |
|---|---|---|---|
| 1 | **Data access** | Query shape and tracking, projection, includes, pagination, transaction boundaries, save-per-operation, the token reaching the database call, migration safety | `ef-core-data-access` |
| 2 | **Security** | Every action's authorization attribute is explicit, input is validated before use, nothing secret is in source or logs, no entity reaches the wire un-projected | `auth-and-security`, `api-surface` |
| 3 | **Concurrency** | `CancellationToken` accepted and propagated to the end of the chain, no `.Result`/`.Wait()`/`.GetAwaiter().GetResult()` inside an async method, no scoped dependency captured by a singleton, shared mutable state, double-processing | `distributed-lock` (double-processing); the rest is general .NET, owned by no skill |
| 4 | **Integration** | Timeouts and failure handling on outbound calls, cache invalidation after the mutation that invalidates it, index writes after the entity change, no exception swallowed into a log line | `distributed-caching`, `elasticsearch-search` |
| 5 | **Correctness** | Guard clauses and their order, empty and null cases, the exception type chosen for the failure, catch filters that do not re-wrap an exception already carrying the right meaning, the message key attached to each outcome, dead code left in front of the real path | `error-handling`, `message-keys`, `module-feature` |
| 6 | **Tests** | The changed behaviour has a test at the right tier, and the test asserts an outcome rather than a call | `dotnet-testing` |
| 7 | **Simplicity / over-build** | Code the change adds that the task in front of it does not need, a helper written where one already exists, a shape more elaborate than the owning skill requires — capped at MEDIUM, collected as candidates, never rewritten here | `references/review-rubric.md` area 7; each check names the skill that owns the shape |
| 8 | **Cleanup / slop** | Unused usings, new analyzer warnings, dead code, stale TODO/HACK/FIXME, dropped tokens — collected as candidates, never deleted here | `references/cleanup-checklist.md` |
| — | **Style and naming** | Only after all of the above, and only when a formatter or analyzer does not already own it | — |

## The report

If this review produces a report, write it to a file under `docs/code-review/`
in the reviewed repository (create the folder if absent) — the file, not the
chat copy, is the deliverable.

One report, the severity words from the ladder as its headings, always in this
order. **Every section appears every time**; write `None.` when a section is
empty. An absent section is ambiguous between *checked, found nothing* and *did
not check*, and a report whose shape varies cannot be compared with the last one.

**`Check coverage` is the section that keeps the rest honest.** The three sibling
rubrics each carry one — *Audit*, *Layer*, *Area* coverage — and this one is
theirs: it names the check numbers that ran. Fill it from the checks you
actually executed, not from the areas you meant to cover; a check silently
skipped because it needed a second file opened is the single most common way a
long rubric ships a clean-looking report over an unexamined defect.

```markdown
## Code review: <scope>

### Summary
<1-3 sentences: what changed, the blast radius scored and why, the merge recommendation>

### CRITICAL
- **<title>** — `<file>:<line>`
  <what is wrong> · <why it matters> · <how to fix> · <owning skill, if a convention>

### HIGH
- **<title>** — `<file>:<line>` …

### MEDIUM
- **<title>** — `<file>:<line>` …

### INFO
- **<title>** — `<file>:<line>` …

### Check coverage
<one row per rubric area: the area, the check numbers you actually ran, and any
check number you skipped with the reason — `1: 1.1-1.10, all run`, `5: 5.1-5.23,
5.9 not applicable (no partial classes touched)`. A check that cost more effort
than the others is exactly the one that goes missing here.>

### Architecture compliance
PASS / FAIL — <placement and dependency direction; deep dive: dotnet-architecture-review>

### Test coverage
<which changed behaviour has a test, at which tier, and what is missing>

### Cleanup candidates
<slop found, unranked, as candidates for /simplify — not applied here>

### What's Good
- <name the patterns worth repeating>
```

Three rules for the findings themselves:

1. **Every finding states what is wrong, why it matters, and how to fix it.** A
   finding without a fix is an opinion, and a fix without a why cannot be
   argued with — so the author either applies it blindly or ignores it. If you
   have no fix, it is an INFO observation; label it one.
2. **Every finding carries `file:line`.** A reviewer who cannot point at a line
   has not finished checking.
3. **Cite the owning skill when the finding is a doctrine violation.** "This
   contradicts `ef-core-data-access`" is actionable; "I prefer" is not.

The one exception to the fixed shape: for a **Low**-radius change, collapse to
Summary + findings + What's Good.

## Routing

**Deep dives — sibling rubrics.** This skill is the general breadth pass. When
one lens dominates the change, load its rubric rather than stretching this one.

| The change is mostly about | Load |
|---|---|
| Layering, dependency direction, placement, a handler doing too much | `dotnet-architecture-review` |
| Authorization gates, secrets, injection, mass assignment, data exposure | `dotnet-security-review` |
| Query cost, allocation, blocking, missing indexes | `dotnet-performance-review` |

**Doctrine — the owning knowledge skill.** The rubric's job is to notice that
the code disagrees with the rule; the rule itself lives here.

| Area of the finding | Owning skill |
|---|---|
| Project, folder, facade or module placement; composition root | `facade-module-architecture` |
| Service and validator structure, guards, request and response types | `module-feature` |
| Routes, DTO chains, response attributes, OpenAPI | `api-surface` |
| Repositories, queries, transactions, entities, migrations | `ef-core-data-access` |
| Which exception, wrapping, the error envelope | `error-handling` |
| Message text, keys, message types | `message-keys` |
| Test tier, doubles, fixtures, assertions | `dotnet-testing` |
| Mapping profiles, CreateMap, IncludeAllDerived, profile placement | `automapper-mapping` |
| JWT, schemes, policies, permission attributes | `auth-and-security` |
| Cache keys, TTL, invalidation | `distributed-caching` |
| Lock keys, options, double-processing | `distributed-lock` |
| Index documents, search descriptors, reindexing | `elasticsearch-search` |
| Outbound HTTP calls — the sender chain, content builders, the integration's settings section, timeouts | `http-client-factory` |
| Shared helpers, extensions and attributes; regex patterns; what may sit in `Facades/Common/` | `common-extensions` |
| Unsure which of the above owns it | `choosing-a-dotnet-skill` |

**Process.** Requesting a review, and triaging feedback received, belong to
`superpowers:requesting-code-review` and `superpowers:receiving-code-review`.
Applying the cleanup this rubric lists belongs to `/simplify`.

## References

**Read `references/review-rubric.md` when** running the pass itself — it holds
the per-area checklists behind the priority order above, each written as a
manual instruction naming what to grep and which file to open, with the
severity each finding carries.

**Read `references/cleanup-checklist.md` when** the change is being tidied
rather than assessed, when compiling the Cleanup candidates section, or before
proposing any deletion — it holds the slop taxonomy and the safe-delete checks
that must pass before anything is called dead (reflection, convention-based DI
registration, serialization, and configuration files can all reference a symbol
no compiler sees).

## Decision Guide

| Situation | Do this |
|---|---|
| Asked to "review my changes" with no scope | Diff against the base branch, score blast radius per file, take the highest, state the score in Summary before reviewing |
| Diff spans Critical and Low files | Review everything at the Critical depth |
| A finding is real but the fix is out of scope | MEDIUM with a named follow-up, not CRITICAL, and say why it is deferred |
| A convention question the diff raises is not settled anywhere | Raise it as INFO with the question stated; do not invent the rule in the review |
| A pre-existing problem in a touched file | INFO — unless the change makes it reachable or worse, then score it normally |
| Cleanup candidates found | List them under Cleanup candidates; hand execution to `/simplify` |
| The change carries more code than the task needed | Area 7 — MEDIUM at most, with `candidate for /simplify` as the fix; never a merge blocker on its own |
| The "over-build" is structure a shipped skill mandates — the module file family, a response tier, a thin envelope, an `Infrastructure/Facades/` capability built ahead of need | Not a finding. Sanctioned structure is not over-build; say so plainly if it is raised |
| The simpler shape is itself a shipped convention — a hand-rolled lock, a duplicated capability, a new package | Report it under the check that already owns it, at that check's severity; do not report it again under area 7 |
| Style is the only thing left to say | Say nothing; the formatter and analyzers own it |
