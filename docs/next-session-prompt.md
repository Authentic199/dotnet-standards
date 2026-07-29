# LANE BOARD — the living index (open this first)

**What this file is.** The one place that says, for every lane: where it
stands, what its next session does, and which file to open. **Every lane
session, at close, updates its own row here and appends to the PENDING log
below if it parks work.** Keep entries to 2–4 lines; depth stays in the
per-lane files. Do not let this file go stale — it is the first thing a
returning session reads.

Shipped through **v0.3.31 (21 skills + 2 commands + 6 agents + 3 hooks)** as of
2026-07-29. The board header sat at 0.3.21 for four releases, then at 0.3.25
through 0.3.26 — if you ship, update this line, or the next session reads a
stale roster. **On the skill count — 23 and 21 are both right.** This line used
to read "23 skills"; `skills/` holds **21**. The 23 comes from `claude plugin
details`, whose inventory line lists the two commands (`dotnet-feature`,
`dotnet-review`) among the skills. Say which number you mean, and **expect 23
from `details`** at prove-it time — a session comparing it against 21 will think
the install failed.
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
0.3.31.

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
| **D — Process Integration** | `next-session-prompt-D.md` | **Dm1 (maintenance) closed 2026-07-29: `dotnet-review-flow` NO-SIGNAL shipped at v0.3.25.** Triggered by a real `/dotnet-review` run that halted on `RED — environment` and delivered no report at all. Also fixed a regression this same change introduced in `dotnet-feature-flow`. Rulings in CHANGELOG 0.3.25; spec + plan under `docs/superpowers/`. Before it: D1 shipped process-integration v1 at v0.3.21 | Lane D's *feature* queue stays PENDING by user direction. When unfrozen: session D2, the `bugfix` flow (v1.5, spec §6.3) — brief still valid in the lane file. A maintenance session on an already-shipped flow does **not** need that unfreeze; treat it as a separate track |

**Solo-only (never in a lane):** `project-scaffolding` (pending), the four
rubrics, Lane D.

## PENDING log (append-only; any lane may park work here)

Format: `- [lane, date] what was parked — where the detail lives — what unblocks it`

- [A, 2026-07-27] `domain-modeling`, `modern-csharp` — detail in
  `next-session-prompt-A.md` — unblocked when the user confirms the S14 freeze
  is lifted for them and picks the order.
- [A, 2026-07-28] Second instance of the same drift family (found by rubric
  #1, CHANGELOG 0.3.15): `module-feature/SKILL.md:187` + validator examples at
  lines 165–172 carry the superseded entity-typed `Messages<T>` form — fix
  together with the entry below in one Lane A warm-up chore.
- [A, 2026-07-27] `module-feature/references/validation-rules.md:322` stale
  line ("every message… `T` is the entity" — superseded by the S15 ruling:
  requests type validator messages) — flagged in the S15 log — any Lane A
  session may fix it as a warm-up chore.
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
- [roadmap, 2026-07-27] `dotnet-test-report` hook (Group B, post-rubrics) and
  the architecture-tests roadmap row — detail in `docs/03-session-roadmap.md`.
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
