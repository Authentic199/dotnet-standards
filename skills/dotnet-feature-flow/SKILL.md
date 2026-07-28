---
name: dotnet-feature-flow
description: >-
  This skill should be used when taking a .NET feature from idea to commit in one
  run: /dotnet-feature, "build this feature end to end", "implement X and get it
  merge-ready", walking brainstorm, plan, implementation, tests, the four review
  lenses and the git step with human gates in between. Not for: reviewing an
  existing diff or branch on its own — dotnet-review-flow; which skill owns a
  convention — choosing-a-dotnet-skill; what each lens checks —
  dotnet-code-review, dotnet-architecture-review, dotnet-security-review,
  dotnet-performance-review; test conventions — dotnet-testing; brainstorming,
  planning and TDD themselves — Superpowers.
---

## Overview

This skill is an **orchestration graph, not a rubric.** It walks one .NET change
from an idea to a reviewed commit, deciding only *which phase runs, who runs it,
and what must be true before the next one starts*.

**It teaches nothing.** Every process step is a Superpowers skill this flow
**calls and never copies**. The test-and-review machinery belongs to
`dotnet-review-flow`, which this flow **invokes and never forks**. Every .NET
convention belongs to a knowledge skill this flow **points at and never
restates**. A line of doctrine or a copied loop appearing in this body is a
defect: it becomes a stale second source of truth the day its owner changes.

### The phase map

| # | Phase | Owner |
|---|---|---|
| 0 | Preflight and workspace | This flow — STOP on any failure |
| 1 | Brainstorm | `superpowers:brainstorming` |
| 2 | Plan | `superpowers:writing-plans` |
| — | **GATE 1 — human** | The user approves design and plan |
| 3 | Implement | `superpowers:test-driven-development` or `superpowers:subagent-driven-development` |
| 4–5 | Test and review | `dotnet-review-flow`, embedded |
| 6 | Git — **GATE 2 inside it** | `superpowers:finishing-a-development-branch` |

Phases run in order. A phase is not skipped because it "looks unnecessary"; where
one genuinely does not apply, say so in the closing summary under *Phases*.

## Core principles

1. **Gates are human, hard, and not inferable.** GATE 1 and GATE 2 pass when the
   user says so. Silence is not approval, an earlier "sounds good" about a
   different thing is not approval, and a plan the user has not seen cannot have
   been approved. Announcing an intention and continuing is not a gate — it is a
   notification, and the two failures that permits (implementing an unapproved
   design, publishing an unapproved branch) are the most expensive things this
   flow can do.

2. **This session implements and fixes; it never grades its own work.** Writing
   code here is the job. Deciding whether that code is correct, safe, fast or
   well-placed is not: that judgement comes only from the fresh-context fleet
   inside the shared block. A session that just wrote the code cannot un-know its
   intent while grading it, which is why PHASES 4–5 are delegated whole rather
   than approximated here.

3. **Call the sibling flow; never fork it.** The loops, the caps, the
   CONFIRMED/PLAUSIBLE rule and the block's report shape live in
   `dotnet-review-flow`. A second copy in this file would drift, and the day it
   drifts a feature run and a review run stop meaning the same thing.

4. **One deliverable per run.** This flow takes one feature to a commit. A second
   request arriving mid-flow is recorded under *Outstanding* and deferred, not
   folded in — the plan the user approved at GATE 1 is the plan that gets built.

5. **Nothing is silently dropped.** A phase skipped, a gate answered
   conditionally, a lens that did not run, a finding left unfixed, a cap that
   halted a loop: each appears in the closing summary by name. Silence reads as
   completion, and this flow's output is a claim about what actually happened.

## PHASE 0 — Preflight, and STOP on any failure

**The first three checks are `dotnet-review-flow`'s, in its section
`PHASE 0 — Preflight, and STOP on any failure`. Run them from there** — that is
the shared definition and it is not restated at length here. In compact form:

1. **Superpowers is present and enabled** — proved by *loading*
   `superpowers:verification-before-completion`, never by reading a registry
   file. STOP with the install command and the restart note.
2. **A git repository is present**, with a resolvable base.
3. **This plugin is complete** — the four rubric skills, `dotnet-testing` and the
   six agents present by name in this session's rosters. A missing name means a
   stale or partial install: STOP with the update-and-restart remedy.

Its fourth check — state the review target — does not apply yet: there is no diff
until PHASES 4–5. **State the feature being built instead**, in one line, and get
it acknowledged before PHASE 1.

Two checks are this flow's alone:

**A — Is there a plan or spec already?** If the user arrived with one, PHASES 1
and 2 may collapse into confirming it at GATE 1. Ask; assume neither way. GATE 1
itself is never skipped.

**B — Decide the workspace, and own it from here.** Ask whether this work should
be isolated: parallel work in the same repository, an experiment that may be
abandoned, anything the user does not want in their current tree. If yes, invoke
`superpowers:using-git-worktrees` and let it create the workspace before PHASE 1
— that skill owns the mechanism.

**Worktree lifecycle belongs entirely to this flow.** `dotnet-review-flow` owns
none of it and must never be asked to clean it up. Created here, used by every
later phase, and **removed only after the merge** — a worktree removed at "the
work is done" takes unmerged commits with it, and that loss is silent. If the run
halts at a gate or a cap, **leave the worktree in place** and say in the summary
that it is still there and where; an abandoned run whose workspace was quietly
deleted cannot be resumed.

## PHASE 1 — Brainstorm

Invoke `superpowers:brainstorming` and follow it. This is an **interactive
dialogue with the user**, not a design this session writes and presents. Do not
shortcut it because the request arrived sounding complete — a request that sounds
complete is the one whose assumptions were never said out loud.

Name the .NET areas the design touches as they surface, and **resist implementing
during the conversation**: code written before GATE 1 is code written against an
unapproved design.

Output: an agreed design, in the user's words as much as yours.

## PHASE 2 — Plan

Invoke `superpowers:writing-plans` and follow it. The plan is what GATE 1 approves
and what PHASE 3 executes, so it must be specific enough to hand to a subagent
that was not in this conversation.

**Every plan step that touches an area a shipped skill owns must name that skill.**
That rule is `choosing-a-dotnet-skill`'s, in its section *When the work is being
planned, not yet written*: a generic step carries none of the nouns these skills
trigger on, so nothing fires exactly when the conventions are being decided. Load
the router while writing the plan and follow it; do not reproduce its base map
here.

**A step in an area no shipped skill owns has nothing to name** — the router says
so itself. Note that in the step and move on; do not stall hunting for an owner
that does not exist.

Those names are what PHASE 3's prompts carry. A step with an owner and no name is
a step whose implementer works from memory.

## GATE 1 — the user approves the design and the plan

**STOP. Present the design and the plan. Wait for an answer.**

- **Approved** — proceed to PHASE 3 with that plan.
- **Changes wanted** — return to PHASE 1 or PHASE 2 as the feedback requires, then
  present again. A revised plan needs its own approval. There is no partial
  approval and no "start on the parts we agree on".
- **Partial, conditional, or no answer** — not approved. Name what is unresolved
  and wait.
- **No implementation before this gate.** Not a scaffold, not "just the entity",
  not a spike that stays. Code written before approval is code the user now argues
  against instead of a plan.

## PHASE 3 — Implement

Branch on the plan's size, counted in **use-cases**.

**How to count a use-case:** a unit of delivered behaviour the user would
recognise as a thing the feature does — "create an order", "cancel an order",
"list orders with paging". It is **not** the plan's task count and not a file
count: `writing-plans` structures work as `### Task N`, and one use-case commonly
spans several tasks. Group the plan's tasks into the behaviours they deliver and
count those. **If you cannot decide whether something is one use-case or two,
count it as two** — over-routing to subagent-driven development merely costs
time, while under-routing puts a large plan through single-session TDD and runs
out of context mid-feature.

State the count and the route to the user before starting.

| Plan size | Route |
|---|---|
| **≤ 3 use-cases** | TDD in this session — invoke `superpowers:test-driven-development` and follow it. Use per-task subagents **when the plan says so**, not by preference |
| **> 3 use-cases** | Invoke `superpowers:subagent-driven-development` and follow it — a fresh-context subagent per plan task |

**Either route, the same prompt requirement:** every task prompt carries the
**pointers to the owning knowledge skills** that PHASE 2 recorded for that step. A
fresh-context subagent has no knowledge of this stack's conventions until a prompt
names the skill that holds them — that is the entire reason the plan records them.

Two rules survive both routes:

- **The plan is the contract.** A task that turns out to need a different approach
  goes back to the plan, and a plan change large enough to alter the design returns
  to GATE 1. Do not renegotiate scope inside an implementation task.
- **Red-green-refactor belongs to Superpowers**, whichever route ran. This flow does
  not describe TDD, restate its cycle, or offer a lighter version of it.

Implementation subagents are **ordinary** Superpowers subagents. The six specialist
agents are test and review agents only: never spawn a reviewer or a tester to write
code, and never ask an implementation subagent to review its own output.

## PHASES 4–5 — Test and review, embedded

**Invoke `dotnet-standards:dotnet-review-flow` with the Skill tool, stating
explicitly in the invocation that this flow is running its shared block embedded
as PHASES 4–5.** That statement is what selects embedded mode; **absent it the
sibling defaults to standalone**, reports, and offers to fix instead of looping
with an implementer. Say it in those terms.

Then, from that skill and in its order:

1. Perform its section **`Diff preparation — the spawn contract`** — resolve the
   base ref, write the diff outside the repository, derive the changed-file list,
   assemble the four spawn inputs, and run its pre-build gate. **This is on this
   flow's side of the seam**: the flow owns the repository, the branch and the
   working tree, so the flow does the git and the gate. On a failed gate build,
   nobody is spawned and the diagnostics come straight back here.
2. Run its section **`The shared block: TEST-LOOP then REVIEW-LOOP`** exactly as
   written.

**What belongs to which side:**

| Owned by `dotnet-review-flow` | Owned by this flow |
|---|---|
| The two loops, their order, their stop conditions | The repository, the branch, the worktree |
| The round caps and what happens when one halts | Being the implementer that applies every fix |
| The spawn contract's shape and the agent roster | Executing diff preparation and the pre-build gate |
| CONFIRMED versus PLAUSIBLE verification | Carrying the block's report into the closing summary |
| The severity ladder and the block's report shape | Everything before PHASE 4 and after PHASE 5 |
| Subagent-failure retry and the *Not run* discipline | Deciding the fix route (below) |

**How this flow fixes — match the route PHASE 3 took.** On the TDD route, fix in
this session. On the subagent-driven route, **dispatch a fixer subagent the same
way PHASE 3 dispatched an implementer**, carrying the same knowledge-skill
pointers plus the finding. The reason is context symmetry: a change built by
fresh-context subagents lands in a session that never read most of it, and fixing
from here would mean editing code this session does not hold.

Fix failing tests, and **CONFIRMED CRITICAL and HIGH findings only**. MEDIUM and
INFO are never chased — they travel to the closing summary. Never raise a cap that
belongs to the sibling, and never review or test anything yourself.

**When a cap halts the block, that halt is this flow's halt.** Bring the sibling's
status summary to the user and wait. Never continue to PHASE 6 with a red suite or
an outstanding CONFIRMED finding on the strength of "close enough".

**Never fork the loops into this body.** One definition, invoked twice; a copy
diverges on the first edit and nobody notices which one the run used.

## PHASE 6 — Git, with GATE 2 inside it

Invoke `superpowers:finishing-a-development-branch` and follow it.

**Commit.** Commit messages follow **the target repository's own convention** —
read the recent history and match it. This plugin's conventions are not the
reviewed repository's.

### GATE 2 — the user approves before anything is published

`superpowers:finishing-a-development-branch` presents integration options — merge
locally, or push and open a pull request — and the human partner chooses.
**GATE 2 is that choice, and this flow must not answer it.**

**STOP at the option choice. Present what would be published and where. Wait.**

Do not select a pushing option, do not accept one by default, and do not treat a
merge workflow that implies a push as already approved. Not a push to a feature
branch, not a "safe" push to a remote nobody watches, and no `--force` of any kind
— that skill's own rules forbid a force-push without an explicit human request,
and so does this gate.

Placing the gate at the option choice rather than after the phase is deliberate: a
gate that arrives after the skill has already offered and taken an option is a
notification. Committing locally is reversible; publishing is not.

**Remove the worktree only after the merge** — the rule from PHASE 0 check B, and
the only point at which removal is safe.

## The closing summary

**Always produced** — when everything shipped, when a gate stopped the run, and
when a cap halted a loop.

The shared block produced its own report inside PHASES 4–5. **Carry it through
whole; do not summarize it away** — its per-lens verdicts, CONFIRMED and PLAUSIBLE
lists, unfixed MEDIUM and INFO, and *Not run* section are most of what the run
bought, and a paraphrase is this flow asserting a result it did not produce. This
flow adds a wrapper above it. Every line appears; write `None.` when empty.

```markdown
## Feature: <what was built>

### Outcome
<shipped and committed / halted at GATE n / halted at a cap — and what remains>

### Phases
0 Preflight · 1 Brainstorm · 2 Plan · 3 Implement · 4–5 Test and review · 6 Git
<ran / skipped and why, per phase>

### Gates
GATE 1 <approved, or not reached> · GATE 2 <approved, or not reached>

### Implementation
<route taken and why · use-case count · the knowledge skills the plan named ·
plan deviations>

### Shipped
<branch · commits · merged or awaiting GATE 2 · worktree created, removed, or
still present and where>

### Outstanding
<requests deferred mid-flow · anything a cap left behind · anything the user
deferred at a gate · the count of unfixed MEDIUM and INFO, which are listed in
full in the block report below — not repeated here>

### Review block
<dotnet-review-flow's own final report, carried through untouched>
```

Before declaring the run complete, invoke
`superpowers:verification-before-completion` and follow it. This flow's closing
claim is that a feature was built, tested, reviewed and committed; that skill is
what turns the claim into evidence.

## Routing

**Sibling flow.** Reviewing an existing diff or branch with no feature attached is
`dotnet-review-flow`, run standalone. This flow calls its shared block; there is
only one copy and the two never diverge.

**Process.** Brainstorming, plan writing, TDD, subagent-driven development,
worktrees, finishing a branch and verification before completion belong to
Superpowers, which this flow calls and never copies. Executing cleanup candidates
belongs to `/simplify`.

**Content.** Which knowledge skill owns a convention is `choosing-a-dotnet-skill`.
What each review lens checks lives in the four rubrics; what a test looks like
lives in `dotnet-testing`. This flow loads none of them to do its own work — the
plan names them, and the implementers and agents load them.

## Decision Guide

| Situation | Do this |
|---|---|
| Invoked with a one-line feature request | Still PHASE 0, then PHASE 1. A request that sounds complete is one whose assumptions were never stated |
| The user already has a spec or plan | Confirm it at GATE 1; PHASES 1–2 may collapse into that confirmation. Never skip GATE 1 itself |
| The user says "just build it, skip the planning" | The plan is what GATE 1 approves and what PHASE 3's prompts are built from. Say what is lost and ask; do not proceed by inference |
| The user wants isolation, or parallel work is in flight | `superpowers:using-git-worktrees` at PHASE 0 check B. This flow owns the worktree from creation to post-merge removal |
| Superpowers will not load, or a rubric or agent name is missing | STOP at PHASE 0 with the sibling's remedy. Never hand-roll the process or the fleet |
| GATE 1 gets a partial or conditional answer | Not approved. Name what is unresolved and wait |
| The plan is exactly 3 use-cases | TDD in this session; the boundary is *more than* 3 |
| Unsure whether something is one use-case or two | Two. Over-routing costs time; under-routing runs out of context mid-feature |
| A plan step names no knowledge skill and the area has an owner | Fix the plan before PHASE 3 — that is the router's rule, not an optional nicety |
| A plan step is in an area no shipped skill owns | Nothing to name. Say so in the step and move on |
| A task turns out to need a different approach | Back to the plan; back to GATE 1 if the design moves |
| A subagent asks which convention applies | Point it at the owning skill; do not answer from this body |
| PHASES 4–5 are about to start | Invoke the sibling and **say it is embedded**. Silence means standalone, and standalone offers instead of fixing |
| A CONFIRMED finding needs fixing | Match PHASE 3's route: TDD route fixes here, subagent route dispatches a fixer |
| The urge to run a test or read the diff for a verdict | Out of bounds — that judgement is the fleet's. Fix what it CONFIRMS |
| A cap halts the shared block | Its halt is this flow's halt. Carry the status summary to the user and wait; never proceed to PHASE 6 |
| Only MEDIUM and INFO findings remain | Proceed to PHASE 6 and carry them into the block report. They are never chased |
| A CONFIRMED HIGH is real but outside this feature's scope | Ask the user; never silently demote it to clear the gate |
| `finishing-a-development-branch` offers its integration options | That is GATE 2. Present them, wait, and let the user choose — including any force push |
| The worktree looks finished before the merge | Leave it. Removal is post-merge only |
| The run ends at a gate or a cap | Leave the worktree, produce the closing summary anyway, and say what is unfinished |
| A new request arrives mid-flow | Record it under *Outstanding* and finish the one deliverable |
| Asked to review an existing branch with no feature to build | Wrong skill — `dotnet-review-flow`, standalone |
