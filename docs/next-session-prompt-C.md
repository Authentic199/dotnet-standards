# Lane C — Infrastructure Services · Session C2: `elasticsearch-search` (S11)

> Copy everything below the line into a fresh Claude Code session in
> `D:\ALTA\Project\dotnet-standards`. Lane C runs in parallel with lanes A and B —
> read `next-session-prompt.md` (the index) for the parallel protocol, which binds
> this session. Written at the close of S10, 2026-07-26.

---

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever be
modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `ops-service` (reusable base), `apsp-backend` (production),
`be-booking` (anti-example quarry only), `digitalcity-backend` (named canonical for
the cache area in S10; other areas need my designation). Triage (`docs/TRIAGE.md`)
is closed input.

**This is Lane C of three parallel lanes.** You own ONLY
`skills/elasticsearch-search/` and this file. Lane A owns `cqrs-feature-slice`,
`ef-core-data-access`, `domain-modeling`, `modern-csharp`. Lane B owns
`api-surface`, `error-handling`, `auth-and-security`, `observability`. The router,
testing, scaffolding and review rubrics are excluded from all lanes. Refuse and
log anything outside your ownership.

## THE DELIVERABLE — `elasticsearch-search`

**What this skill owns:** the ElasticSearch facade, and the `ElkEntities/`
convention in depth per the S7b roadmap note — Elk-prefixed search documents,
never index a DB entity — index/projection/query conventions.

**Landmarks already on record (verify, do not assume):** the S10 exemplars showed
an `IElasticSearchWrapper` with `Repository<T>()` + `FirstOrDefaultAsync(...)`
call sites, an `ElkBaseEntity` base, and `ElasticsearchSettings` (Nodes, Username,
Password, IndexPrefix, DefaultSize, per-index `MaxResultWindow`) nested inside the
Persistence facade's `DatabaseSettings` — the same nesting `distributed-caching`
normalized away for Redis; expect the settings-placement question to recur and put
it to me. `distributed-caching`'s P4 uses a sanitized `ISearchWrapper` vocabulary
for its fallback examples — align or diverge deliberately, and say which.

**Not this skill:** caching (`distributed-caching`, shipped), distributed locking
(`distributed-lock`, queued in the Lane log), background jobs (`background-worker`,
later in this lane), data access (Lane A), HTTP/auth surface (Lane B).

## THE THREE-WAY PROCESS — MANDATORY

Per piece, not per skill: (1) you explain first in Vietnamese, I comment; (2) you
(author A: loads `docs/02-repo-structure.md` §5 + `docs/00-brainstorm.md` §3 + the
kit's skill format — NOT superpowers:writing-skills) and `skill-writer-sp` (author
B) draft independently, neither writes files; (3) `skill-arbiter` verdicts
A/B/MERGE/NEITHER with file-verified reasons; (4) I approve; only then write.
Both agents exist in `.claude/agents/` and are dispatchable — verify with a ping
before relying on them. Agent prompts must carry: the exemplar list I name, all
relevant settled rulings, and equal-source-access discipline. Announce every agent
use; relay milestones; agents end with `## QUESTIONS`; continue them via
SendMessage. Run agents in the current working directory — no worktree for
subagents (the lane itself follows the index's isolation rule).

**S10 process notes that worked — keep them:** batch the authors' `## QUESTIONS`
and put them to me with consequences BEFORE dispatching the arbiter, so verdicts
land ruling-complete; drafts live in the session scratchpad (never `skills/`)
until I approve; arbiter prompts carry the draft file paths plus every ruling.

## READING DISCIPLINE

I name the exemplars at session start — ask me for the list before reading
anything in `reference/projects/`; never select them yourself. Likely areas I will
name: the ElasticSearch facade folder and `ElkEntities/` module folders — but WAIT
for my list and my canonical-source designation per area (R7; S10 precedent:
per-area designation — digitalcity-backend was named canonical for cache). Widening
= targeted lookup, announced. No bulk scans. Bash find/ls/grep, never Glob, inside
`reference/projects/`. R8: anti-examples are code I point at; ask before
labelling. Sanitize: no project names, no business-domain names, no real paths, no
secrets (Elasticsearch config holds credentials — list file NAMES only, never open
config JSON).

## SETTLED — DO NOT RELITIGATE

- Everything shipped in `facade-module-architecture` v0.3.0 (installed body +
  `references/facades.md`: facade anatomy, Options four calls,
  settings-follow-their-service, reach-not-size) and in `distributed-caching`
  v0.3.1 (its guard/prerequisite/normalization idioms are the house style for
  scaffolding skills — read the installed skill before drafting).
- Description law (`02-repo-structure.md` §5, settled S7b): third person
  `This skill should be used when…`, <100 words, trigger-noun pushy, `Not for:`
  routing list naming every sibling that owns an excluded area. `distributed-caching`
  must appear in this skill's `Not for:` (cache nouns) and vice versa is already
  shipped (search nouns route here).
- The `references/` mechanism: decision-layer body, depth in references with
  conditional pointers; the split goes through the loop.
- S10 rulings that carry forward: internal rule numbers (R7/R8) never appear in
  artifacts; a scaffolding skill states a generic pre-scaffold guard and STOP
  prerequisites rather than asserting what existing folders contain; deviations
  from canonical are marked honestly at the exact spot.
- My stack: Redis, Elasticsearch, Hangfire; MediatR is in-process messaging, not
  CQRS.

## HARD CONSTRAINTS

1. One session, one deliverable: `elasticsearch-search` only. Extra requests → log
   under `## Lane log` and refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new skill
   count; report failures honestly. Follow the index's merge/version protocol
   (patch bump, CHANGELOG at top, one install at a time).
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol, then rewrite THIS file so it opens Lane C's next
   session (`background-worker` — Hangfire conventions; note the Lane log's
   `distributed-lock` insertion decision below), carrying the Lane log forward.

## Lane log

- **S10 (2026-07-26) — shipped `distributed-caching` v0.3.1.** Three-way process
  end to end: 4 pieces × (A/B drafts → arbiter → my approval). Verdicts: P1 MERGE,
  P2 NEITHER (arbiter redraft), P3 MERGE, P4 MERGE. Verified installed:
  `Skills (2)`.
- **S10 rulings recorded for reuse:** cache facade taught at
  `Facades/Common/RedisCaches/` with scaffold-if-missing; normalized anatomy
  (internal Startup, Options four calls, `RedisSettings` extracted out of
  `DatabaseSettings`, entry point `AddRedisCache`); named key
  (`static string CacheKey => typeof(T).Name` on the owning interface) legal for
  singleton rows — the ban targets module-local generic key factories; HybridCache
  considered-not-adopted (.NET 9+ vs canonical .NET 7); canonical
  `IValidatableObject` + `validationContext.Required()` validation with the helper
  as a STOP prerequisite; **Redis queue scrubbed entirely** from the skill per my
  instruction (no mention anywhere, `RedisQueueService` never referenced).
- **S10 — new skill requested: `distributed-lock`.** My ruling during the
  exemplar-naming step: `ConcurrencyHandlers` is NOT part of `distributed-caching`;
  it becomes its own skill named `distributed-lock`. Exemplar (user-named):
  `reference/projects/apsp-backend/src/Infrastructure/Facades/Common/Services/ConcurrencyHandlers`.
  Settled ruling to carry: **drop the `KeyedLocker` option from now on**. Same
  scaffold-if-missing mechanism as `distributed-caching`. **Queue position needs my
  decision at the start of S11 or the session after:** insert `distributed-lock`
  before `background-worker`, or after the original Lane C queue — ask me, one
  question, when the lane next has a free slot.
- **S10 exemplar designation for the cache area (precedent for per-area
  designation):** `digitalcity-backend/src/Infrastructure/Facades/Common/RedisCaches`
  (RedisCache only), plus its consumers in `Modules/DetectHistories/` and
  `Modules/Systems/`, `Persistence/DatabaseSettings.cs`, and
  `Common/Services/JsonSerializerService.cs` — widened by announced targeted
  lookups only.
- **Deferred to a solo session (not Lane C's to fix):** the roadmap/index still
  say "Facades/Cache in one or both projects" for S10 and list ops-service/apsp as
  the only reference projects — superseded by the S10 designations above;
  consolidate when the lane logs fold into `03-session-roadmap.md`.
