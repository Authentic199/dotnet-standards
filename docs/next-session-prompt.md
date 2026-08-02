# LANE BOARD — the living index (open this first)

**What this file is.** The one place that says, for every lane: where it
stands, what its next session does, and which file to open. **Every lane
session, at close, updates its own row here and appends to the PENDING log
below if it parks work.** Keep entries to 2–4 lines; depth stays in the
per-lane files. Do not let this file go stale — it is the first thing a
returning session reads.

Shipped through **v0.3.65 (26 skills + 2 commands + 6 agents + 6 hook
scripts)** as of 2026-08-02. **0.3.64–0.3.65 — the plugin installs on Codex**
(`.codex-plugin/plugin.json` + `.agents/plugins/marketplace.json`; verified
installed and enabled by `codex plugin list`), and **0.3.65 corrected 0.3.64's
central claim within the same session**: a rejected manifest field is not an
absent feature. `codex features list` is the authority — `hooks` stable/true,
`multi_agent` stable/true, `plugin_hooks` **removed**. So hooks, agents and
commands all exist on Codex, read from `~/.codex/hooks.json`,
`~/.codex/agents/*.toml` and `~/.codex/prompts/*.md`; `codex/install.sh` puts
them there and `codex/sync-from-plugin.py --check` guards the projections
against drift. Facts a later session needs: **Codex launches a hook command
without a shell**, so a bare `.cmd` path silently never runs — every entry needs
`commandWindows` with `cmd /c`, and a planted canary hook is how that was
distinguished from "Codex runs no hooks"; **the Codex cache is built from git
HEAD, not the working tree**, so an uncommitted change installs as the previous
commit's content; the manifest validator at
`~/.codex/skills/.system/plugin-creator/scripts/validate_plugin.py` also checks
every `SKILL.md` frontmatter. **Still unverified: whether the six custom agents
resolve for spawning** — `codex exec` exposes no spawn tool at all. The
instructions file is unchanged on both harnesses: rules live in `CLAUDE.md`, and
`AGENTS.md` is a pointer at it written by `claude-md-builder` PHASE 7 — never a
copy, never a symlink.

**0.3.63 — every hook marker is now keyed per
CONTEXT, not per session**, and the bug it fixes had shipped since 0.3.44: under
`subagent-driven-development` the first `dotnet test` of a run fires inside a
throwaway implementer subagent, which consumed the session's only emit and
vanished, so the coordinating session's own final test run was never told
anything. `agent_id` (present only inside a subagent — CLI 2.1.220 schema) now
keys the marker in all three nudge hooks; `process-handback` had the identical
defect on the day it shipped, since `dotnet-feature-flow:210` orders every
implementer subagent to load skills with the Skill tool. **Standing lesson: a
"once per session" marker is wrong wherever subagents do the work — ask which
context receives the emit, not whether the hook fires.**

**0.3.62 was the first release that changed no
doctrine at all** — a consumer session skipped this plugin at both ends of one
feature (a specification written with no knowledge skill loaded, then 20+ review
rounds with no rubric and no specialist agent), and the cause was **entry and
hand-back, not content**: `dotnet-feature-flow` owned that whole task, sat in
the session's skill list all day, and was never opened. Two facts from it outrank
the version number. **(1) `UserPromptSubmit` cannot catch a phase change** — the
review rounds ran inside one autonomous `subagent-driven-development` turn, so
the write→review transition was decided by the model and no prompt existed to
hang a nudge on; that is why the two new hooks are `PreToolUse`, the first
per-tool-call hooks this plugin has admitted. **(2) `CLAUDE.md` is the only
channel that outranks a process skill already holding the wheel**, by
Superpowers' own precedence rule — hence R28–R31, which 0.3.57's update-mode
reconciliation back-fills into every `CLAUDE.md` already generated. Rulings:
CHANGELOG 0.3.62. **Read these before assuming anything about the tree:** 0.3.53
narrowed the soft-delete section's `HasQueryFilter` claim (a corpus census
falsified "none registered anywhere"; two entities register one, for staged
imports) · 0.3.54 **the R8 labelling pass** — 63 banked candidates ruled, 32
labelled, 31 dropped; five skills gained a `references/anti-patterns.md`;
decisions table at `docs/ext-batch-2026-07-31/r8-decisions.md`, one row per
candidate so any label can be vetoed alone · 0.3.55 two user rules (every request
property nullable + `NotEmpty()` on the required ones, taught in `api-surface`
and `module-feature`; `claude-md-builder` R26 timestamps-are-UTC, written
narrower than dictated because the corpus convention covers `DateTimeOffset`
only) · 0.3.56 rubric checks 5.21/5.22 closing those two orphans — **both
`Find:` greps were wrong on the first pass and were fixed by smoke-testing them
against a real project, which is now standing practice** · 0.3.57
`claude-md-builder` update mode reconciles against the rule catalogue (field
report: an update run added one factual line and none of the three static rules
that had shipped that day and applied — update mode had never re-opened the
catalogue, so every `CLAUDE.md` was frozen against the rules of its creation
day) · 0.3.58 **the activation surface** — body pointers from the pre-existing
skills went 1/5 → 4/5 and the review layer, which had 0/5 rubric citations and
0/5 agent mentions, can now see the five new skills and their 32 anti-examples;
three checks added, a fourth dropped as judgement-call. · 0.3.59 **three
activation defects, all found by one field failure** — a consumer session
designed a MediatR surface from memory and produced a spec with five convention
violations. Its three stated causes were all ours: (1) the generated *Where this
repository differs* section read as a licence to skip a skill — **R27** now
opens it, placed in the rule catalogue so 0.3.57's reconciliation carries it into
existing files; (2) all six `mediatr-messaging` envelope examples shipped
`public` against `module-feature`'s `internal sealed` rule (0.3.31 normalized
them the wrong way) — **a disclaimer naming no rule loses to an example showing
the opposite**, and a fourth leak was found in `elasticsearch-search`; (3) the
router's *"load it one row and stop"* sat 100 lines above the section governing
spec-writing, so the planning section **moved above the tables**. A fourth cause
was logged as "no skill owns a module whose public surface *is* its MediatR
commands" and is **withdrawn — that was a misreading**; `module-feature:249-272`
owns it with a diagram. What survives in the PENDING log is narrower and real:
**inverted reach** — a plug-in interface the module owns and *other* modules
implement, which no rule covers in either direction. · 0.3.60 **a shipped
contradiction between two skills**, found the same day by the same consumer
session, which had to adjudicate it itself in a *Xung đột giữa hai skill*
section — having to do that is the defect. `ef-core-data-access:222` claimed the
entity file holds *"any enums it owns"* (its example declared one inline) while
`facade-module-architecture:215` says every enum lives in `Enums/`, *"never
declared inside an entity, response or service file"*. **Resolved toward
`facade-module-architecture`** — it owns placement. **Second instance of the
0.3.59 family**: prose deferred correctly or said nothing while the *example*
demonstrated another skill's rule being broken, and the example is what gets
copied. **A sweep for further example-versus-rule conflicts across skill pairs
is worth a session** — two found in one day, neither by review, both by field
use. · 0.3.61 **`message-keys` taught the wrong form and it had spread to four
skills** — the most damaging find yet, caught only because the session, trusting
`message-keys`, rewrote `module-feature`'s *correct* validator examples into
wrong ones and the user stopped it. `Messages<T>` takes the **entity** in
validator rules; the skill's premise that "a selector can only compile against
the type being validated" is false. `[MessageDisplay]`'s real job, settled by
reading `Messages.cs`: it is read off `typeof(T)` with a `type.Name` fallback, so
it matters **only** for a Facades-tier request with no entity behind it — on the
twelve module requests carrying it, nothing reads it. Fixed in `message-keys`,
`dotnet-testing` (3 examples), `api-surface`, and **`dotnet-code-review` check
5.5, which was inverted and would have flagged conforming code**. Also: the
nullable-request law moved into `api-surface`'s body — it lived only in
`references/`, and a real request shipped `DateTimeOffset OccurredAt` unvalidated
as a result. **Standing lesson: verify a sibling skill's rule against the source
before acting on it — the examples were corpus-checked, the rule was not.**

Original 0.3.52 close follows — expect `details` to print **Skills (28)** and
**Hooks (3)** (both counting quirks below). 0.3.35–0.3.43 were
maintenance/ruling releases (see CHANGELOG); 0.3.44 added the fourth hook,
`test-report-nudge` (the parked `dotnet-test-report` roadmap row, redesigned:
nudges the model to write a human-readable `test-report.md` instead of
shell-parsing TRX; format user-approved 2026-07-31). **The 2026-07-31
mega-session (solo, user-directed) then shipped:** 0.3.45 integration-tier
hardening (subcutaneous never substitutes for the factory host; renumbered
after a same-number collision with 0.3.44) · 0.3.46 three user rules (review
reports → `docs/code-review/` in the reviewed repo; R16's house-pattern
exception; R25 lookup-first over `Common/Extensions`) · **the
common-extensions batch 0.3.47–0.3.52**: soft delete into `ef-core-data-access`
(0.3.47), then five NEW skills — `excel-miniexcel` (0.3.48),
`http-client-factory` (0.3.49), `file-storage` (0.3.50), `list-query-pipeline`
(0.3.51), `common-extensions` (0.3.52) — every piece through the three-way
loop run by delegated headless `claude -p` coordinators (playbook in the
project memory; skill-creator now installed at USER scope — the old
local-scope install was bound to the repo's pre-move path and broke every
arbiter). All six coordinator reports + the field-trial evidence preserved
under `docs/ext-batch-2026-07-31/`. Sonnet field trial on the consumer repo:
the hook→router chain self-triggered (api-surface + module-feature +
list-query-pipeline loaded unprompted), and a from-scratch module landed
canonical — entity config chain verbatim, list pipeline verbatim, permission
catalogue wired, real migration generated; the user stopped the remaining two
trial tasks as unnecessary. The board header sat at 0.3.21 for four releases, then at 0.3.25
through 0.3.26 — if you ship, update this line, or the next session reads a
stale roster. **On the skill count — 23 and 21 are both right.** This line used
to read "23 skills"; `skills/` holds **21**. The 23 comes from `claude plugin
details`, whose inventory line lists the two commands (`dotnet-feature`,
`dotnet-review`) among the skills. Say which number you mean, and **expect 23
from `details`** at prove-it time — a session comparing it against 21 will think
the install failed. **Same trap for hooks since 0.3.44: `details` prints
an EVENT count, not a script count** — since 2026-08-02 the events are
SessionStart, UserPromptSubmit, PreToolUse and PostToolUse (**4**), while
**6 scripts** ship: PostToolUse carries `post-edit-format` and
`test-report-nudge`, PreToolUse carries `fleet-nudge` and `process-handback`.
The script list in the cache's `hooks/` dir is the number to verify.
Earlier: 0.3.21 = process-integration v1, Lane D session D1:
`dotnet-feature-flow`, `dotnet-review-flow`, `/dotnet-feature`,
`/dotnet-review`, six specialist agents, SessionStart `superpowers-check`,
two-layer description — full rulings CHANGELOG 0.3.21). Before it:
facade-module-architecture 0.3.0 · api-surface 0.3.2 · module-feature 0.3.3 ·
error-handling 0.3.4 · elasticsearch-search 0.3.5 · distributed-caching 0.3.6 ·
message-keys 0.3.7 · distributed-lock 0.3.8 · ef-core-data-access 0.3.9 ·
choosing-a-dotnet-skill 0.3.10 · dotnet-testing 0.3.11 · automapper-mapping
0.3.12 · auth-and-security 0.3.13 (0.3.14 = router-alignment hotfix) ·
dotnet-code-review 0.3.15 · mediatr-messaging 0.3.16 ·
dotnet-architecture-review 0.3.17 · dotnet-security-review 0.3.18 (0.3.19 =
budget-fix) · dotnet-performance-review 0.3.20 · process-integration v1 0.3.21 ·
claude-md-builder 0.3.22 (0.3.23 Vietnamese, 0.3.24 delta-not-doctrine) ·
`dotnet-review-flow` NO-SIGNAL 0.3.25 · `claude-md-builder` contradictions
0.3.26 · `router-nudge` / mechanism E 0.3.27 · `dotnet-review-flow`'s standing-code
path scope 0.3.28 · the router's entry trigger 0.3.29 · write-simple-code
(rubric area 7, flow carriers, static rule R24) 0.3.30 · the first field
trial's 15-item feedback (E1 agents lacked the Skill tool + cache-read ban,
E2 failure classification, 12 rule additions across 9 skills, C4 declined)
0.3.31 · the two-module-name rule (no `OrderShipmentsController` /
`OrderShipmentService` — owning module's suffix part + `Send`; architecture
check 3.5) and the reviewers' knowledge-layer loads (citation-counted lists
per lens) 0.3.32 · the delegation reversal (member order + property XML docs
are house rules now — checks 5.17/5.18) plus checks 1.10, 5.15, 5.16, 5.19
and the `## 6. Tests` heading restore 0.3.33 · round-1 readout fixes (split
test gets check 4.10; 5.16/5.19 re-rooted + code reviewer's resolve-roots
bullet; 3.5 names the parent as owner; standalone continues past a RED tier;
host-readable diff path) 0.3.34 — the self-evaluating trial loops against
this line: loop A re-reviews standing code, loop B builds three features from
scratch off `develop` through the full feature flow.

**The three defects behind the 2026-07-29 no-trigger failure are now all
closed** — 0.3.27 the hook, 0.3.28 the standing-code scope, 0.3.29 the router's
own description. **First real-session readout, 2026-07-29 (user-reported, in
BE-Ops-Service):** **0.3.28 CONFIRMED PASS** — a session reviewed standing code
(no diff, nothing changed) via the path scope and it worked, clean evidence.
**0.3.29 NOT ISOLATED, and may be structurally unable to be**: the user saw the
`router-nudge` context line *before* the router loaded — the hook fired first,
as it always does on the first prompt of a session
(`hooks/README.md` — "the first prompt"), which primes the model before its own
description ever gets a chance to self-trigger unprompted. Every future real
session will show the same hook-then-router order, so this trial may never
cleanly isolate "does the description alone fire" — see PENDING log entry
below. This does not mean 0.3.29 failed: the observed outcome (router loaded,
request served) is the one that matters operationally; only the specific
question "would the description have fired without the hook" stays open.

**Second readout from the same trial, landed as 0.3.31 (2026-07-29, maintenance
session):** the consumer repo's full 15-item feedback report was verified
against the tree and shipped — headline defect **E1**: all six agents
commanded a Skill-tool load their `tools` list did not grant (2 of 12 spawns
failed outright; 10 self-healed by reading the plugin cache, unverifiable
version). The four `[CẦN XÁC NHẬN]` items shipped only after the user answered
the report's five questions; C4 (XML `<summary>` on properties) was declined —
stays with the analyzer, remedy is `error` severity by rule group. Trial
limits worth remembering: one project, one commit, `/dotnet-review` only —
`dotnet-feature-flow` and 12 knowledge skills remain unexercised, and the
package-vulnerability layer has still never run (reviewers have no shell).
Full rulings CHANGELOG 0.3.31.

## Lane status

| Lane | Open this file | Status (last update) | Next session does |
|---|---|---|---|
| **A — Data & Feature Spine** | `next-session-prompt-A.md` | S9b closed 2026-07-27: `auth-and-security` v0.3.13 shipped (module-feature, ef-core-data-access before it) | Confirm with the user whether the queue is unfrozen; if yes: `domain-modeling`, then `modern-csharp` (order TBC). Warm-up task carried: fix the stale line `module-feature/references/validation-rules.md:322` (S15 flag) **+ second instance found by rubric #1: `module-feature/SKILL.md:187` and validator examples at lines 165–172 (superseded entity-typed `Messages<T>` form)** |
| **B — API & Security Surface** | `next-session-prompt-B.md` | Queue COMPLETE at S15 close (api-surface, error-handling, message-keys, dotnet-testing). Lane closed | Nothing — the B file exists to hold its Lane log for rubric harvesting. Reopen only by explicit user direction |
| **C — Infrastructure Services** | `next-session-prompt-C.md` (mirrors the tree's CLAUDE.md) | **S17 closed 2026-07-28: `mediatr-messaging` v0.3.16 shipped** (router alignment same commit; full rulings CHANGELOG 0.3.16) | Queue empty of unblocked work — ask the user whether `observability` / `background-worker` / `http-resilience` unfreezes; if none, the lane pauses while rubrics #2–4 run solo |
| **Rubrics — 4 solo sessions** | `next-session-prompt-rubrics.md` | **COMPLETE. Rubric #4 `dotnet-performance-review` shipped v0.3.20, 2026-07-28** (#1 v0.3.15, #2 v0.3.17, #3 v0.3.18/19 before it) — 5 areas, honesty rule verbatim, 15 graded-by rows, 12-row Refused table; router: reservation row deleted + base-map row + slow/cost disambiguation row same commit; six grade-once violations caught pre-ship, durable fix recorded (briefs carry the sibling's full check-title inventory); full log in the rubrics file | Nothing — the rubrics file exists to hold its log. **Lane D is UNLOCKED** |
| **D — Process Integration** | `next-session-prompt-D.md` | **Dm2 (maintenance) closed 2026-08-02: process handback shipped at v0.3.62** — the plugin was skipped at both ends of one consumer feature; remedy is four `claude-md-builder` rules, two `PreToolUse` hooks and description text, changing no doctrine. Rulings CHANGELOG 0.3.62; spec + plan under `docs/superpowers/`; evidence `docs/field-reports/`. Before it: **Dm1 closed 2026-07-29: `dotnet-review-flow` NO-SIGNAL shipped at v0.3.25.** Triggered by a real `/dotnet-review` run that halted on `RED — environment` and delivered no report at all. Also fixed a regression this same change introduced in `dotnet-feature-flow`. Rulings in CHANGELOG 0.3.25; spec + plan under `docs/superpowers/`. Before it: D1 shipped process-integration v1 at v0.3.21 | **First: the 0.3.62 field trial** (PENDING log) — it runs in a consumer repository, not here, and until it runs nobody knows whether the two hooks are heeded. Lane D's *feature* queue stays PENDING by user direction. When unfrozen: session D2, the `bugfix` flow (v1.5, spec §6.3) — brief still valid in the lane file. A maintenance session on an already-shipped flow does **not** need that unfreeze; treat it as a separate track |

**Solo-only (never in a lane):** `project-scaffolding` (pending), the four
rubrics, Lane D.

## PENDING log (append-only; any lane may park work here)

Format: `- [lane, date] what was parked — where the detail lives — what unblocks it`

- [solo, 2026-07-31] **Inverted reach: a plug-in interface a module owns and
  *other* modules implement.** The house rule for crossing a module boundary is
  one-directional — `module-feature:252` *"a service that needs a foreign one
  names a message, never the foreign service interface"*. The consumer case runs
  the other way: an access-control core declares `IScenarioEvaluator`, injects
  `IEnumerable<IScenarioEvaluator>`, and calls implementations that live in
  **other** modules. Nothing is `Send`, nothing is foreign-named — the interface
  is the core's own — so no shipped rule is broken and none applies either. Open
  questions a ruling would settle: where such a contract and its context/result
  records live (the module-folder vocabulary in `facade-module-architecture:203`
  has no slot, and `Services/` is closed to non-services); whether the registry
  that dispatches by key is a class at all or belongs inside the owning service
  as a partial; and whether fail-fast-on-duplicate at startup is reachable when
  the implementations are scoped. Needs a corpus exemplar before it can be
  written — do not invent one. Evidence: CHANGELOG 0.3.59.
  **Correction:** an earlier draft of this entry claimed no skill owned "MediatR
  as a module's public surface". That was wrong and is withdrawn —
  `module-feature:249-272` and `references/mediatr-envelopes.md:20-48` own it
  fully, diagram included: envelopes live in the **owning** module's `Commands/`,
  the foreign module's service `Send`s them, and `internal` does not hide an
  envelope from another module (same assembly) — only from the HTTP project. A
  core module exposing capability as commands is the house pattern working as
  designed, not an exception to it.

- [solo/Lane D, 2026-08-02] **Process handback — this plugin was skipped twice
  in one consumer session, at both ends of the same feature.** A session on
  `feature/access-control-core` wrote a MediatR architecture spec with no
  knowledge skill loaded, then ran 20+ subagent review rounds — including the
  final whole-branch review — without loading one of the five review skills or
  spawning one of the six agents; every round was `general-purpose` with a
  hand-written constraint block, so the performance lens never ran at all.
  Verified against Superpowers 6.2.0: `brainstorming:132` bans loading any other
  skill unqualified, all three `subagent-driven-development` templates hard-code
  `Subagent (general-purpose)`, its final reviewer is hard-coded to
  `requesting-code-review/code-reviewer.md`, its rubric slot is
  `[GLOBAL_CONSTRAINTS]` copied by hand, and neither it nor `writing-plans`
  mentions a domain plugin anywhere. **Two findings the field report missed:**
  its remedies for Superpowers cannot be executed (no SP file may be modified,
  and a marketplace update erases local edits), and the write→review transition
  is **model-initiated**, so `UserPromptSubmit` — our only injection channel —
  cannot fire there. **Nothing about this plugin's content failed**;
  `dotnet-feature-flow` already owns the whole task and was never opened.
  Design: `docs/superpowers/specs/2026-08-02-process-handback-design.md` ·
  plan: `docs/superpowers/plans/2026-08-02-process-handback.md` · evidence:
  `docs/field-reports/2026-08-02-skill-routing-failure.md`. Three layers —
  four self-gating `claude-md-builder` rules (R28–R31, which update mode
  back-fills into every generated `CLAUDE.md`), two new `PreToolUse` hooks
  (`Task|Agent` and `Skill`; `additionalContext` support verified in CLI
  2.1.220), and description/router text. Refused in the design and not to be
  reintroduced: rewriting `subagent_type` via `updatedInput`, and any
  `permissionDecision` gate before a measurement.
  **SHIPPED at v0.3.62, 2026-08-02** — both frozen wordings approved by the
  user, 23 synthetic-payload smoke tests green before ship. **What stays
  parked is the only thing that settles it: the field trial.** Two runs, in a
  consumer repository, neither executable from this tree — (a) a session that
  builds a feature through `subagent-driven-development` in a .NET repo,
  measuring whether `process-handback` fired and was acted on, whether
  `fleet-nudge` fired at the first review spawn, whether the specialist agents
  were used, and whether a flow was entered at all; (b) `/dotnet-review` on
  `feature/access-control-core`, to quantify what the improvised review missed —
  the performance lens is the one that never ran. **Checklist ready:
  `docs/field-reports/2026-08-02-trial-checklist.md`** — preflight (the consumer
  is still on 0.3.58), the marker files under `/tmp/dotnet-standards/` as the
  hard signal, the transcript reads as the soft one, and the blinding rule: the
  trial session must never be shown the checklist or told what is being
  measured. **`dotnet-feature-flow` has
  still never been run end to end in the field**, and 0.3.62 routes more
  traffic at it. If the trial shows the nudges ignored, the escalation ladder is
  written in the design §Risks: `permissionDecision: "ask"` on a review spawn
  that named `general-purpose`, then `deny` with a remedy — neither ships
  without that evidence.

- [A, 2026-07-27] `domain-modeling`, `modern-csharp` — detail in
  `next-session-prompt-A.md` — unblocked when the user confirms the S14 freeze
  is lifted for them and picks the order.
- [A, 2026-07-28] ~~Second instance of the same drift family (found by rubric
  #1, CHANGELOG 0.3.15): `module-feature/SKILL.md:187` + validator examples at
  lines 165–172 carry the superseded entity-typed `Messages<T>` form — fix
  together with the entry below in one Lane A warm-up chore.~~ **VOID as of
  0.3.61 — do not action.** The S15 "ruling" this cites was itself the defect:
  entity-typed `Messages<T>` in a validator is the house form, not superseded.
  CHANGELOG 0.3.61 traces the false premise and the four skills it had spread
  to. Left struck through, not deleted, so a session skimming old entries does
  not resurrect it.
- [A, 2026-07-27] ~~`module-feature/references/validation-rules.md:322` stale
  line ("every message… `T` is the entity" — superseded by the S15 ruling:
  requests type validator messages) — flagged in the S15 log — any Lane A
  session may fix it as a warm-up chore.~~ **VOID as of 0.3.61 — same reason as
  the entry above.** The line these called "stale" was correct; 0.3.61 kept it
  and fixed the sibling text that disagreed with it instead.
- [solo, 2026-08-02] **`dotnet-testing` has no path for a repo without
  NSubstitute.** The skill's only rule for a service's decision logic is *"Unit
  test, NSubstitute at the constructor boundary"* — no fallback is written for
  when the toolchain it assumes is absent. Surfaced in BE-Ops-Service: `Services/`
  correctly absorbed policy-matching and scenario-selection logic that used to
  live in separate resolver/registry classes (per `facade-module-architecture`),
  which means that logic can now only be unit-tested through the service's
  constructor — but the project has no NSubstitute and runs xUnit 2.5.2, not the
  v3 the skill assumes (both stated in the project's own `CLAUDE.md`). A session
  needing to write that test today has no rule to follow. Two arms, user's call:
  (a) add NSubstitute via the R16 house-pattern exception (already permitted,
  just needs the skill to say so) and write the fallback into `dotnet-testing`,
  or (b) teach a way to keep decision logic as `public static` inside a service
  partial so no mock is ever needed — the AccessControl spec's own
  `AccessPolicyService.Policy.cs` already does this for the policy-matching half.
  Not resolved here; needs a ruling before either arm is written.
- [B→rubrics, 2026-07-27] Rubric feed: S13 error-handling candidates
  (CHANGELOG 0.3.4), S13b message-keys candidates, S12 anti-example list —
  detail in `next-session-prompt-B.md` — consumed by the rubric sessions.
- [A→rubrics, 2026-07-27] S9b auth ledger: 37 candidates, 4 embedded, the
  rest rubric feed incl. security findings (username enumeration,
  revoke-no-evict + sliding expiry, Type.GetType fail-open, committed keys in
  two config files) and three banked design forks — detail in CHANGELOG
  0.3.13 + `next-session-prompt-A.md` Lane log.
- [C, 2026-07-27] `observability`, `background-worker`, `http-resilience` —
  user-PENDING since S14 — unblocked only by user direction.
- [roadmap, 2026-07-27 → hook CLOSED 2026-07-31] `dotnet-test-report` hook
  (Group B, post-rubrics) — **SHIPPED at 0.3.44 as `hooks/test-report-nudge`**,
  redesigned by user direction: no shell parsing, the hook nudges the model to
  keep a plain-language `test-report.md` current (format + overwrite behaviour
  user-approved; R5 five-item check in CHANGELOG 0.3.44). The
  architecture-tests roadmap row remains open — detail in
  `docs/03-session-roadmap.md`.
- [C, 2026-07-28] `api-surface` reciprocal `Not for:` route to
  `automapper-mapping` (its description claims "colocated validator and
  mapping profile" but routes nothing back) — detail in CHANGELOG 0.3.12
  "Known seams" — needs an api-surface-owning session; NOT Lane C's.
- [C, 2026-07-28 → CLOSED 2026-07-29] Mechanism E: UserPromptSubmit hook
  pointing at the router — **SHIPPED at 0.3.27 as `hooks/router-nudge`** by a
  solo session. Not a speculative build: it was triggered by a real failure in a
  consumer repository where the plugin was installed, enabled and completely
  ignored. Rulings + the reversed S6 refusal in CHANGELOG 0.3.27.
- [C, 2026-07-28] Lane-log consolidation: fold lane logs into
  `docs/03-session-roadmap.md`; roadmap/index still carry stale references
  ("Facades/Cache in one or both projects", reference-project list) —
  solo chore session, best after the rubrics harvest the logs.
- [C, 2026-07-28] S11 `CompileQueryAsync(...)` pagination extension — needs
  the USER to name its source file (R7) — then belongs to
  `elasticsearch-search` or a rubric; blocked on user.
- [C→rubrics, 2026-07-28] S17 declined anti-example candidates, banked: dead
  `params Assembly[]` on a registration extension; a generic handler branching
  on `typeof(TData)` (handler-body territory) — detail in CHANGELOG 0.3.16 —
  consumed by the rubric sessions.
- [C, 2026-07-28] Seventh-anti-pattern candidate for `mediatr-messaging`:
  registration/behaviours carry no negative example (all six user labels are
  shape-and-naming) — flagged by both authors + arbiter at S17 — needs a
  future mediatr-owning session and a user label.
- [C, 2026-07-28] `qms-backend` reference scope: named at S17 for ONE file
  only (`Modules/Reports/Startup.cs`); not a general quarry — user may widen
  or close it.
- [C, 2026-07-28] Small notes bank: automapper `references/` future
  candidates (troubleshooting catalogue; IncludeMembers precedence;
  value/type-converters — CHANGELOG 0.3.12) and `dotnet-testing`'s untouched
  `IMapper` substitutability note — nothing to do until a session touches
  those skills.

- [rubric-2, 2026-07-28] `facade-module-architecture` tier list still prints
  `Events/` (SKILL.md:197, `references/modules.md:26`) — stale vs
  `mediatr-messaging`'s `DomainEvents/` ruling; rubric #2's catalogue ships an
  explicit precedence note — unblocked by any fma-owning session (pair with the
  Lane A warm-up chore family).
- [rubric-3, 2026-07-28] Banked at 0.3.18 (detail in CHANGELOG + rubrics-file
  log): test-posture security check (needs a user-named shipped sentence);
  `[ApiKey]`+`[HasPermission]` BAD/GOOD anti-example (needs R8 label);
  `GetFallbackPolicyAsync`-null hazard label (R8, user's call); ClockSkew-Zero
  clock-drift trade-off (no shipped owner) — first three unblock on user word,
  the last likely refuses again at rubric #4.
- [D, 2026-07-28] The `bugfix` flow (v1.5, spec §6.3) — full session brief
  ready in `next-session-prompt-D.md` — **user-PENDING since 2026-07-28**;
  unblocked only by user direction, like Lane C's frozen queue.
- [D, 2026-07-28] `README.md` install snippet still names a stale path
  (`D:/ALTA/Project/dotnet-standards`) — pre-existing staleness, NOT caused by
  Lane D (which corrected only its own falsified lines) — any solo chore
  session may fix it.
- [D, 2026-07-28] The six agents' rationalization tables are predicted, not
  baselined — rewrite from OBSERVED behaviour once real `/dotnet-review` /
  `/dotnet-feature` runs accumulate (flag by author B, arbiter-endorsed; detail
  in `next-session-prompt-D.md` Lane log). First observation already banked
  from the D1 smoke test: flow-spawned subagents had no Skill tool — the
  retry-once rule recovered by passing the rubric file path; D2 decides whether
  the agent bodies document that fallback.
- [rubric-2, 2026-07-28] `Guid.NewGuid()` sequential-key rule is a VERIFIED
  ORPHAN (`fma/references/core-contracts.md:40` states it; no rubric checks
  it) — detail in CHANGELOG 0.3.17 Known seams — belongs in
  `dotnet-code-review` review-rubric area 1; needs a dotnet-code-review-owning
  session.

- [D, 2026-07-29] **The install is project-scoped, and it installs from GitHub,
  not from this checkout.** `installed_plugins.json` records
  `dotnet-standards@dotnet-standards-dev` at scope **project**, bound to
  `D:\ALTA\Project\TWOH\ops-service`; the marketplace source is the GitHub repo
  `Authentic199/dotnet-standards` with `autoUpdate: true`. Consequences the
  prove-it rule does not yet state: `claude plugin update ...` without
  `--scope project` fails with "not installed at scope user", and a local merge
  to `main` changes nothing until it is **pushed**. Unblocks: someone folding
  this into the standing prove-it rule.
- [D, 2026-07-29] **Two sessions can silently pick the same version number.**
  Both this lane and the parallel `claude-md-builder` session wrote `0.3.24`
  into both manifests. Git saw identical strings and merged them without a
  conflict — only `CHANGELOG.md` conflicted, and only because both entries
  wanted the top slot. The duplicate would have shipped unnoticed. Unblocks:
  a check in the ship protocol that reads the version off `main` at merge time
  and compares, rather than trusting the branch's own number.
- [D, 2026-07-29] **`RED — tests failed` in standalone mode never reaches the
  review lenses.** Nobody fixes in standalone, so the tiers never go green and
  REVIEW-LOOP's entry condition is never met — the same shape as the defect
  0.3.25 just closed, in a case that was explicitly out of its scope. Concretely:
  standalone mode fixes nothing, so a failing tier never goes green, REVIEW-LOOP's
  entry condition is never met, and the run reports four empty lens verdicts —
  even though the lenses never needed the tiers. Raised by the final whole-branch
  review of 0.3.25 and deliberately not fixed there. Unblocks: a
  `dotnet-review-flow`-owning session.

- [solo, 2026-07-29] **`choosing-a-dotnet-skill`'s description triggers on
  confusion, not on entry.** It reads *"when it is unclear which skill owns the
  question … no skill self-triggered"* — a condition about the reader's own
  confusion, which a session that never considered this plugin does not meet.
  0.3.27's hook now names the router directly, so entry no longer depends on
  this description; the description was still wrong on its own terms.
  **SHIPPED at 0.3.29** — three-way loop, one piece, MERGE. 97 words in, 97 out.
  The decisive finding was the arbiter's and neither author nor the coordinator
  raised it: one draft's guard clause made its own retained trigger unreachable,
  because a `Not for:` pointer can only be followed from a skill already loaded.
  Full rulings and two unlabelled anti-example candidates in CHANGELOG 0.3.29.
  **Open follow-up:** the body's `## How to use these tables` (`:14-18`) does not
  state the entry condition the description now carries — one sentence, for a
  session that owns the body.
- [solo, 2026-07-29] Two small chores banked at 0.3.28, neither blocking: record
  the em-dash word-count convention (`wc -w`, count them) in
  `02-repo-structure.md` §5 — it was ruled at 0.3.28 but never written down; and
  note that `git ls-files` reads the index while `git diff <empty-tree> HEAD`
  reads HEAD, so a staged-but-uncommitted file appears in a path scope's file
  list and not in its patch. Detail: CHANGELOG 0.3.28 "Known seams".
- [solo, 2026-07-29 — scope narrowed by the user the same day] **The review
  surface is diff-anchored; reviewing standing code has no owner.**
  `dotnet-review-flow` hard-stops without a diffable base (`SKILL.md:99-101`)
  and derives every subagent input from the diff; `dotnet-code-review`'s
  description says *"reviewing **changed** … code … before merge"*.

  **Exactly one thing is missing: a scope that is a set of paths instead of a
  diff.** Do not build more than that. The user has since explained the request
  that exposed this: *"write the report to a file, change nothing"* was a
  point-in-time need — validate the plugin's review quality against code written
  before the plugin existed, keeping that code untouched as evidence to compare
  the report against — **not a permanent read-only mode**. Two things that
  looked like gaps are not: standalone mode already changes nothing until the
  user accepts the offer, and the never-write-inside-the-repository rule at
  `SKILL.md:135-136` binds the **diff file**, not the report.

  **SHIPPED at 0.3.28** — three-way loop, three pieces, three MERGE verdicts;
  full rulings, measurements and coordinator catches in CHANGELOG 0.3.28. Router
  rows `:58` and `:85` rebalanced in the same commit. **One design call was
  deliberately NOT made and is now the open seam:** `dotnet-code-review` still
  ranks by blast radius, which assumes a change. A standing audit of pre-plugin
  code will return volume, and nothing yet re-ranks it — the new Decision Guide
  row only forbids moving a severity because the code is old. Needs a
  `dotnet-code-review`-owning session.

- [solo, 2026-07-29] **"Write simple code" — SHIPPED at 0.3.30** (same day, the
  implementation session ran the design below to completion; full rulings and
  Known seams in CHANGELOG 0.3.30 — including two live checks logged unrun: the
  R24 micro-test and the cleanup offer's soft-yes test; the Facades control test
  WAS run pre-merge and passed both directions). Original decision entry kept
  for the record: ownership DECIDED, implementation
  pending. Decision doc
  `docs/superpowers/specs/2026-07-29-write-simple-code-ownership-design.md`.
  (A) install `DietrichGebert/ponytail` REFUSED — its `SessionStart` mechanism
  is the one 0.3.27 measured being ignored, and a generic YAGNI voice cannot
  distinguish sanctioned structure (Facades-axis ahead-of-need code is a USER
  RULING, recorded in the doc §1) from slop; reversal conditions in §4. (C)
  do-nothing REFUSED — over-build observed by the user in sessions with the
  plugin ACTIVE (three shapes, three contexts, 2026-07-29), and the slop
  taxonomy has no complex-where-simpler-works category. (B) CHOSEN: new
  `dotnet-code-review` priority area 7 (severity cap MEDIUM; cleanup renumbers
  to 8), a `dotnet-feature-flow` PHASE 2 ladder instruction, a new
  `claude-md-builder` static rule, plus an OFFERED `/simplify` call site in
  `dotnet-feature-flow` after the shared block goes green, before GATE 2
  (user-added, doc §5.1 carrier 1b); two ponytail lines COPY (MIT → `NOTICE`
  obligation 3); router rows named in §5.6; the (A) refusal to be mirrored
  into `hooks/README.md`'s table. Unblocks: one implementation session,
  three-way loop, 3 pieces. **The wait condition has read out (see the
  0.3.28/0.3.29 entry above) — implementation may proceed.** The design never
  touches the two on-trial descriptions anyway (body-table rows only), so this
  was a formality confirmed, not a blocker lifted.

- [solo, 2026-07-29] **The 0.3.29 router-description trial may be
  structurally unfalsifiable in normal use.** `router-nudge` fires on the
  session's first prompt and always primes the model with a pointer to
  `choosing-a-dotnet-skill` before the description gets any chance to
  self-trigger on its own — so a real session can only ever show
  hook-then-router, never description-alone. If isolating the description's
  own trigger quality still matters, the only clean test is an artificial one:
  a session with the hook disabled (or the marker file pre-seeded so the hook
  stays silent) asked the same kind of request. Whether that test is worth
  running, or whether hook-primed success is sufficient going forward, is a
  call for a session that owns `choosing-a-dotnet-skill` or `router-nudge`.

- [solo, 2026-07-31] **The batch's R7/R8 banks, none labelled** — 7 soft-delete
  candidates (strongest: a live no-op `.IgnoreQueryFilters()` call), 8
  excel-miniexcel, 10 file-storage, the http-client-factory bank, 16
  common-extensions — all in `docs/ext-batch-2026-07-31/*-report.md`; consumed
  by a user labelling pass or the rubrics.
- [solo, 2026-07-31] Banked follow-ups from the batch: the `Any()`-probe drop
  (exact edit locations in the list-query-pipeline report; dropping it forces
  a dotnet-performance-review edit); api-surface + automapper-mapping
  reciprocal `Not for:`s (file-storage report); api-surface reciprocal for
  http-client-factory; the vetoable `$null` `it.`-prefix ruling (revert
  instruction in the lqp report); the `*-RESCUED.md` http references drafts
  (an R7 call inside them is the user's).
- [solo, 2026-07-31] `module-feature`'s description still names
  `background-worker` (unshipped — shipped-only-roster violation, pre-existing;
  observed during the batch's description trims) — needs a
  module-feature-owning session.
- [solo, 2026-07-31] Excel/file-storage corpus defects were fixed IN THE SKILL
  CANON only — the real projects still carry them (apsp template-name save
  bug; the S3FilePath converter's broken `Read` in five projects; the
  non-disposing `Service<T>()`) — fixing the projects themselves is outside
  the plugin's scope and stays the user's call.

- [solo, 2026-07-31 → CLOSED same day] **Two shipped rules were verified orphans
  — checks 5.21 and 5.22 shipped at 0.3.56.** Original entry: (a) every request property nullable + `NotEmpty()` on the required ones
  (0.3.55, taught in `api-surface` and `module-feature`); (b) `claude-md-builder`
  R26, no hand-written `ToUniversalTime()`. Same shape as the `Guid.NewGuid()`
  orphan already logged below — belongs in `dotnet-code-review`'s rubric, needs a
  session that owns it and will write the check in the rubric's own numbered
  format.

## Standing rules (unchanged, summarized)

- One session, one deliverable **+ the mandatory router merge-time edits in
  the same session** (alignment rule, CHANGELOG 0.3.10 — S9b skipped this and
  needed the 0.3.14 hotfix); lanes share one working tree — stage only your
  own paths; expect mid-session `main` movement (conflict rule: keep both
  CHANGELOG entries, renumber yours above theirs).
- The three-way loop (`three-way-skill-loop` skill) is mandatory for any skill
  piece; main session coordinates only. R7/R8 stay with the user.
- Prove-it at ship: validate + `claude plugin update
  dotnet-standards@dotnet-standards-dev` + details shows the new count +
  `installed_plugins.json` points at the new cache + delete `reference/` from
  the new cache dir. Both manifests must agree on the version.
- Artifact language English; talk to the user in Vietnamese.
