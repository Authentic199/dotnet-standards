
## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever be
modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `ops-service` (reusable base), `apsp-backend` (production,
canonical), `be-booking` (anti-example quarry — and, uniquely for THIS skill, the
canonical source of the written convention, see below). Triage (`docs/TRIAGE.md`)
is closed input.

**This is Lane B of three parallel lanes.** You own ONLY `skills/message-keys/`
and this file. Lane A owns `module-feature` (shipped v0.3.3 — renamed from
`cqrs-feature-slice` by user ruling), `ef-core-data-access`, `domain-modeling`,
`modern-csharp`. Lane C owns `distributed-caching` (shipped v0.3.1),
`elasticsearch-search` (in flight at S13's close), `background-worker`,
`http-resilience`. The router, testing, scaffolding and review rubrics are
excluded from all lanes. Refuse and log anything outside your ownership.

## THE DELIVERABLE — `message-keys` (S13b)

**What this skill owns:** the `Messages<T>` key grammar — how success and error
message keys are composed, the `Messages<T>.X(selector)` helper family, the
`MessagesType` constants, `[MessageDisplay]`, and which form is used where.
Both shipped Lane B skills and Lane A's `module-feature` route ALL message
wording here; this skill is the contract they point at.

**Sources (ruled in S12 — the user confirms the list at session start):**
`be-booking/CONVENTION.md` "Message Keys" section — verified in S12 to match
`apsp-backend/src/Infrastructure/Facades/Definitions/Messages.cs` key-for-key —
plus that file and its call sites. Note the inversion: for this one skill,
be-booking carries the canonical *written* convention while apsp-backend is the
canonical *implementation*; ops-service is not yet named for this area.

**Must settle (both queued by earlier sessions):**
1. Validators use two message forms — `Messages<T>.X(selector)` and bare
   `MessagesType` constants. Constants are MORE frequent by raw count;
   `Messages<T>` matches the shared facade and both shipped skill bodies
   (S12 finding). Pick ONE going forward (single-style doctrine).
2. `[MessageDisplay]` vs `Messages<T>`-lambda conflict, logged by S8
   (`module-feature`) at its close — read their CHANGELOG 0.3.3 entry and lane
   log for the exact shape before drafting.

**Not this skill:** error flow, exceptions, the middleware envelope
(`error-handling`, shipped v0.3.4); endpoints, wrappers, `ProducesResponseType`
(`api-surface`); feature/service/validator internals (`module-feature`, Lane A);
JWT/policies (`auth-and-security`, next in this lane); anything Lane C.

## THE THREE-WAY PROCESS — MANDATORY

Per piece, not per skill: (1) you explain first in Vietnamese, I comment; (2) you
(author A: loads `docs/02-repo-structure.md` §5 + `docs/00-brainstorm.md` §3 + the
kit's skill format — NOT superpowers:writing-skills) and `skill-writer-sp` (author
B) draft independently, neither writes files; (3) `skill-arbiter` verdicts
A/B/MERGE/NEITHER with file-verified reasons; (4) I approve; only then write.
Both agents exist in `.claude/agents/` and are dispatchable — verify with a ping
before relying on them; the ping doubles as the context-package load, and the
same agents continue across pieces via SendMessage (S12 and S13 both proved
this). Agent prompts must carry: the exemplar list I name, all relevant settled
rulings, and equal-source-access discipline. Announce every agent use; relay
milestones; agents end with `## QUESTIONS`. Run agents in the current working
directory — no worktree for subagents. **S12 lesson: the arbiter can introduce
errors while reformulating a ruling** — diff every rephrasing against the
original before writing. **S13 lesson (the flip side): the arbiter also CATCHES
author errors** — it corrected a false factual claim by rewriting it as a
normative contract statement, fixed a wrong access modifier against the source,
and rejected a mechanism explanation both authors got differently. The
diff-the-rephrasing duty and the trust-but-verify stance are both earned.

## READING DISCIPLINE

I name the exemplars at session start — ask me for the list before reading
anything in `reference/projects/`; never select them yourself. Known starting
points I will likely name: `be-booking/CONVENTION.md` (Message Keys section),
`apsp-backend/src/Infrastructure/Facades/Definitions/Messages.cs` (and its
`MessagesType` sibling if named), plus validator/controller call sites — but
WAIT for my list. Widening = targeted lookup, announced. No bulk scans. Bash
find/ls/grep, never Glob, inside `reference/projects/`. R7: one canonical source
per area, I designate; never average. R8: anti-examples are code I point at; ask
before labelling. Sanitize: no project names, no business-domain names, no real
paths, no secrets.

## SETTLED — DO NOT RELITIGATE

- Everything in `facade-module-architecture` v0.3.0, `api-surface` v0.3.2,
  `module-feature` v0.3.3 AND `error-handling` v0.3.4 (read the installed bodies
  + references as baseline; do not contradict: BaseController-only base,
  wrappers-only success path, thin expression-bodied endpoints, four sealed
  exceptions + growth-by-leaf, middleware-shapes-failures, not-found=400,
  bubble-by-default, the invalid-model-state `{ message }` carve-out).
- Description law (`02-repo-structure.md` §5): third person `This skill should
  be used when…`, <100 words, trigger-noun pushy, `Not for:` routing list naming
  every sibling that owns an excluded area.
- The `references/` mechanism: decision-layer body, depth in references with
  conditional pointers; the split goes through the loop.
- My stack: Controllers not Minimal API, Swashbuckle not Scalar, NO API
  versioning, FluentValidation + AutoMapper.
- S13 rulings recorded in CHANGELOG 0.3.4 (not-found doctrine, Forbidden
  honesty, wrap-vs-bubble, growth sanction boundary, carve-out).

## HARD CONSTRAINTS

1. One session, one deliverable: `message-keys` only. Extra requests → log
   under `## Lane log` and refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new skill
   count; report failures honestly. Follow the index's merge/version protocol
   (patch bump +1 relative to whatever `main` then carries, CHANGELOG at top,
   one install at a time). **Install-scope note from S13:** the active install
   is now USER-scope (Lane A switched it 2026-07-26 16:04); ops-service still
   holds a local-scope install pinned to cache 0.3.2 — check
   `installed_plugins.json` before deleting ANY cached version dir (S12
   incident), and after reinstalling delete `reference/` from the new cache
   copy (~387 MB of dead weight per install). Cache 0.3.3 is now unreferenced;
   it was left in place.
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol (lane branch `lane-b/message-keys`, feat commit,
   merge into main — expect mid-session `main` movement from other lanes; the
   conflict rule is: keep both CHANGELOG entries, renumber yours above theirs,
   and align any cross-skill names that changed under you, as S13 had to do
   for the `module-feature` rename). Then rewrite THIS file so it opens Lane
   B's next session (`auth-and-security` — it owns: JWT schemes and their
   settings, the JwtSettings divergence decision queued in the roadmap,
   policies and the permission handler internals behind `[HasPermission]`,
   secrets handling), carrying the Lane log forward.

## Lane log

- **S13 (error-handling, 2026-07-26→27) — shipped v0.3.4.** Verdicts: P1 MERGE
  (description), P2 MERGE (body), P3 MERGE B-dominant (middleware reference).
  User adjudicated through P1, then delegated all remaining decisions. Rulings
  are in CHANGELOG 0.3.4.
- S13 exemplars used (user-named): both projects' `Core/Common/Exceptions/` +
  `ExceptionHandlerMiddleware.cs`; R7 split — ops-service canonical for SHAPE,
  apsp-backend canonical for THROW PATTERNS. Throw-site census: BadRequest 234,
  InternalServer 41, UnAuthorized 5 (3 in VerifyJwtUserMiddleware + 2
  current-principal), Forbidden 0, Locked 3 (all in ConcurrencyHandler).
- S13 discovery, ruled into the skill: `Web/Program.cs`
  `InvalidModelStateResponseFactory` answers automatic validation 400s with a
  plain `{ message }` object — NOT the envelope. Doctrine scoped to *thrown*
  exceptions; carve-out named in the body. The two-error-shapes divergence
  itself is deliberately unruled (candidate for a future review rubric).
- S13 anti-examples labelled (user-confirmed, real paths for reviewer use):
  apsp `BadRequestException(message, innerException)` never pins StatusCode
  (latent status-0; ops-service fixed it); the `S3FileUploadException` catch in
  `ExceptionHandlerMiddleware` compensates but never writes a response —
  present IDENTICALLY in both projects, framed as a shape defect. NOT labelled
  (user ruled): apsp's data-payload ctor + `ErrorResultWrapper.Data` +
  middleware `Data["Data"]` line (divergence stated as law without citing code).
- S13 unruled candidates for a future review rubric: `HttpCustomException.Value`
  dead in BOTH projects (set by every ctor, read by nothing); public
  parameterless `BadRequestException()` unpinned (second status-0 path); the
  S3 catch logs unconditionally while the general path gates at >= 500;
  `ErrorResponseSettings` read per-request from `IConfiguration`, bypassing the
  options pattern facades mandate.
- S13 roadmap edit by user direction (lane-ownership exception):
  `distributed-lock` row added under S16+ — owns `ConcurrencyHandlers` +
  `LockedException` 423; error-handling cites 423 only as the growth example.
- S13 sequencing ruling (resolved by main session under delegation): the S13
  lane file's constraint 4 said "next = auth-and-security (B3)", but roadmap
  row S13b (added LATER at S12's close by explicit user direction) says
  `message-keys` runs immediately after S13. Later direction won; THIS file
  opens `message-keys`, and `auth-and-security` follows it.
- S13 cross-lane events: S8 (`module-feature` v0.3.3, including the
  `cqrs-feature-slice` rename) merged into `main` mid-session — S13 resolved
  the CHANGELOG conflict per protocol (renumbered to 0.3.4) and renamed its own
  routes before merging. Lane C's `elasticsearch-search` appeared untracked in
  the shared checkout mid-session and was swept into the 0.3.4 user-scope
  install by the directory-copying installer (details reports Skills (6), not
  the protocol's n+1=5); harmless — Lane C's own install will supersede it.
  `docs/next-session-prompt-A.md` remains modified-uncommitted by another lane;
  untouched by Lane B (S12 precedent). An empty `skills/module-feature/`
  scaffold appeared pre-merge; ignored.
- **Carried from S12** (still relevant): `Messages<T>` conventions ruled OUT of
  api-surface and error-handling into THIS skill; validator dual message forms
  (constants more frequent; `Messages<T>` matches the facade) — S13b must pick
  one; S12 fact corrections (single `[HasPermission]` ctor
  `(string[] schemes = default!, params string[] permissions)`;
  services-only constructor injection reads as 2–3 interfaces canonical;
  renaming a misspelled action is safe; `GenerateDocumentationFile` in root
  `Directory.Build.props`). S12 anti-example list lives in the S12 section of
  the superseded lane file (git history: `docs/next-session-prompt-B.md` @
  commit 6848e17) and in CHANGELOG 0.3.2.
- Queued, do not solve in S13b: JwtSettings divergence (ops `double`
  expirations + helpers vs apsp `string` expirations + extra schemes) →
  `auth-and-security`.
