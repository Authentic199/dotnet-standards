# NO-SIGNAL branch for `dotnet-review-flow` — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A `/dotnet-review` run that cannot get a test signal still repairs what it can, offers the user options built from real numbers, and always delivers the review report.

**Architecture:** One new named unit, `NO-SIGNAL`, inside the existing shared block of `skills/dotnet-review-flow/SKILL.md`, between `TEST-LOOP` and `REVIEW-LOOP`. Six existing lines are rewired to route into it instead of halting or silently skipping. One additive report section in `agents/dotnet-unit-tester.md` so the unit tier has somewhere to put an environment message.

**Tech Stack:** Markdown skill and agent files in a Claude Code plugin. No code, no test framework. Verification is by `git diff`, `grep` for phrases that must be gone, line counts, plugin validation, and a real install check.

**Spec:** `docs/superpowers/specs/2026-07-28-review-flow-no-signal-design.md`

## Global Constraints

- **Artifact language is English.** Talk to the user in Vietnamese; every file written is English.
- **The heading `## The shared block: TEST-LOOP then REVIEW-LOOP` must not be renamed or reworded.** `skills/dotnet-feature-flow/SKILL.md:213` names it verbatim to run it as its PHASES 4–5. Renaming it silently breaks the sibling flow.
- **Do not fork a copy of the loops into another skill.** Same line, `SKILL.md:167-168`.
- **This skill teaches nothing.** No .NET doctrine, no test-writing guidance. Route to `dotnet-testing` for what a test looks like. A line of doctrine in this body is a defect (`SKILL.md:21-26`).
- **Line budget:** `skills/dotnet-review-flow/SKILL.md` is **399 lines** today. Hard bar is <500. Sibling skills run 117–450. Target ≤450. If over, cut wording, never content.
- **Counting lines in PowerShell:** use `(Get-Content <file>).Count`. `Measure-Object -Line` skips blank lines and under-reports by ~90 on this file.
- **Neither tester gains any power to repair.** The unit tester change is a report section only. Its bans on writing files, `dotnet add package`, and hand-managing containers stay exactly as they are.
- **Report-rule changes are frozen to the two the user approved verbatim** (Task 4). No other change to the report's structure, and no new report section.
- **Router needs no edit.** Verified: `choosing-a-dotnet-skill` references `dotnet-review-flow` by capability only (lines 57 and 82), never by its halt behaviour.
- **Commit style:** end every commit message with `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`. Multi-line messages go through a file: write to the scratchpad and use `git commit -F <path>`. PowerShell here-strings mangle these messages — do not use them.
- **Working directory:** the worktree `D:\AI-PLUGIN\dotnet-standards-laned-s18`, branch `lane-d/review-flow-no-signal`. Never edit `D:\AI-PLUGIN\dotnet-standards` — another session has uncommitted work there (`skills/claude-md-builder/`).

---

## File Structure

| File | Responsibility | Change |
|---|---|---|
| `skills/dotnet-review-flow/SKILL.md` | The orchestration graph | New `NO-SIGNAL` section; six rewired lines; two approved report-rule edits |
| `agents/dotnet-unit-tester.md` | Unit tier evidence | One added report section, `### Environment` |
| `CHANGELOG.md` | Release record | New version entry with the rulings |
| `.claude-plugin/plugin.json` | Plugin manifest | Version bump |
| `.claude-plugin/marketplace.json` | Marketplace manifest | Version bump — must match `plugin.json` |

---

### Task 1: Give the unit tester somewhere to put an environment message

**Files:**
- Modify: `agents/dotnet-unit-tester.md:81-104` (the report template)

**Interfaces:**
- Consumes: nothing.
- Produces: an `### Environment` section in the unit tester's report, which Task 2's NO-SIGNAL step 1 and step 2 read to diagnose and measure.

**Why this is not scope creep:** `agents/dotnet-integration-tester.md:59-63` already carries the environment message verbatim in its own *Environment* section. The unit tester's template has no such section, so a `RED — environment` verdict has only its one-sentence tail to carry the cause. In the incident that motivated this work, the unit tier was the failing tier and the agent surfaced the blocking message by improvising around its own template. NO-SIGNAL cannot depend on improvisation.

- [ ] **Step 1: Read the integration tester's Environment section to match its wording**

Run:
```powershell
Select-String -Path 'agents\dotnet-integration-tester.md' -Pattern 'Environment' -Context 3,3
```
Expected: its report template contains an `### Environment` section, and its prose at line ~59-63 says the section carries the message verbatim.

- [ ] **Step 2: Add the section to the unit tester's report template**

In `agents/dotnet-unit-tester.md`, inside the fenced report template, insert between the `### Build` block and the `### Results` block:

```markdown
### Environment
<on `RED — environment` only: the blocking message verbatim, and the command
that produced it. Otherwise `None.`>
```

- [ ] **Step 3: Add the one prose line that makes the section binding**

Immediately after the closing fence of the report template (currently `SKILL`-style prose beginning "The Verdict words are a closed set"), add before that sentence:

```markdown
On `RED — environment`, the *Environment* section carries the blocking message
**verbatim** — a file lock, an access-denied on `obj/` or `bin/`, a policy that
refused to load a built assembly. Paraphrasing it costs the flow the only
string it can classify the failure from.
```

- [ ] **Step 4: Verify the template still has every section and nothing else moved**

Run:
```powershell
git -C D:\AI-PLUGIN\dotnet-standards-laned-s18 diff --stat
Select-String -Path 'agents\dotnet-unit-tester.md' -Pattern '^### '
```
Expected: exactly one file changed. The section list reads `Commands run`, `Build`, `Environment`, `Results`, `Failures`, `Verdict`. No section removed, no section reordered other than the insertion.

- [ ] **Step 5: Confirm no repair power leaked in**

Run:
```powershell
Select-String -Path 'agents\dotnet-unit-tester.md' -Pattern 'dotnet add package|never as a routine|edits no source'
```
Expected: the existing prohibitions are still present and unmodified. If any prohibition text changed, revert and redo — this task adds a reporting slot and nothing else.

- [ ] **Step 6: Commit**

Write the message to the scratchpad, then:
```powershell
git -C D:\AI-PLUGIN\dotnet-standards-laned-s18 add agents/dotnet-unit-tester.md
git -C D:\AI-PLUGIN\dotnet-standards-laned-s18 commit -F <scratchpad>\msg1.txt
```
Message body:
```
feat(agents): give the unit tester an Environment section

The integration tester carries a blocking environment message verbatim in its
own report section; the unit tester's template had nowhere to put one, leaving
a one-sentence verdict tail as the only home. The unit tier is the tier that
failed in the incident behind this change, and it surfaced the message only by
improvising around its own template.

Reporting slot only. Every prohibition on repairing, writing files or managing
containers by hand is unchanged.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
```

---

### Task 2: Add the NO-SIGNAL section

**Files:**
- Modify: `skills/dotnet-review-flow/SKILL.md` — insert a new `### NO-SIGNAL` section between the end of `### TEST-LOOP` (ends at the "Cap: 5 rounds" paragraph, ~line 201) and the `### REVIEW-LOOP` heading (~line 203)

**Interfaces:**
- Consumes: the tester verdict strings `RED — environment` and `tier absent — nothing run`, and the unit tester's `### Environment` section from Task 1.
- Produces: the section name `NO-SIGNAL`, referenced by Task 3's six rewired lines and Task 4's report `### Run` field. Spell it exactly `NO-SIGNAL` — uppercase, hyphen — everywhere.

**Do not touch the `## The shared block: TEST-LOOP then REVIEW-LOOP` heading.** It is named verbatim by `skills/dotnet-feature-flow/SKILL.md:213`.

- [ ] **Step 1: Confirm the insertion point**

Run:
```powershell
Select-String -Path 'skills\dotnet-review-flow\SKILL.md' -Pattern '^### |^## '
```
Expected: `### TEST-LOOP` then `### REVIEW-LOOP` with nothing between them. Insert between those two.

- [ ] **Step 2: Insert the section**

```markdown
### NO-SIGNAL

Entered when a tester returns `RED — environment` or `tier absent — nothing
run`. Both mean one thing — **no evidence about the code under review, and
nothing in the code to fix** — so from here the flow treats them identically.
Splitting them is what let one of them deliver a report and the other deliver
nothing.

> **NO-SIGNAL may end in a question. It may never end in nothing delivered.**
> Whether repair succeeds, fails, or waits on an answer, REVIEW-LOOP still runs
> and the report is still produced. The lenses never depended on the tiers.

**1 — State it so the user can act on it.** Name what is missing and why, in
words that support a decision. A verdict string and an error code are a
symptom, not a diagnosis. A user who cannot tell what is being asked does not
answer, and an unanswered question is exactly how a run ends with nothing.

**2 — Measure before offering. Numbers, not adjectives.**

| Entry | Measure |
|---|---|
| `RED — environment` | What is blocking, taken from the tester's *Environment* section; whether it is repairable here; which rung of the table below it falls on |
| `tier absent — nothing run` | How many types in scope have no test, which tiers exist versus are empty, and whether the missing tier needs infrastructure stood up — that last one changes the size of the job by an order of magnitude |

"This would be a large job" is unusable. "Module X: 14 types, 0 tests" is a
decision input.

**3 — Repair, at most twice.** One question classifies every action: **does it
acquire something over the network?**

| Do it | Ask first | Never |
|---|---|---|
| Start containers whose images are already local | **Anything acquired over the network** — a missing package, an image not yet pulled | Anything irreversible on the user's machine |
| Re-run the pair **serially** — unit first, then integration — on an artifact lock, and note the serialization | Install software on the machine | Anything needing administrator rights |
| Re-run a command, read configuration | Edit project files, change ports, delete build caches | Anything governed by policy the user does not own |
| | | Edit a test to dodge a failure — the testers' ban, and it does not loosen because the coordinator is the one holding the pen |

**Two attempts, then explain and ask.** Every other loop here is capped; an
uncapped repair loop spends a session invisibly. An ordinary build restoring
its own packages is building, not repairing, and this table does not govern it
— a build that **fails because acquisition failed** is what enters here.

**4 — Offer options built from the measurement. Never a bare yes/no.** The list
is generated from what step 2 counted; it is not written down here, because a
fixed menu cannot know what was measured. It always includes *do nothing,
record it in the report*, and it includes a partial option whenever the
measurement decomposes into parts — one module rather than four, the unit tier
rather than both. Yes/no forces a user who has an hour to choose between
nothing and everything.

If the user accepts writing tests, **this session writes them** — the same
mechanism as the end-of-report offer, on the same authority: the user's answer.
What a test looks like belongs to `dotnet-testing`; none of it is taught here.
**This offer is standalone only.** Embedded under `dotnet-feature-flow`, tests
are written as the feature is built and the calling flow owns that. The repair
ladder above applies in both modes.

**Then continue to REVIEW-LOOP regardless.** Every tier that produced no signal
goes into *Not run* with what was attempted and what the user chose.
```

- [ ] **Step 3: Check the line budget**

Run:
```powershell
(Get-Content 'skills\dotnet-review-flow\SKILL.md').Count
```
Expected: ~450, and **must be under 500**. If over 450, cut wording — not content — and re-count.

- [ ] **Step 4: Confirm no doctrine leaked in**

Read the inserted section once against the skill's own rule at `SKILL.md:21-26`: it teaches nothing about .NET, about what a test asserts, or about how to configure a container. It names *who decides* and *what gets recorded*. If any sentence explains a .NET technique, delete it and route to the owning skill.

- [ ] **Step 5: Commit**

```powershell
git -C D:\AI-PLUGIN\dotnet-standards-laned-s18 add skills/dotnet-review-flow/SKILL.md
git -C D:\AI-PLUGIN\dotnet-standards-laned-s18 commit -F <scratchpad>\msg2.txt
```
Message body:
```
feat(review-flow): add the NO-SIGNAL section

A third named unit inside the shared block, between TEST-LOOP and REVIEW-LOOP.
It absorbs both ways a run ends up with no test evidence - the environment
blocked the tiers, or there were no tiers - because they mean the same thing to
a reader and only one of them used to deliver a report.

Diagnose in words the user can act on, measure in numbers, repair under a
capped ladder whose single classifier is whether an action acquires something
over the network, then offer options built from the measurement rather than a
yes/no. The invariant sits at the top: NO-SIGNAL may end in a question, never
in nothing delivered.

The shared block heading is untouched - dotnet-feature-flow names it verbatim.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
```

---

### Task 3: Rewire the six existing lines into NO-SIGNAL

**Files:**
- Modify: `skills/dotnet-review-flow/SKILL.md` — six sites

**Interfaces:**
- Consumes: the section name `NO-SIGNAL` from Task 2.
- Produces: a skill with no remaining instruction to halt on an environment failure and no remaining `Never scaffold a tier`.

Line numbers below are from the original 399-line file and will have shifted by Task 2's insertion. **Match on the quoted text, not the number.**

- [ ] **Step 1: Rewire the `tier absent` verdict row (was `:189`)**

Find:
```
| `tier absent — nothing run` | Does not block the loop, and **is not a pass.** Carry it into the final report under *Not run*. Never scaffold a tier |
```
Replace with:
```
| `tier absent — nothing run` | Does not block the loop, and **is not a pass.** Enter **NO-SIGNAL**, then carry the tier into the final report under *Not run* |
```

- [ ] **Step 2: Rewire the `RED — environment` verdict row (was `:192`)**

Find:
```
| `RED — environment` | **Halt immediately and surface it to the user. This does not consume a round.** No container runtime, an unreachable image, an artifact lock: there is nothing in the code to fix and another round fails identically. On an artifact lock specifically, re-run the pair **serially** — unit first, then integration — once, and note the serialization in the report before halting |
```
Replace with:
```
| `RED — environment` | **Enter NO-SIGNAL. This does not consume a round.** No container runtime, an unreachable image, an artifact lock: there is nothing in the code to fix, so an identical rerun is not the answer — repair, or record and continue, is |
```
The serial re-run on an artifact lock is not lost: it became a rung in NO-SIGNAL's repair table.

- [ ] **Step 3: Widen the REVIEW-LOOP entry condition (was `:205`)**

Find:
```
Entered **only with both tiers green** (or absent and recorded).
```
Replace with:
```
Entered **only with both tiers green** — or with a tier that produced no signal,
once NO-SIGNAL has recorded it. A blocked tier is not a failing tier, and the
lenses never depended on either.
```

- [ ] **Step 4: Rewire the Decision Guide row for no test projects (was `:387`)**

Find:
```
| The repository has no test projects at all | Both tiers absent: record it, report it under *Not run*, go straight to REVIEW-LOOP. Never scaffold a tier |
```
Replace with:
```
| The repository has no test projects at all | Both tiers absent. NO-SIGNAL: measure the gap, offer options built from the count, then REVIEW-LOOP either way |
```

- [ ] **Step 5: Rewire the Decision Guide row for `RED — environment` (was `:388`)**

Find:
```
| A tester reports `RED — environment` | Halt and surface it. Does not consume a round; on an artifact lock, re-run the pair serially once and note it |
```
Replace with:
```
| A tester reports `RED — environment` | NO-SIGNAL. Does not consume a round, and never halts the run — the report is owed regardless |
```

- [ ] **Step 6: Narrow PHASE 0's install ban (was `:80-81`)**

Find:
```
Four checks, in order. Each failure is a **STOP**: report what is missing, give
the exact remedy, and wait for the user. Do not degrade, do not work around, do
not install anything.
```
Replace with:
```
Four checks, in order. Each failure is a **STOP**: report what is missing, give
the exact remedy, and wait for the user. Do not degrade, do not work around,
and install nothing to get past a failed check here. Standing up a test
environment later, under NO-SIGNAL, is a different act with its own rules.
```
Left as written, this sentence contradicts NO-SIGNAL's repair ladder — one of them would have to be ignored, and nobody would know which.

- [ ] **Step 7: Add one Decision Guide row for the new unit**

After the `RED — environment` row, add:
```
| A repair inside NO-SIGNAL would download something | Ask first. That single question — does this acquire over the network — is the whole classifier |
```

- [ ] **Step 8: Verify both banned phrases are gone and nothing halts on environment**

Run:
```powershell
Select-String -Path 'skills\dotnet-review-flow\SKILL.md' -Pattern 'Never scaffold a tier'
Select-String -Path 'skills\dotnet-review-flow\SKILL.md' -Pattern 'Halt immediately'
Select-String -Path 'skills\dotnet-review-flow\SKILL.md' -Pattern 'NO-SIGNAL'
```
Expected: the first two return **nothing**. The third returns the section heading plus at least six references.

- [ ] **Step 9: Verify the protected heading survived**

Run:
```powershell
Select-String -Path 'skills\dotnet-review-flow\SKILL.md' -Pattern '^## The shared block: TEST-LOOP then REVIEW-LOOP$'
```
Expected: exactly one match. If zero, the heading was reworded — restore it verbatim before continuing, or `dotnet-feature-flow:213` breaks.

- [ ] **Step 10: Commit**

```powershell
git -C D:\AI-PLUGIN\dotnet-standards-laned-s18 add skills/dotnet-review-flow/SKILL.md
git -C D:\AI-PLUGIN\dotnet-standards-laned-s18 commit -F <scratchpad>\msg3.txt
```
Message body:
```
feat(review-flow): route the six no-signal sites into NO-SIGNAL

Both verdict rows, both Decision Guide rows, the REVIEW-LOOP entry condition,
and PHASE 0's install ban. Deletes "Never scaffold a tier" at both sites - a
user ruling of 2026-07-28, recorded in the spec so no later session restores it
as a lost invariant. Narrows PHASE 0's "install nothing" to the preflight it was
written for, so it no longer contradicts the repair ladder.

The artifact-lock serial rerun is not lost; it became a rung in NO-SIGNAL's
repair table.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
```

---

### Task 4: The two approved report-rule changes

**Files:**
- Modify: `skills/dotnet-review-flow/SKILL.md` — the `## The final report` intro and the `### Run` line of the template

**Interfaces:**
- Consumes: the section name `NO-SIGNAL` from Task 2.
- Produces: nothing later tasks read.

**These two edits and no others.** The user approved this exact wording and asked to be shown any further report-rule change before it is made. Adding a report section, renaming one, or reordering the template is out of bounds here — stop and ask instead.

- [ ] **Step 1: Widen "Always produced"**

Find:
```
**Always produced** — in both modes, when everything passed, and when a cap
halted the run. Every section appears; write `None.` when empty.
```
Replace with:
```
**Always produced** — in both modes, when everything passed, when a cap halted
the run, and when NO-SIGNAL ended in an unanswered question. **There is no path
through this flow that ends without the report.** Every section appears; write
`None.` when empty.
```

- [ ] **Step 2: Add NO-SIGNAL to the Run line**

Find:
```
TEST-LOOP <n> of 5 · REVIEW-LOOP <n> of 3 · <cap hit? say so> · <commands the flow ran>
```
Replace with:
```
TEST-LOOP <n> of 5 · REVIEW-LOOP <n> of 3 · NO-SIGNAL <what was attempted, what
the user chose, or "not entered"> · <cap hit? say so> · <commands the flow ran>
```

- [ ] **Step 3: Verify no other report change crept in**

Run:
```powershell
git -C D:\AI-PLUGIN\dotnet-standards-laned-s18 diff -- skills/dotnet-review-flow/SKILL.md
Select-String -Path 'skills\dotnet-review-flow\SKILL.md' -Pattern '^### ' | Select-Object Line
```
Expected: the diff shows exactly the two hunks above. The report template's section list is unchanged — `Verdicts`, `Tests`, `CONFIRMED findings`, `PLAUSIBLE findings`, `Unfixed MEDIUM and INFO`, `Not run`, `Run`. If any section was added, removed or renamed, revert it.

- [ ] **Step 4: Commit**

```powershell
git -C D:\AI-PLUGIN\dotnet-standards-laned-s18 add skills/dotnet-review-flow/SKILL.md
git -C D:\AI-PLUGIN\dotnet-standards-laned-s18 commit -F <scratchpad>\msg4.txt
```
Message body:
```
feat(review-flow): report rules cover the NO-SIGNAL exit

Two changes, both approved verbatim by the user before they were made. The
"always produced" clause now names the NO-SIGNAL exit explicitly and carries a
closing sentence - there is no path through this flow that ends without the
report - because that is the exact failure this whole change exists to prevent.
The Run line records what NO-SIGNAL attempted and what the user chose.

No new report section. A deferred choice goes into the existing Not run section
with its reason.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
```

---

### Task 5: Version, changelog, validation, and install proof

**Files:**
- Modify: `CHANGELOG.md`, `.claude-plugin/plugin.json`, `.claude-plugin/marketplace.json`

**Interfaces:**
- Consumes: everything above.
- Produces: the shipped version.

**Read the version number off `main` first, not off this branch.** Another session is shipping `claude-md-builder` concurrently and will bump the version. The target number is whatever `main` holds **plus one patch** at the moment of merge, and it may move twice.

- [ ] **Step 1: Merge `main` into this branch before touching manifests**

```powershell
git -C D:\AI-PLUGIN\dotnet-standards-laned-s18 fetch
git -C D:\AI-PLUGIN\dotnet-standards-laned-s18 merge main
```
Resolve conflicts if the other session touched `CHANGELOG.md` or the manifests. Expected conflict sites: `CHANGELOG.md` (both add a top entry) and both manifests (both bump a version). Take the other session's version number as the base and increment from it. Doing this **before** editing the manifests is the S17 rule; editing first invites the conflict.

- [ ] **Step 2: Read the current version from both manifests**

```powershell
Select-String -Path '.claude-plugin\plugin.json','.claude-plugin\marketplace.json' -Pattern '"version"'
```
Expected: both files agree. If they disagree, stop — that is a pre-existing defect and shipping on top of it hides it.

- [ ] **Step 3: Bump both manifests to the next patch version**

Edit both files to the same new number. **Both must agree** — a mismatch is the failure mode called out in the repo brief.

- [ ] **Step 4: Add the CHANGELOG entry**

Add a new top entry under the new version recording:
- NO-SIGNAL added; environment failures and absent tiers unified
- `Never scaffold a tier` deleted at both sites — user ruling, 2026-07-28
- the repair ladder and its single classifier ("does it acquire over the network"), capped at two attempts
- options must be built from a measurement, never a bare yes/no
- PHASE 0's install ban narrowed to the preflight
- the two report-rule changes, user-approved verbatim
- the unit tester's `### Environment` section
- explicitly: neither tester gained any power to repair

- [ ] **Step 5: Validate the plugin**

```powershell
claude plugin validate D:\AI-PLUGIN\dotnet-standards-laned-s18
```
Expected: no errors. Fix anything reported before continuing.

- [ ] **Step 6: Confirm the skill still fits its budget**

```powershell
(Get-Content 'skills\dotnet-review-flow\SKILL.md').Count
```
Expected: ≤450, hard fail above 500.

- [ ] **Step 7: Commit, merge to main, and prove the install**

Commit the manifests and changelog, merge the branch into `main`, then:
```powershell
claude plugin update dotnet-standards@dotnet-standards-dev
claude plugin details dotnet-standards
```
Then verify the install actually moved — `details` alone is never proof:
- open `installed_plugins.json` and confirm it points at the **new** cache directory and that `gitCommitSha` matches the merge commit
- delete `reference/` from the new cache directory
- confirm both manifests in the cache agree on the version
- check `installed_plugins.json` before deleting **any** cached version directory

- [ ] **Step 8: Close the lane**

Rewrite `CLAUDE.md` and the Lane D lane file for the next session, carrying the Lane log. Update the LANE BOARD row. Record in the Lane log:
- the three-way drafting loop was waived by the user for this bugfix
- the report-rule approval gate is now a standing rule (also saved to memory)
- the unit tester `### Environment` gap was found by verification, not assumed
- the originating incident: Smart App Control, and the `dotnet test` exit-0-on-zero-tests false pass observed alongside it

---

## Self-Review

**Spec coverage:** NO-SIGNAL section → Task 2. Invariant → Task 2 Step 2. Four steps → Task 2 Step 2. Repair ladder and cap → Task 2 Step 2. Six consequential edits → Task 3 Steps 1–6. Two report-rule changes → Task 4. Unit tester `### Environment` → Task 1. Line budget risk → Task 2 Step 3 and Task 5 Step 6. Standalone-only scope of the missing-tests offer → found missing on review and fixed inline: Task 2 Step 2's step-4 paragraph now states it, and states that the repair ladder applies in both modes.

**Placeholder scan:** every step carries its exact find/replace text or its exact command. `<scratchpad>` in commit commands is the session scratchpad path, substituted at execution.

**Naming consistency:** `NO-SIGNAL` is spelled uppercase-hyphen in Tasks 2, 3, 4 and 5. `### Environment` is spelled identically in Task 1 and Task 2's measurement table.
