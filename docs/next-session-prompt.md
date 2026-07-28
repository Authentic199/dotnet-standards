# LANE BOARD — the living index (open this first)

**What this file is.** The one place that says, for every lane: where it
stands, what its next session does, and which file to open. **Every lane
session, at close, updates its own row here and appends to the PENDING log
below if it parks work.** Keep entries to 2–4 lines; depth stays in the
per-lane files. Do not let this file go stale — it is the first thing a
returning session reads.

Shipped through **v0.3.19 (17 skills)** as of 2026-07-28 (0.3.19 = rubric-3
budget fix, body 804→498 via table-form conversion):
facade-module-architecture 0.3.0 · api-surface 0.3.2 · module-feature 0.3.3 ·
error-handling 0.3.4 · elasticsearch-search 0.3.5 · distributed-caching 0.3.6 ·
message-keys 0.3.7 · distributed-lock 0.3.8 · ef-core-data-access 0.3.9 ·
choosing-a-dotnet-skill 0.3.10 · dotnet-testing 0.3.11 · automapper-mapping
0.3.12 · auth-and-security 0.3.13 (0.3.14 = router-alignment hotfix) ·
dotnet-code-review 0.3.15 · mediatr-messaging 0.3.16 ·
dotnet-architecture-review 0.3.17 · dotnet-security-review 0.3.18 (0.3.19 =
budget-fix).

## Lane status

| Lane | Open this file | Status (last update) | Next session does |
|---|---|---|---|
| **A — Data & Feature Spine** | `next-session-prompt-A.md` | S9b closed 2026-07-27: `auth-and-security` v0.3.13 shipped (module-feature, ef-core-data-access before it) | Confirm with the user whether the queue is unfrozen; if yes: `domain-modeling`, then `modern-csharp` (order TBC). Warm-up task carried: fix the stale line `module-feature/references/validation-rules.md:322` (S15 flag) **+ second instance found by rubric #1: `module-feature/SKILL.md:187` and validator examples at lines 165–172 (superseded entity-typed `Messages<T>` form)** |
| **B — API & Security Surface** | `next-session-prompt-B.md` | Queue COMPLETE at S15 close (api-surface, error-handling, message-keys, dotnet-testing). Lane closed | Nothing — the B file exists to hold its Lane log for rubric harvesting. Reopen only by explicit user direction |
| **C — Infrastructure Services** | `next-session-prompt-C.md` (mirrors the tree's CLAUDE.md) | **S17 closed 2026-07-28: `mediatr-messaging` v0.3.16 shipped** (router alignment same commit; full rulings CHANGELOG 0.3.16) | Queue empty of unblocked work — ask the user whether `observability` / `background-worker` / `http-resilience` unfreezes; if none, the lane pauses while rubrics #2–4 run solo |
| **Rubrics — 4 solo sessions** | `next-session-prompt-rubrics.md` | **Rubric #3 `dotnet-security-review` shipped v0.3.18, 2026-07-28** (#1 v0.3.15, #2 v0.3.17 before it) — 6 layers, honesty rule verbatim, kit-divergence suppressions first-class; router base-map row + secrets/tokens/gates disambiguation row + a `dotnet-performance-review` reservation row same commit; full log in the rubrics file | Rubric #4 `dotnet-performance-review` next — the LAST rubric (solo, sequential); it deletes the router reservation row as part of its own alignment; after it ships, state explicitly that Lane D is UNLOCKED |
| **D — Process Integration** | `next-session-prompt-D.md` | Blocked by design: runs ONLY after the four rubrics (dotnet-testing prerequisite already shipped) | Closed-loop workflows + specialist agents per the approved S14 spec |

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
- [C, 2026-07-28] Mechanism E: UserPromptSubmit hook pointing at the router
  (`choosing-a-dotnet-skill` was written hook-friendly) — endorsed by the
  user at S15 — small solo follow-up session; unblocked any time.
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
- [rubric-2, 2026-07-28] `Guid.NewGuid()` sequential-key rule is a VERIFIED
  ORPHAN (`fma/references/core-contracts.md:40` states it; no rubric checks
  it) — detail in CHANGELOG 0.3.17 Known seams — belongs in
  `dotnet-code-review` review-rubric area 1; needs a dotnet-code-review-owning
  session.

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
