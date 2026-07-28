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
| 1 | `dotnet-code-review` | ☑ v0.3.15, 2026-07-28 |
| 2 | `dotnet-architecture-review` | ☑ v0.3.17, 2026-07-28 |
| 3 | `dotnet-security-review` | ☑ v0.3.18 (+0.3.19 fix), 2026-07-28 |
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

- **Rubric #3 (`dotnet-security-review`, 2026-07-28) — shipped v0.3.18 +
  v0.3.19 budget fix** (post-assembly coordinator catch: body was 804 lines vs
  the <500 bar — the final pass had reported "~470" WITHOUT counting; root
  cause structural, all-prose checks where the sibling convention is table
  rows; 26 of 29 checks converted, 498 final, nothing dropped, references file
  untouched. Defect class recorded: "an estimate presented as a count" — line
  counts only from an actual `wc -l` from now on. Margin warning: 498/500.)
  Verdicts: P1 MERGE, P2 MERGE, P3 MERGE, P4 (router) MERGE; final whole-skill
  consistency pass FAIL→PASS (D1 honesty-rule wording drift between Principle 1
  and the template — blocking, canonicalised; D2/D3 body foreclosing its own
  references tail in layers 1 and 5 — softened). Ran in worktree
  `../dotnet-standards-rubric-3`, branch `rubric/dotnet-security-review`,
  merged fast-forward at `2eb6150`; install proof: 17 skills,
  `installed_plugins.json` at the 0.3.18 cache (gitCommitSha = merge commit),
  `reference/` (2.4G) deleted from the cache copy, both manifests at 0.3.18.
  Full ruling list in CHANGELOG 0.3.18.
- **Rubric #3 rulings the next session inherits:** severity calibration
  precedents — refresh-replay response = HIGH not CRITICAL (first mechanical
  application of the ladder's precondition test to overrule an author);
  fail-closed defects de-escalate and SAY SO in the check (3.6, 5.4);
  availability defects on the key path are MEDIUM and say what they are (2.6).
  In-file `Refused — and why` table + report-level `Suppressions applied`
  section are first-class (shared inventions by both authors, upheld). First
  C# BAD/GOOD block in a rubric body (the R8 comment-hidden-property
  anti-example under 6.2) — recorded divergence. The `dotnet-performance-review`
  reservation row now sits in the router's *Not yet covered* — **rubric #4
  deletes it as part of its own alignment.**
- **Rubric #3 coordinator catches (the loop worked — 5th+ instances of the
  shared-blind-spot series):** arbiter's cut of the shipped "two independent
  gates" sentence reversed on file evidence (jwt-and-tokens.md:452-455);
  arbiter twice misread `Required(...)` exclusion semantics (called A's
  optionality clause false, then claimed a false auth-and-security intra-skill
  contradiction — jwt-and-tokens.md:89-92 documents the semantics; the false
  Known-seam note was withdrawn before it could defame a healthy skill). Both
  authors corrupted the shipped five-site principal-type list identically;
  A fabricated an `[AllowAnonymous]` stale-principal mechanism and miscited
  four body-check titles; B abridged the pipeline order (dropped APM+CORS) and
  inverted 5.1's rationale. All caught by file verification, none shipped.
- **Rubric #3 user rulings:** blanket "làm như bạn khuyến nghị đi" delegation
  at session start — R7/R8 carve-outs held (`reference/projects/` never
  opened, no new exemplars; the two user-banked S9b items — username
  enumeration, and the S12 comment-hidden DTO property — shipped under their
  existing bank/label authority, sanitized).
- **Banked for rubric #4 (`dotnet-performance-review`):** everything in the
  0.3.15 bank (dead-Include cost, sync Count, `entities.Any()` probes,
  five-round-trip search chain, N+1/projection/index checklists) + from this
  session: the DoS-shaped `LIKE`-pattern note in 3.1 routes rate-limiting/DoS
  questions to rubric #4's Deep-dives row; ClockSkew-Zero clock-drift
  trade-off (no shipped owner — likely refuse-and-bank again).
- **Banked, needing a future owner:** test-posture security check ("a test
  scheme reachable from a deployed composition") — refused, no shipped
  sentence; `[ApiKey]`+`[HasPermission]` BAD/GOOD anti-example candidate (not
  user-labelled); `GetFallbackPolicyAsync` null as an R8 hazard-label
  candidate (user's call).

- **Rubric #2 (`dotnet-architecture-review`, 2026-07-28) — shipped v0.3.17.**
  Verdicts: P1 MERGE, P2 MERGE, P3 MERGE; final consistency pass PASS on the
  skill files (zero defects), one defect in a coordinator edit fixed
  (rubric-1 5.8 pointer: owner column must name the LEGISLATING skill, `Find:`
  must stay), one router disambiguation arm added on arbiter recommendation.
  Full ruling list in CHANGELOG 0.3.17. Ran in worktree
  `../dotnet-standards-rubric-2`, branch `rubric/dotnet-architecture-review`,
  merged fast-forward; install proof: 16 skills, registry at 0.3.17 cache
  (gitCommitSha = merge commit), `reference/` (2.4G) deleted from cache.
- **Rubric #2 rulings the next sessions inherit:** severity ladder cited from
  rubric #1 and CALIBRATED per rubric (crossing = HIGH, shape inside a correct
  boundary = MEDIUM, placement alone never CRITICAL) — #3/#4 may calibrate the
  same way, never re-define; check numbering is per-audit and CONTINUES into
  the references file (body 1.1–5.7, catalogue 1.7–5.12, no reuse — a citation
  never needs a filename); kit-anchor divergences are RECORDED in the CHANGELOG
  (arch-check Step 1 four-baseline table collapsed, Step 3 cycle audit dropped
  with reason); false-positive suppressions ("not a finding" blocks) are
  first-class rubric content when the kit anchor generates them.
- **Claimed/settled cross-rubric at #2:** rubric #1's check 5.8 → pointer to
  rubric #2's 4.9 (number and `Find:` kept, owner = `module-feature`); check
  5.9 stays in rubric #1; entity-base-response stays rubric #1's 2.7.
- **Banked, verified orphan:** `Guid.NewGuid()` on entity keys —
  `facade-module-architecture/references/core-contracts.md:40` states the
  sequential-key rule, NO rubric checks it; natural home is rubric #1's
  review-rubric area 1 (data access) — a dotnet-code-review-owning session.
- **Flagged outside ownership (CHANGELOG 0.3.17 Known seams + board PENDING):**
  fma still prints `Events/` in its module tier list (SKILL.md:197,
  references/modules.md:26) — stale vs `mediatr-messaging`'s `DomainEvents/`
  ruling; rubric #2's catalogue ships an explicit precedence note.
- **Rubric #2 process notes:** all three agents pinged once, continued across
  P1→P2→P3→final pass via SendMessage; drafts forwarded VERBATIM every round
  (clean session); coordinator catches: body 4.9 section-name erratum (*When a
  service outgrows one file*), 3 of A's + 1 of B's body cross-references
  miscited (arbiter verified and fixed all); the `uniq -d` duplicate-
  registration one-liner was RUN before shipping; standing delegation held,
  R7/R8 untouched (no new exemplars, no new labels, `reference/projects/`
  never opened).

- **Rubric #1 (`dotnet-code-review`, 2026-07-27/28) — shipped v0.3.15.**
  Verdicts: P1 MERGE, P2 MERGE, P3 MERGE; final consistency pass PASS (3
  defects fixed, incl. new P2 check 5.13 closing a dangling nullability
  cross-reference). Full ruling list in CHANGELOG 0.3.15.
- **SEVERITY LADDER SET — rubrics #2–#4 REUSE IT, do not re-derive:**
  CRITICAL / HIGH / MEDIUM / INFO, consequence-based; dropped
  `CancellationToken` = HIGH default, CRITICAL only when corrupting/exposing;
  every check a manual instruction (grep / open file / build-and-read-
  diagnostics); report shape = every section always appears, `None.` when
  empty; checks trace to a shipped body or are universal defects — nothing
  else qualifies; cross-references cite number AND name.
- **User rulings this session:** conformance surface = ALL skills on `main` at
  merge time (13 at our merge; count before trusting any list); "seal
  non-inherited classes" is KIT doctrine, not house law — excluded everywhere,
  standing note in cleanup-checklist.md; previously labelled anti-example
  ledgers reused sanitized, no new exemplars named, `reference/projects/`
  untouched all session.
- **Shared-blind-spot catches (the loop worked — 3rd+4th instances of the
  S13b/S15 series):** both authors imported kit sealing doctrine as house law;
  Author B twice wrote plausible .NET instinct as house law (`= default` on
  action token param; `request.X!` nullable-by-convention) — cut, banked as
  anti-example candidates. The arbiter's own P2 briefly carried the dangling
  5.13 target — caught in its consistency pass.
- **Flagged to Lane A (NOT fixed here — ownership):** `module-feature/
  SKILL.md:187` + validator examples at lines 165–172 carry the superseded
  entity-typed `Messages<T>` form — second instance of the
  `validation-rules.md:322` drift family (S15). Lane A owns both fixes.
- **Banked for rubric #2 (`dotnet-architecture-review`):** P2 checks 5.8
  (`Services/` contents) and 5.9 (base list on non-core part) are structural —
  rubric #2's session may claim them and slim P2; "Not this skill" convention
  is 7/11 shipped bodies (this skill omitted it — Routing does the job);
  description-headroom decision banked: if a future entry is needed at
  97/100 words, drop `facade-module-architecture` from the trailing
  conventions list (its nouns are covered by the dotnet-architecture-review
  entry).
- **Banked for rubric #4 (`dotnet-performance-review`):** all
  performance-shaped S9 ledger items deliberately excluded from #1 (dead-
  Include cost side, sync Count, the 3 `entities.Any()` probes,
  five-round-trip search chain, N+1/projection/index checklists).
- **Mid-session main movement absorbed THREE times:** automapper-mapping
  v0.3.12 (S16) → P1 description re-trimmed to 97 words + routing row;
  auth-and-security v0.3.13 + router hotfix v0.3.14 (S9b) → P2 2.1 gained
  `[ApiKey]` as the third explicit access decision; router alignment row +
  order-note extension ("… → tests → review") shipped in the 0.3.15 commit
  per the alignment rule (S9b hotfix precedent; arbiter-reviewed).
- **Delegation calls recorded (standing delegation; blanket grant
  mid-session, each call still reported for cheap veto):** severity ladder
  adopted from brief; 11-not-9 conformance correction; description
  NEITHER-trim protocol; no "Not this skill" section; ct = HIGH; Cleanup
  candidates section kept (always-appears semantics); 5.8/5.9 stay in P2;
  at-a-glance severity table cut; tool = grep -rn; message-keys governs where
  older material disagrees (no sibling named defective in the artifact);
  diff-scope stated once; volume cap added; inline `4/4 clear` marker kept;
  P1 Principle 2 five-word amendment ("build and read the diagnostics")
  approved; 53 checks kept uncut.
