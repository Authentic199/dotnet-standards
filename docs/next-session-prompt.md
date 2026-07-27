# LANE BOARD — the living index (open this first)

**What this file is.** The one place that says, for every lane: where it
stands, what its next session does, and which file to open. **Every lane
session, at close, updates its own row here and appends to the PENDING log
below if it parks work.** Keep entries to 2–4 lines; depth stays in the
per-lane files. Do not let this file go stale — it is the first thing a
returning session reads.

Shipped through **v0.3.13 (13 skills)** as of 2026-07-27:
facade-module-architecture 0.3.0 · api-surface 0.3.2 · module-feature 0.3.3 ·
error-handling 0.3.4 · elasticsearch-search 0.3.5 · distributed-caching 0.3.6 ·
message-keys 0.3.7 · distributed-lock 0.3.8 · ef-core-data-access 0.3.9 ·
choosing-a-dotnet-skill 0.3.10 · dotnet-testing 0.3.11 · automapper-mapping
0.3.12 · auth-and-security 0.3.13.

## Lane status

| Lane | Open this file | Status (last update) | Next session does |
|---|---|---|---|
| **A — Data & Feature Spine** | `next-session-prompt-A.md` | S9b closed 2026-07-27: `auth-and-security` v0.3.13 shipped (module-feature, ef-core-data-access before it) | Confirm with the user whether the queue is unfrozen; if yes: `domain-modeling`, then `modern-csharp` (order TBC). Warm-up task carried: fix the stale line `module-feature/references/validation-rules.md:322` (S15 flag) |
| **B — API & Security Surface** | `next-session-prompt-B.md` | Queue COMPLETE at S15 close (api-surface, error-handling, message-keys, dotnet-testing). Lane closed | Nothing — the B file exists to hold its Lane log for rubric harvesting. Reopen only by explicit user direction |
| **C — Infrastructure Services** | `next-session-prompt-C.md` (mirrors the tree's CLAUDE.md while C is in flight) | S16 closed 2026-07-27: `automapper-mapping` v0.3.12 shipped; **S17 in flight: `mediatr-messaging`** | Finish S17 (mediatr-messaging + mandatory router edits, same commit). After that: `observability` / `background-worker` / `http-resilience` remain user-PENDING |
| **Rubrics — 4 solo sessions** | `next-session-prompt-rubrics.md` | Not started. All lane ledgers now feed them (biggest: S9b's 37-item auth ledger in CHANGELOG 0.3.13 + lane-A log) | Run one rubric per session, solo, sequential — never in parallel with a lane |
| **D — Process Integration** | `next-session-prompt-D.md` | Blocked by design: runs ONLY after the four rubrics (dotnet-testing prerequisite already shipped) | Closed-loop workflows + specialist agents per the approved S14 spec |

**Solo-only (never in a lane):** `project-scaffolding` (pending), the four
rubrics, Lane D.

## PENDING log (append-only; any lane may park work here)

Format: `- [lane, date] what was parked — where the detail lives — what unblocks it`

- [A, 2026-07-27] `domain-modeling`, `modern-csharp` — detail in
  `next-session-prompt-A.md` — unblocked when the user confirms the S14 freeze
  is lifted for them and picks the order.
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

## Standing rules (unchanged, summarized)

- One session, one deliverable; lanes share one working tree — stage only your
  own paths; expect mid-session `main` movement (conflict rule: keep both
  CHANGELOG entries, renumber yours above theirs).
- The three-way loop (`three-way-skill-loop` skill) is mandatory for any skill
  piece; main session coordinates only. R7/R8 stay with the user.
- Prove-it at ship: validate + `claude plugin update
  dotnet-standards@dotnet-standards-dev` + details shows the new count +
  `installed_plugins.json` points at the new cache + delete `reference/` from
  the new cache dir. Both manifests must agree on the version.
- Artifact language English; talk to the user in Vietnamese.
