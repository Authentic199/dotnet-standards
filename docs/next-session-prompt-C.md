# Lane C — Infrastructure Services · Session C3: `distributed-lock` (S14)

> Copy everything below the line into a fresh Claude Code session in
> `D:\ALTA\Project\dotnet-standards`. Lane C runs in parallel with lanes A and B —
> read `next-session-prompt.md` (the index) for the parallel protocol, which binds
> this session. Written at the close of S11, 2026-07-26.

---

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever be
modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `ops-service` (reusable base), `apsp-backend` (production;
canonical for the search area per S11 and for the lock exemplar per S10),
`be-booking` (anti-example quarry only), `digitalcity-backend` (canonical for the
cache area per S10; other areas need my designation). Triage (`docs/TRIAGE.md`) is
closed input.

**This is Lane C of three parallel lanes.** You own ONLY `skills/distributed-lock/`
and this file. Lane A owns `cqrs-feature-slice`, `ef-core-data-access`,
`domain-modeling`, `modern-csharp` (+ `module-feature` seen in flight during S11).
Lane B owns `api-surface` (shipped), `error-handling` (in flight during S11),
`auth-and-security`, `observability`. The router, testing, scaffolding and review
rubrics are excluded from all lanes. Refuse and log anything outside your ownership.

**START IN YOUR OWN WORKTREE.** S11 began on the shared checkout and had to
mid-session evacuate to a worktree because lanes A and B were committing and
switching branches in the same tree simultaneously. Do it first, not mid-session:
`git worktree add ../dotnet-standards-lanec-s14 -b lane-c/distributed-lock main`
and work there; the shared checkout's branch belongs to whoever grabbed it.

## THE DELIVERABLE — `distributed-lock`

**What this skill owns:** distributed mutual exclusion — the `ConcurrencyHandlers`
capability, lock acquisition/expiry, `LockedException`, and the scaffold-if-missing
mechanism for projects that lack the capability (same guard/prerequisite idiom as
`distributed-caching` and `elasticsearch-search`; read both installed skills before
drafting — they are the house style).

**Exemplar (user-named in S10, confirm the list at session start before reading):**
`reference/projects/apsp-backend/src/Infrastructure/Facades/Common/Services/ConcurrencyHandlers`.
Likely adjacent landmarks to put to me as targeted lookups (do NOT open unasked):
call sites of the handlers, `LockedException` in Core's exception family, and the
apsp cache facade's RedLock.net wiring — the S10 ruling that `ConcurrencyHandlers`
is NOT part of `distributed-caching` is settled; the boundary between this skill and
that one must be drawn in both descriptions' `Not for:` lists (the shipped side
already routes lock nouns here).

**Settled ruling to carry: drop the `KeyedLocker` option from now on** — it does not
appear in the skill at all.

**Not this skill:** caching (`distributed-caching`, shipped v0.3.6), search
(`elasticsearch-search`, shipped v0.3.5), background jobs (`background-worker`, next
in this lane), data access (Lane A), HTTP/auth surface (Lane B).

## THE THREE-WAY PROCESS — MANDATORY

Per piece, not per skill: (1) you explain first in Vietnamese, I comment; (2) you
(author A: loads `docs/02-repo-structure.md` §5 + `docs/00-brainstorm.md` §3 + the
kit's skill format — NOT superpowers:writing-skills) and `skill-writer-sp` (author
B) draft independently, neither writes files; (3) `skill-arbiter` verdicts
A/B/MERGE/NEITHER with file-verified reasons; (4) I approve; only then write. Both
agents exist in `.claude/agents/` — verify with a ping before relying on them.
Agent prompts must carry: the exemplar list I name, all relevant settled rulings,
and equal-source-access discipline. Announce every agent use; relay milestones;
agents end with `## QUESTIONS`; continue them via SendMessage. Run agents in the
lane worktree — no nested worktrees for subagents.

**Process notes that worked in S10+S11 — keep them:** batch the authors'
`## QUESTIONS` and put them to me with consequences BEFORE dispatching the arbiter,
so verdicts land ruling-complete; drafts live in the session scratchpad (never
`skills/`) until approval; arbiter prompts carry the draft file paths plus every
ruling, including fresh ones resolved between rounds; while one author runs in the
background, write the other draft and pre-read the next piece's exemplars — the
lane's wall-clock cost is agent latency, not tokens. In S11 I delegated blanket
approval mid-session ("tự động approve theo ý bạn"); do not assume that delegation —
ask, or wait for me to grant it.

## READING DISCIPLINE

I confirm the exemplar list at session start — ask before reading anything in
`reference/projects/`; never select exemplars yourself. Widening = targeted lookup,
announced. No bulk scans. Bash find/ls/grep, never Glob, inside
`reference/projects/`. Anti-examples are code I point at; ask before labelling
(S11 precedent: candidates flagged by authors/arbiter stayed unlabelled — the only
BAD/GOOD pair was the one I authorized). Sanitize: no project names, no
business-domain names, no real paths, no secrets; config JSON holds credentials —
list file NAMES only, never open them.

## SETTLED — DO NOT RELITIGATE

- Everything shipped in `facade-module-architecture` v0.3.0,
  `distributed-caching` v0.3.6 and `elasticsearch-search` v0.3.5 (read the installed
  skills; their guard/prerequisite/deviation idioms are the house style).
- Description law (`02-repo-structure.md` §5): third person
  `This skill should be used when…`, <100 words, trigger-noun pushy, `Not for:`
  routing list naming every sibling that owns an excluded area. Both shipped Lane C
  skills already route lock nouns (`distributed locks, ConcurrencyHandlers,
  LockedException`) to `distributed-lock` — this skill must route back (cache nouns
  → distributed-caching; search nouns → elasticsearch-search; job nouns →
  background-worker).
- The `references/` mechanism: decision-layer body, depth in references with
  conditional pointers; two references files per scaffolding skill is the Lane C
  norm (implementation.md + usage-patterns.md).
- Internal rule numbers never appear in artifacts; a scaffolding skill states a
  generic pre-scaffold guard and STOP prerequisites rather than asserting what
  existing folders contain; deviations from canonical are marked honestly at the
  exact spot; typos and broken error messages in canonical get corrected in the
  scaffold with an honest normalization row (S11 precedent: `Querry`, `nameof(T)`,
  the mapper-scan message).
- My stack: Redis, Elasticsearch, Hangfire; MediatR is in-process messaging, not
  CQRS.

## HARD CONSTRAINTS

1. One session, one deliverable: `distributed-lock` only. Extra requests → log
   under `## Lane log` and refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new skill
   count; report failures honestly. Follow the index's merge/version protocol
   (patch bump +1 vs whatever main carries at merge time, both manifests agree,
   CHANGELOG at top, one install at a time — wait if another lane is mid-install).
   S11 note: installing from the shared checkout ships other lanes' untracked WIP
   into the plugin cache; install from the lane worktree (it has no `reference/`
   either, so the cache copy stays small).
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol, then rewrite THIS file so it opens Lane C's next
   session (`background-worker` — Hangfire conventions), carrying the Lane log
   forward.

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
- **S10 — `distributed-lock` created as its own skill** (ConcurrencyHandlers is NOT
  part of distributed-caching). Exemplar (user-named):
  `reference/projects/apsp-backend/src/Infrastructure/Facades/Common/Services/ConcurrencyHandlers`.
  Drop `KeyedLocker` from now on. Same scaffold-if-missing mechanism.
- **S11 (2026-07-26) — shipped `elasticsearch-search` v0.3.5 + `distributed-caching`
  v0.3.6 patch.** Queue ruling: `distributed-lock` INSERTED BEFORE
  `background-worker` (user decision, opening of S11) — hence this session. Three-way
  process, 4 pieces, verdicts all MERGE; user granted blanket auto-approval after P2.
  Exemplars (user-named): `Facades/Persistence/ElasticSearch/` +
  `Facades/ElasticSearch/` + `Modules/Vouchers/ElkEntities/` in apsp-backend; widened
  by announced targeted lookups (ElkBaseEntity, DatabaseSettings, wrapper DI
  registration, composition chain, one write-back handler, one read call site).
- **S11 rulings recorded for reuse:** `ElasticsearchSettings` stays NESTED in
  `DatabaseSettings` (deliberate divergence from the Redis extraction — per-area
  fidelity beats cross-skill symmetry when I say so); two-folder facade anatomy
  taught as-is; `ElkBaseEntity` normalized into `Facades/ElasticSearch/`; canonical
  registration split kept (wrapper scoped in the persistence facade's `Startup`,
  beside `IRepositoryWrapper`; client singleton in `AddElasticsearch()`); `public
  static Startup` as canonical (no internal normalization for this area); `NewId`
  kept for document ids; `Querry` typos corrected + honest note; blocking
  `Search(out)`/`BulkAll` omitted from the scaffold, the sole BAD/GOOD pair;
  root-vs-embedded document distinction (colocated `IndexSettingsMapper<T>` presence
  = root); enums index numerically; `AddRangeAsync` defaults `Refresh.WaitFor` (both
  authors and locked P2 initially wrong — the P4 arbiter caught it against the code;
  keep arbiters verifying already-locked pieces); `DefaultSize` declared-and-validated
  but read by no call site (honest note, no invented fallback); grouped
  interface-member ordering with `// Write / Read / Update / Delete / PIT` headers
  (canonical order verified accreted, not semantic).
- **S11 deferred:** a `CompileQueryAsync(...)` pagination extension over the search
  wrapper exists at canonical call sites (returns `PaginationResponse<T>`) — ruled
  OUT OF SCOPE for v1 of `elasticsearch-search`; candidate for a later targeted
  addition after I name the extension's file as an exemplar.
- **S11 concurrency incident (why the worktree rule above is now hard):** during
  S11, Lane B shipped S12 (`api-surface` v0.3.2) and S13 (`error-handling`
  v0.3.4), and Lane A shipped S8 (`module-feature` v0.3.3) — all in the shared
  checkout, which changed branch under Lane C mid-session. Lane C evacuated to
  `../dotnet-standards-lanec`, lost the 0.3.3/0.3.4 numbers to the other lanes'
  earlier merges, and renumbered to 0.3.5/0.3.6 during a rebase onto their main —
  exactly the index's conflict rule, and painless because the CHANGELOG entries
  are self-contained blocks.
- **Deferred to a solo session (not Lane C's to fix):** the roadmap/index still
  say "Facades/Cache in one or both projects" for S10 and list ops-service/apsp as
  the only reference projects — superseded by the S10/S11 designations; consolidate
  when the lane logs fold into `03-session-roadmap.md`.
