# Lane C — Infrastructure Services · Session C1: `distributed-caching` (S10)

> Copy everything below the line into a fresh Claude Code session in
> `D:\ALTA\Project\dotnet-standards`. Lane C runs in parallel with lanes A and B —
> read `next-session-prompt.md` (the index) for the parallel protocol, which binds
> this session. Written at the close of S7b, 2026-07-26.

---

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever be
modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `ops-service` (reusable base), `apsp-backend` (production),
`be-booking` (anti-example quarry only). Triage (`docs/TRIAGE.md`) is closed input.

**This is Lane C of three parallel lanes.** You own ONLY
`skills/distributed-caching/` and this file. Lane A owns `cqrs-feature-slice`,
`ef-core-data-access`, `domain-modeling`, `modern-csharp`. Lane B owns
`api-surface`, `error-handling`, `auth-and-security`, `observability`. The router,
testing, scaffolding and review rubrics are excluded from all lanes. Refuse and
log anything outside your ownership.

## THE DELIVERABLE — `distributed-caching`

**What this skill owns:** the Cache facade conventions — Redis (+ RedLock in
production), cache key conventions, expiry policy, when to cache, the
concurrency-locking connection (production grew `LockedException` and
`ConcurrencyHandlers` when locking arrived — the architecture skill records the
growth story; this skill owns the caching-side mechanics).

**Open question this session must put to me early (recorded in the roadmap since
S7):** HybridCache vs the current Redis approach — I decide, you record the
ruling and build accordingly.

**Not this skill:** search (that is `elasticsearch-search`, next in this lane),
background jobs (`background-worker`, later in this lane), data access (Lane A),
HTTP/auth surface (Lane B).

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

## READING DISCIPLINE

I name the exemplars at session start — ask me for the list before reading
anything in `reference/projects/`; never select them yourself. Likely areas I will
name: `Facades/Cache/` in one or both projects — but WAIT for my list and my
canonical-source designation per area (R7; the S7b precedent: per-area
designation, ops-service = base conventions, apsp-backend = production growth).
Widening = targeted lookup, announced. No bulk scans. Bash find/ls/grep, never
Glob, inside `reference/projects/`. R8: anti-examples are code I point at; ask
before labelling. Sanitize: no project names, no business-domain names, no real
paths, no secrets (cache config files may hold connection strings — list file
NAMES only, never open config JSON).

## SETTLED — DO NOT RELITIGATE

- Everything shipped in `facade-module-architecture` v0.3.0 (read the installed
  skill body + `references/facades.md` as your baseline: facade anatomy, Options
  pattern four calls, settings-follow-their-service, reach-not-size).
- Description law (`02-repo-structure.md` §5, settled S7b): third person
  `This skill should be used when…`, <100 words, trigger-noun pushy, `Not for:`
  routing list naming every sibling that owns an excluded area.
- The `references/` mechanism (roadmap standing instruction): decision-layer body,
  depth in references with conditional pointers; the split goes through the loop.
- My stack: Redis, Elasticsearch, Hangfire; MediatR is in-process messaging, not
  CQRS.

## HARD CONSTRAINTS

1. One session, one deliverable: `distributed-caching` only. Extra requests → log
   under `## Lane log` and refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new skill
   count; report failures honestly. Follow the index's merge/version protocol
   (patch bump, CHANGELOG at top, one install at a time).
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol, then rewrite THIS file so it opens Lane C's next
   session (`elasticsearch-search`, S11 — it owns: the ElasticSearch facade, the
   `ElkEntities/` convention in depth per the S7b roadmap note — Elk-prefixed
   search documents, never index a DB entity — index/projection/query
   conventions), carrying the Lane log forward.

## Lane log

- (empty — first session of Lane C)
