# Design — `dotnet-review-flow`: the NO-SIGNAL branch

**Date:** 2026-07-28 · **Lane:** D · **Branch:** `lane-d/review-flow-no-signal`
**Target skill:** `skills/dotnet-review-flow/SKILL.md` (399 lines at `2882034`)

## The problem

A real run of `/dotnet-review` against an external repository produced **no
deliverable at all**. Both testers returned `RED — environment` — Windows Smart
App Control blocked the test host from loading locally built assemblies — and
the flow halted per `SKILL.md:192`, before the four review lenses ever spawned.
The review lenses do not depend on the test tiers. The report the user actually
wanted was reachable the whole time and was never produced.

Two defects sit behind that outcome.

**Defect 1 — two equivalent situations, two different outcomes.** The skill
treats "no test signal" inconsistently:

| Situation | `SKILL.md` | Outcome |
|---|---|---|
| Repository has no test projects at all | `:189`, `:387` | Record under *Not run*, **go straight to REVIEW-LOOP**, report produced |
| Test projects exist but cannot run | `:192`, `:388` | **Halt everything**, no review, no report |

Both mean the same thing to a reader: *nothing is known about whether the code
works, and there is nothing in the code to fix.* One still delivers; the other
does not.

**Defect 2 — the skill contradicts itself.** `:290` says the report is
"**Always produced**". `:192` says halt. The running session cannot satisfy
both, so it asked the user — and a user who did not yet understand the
situation could not answer.

Solving the underlying environment problem does not close this. `:192` names
"No container runtime, an unreachable image" as its own examples: moving test
execution into containers **replaces** one source of environment RED with
others (daemon not running, image pull blocked, socket unreachable). The class
of failure persists; only the instance changed.

## Scope

**In:** `skills/dotnet-review-flow/SKILL.md`, plus one additive report section
in `agents/dotnet-unit-tester.md` (see below).

**Neither tester gains any power to repair.** Both forbid all repair — no file
writes, no `dotnet add package`, no starting or stopping containers by hand —
for a reason stated in their own bodies: *"a tester that repairs its own red is
no longer evidence the suite was ever red."* That constraint is correct and
load-bearing, and nothing here touches it. Environment repair belongs to the
coordinating session, which is also the only actor that can ask the user and
the only one not running in parallel with a peer.

`agents/dotnet-unit-tester.md` gains an `### Environment` section in its report
template, mirroring `agents/dotnet-integration-tester.md:61`. Verified gap: the
integration tester carries an environment message verbatim in its own section;
the unit tester's template has no such section, leaving a one-sentence Verdict
tail as the only home for it. NO-SIGNAL's Steps 1 and 2 require that message to
diagnose and measure, and the unit tier is the one that failed in the
originating incident. Purely additive — a place to put text the agent already
has.

**Out, deliberately:**
- How to write a test. This skill "teaches nothing" (`:21`). When tests must be
  written, it routes to `dotnet-testing`.
- The four rubric skills, the four reviewer agents, `dotnet-feature-flow`.

## Design

### 1. A third named unit: `NO-SIGNAL`

A new section inside *The shared block*, sitting between `TEST-LOOP` and
`REVIEW-LOOP`. It absorbs both entry conditions:

- a tester returned `RED — environment`
- a tester returned `tier absent — nothing run`

One mechanism, because both mean *no evidence about the code under review*.

### 2. The invariant, stated first in the section

> **NO-SIGNAL may end in a question. It may never end in nothing delivered.**
> Whether repair succeeds or fails, REVIEW-LOOP still runs and the report is
> still produced.

Everything else in the section is detail. This line is the fix.

### 3. Four steps

**Step 1 — State it so the user can act on it.** Name what is missing and why,
in terms that support a decision. An error code and a verdict string are not a
diagnosis. This is a mandate in the skill body, not a nicety: the original
incident escalated because the situation was reported in vocabulary the user
could not act on.

**Step 2 — Measure before offering.** Emit numbers, not adjectives.

- *Environment:* what is blocking, whether it is repairable, which rung of the
  ladder it falls on.
- *Absent tier:* how many types in the reviewed scope have no test, which tiers
  exist versus are empty, whether the integration tier needs infrastructure
  (a materially larger job than the unit tier).

"This would be a large job" is unusable. "Module `Users`: 14 types, 0 tests" is
a decision input.

**Step 3 — The repair ladder.** Maximum **2 attempts** per environment problem,
then explain and ask. Every other loop in this skill is capped; an uncapped
repair loop burns a session invisibly.

| Do it, no question | Ask first | Never |
|---|---|---|
| Start containers whose images are already local | **Anything that downloads over the network** — a missing package, an image not yet pulled | Disable Smart App Control (irreversible without reinstalling Windows) |
| Re-run the tiers serially on an artifact lock | Install software on the machine | Anything needing administrator rights |
| Run build and test commands, read config | Edit project files (target framework, `.csproj`) | Anything governed by corporate policy |
| | Change ports, touch the firewall, delete build caches | Edit a test file to dodge a failure |

The classifier is one question: **does this download from the network?** If yes,
ask. That single rule covers both package and image acquisition, and it matches
the environments this plugin's user works in — corporate networks, proxies, and
version surprises are all things the user must see coming.

*Edge case:* an ordinary `dotnet build` restores missing packages as part of
building. That is building, not repairing, and is not governed by the ladder.
Only a build that **fails because acquisition failed** enters NO-SIGNAL.

**Step 4 — Offer options derived from the measurement.** Never a bare yes/no.
The option list is generated from what Step 2 measured, not written into the
skill as a fixed menu. It must always include *do nothing, record it in the
report*, and must include at least one partial option whenever the measurement
decomposes into parts.

Worked example from the originating incident — 4 modules in scope, one module
with zero tests, an empty integration tier:

- Write tests for `Users` only (the one module with none) — small
- Write tests for all four modules in scope — much larger
- Unit tier only; defer the integration tier (it needs infrastructure stood up)
- Write nothing; record the gap in the report

Whether the review session writes those tests is settled: **it may, once the
user says so.** The skill already carries that precedent — `:350`, the
end-of-report fix offer, has the accepting user's answer authorize direct edits
by this session. Test writing reuses that mechanism rather than inventing an
exception. There is no standing "never write" rule to reconcile: the read-only
instruction in the originating run was a one-off to preserve the scene during a
plugin test, not a durable preference. Nothing is codified for it — an
instruction given at invocation time is simply obeyed.

### 4. Consequential edits to existing lines

| `SKILL.md` | Change |
|---|---|
| `:189` `tier absent` row | Enter NO-SIGNAL. Delete **"Never scaffold a tier"** |
| `:192` `RED — environment` row | Enter NO-SIGNAL instead of halting. Keep the serial re-run on artifact lock — it becomes a ladder rung |
| `:205` REVIEW-LOOP entry condition | Currently "only with both tiers green (or absent and recorded)". Add: or blocked and recorded |
| `:387` Decision Guide, no test projects | Point at NO-SIGNAL. Delete **"Never scaffold a tier"** |
| `:388` Decision Guide, `RED — environment` | Point at NO-SIGNAL |
| `:80` PHASE 0 "do not install anything" | Narrow: that sentence governs working around a **failed preflight**, not standing up a test environment. Left as-is it contradicts the ladder |

`Never scaffold a tier` appears twice and was chosen deliberately. Removing it
is a user ruling made on 2026-07-28, recorded here so no later session restores
it as a lost invariant.

### 5. Report-rule changes — user-approved, exact wording

Both were presented to the user and approved before implementation.

**Change 1** — `:290`. From:

> **Always produced** — in both modes, when everything passed, and when a cap halted the run.

To:

> **Always produced** — in both modes, when everything passed, when a cap halted the run, and when NO-SIGNAL ended in an unanswered question. **There is no path through this flow that ends without the report.**

**Change 2** — the `### Run` line of the report template. From:

> `TEST-LOOP <n> of 5 · REVIEW-LOOP <n> of 3 · <cap hit? say so> · <commands the flow ran>`

To:

> `TEST-LOOP <n> of 5 · REVIEW-LOOP <n> of 3 · NO-SIGNAL <what was attempted, what the user chose, or "not entered"> · <cap hit? say so> · <commands the flow ran>`

**No new report section.** A deferred choice ("write `Users` tests later") goes
into the existing *Not run* section with its reason. The report's structure is
unchanged.

### 6. Mode

The missing-tests offer runs in **standalone mode only**. Under
`dotnet-feature-flow`, tests are written as the feature is built, so the
zero-tests case does not arise there. The environment ladder applies in **both**
modes.

## Risks

**Line budget.** 399 lines today; the new section is ~50 lines and the
consequential edits are net-neutral, landing near 450. The `skill-creator` hard
bar is <500, so it does not break — but sibling skills run 117–450, putting
this at the top of the range. If trimming is needed, cut wording, never content
(arbiter ruling, S17).

**Verification gap — checked, and it was real.** The flow's diagnosis quality
depends on the testers reporting enough detail to classify a failure.
`dotnet-integration-tester.md:61` carries the environment message verbatim in
its *Environment* section; `dotnet-unit-tester.md:81-104` has no such section.
In the originating incident the unit tester surfaced the blocking message
anyway, by improvising around its own template — which is not a property to
depend on. Closed by the additive section in Scope, above.

## Decisions and who made them

| Decision | Made by |
|---|---|
| Delete "Never scaffold a tier"; ask, then write | User |
| Measure the job and offer situational options, never bare yes/no | User |
| Downloading over the network requires asking | User |
| Both report-rule changes above | User, approved verbatim |
| No standing read-only lock is codified | User |
| One new named section rather than scattered edits | User (option A) |
| Three-way drafting loop waived for this bugfix | User |
| Add `### Environment` to the unit tester's report template | User, after the gap was verified |
| Repair belongs to the coordinator, not the testers | Session, from the agent files' own stated reasoning |
| Cap of 2 repair attempts | Session, by analogy with the skill's other caps |
| Missing-tests offer is standalone-only | Session |
