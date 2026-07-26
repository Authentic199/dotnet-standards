# Lane A — Data & Feature Spine · Session A1: `cqrs-feature-slice` (S8)

> Copy everything below the line into a fresh Claude Code session in
> `D:\ALTA\Project\dotnet-standards`. Lane A runs in parallel with lanes B and C —
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

**This is Lane A of three parallel lanes.** You own ONLY
`skills/cqrs-feature-slice/` and this file. Lane B owns `api-surface`,
`error-handling`, `auth-and-security`, `observability`. Lane C owns
`distributed-caching`, `elasticsearch-search`, `background-worker`,
`http-resilience`. The router, testing, scaffolding and review rubrics are excluded
from all lanes. Refuse and log anything outside your ownership.

## THE DELIVERABLE — `cqrs-feature-slice`, REFOUNDED

The roadmap flags this gateway as needing refounding: **the name suggests a CQRS
pipeline I do not run.** My settled rule (confirmed in code and shipped in
`facade-module-architecture`): **MediatR is in-process messaging, NOT CQRS
read/write separation.** This session first proposes to me (step 1, in Vietnamese)
what this skill actually is — likely "the feature slice": how to write one business
capability end to end (service, requests/responses, validation, MediatR
command/query/event envelopes, when to use MediatR vs a direct service call) —
and possibly whether it should be renamed. I decide; the skill is then built
under the three-way process.

**What this skill owns:** the internals of one feature — writing the service
(one-file interface+impl law applies), requests/responses, FluentValidation rules
(incl. when to use the module's `<X>Validation.cs` global validations), thin
MediatR envelopes and WHEN to use them, the Services/ discipline at file-creation
level (the user ordered the "Services/ is not a dumping ground" convention repeated
here so they never have to re-flag it — see roadmap "Requests deferred out of S7b").
**Not this skill:** placement (`facade-module-architecture` owns it), data access
mechanics (S9, next in this lane), HTTP surface (Lane B), anything in Lane C.

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
anything in `reference/projects/`; never select them yourself. Widening = targeted
lookup, announced (what/why). No bulk scans. Bash find/ls/grep, never Glob, inside
`reference/projects/`. R7: one canonical source per area, I designate; never
average projects. R8: anti-examples are code I point at; ask before labelling.
Sanitize: no project names, no business-domain names, no real paths, no secrets.

## SETTLED — DO NOT RELITIGATE

- The architecture and every ruling shipped in `facade-module-architecture` v0.3.0
  (read the installed skill's body + references as your baseline; rules and your
  new skill must not contradict them).
- Description law (`02-repo-structure.md` §5, settled S7b): third person
  `This skill should be used when…`, <100 words, trigger-noun pushy, `Not for:`
  routing list naming every sibling that owns an excluded area.
- The `references/` mechanism (roadmap, "standing instruction"): body ≤~300 lines
  decision layer; depth in references with conditional pointers; the split itself
  goes through the three-way loop.
- My stack: Controllers not Minimal API, Swashbuckle, no API versioning,
  FluentValidation + AutoMapper, Redis, Elasticsearch, Hangfire.

## HARD CONSTRAINTS

1. One session, one deliverable: `cqrs-feature-slice` only. Extra requests → log
   under `## Lane log` below and refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new skill
   count; report failures honestly. Follow the index's merge/version protocol
   (patch bump, CHANGELOG at top, one install at a time).
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol, then rewrite THIS file so it opens Lane A's next
   session (`ef-core-data-access`, S9 — it owns: repository-over-EF-Core with the
   real `RepositoryBase`/`IRepositoryWrapper` evidence, DbContext, entities,
   configurations incl. `HasCode<T>`/`ICode`, migrations, query conventions),
   carrying the Lane log forward.

## Lane log

- (empty — first session of Lane A)
