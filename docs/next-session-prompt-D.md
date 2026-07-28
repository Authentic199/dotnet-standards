# Lane D — Process Integration · Session D2: the `bugfix` flow (v1.5)

> **LANE PENDING — user direction, 2026-07-28.** Do not open this session until
> the user unfreezes Lane D. At session start, confirm with the user that the
> freeze is lifted; do not decide that yourself (same rule as Lane C's frozen
> queue). Written at the close of D1, 2026-07-28, which shipped
> process-integration v1 at **v0.3.21**. When unfrozen: copy everything below
> the line into a fresh Claude Code session in the dotnet-standards checkout.

---

## CONTEXT

I am building `dotnet-standards`. Lane D owns the **process-integration
layer** — shipped at v1 (v0.3.21, D1, 2026-07-28): `dotnet-feature-flow` +
`/dotnet-feature`, `dotnet-review-flow` + `/dotnet-review`, six specialist
agents (4 read-only reviewers, 2 testers), the SessionStart `superpowers-check`
hook, the two-layer description. **The design source of truth is
`docs/superpowers/specs/2026-07-27-process-integration-design.md`; the v1
rulings live in CHANGELOG 0.3.21 — read both FIRST and treat everything in them
as settled.** No Superpowers file may ever be modified — call, never copy.

**This session's deliverable is v1.5: the `bugfix` flow (spec §6.3).**
Shape already approved: PHASE 0 → `superpowers:systematic-debugging` → fix →
the shared TEST/REVIEW blocks → git. ~70% shared with `feature`.

**You own ONLY:** `skills/dotnet-bugfix-flow/` (or the name the loop settles),
`commands/dotnet-bugfix.md`, the router rows + CHANGELOG + version alignment
the ship protocol mandates, and this file. Refuse and log anything else.

**START IN YOUR OWN WORKTREE:**
`git worktree add ../dotnet-standards-laned-d2 -b lane-d/bugfix-flow main`

## SETTLED AT D1 — DO NOT RELITIGATE (full detail: CHANGELOG 0.3.21)

- The shared block is `dotnet-review-flow`'s heading
  `## The shared block: TEST-LOOP then REVIEW-LOOP` — bugfix QUOTES it verbatim
  and invokes the skill embedded, exactly as `dotnet-feature-flow` does
  (arbiter ruling D1: bugfix quotes review-flow's heading; feature-flow
  deliberately exposes NO stable heading of its own — do not mint one there).
- Embedded mode must be STATED in the invocation; absent it, review-flow
  defaults to standalone (offers instead of fixing) — the safe direction.
- Diff preparation + the pre-build gate sit on the CALLING flow's side of the
  seam; the caller owns repo/branch/worktree, performs review-flow's
  `Diff preparation — the spawn contract` section, then runs the block.
- Tester verdict vocabulary is CLOSED (six strings, see the tester bodies);
  `RED — environment` halts immediately without consuming a round;
  `tier absent — nothing run` never blocks the loop and never reads as a pass.
- PHASE 0 checks Superpowers by LOADING a Superpowers skill (never the
  registry); plugin completeness by the session's skill/agent ROSTER (never by
  loading five rubrics). STOP style with exact remedy.
- Severity = CRITICAL/HIGH/MEDIUM/INFO (dotnet-code-review's ladder, cited
  never defined); loop exit = no CONFIRMED CRITICAL/HIGH; MEDIUM/INFO never
  chased. Caps: 5 test / 3 review — they are review-flow's; a cap number in a
  calling flow's body is a defect.
- Fix route matches the implementation route (context symmetry). GATE placement:
  a human gate binds the actual choice point (D1 moved GATE 2 inside PHASE 6 to
  the finishing-a-development-branch option choice — same reasoning will apply
  to any bugfix gate).
- Description law §5 binds flow SKILLS in full; agent files are NOT under §5
  (noun-phrase register, <100 words, Not for: names sibling AGENTS).
- Flow skills teach nothing; single-body unless a true long tail appears
  (117–450 norm, <500 hard).

## THE PROCESS

Three-way loop (`three-way-skill-loop`) for the flow skill body+frontmatter;
command + alignment edits are mechanical (single author + arbiter sanity pass).
The arbiter MUST invoke `skill-creator:skill-creator` LIVE (`Unknown skill` →
restart the parent session). Verification target = the spec + CHANGELOG 0.3.21
+ the shipped v1 bodies (not reference/projects/). STANDING DELEGATION (LAW):
execute clear recommendations, report, log each use; R7/R8 stay with the user.

## HARD CONSTRAINTS

1. One session, one deliverable (+ router/CHANGELOG/version alignment, same
   feat family). Prove it: validate + `claude plugin update
   dotnet-standards@dotnet-standards-dev` + details shows the new counts +
   `installed_plugins.json` points at the new cache (gitCommitSha = merge
   head) + delete `reference/` from the new cache dir + ONE live smoke test of
   `/dotnet-bugfix` on a small real bug (the D1 smoke-repo pattern in the
   session scratchpad works: tiny solution + seeded defect). Report failures
   honestly.
2. Merge protocol: patch +1 vs main at merge time, both manifests agree,
   CHANGELOG at top, merge main INTO the lane branch BEFORE alignment edits.
3. Artifact language English; talk to the user in Vietnamese.
4. End: commit per protocol, rewrite THIS file for Lane D's next session (v2
   candidates: the deferred PM workflow and project-setup workflow — spec §2
   records both as technical debt; neither is designed), update the LANE BOARD
   row, carry the Lane log.

## Lane log

- **D1 (2026-07-28) — shipped process-integration v1 at v0.3.21** (merge head
  5d4c4c9; 20 skills + 2 commands + 6 agents + 2 hooks). All four loop pieces
  MERGE verdicts; full rulings in CHANGELOG 0.3.21. Smoke test: `/dotnet-review`
  run in a fresh headless session on a scratch .NET solution — PASS end to end
  (PHASE 0, gate build, 2 testers + 4 reviewers parallel, closed verdict
  strings used correctly incl. `tier absent`, CONFIRMED-vs-PLAUSIBLE
  verification with real evidence, cross-lens dedup, standalone offer honored —
  nothing auto-fixed). ONE observed deviation, logged: subagents spawned by the
  flow had NO Skill tool on first attempt — all three affected reviewers
  stopped per their body's rule, the flow's retry-once resent with the rubric
  file path on disk, and every lens completed. First entry in the
  observed-behaviour bank; D2 should check whether agent definitions should
  point at the rubric PATH as the documented fallback.
- D1 process events: blanket delegation per the brief; agents pinged once and
  continued via SendMessage across all pieces; arbiter loaded skill-creator
  LIVE first thing (no restart needed). `main` never moved mid-session (solo
  day). Two shared-blind-spot catches by the arbiter (piece 1: the Low-radius
  report-collapse exception; piece 4: the router's uncovered-area carve-out,
  router SKILL.md L114–115) — the S13b/S15/S17 pattern held twice more.
- D1 verified facts recorded for reuse: plugin commands are always namespaced
  `/dotnet-standards:<name>`, bare form as fallback (v2.1.216+), plugins CAN
  shadow built-ins; agent frontmatter fields = name/description/tools/model/
  disallowedTools (`capabilities:` in the house template is drift);
  `superpowers:writing-plans` has no "use-case" unit (unit = `### Task N`);
  `superpowers:finishing-a-development-branch` presents push as a user-chosen
  option; `installed_plugins.json` shape = `{"plugins": {"name@marketplace":
  [{...}]}}`.
- D1 coordinator boundary calls (logged for veto): minimal README corrections
  beyond the literal ownership list (three lane-falsified statements + the CQRS
  phrase, arbiter-flagged, recorded in CHANGELOG 0.3.21); `hooks/README.md`
  restructure to keep `post-edit-format` contiguous with its Step-4 footnote.
- D1 banked for later: rewrite the six agents' rationalization tables from
  OBSERVED behaviour if smoke/real runs diverge from the predictions (they are
  predicted, not baselined — B's flag, arbiter-endorsed); the four reviewer
  bodies share ~50% structure with no include mechanism — a shared-wording edit
  is made four times (recorded maintenance cost); flow-level single pre-build
  is encoded as a GATE (review-flow) — if artifact-lock collisions still occur
  in practice, revisit; `README.md` install snippet still names a stale path
  (`D:/ALTA/Project/...`) — pre-existing, parked to the LANE BOARD PENDING log.
- Deferred (spec §2, unchanged): PM workflow (BA requirements → task skeleton),
  project-setup workflow (per-project CLAUDE.md). Neither is designed; v2
  discussion starts from the spec's Deferred section.
