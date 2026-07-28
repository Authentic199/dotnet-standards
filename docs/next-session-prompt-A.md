> **OPENER — 2026-07-27, S9b close (Lane A).** This file opens **Lane A's next
> session**. Queue: `domain-modeling`, then `modern-csharp` — **confirm the
> choice and order with the user at session start** (the S14 freeze was lifted
> for `auth-and-security` by explicit reassignment; whether the rest of the
> queue is unfrozen is the user's call). `auth-and-security` SHIPPED v0.3.13
> this session — it is no longer pending anywhere. If the tree's CLAUDE.md
> names another lane's deliverable, that file belongs to that lane; Lane A's
> brief is THIS file.

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever
be modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `apsp-backend` (production, canonical), `BE-Ops-Service`
(reusable base), `BE-service-booking` (anti-example quarry),
`digitalcity-backend` (older quarry, extension-only). Triage (`docs/TRIAGE.md`)
is closed input.

**This is Lane A.** Shipped: `module-feature` (v0.3.3), `ef-core-data-access`
(v0.3.9), `auth-and-security` (v0.3.13, S9b). You own ONLY the next queued
deliverable's directory and this file. Main carries **v0.3.13 (13 skills)** at
S9b close; Lane C is running S17 (`mediatr-messaging`) and may move `main`
mid-session — conflict rule: keep both CHANGELOG entries, renumber yours above
theirs, align cross-skill names. **Lanes share one working tree: before every
commit run `git status` and stage ONLY your own paths.** (S9b ran directly in
the shared checkout on a lane branch; Lane C uses worktrees — either works, but
verify `git branch --show-current` before writing.)

## THE THREE-WAY PROCESS — MANDATORY, SKILL-DRIVEN

**Invoke `three-way-skill-loop` at session start** — the main session
COORDINATES ONLY (memory `author-a-delegated`). Author A = `skill-writer-a`,
Author B = `skill-writer-sp`, arbiter = `skill-arbiter` (invokes
`skill-creator:skill-creator` LIVE; `Unknown skill` → restart parent session).
Ping all three first; the ping doubles as the context-package load. Drafts to
the arbiter **VERBATIM — never summarized, never bracket-condensed** (S16 AND
S9b both slipped here; S9b needed supplements and produced racing verdicts).

**S9b process lessons (new, binding):**
- **Agent message races are real.** Queued SendMessages to one agent can spawn
  overlapping runs with partial transcripts, producing multiple verdict-shaped
  outputs that contradict each other. Treat only the latest self-consistent
  ruling as real; on any confusion, ask the agent to restate what it can see.
- **Quote held text to agents; never cite "your earlier ruling"** — an agent
  may not be able to see its own prior outputs (arbiter's explicit request).
- **Sanitize is a coordinator duty, not just an author duty:** an author
  reproduced REAL committed key values from memory believing them invented.
  Grep every final text against the real secrets you have seen before ship.
- Coordinator verification duties held: diff rephrasings (S12), verify shared
  claims (S13b/S15/S9b: both authors' revocation story was wrong the same way),
  diff modality, verify self-declared additions, and **diff successive verdict
  versions against each other** (S9b: a later pass regressed a corrected
  bullet; the coordinator caught it).

**STANDING DELEGATION (LAW):** execute clear recommendations, report them with
brief confirmations so vetoes stay cheap, log each use; ask only the genuinely
undecidable. Carve-outs remain the user's alone: naming canonical
sources/exemplars (R7), labelling anti-examples (R8 — S9b: the user granted
labels for exactly four embeds; everything else stays ledger).

## READING DISCIPLINE

Ask the user for the exemplar list at session start — never select exemplars
yourself (S9b variant: the user may delegate the scan-and-propose, then
approve). Widening = announced targeted lookup (S9b ran ~10, all logged). Bash
find/ls/grep, never Glob, inside `reference/projects/`. **Exclude
`apsp-backend/.claude/worktrees/` from any census** (S16: ~5× inflation). R7:
one canonical source per area, never average — but a user DECREE composing
shapes across projects is a ruling, not an average (S9b JwtSettings). R8:
anti-examples are code the user points at. Sanitize: no project names, no
business-domain nouns, no real paths, **no real key material** (S9b: grep
finals against every real secret read during the session). Sample vocabulary:
Order/OrderLine/Customer + FulfilmentStatus; principal families User/Device/
Customer are ruled generic-enough for auth contexts.

## SETTLED — DO NOT RELITIGATE

- Everything in shipped bodies through **v0.3.13** (read installed bodies as
  baseline; `claude plugin details` shows 13 skills). Headline S9b rulings now
  settled in `auth-and-security` + CHANGELOG 0.3.13: the JwtSettings decree
  (double + four Get* helpers, UTF-8, multiplicity per client scheme);
  Required(params) = exclusions; issuer/audience stamped-not-verified; signing
  key = family boundary; authorization = DB read (id only from principal);
  sliding cache + sync-verbs-evict; verify middleware order
  (Routing → ExceptionHandler → Authentication → CurrentUser →
  VerifyJwtUser → Authorization); secrets = shape committed, keys
  per-environment, no vault convention prescribed.
- Description law (`02-repo-structure.md` §5): third person, <100 words by
  wc -w (measure it), trigger-noun pushy, `Not for:` naming every owning
  sibling. No H1 in skill bodies (references files DO carry H1 + TOC when
  >300 lines).
- The `references/` mechanism: splits go through the loop.
- Stack: .NET 8, Controllers, Swashbuckle, NO API versioning, FluentValidation
  + AutoMapper v12 (single-arg MapperConfiguration), PostgreSQL primary;
  MediatR = in-process messaging, not CQRS.

## HARD CONSTRAINTS

1. One session, one deliverable — **plus the mandatory router merge-time
   edits, same session** (alignment rule, CHANGELOG 0.3.10: the router covers
   every skill on `main` at merge — new base-map row, fix any *not yet
   covered* arms, delete the reservation row; through the loop or
   arbiter-reviewed per the S16 precedent). S9b SKIPPED this and needed a
   0.3.14 hotfix — do not repeat. Extra requests → log under `## Lane log`,
   refuse.
2. Prove it: `claude plugin validate .` + `claude plugin update
   dotnet-standards@dotnet-standards-dev` (NOT install — reports "already
   installed" without refreshing; short name fails) + `claude plugin details`
   shows the new skill count + **verify `installed_plugins.json` points at the
   new cache** + delete `reference/` from the new cache dir (S9b: installer
   did NOT sweep it; manual delete was needed). Version = patch bump +1
   relative to whatever `main` carries at merge; BOTH manifests
   (`.claude-plugin/plugin.json` AND `.claude-plugin/marketplace.json`) must
   agree. Caches 0.3.7–0.3.12 left unreferenced in place.
3. Artifact language English; talk to the user in Vietnamese.
4. End: commit per protocol (lane branch `lane-a/<skill>`, feat commit, merge
   into main; expect mid-session `main` movement), then rewrite THIS file
   carrying the Lane log. Do NOT touch the tree's CLAUDE.md if it belongs to
   another lane.

## Lane log

- **S9b (auth-and-security, 2026-07-27) — shipped v0.3.13.** Verdicts: P1
  MERGE, P2 MERGE, P3 MERGE (+ five-patch delta on Draft A's late-arriving
  prose), P4 MERGE, P5 MERGE. Exemplars user-approved from coordinator
  proposal (delegated scan): apsp Facades/Auth/** + Identity (JwtToken,
  GrantPermission, Base) + Middleware/VerifyJwtUserMiddleware + Definitions
  (JwtTokenPayload, MXMPermissions) + security configs; ops comparator for the
  JwtSettings divergence only. User rulings: divergence decree (ops shape +
  apsp multiplicity, UTF-8); Zalo handler OUT; ApiKey IN (secondary);
  Password/** + CleanRefreshTokenWorker OUT; scheme family taught from code
  (no `User` scheme exists — project docs stale); FOUR anti-patterns labelled
  for embedding (type-name-as-data/fail-open; call-site key encoding;
  revoked-grant-never-lapses; committed key material).
- S9b delegated calls (recorded): test-auth content excluded (dotnet-testing
  owns); secrets scope = binding + non-commitment only; references split fixed
  pre-P1 (3 files); encoding ruling UTF-8; Required-semantics verified fact
  overrode Draft A; RequireExpirationTime drop accepted after arbiter
  verification; IDeviceJwtUser carve-out omitted; ValidateRefreshToken taught
  corrected + real call site (request validators); UseClaims override =
  taught mechanism (one corpus override, drift noted); hardcoded <User>
  handler taught neutrally; AppPermissions/PermissionDefinition/AppResource/
  AppAction naming; fallback-null taught with fails-open clause;
  PermissionsValue static-readonly correction approved; one body principle per
  P3/P4; numbering unified; "blocked" vocabulary; "logout" dropped;
  AP1 retrofit-guard sentence added.
- S9b verified mechanism facts (targeted lookups, all announced): config
  layering (base mandatory + env optional, reloadOnChange); MultipleScheme
  ForwardDefaultSelector routes on modelType claim by typeof FullName;
  ValidateDataAnnotationsRecursively = ReHackt.Extensions.Options.Validation
  7.0.1; JwtTokenPayload.ModelType = "modelType"; ValidateRefreshToken called
  from 3 refresh-request validators (scheme-typed); one UseClaims override
  (Device.cs:88, re-lists by hand); no lazy proxies; CacheKeys.GetKeyByModel =
  typeof(T).Name + id; Give/Revoke never evict (only syncs do); userPermissions
  const dead; Guards = seeding/equality only (AdminUserSeeder:38); pipeline
  order at Infrastructure/Startup.cs:103-110; ApiKeySettings binds from
  appsettings.json:109; ICurrentUser.Name has zero readers.
- **S9b anti-example ledger — 37 candidates, FOUR labelled/embedded; the rest
  are RUBRIC FEED** (full lists live in the P1–P4 sections of CHANGELOG 0.3.13
  and the session scratchpad). Security findings held for the rubrics:
  username enumeration at login (NotFound vs Invalid(Password) texts);
  PermissionsValue hot-path dictionary rebuild; sync-over-async in handler and
  cache reads; catch(Exception)→null; Replace-based prefix strip; count-guard
  duplicate defeat; FixedTimeEquals over MemoryMarshal.Cast; Permission value
  equality while EF-tracked; dead members (userPermissions, ICurrentUser.Name,
  GetApplicationId nullability, serviceProvider param); ApiKey namespace drift
  (→ facade-module-architecture); bare UnauthorizedResult (→ error-handling);
  Messages<T> reflection (→ message-keys); Identity facade hosting a worker
  (→ background-worker); design forks banked: fallback null vs deny-by-default,
  hardcoded <User> handler vs polymorphic schema, sliding vs absolute expiry.
- S9b cross-lane events: opened with CLAUDE.md as Lane B's stale B4 opener
  (collision averted — the reassignment held); mid-session `main` moved
  (Lane C shipped automapper-mapping v0.3.12 + rewrote CLAUDE.md as Lane C
  S17 opener). This session did not touch CLAUDE.md.
- S9b environment: install was 0.3.11 user-scope at start → 0.3.13 at close;
  `claude plugin update` refreshed correctly; reference/ manually deleted from
  the 0.3.13 cache.
- **Carried forward:** `domain-modeling` then `modern-csharp` (order TBC);
  the rubric sessions consume the ledger above plus the S13/S13b/S12/S8/S9
  lists; "Services/ is not a dumping ground" rubric item still carried; the
  `validation-rules.md:322` stale-line fix (flagged S15; Lane A owns
  module-feature) remains OPEN — a good warm-up task for the next session,
  **now a family of two**: rubric #1 (CHANGELOG 0.3.15) found the same
  superseded entity-typed `Messages<T>` form at `module-feature/SKILL.md:187`
  and in the validator examples at lines 165–172. Fix both in one warm-up
  chore (patch bump + reinstall proof per protocol; grep the whole
  module-feature skill for further instances while there).
