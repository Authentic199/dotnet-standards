# Lane B — API & Security Surface · Session B2: `error-handling` (S13)

> Copy everything below the line into a fresh Claude Code session in
> `D:\ALTA\Project\dotnet-standards`. Lane B runs in parallel with lanes A and C —
> read `next-session-prompt.md` (the index) for the parallel protocol, which binds
> this session. Written at the close of S12, 2026-07-26.

---

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever be
modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `ops-service` (reusable base), `apsp-backend` (production,
canonical), `be-booking` (anti-example quarry only). Triage (`docs/TRIAGE.md`) is
closed input.

**This is Lane B of three parallel lanes.** You own ONLY `skills/error-handling/`
and this file. Lane A owns `cqrs-feature-slice`, `ef-core-data-access`,
`domain-modeling`, `modern-csharp`. Lane C owns `distributed-caching` (shipped
v0.3.1), `elasticsearch-search`, `background-worker`, `http-resilience`. The
router, testing, scaffolding and review rubrics are excluded from all lanes.
Refuse and log anything outside your ownership.

## THE DELIVERABLE — `error-handling` (S13)

**What this skill owns:** when to throw which of the four sealed exceptions
(`BadRequestException` 400, `UnAuthorizedException` 401, `ForbiddenException`
403, `InternalServerException` 500); the exception middleware's behavior (how a
thrown exception becomes an `ErrorResultWrapper` — the middleware is the ONLY
producer of error responses, settled); the **`LockedException` growth pattern in
practice** — apsp-backend grew a fifth sealed exception beyond the settled
four-exception base (its `Core` also carries `ISingletonService`, a third
lifetime marker the architecture skill forbids) — decide whether S13 documents
the exception-growth as the sanctioned pattern the architecture skill already
describes ("a new exception is one sealed file with two constructors; the
middleware handles it the day it is written") and how far that sanction goes.
Also **queue, do not solve**: the JwtSettings divergence decision the roadmap
holds for `auth-and-security` (B3).

**Cross-skill contracts — binding:**
- `Messages<T>` text conventions belong to a **dedicated future `message-keys`
  skill** (ruled S12) — NOT to error-handling. Error *flow* is yours; error
  *wording* is not. Your description's `Not for:` must route message text there,
  and your body points there exactly as `api-surface` does.
- `api-surface` (shipped v0.3.2) owns the success path, `ProducesResponseType`
  documentation of the error envelope, and endpoint-writing conventions. Do not
  re-legislate them; read the installed body first.
- `ErrorResultWrapper` has **no `Data` property**; exceptions take `(message)`
  and `(message, innerException)`, never a data payload (settled S7b).
- Controllers never build error responses — they throw (settled).

**Not this skill:** success envelope & endpoints (`api-surface`), JWT/policies
(`auth-and-security`, B3 — next in this lane), message text (`message-keys`),
feature internals (Lane A), anything Lane C.

## THE THREE-WAY PROCESS — MANDATORY

Per piece, not per skill: (1) you explain first in Vietnamese, I comment; (2) you
(author A: loads `docs/02-repo-structure.md` §5 + `docs/00-brainstorm.md` §3 + the
kit's skill format — NOT superpowers:writing-skills) and `skill-writer-sp` (author
B) draft independently, neither writes files; (3) `skill-arbiter` verdicts
A/B/MERGE/NEITHER with file-verified reasons; (4) I approve; only then write.
Both agents exist in `.claude/agents/` and are dispatchable — verify with a ping
before relying on them. **Continue the same agent across pieces via SendMessage**
(S12 proved this works and keeps verified context). Agent prompts must carry: the
exemplar list I name, all relevant settled rulings, and equal-source-access
discipline. Announce every agent use; relay milestones; agents end with
`## QUESTIONS`; continue them via SendMessage. Run agents in the current working
directory — no worktree for subagents. **S12 lesson: the arbiter can introduce
errors while reformulating a ruling** (it rewrote the wrapping rule and broke it
against its own example; author B caught it). When a verdict rephrases one of my
rulings, diff the rephrasing against the original ruling before writing.

## READING DISCIPLINE

I name the exemplars at session start — ask me for the list before reading
anything in `reference/projects/`; never select them yourself. Known starting
points I will likely name: `apsp-backend/src/Core/Common/Exceptions/` (the four +
`LockedException`, wrappers), the `Middleware` facade
(`ExceptionHandlerMiddleware`, `VerifyJwtUserMiddleware`) — but WAIT for my list.
Widening = targeted lookup, announced. No bulk scans. Bash find/ls/grep, never
Glob, inside `reference/projects/`. R7: one canonical source per area, I
designate; never average. R8: anti-examples are code I point at; ask before
labelling. Sanitize: no project names, no business-domain names, no real paths,
no secrets.

## SETTLED — DO NOT RELITIGATE

- Everything in `facade-module-architecture` v0.3.0 AND `api-surface` v0.3.2
  (read both installed bodies + their references as baseline; do not contradict:
  BaseController-only base, wrappers-only success path, thin expression-bodied
  endpoints, CancellationToken law, suffix partial law, four sealed exceptions +
  growth-by-leaf, middleware-shapes-failures).
- Description law (`02-repo-structure.md` §5): third person `This skill should
  be used when…`, <100 words, trigger-noun pushy, `Not for:` routing list naming
  every sibling that owns an excluded area.
- The `references/` mechanism: decision-layer body, depth in references with
  conditional pointers; the split goes through the loop.
- My stack: Controllers not Minimal API, Swashbuckle not Scalar, NO API
  versioning, FluentValidation + AutoMapper.
- S12 rulings recorded in CHANGELOG 0.3.2 (single endpoint body style, strict
  binding sources, wrapping counts every parameter, `{id:guid}`, DTO
  inheritance laws, `message-keys` ownership).

## HARD CONSTRAINTS

1. One session, one deliverable: `error-handling` only. Extra requests → log
   under `## Lane log` and refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new skill
   count; report failures honestly. Follow the index's merge/version protocol
   (patch bump → 0.3.3, CHANGELOG at top, one install at a time). **Cache
   warning from S12:** other projects hold local-scope installs of this plugin
   (e.g. `D:\ALTA\Project\TWOH\ops-service`) whose `installPath` points into the
   shared version cache — check `installed_plugins.json` before deleting any
   cached version dir, and after reinstalling delete `reference/` from the new
   cache copy (it re-copies ~387 MB of dead weight every install).
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol (lane branch `lane-b/error-handling`, feat commit,
   merge into main), then rewrite THIS file so it opens Lane B's next session
   (`auth-and-security`, B3 — it owns: JWT schemes and their settings, the
   JwtSettings divergence decision queued in the roadmap, policies and the
   permission handler internals behind `[HasPermission]`, secrets handling),
   carrying the Lane log forward.

## Lane log

- **S12 (api-surface, 2026-07-26) — shipped v0.3.2.** Verdicts: P1 MERGE
  (description), P2 MERGE + errata (body), P3–P5 MERGE (references).
- S12 ruling: `Messages<T>` conventions get a **dedicated `message-keys` skill**
  (sources: `be-booking/CONVENTION.md` "Message Keys" section — verified to
  match `apsp-backend/src/Infrastructure/Facades/Definitions/Messages.cs`
  key-for-key — plus that file and its references). Needs a roadmap row and its
  own session; suggested slot: immediately after S13, since error-handling
  wants it. Roadmap edit belongs to the index session, not a lane.
- S12 deferred to `message-keys`: validators use two message forms —
  `Messages<T>.X(selector)` and bare `MessagesType` constants (constants are
  MORE frequent by raw count; `Messages<T>` matches the shared facade and both
  shipped skill bodies). That skill must pick one.
- S12 anti-examples labelled (user-confirmed, real paths for reviewer use):
  `Web/Controllers/Engines/EnginesController.cs` (11-defect pre-convention
  file), `Users/RolesController.cs` (naming drift + redundant class-level
  `[Authorize]`), `Customers/CustomersController.Auth.cs` (base list on
  non-core part), `Devices/*` (base list repeated on every part),
  `Users/Auth.UsersController.cs` etc. (prefix-named partials),
  `Modules/Vehicles/Request/` (singular folder), `Devices` responses
  (Base/Default sibling duplication), ~200-char one-line signatures,
  `CancellationToken = default` on actions.
- S12 fact corrections: `[HasPermission]` is ONE constructor
  `(string[] schemes = default!, params string[] permissions)` — not an
  overload pair (this file previously said "overload", wrong); "constructor
  injects the module's service interface and nothing else" reads as
  *services-only* (2–3 service interfaces is canonical), not *exactly one*;
  renaming a misspelled action is SAFE (no `[action]` token, no
  `CustomOperationIds`); `GenerateDocumentationFile` lives in root
  `Directory.Build.props`.
- S12 unruled candidates for a future review rubric: a response DTO property
  documented "internal only, not returned in JSON" while being a plain public
  property (`Devices` default response); the style-canonical Vouchers file
  itself has one unwrapped 2-param signature and two block bodies.
- S12 incident: deleting stale cached plugin versions broke ops-service's
  local-scope install (its installPath pointed at the deleted 0.1.0 dir);
  repaired by reinstalling 0.3.2 in that project. See hard constraint 2.
- S12 observation: `docs/next-session-prompt-A.md` was modified in the working
  tree by another lane mid-session; left uncommitted and untouched by Lane B.
