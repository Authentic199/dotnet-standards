# Design — process handback: keeping this plugin present in a Superpowers-driven session

**Date:** 2026-08-02 · **Lane:** solo (Lane D track, maintenance) ·
**Branch:** `lane-d/process-handback`
**Origin:** the field report copied into this tree as
`2026-08-02-skill-routing-failure-postmortem.md` (moved to `docs/` by this
change — see Scope).

## The problem

One consumer session — building an access-control module on
`feature/access-control-core` — skipped this plugin twice.

**Incident A, design phase.** An architecture spec for a MediatR module was
written without loading `mediatr-messaging`, `module-feature`,
`facade-module-architecture`, `ef-core-data-access` or `api-surface`. It placed
handlers wrongly, anchored `AddMediatR` wrongly, and invented a `Contracts/`
folder outside the house vocabulary. Caught by the user at spec review.

**Incident B, review phase.** Across 20+ subagent review rounds, including the
final whole-branch review, **not one** of `dotnet-review-flow`,
`dotnet-code-review`, `dotnet-architecture-review`, `dotnet-security-review`,
`dotnet-performance-review` was loaded, and none of the six specialist agents
was spawned. Every round ran `general-purpose` with a constraint block the
coordinating session wrote by hand.

### What was verified, and where

Every quotation below was re-checked against Superpowers **6.2.0** in this
machine's plugin cache on 2026-08-02. This section states measurements, not
inferences.

| Fact | Evidence |
|---|---|
| `brainstorming` forbids loading other skills | `SKILL.md:13`, `:61` scope the ban to *implementation* skills; **`:132` states it unqualified**: *"Do NOT invoke any other skill."* |
| `subagent-driven-development` hard-codes the agent type | `implementer-prompt.md:6`, `task-reviewer-prompt.md:11`, `re-review-prompt.md:11` — all three read `Subagent (general-purpose):` |
| It hard-codes the final reviewer | `SKILL.md:74`, `:103`, `:104`, `:400` — `../requesting-code-review/code-reviewer.md` |
| It asks the coordinator to hand-write the rubric | `task-reviewer-prompt.md:26`, `:172` — `[GLOBAL_CONSTRAINTS]`, *"copied verbatim from the plan's Global Constraints section or the spec"* |
| Neither it nor `writing-plans` knows a domain plugin can exist | `grep -ri 'dotnet\|domain skill\|domain plugin\|other plugin'` over both — **zero hits** |

### The two findings the field report did not reach

**1. The remedies it proposes for Superpowers cannot be executed.** This
repository's standing rule is absolute: *no Superpowers file may ever be
modified*. Superpowers is also installed from a marketplace, so any local edit
is erased by the next update. Every remedy must therefore live in
dotnet-standards, or in the consumer repository's own `CLAUDE.md`.

**2. The write→review transition is model-initiated, so our only injection
channel is structurally deaf to it.** The report proposes re-firing
`router-nudge` when a prompt mentions review. In Incident B the user never typed
anything at that moment: the 20+ review rounds ran *inside* one autonomous
`subagent-driven-development` turn. `UserPromptSubmit` cannot fire where there
is no user prompt. Any mechanism that is to catch this failure must fire at
**tool level**, at the moment the subagent is spawned.

### What did not fail

Nothing about this plugin's *content* was implicated, and the design below
deliberately changes none of it:

- `dotnet-feature-flow:200–216` already forces every task prompt to name the
  owning knowledge skills **and to order the Skill-tool load** — the exact
  defence Incident A needed.
- `choosing-a-dotnet-skill`'s planning section already governs spec, plan and
  subagent-prompt writing, and 0.3.59 already moved it above the tables.
- `dotnet-review-flow` already owns the fleet, the verification discipline and
  the standing-code scope.

**The failure is entry and hand-back, not doctrine.** The correct flow existed,
sat in the session's skill list all day, and was never opened, because
`using-superpowers` arrives first — injected verbatim at `SessionStart`, in
`<EXTREMELY_IMPORTANT>` framing — and frames the whole session as a Superpowers
pipeline. This plugin's counterweight is one sentence on the first prompt.

### The legitimacy chain this design rests on

`using-superpowers` states its own precedence: *"User instructions (CLAUDE.md,
AGENTS.md, direct requests) take precedence over skills."* A rule written into
the consumer repository's `CLAUDE.md` therefore outranks every process skill by
Superpowers' own rule — and `claude-md-builder` already owns the generation of
that file. That is the strongest lever available, and it requires no new
machinery.

## Scope

**In:**

| Component | Change |
|---|---|
| `skills/claude-md-builder/references/static-rules.md` | new rule group, R28–R31 |
| `skills/claude-md-builder/references/template.md` | where the group lands in section 8, and its budget |
| `skills/claude-md-builder/references/checklist.md` | the group joins *Never cut* |
| `hooks/fleet-nudge` | **new** — `PreToolUse`, matcher `Task\|Agent` |
| `hooks/process-handback` | **new** — `PreToolUse`, matcher `Skill` |
| `hooks/hooks.json` | two registrations |
| `hooks/README.md` | the four-hook inventory becomes six; two rows added to *Why only these hooks*; the `router-nudge` doctrine amendment recorded |
| `hooks/router-nudge` | emit text gains one sentence naming the two flow entry points |
| `skills/choosing-a-dotnet-skill/SKILL.md` | new section *Composing with Superpowers process skills*; one row in the shared-token table |
| `skills/dotnet-feature-flow/SKILL.md` | description only |
| `skills/dotnet-review-flow/SKILL.md` | description only |
| `2026-08-02-skill-routing-failure-postmortem.md` | moved to `docs/field-reports/` — a report at the repository root is not where this tree keeps evidence |
| `CHANGELOG.md`, both manifests, `docs/next-session-prompt.md` | per the close protocol |

**Out, deliberately:**

- **Any Superpowers file.** Not negotiable, and not a matter of politeness: a
  local patch is erased by the next marketplace update, so a design that
  depends on one is a design that silently expires.
- **Forking or restating `subagent-driven-development`.** `dotnet-feature-flow`
  calls it and never copies it; a .NET-flavoured fork would become a stale
  second source of truth the day Superpowers changes.
- **`updatedInput` — rewriting `subagent_type` from the hook.** The CLI schema
  permits it (measured below). It is refused: silently substituting an agent the
  session did not choose is action at a distance, it makes the transcript lie
  about what was spawned, and it inverts this repository's hook doctrine
  (*"under-fire, never over-fire"*). A hook here informs; it does not steer.
- **`permissionDecision: "ask"` on review spawns.** A hard gate is the next
  escalation if the field trial shows nudges are ignored. It is not shipped
  blind — see *Risks*.
- **Any hook that fires on every prompt.** S6's token objection was never
  falsified; only its premise about description discipline was.
- **New knowledge content.** No rubric, no convention, no lens changes here.

## The hook mechanism is verified, not assumed

Read out of the Claude Code binary at
`~/.local/share/claude/versions/2.1.220` on 2026-08-02:

- **`PreToolUse` carries `additionalContext`.** The output schema is
  `hookEventName: "PreToolUse"`, `permissionDecision`, `permissionDecisionReason`,
  `updatedInput`, `additionalContext: string().optional()`. Context injection at
  tool level is therefore a supported channel, not a trick.
- **The input payload carries what the gates need**:
  `hook_event_name: "PreToolUse", tool_name, tool_input, tool_use_id`, plus the
  common fields (`session_id`, `cwd`).
- **The subagent tool answers to two names.** The binary defines both
  (`"Agent"`, `"Task"`) and branches on `e !== "Agent" && e !== "Task"`. The
  matcher must be `Task|Agent`; a future rename loses the nudge and nothing else.
- **`Skill` is an ordinary tool name**, so a `PreToolUse` matcher can see a skill
  being loaded, and `tool_input.skill` carries which one.

## Design

### Layer 1 — four static rules in every consumer `CLAUDE.md`

A new group in `references/static-rules.md`, **Process ownership**, placed
immediately after *Communication and language*. It is **self-gating**, exactly
like R23 (*"the rule states its own condition, so it ships whether or not the
scan can detect Superpowers"*) — the precedent already exists in this catalogue.

**R28** — `The .NET process in this repository has two entry points: /dotnet-feature to take a change from idea to reviewed commit, /dotnet-review to review a branch, a diff or a set of paths. Superpowers brainstorming, plan writing, TDD and subagent-driven development are phases those flows call — do not assemble that sequence by hand.`
*Prevents:* a whole session running a hand-rolled Superpowers pipeline that no
.NET flow ever enters. Observed 2026-08-02: the flow that owns the entire task
sat in the session's skill list all day and was never opened.

**R29** — `Subagents that review or test .NET code are the ones dotnet-review-flow names, never general-purpose, and their criteria are the four rubric skills — do not hand-write a constraint block in place of a rubric.`
*Prevents:* a review whose coverage equals whatever the coordinating session
happened to think of. Observed: over 20 review rounds, the performance lens was
never applied once, and the architecture and security lenses ran on improvised
criteria.
*Note:* names no agent. The roster lives in `dotnet-review-flow`; repeating it
here creates a second list to keep in step.

**R30** — `A Superpowers process skill saying "do not invoke any other skill" is barring implementation skills. It does not suspend the knowledge layer: before any brainstorm answer, plan step or subagent prompt states a .NET convention, load the dotnet-standards skill that owns it.`
*Prevents:* a spec written from memory during brainstorming.
*Why this reading is correct, and not us overriding another plugin:*
`brainstorming:13` and `:61` both scope the ban to implementation skills — *"any
other **implementation** skill"*, *"frontend-design, mcp-builder"*. `:132` is the
one-line summary of that same rule with the qualifier dropped. Reading the
summary in the light of the two statements it summarises is ordinary reading, not
an override. The rule is stated in `CLAUDE.md` because that is the only place a
reading can bind when the process skill itself holds the wheel.

**R31** — `Re-route when the work changes phase — design, code, test, review. choosing-a-dotnet-skill is a lookup table consulted at each phase, not a file read once at the start of a session.`
*Prevents:* the observed shape of both incidents — the router *was* in context
all day, consulted once for a placement question, and never revisited when the
work changed nature.

**Placement.** `template.md` §8 currently orders hard constraints first. These
four ship as a sub-heading `### Process` **immediately after the hard-constraint
block and before every other group**, because they govern how the rest of the
file gets read. Four lines against section 8's 55-line budget.

**The multiplier.** Update mode (0.3.57) re-runs PHASE 3 selection against the
current catalogue, so these rules are offered into **every `CLAUDE.md` this
plugin has already generated** at the next update run — no consumer has to know
this change happened. That is the whole reason Layer 1 leads.

### Layer 2 — two hooks that fire at the moment of the decision

Both obey the house rules in `hooks/README.md` without exception: extensionless,
through `run-hook.cmd`, emit **once per session**, gated to .NET solutions,
silent absence benign, and **naming no destination that a skill already owns**.

**Shared gate order — cheapest first, and different from `router-nudge`'s.**
These hooks fire per tool call, not per prompt, so the marker check comes
**before** the `.NET` glob, and the gate result is memoised either way: the first
invocation of a session writes either an `emitted` marker or a `not-applicable`
marker, and every later invocation is one `stat`. A session in a non-.NET
repository pays the solution-shape check exactly once.

#### Hook A — `fleet-nudge`

| | |
|---|---|
| Event | `PreToolUse` |
| Matcher | `Task\|Agent` |
| Extra gate | the spawn looks like review or test work — `tool_input.prompt` / `.description` matching a small keyword set (`review`, `reviewer`, `audit`, `test`, `tester`, `verify`) **or** `tool_input.subagent_type` being `general-purpose` |
| Emits | once per session |

Emit text (final wording to be fixed in the plan, subject to the same approval
rule as any report rule):

> A subagent is being spawned in a .NET solution where dotnet-standards is
> installed. If this spawn reviews or tests code, this plugin owns that job:
> load `dotnet-standards:dotnet-review-flow` and spawn the specialist agents it
> names, rather than a general-purpose agent carrying a hand-written constraint
> block — a process skill that hard-codes `general-purpose` is naming a default,
> not forbidding these. Implementation spawns are unaffected, except that their
> prompts must order the load of the knowledge skills that own the conventions
> they touch; `dotnet-standards:choosing-a-dotnet-skill` maps them. Emitted once
> per session.

This is the only mechanism in the design that fires where Incident B happened.

#### Hook B — `process-handback`

| | |
|---|---|
| Event | `PreToolUse` |
| Matcher | `Skill` |
| Extra gate | `tool_input.skill` is one of `superpowers:brainstorming`, `writing-plans`, `subagent-driven-development`, `test-driven-development`, `executing-plans`, `requesting-code-review` |
| Emits | once per session |

Emit text:

> A Superpowers process skill is being loaded in a .NET solution where
> dotnet-standards is installed. The two layers compose: Superpowers owns
> brainstorming, planning and TDD; dotnet-standards owns which .NET convention
> governs each step and who reviews the result. A "do not invoke any other
> skill" line in a process skill bars implementation skills — it does not
> suspend the knowledge layer. Before any design answer, plan step or subagent
> prompt states a .NET convention, route it through
> `dotnet-standards:choosing-a-dotnet-skill` and load the owning skill. If the
> whole task is a .NET feature or a .NET review, the flow that already composes
> both layers is `/dotnet-feature` or `/dotnet-review`. Emitted once per session.

**Why both hooks and not one.** They fire at different moments for different
failures: B catches Incident A at the point of capture, A catches Incident B at
the point of spawn — a moment B's session-scoped emit may precede by an hour and
dozens of turns.

### Layer 3 — text on this side

1. **`dotnet-feature-flow` description.** It currently lists phases. It must
   also say what it *replaces*: hand-assembling brainstorming + writing-plans +
   subagent-driven development for a .NET change. Add the phrasings people
   actually type — *"execute this plan"*, *"implement the plan with subagents"*.
   Description law still binds: third person, under 100 words, `Not for:` naming
   every owning sibling.
2. **`dotnet-review-flow` description.** Add *"final review before merge"*,
   *"review each task as it lands"* — the phrasings of an in-flight review round,
   which the current description does not carry.
3. **`choosing-a-dotnet-skill`.** A short section *Composing with Superpowers
   process skills*, placed directly after the planning section (which 0.3.59 put
   above the tables) — three bullets: the scope of the ban (R30's reasoning), the
   phase re-route rule (R31), and the spawn rule (R29). Plus one row in the
   shared-token table for *spawning a subagent*: review or test → the flow and
   its agents; implementation → the plan's named knowledge skills.
4. **`router-nudge` emit.** One added sentence naming `/dotnet-feature` and
   `/dotnet-review` for whole-task cases.

**This last point amends a written doctrine and the amendment is recorded, not
slipped in.** `hooks/router-nudge`'s header says *"IT NAMES THE ROUTER AND
NOTHING ELSE"*, on the ground that naming a destination makes the hook a second
source of truth for routing. That reasoning holds for *table rows* and still
does — the added sentence names no skill from the tables. It does not hold for
the choice the router cannot express: the router routes a question to a skill,
while both incidents were failures to choose a **process for the whole session**,
which no table row can carry. Per the precedent this repository set at 0.3.27
(*"refusing a component for a reason that later stops holding is a defect, and
correcting it belongs in the same change"*), the header gains the amendment and
its reason.

## Risks

**The flows have never been run end to end in the field.** `dotnet-feature-flow`
has no trial behind it; the only field exercise was `/dotnet-review` on one
commit. This change adds three pointers at it. If the flow itself has defects,
this design routes more traffic into them — which is an argument for running the
trial in *Verification* before considering this closed, not for shipping less.

**Per-tool-call cost.** Two hooks now fire on every `Skill` call and every
subagent spawn — on Windows, a `cmd` plus a `bash` per firing. The memoised gate
holds it to one real evaluation per session; everything after is a `stat`. Stated
plainly so the next session can measure it rather than rediscover it.

**Over-firing.** Hook A's keyword gate will match implementation spawns whose
prompts say "test". Accepted: the emit tells such a spawn it is unaffected, and
this repository's standing preference is under-firing on the *emit* while
tolerating a loose *gate*.

**A nudge can be ignored — this has already been measured once.** Session-start
injection was observed being ignored on turn 1 (the finding behind 0.3.27). These
two hooks are better placed but carry no more force. If the trial shows they are
ignored, the escalation ladder is: (1) `permissionDecision: "ask"` on a review
spawn that named `general-purpose`, (2) the same as `deny` with a remedy. Both
are gates, both can be wrong, and neither ships without evidence that the nudge
was not enough.

**Superpowers is a moving target.** Every line quoted here is 6.2.0. A future
version may fix the hard-coded agent type upstream, at which point Hook A's emit
is redundant but harmless. Nothing in this design breaks if Superpowers changes;
the worst case is a note nobody needs.

**Consumer `CLAUDE.md` budget.** Four rules against a 200-line ceiling.
`claude-md-builder`'s reconciliation already surfaces budget overflow to the user
rather than silently trimming; no new mechanism needed.

## Verification

1. **Static** — `claude plugin validate`; both manifests agree on the version;
   install through the real update path and confirm `installed_plugins.json`
   points at the new cache.
2. **Hook smoke tests, before shipping** — the practice this repository adopted
   at 0.3.56 for rubric greps, applied to hooks: feed each script a synthetic
   `PreToolUse` payload on stdin and assert emit / no-emit for: .NET dir vs not,
   review-shaped spawn vs implementation spawn, in-scope skill vs out-of-scope
   skill, second call in the same session, missing `session_id`, unwritable
   `TMPDIR`.
3. **Field trial — the only thing that settles this.** A consumer session that
   builds a feature through `subagent-driven-development` in a .NET repository.
   Measure: did Hook B fire and was it acted on; did Hook A fire at the first
   review spawn; were the specialist agents used; was a flow entered at all.
4. **Quantify the original miss** — run `/dotnet-review` on
   `feature/access-control-core`, as the field report's §9 asks, and record
   whether the four lenses find what the improvised review did not. The
   performance lens is the one that never ran.

## Decisions and who made them

| Decision | Who | When |
|---|---|---|
| All three layers ship in one session | User | 2026-08-02 |
| Spec and plan first; no skill or hook edited before approval | User | 2026-08-02 |
| `updatedInput` agent-substitution refused | This design — reasons under *Out* | 2026-08-02 |
| Hard gates (`ask` / `deny`) deferred behind a measurement | This design | 2026-08-02 |
| No Superpowers file touched; upstream report optional and out of scope | Standing rule (`CLAUDE.md`) | — |
| Final wording of both hook emits and of R28–R31 | **User — pending**; report-rule wording needs approval before it ships | — |
