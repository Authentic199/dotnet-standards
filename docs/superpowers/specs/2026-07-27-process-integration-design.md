# Process Integration — closed-loop workflows inside `dotnet-standards`

**Date:** 2026-07-27 · **Status:** approved design, awaiting implementation lane
(runs AFTER the four review rubrics and `dotnet-testing` ship — hard sequencing
constraint, see §7). Brainstormed and approved section-by-section in the S14
session (Lane C), under `superpowers:brainstorming`.

## 1. Goal

Instead of remembering and invoking each Superpowers skill by hand, the user
runs ONE command and an agentic flow walks the whole process end to end,
combining Superpowers (process layer) with dotnet-standards (knowledge layer).
A second requirement: installing `dotnet-standards` must ensure Superpowers is
present — Claude Code has no native plugin-dependency mechanism, so the plugin
builds its own check.

**Decision (user, explicit): this lives INSIDE `dotnet-standards` (approach 2),
deliberately breaking the "Knowledge only" promise in the current plugin
description.** The description gets rewritten (§5). A separate workflows plugin
was considered and rejected by the user.

## 2. Scope

- **v1:** `feature` workflow + `review` workflow.
- **v1.5 (immediately after):** `bugfix` workflow (systematic-debugging → fix →
  reuses the shared TEST/REVIEW blocks; ~70% shared with `feature`).
- **Deferred (recorded, not designed):** a PM workflow — from BA requirements /
  ad-hoc tasks / raw ideas to a task skeleton + project overview. The user
  judged Superpowers + the kit currently unfit for this; it is technical debt
  for a later addition. Also deferred: a project-setup workflow (per-project
  `CLAUDE.md` best practices).

## 3. Architecture — two layers inside one plugin

```
dotnet-standards/
├── skills/<knowledge skills>          # existing layer, unchanged
├── skills/dotnet-feature-flow/        # NEW — orchestration skill: full process graph
├── skills/dotnet-review-flow/         # NEW — orchestration skill: review fleet + fix loop
├── commands/
│   ├── dotnet-feature.md              # NEW — thin deterministic entry → invokes dotnet-feature-flow
│   └── dotnet-review.md               # NEW — thin deterministic entry → invokes dotnet-review-flow
├── agents/                            # NEW — specialist subagents (see §4)
└── hooks/hooks.json                   # + SessionStart superpowers-check (reuses run-hook.cmd)
```

Principles:
- **Command = deterministic entry, skill = process graph.** All logic (phases,
  gates, loops, stop conditions) lives in the flow skill; the command only
  invokes it. The skill also self-triggers on description match, so both entry
  paths exist. Command names carry the `dotnet-` prefix to avoid built-in
  collisions (`/review`, `/code-review`, …).
- **Flow skills teach nothing.** They CALL Superpowers skills for process
  (brainstorming, writing-plans, test-driven-development,
  subagent-driven-development, using-git-worktrees,
  finishing-a-development-branch, verification-before-completion) and POINT
  subagents at dotnet-standards knowledge/rubric skills for content. The
  relationship to Superpowers is *call*, never *copy* — no Superpowers file is
  ever modified.
- Flow-skill descriptions still follow the §5 description law (third person,
  <100 words, `Not for:` routing to each other and to Superpowers for
  process-only questions).

## 4. Specialist agents (user ruling: one agent per concern, no sharing)

The user explicitly wants agent-per-concern so no agent inherits another's
context or specialty. Roster:

| Agent | Binds to | Tools |
|---|---|---|
| `dotnet-code-reviewer` | rubric skill `dotnet-code-review` | read-only |
| `dotnet-architecture-reviewer` | rubric skill `dotnet-architecture-review` | read-only |
| `dotnet-security-reviewer` | rubric skill `dotnet-security-review` | read-only |
| `dotnet-performance-reviewer` | rubric skill `dotnet-performance-review` | read-only |
| `dotnet-unit-tester` | `dotnet-testing` (unit conventions) | read + run (build/test), no source edits |
| `dotnet-integration-tester` | `dotnet-testing` (integration conventions) | read + run, no source edits |

- Review agents are **read-only by configuration**, not by promise — a reviewer
  cannot "fix it while I'm here". Finding vs fixing is a hard boundary: agents
  find, the main flow fixes.
- Tester agents run the suite and report structured failures; they never edit
  source — the fix always returns to the implementer.
- The final test-agent roster follows whatever test kinds `dotnet-testing`
  actually ships (it defines the stack's test taxonomy; the roster mirrors it,
  adding nothing).
- **Implementation subagents are NOT specialist agents** — implementation uses
  Superpowers' `subagent-driven-development` as designed (generic subagent per
  plan task, prompt carries pointers to the relevant knowledge skills).
- **Sequencing consequence:** these agents cannot be authored until the four
  rubrics and `dotnet-testing` exist. The user's full intent is recorded here
  precisely so the implementation lane can run later without re-deriving it.

## 5. Dependency mechanism (user chose: warn early + hard-stop at use)

1. **SessionStart hook** (added beside the existing PostToolUse format hook,
   reusing the shipped polyglot `run-hook.cmd` + extension-less script
   convention): reads `installed_plugins.json`; if no `superpowers@…` entry,
   prints a loud warning with the exact install command
   (`claude plugin install superpowers@claude-plugins-official`) and a restart
   reminder. **Warn only — never block the session** (knowledge-only sessions
   are legitimate).
2. **PHASE 0 of every flow skill hard-STOPs** when Superpowers is missing
   (report what is missing, give the install command, wait) — the house
   STOP-prerequisite idiom. PHASE 0 also checks the plugin's own completeness:
   the four rubric skills and `dotnet-testing` must be present in the installed
   version (guards against a stale cache/partial install).
3. **No auto-install** (user rejected): installing requires a session restart
   to take effect (proven twice in S14 with skill-creator), so silent install
   both overreaches and still needs manual action.
4. `plugin.json` description rewritten: drop "Knowledge only — the
   brainstorm/plan/TDD/review process stays with Superpowers"; describe the two
   layers; state that workflows sit ON TOP of Superpowers and require it.

## 6. Workflow anatomies

### 6.1 `feature` (`/dotnet-feature` → `dotnet-feature-flow`)

```
PHASE 0  Preflight — STOP if Superpowers missing / not a git repo / rubrics or
         dotnet-testing absent from install. Parallel work wanted? →
         superpowers:using-git-worktrees.
PHASE 1  Brainstorm — superpowers:brainstorming (interactive dialogue).
PHASE 2  Plan — superpowers:writing-plans.
         ── GATE 1 (human): user approves design + plan ──
PHASE 3  Implement:
         • plan ≤ 3 use-cases → TDD in the main session
           (superpowers:test-driven-development), per-task subagents if the
           plan says so
         • plan > 3 use-cases → superpowers:subagent-driven-development
           (fresh-context subagent per task; task prompts carry pointers to the
           relevant knowledge skills)
PHASE 4  TEST-LOOP: spawn dotnet-unit-tester + dotnet-integration-tester in
         parallel → failures → fix (main/implementer) → rerun.
         STOP when build + full suite green. Safety cap: 5 rounds → halt & ask.
PHASE 5  REVIEW-LOOP: spawn all 4 review agents in parallel on the diff →
         verify findings against code (CONFIRMED vs PLAUSIBLE) → fix CONFIRMED
         blocking/major → back through PHASE 4 (always re-verify before
         re-review) → re-review.
         STOP when no CONFIRMED blocking/major remain; minors go to the final
         report. Safety cap: 3 rounds → halt & ask.
PHASE 6  Git — superpowers:finishing-a-development-branch: commit.
         ── GATE 2 (human): user approves before any push ──
```

Test-before-review ordering is deliberate: tests are cheap and mechanical,
review is expensive; reviewers never see non-green code; every review-fix
iteration re-tests anyway.

The main session's role is **coordinate + fix only** — it never reviews or
tests (the user's context-contamination rule: review/test judgment must come
from fresh-context subagents).

### 6.2 `review` (`/dotnet-review` → `dotnet-review-flow`)

Exactly PHASES 4+5 extracted, run standalone against an existing diff/branch —
one shared block, two entries. Ends with the findings report; fixing is offered,
not automatic, when invoked standalone.

### 6.3 `bugfix` (v1.5)

PHASE 0 → superpowers:systematic-debugging → fix → shared TEST/REVIEW blocks →
git. Designed in the implementation lane, reusing §6.1's blocks.

## 7. Sequencing constraint

This lane runs AFTER: the four review rubrics (`dotnet-code-review`,
`dotnet-architecture-review`, `dotnet-security-review`,
`dotnet-performance-review`) and `dotnet-testing` have shipped. The review
agents and tester agents bind to those skills; building the flows first would
mean flows that reference skills that do not exist.

## 8. Error handling & edge conditions

- **Loop caps:** review-loop 3 rounds (user-set), test-loop 5 rounds; hitting a
  cap halts with a status summary and asks the user. No unbounded loops.
- **Findings verified before fixed** (S13/S14 arbiter lessons): only CONFIRMED
  blocking/major findings force the loop; PLAUSIBLE and minor findings are
  reported, not chased.
- **Subagent failure:** one retry with the identical prompt; still failing →
  surface to the user; never silently drop a lens or a test kind.
- **Git safety:** no push without GATE 2; worktree lifecycle owned by the flow
  (remove only after merge); commit messages follow the target repo's
  convention.
- **Final report always:** unfixed minors, loop counts, suites passed — nothing
  the subagents learned is lost to the user.

## 9. Open items for the implementation lane

1. **Verify plugin-command namespacing/collision behavior** against current
   Claude Code docs (claude-code-guide agent) before naming the commands —
   do not guess.
2. Test-agent roster finalized against the shipped `dotnet-testing` taxonomy.
3. Severity vocabulary (blocking/major/minor) aligned with whatever the shipped
   rubrics actually define — the rubrics own the scale; the flows consume it.
4. Whether `dotnet-review-flow` needs a `references/` file or stays a
   single-body skill (the split goes through the three-way loop as always).
