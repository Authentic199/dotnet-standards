# Lane B — API & Security Surface · Session B4: `dotnet-testing` (REPRIORITIZED)

> **PRIORITY OVERRIDE — 2026-07-27, S14 close, explicit user direction (recorded
> in the Lane C log; ship-the-lean-plugin-first reorder, see the index and
> `03-session-roadmap.md`).** B4's deliverable is now **`dotnet-testing`** — the
> S14 research-variant row: NO exemplar exists in `reference/projects/` (both
> projects' test projects are dead/empty scaffolding per S7b), so this skill is
> distilled from the kit's testing skills + research, adapted to the stack
> (xUnit-family conventions against Controllers/MediatR/FluentValidation/EF —
> confirm specifics with the user at session start; the R7 ask-first rule still
> applies to any `reference/projects/` peek). The three-way process, description
> law, standing delegation (memory `delegate-on-recommendation`) and all
> SETTLED/HARD-CONSTRAINT sections below still bind — read them with
> `auth-and-security` mentally replaced by `dotnet-testing`, and own
> `skills/dotnet-testing/` instead. **`auth-and-security` is PENDING** (queue
> frozen after this skill: the four review rubrics run next). Everything below
> this banner is the auth-and-security brief, kept intact for when it resumes —
> its JwtSettings divergence decision stays queued, untouched.

# ~~Lane B — API & Security Surface · Session B4: `auth-and-security`~~ (pending)

> Copy everything below the line into a fresh Claude Code session in
> `D:\agentic-plugin\dotnet-standards`. Lane B runs in parallel with lanes A and C —
> read `next-session-prompt.md` (the index) for the parallel protocol, which binds
> all lanes. This file was rewritten at S13b's close (message-keys v0.3.7 shipped).

---

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever be
modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `ops-service` (reusable base), `apsp-backend` (production,
canonical), `be-booking` (anti-example quarry), `digitalcity-backend` (older
project, introduced by the user in S13b — call-site quarry only, usable to extend
where apsp lacks a pattern). Triage (`docs/TRIAGE.md`) is closed input.

**This is Lane B of three parallel lanes.** You own ONLY
`skills/auth-and-security/` and this file. Lane A owns `module-feature` (shipped
v0.3.3), `ef-core-data-access` (in flight at S13b's close — untracked scaffold
WITH a SKILL.md sat in the shared checkout), `domain-modeling`, `modern-csharp`.
Lane C owns `distributed-caching` (v0.3.1→0.3.6), `elasticsearch-search`
(v0.3.5), `background-worker`, `http-resilience`. The router, testing,
scaffolding and review rubrics are excluded from all lanes. Refuse and log
anything outside your ownership.

## THE DELIVERABLE — `auth-and-security` (B4)

**What this skill owns:** JWT schemes and their settings (the `JwtScheme` family
— Device/User/Customer observed in apsp — and how a scheme binds to its
settings); the **JwtSettings divergence decision** queued in the roadmap
(ops-service `double` expirations + helpers vs apsp `string` expirations + extra
schemes — the user must adjudicate; do NOT average, R7); policies and the
permission handler internals behind `[HasPermission]`; secrets handling.

**Boundary facts already settled elsewhere (do not re-derive, do not contradict):**
`[HasPermission]` has a single ctor `(string[] schemes = default!, params
string[] permissions)` with three call shapes and a positional trap — the *usage*
side shipped in `api-surface` v0.3.2; this skill owns the *internals* (the
attribute's handler, policy wiring, grant-permission plumbing). UnAuthorized
throw sites census (S13): 3 in `VerifyJwtUserMiddleware` + 2 current-principal.
Exception SHAPES and middleware belong to `error-handling`; message wording
belongs to `message-keys` (v0.3.7 — route ALL key composition there).

**Not this skill:** message keys (`message-keys`); exception flow / middleware
envelope (`error-handling`); endpoints, wrappers, `[HasPermission]` usage shapes
(`api-surface`); validator/service internals (`module-feature`, Lane A);
anything Lane C.

## THE THREE-WAY PROCESS — MANDATORY

Per piece, not per skill: (1) you explain first in Vietnamese, I comment; (2) you
(author A: loads `docs/02-repo-structure.md` §5 + `docs/00-brainstorm.md` §3 + the
kit's skill format — NOT superpowers:writing-skills) and `skill-writer-sp` (author
B) draft independently, neither writes files; (3) `skill-arbiter` verdicts
A/B/MERGE/NEITHER with file-verified reasons; (4) I approve; only then write.
Both agents exist in `.claude/agents/` and are dispatchable — verify with a ping
before relying on them; the ping doubles as the context-package load, and the
same agents continue across pieces via SendMessage. Agent prompts must carry:
the exemplar list I name, all relevant settled rulings, and equal-source-access
discipline. Announce every agent use; relay milestones; agents end with
`## QUESTIONS`. Run agents in the current working directory — no worktree.
**S12 lesson:** the arbiter can introduce errors while reformulating a ruling —
diff every rephrasing against the original before writing. **S13 lesson:** the
arbiter also CATCHES author errors. **S13b lessons:** (a) the arbiter caught
BOTH authors agreeing on a false rule at the doctrine's center (request-typed
success keys — corpus said entity-typed, 0 vs 130/109) — independent drafts can
share a blind spot, so the arbiter must verify the *shared* claims, not just the
disagreements; (b) the arbiter corrected a modality drift where author A turned
the user's *permission* into an *obligation* — diff modality, not just facts;
(c) the arbiter itself may add content (the resource/value enum reading) — it
must self-declare, and you verify it like any author claim. **skill-creator is
now INSTALLED (user scope)** and the arbiter MUST invoke it live — the user
rejected disk-read provenance. A subagent's skill roster is snapshotted from the
parent session's startup state: a plugin installed mid-session is INVISIBLE to
all subagents until the parent Claude Code session restarts (proven twice in
S13b). If the arbiter reports `Unknown skill`, restart the parent session; do
not accept fallbacks.

## READING DISCIPLINE

I name the exemplars at session start — ask me for the list before reading
anything in `reference/projects/`; never select them yourself. Likely starting
points I may name: `apsp-backend/src/Infrastructure/Facades/Auth/` (JwtScheme,
HasPermission attribute), `Facades/Identity/` (token generation, grant
permission, ICurrentUser), both projects' JwtSettings + `security.json`
configs, `VerifyJwtUserMiddleware` — but WAIT for my list. Widening = targeted
lookup, announced. No bulk scans. Bash find/ls/grep, never Glob, inside
`reference/projects/`. R7: one canonical source per area, I designate; never
average — the JwtSettings divergence is exactly such a designation, pending.
R8: anti-examples are code I point at; ask before labelling. Sanitize: no
project names, no business-domain names, no real paths, no secrets — secrets
handling content makes this rule LOAD-BEARING this session: never quote real
key material, connection strings, or issuer/audience values from configs.

## SETTLED — DO NOT RELITIGATE

- Everything in `facade-module-architecture` v0.3.0, `api-surface` v0.3.2,
  `module-feature` v0.3.3, `error-handling` v0.3.4 AND `message-keys` v0.3.7
  (read the installed bodies + references as baseline; do not contradict:
  BaseController-only base, wrappers-only success path, thin expression-bodied
  endpoints, four sealed exceptions + growth-by-leaf, middleware-shapes-failures,
  not-found=400, bubble-by-default, the invalid-model-state `{ message }`
  carve-out, and message-keys' full ruling set in CHANGELOG 0.3.7 — notably:
  requests type validator messages / entities type outcome messages; the
  `MessagesType` enum closed at 15; action-family growth-by-reuse
  permitted-not-required).
- Description law (`02-repo-structure.md` §5): third person `This skill should
  be used when…`, <100 words, trigger-noun pushy, `Not for:` routing list naming
  every sibling that owns an excluded area.
- The `references/` mechanism: decision-layer body, depth in references with
  conditional pointers; the split goes through the loop.
- My stack: Controllers not Minimal API, Swashbuckle not Scalar, NO API
  versioning, FluentValidation + AutoMapper.
- **Standing delegation (S13b, saved to memory as `delegate-on-recommendation`):**
  when you present a decision WITH a clear recommendation, execute the
  recommendation and report it done; ask only when genuinely undecidable.
  Record each use in the Lane log.

## HARD CONSTRAINTS

1. One session, one deliverable: `auth-and-security` only. Extra requests → log
   under `## Lane log` and refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new skill
   count; report failures honestly. Follow the index's merge/version protocol
   (patch bump +1 relative to whatever `main` then carries, CHANGELOG at top,
   one install at a time). **Install-state note from S13b's close:** the active
   install is USER-scope `dotnet-standards 0.3.7` from marketplace
   `dotnet-standards-dev` (cache
   `~/.claude/plugins/cache/dotnet-standards-dev/dotnet-standards/0.3.7`,
   `reference/` deleted from it). Details reports **Skills (8)** — Lane A's
   untracked `ef-core-data-access` was swept in by the directory-copying
   installer (S13 precedent, harmless). Mid-session in S13b the previous
   user-scope install AND the `dotnet-standards-dev` marketplace registration
   vanished from the user registry (other-lane movement); if that recurs:
   `claude plugin marketplace add ./` (bare `.` is rejected), then install.
   Check `installed_plugins.json` before deleting ANY cached version dir (S12
   incident); ops-service's local-scope pin is not visible from this project's
   registry — assume it still exists.
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol (lane branch `lane-b/auth-and-security`, feat
   commit, merge into main — expect mid-session `main` movement from other
   lanes; conflict rule: keep both CHANGELOG entries, renumber yours above
   theirs, align cross-skill names that changed under you). Then rewrite THIS
   file AND `docs/next-session-prompt-B.md` so they open Lane B's next session
   (per the roadmap; consult it at close — B5 was provisionally the review
   rubric handoff, but roadmap rows added by other lanes may resequence),
   carrying the Lane log forward.

## Lane log

- **S13b (message-keys, 2026-07-27) — shipped v0.3.7.** Verdicts: P1 MERGE,
  P2 NEITHER (arbiter-corrected doctrine), P3 MERGE (two verdict rounds — the
  second, issued after author B's verbatim text arrived, superseded the first;
  differences reconciled by main session before write). User adjudicated
  through P3, then granted the standing delegation (see SETTLED). Rulings in
  CHANGELOG 0.3.7.
- S13b exemplars (user-named): `be-booking/CONVENTION.md` "Message Keys"
  (canonical WRITTEN); `apsp-backend/.../Facades/Definitions/Messages.cs`
  (canonical IMPLEMENTATION); call sites `apsp .../Modules/Customers/Request/`
  + `digitalcity .../ObjectRetrievals/Requests/` + `.../TrackCommands/Requests/`;
  late-named `be-booking .../Controllers/Creatives/CreativesController.cs`
  (Approve/Reject via `Action("X", true)`). `digitalcity-backend` introduced
  this session: older quarry, extension-only where apsp lacks a pattern.
- S13b R7 outcome: CONVENTION.md's own worked example
  (`Messages<CreateUserRequest>.Create()`) ruled DRIFT against its own repo's
  code (0 request-typed success calls in be-booking AND apsp; 109/130
  entity-typed) — precedent: a written convention's example can lose to the
  corpus, matching S7b's "its own frontmatter breaks the rule" treatment.
- S13b anti-example labelled (user-confirmed, real path for reviewer use):
  `apsp .../Modules/Customers/Request/*.cs` — all four request classes lack
  `[MessageDisplay]`, leaking request type names into keys (only outlier among
  57 attribute-carrying request files). NOT labelled and NEVER to be mentioned
  in artifacts (user ruled): the wrong-`T` copy-paste
  (`Messages<ObjectRetrievalVehicleImageRequest>` inside the Analyze validator,
  `AnalyzeRetrievalImageRequest.cs:79`) and the pseudo-segment string keys
  (`Required("AtLeastPersonValue")`, `AlreadyExist("InHandleIncident")`).
- S13b unruled candidates for a future review rubric: the non-generic facade's
  hardcoded const key (`Messages.Middleware.IPAddressForbidden` =
  literal `"Mes.Middleware.IPAddress.Forbidden"`) — a third key mechanism no
  ruling covers; the `Action(MessagesType.X)` bypass (compiles, zero call
  sites, deliberately excluded from artifacts); validator dual-form census
  detail (apsp 194 constants vs 135 lambda, digitalcity 142 vs 245 — the newer
  project flipped toward the lambda before the doctrine existed).
- S13b process events: skill-creator plugin installed mid-session
  (user-scope) after the user rejected disk-read provenance; two arbiter
  spawns failed on stale skill rosters before a parent-session restart fixed
  it — subagent rosters snapshot at parent startup, mid-session installs are
  invisible to them. The restarted session resumed cleanly; SendMessage
  continuation of pre-restart agents (author B) worked.
- S13b install events: at close, the prior user-scope install and the
  `dotnet-standards-dev` marketplace registration were both GONE from the user
  registry; re-added marketplace (`claude plugin marketplace add ./`) and
  installed 0.3.7 fresh. `reference/` deleted from the 0.3.7 cache. No cached
  version dirs deleted. Cosmetic: arbiter's final consistency pass PASSed with
  two optional notes; note 2 applied (`Create("part")` casing), note 1 declined
  (placeholder-notation alignment across the three pieces).
- **Carried from S13** (still relevant): R7 split precedent (ops-service
  canonical for SHAPE, apsp for THROW PATTERNS); UnAuthorized census (3
  VerifyJwtUserMiddleware + 2 current-principal) — directly relevant to B4;
  error-handling's unruled candidates list (CHANGELOG 0.3.4 + superseded lane
  file @ 5d7ac3c); `distributed-lock` roadmap row (S16+, owns
  ConcurrencyHandlers + LockedException 423).
- **Carried from S12:** single `[HasPermission]` ctor
  `(string[] schemes = default!, params string[] permissions)` — B4's boundary
  with api-surface; services-only constructor injection 2–3 interfaces
  canonical; `GenerateDocumentationFile` in root `Directory.Build.props`.
  S12 anti-example list: superseded lane file @ 6848e17 + CHANGELOG 0.3.2.
- **Queued for THIS session (B4):** the JwtSettings divergence (ops `double`
  expirations + helpers vs apsp `string` expirations + extra schemes) — user
  adjudicates under R7; do not average.
