---
name: dotnet-review-flow
description: >-
  This skill should be used when putting a .NET change through the test-and-review
  fleet: /dotnet-review, "review my branch", "check my changes before merge",
  running the suite and the four review lenses over one diff, spawning the tester
  and reviewer agents in parallel, verifying findings against the code before any
  fix, or looping tests and review to green. Not for: the full feature process,
  brainstorm to commit — dotnet-feature-flow; what each lens checks —
  dotnet-code-review, dotnet-architecture-review, dotnet-security-review,
  dotnet-performance-review; test conventions — dotnet-testing; brainstorming,
  planning, TDD — Superpowers; unclear ownership — choosing-a-dotnet-skill.
---

## Overview

This skill is an **orchestration graph, not a rubric.** It runs a change through
two loops — tests, then review — using fresh-context subagents, and it decides
only *what runs, in what order, who runs it, and when to stop*.

**It teaches nothing.** Process belongs to Superpowers, which this flow **calls
and never copies**. What each lens checks belongs to the four rubric skills; what
a test looks like belongs to `dotnet-testing`; the .NET conventions under all of
it belong to the knowledge skills. A line of .NET doctrine appearing in this body
is a defect — it becomes a second source of truth the day the owning skill
changes.

### Two modes, one block

| Mode | Entered by | Who fixes | Ends at |
|---|---|---|---|
| **Standalone** | `/dotnet-review`, or this skill triggering on its own description | Nobody, until the user accepts the offer | The final report, plus an **offer** to fix the CONFIRMED findings |
| **Embedded** | `dotnet-feature-flow`, as its PHASES 4–5 | The calling flow's implementer | Return to the caller; the caller owns what happens next |

**How the mode is decided: the caller says so.** `dotnet-feature-flow` states that
it is running this skill's shared block as its PHASES 4–5 when it invokes this
skill. **Absent that explicit statement, the mode is standalone.** Default that
way deliberately — a standalone run that wrongly thinks it is embedded edits the
tree without asking, while the reverse merely asks a question that was already
answered.

State the mode in the first line of the final report. The loops are identical in
both; only who applies a fix, and what happens at the end, differ. A reader who
does not know which mode ran cannot tell "nothing was fixed" from "nothing needed
fixing".

## Core principles

1. **Judgement comes from fresh context; this session coordinates.** The session
   running this flow **never reviews and never tests.** It computes the diff,
   spawns the agents, verifies specific claims against specific lines, applies
   fixes, and writes the report. A session that has just written the code cannot
   un-know its intent while grading it — that is the whole reason the fleet
   exists.

   **Verifying is not reviewing.** Opening `<file>:<line>` to check whether a
   reported symptom is actually there is a fact check with a stated answer.
   Forming an opinion about code no agent flagged is reviewing, and it is out of
   bounds.

2. **Tests before review, always.** Tests are cheap and mechanical; review is
   expensive. Reviewers never see non-green code, and every review-driven fix
   re-enters the tests anyway — so reviewing first spends the expensive pass on
   code that is about to change.

3. **A finding is verified before it is fixed.** A subagent's CRITICAL is a claim
   about lines this session can open. Reproduce it or downgrade it. Fixing an
   unverified finding edits working code on a subagent's say-so, and no later
   round catches that.

4. **Nothing is silently dropped.** A lens that failed, a tier that was absent, a
   check that could not run, a finding that stayed PLAUSIBLE — each appears in the
   final report by name. Silence reads as a pass, and the distinction between
   *clean* and *unexamined* is this flow's entire output.

## PHASE 0 — Preflight, and STOP on any failure

Four checks, in order. Each failure is a **STOP**: report what is missing, give
the exact remedy, and wait for the user. Do not degrade, do not work around, do
not install anything.

**1 — Superpowers is present and enabled.** Verify by **actually loading a
Superpowers skill**: invoke the Skill tool on
`superpowers:verification-before-completion`. This flow needs it later anyway, so
the probe costs nothing.

Do **not** verify by reading `installed_plugins.json`. A disabled or stale install
still has a registry entry, so the file answers "is it registered", not "can I
call it". The registry check is the SessionStart hook's job, at warn level; the
only trustworthy check here is the call.

> **STOP — Superpowers is not available.**
> This flow calls Superpowers for process and cannot substitute its own.
> Install: `claude plugin install superpowers@claude-plugins-official`
> Then restart Claude Code — a new plugin does not take effect in this session.

**2 — A git repository is present**, with a resolvable base to diff against
(`git rev-parse --git-dir`). No repository means no diff, and this flow reviews
diffs. STOP with what was found.

**3 — This plugin is complete, not a stale or partial install.** The block binds
to five skills and six agents:

| Must be present | Names |
|---|---|
| Rubric skills | `dotnet-code-review`, `dotnet-architecture-review`, `dotnet-security-review`, `dotnet-performance-review` |
| Test conventions | `dotnet-testing` |
| Agents | `dotnet-unit-tester`, `dotnet-integration-tester`, `dotnet-code-reviewer`, `dotnet-architecture-reviewer`, `dotnet-security-reviewer`, `dotnet-performance-reviewer` |

**Check the names against this session's available skills and agent types. Do not
load the rubrics to prove they exist** — five rubric bodies in this session's
context is both expensive and exactly the contamination Principle 1 forbids, and
a name that is absent from the roster is absent from the install. A missing name
means the cached copy is stale or partial: STOP, name what is absent, and give the
update-and-restart remedy.

The agents carry a second net — each stops and says so when its bound skill will
not load — but discovering that one round in has already spent a fleet.

**4 — Determine and state the review target.** Take it from the invocation
argument: a branch, a commit range, or a named scope. With no argument, default to
the working diff against the repository's default branch. **State the target back
to the user before running**, as a one-line scope label. A review of the wrong
range is indistinguishable from a clean one until someone reads the file list.

## Diff preparation — the spawn contract

**The flow computes the diff exactly once, and every subagent receives the same
four inputs.** No subagent runs git: the four reviewers have no shell at all, and
the two testers have one only to build and test.

1. Resolve the base ref from the target.
2. Write the diff to a file **under the session's scratch area, never inside the
   repository** — a diff file committed by accident is a defect this flow caused.
3. Derive the changed-file list (`git diff --name-only <base>...HEAD`).
4. Hand every spawned agent these four items, verbatim and identical:

| Input | What it is |
|---|---|
| **Diff file path** | The single computed diff, already on disk |
| **Changed-file list** | The paths the diff touches — the reviewers' scope, and what the testers match stack frames against |
| **Base ref** | What the diff is against, so a finding can say what is new |
| **Scope label** | The human-readable target, reused verbatim in every report heading |

One diff, one contract, six agents. Handing two lenses different inputs produces
two reports that cannot be compared, and nobody can tell which one was wrong.

**Recompute the diff after every fix round**, before re-spawning. A reviewer
handed a stale diff reports findings that no longer exist and misses the ones the
fix introduced.

**Then build once, as a gate.** Run `dotnet build` on the solution. On failure,
report the compiler diagnostics and **spawn nobody** — a fleet dispatched at code
that does not compile burns six agents to rediscover one error. On success the
testers still run their own builds; that is their contract and it is what keeps a
build failure reportable separately from a test failure. Against warm outputs
their builds cost almost nothing, which is the gate's second benefit: two agents
compiling the same cold tree in parallel is what produces artifact-lock
collisions.

This flow owns no worktree and no branch. Worktree lifecycle belongs entirely to
`dotnet-feature-flow`.

## The shared block: TEST-LOOP then REVIEW-LOOP

*This heading is stable. `dotnet-feature-flow` names it verbatim to run this block
as its PHASES 4–5. Do not rename it, and do not fork a second copy of the loops
below into another skill.*

Run **TEST-LOOP** to green, then **REVIEW-LOOP**. Every fix inside REVIEW-LOOP
re-enters TEST-LOOP before the next review round.

### TEST-LOOP

**Spawn both testers in parallel**, in one message, each with the spawn contract:

- `dotnet-standards:dotnet-unit-tester`
- `dotnet-standards:dotnet-integration-tester`

Each builds and runs its own tier and returns its own report. **Do not read the
tiers' conventions, re-run a suite, or second-guess a tier from this session** —
the testers own that, and Principle 1 forbids it.

Each tester ends on one of six verdict strings. Branch on them:

| Verdict | Do this |
|---|---|
| `GREEN` | This tier is done |
| `tier absent — nothing run` | Does not block the loop, and **is not a pass.** Carry it into the final report under *Not run*. Never scaffold a tier |
| `RED — tests failed` | Fix and rerun — see who fixes, below |
| `RED — build failed` | Fix the build; no test result exists to interpret yet |
| `RED — environment` | **Halt immediately and surface it to the user. This does not consume a round.** No container runtime, an unreachable image, an artifact lock: there is nothing in the code to fix and another round fails identically. On an artifact lock specifically, re-run the pair **serially** — unit first, then integration — once, and note the serialization in the report before halting |
| `RED — timed out` | Report the command and the budget; a run killed at a limit is not a failing suite. Retry once with a larger budget, then halt |

**Who fixes:** embedded mode, the calling flow's implementer. Standalone mode,
**nobody** — report the failures and stop; fixing is offered after the report,
never before.

**Cap: 5 rounds.** On the fifth, halt with a status summary — which tier, which
tests, what changed between rounds, how many rounds ran — and ask the user. Never
a sixth, and never relax the green bar to escape the cap.

### REVIEW-LOOP

Entered **only with both tiers green** (or absent and recorded).

**Spawn all four reviewers in parallel**, in one message, with the same spawn
contract:

- `dotnet-standards:dotnet-code-reviewer`
- `dotnet-standards:dotnet-architecture-reviewer`
- `dotnet-standards:dotnet-security-reviewer`
- `dotnet-standards:dotnet-performance-reviewer`

All four, every round. One agent per lens, no sharing: no reviewer inherits
another's context. **Never collapse the four into fewer spawns, and never drop one
because "this change is not about security"** — the lenses grade different things,
a merged pass reliably drops one, and a lens dropped by judgement is a lens whose
absence nobody notices.

Collect the four reports as returned. Each carries its own rubric's sections and
verdict; **do not merge, rewrite or re-grade them.**

Then verify (below), and:

1. **Fix only CONFIRMED CRITICAL and HIGH findings**, then re-enter **TEST-LOOP**,
   recompute the diff, and re-spawn all four reviewers.
2. **Stop when no CONFIRMED CRITICAL or HIGH findings remain.**

**MEDIUM and INFO are never chased.** They go to the final report unfixed and
unargued. Chasing them turns a bounded loop into open-ended cleanup, and
`/simplify` owns cleanup.

Severity words are `dotnet-code-review`'s ladder — CRITICAL / HIGH / MEDIUM /
INFO — which every rubric cites. This flow consumes that scale and defines
nothing.

**Cap: 3 rounds.** On the third, halt with a status summary — which findings
survive, which were fixed, how many rounds ran — and ask. Never lower a severity
to clear the gate.

### Verifying findings — CONFIRMED vs PLAUSIBLE

Every CRITICAL and HIGH finding is verified before it forces a fix. Open the cited
`<file>:<line>` and answer one question: **is the reported symptom actually there,
as described?**

| Verdict | Meaning | Consequence |
|---|---|---|
| **CONFIRMED** | Reproduced from the cited lines | Forces a fix (CRITICAL and HIGH only) |
| **PLAUSIBLE** | Could not be reproduced from the cited lines — they do not show it, the citation is stale, or it depends on context the reviewer could not see | Reported with the reason, never fixed, never deleted |

Three rules keep this honest:

- **PLAUSIBLE is not "wrong".** It means the evidence handed over did not
  establish it here. It survives into the final report in the reviewer's own
  wording, so the user can judge it. Never upgrade a PLAUSIBLE by argument.
- **Verify the claim, not the code.** Read the cited lines and their immediate
  context. A verification pass that wanders into a general opinion has become the
  review this session may not perform.
- **Do not verify MEDIUM or INFO.** They force nothing, so verification buys
  nothing and costs a read of every file in the change.

For triaging what comes back — arguing with a finding, telling a technical
disagreement from a performative one — load
`superpowers:receiving-code-review` rather than improvising.

### When a subagent fails

A tester or reviewer that errors, returns nothing, or returns something that is
not its report shape: **retry once with the identical prompt.** Same contract,
same wording — a reworded retry tests a different thing and its result is not
comparable.

Still failing: **surface it to the user by name**, say what is now unknown, and
list it under *Not run*. **Never silently drop a lens or a tier, and never
substitute this session's judgement for one that did not run.** A four-lens report
missing one lens is a three-lens report, and a report missing the security lens
looks exactly like a report where security found nothing — the most expensive
ambiguity this flow can ship.

Before declaring the block complete, invoke
`superpowers:verification-before-completion` and follow it. This flow's output is
a claim that the suite is green and the lenses are clean; that skill turns the
claim into evidence. "Green" means counts read out of a tester's report, not an
impression of one.

## The final report

**Always produced** — in both modes, when everything passed, and when a cap
halted the run. Every section appears; write `None.` when empty.

```markdown
## Review: <scope label>

Mode: standalone / embedded in dotnet-feature-flow · Base: <ref>

### Verdicts
| Lens | Verdict | CONFIRMED CRITICAL/HIGH | Unfixed MEDIUM/INFO |
|---|---|---|---|
| Code | PASS / FAIL | n | n |
| Architecture | PASS / FAIL | n | n |
| Security | PASS / FAIL | n | n |
| Performance | PASS / FAIL | n | n |

### Tests
| Tier | Verdict | Passed | Failed | Skipped |
|---|---|---|---|---|
<counts exactly as the testers reported them, with their verdict strings. A tier
that reported `tier absent — nothing run` still gets its row here, with the
verdict spelled out and dashes in the counts — it is never omitted, never merged
into another row, and never written as green. It appears again under *Not run*.>

### CONFIRMED findings
<per finding: lens · severity · file:line · fixed (what changed) or outstanding (why)>

### PLAUSIBLE findings
<per finding: lens · severity · file:line · why verification could not reproduce it,
in the reviewer's own wording>

### Unfixed MEDIUM and INFO
<by lens, unranked, carried through untouched>

### Not run
<lenses, tiers, layers, areas or checks that did not run, and why>

### Run
TEST-LOOP <n> of 5 · REVIEW-LOOP <n> of 3 · <cap hit? say so> · <commands the flow ran>
```

Three rules for the report:

1. **A green run reports the numbers.** "All clean" with no counts is
   indistinguishable from a suite that discovered nothing and a fleet that never
   spawned.
2. **Carry the subagents' own coverage sections through.** Each rubric mandates a
   coverage line — audits, layers, areas — and each tester reports what did not
   run. Fold them into *Not run*; do not summarize them away. What the fleet did
   not examine is the most perishable thing it learned.
3. **Nothing a subagent learned is dropped.** The user paid for six fresh-context
   passes; a summary that keeps only the blockers throws away most of what was
   bought.

## Standalone mode — the offer

The standalone run ends at the report. Then, and only then, **offer**:

> N CONFIRMED findings remain (X CRITICAL, Y HIGH). Fix them now?

- **Accepted:** this session fixes them directly, re-enters the shared block from
  TEST-LOOP, and reissues the report — same block, same caps.
- **Declined, or no answer:** stop. The report is the deliverable.
- **Never fix first and report after.** A standalone review that edited the tree
  before the user saw the findings has taken a decision nobody offered — and that
  boundary is exactly the one the read-only fleet exists to draw.

The offer covers CONFIRMED CRITICAL and HIGH only. MEDIUM and INFO are not offered
and not chased.

## Routing

**Sibling flow.** The full feature process — brainstorm, plan, implement, this
block, then git — is `dotnet-feature-flow`. It calls the shared block above; it
does not reimplement it.

**Content.** What each lens checks lives in `dotnet-code-review`,
`dotnet-architecture-review`, `dotnet-security-review` and
`dotnet-performance-review`; what a test looks like lives in `dotnet-testing`.
This flow loads none of them — the agents do.

**Process.** Brainstorming, planning, TDD, worktrees, finishing a branch, and the
request/receive review discipline belong to Superpowers, which this flow calls and
never copies. Executing cleanup candidates belongs to `/simplify`.

**Unclear ownership.** `choosing-a-dotnet-skill`.

## Decision Guide

| Situation | Do this |
|---|---|
| Invoked with no argument | Diff the working tree against the default branch; state the scope label back before running |
| No caller said "embedded" | Standalone. Offer before fixing anything |
| Superpowers will not load | STOP with the install command and the restart note. Never substitute a hand-rolled process |
| A rubric skill or an agent name is missing from the roster | STOP — the install is stale or partial. Name what is absent, give the update-and-restart remedy |
| The pre-build gate fails | Report the diagnostics, spawn nobody, hand it back |
| The gate passes but a tester reports `RED — build failed` | A test project does not compile and sits outside what the solution build covered. Treat it as a build failure for that tier, not a test failure |
| The repository has no test projects at all | Both tiers absent: record it, report it under *Not run*, go straight to REVIEW-LOOP. Never scaffold a tier |
| A tester reports `RED — environment` | Halt and surface it. Does not consume a round; on an artifact lock, re-run the pair serially once and note it |
| A reviewer's CRITICAL cannot be reproduced at its `file:line` | PLAUSIBLE. Report it with the reason; do not fix and do not delete |
| A reviewer returns a finding with no `file:line` | PLAUSIBLE by definition — there is nothing to verify against |
| Two lenses report the same defect | Verify once, fix once, report once naming both lenses. Never carry two severities for one shape |
| A CONFIRMED HIGH is real but outside this change's scope | It still blocks the loop, or the user decides it does not. Ask; never silently demote it |
| A finding is MEDIUM and obviously right | Still not chased. The stop condition is CONFIRMED CRITICAL and HIGH only |
| A lens fails twice | Surface it by name, list it under *Not run*, continue with the rest. Never review that lens yourself |
| A cap is hit | Halt, summarize status, ask. Never raise the cap and never relax the stop condition to clear it |
| The urge to read the diff and form a view | Out of bounds. Verify a cited line; form no opinion the fleet did not raise |
| The user asks what a finding means | Point at the owning rubric or knowledge skill; do not re-explain the rule here |
| Asked to also plan, brainstorm or commit | Not this skill. `dotnet-feature-flow` owns the full process; Superpowers owns each process step |
