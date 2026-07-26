# Lane B — API & Security Surface · Session B1: `api-surface` (S12)

> Copy everything below the line into a fresh Claude Code session in
> `D:\ALTA\Project\dotnet-standards`. Lane B runs in parallel with lanes A and C —
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

**This is Lane B of three parallel lanes.** You own ONLY `skills/api-surface/` and
this file. Lane A owns `cqrs-feature-slice`, `ef-core-data-access`,
`domain-modeling`, `modern-csharp`. Lane C owns `distributed-caching`,
`elasticsearch-search`, `background-worker`, `http-resilience`. The router,
testing, scaffolding and review rubrics are excluded from all lanes. Refuse and
log anything outside your ownership.

## THE DELIVERABLE — `api-surface`

**What this skill owns (contracts already recorded in the roadmap, S7b rows):**
routes, DTOs, versioning stance (my stack: NO API versioning), OpenAPI/Swashbuckle
conventions, and the **controller file-writing conventions** the S7b session
deferred here: expression-bodied vs block-bodied endpoints, long-signature
wrapping, enforcement of the unified partial rule (base list only in the
suffix-less core file), and the multi-permission/multi-scheme `[HasPermission]`
overload (a real second attribute shape the architecture skill's single-permission
sample does not show — auth *internals* still belong to `auth-and-security`).

**Cross-skill contract set by the S7b description verdict — binding:**
`api-surface` claims routes, DTOs, versioning, OpenAPI and endpoint-writing
conventions; it must **NOT** claim controller *placement*, which
`facade-module-architecture` owns (its description triggers on "where a controller
goes"). Your description's `Not for:` list must route placement back there.
Also decide and record (Lane log) who owns `Messages<T>` response-text
conventions — this session or `error-handling` (B2) — so the two never collide.

**Not this skill:** feature internals (Lane A), exception flow and middleware
behavior (`error-handling`, next in this lane), JWT/policies (`auth-and-security`,
later in this lane), anything in Lane C.

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
anything in `reference/projects/`; never select them yourself. Known starting
points I will likely name: `apsp-backend/src/Web/Controllers/` exemplars (the
S7b set: BaseController, Vouchers single-file, Customers/Devices partials) and
`ops-service` OpenAPI facade — but WAIT for my list. Widening = targeted lookup,
announced. No bulk scans. Bash find/ls/grep, never Glob, inside
`reference/projects/`. R7: one canonical source per area, I designate; never
average. R8: anti-examples are code I point at; ask before labelling. Known
candidates I previously ruled "skip for architecture, settle in S12": mixed
endpoint body styles in one file; base list on a non-core partial; ~200-char
one-line signatures. Re-raise them here for a convention ruling. Sanitize: no
project names, no business-domain names, no real paths, no secrets.

## SETTLED — DO NOT RELITIGATE

- Everything shipped in `facade-module-architecture` v0.3.0 (read the installed
  skill body + `references/web-controllers.md` as your baseline; do not
  contradict: BaseController-only base, no own `[Route]`, wrappers-only success
  path, thin endpoints, CancellationToken, suffix partial law).
- Description law (`02-repo-structure.md` §5, settled S7b): third person
  `This skill should be used when…`, <100 words, trigger-noun pushy, `Not for:`
  routing list naming every sibling that owns an excluded area.
- The `references/` mechanism (roadmap standing instruction): decision-layer body,
  depth in references with conditional pointers; the split goes through the loop.
- My stack: Controllers not Minimal API, Swashbuckle not Scalar, NO API
  versioning, FluentValidation + AutoMapper.

## HARD CONSTRAINTS

1. One session, one deliverable: `api-surface` only. Extra requests → log under
   `## Lane log` and refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new skill
   count; report failures honestly. Follow the index's merge/version protocol
   (patch bump, CHANGELOG at top, one install at a time).
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol, then rewrite THIS file so it opens Lane B's next
   session (`error-handling`, S13 — it owns: when to throw which of the four
   exceptions, middleware behavior, error message conventions, the
   LockedException growth pattern in practice; plus the JwtSettings divergence
   decision queued in the roadmap for `auth-and-security`, B3), carrying the
   Lane log forward.

## Lane log

- (empty — first session of Lane B)
