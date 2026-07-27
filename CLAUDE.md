> **LANE B STATUS — 2026-07-27, S15 close: QUEUE COMPLETE, NO BUILD DELIVERABLE
> OPEN.** `dotnet-testing` shipped v0.3.11 (Lane B's promoted B4). Lane B's
> former pending deliverable **`auth-and-security` was REASSIGNED to Lane A by
> explicit user direction** (commit `f2d60c0`, Lane A S9 close) — if a Lane B
> session appears to be building it, STOP and surface the collision.
> `observability` stays PENDING per the S14 reprioritization. What runs next is
> the **four review rubrics, solo sequential sessions** per the rubric-phase
> prompt (commit `268aeec`) — they are NOT Lane B sessions and do not use this
> file as their opener. This file now exists to (a) hold the Lane B log for
> rubric harvesting, (b) bind any future reopened Lane B session to the process
> below.

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever
be modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `ops-service`/`BE-Ops-Service` (reusable base),
`apsp-backend` (production, canonical), `be-booking` (anti-example quarry),
`digitalcity-backend` (older quarry, extension-only). Triage (`docs/TRIAGE.md`)
is closed input.

**Lane B has shipped:** `api-surface` v0.3.2, `error-handling` v0.3.4,
`message-keys` v0.3.7, `dotnet-testing` v0.3.11. Lane A owns `module-feature`
(v0.3.3), `ef-core-data-access` (v0.3.9), and now `auth-and-security` (+
`domain-modeling`, `modern-csharp` queued behind it). Lane C shipped
`distributed-caching` v0.3.6, `elasticsearch-search` v0.3.5,
`choosing-a-dotnet-skill` v0.3.10; `automapper-mapping` + `mediatr-messaging`
queued post-S15. Lane D (process integration) runs after the rubrics.

## THE THREE-WAY PROCESS — MANDATORY FOR ANY SKILL PIECE

Codified as of S15 (user direction) in **`.claude/skills/three-way-skill-loop/`**
— load it; it binds. Summary: the main session is COORDINATOR ONLY and never
drafts. Author A = `skill-writer-a` agent (house methodology), Author B =
`skill-writer-sp` (Superpowers writing-skills), arbiter = `skill-arbiter`
(invokes `skill-creator` LIVE — if `Unknown skill`, restart the parent session).
Per piece: explain in Vietnamese → parallel independent drafts → arbiter verdict
A/B/MERGE/NEITHER with file-verified reasons → user approval → only then write.
Forward drafts to the arbiter VERBATIM, never summarized. Verify the arbiter's
self-declared additions; diff rephrasings against originals (S12); expect shared
blind spots in convergent drafts (S13b: request-typed success keys; S15: the
mirror — entity-typed validator assertions); diff modality (S13b: permission→
obligation; S15: not-chosen→"banned"). Standing delegation
(`delegate-on-recommendation` memory): execute clear recommendations, ask only
the genuinely undecidable, record each use in the Lane log. Agent-type
definitions in `.claude/agents/` hot-load mid-session (S15, observed); PLUGIN
skill rosters inside subagents still snapshot at parent startup (S13b).

## READING DISCIPLINE

The user names exemplars at session start — ask before reading anything in
`reference/projects/`; never self-select. Widening = targeted lookup, announced
(S15 precedent: the `VerifyJwtUserMiddleware` read that settled the test-auth
mechanism, and the AutoMapper-version grep). No bulk scans. Bash
find/ls/grep, never Glob, inside `reference/projects/`. R7: one canonical
source per area, user-designated; never average. R8: anti-examples are code the
user points at; ask before labelling. Sanitize: no project names, no
business-domain names, no real paths, no secrets.

## SETTLED — DO NOT RELITIGATE

- Everything in the shipped bodies: `facade-module-architecture` v0.3.0,
  `api-surface` v0.3.2, `module-feature` v0.3.3, `error-handling` v0.3.4,
  `message-keys` v0.3.7, `ef-core-data-access` v0.3.9,
  `choosing-a-dotnet-skill` v0.3.10, `dotnet-testing` v0.3.11 (and Lane C's
  caching/search bodies). Notably from S15: requests type validator messages /
  entities type outcome messages; the dotnet-testing toolchain (xUnit v3,
  Shouldly, NSubstitute, Testcontainers + Respawn; FluentAssertions v8+ banned,
  Verify/WireMock/MockQueryable declined); fixture-by-config-override;
  `Received/DidNotReceive` in exactly two shapes.
- Description law (`02-repo-structure.md` §5): third person `This skill should
  be used when…`, <100 words, trigger-noun pushy, `Not for:` naming every
  owning sibling. No H1 in skill bodies (S15 grep: majority convention;
  `message-keys` is the recorded outlier).
- The `references/` mechanism: decision-layer body, depth in references with
  conditional pointers; splits go through the loop.
- Stack: .NET 8, Controllers not Minimal API, Swashbuckle not Scalar, NO API
  versioning, FluentValidation + AutoMapper (v12 — single-arg
  `MapperConfiguration`).

## HARD CONSTRAINTS (for any reopened Lane B build session)

1. One session, one deliverable. Extra requests → log under `## Lane log` and
   refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new skill
   count; report failures honestly. Patch bump +1 relative to whatever `main`
   then carries, CHANGELOG at top, one install at a time. **Install state at
   S15 close:** USER-scope `dotnet-standards 0.3.11` from marketplace
   `dotnet-standards-dev` (directory source = this checkout), Skills (11),
   registry healthy (no S13b-style vanishing this session). Check
   `installed_plugins.json` before deleting ANY cached version dir.
3. Artifact language English; talk to the user in Vietnamese.
4. End: commit per protocol (lane branch, feat commit, merge into main — expect
   mid-session `main` movement; conflict rule: keep both CHANGELOG entries,
   renumber yours above theirs). Rewrite THIS file and
   `docs/next-session-prompt-B.md`, carrying the Lane log.

## Lane log

- **S15 (dotnet-testing, 2026-07-27) — shipped v0.3.11.** Verdicts: P1 MERGE,
  P2 MERGE, P3 MERGE, P3b MERGE (user-directed flow-test addition), P4 MERGE;
  final consistency pass PASS (note 2 applied — package-table row alignment;
  notes 1 & 3 recorded). Research variant: NO exemplars named; the
  `BE-Ops-Service/tests` scaffold contributed exactly two facts (naming layout,
  coverlet). User-approved stack table at session start (delegation), then ~20
  recorded delegation calls through the pieces (sentinel `partial Program`,
  Verify OUT, `tests/<X>.{Unit,Integration}Tests` law, HttpMessageHandler-only,
  H1 drop, IMapper substitutable, description trigger trim, MockQueryable
  routed-not-adopted, single-failing-rule carve-out example, UseEnvironment
  kept, TestUsers constant kept, ReadAsync beside SeedAsync, background-worker
  carve-out, transition-route grammar, inline request swap, TOCs added, AP2
  real-keys reversal, package-table split, Overview added / Not-this-skill
  skipped).
- S15 arbiter catches (the loop worked): BOTH authors wrote the superseded
  entity-typed validator assertion — mirror of S13b's error, root cause the
  STALE line `module-feature/references/validation-rules.md:322` ("every
  message… `T` is the entity") — **flagged to Lane A, not fixed here**; both
  authors omitted `auth-and-security` from `Not for:` while triggering on
  "test authentication handler"; author B promoted Moq from not-chosen to
  "banned" (modality); author A's flow example drifted `OrderStatus` against
  P2's `FulfilmentStatus`; author A's AP5 GOOD block was a unit test that
  cannot run under P2's own projecting-read ruling.
- S15 verified mechanism facts (targeted lookups, announced):
  `VerifyJwtUserMiddleware` reads the ESTABLISHED principal (test auth handler
  viable) and re-checks the user row in the DB (NotFound/Blocked/ApplicationId
  — the S13 census, so JWT-user tests must seed their row); AutoMapper pin is
  12.0.1 (single-arg `MapperConfiguration`); Respawn excludes nothing by
  default (`TablesToIgnore` for `__EFMigrationsHistory` is mandatory).
- S15 unruled candidates banked for the rubrics: message-keys' "Which form
  where" table has no row for a selector-bearing entity-typed service throw
  (`Messages<Order>.AlreadyExist(x => x.Code)` — corpus shape, table gap); the
  `validation-rules.md:322` drift above; kit anti-example bank (fixture
  `RemoveAll`+`AddDbContext` shape; kit's own `CreateInMemoryDb()` against its
  own ban; Moq-syntax anti-pattern illustration; Verify snapshot section;
  WireMock decision-guide row).
- S15 process events: user directive mid-session — main session stops drafting;
  `skill-writer-a` agent + `three-way-skill-loop` skill created and BOTH
  hot-loaded without restart (corrects the scope of the S13b restart lesson:
  it applies to plugin skill rosters in subagents, not agent-type definitions).
  Memory updated (`author-a-delegated`). Roadmap row added by user direction:
  `dotnet-test-report` hook (Group B, post-rubrics). Mid-session `main`
  movement absorbed twice (router v0.3.10; `f2d60c0` auth→Lane A reassignment —
  collision with the stale B4 opener averted because this session built
  `dotnet-testing`).
- **Carried forward for the rubrics** (from earlier sessions): S13's four
  unruled error-handling candidates (CHANGELOG 0.3.4); S13b's message-keys
  candidates (hardcoded const key third mechanism; `Action(MessagesType.X)`
  bypass; validator dual-form census); S12 anti-example list (superseded lane
  file @ 6848e17 + CHANGELOG 0.3.2); `distributed-lock` roadmap row (S16+).
- **S13b (message-keys) — shipped v0.3.7.** Full entry preserved in
  `docs/next-session-prompt-B.md` @ commit `77ed0a3` and CHANGELOG 0.3.7;
  headline rulings live in SETTLED above.
