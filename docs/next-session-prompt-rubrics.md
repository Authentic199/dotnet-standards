# Review Rubrics — one prompt, four solo sequential sessions

> Copy everything below the line into a fresh Claude Code session in
> `D:\ALTA\Project\dotnet-standards`. **Precondition: `dotnet-testing` (Lane B)
> and `choosing-a-dotnet-skill` (Lane C) are BOTH merged into `main`.** These
> sessions run SOLO and SEQUENTIALLY — never two rubrics in parallel, one
> rubric per session. Each session ships the FIRST unshipped row in the
> progress table, then checks it off and rewrites the "next" pointer.
> After the fourth ships → Lane D (`next-session-prompt-D.md`) unlocks.

---

## PROGRESS (the session updates this at its close)

| # | Rubric | Status |
|---|---|---|
| 1 | `dotnet-code-review` | ☐ |
| 2 | `dotnet-architecture-review` | ☐ |
| 3 | `dotnet-security-review` | ☐ |
| 4 | `dotnet-performance-review` | ☐ |

## CONTEXT

I am building `dotnet-standards`. This session ships ONE review rubric — the
first unshipped row above. Superpowers owns the review *process*
(`requesting-code-review`, `receiving-code-review`); these rubrics are
**.NET-specific review knowledge consumed by that process** — a `combine`: no
Superpowers file is touched, no competing review workflow is created, and
**rubrics are skills, not commands** (no slash-command name — that is what
sidesteps collision with the built-in `/code-review` and `/security-review`;
settled in `00-brainstorm.md` §5, do not relitigate). `reference/dotnet-claude-kit`
is read-only (pinned SHA `cd83d315986c27621da178dad73bd95d503c1540`).
`reference/projects/` is gitignored; R7/R8 as always — the user names any
exemplar or anti-example from real code; ask, never self-select.

**You own ONLY `skills/<this-session's-rubric>/` and this file.** Refuse and
log anything else. **START IN YOUR OWN WORKTREE:**
`git worktree add ../dotnet-standards-rubric-<n> -b rubric/<skill-name> main`

## STEP ZERO — HARVEST BEFORE MINING (mandatory first action)

Before reading any kit file, harvest the banked rubric material:

1. **Lane logs** (`docs/next-session-prompt-B.md`, `-C.md`, `-A.md`, git
   history of each) and **CHANGELOG** — the lanes deliberately banked "unruled
   candidates for a future review rubric": S13's four (dead
   `HttpCustomException.Value`; unpinned parameterless `BadRequestException()`;
   the S3-catch logging unconditionally while the general path gates at >=500;
   `ErrorResponseSettings` read per-request bypassing options) and S14's two
   (the catch-filter rule — *"a filter that converts status must exclude
   exceptions that already carry the right one"*; the semaphore-registry
   cleanup race). More may have accrued since — read every lane log.
2. **The installed skill bodies** — every honest note, "(Drift, noted
   once: …)" and deviation marker in the shipped skills is a rubric row
   candidate: the skills say what SHOULD hold; the rubric checks that it DOES.
3. Only then open the kit anchors for this session's rubric (below).

## THE FOUR RUBRICS — content brief per session

All four follow brainstorm §5 + the TRIAGE rows (closed input, but their
Rationale columns are the distilled brief — quoted essentials below).

**1. `dotnet-code-review`** — kit anchors `skills/code-review/` (A09) +
`skills/de-sloppify/` (A13). Keep: the blast-radius table (review depth follows
blast radius, not line count); the priority order (data access → security →
concurrency → integration → correctness → tests → style LAST); the report
template (Critical / Warnings / Suggestions / Architecture / Test coverage /
What's Good); A13's **slop taxonomy only** (unused usings, analyzer warnings,
dead code, stale TODO/HACK/FIXME, unsealed non-inherited classes, dropped
`CancellationToken` — plus the safety checks before deleting "dead" code:
reflection, DI-convention registration, serialization). Drop: A13's execution
pipeline (Superpowers + `/simplify` own it); A09's per-architecture branches
(only Facade/Module exists here). Add: the harvested candidates that are
code-quality-shaped (the catch-filter rule, DI lifetime traps from A14's
captive-dependency note).

**2. `dotnet-architecture-review`** — kit anchor `skills/arch-check/` (A02) +
`facade-module-architecture`. Keep: dependency-direction audit, cycle
detection, namespace-leak probes, presentation-boundary check, the
CRITICAL/HIGH/MEDIUM/INFO severity ladder. Collapse the kit's four-baseline
table to the ONE real architecture (`Core` → `Infrastructure` → `Web`,
`Facades/` × `Modules/`) — the shipped skill bodies define conformance; the
rubric checks against THEM (facade anatomy, composition-root rules, capability
placement, the settled `Not for:` boundaries).

**3. `dotnet-security-review`** — kit anchor `skills/security-scan/` (A32) +
`api-surface`. Keep: the 6-layer taxonomy, the OWASP Top 10:2025 mapping,
severity-with-context rules (test fixtures / `appsettings.Development.json`
are not HIGH; a missing XML comment is never a security finding; Critical =
exploitable-now), and the **honesty rule VERBATIM** — "static analysis, not a
penetration test" appears in every report. Re-express Minimal-API samples as
controller attributes (`[Authorize]`/`[AllowAnonymous]`). Note: with
`auth-and-security` pending, auth-internals depth is limited to what shipped
skills state — the rubric checks posture, it does not invent auth doctrine.

**4. `dotnet-performance-review`** — kit anchor: the `performance-analyst`
agent's method (read it for the checklist shape only — no agent is registered).
Core rows: N+1 queries and missing `Include`/projection, excess allocation,
async blocking (`.Result`/`.Wait()`/sync-over-async), missing indexes,
`AsNoTracking` on read paths, plus the shipped skills' performance notes
(cache connection policy, `terminateAfter`/`Refresh` traps, eager-connect
costs, lock-hold duration and the interleaving test).

**Every rubric:** decision-layer body + `references/` split through the loop as
usual; severity vocabulary must be ONE ladder consistent across all four (the
first session sets it — CRITICAL/HIGH/MEDIUM/INFO from A02/A32 recommended —
and later sessions reuse it; Lane D's flows will consume this vocabulary).
**C01 note:** the kit anchors lean on a Roslyn MCP that was never adopted —
every MCP-dependent step must degrade to explicit manual inspection
instructions, stated in the rubric rather than hidden.

## PROCESS

Use the **`three-way-skill-loop`** skill (now packaged) with `skill-writer-a`
(Author A), `skill-writer-sp` (Author B) and `skill-arbiter` — per piece, in
the lane worktree. The arbiter MUST invoke `skill-creator:skill-creator` live
(if `Unknown skill`, restart the parent session — subagent rosters snapshot at
parent start). **STANDING DELEGATION (LAW, memory
`delegate-on-recommendation`):** execute recommendations, report done; ask only
when genuinely undecidable. Carve-out: naming canonical sources / labelling
anti-examples from real code stays with the user. Description law (§5 of
`02-repo-structure.md`) binds each rubric's description — `Not for:` must route
to the other three rubrics, to the knowledge skill that OWNS each area, and to
Superpowers for the review process itself.

## HARD CONSTRAINTS

1. One session, one rubric. Extra requests → log under `## Rubric log` below
   and refuse.
2. Prove it: validate + reinstall from the worktree + `claude plugin details`
   shows the new skill count; report failures honestly. Merge/version protocol
   as always (patch +1 vs `main` at merge time, both manifests agree, CHANGELOG
   at top, one install at a time; check `installed_plugins.json` before
   touching any cache dir; delete `reference/` from the new cache copy).
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol (branch `rubric/<skill-name>`, merge into `main`),
   tick this file's PROGRESS row, append to `## Rubric log`, commit this file.
   After rubric #4: state explicitly that Lane D is UNLOCKED.

## Rubric log

(empty — first rubric session starts it)
