> **PRIORITY OVERRIDE — 2026-07-27, S14 close, explicit user direction (recorded
> in the Lane C log; ship-the-lean-plugin-first reorder, see
> `docs/next-session-prompt.md` and `docs/03-session-roadmap.md`).**
> `domain-modeling` and `modern-csharp` are **PENDING** — with
> `ef-core-data-access` v0.3.9 shipped, **Lane A's queue is frozen and the lane
> STOPS**. Do not open the session this file describes. The promoted pair runs
> instead (`dotnet-testing` in Lane B, `choosing-a-dotnet-skill` in Lane C),
> then the four review rubrics. The brief below is kept intact for when the
> user unfreezes the queue.

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever be
modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `BE-Ops-Service` (reusable base), `apsp-backend`
(production, canonical), `BE-service-booking` (anti-example quarry),
`digitalcity-backend` (older project — call-site quarry only). Triage
(`docs/TRIAGE.md`) is closed input.

**This is Lane A of three parallel lanes.** You own ONLY
`skills/domain-modeling/` and this file. Lane A has shipped `module-feature`
(v0.3.3) and `ef-core-data-access` (v0.3.9). Lane B owns `api-surface`
(v0.3.2), `error-handling` (v0.3.4), `message-keys` (v0.3.7),
`auth-and-security` (B4, may be in flight), `observability`. Lane C owns
`distributed-caching` (v0.3.6), `elasticsearch-search` (v0.3.5),
`distributed-lock` (v0.3.8), `background-worker` (S15, may be in flight),
`http-resilience`. The router, testing, scaffolding and review rubrics are
excluded from all lanes. Refuse and log anything outside your ownership.
**Lanes share one working tree: before every commit run `git status` and stage
ONLY your own paths.** Expect mid-session `main` movement (S9 absorbed two
merges mid-flight); conflict rule: keep both CHANGELOG entries, renumber yours
above theirs, align any cross-skill names that changed under you.

## THE DELIVERABLE — `domain-modeling` (next Lane A session)

Provenance `from-kit` (brainstorm §4 row 13): aggregates, value objects, domain
events, invariants — adapted to MY codebase, not copied from the kit. Known
boundary facts already shipped that this skill must fit UNDER, not fight:
`ef-core-data-access` v0.3.9 teaches entities as data + configuration with
"small fluent setters returning `this` are as far as entity behaviour goes
here; anything that makes a decision belongs to domain-modeling" — so THIS
skill owns that routed territory: entity behaviour that makes decisions,
invariant enforcement, and whatever the user rules in from the kit's
aggregate/VO/domain-event material. `Vehicle.cs` imports
`OilChangeHistories.DomainEvents` — real domain events exist in apsp; ask the
user whether they are exemplar or anti-example BEFORE reading them. Expressions/
computed values stay with `module-feature`; mapping with future
`automapper-mapping`; persistence shape with `ef-core-data-access`.
**Consolidation caveat:** the roadmap parks `domain-modeling` in S16+ and a
solo consolidation session may re-order the queue — confirm the deliverable
with the user at session start before anything else.

## THE THREE-WAY PROCESS — MANDATORY

Per piece, not per skill: (1) you explain first in Vietnamese, I comment; (2) you
(author A: loads `docs/02-repo-structure.md` §5 + `docs/00-brainstorm.md` §3 + the
kit's skill format — NOT superpowers:writing-skills) and `skill-writer-sp` (author
B) draft independently, neither writes files; (3) `skill-arbiter` verdicts
A/B/MERGE/NEITHER with file-verified reasons; (4) I approve; only then write.
Both agents exist in `.claude/agents/` and are dispatchable — verify with a ping
before relying on them; the ping doubles as the context-package load; continue
the same agents across pieces via SendMessage. Agent prompts must carry: the
exemplar list I name, all relevant settled rulings, and equal-source-access
discipline. Announce every agent use; relay milestones; agents end with
`## QUESTIONS`. Run agents in the current working directory — no worktree.
**skill-creator is INSTALLED (user scope) and the arbiter MUST invoke it live
(`skill-creator:skill-creator`).** A subagent's skill roster is snapshotted
from the parent session's startup state — a plugin installed mid-session is
invisible to all subagents until the parent session restarts (proven in S13b
AND S9). If the arbiter reports `Unknown skill`, restart the parent session;
do not accept fallbacks. S9 lessons: the arbiter corrected BOTH authors'
central cost claim (2 queries → verified 5) and caught author A teaching a
labelled anti-example as doctrine — but it also once misdescribed a call site
("never rethrows" at BrandService:109; it cleanup-then-wraps) — diff every
factual claim you can check yourself.

## READING DISCIPLINE

I name the exemplars at session start — ask me for the list before reading
anything in `reference/projects/`; never select them yourself. Widening =
targeted lookup, announced (what/why). No bulk scans. Bash find/ls/grep, never
Glob, inside `reference/projects/`. R7: one canonical source per area, I
designate; never average. R8: anti-examples are code I point at; ask before
labelling — BUT the philosophy ruled in S9 binds: canonical code is the
strongest truth EXCEPT where it is a bug — then the skill teaches the correct
form and the bug gets labelled for the future review rubrics. Sanitize: no
project names, no business-domain names (Order/OrderLine/Customer +
FulfilmentStatus is the established sample vocabulary), no real paths, no
secrets. **Standing delegation (S13b, reaffirmed S9):** when you present a
decision WITH a clear recommendation, execute it and report; ask only when
genuinely undecidable. R8 labelling and piece approval stay with the user.

## SETTLED — DO NOT RELITIGATE

- Everything in all NINE shipped skills (read installed bodies + references as
  baseline; `claude plugin details` shows the inventory). Key S9 rulings that
  touch this skill: entity files are entity + configuration + enums ONLY;
  fluent setters `return this` are the entity-behaviour ceiling in
  ef-core-data-access — the decision-making layer routes HERE; ICode/HasCode
  ruled out of the corpus ("treat as nonexistent"); collection navigations
  non-nullable `= default!`, reference navigations nullable; opener
  `HasBaseEntity().UnderscoreTable()` single-style; sequential-GUID identity
  assigned in the BaseEntity constructor; entity-static Expression members are
  anti-example (module-feature's Expressions/ owns computed values).
- Description law (`02-repo-structure.md` §5): third person `This skill should
  be used when…`, <100 words by wc -w (measure it), trigger-noun pushy,
  `Not for:` naming every owning sibling (now nine + future names; forward
  references to unshipped skills are precedented).
- The `references/` mechanism: decision-layer body ≤~300 lines, depth in
  references with conditional "Read X when" pointers; the split goes through
  the loop.
- My stack: Controllers not Minimal API, Swashbuckle, NO API versioning,
  FluentValidation + AutoMapper, PostgreSQL primary (MySql migrator exists).

## HARD CONSTRAINTS

1. One session, one deliverable. Extra requests → log under `## Lane log` and
   refuse.
2. Prove it: `claude plugin validate .` + reinstall + `claude plugin details`
   shows the new skill count; report failures honestly. Version = patch bump
   +1 relative to whatever `main` carries at merge time (S9 entered aiming
   0.3.7 and shipped 0.3.9 — check `plugin.json` AND `marketplace.json`, both
   must match; CHANGELOG entry at top). One install at a time; check
   `installed_plugins.json` before touching ANY cache dir; after reinstall
   delete `reference/` from the new cache copy. **Machine note:** the user
   moves between home and company machines — verify install state and paths
   at session start instead of trusting the previous session's notes.
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol (lane branch `lane-a/domain-modeling`, feat
   commit, merge into main per the conflict rule), then rewrite THIS file so
   it opens Lane A's following session, carrying the Lane log forward.

## Lane log

- **S9 (ef-core-data-access, 2026-07-27, company machine) — shipped v0.3.9.**
  Verdicts: P1 NEITHER→merge (arbitrated TWICE — first pass provenance-tainted
  because skill-creator was installed mid-session and invisible to subagents;
  user ordered a session restart and a cold re-arbitration, which converged on
  the same verdict with better trigger nouns), P2 MERGE, P3 MERGE, P4 MERGE
  (user then ruled ICode out — applied post-verdict), P5 MERGE. User
  adjudicated every piece; standing delegation used for: Include-free
  get-shape, 5-round-trip number kept, malformed-filter clause kept,
  PopulateKeys out, distributed-lock Not-for entry added at ship time (its own
  session shipped mid-S9, activating the S9-P1 deferred ruling).
- S9 exemplars (user-named): BE-Ops-Service `Facades/Persistence/**` (canonical
  repository/DbContext — R7: ops is source of truth for repo shape; apsp's
  `GetByIdAsync(params object[])` erased from the skill's world); apsp entity
  files (Devices, DeviceGroups, RewardPointSetting, Vehicles — canonical
  entities/configurations); apsp `RoleService.SearchAsync/GetAsync` + 
  `PaginationExtension.cs` + `QueryExpressionExtension.cs` (query patterns).
  Migration workflow ruled: prod `UseAutoMigration`; dev
  `dotnet ef migrations add <Name> -p src/Migrators/Migrators.PostgreSql
  -s src/<Web> -c ApplicationDbContext` (+ `database update`).
- S9 census/factual corrections recorded in CHANGELOG 0.3.9: catch form
  `catch (Exception)` 22/22; DeleteBehavior Cascade 27 / Restrict 14 / SetNull
  3; opener 7:4; zero non-Guid `BaseEntity<TId>` usages; zero `CreatedAt`
  overrides; seeding = bail-out OR reconcile (5 real seeders); search chain =
  5 round trips (3 `entities.Any()` probes + page + count); `$null` no
  trailing colon; `$not:$eq:x` real; first-colon-only value split; repeated
  filter keys AND, `$in` OR; `ApplySort` auto-appends `Id descending`.
- S9 anti-examples LABELLED (user-confirmed; real paths; the skill teaches the
  correct form — labels are feed for the future review rubrics):
  (1) 12/29 `BeginTransactionAsync` call sites drop the ct (all apsp);
  (2) apsp `Brands/Services/BrandService.cs:109` — rollback → file-cleanup
  interleaved → wrap-throw with raw `ex.Message`;
  (3) sync `Any()` inside async `SeedAsync` (GeographiesSeeder, AdminUserSeeder);
  (4) three `entities.Any()` probes (QueryExpressionExtension:33/92/163);
  (5) `ApplyFilter` silent catch + `Console.WriteLine` both paths;
  (6) `ToPagedListAsync` sync `Count()` dropping its ct;
  (7) `QueryContainer.Validate` blames PageSize for a bad Current;
  (8) apsp `RoleService.GetAsync:90-91` dead Include chain before ProjectTo;
  (9) response DTOs inheriting `BaseEntity<Guid>` (UserResponse, RoleResponse,
  NotificationResponse, GeographyBaseResponse — BOTH projects; api-surface
  boundary).
  NOT labelled (user ruled allowed/superseded): hand-rolled citext-unique
  without ICode; two validation idioms in DatabaseSettings; DbInitializer
  silent no-op on unreachable DB; unconditional `HasPostgresExtension`;
  ICode drift (moot — ICode ruled out).
- S9 cross-lane events: `message-keys` v0.3.7 (Lane B S13b) AND
  `distributed-lock` v0.3.8 (Lane C S14) both merged into `main` mid-session;
  S9 renumbered to 0.3.9 and added the `distributed-lock` routing entry to its
  own description per the deferred ruling. CLAUDE.md in the tree currently
  belongs to Lane B (B4 opener) — Lane A's file is THIS one. The S13b lane
  file describes S9's scaffold from its own vantage; harmless.
- S9 environment lessons: company machine had NO dotnet-standards install at
  session start (home-machine notes were stale) — S13b later installed
  0.3.7→0.3.8 user-scope from marketplace `dotnet-standards-dev`; S9 updated
  it to 0.3.9 and stripped `reference/` from the cache copy; caches
  0.3.7/0.3.8 left unreferenced in place (S13 precedent). Plugin skills
  installed mid-session are invisible to the running session AND its
  subagents until restart (proven twice).
- Queued for a consolidation/solo session: pagination depth beyond
  query-conventions (none left — shipped in S9); `automapper-mapping` +
  `mediatr-messaging` catalog placement (S8 order, still unbuilt); the
  two-error-shapes divergence (S13, unruled); review-rubric feed = the
  labelled anti-example ledger above + S13's list in its lane file.
- **Carried from S8:** "Services/ is not a dumping ground" must appear in the
  future review rubrics as a checklist item (carry until the rubric sessions
  consume it). S8 anti-example candidates not taken remain listed in the S8
  section of the superseded lane file (git history of this file @ commit
  4fb954d and earlier) and in `module-feature`'s CHANGELOG 0.3.3.
