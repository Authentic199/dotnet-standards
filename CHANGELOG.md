# Changelog

All notable changes to `dotnet-standards`.

Versions follow semantic versioning. The version in `.claude-plugin/plugin.json`
is the only signal an installed copy is stale, so it is bumped whenever
components change materially — not only on releases.

---

## [0.3.10] — S15 (Lane C), 2026-07-27

### Added
- **`choosing-a-dotnet-skill`** — the ROUTER (mechanism D from brainstorm §3),
  Lane C's deliverable under the three-way authoring process (verdicts: P1
  MERGE, P2 MERGE, P3 MERGE; arbiter final consistency pass PASS — one defect
  fixed: a falsely-justified H1 removed, 8/9 siblings open at `##`). A single
  decision-table SKILL.md, no `references/`. Session rulings:
  - Two-user goal: sibling disambiguation AND process-phase coverage — the
    router fires when Superpowers brainstorming/plan-writing/subagent-dispatch
    runs on a generic .NET task whose trigger nouns have not surfaced yet.
  - Description uses meta-shaped triggers (uncertainty situations, plan/spec/
    subagent-prompt authoring), not domain nouns — deliberate §5-letter
    tension, spirit-compliant, so the router never competes with the nine
    siblings it routes to. Collapsed two-entry `Not for:` (process layer →
    Superpowers; confidently matched → load directly) — vacuously satisfies
    "name every excluded sibling" since the router owns no content area.
  - Routes ONLY to skills existing on `main` (nine at ship time). One
    `## Not yet covered` section, ten uniform topic-noun rows, two populations
    undistinguished in the artifact: six names dangled by shipped `Not for:`
    lists printed as reservations (`auth-and-security`, `observability`,
    `background-worker`, `automapper-mapping`, `mediatr-messaging`,
    `project-scaffolding` — "nothing to load"), four name-less roadmap areas
    (testing, HTTP resilience, domain modelling, modern C#).
  - The body-scoped obligation (user-approved "must"): each spec/plan/subagent
    step touching an area a SHIPPED skill owns must name that skill inside the
    step; uncovered areas have nothing to name (permission clause: say so in
    the step if it helps the plan). Deliberately absent from the description —
    unscopable at that tier.
  - Single entry-point per row, never sequences; whole-feature conditional:
    `facade-module-architecture` if the module does not exist yet, else
    `module-feature` (corrected against fma's literal description).
  - Disambiguation table may source tokens from skill BODIES
    (`ConcurrencySettings`, `Repository<T>()`) — it exists for tokens
    description-matching cannot resolve; base-map rows stay strictly sparser
    than their target descriptions (compose, never amplify).
  - No section-heading pointers into targets (headings drift). Disclaimer
    principle: a `Not for:` entry is a disclaimer, not an ownership assignment;
    when two shipped pointers differ in grain, the finer one from the area's
    owner governs (`[HasPermission]` usage → `api-surface`, internals →
    not yet covered).
  - `dotnet-testing` merge-time swap pre-written: delete the Testing row from
    Not yet covered; append base-map row "Writing or changing tests: unit,
    integration, fixtures, test doubles → dotnet-testing"; extend the order
    note to "… → capabilities → tests". Three mechanical edits.

---

## [0.3.9] — S9 (Lane A), 2026-07-27

### Added
- **`ef-core-data-access`** — Lane A's second deliverable, built under the
  three-way authoring process (verdicts: P1 NEITHER→merge twice — re-arbitrated
  cold after a parent-session restart so `skill-creator` could be invoked live;
  P2–P5 MERGE). The data-access gateway: repository-over-EF-Core with the
  wrapper as the law (`IRepositoryWrapper.Repository<T>()`, scoped, the kit's
  no-wrapper stance overruled by the codebase), save-per-operation with wrapper
  transactions (`catch (Exception)` + rollback + rethrow — 22/22 real sites),
  `Find(isAsNoTracking:)` as the query gate, thin `ApplicationDbContext`
  (no DbSets, citext extension, global DateTimeOffset→UTC converter),
  options-first `DatabaseSettings` (SqlSettings only; Redis/ES routed to
  Lane C), provider switch with `MigrationsAssembly($"Migrators.{provider}")`,
  the migrations workflow (dev: `dotnet ef` with `-p`/`-s`/`-c`; prod:
  `UseAutoMigration` at boot), seeding (`IDataSeedContributor` as the only
  public seam; bail-out and reconcile idempotency strategies, order between
  contributors not guaranteed), entities & configurations (sequential-GUID
  `BaseEntity`, one-file co-location, `HasBaseEntity().UnderscoreTable()`
  opener, explicit FK pairs, `OnDelete` as a decision question with census
  Cascade 27 / Restrict 14 / SetNull 3), and
  `references/query-conventions.md` (the QueryContainer search shape,
  operator grammar verified against the parser, the get shape, and the honest
  five-round-trip cost of the canonical search chain).

### Rulings recorded for reuse
- `ICode`/`HasCode` ruled OUT of the skill entirely — citext taught via
  `HasCitextUnique` directly; hand-rolled citext-unique without the interface
  is explicitly allowed.
- Collection navigations: non-nullable `ICollection<X> Xs { get; set; } =
  default!`; reference navigations stay nullable.
- Single-style opener: `HasBaseEntity().UnderscoreTable()` (7:4 census, the
  minority order confined to one module).
- `GetByIdAsync(params object[])` does not exist in this skill's world
  (ops-service repository shape is the source of truth).
- Entity-static `Expression` members and entity-file AutoMapper Profiles are
  omitted entirely (module-feature's Expressions/ and the future
  automapper-mapping own them).
- Get-single is taught without `Include` — `ProjectTo` ignores it; the real
  call site's Include chain is a labelled anti-example.
- Anti-examples labelled for future review rubrics (real paths in the lane
  log): transaction-ct drops (12/29 sites), BrandService rollback-cleanup-wrap,
  sync `Any()` in async seeders, the three `entities.Any()` probes in the
  query helpers, ApplyFilter's silent catch + Console.WriteLine, sync
  `Count()` dropping its ct in ToPagedListAsync, QueryContainer.Validate
  blaming PageSize for Current, response DTOs inheriting `BaseEntity<Guid>`.

---

## [0.3.8] — S14 (Lane C), 2026-07-27

### Added
- **`distributed-lock`** — Lane C's third deliverable, built under the three-way
  authoring process (A/B independent drafts per piece; `skill-arbiter` invoked
  the installed `skill-creator` live after a parent-session restart; verdicts:
  P1 MERGE, P2–P4 MERGE B-dominant; user adjudicated through P3, standing
  delegation applied from P4). Owns distributed mutual exclusion: the
  `ConcurrencyHandlers` capability (`IConcurrencyHandler`, two `LockedAsync`
  overloads, `ConcurrencyHandlerOptions`, `ConcurrencySettings`), provider
  choice (SemaphoreSlim honestly framed single-instance-only vs RedLock — every
  production call site passes RedLock explicitly), lock-key discipline
  (`{Noun}:{id}`, private static helpers, no central factory, no CachePrefix),
  the ExpiryTime/WaitTime/RetryTime doctrine, and `LockedException` (423) as the
  cited contract routed to `error-handling`. Decision-layer body plus two
  references (`implementation.md` with the full scaffold bodies;
  `usage-patterns.md` with the three production patterns).
- **Rulings recorded for reuse:** the third in-memory provider option is
  scrubbed entirely (enum member, dispatch branch, package — no mention
  anywhere); the scaffold reads the EXTRACTED `RedisSettings` section owned by
  `distributed-caching` (deviation from the canonical `DatabaseSettings`
  nesting; that section + `Required()` + the `LockedException` family are STOP
  prerequisites); `ConcurrencySettings.Provider` is dead config — honest note,
  no normalization, no invented fallback; the semaphore-registry cleanup race
  (`TryRemove`/`GetOrAdd`) is an honest note, canonical code kept, not a
  BAD/GOOD pair; authorized normalizations: settings filename typo, Vietnamese →
  English XML docs, single-key `RedLock` → `RedLockAsync` rename, and — in
  usage-patterns only — the Pattern 3 catch filter gains
  `and not LockedException` (the canonical filter compensates work that never
  started and downgrades a retryable 423 to a 500); the ExpiryTime mid-work
  release is taught as the canonical's documented intent with an explicit
  non-assertion about client auto-renewal (unverified for the pinned version);
  placement asymmetry named once (lock at `Common/Services/`, cache at
  `Common/` — both canonical, don't move existing folders); lock keys may carry
  two ids when the guarded resource is the pair; drift noted once (one canonical
  call site passes a bare Guid key).

## [0.3.7] — S13b (Lane B), 2026-07-27

### Added
- **`message-keys`** — third Lane B deliverable, built under the three-way
  authoring process (A/B independent drafts per piece; `skill-arbiter` ran with a
  LIVE skill-creator invocation after a mid-session plugin install plus parent
  session restart; verdicts: P1 MERGE, P2 NEITHER with arbiter-corrected
  doctrine, P3 MERGE). Owns the `Messages<T>` key grammar: key anatomy
  (`Mes.{Module}.{Rest}`), the success/action helper family, the `Action`
  overload family and its no-default trap, the 15-member `MessagesType` matrix,
  `[MessageDisplay]`, and which form is used where. Decision-layer body plus
  `references/key-grammar.md`.
- Session rulings recorded:
  - Single-style doctrine: `Messages<T>.X(selector)` is THE validator-message
    law; the `WithMessage(MessagesType.X)` extension is legacy — recognised when
    reading, never written new.
  - `[MessageDisplay]` and the selector lambda are complementary, not competing:
    every request class carries `[MessageDisplay(nameof(Entity))]`; validator
    messages are request-typed; cross-entity existence checks entity-typed.
  - Two generics, two jobs (arbiter-discovered against BOTH authors' drafts):
    requests type validator messages, entities type outcome messages —
    corpus-verified (zero request-typed success calls in either project; the
    written convention's request-typed worked example ruled drift against its
    own repo's code).
  - Growth-by-reuse: the `MessagesType` enum is closed; the action family may
    grow — an unnamed action starts as `Action("X", true)`, and promotion to a
    dedicated facade helper is permitted (not required) once the action recurs
    across modules (Approve/Reject/Cancel named as typical).
  - The enum is authoritative at 15 members; the written convention's 14-item
    list is stale. Overload coverage is non-uniform and a missing shape is
    final (the absence pattern tracks the enum's own resource/value split).
  - Older entity-typed validation of a request's own properties acknowledged in
    one clause as superseded.
  - Sanctioned anti-example (generic form only): a request class without
    `[MessageDisplay]` leaks its type name into every key.

## [0.3.6] — S11 (Lane C), 2026-07-26

### Changed
- **`distributed-caching`** — `references/usage-patterns.md` now names the real search
  facade type `IElasticSearchWrapper` in the pipeline-handoff producer example instead
  of the sanitized `ISearchWrapper`, per the cross-skill vocabulary ruling made when
  `elasticsearch-search` shipped.

## [0.3.5] — S11 (Lane C), 2026-07-26

### Added
- **`elasticsearch-search`** — Lane C's second deliverable, built under the
  three-way authoring process (A/B independent drafts per piece, `skill-arbiter`
  file-verified verdicts: P1–P4 all MERGE; user adjudication through P2, delegated
  auto-approval from P3 onward). Owns the Elasticsearch facade
  (`IElasticSearchWrapper`, `ElasticSearchRepositoryBase<T>`, `AddElasticsearch`,
  `IndexSettingsMapper<T>`) and the `ElkEntities/` convention — `Elk*` documents,
  never index a DB entity, the root-vs-embedded document distinction, projection
  profiles, query and re-index patterns. Decision-layer body plus two references
  (`implementation.md` with the full corrected scaffold bodies; `usage-patterns.md`
  with document authoring, read/write-back patterns and the single authorized
  blocking-pair anti-example).
- Session rulings recorded: `ElasticsearchSettings` stays nested in `DatabaseSettings`
  (deliberate divergence from the Redis extraction); two-folder facade anatomy taught
  as-is; `ElkBaseEntity` normalized into the facade; canonical registration split kept
  (wrapper registered in the persistence facade's `Startup`); `Querry` spellings
  corrected with an honest note; blocking `Search(out)`/`BulkAll` omitted from the
  scaffold and treated as the sole BAD/GOOD pair.

## [0.3.4] — S13 (Lane B), 2026-07-26

### Added
- **`error-handling`** — fifth skill, second Lane B deliverable, built under the
  three-way authoring process (A/B independent drafts per piece, `skill-arbiter`
  file-verified verdicts: P1 MERGE, P2 MERGE, P3 MERGE B-dominant; user delegated
  adjudication after P1). Owns: when to throw which of the four sealed exceptions,
  catch/bubble/wrap doctrine, the exception middleware's envelope contract
  (`ErrorResultWrapper`), and the growth sanction's boundary. Decision-layer body
  plus one reference (`middleware-behavior.md`: full handler walkthrough, the three
  shaping paths, diagnostics fields, `HiddenProperties` redaction mechanics,
  pipeline position and its consequences, the dedicated-catch anti-example, and a
  symptom→cause troubleshooting table).
- Session rulings recorded: **business not-found = `BadRequestException` 400** (no
  `NotFoundException`, ever; 404 stays routing's answer per `{id:guid}`);
  current-principal-not-found → `UnAuthorizedException` 401; `ForbiddenException`
  documented honestly (zero throw sites — real 403s are bare authorization
  short-circuits, never enveloped; reserved leaf for a domain-decided enveloped
  403); **bubble by default** — wrap into `InternalServerException(message, inner)`
  only when the catch adds context, `(ex.Message, ex)` is not house style; growth
  sanction boundary — a status-pinning sealed leaf is free, a payload-carrying
  exception demanding middleware compensation is outside the sanction; the
  "middleware is the only producer" doctrine scoped to *thrown* exceptions, with
  the invalid-model-state `{ message }` carve-out named (Web's
  `InvalidModelStateResponseFactory`, consistent with the architecture skill's
  Web-owns line; validator rules → `module-feature`).
- Labelled anti-examples (user-confirmed): a leaf constructor that fails to pin
  `StatusCode` (latent status-0 defect); the middleware's dedicated file-upload
  `catch` that compensates but never writes a response — framed as a defect of the
  shared shape (present, identical, in both reference codebases).
- Roadmap: **`distributed-lock` row added** at S13's open by user direction
  (lane-ownership exception) — it owns `ConcurrencyHandlers` and `LockedException`
  (423); `error-handling` cites 423 only as the growth worked example and routes
  lock semantics there.

### Changed
- Merge-time alignment with S8 (which landed on `main` mid-session): every
  `cqrs-feature-slice` route in `error-handling` (description `Not for:`, body
  carve-out, `Not this skill`, reference) ships as **`module-feature`**, per the
  S8 rename ruling.

## [0.3.3] — S8 (Lane A), 2026-07-26

### Added
- **`module-feature`** — fourth skill, Lane A's refounding of the `cqrs-feature-slice`
  charter under its new name (user ruling: MediatR is in-process messaging, not CQRS,
  so the old name lied). Built under the three-way authoring process (A/B independent
  drafts per piece, `skill-arbiter` file-verified verdicts: P0–P6 all MERGE; user
  adjudication through P3, then blanket delegation). Owns writing one feature inside a
  module: the service file (one file interface+implementation, `IScopedService` on the
  interface, suffix partials, `Services/` purity with two authorized dumping-ground
  inventories), request/response files (one-file law, tiers, facade bases),
  `<X>Validation.cs` (IsExist… predicates / ThrowIf… guards, symmetric boundary), and
  thin MediatR envelopes (`internal sealed`, handler-delegates-to-service absolute).
  Decision-layer body (282 lines) + four references (`service-growth.md`,
  `request-response-families.md`, `validation-rules.md`, `mediatr-envelopes.md`).
- Session rulings recorded: ct mandatory on every service operation (`= default`,
  last parameter; private helpers required-no-default); XML `<summary>` law (English,
  no TODOs); response tier suffix naming with strict chain; `DeleteRange<X>Request`
  standard; Expressions/-mandatory for business-computed members; `IsExist…` prefix
  law; envelope visibility law with the `internal`-blocks-Web (not module-vs-module)
  mechanism; `GetByIdAsync`-token trap documented.

### Changed
- **Rename ripple:** `cqrs-feature-slice` → `module-feature` across
  `facade-module-architecture` (description + Not-this-skill) and `api-surface`
  (description Not-for, Overview, body, `request-response-dtos.md`) — routing text
  otherwise untouched; api-surface's "validation rules" hand-back preserved.
- Cross-lane alignment: `module-feature`'s request/response piece amended to the
  shipped api-surface DTO chain law (base request Profile only when customized);
  open `[MessageDisplay]` vs `Messages<T>`-lambda conflict logged for `message-keys`.

## [0.3.2] — S12 (Lane B), 2026-07-26

### Added
- **`api-surface`** — third skill, first Lane B deliverable, built under the three-way
  authoring process (A/B independent drafts per piece, `skill-arbiter` file-verified
  verdicts: P1 MERGE, P2 MERGE + user-approved errata, P3–P5 MERGE, user adjudication
  throughout). Claims routes, request/response DTO base-class chains, versioning stance
  (**none**), Swashbuckle/OpenAPI, and controller endpoint-writing conventions.
  Decision-layer body plus three references (`endpoint-anatomy.md` with two worked
  controllers and two authorized anti-examples; `request-response-dtos.md` with both
  DTO chains, the conditional base-Profile rule and pagination contracts;
  `openapi-swashbuckle.md` with the full facade walkthrough and a debugging table).
- Session rulings recorded: expression-bodied endpoints only (body-style hand-off from
  `facade-module-architecture` explicitly claimed); signature wrapping counts every
  parameter including the token; strict binding sources on new endpoints; `{id:guid}`
  always; suffix-partial base-list law with three named anti-patterns; request
  inheritance law (base-first, lookup before defining) and response ladder rooted at
  `BaseEntity`/`ElkBaseEntity`; base request `Profile` only when customized
  (`.IncludeAllDerived()`), response base rungs always carry it;
  `PaginationResponse` as the only list envelope, `MoreInfo` = companion computed by
  the same search; `[HasPermission]` single constructor, three call shapes, positional
  trap documented; `Messages<T>` text conventions assigned to a **dedicated future
  `message-keys` skill** (neither api-surface nor error-handling).

## [0.3.1] — S10 (Lane C), 2026-07-26

### Added
- **`distributed-caching`** — second skill, first Lane C deliverable, built under the
  three-way authoring process (A/B independent drafts per piece, `skill-arbiter`
  file-verified verdicts: P1 MERGE, P2 NEITHER-redraft, P3 MERGE, P4 MERGE, user
  adjudication throughout). Canonical source: the user-designated
  `Facades/Common/RedisCaches` cache exemplar (RedisCache only). Decision-layer body
  plus two references (`implementation.md` scaffold with pre-scaffold guard and two
  STOP prerequisites; `usage-patterns.md` with the handoff read-once and cache-aside
  patterns, one authorized anti-example, and the HybridCache
  considered-not-adopted ruling).
- Session rulings recorded: cache facade taught at `Facades/Common/RedisCaches/`
  (scaffold-if-missing); normalized anatomy (`internal` Startup, Options four calls,
  `RedisSettings` extracted from `DatabaseSettings` per settings-follow-their-service,
  entry point `AddRedisCache`); named key legal for singleton rows; Redis queue
  scrubbed from the skill entirely; canonical `IValidatableObject` +
  `validationContext.Required()` validation with the helper as a STOP prerequisite.

## [0.3.0] — S7b, 2026-07-26

### Changed
- **`facade-module-architecture` rebuilt from scratch** under the three-way authoring
  process (independent A/B drafts, `skill-arbiter` verdicts with file-verified reasons,
  user adjudication per piece). Evidence base re-founded on the user's per-area canonical
  designations (`ops-service` as the base project for solution/Core/facade-startup/
  composition-root conventions; `apsp-backend` as the production source for modules and
  controllers; `be-booking` for one anti-example only). All six recorded defects of the
  0.2.0 version are fixed, plus rulings made this session: two lifetime markers (no
  singleton marker), four sealed exceptions with no serialization ceremony on new ones,
  unified suffix-partial law (base list only in the suffix-less core file — services and
  controllers alike), `Enums/` unconditional, `<X>Validation.cs` naming, settings follow
  their service, `Facades/Common` fractal growth with reach-not-size placement,
  Expressions write-once, no `Mappings/` folder.
- **New shape:** a ~300-line decision-layer body plus six verbatim-approved
  `references/` files (`solution-layout`, `core-contracts`, `facades`, `modules`,
  `composition-root`, `web-controllers`), replacing the three 0.2.0 references.
  Nine authorized anti-examples live in the references, none in the body.
- **Description voice settled** by the arbiter loading Anthropic's official
  `skill-creator`: third person, under 100 words, trigger-noun "pushy", `Not for:`
  routing list. `docs/02-repo-structure.md` §5 rewritten to match; the shipped
  description is the reference example.

## [Unreleased] — process change, 2026-07-26

**No version bump, deliberately.** No plugin component changed — the version is the only
signal an installed copy is stale, and bumping it would mint a fresh 41 MB cache directory
for a docs-and-tooling change. Everything here is process.

### Added
- `.claude/agents/skill-writer-sp.md` and `.claude/agents/skill-arbiter.md` — the second and
  third authors in the new three-way skill authoring process. **Project tooling, not plugin
  content**: they live in `.claude/agents/`, never in the plugin's `agents/`, because triage
  settled that exactly one agent ships (`ef-core-specialist`, B18). Neither agent may write a
  file; both return draft text so nothing is written before the user approves. **Amended same
  day at the user's direction: all three participants — main session, writer, arbiter — read
  the user-named exemplar files in `reference/projects/` directly, with equal access.** The
  first design fed the agents only material pre-digested by the main session, which made every
  draft inherit one reading of the code and left the arbiter judging two drafts that shared
  one pair of eyes. Equal capability, differing only in loaded methodology; the reading
  discipline (user names the files, widening requires asking, no bulk scans, R7, Bash not
  Glob) binds all three identically.
- `docs/03-session-roadmap.md` — the **three-way authoring process**, mandatory from S7b
  onward. Main session drafts A from the repo's own rules; `skill-writer-sp` drafts B from
  Superpowers' `writing-skills`; `skill-arbiter` decides using Anthropic's official
  `skill-creator`. Piece by piece, explained in Vietnamese, user-approved before any write.

### Changed
- **R7 canonical source re-designated: `apsp-backend`, not `ops-service`.** The user
  identified `ops-service` as a base project rather than production. `apsp-backend`
  **confirms** the Facade/Module architecture — identical project graph, identical `Core`
  shape, identical two-axis split, identical 13-file configuration load order — so **Q1's
  answer stands**. Six details change, and `facade-module-architecture` is queued for rebuild
  in S7b.
- `docs/02-repo-structure.md` §5 — the description **voice** is now marked contested and
  explicitly undecided. Second person (§5) versus third person (the user's own
  `skill-creator` convention). Assigned to the arbiter, to be settled with reasons *after*
  drafts exist. Anti-triggers remain the settled part of the rule.

### Known issues in the shipped skill, pending the S7b rebuild
- `references/dependency-injection.md` documents two DI marker interfaces; `apsp-backend` has
  **three** (`ISingletonService` is missing).
- The principle *"`Core` holds primitives only"* is **wrong**. `Core` also holds an exception
  hierarchy and result wrappers.
- The one shipped anti-example — target-framework drift — **does not reproduce in
  `apsp-backend`**, where every project including both test projects targets `net7.0`. It was
  an `ops-service`-only defect and loses its standing unless re-authorised on new evidence.

### Notes
- **Definitions do not load in the session that creates them.** A newly written agent type is
  undispatchable until restart — the same constraint S6 measured for hooks, now confirmed to
  generalise to project-level agents. This is why S7 stopped at defining the process rather
  than exercising it.
- `apsp-backend` ships **eleven skills of its own**, now designated the highest-tier
  `from-my-code` source. `dotnet-standards` generalises an existing personal convention rather
  than writing on a blank page.
- **S8's blocking question is answered by the user's own written rule:** MediatR is
  *in-process messaging, not CQRS read/write separation*. The `cqrs-feature-slice` gateway as
  named describes a pipeline the user does not run.

---

## [0.2.0] — 2026-07-26

The first knowledge session. Q1 — open since S0 — is answered from real code, and
the first skill ships on top of the answer.

### Q1 resolved — the architecture has a name

The architecture is a **three-project chain** — `Core` → `Infrastructure` → `Web`,
with `Migrators.<Provider>` between the last two — whose `Infrastructure` project is
split on **two axes**: `Facades/` for technical capabilities and `Modules/` for
business ones. Every facade wires itself through a `Startup.cs` exposing
`AddX()`/`UseX()`, composed into a single flat fluent chain.

**It is not Clean Architecture and not Vertical Slice Architecture.** `Core` holds no
entities, business logic lives inside `Infrastructure`, and there is no `Domain` or
`Application` project — so it is not "close to" Clean Architecture either. Three
skipped triage rows (A03, A08, A35) are confirmed by this answer rather than reopened.

### Added
- `skills/facade-module-architecture/SKILL.md` — the first skill in the plugin, and
  the gateway that answers "where does this file belong?". Its description carries
  **anti-triggers** naming six sibling skills to use instead.
- `skills/facade-module-architecture/references/solution-layout.md` — solution and
  build files, package-version discipline, and the six solution-hygiene checks
  (TRIAGE A28 + D07 + B28).
- `skills/facade-module-architecture/references/configuration-and-options.md` — the
  Options pattern, startup validation, and the per-capability configuration-file
  convention (TRIAGE A10).
- `skills/facade-module-architecture/references/dependency-injection.md` — lifetimes,
  the captive-dependency bug, keyed services, and where registration lives
  (TRIAGE A14).

### Changed
- Gateway renamed **`solution-architecture ⚠️` → `facade-module-architecture`**
  across TRIAGE rows A10, A14, A28, B28 and D07, and in `00-brainstorm.md` §4.
  Historical decision-log entries keep the old name — the log is append-only.
- `00-brainstorm.md` §8: **Q1 closed**. Roadmap S7 row marked complete.
- TRIAGE **A05** and **A33**: setup sites traced from named-but-unverified to
  concrete paths, and confirmed to exist. Traced only — neither is distilled here.

### Fixed
- **TRIAGE A33 carried a wrong configuration path.** The row claimed Serilog is
  configured from a `Serilog` section in `appsettings*.json`. No such section exists;
  configuration is a strongly-typed POCO bound from a per-capability `logger.json`
  and applied imperatively in code. Corrected in the row and logged.

### Notes
- **One anti-example ships**, adjudicated by the user rather than assumed:
  target-framework drift. Four further divergences from the kit (no central package
  management, no `global.json`, a rules-free `.editorconfig`, classic `.sln`) are
  recorded as **observed conventions — neither endorsed nor faulted**, per R7's
  "label, don't blend".
- All version-specific content is dated **2026-07-26**. The stack targets **.NET 8**,
  not the kit's .NET 10. Next R10 trigger: **.NET 11 GA, 2026-11-10**.
- `NOTICE` unchanged — no new *kind* of artifact carries kit material; the three
  `references/` files are derived components already covered.

---

## [0.1.0] — 2026-07-26

The scaffold. No .NET knowledge ships in this version; this is the plumbing that
makes everything else installable.

### Added
- `.claude-plugin/plugin.json` and `.claude-plugin/marketplace.json` — the plugin
  manifest and the local development marketplace (`dotnet-standards-dev`).
- `hooks/post-edit-format` — the one hook that survived triage. Runs
  `dotnet format` scoped to the nearest `.csproj` after every `.cs` edit.
  Extensionless by necessity: Claude Code on Windows prepends `bash` to any
  command containing `.sh`.
- `hooks/run-hook.cmd` — polyglot CMD/POSIX wrapper. Copied in pattern from
  Superpowers, never referenced across plugins.
- `hooks/hooks.json` — rebuilt manifest, one `PostToolUse` entry. Auto-loaded, so
  it is deliberately **not** declared under `plugin.json`'s `manifest.hooks`.
- `hooks/README.md` — the three-kinds hook taxonomy, the Windows cost, and the
  rule that a hook may ship only if its silent absence is benign.
- `NOTICE` — two MIT attributions: `codewithmukesh/dotnet-claude-kit` at the
  pinned commit, and the wrapper pattern from Superpowers.
- Empty `skills/` and `agents/` directories for the components S7–S8 will build.

### Fixed
- `post-edit-format` — the reference kit's `dotnet format "$PROJECT" --include "$FILE"` call
  formats **nothing** on Windows, silently. Two independent causes, both measured on
  .NET SDK 10.0.301: an absolute project path with forward slashes triggers
  *"Skipping referenced project"*, and `--include` only matches paths relative to
  the current working directory. Now runs from the project directory with
  relative paths. See `hooks/README.md`.
- `post-edit-format` — the project walk now recognises `.slnx`, the `dotnet new sln`
  default since .NET 10, alongside `.sln`.

### Verified
- **Live confirmation, closing the one gap S6 could not close itself.** S6 proved
  the hook worked by running `run-hook.cmd` directly; it could not prove the
  hook fires *inside a live Claude Code session*, because the session that
  installs a plugin predates its hooks. Confirmed 2026-07-26 in a separate test
  project, project-scoped install (`--scope local`), fresh session after
  restart: writing a `.cs` file with irregular indentation through Claude's
  Write tool triggered `post-edit-format` and the file came back re-indented
  to 4-space / brace-on-own-line convention. **The hook fires end to end.**

### Notes
- **Installing this plugin copies the whole source directory and ignores
  `.gitignore`** — including `reference/`, which holds the kit clone and the
  author's real projects. First install copied 39 MB against a ~330 KB plugin.
  No exclusion mechanism exists for a `directory` marketplace source. Two
  candidate fixes are recorded in `docs/02-repo-structure.md` §4; neither is
  chosen yet because both change what §1 specifies. Until then, delete
  `reference/` from the cache copy after each install.
- **Install copies, it does not link.** Editing this repository changes nothing in
  the installed plugin until uninstall → install → restart.
- **No `mcpServers` block in `plugin.json`.** `CWM.RoslynNavigator` is kept as an
  externally installed dotnet tool, not bundled. Its install command and
  `.mcp.json` shape become a `references/` file in a later version.
- **No `commands/` directory**, by design — see `README.md`.
- **This plugin's knowledge layer carries dated content.** .NET/C# version
  guidance, breaking-change notes, package versions and commercial-licence
  boundaries all expire. The nearest known expiry is **.NET 11 GA,
  2026-11-10**. Treat a stale "current as of" line in `README.md` as a defect,
  not as cosmetics.
