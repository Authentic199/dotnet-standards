# Design: who owns "write only the minimum the task needs" — and through what mechanism

**Date:** 2026-07-29 · **Session:** solo (decision doc only — no implementation)
**Deliverable:** this file. Nothing else was edited except the LANE BOARD row this
session is required to update at close.
**Decision:** **Option (B)** — distil the principle into house components.
Options (A) and (C) are refused, each with a recorded, checkable reason (§4).

---

## 1. The problem

The plugin has the **after-the-fact** side of unnecessary complexity:
`dotnet-code-review/references/cleanup-checklist.md` (five slop categories, four
safe-delete checks) and `/simplify` as the execution owner, pointed at from five
shipped locations (`dotnet-feature-flow:335`, `dotnet-review-flow:326,472`,
`dotnet-code-review:220,245`).

It has no **before-the-fact** side. Nothing fires during
`dotnet-feature-flow`'s PHASE 3 forcing the question *"does this need to exist
at all?"* before the first line is written, and no rubric area grades
*"complex where simpler satisfies the same conventions"* after it is written —
a full-tree grep of `skills/ agents/ commands/ hooks/` for
simplicity/over-engineering vocabulary returns zero owning sentences.

**Evidence this gap is real, not speculative** (gathered in this session,
2026-07-29, from the user — the same evidence bar `hooks/README.md:245` set for
`router-nudge`: observation first, component second):

- Over-build is observed in **three shapes**: speculative code "for later",
  re-implementing what already exists in the codebase, and complex solutions
  where a simpler one satisfies the task. Notably **not** selected: the classic
  abstraction-mania shape (interface-with-one-implementation) — the generic
  YAGNI literature's favourite target is not the observed problem here.
- Observed in **all three contexts**: sessions with the plugin active, sessions
  where the plugin was installed but never entered (the 0.3.27 shape), and
  repositories without the plugin. A flows-only fix therefore cannot reach two
  of the three contexts.
- **One user ruling recorded during evidence-gathering (binding on every rung
  rewrite):** infrastructure code built ahead of need that sits correctly on
  the `Infrastructure/Facades/` axis is **sanctioned structure, not
  over-build** (`facade-module-architecture:41,128` — "a technical capability
  many projects reuse"). The same family as the mandate that a thin MediatR
  envelope is NOT "code that need not exist" (`mediatr-messaging`, CHANGELOG
  0.3.16 ruling set).

## 2. The candidate examined

**`DietrichGebert/ponytail`** (MIT): a ~100-line ruleset — a lazy-senior-dev
persona, a 7-rung ladder (exist at all? → in-codebase reuse → stdlib → native
platform → installed dep → one line → minimum that works), a Rules list, three
intensity modes, an Output-brevity discipline, and a "When NOT to be lazy"
boundary list. Ships as a plugin: `SessionStart` injects the ruleset,
`UserPromptSubmit` switches modes, and it injects into every subagent.

**Its own benchmark, read honestly** (`benchmarks/results/2026-06-18-agentic.md`):
LOC reductions concentrate where **native platform features replace custom
builds** — frontend date picker −94%, color picker −92%. **Backend CRUD
converged across all arms (~20–44 LOC, minimal differences).** Safety held at
100% (the ~3 lines ponytail kept *were the path-traversal check*). Tested on
FastAPI + Haiku 4.5, n=4, single model. Conclusion quoted: *"huge where
there's bloat to cut, nothing where there isn't."* This plugin's domain —
backend .NET under a mandated architecture — is the "nothing where there
isn't" arm of ponytail's own data for rungs 3–6; the rungs that transfer are
1, 2 and 7, which are exactly the three shapes the user observed.

## 3. The test battery — every house law against every option

Options: **(A)** install ponytail as a third plugin · **(B)** distil into house
components · **(C)** do nothing.

| # | Test | (A) install | (B) distil | (C) nothing |
|---|---|---|---|---|
| 1 | Grade-once | **FAIL** — an always-on voice judging code shape is a second grader for shapes the rubrics already own: hand-rolled lock (`review-rubric.md:286` 3.9 HIGH), dead code (`cleanup-checklist.md:59`), new dependency (`static-rules.md:148` R16; `review-rubric.md:596` 6.8) | **PASS** — grading lands in exactly one grader (`dotnet-code-review`); the flow and CLAUDE.md carry generation-time instruction, which constrains what gets written and grades nothing | **PASS** — nothing added, nothing double-graded |
| 2 | Provenance law | **N/A as written** — the law binds this plugin's shipped claims, and (A) ships none; but the *effect* the law protects against (ungrounded judgements steering .NET code) arrives whole: no rung cites any shipped body | **PASS by construction** — every rung is sorted COPY/POINTER/REWRITE in §6; a rung with no owner is REFUSED in §7, not shipped | **PASS** — trivially |
| 3 | Principle 8 — delta, not doctrine | **FAIL** — rung 2 restates the reuse law (`distributed-lock:45`, `distributed-caching:43`, `elasticsearch-search:61`), rung 5 restates R16, the boundary list restates validator/error-handling doctrine — ambient, every session, a permanent second source of truth | **PASS** — restatements become pointers (§6); only the unowned residue is written new | **PASS** — trivially |
| 4 | Hook conflict; silent-absence rule | **PASS on mechanics, and it does not help** — ponytail's `SessionStart`/`UserPromptSubmit` append alongside this plugin's three hooks (`hooks/README.md:36-151`) with no technical collision, and its silent absence is benign (`:207-210`); but the same section records that occupying an injection slot ≠ being heeded | **N/A** — (B) ships no hook; the before-the-fact side rides named call sites and CLAUDE.md, both hookless | **N/A** |
| 5 | Measured evidence, 0.3.27 | **FAIL** — (A)'s primary mechanism is `SessionStart` injection, and `CHANGELOG.md:281-283` records that Superpowers' own emphatic `SessionStart` block "was present in that session and was ignored on turn 1"; `hooks/README.md:144-149` made that measurement the reason `router-nudge` hangs off the prompt instead | **PASS** — (B) uses the channels the plugin already trusts: flow phases (run when invoked) and per-repo `CLAUDE.md` (`claude-md-builder`'s entire premise: loaded every session) | **FAIL** — the same evidence bar demands action: over-build was observed *with the plugin active* (§1), which is the router-nudge shape — a measured failure the current tree cannot answer |
| 6 | Subagent contamination | **FAIL** — ponytail states it injects into every subagent. Its Output rule ("code first, at most three short lines, no essays") directly contradicts the mandatory report shapes: every rubric section always appears, `None.` when empty (`dotnet-code-review:136-140`); a reviewer or tester under brevity pressure bends the report contract, and the fleet's verification depends on full reports | **PASS** — only `dotnet-code-reviewer` loads the rubric that gains the area; the other five agents are untouched; implementers receive the doctrine only via the plan's named skill pointers (`dotnet-feature-flow:130-136,182-184`) | **PASS** — trivially |
| 7 | House-architecture conflict (heaviest) | **FAIL** — rung 1 and rung 6 + "fewest files possible" + "no interface with one implementation" condemn, with no means of exception: the module file family (`module-feature`), the four-tier response chain (`module-feature:189-194`), thin envelopes (CHANGELOG 0.3.16), marker types, `Expressions/`, per-capability Settings (`facade-module-architecture`), the repository abstraction whose *purpose* is substitutability (`unit-testing.md:147`), and the user's Facades-axis carve-out (§1). A generic voice has no knowledge of the shipped bodies and therefore no means to distinguish deliberate structure from slop | **PASS** — the distinction is written *into each rung* as a "does not apply to" clause citing the owning skill (§5.3), which is precisely what (B) buys over (A) | **PASS** — no new judge, no conflict |
| 8 | Does "call, never copy" transfer? | **FAIL** — Superpowers is called at named phases (`dotnet-feature-flow:30-38`); ponytail has **no call site** in either flow — it is ambient pressure on every token. The pattern this plugin uses for third-party process does not apply to (A) | **PASS** — (B) creates the named call sites the pattern requires: PHASE 2/3 instruction, rubric area, `/simplify` handoff | **N/A** |
| 9 | Budget | **N/A** — (A) adds no lines to plugin bodies | **PASS with a constraint** — additions land only in files with room: `dotnet-code-review` 246, `dotnet-feature-flow` 369, `claude-md-builder` 327 + its reference file. The three files at the bar are **not touched**: `dotnet-review-flow` 495, `dotnet-security-review` 498, `dotnet-performance-review` 499 (measured `wc -l`, this session). The fleet needs no flow edit — the code reviewer already loads the rubric | **PASS** — trivially |
| 10 | Timing vs the two outstanding description trials | **FAIL** — a third plugin injecting context into the consumer repository is a third variable confounding both trials (`CHANGELOG.md:91-97`) | **PASS with a constraint** — implementation must not change the two on-trial descriptions (`dotnet-review-flow`, `choosing-a-dotnet-skill`); this design needs router **body-table** rows only, and `dotnet-code-review`'s description (not on trial) may gain trigger nouns. Wait-condition on anything further: the next real consumer-repo session runs and both trials read out | **PASS** — trivially, and it is the only thing (C) has |

## 4. Verdicts, with the reasons a future session can check

**(A) — REFUSED.** Two independent grounds, either sufficient:
(1) its delivery mechanism is the one this repository has already measured
being ignored (`CHANGELOG.md:281-283`); (2) its content cannot distinguish
sanctioned structure from slop in this architecture (test 7), and it offers no
per-repo carve-out mechanism to teach it.
**This refusal stops holding — and must be revisited — only if BOTH become
true:** a measurement *in this environment* shows ambient session-start
injection being heeded on turn 1, **and** ponytail (or a successor) ships a
repo-level exception mechanism capable of expressing "structure mandated by a
shipped skill is exempt". Until both, re-proposing (A) re-litigates settled
evidence. The implementation session should mirror this row into
`hooks/README.md`'s refusal table (precedent: the `router-nudge` row carries
both verdicts and the evidence between them) — that file is not edited by this
decision-only session.

**(C) — REFUSED.** Its premise — "cleanup-checklist plus the four rubrics
already cover this" — is false on two counts, both checkable: the five-category
taxonomy (`cleanup-checklist.md:30,41,59,77,96`) contains no category for
*complex-where-simpler-works* (its own boundary sentence routes everything else
to "a real finding for one of the six areas or a preference"), and the user
observed over-build **in sessions with the plugin active** (§1, recorded
2026-07-29).
**This refusal stops holding if** the observations of §1 are re-attributed —
e.g. the observed cases turn out on inspection to be sanctioned-structure
false positives (the user's own Facades carve-out) — or if a future release
ships the before-the-fact side by another route and the observations stop.

**(B) — CHOSEN**, with the obligations the battery attached: the REFUSED table
(§7) for rungs with no owner, the budget constraint (test 9), and the timing
constraint (test 10).

## 5. The (B) design

### 5.1 Ownership — one grader, two carriers

**Grading owner (exactly one): `dotnet-code-review`.** A new priority-order
area — working title **"Simplicity / over-build"** — inserted as row 7,
pushing Cleanup/slop to row 8 (one cross-reference update:
`cleanup-checklist.md:3` "Priority row 7" → row 8; style stays last). Checks
live in `references/review-rubric.md` as a new area continuing the numbering
convention (per-area, never reused). Rejected alternates:
`dotnet-architecture-review` (owns placement, not shape; its 4.2 stays where
it is and the new area cross-cites it by number and name) and a new fifth
rubric (touches ~8 anchored sites in `dotnet-review-flow` — the "four lens"
roster at `:103,111,301-311` — plus a seventh agent and four description
edits, for no grading power the row-7 form lacks).

**Carrier 1 — `dotnet-feature-flow` (before the fact, inside the flows).**
PHASE 2 gains one instruction: the plan applies the house ladder (§5.3) *while
steps are being written* — a step that exists for a speculative need is cut at
plan time, where it costs a sentence instead of a review round. PHASE 3's
existing prompt-pointer mandate (`:182-184`) then carries the doctrine to
implementers by naming the owning skills — no new mechanism.

**Carrier 2 — `claude-md-builder` static rule (before the fact, every
session).** A new approved static rule (next free R-number) in
`references/static-rules.md`, group *Scope, workflow and verification* —
reaching the context (§1) that flows-only cannot: sessions that never enter
the plugin. It must survive `claude-md-builder`'s own principle 4 ban on
generic advice (`SKILL.md:51-54`), so it is written as behavioural constraint,
not exhortation — same altitude as R16/R17: search-before-write, no code for a
need the current task does not have, simplest shape *the conventions this file
points to* allow. Principle 1's own test admits it: removing the line causes a
mistake that has been observed, three contexts, 2026-07-29. Repositories
without the plugin remain out of reach by definition; that context closes only
by installing.

**Why grade-once is satisfied:** the carriers instruct generation and emit no
judgement about existing code; every after-the-fact judgement carries the new
area's check number from the one rubric. `/simplify` remains the sole
execution owner — the area produces candidates for it, exactly as
Cleanup/slop already does.

### 5.2 When it fires — named, per the mandate

| Moment | Site |
|---|---|
| Plan written | `dotnet-feature-flow` PHASE 2 (`:125-143`) — ladder applied to steps as drafted, before GATE 1 |
| Code written | PHASE 3 via the existing pointer mandate (`:182-184`) — implementer prompts name the owning skills |
| Any session, any repo with a generated `CLAUDE.md` | the new static rule, loaded every turn |
| Review | `dotnet-code-review` new area 7, run by this session's reviews and by the fleet's `dotnet-code-reviewer` (loads the rubric as its first action — **no `dotnet-review-flow` edit**) |
| Execution of candidates | `/simplify`, unchanged |

### 5.3 The house ladder — direction for the three-way loop, not final text

Each rung states what it does NOT apply to, inside the rung. Final wording
belongs to the implementation session's three-way loop.

1. **Does the task in front of you need this code?** No speculative need: no
   scaffolding "for later", no parameter nothing passes, no branch for a state
   a guard or validator already excluded (cross-cite `cleanup-checklist.md:59`
   dead-code shapes; `dotnet-architecture-review:213` 4.2 for folders).
   *Does not apply to:* structure a shipped skill mandates — the module file
   family (`module-feature`), response tiers (`module-feature:189-194`), thin
   envelopes (CHANGELOG 0.3.16), and **Facades-axis infrastructure built ahead
   of need** (`facade-module-architecture:41,128`; user ruling §1).
2. **Does it already exist in this codebase?** Search before writing; call the
   handler/helper/pattern that exists. Owned in the capability skills
   (`distributed-lock:45`, `distributed-caching:43`, `elasticsearch-search:61`,
   `module-feature` request families) — the rung generalises them to ordinary
   helpers, graded MEDIUM; capability duplicates keep their owners' HIGH.
3. **Is this the smallest shape the owning skills allow?** The floor is the
   shipped convention, never fewer lines than it: an abstraction earns its
   place when something must substitute it (`unit-testing.md:147` supplies the
   house criterion) or a shipped skill mandates it; clarity beats brevity —
   over-simplification that merges concerns, obscures debugging, or changes
   behaviour is the same defect from the other side (the balance
   `code-simplifier` §4 states and `/simplify`'s no-behaviour-change rule
   enforces).

Severity: **capped at MEDIUM** — MEDIUM/INFO are never chased by the loops
(`dotnet-feature-flow:234`), so the area reports and routes to `/simplify`
without jamming a merge on subjective grounds. **HIGH only where the simpler
shape is itself a shipped convention** — those checks already exist and keep
their numbers (3.9 hand-rolled lock; capability duplicates; R16 territory) —
cited, not re-graded (grade-once).

### 5.4 Hard boundaries — never simplified away (listed, not implied)

Validator rules at trust boundaries (`module-feature`); exception flow and the
error envelope (`error-handling`); `CancellationToken` declaration and
propagation (R6; `review-rubric.md` 3.1 HIGH); authorization attributes and
everything `dotnet-security-review` grades; message keys (`message-keys`);
migration safety (R1–R4); the sanctioned structural families named in rung 1;
anything explicitly requested by the user. (Ponytail's own boundary list,
mapped to owners — the mapping is the POINTER bucket, §6.)

### 5.5 Attribution

`NOTICE` obligation 3: `DietrichGebert/ponytail`, MIT, same form as the two
existing entries — covering the ladder structure and the verbatim lines in the
COPY bucket. Written by the implementation session.

### 5.6 Alignment — router edits, same commit, named now

- Base-map row `choosing-a-dotnet-skill:54` (`dotnet-code-review`) gains the
  new nouns: unnecessary complexity, over-build, simplification candidates.
- New shared-token row: **"this is over-built / simplify this"** → grading the
  claim — `dotnet-code-review`; executing the cleanup — `/simplify`; the
  build-time rule — `dotnet-feature-flow` / the repository's `CLAUDE.md`.
- The router's **description is not touched** (0.3.29 trial outstanding).

### 5.7 Roadmap — one implementation session

Three-way loop (skill pieces), one deliverable, one version: piece 1
`dotnet-code-review` (area row + rubric checks + cleanup renumber + its own
description nouns), piece 2 `dotnet-feature-flow` PHASE 2 instruction, piece 3
the static rule. Plus, outside the loop: router rows (§5.6), `NOTICE` (§5.5),
the `hooks/README.md` refusal-table row for (A) (§4). Inputs: this file, the
ponytail SKILL.md + benchmark, the user rulings in §1. Done = validate + both
manifests agree + `claude plugin update --scope project` + the prove-it checks
+ router rows in the same feat commit. **Not in scope, same owner, separate
deliverable:** the 0.3.28 seam (blast-radius re-rank for standing audits,
`CHANGELOG.md:247-251`) — it shares the file, not the problem.

## 6. COPY / POINTER / REWRITE — every ladder line sorted

| Ponytail line | Bucket | Reason |
|---|---|---|
| Rung 1 "Does this need to exist at all? (YAGNI)" | **REWRITE** → §5.3 rung 1 | Judges code shape in generic vocabulary; owner evidence exists (4.2, dead-code shapes, user observations) but the exemption list must be house-specific |
| Rung 2 "Already in this codebase? … a few files over is the most common slop" | **REWRITE** (generalisation) + **POINTER** (capability duplicates → `distributed-lock:45`, `distributed-caching:43`, `elasticsearch-search:61`) | The capability half is owned; the ordinary-helper half is the user's observed shape 2 and needs house wording |
| Rung 3 "Stdlib does it?" | **REFUSED** (§7) | No shipped owner names stdlib-vs-custom anywhere |
| Rung 4 "Native platform feature covers it?" | **REFUSED** (§7) | Frontend-flavoured; ponytail's own benchmark shows ~no backend effect |
| Rung 5 "Already-installed dependency solves it?" | **POINTER** → R16 (`static-rules.md:148`) | Owned, verbatim territory |
| Rung 6 "Can it be one line?" | **REFUSED** (§7) | No owner; head-on collision with mandated structure; its spirit survives only inside rung 3's "smallest shape the owning skills allow" |
| Rung 7 "the minimum code that works" | **REWRITE** → §5.3 rung 3 | The core doctrine, but the floor must be the shipped convention, not zero |
| "The ladder runs after you understand the problem, not instead of it" | **COPY** | Reading/thinking discipline; judges no .NET code; no owner |
| "Read fully, then be lazy" | **COPY** | Same |
| Bug-fix = root cause, grep every caller | **POINTER** → `superpowers:systematic-debugging` | Process, owned outside this plugin |
| "No interface with one implementation…" | **REWRITE** folded into §5.3 rung 3 via the substitutability criterion (`unit-testing.md:147`) | The generic form condemns the repository abstraction; the house form has a test |
| "No config for a value that never changes" | **REFUSED** (§7) | Settings placement is owned (`fma` 4.4); no shipped sentence grades config-value churn |
| "Deletion over addition. Boring over clever" | **REFUSED** as shipped text | Slogan altitude; principle 4 territory — its content arrives via rungs 1 and 3 |
| Complex request → ship lazy + question in same response | **REFUSED** | Collides with GATE 1 and the plan-is-the-contract rule (`dotnet-feature-flow:188-190`) |
| `ponytail:` ceiling comments | **REFUSED** | A foreign marker convention; the house already has the TODO taxonomy (`cleanup-checklist.md:77`) |
| Output brevity ("code first, three lines, no essays") | **REFUSED** | Contradicts mandatory report shapes and the user's communication rules (R22/R23) |
| Persona + intensity modes ("lazy senior dev", lite/full/ultra) | **REFUSED** | Mode-switching ambient persona; no call site; test 8 |
| "When NOT to be lazy" boundary list | **POINTER** → §5.4 owners | Every item is owned by a shipped skill or a static rule |
| One runnable check per non-trivial change | **POINTER** → `dotnet-testing` | Test placement and tiers are owned; the rung must not create a second testing doctrine |

COPY bucket final size: **two lines.** Per the mandate, that is the correct
result, not a failure.

## 7. Refused — and why (checkable, reversible on stated conditions)

| Refused | Reason | Reverses if |
|---|---|---|
| Rung 3 (stdlib) | No shipped body, no corpus sentence | A corpus example is user-named (R7) showing stdlib-vs-custom drift |
| Rung 4 (native platform) | No owner; benchmark shows no backend effect | The plugin ever grows a frontend surface |
| Rung 6 (one line) | No owner; condemns mandated structure | Never as a rule; its spirit already ships inside rung 3 |
| Config-for-constant | No shipped sentence grades it | A corpus anti-example is user-labelled (R8) |
| Persona / modes / output brevity / `ponytail:` markers / lazy-ship-and-question | Conflict with report shapes, gates, communication rules, marker taxonomy | Not reversible inside this architecture |
| A hook carrier for the doctrine | Injection slot measured weak (0.3.27); `router-nudge` already occupies the once-per-session slot; the CLAUDE.md carrier reaches the same sessions reliably | A measurement shows the CLAUDE.md carrier failing the way descriptions failed at 0.3.27 |

## 8. Requests deferred to this roadmap

None yet. Additional asks arriving before the implementation session are logged
here, not acted on.
