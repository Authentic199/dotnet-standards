> **CORRECTION BANNER — 2026-07-29, second pass.** The opener below is **Lane
> C's** brief and its numbers are stale. The tree ships **v0.3.27** — 21 skills
> (`claude plugin details` says 23; it counts the 2 commands), 6 agents, 3 hooks
> — not v0.3.16 (15). Everything else in the opener still stands for a Lane C
> session; maintenance sessions append to this banner rather than rewriting
> another lane's brief out from under it. **Open `docs/next-session-prompt.md`
> (LANE BOARD) first — it is authoritative for versions, lanes and parked work;
> this file is not.**
>
> What landed since the opener was written: rubrics #2–4
> (`dotnet-architecture-review` 0.3.17, `dotnet-security-review` 0.3.18/0.3.19,
> `dotnet-performance-review` 0.3.20) · process-integration v1 0.3.21 ·
> `claude-md-builder` 0.3.22–0.3.24 · `dotnet-review-flow`'s NO-SIGNAL branch
> 0.3.25 · `claude-md-builder` contradictions 0.3.26 · **mechanism E's
> `router-nudge` hook 0.3.27**.
>
> **A process fact added at 0.3.27, and it outranks the version numbers.** Skill
> descriptions do not reliably get this plugin entered. Measured, not feared: in
> the consumer repository this plugin is installed into, a session with every
> description loaded answered a review request by going straight to `find` —
> twice — and loaded no skill, command or agent. `hooks/router-nudge` now names
> the router on the first prompt of a .NET session. **Two defects behind that
> failure are still open**, queued in this order by the user: the router's own
> description triggers on confusion rather than on entry, and the entire review
> surface is diff-anchored, so "audit these folders, change nothing" has no
> owner. Both sit in the board's PENDING log with the evidence.
>
> **Two process facts worth more than the version numbers.** The plugin installs
> from the **GitHub** repo, not from this checkout, and at **project** scope
> bound to an unrelated repository — so a local merge to `main` ships nothing
> until it is pushed, and `claude plugin update` needs `--scope project`. And two
> sessions running at once can pick the **same version number with no git
> conflict**, because identical strings in both manifests merge silently; read
> the version off `main` at merge time, never off your own branch.

> **OPENER — 2026-07-28, S17 close (Lane C).** `mediatr-messaging` SHIPPED at
> **v0.3.16 (15 skills)**. **Lane C's queue is now empty of unblocked work** —
> the remaining Lane C names (`observability`, `background-worker`,
> `http-resilience`) are user-PENDING since S14. **At session start, ask the
> user which (if any) is unfrozen; do not pick one yourself.** If none, this
> lane pauses — the rubric phase continues as SOLO sessions (rubric #1 shipped
> `dotnet-code-review` at 0.3.15; #2–4 remain, prompt file
> `docs/next-session-prompt-rubrics.md`). The living index of all lanes is
> `docs/next-session-prompt.md` (LANE BOARD) — open it first. Lane C's lane
> file `docs/next-session-prompt-C.md` mirrors this brief.

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever
be modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `apsp-backend` (production, canonical), `ops-service`
(reusable base), `be-booking` (anti-example quarry), `digitalcity-backend`
(older quarry, extension-only), `qms-backend` (named at S17 — used ONLY for the
one file the user named; not a general quarry unless the user widens it).
Triage (`docs/TRIAGE.md`) is closed input.

**This is Lane C.** Shipped through **v0.3.16** (15 skills —
`mediatr-messaging` landed at S17; rubric #1's `dotnet-code-review` landed at
0.3.15 the same day). Lane C owns no in-flight deliverable. PENDING by user
direction: `observability`, `background-worker`, `http-resilience` (Lane C's),
`domain-modeling`, `modern-csharp` (Lane A's), `project-scaffolding`
(solo-only). Refuse and log anything outside your ownership.

**START IN YOUR OWN WORKTREE once a deliverable is confirmed** (proven
S14–S17): `git worktree add ../dotnet-standards-lanec-s18 -b
lane-c/<skill-name> main`. The worktree has no `reference/` — read exemplars
through the shared checkout path `D:\agentic-plugin\dotnet-standards\reference\`.
S17 lesson: merge `main` INTO the lane branch BEFORE making router edits —
`main` moves mid-session (S17 saw three moves: 0.3.14 hotfix, 0.3.15 rubric
ship, a board-header commit) and pre-merge editing of the router invites
conflicts.

## THE THREE-WAY PROCESS — MANDATORY, SKILL-DRIVEN

**Invoke `three-way-skill-loop` at session start** — it defines the loop; the
main session COORDINATES ONLY (memory `author-a-delegated`). Author A =
`skill-writer-a`, Author B = `skill-writer-sp`, arbiter = `skill-arbiter`
(invokes `skill-creator:skill-creator` LIVE; `Unknown skill` → restart parent
session). Ping all three with the context package first; batch authors'
`## QUESTIONS`; drafts to the arbiter **VERBATIM — never summarized** (S16
violation, self-caught; S17 clean). Verify arbiter self-declared additions;
diff rephrasings (S12); verify SHARED claims (S13b/S16/S17 — S17 caught: both
authors' MS.DI open-generic registration cannot resolve the corpus's
nested-type-parameter shape, and both authors' unconditional
`<EventName>Handler` rule breaks on fan-out); diff modality both directions
(S13b/S15/S17). **Coordinator must also check the arbiter's recommendations
against its own evidence** — S17: the arbiter recommended dropping the user's
naming-drift anti-example label "because fan-out requires descriptive names",
but the two labelled sites are single-handler events; the label survived,
narrowed. Run agents in the lane worktree.

**STANDING DELEGATION (LAW):** execute clear recommendations, report them with
brief confirmations (vetoes stay cheap), log each use; ask only the genuinely
undecidable. Carve-outs remain the user's alone: naming canonical
sources/exemplars (R7), labelling anti-examples (R8) — S17 note: UN-labelling
or narrowing a label is also R8, the user's alone.

## READING DISCIPLINE

Ask the user for the exemplar list at session start — never select exemplars
yourself. Widening = announced targeted lookup. Bash find/ls/grep, never Glob,
inside `reference/projects/`. **`apsp-backend/.claude/worktrees/` holds four
duplicate checkouts — exclude them or every census is inflated ~5×.** R7: one
canonical source per area, never average. R8: anti-examples are code the user
points at; ask before labelling. Sanitize: no project names, no
business-domain nouns, no real paths, no secrets. Neutral placeholder set:
`Entity`/`EntityBaseResponse`/`CreateEntityRequest`/`Wrapper`; corpus-specific
API names (S17: `LogExtension.Error`) are also sanitized out.

## SETTLED — DO NOT RELITIGATE

- Everything in shipped bodies through **v0.3.16** (read them as baseline),
  incl. `mediatr-messaging`'s full ruling set in CHANGELOG 0.3.16
  (DomainEvents/ canonical folder; three-armed naming law with the fan-out
  exception; controller dispatch = house default not ban; handler `internal
  sealed` = recommendation, mix = the defect; marker-type AddMediatR
  registration; nested-generic open-generic registration needs a unifying
  container — MS.DI cannot; Publish semantics only inside doc-provenance
  markers) and `automapper-mapping`'s set in CHANGELOG 0.3.12.
- Router rulings (CHANGELOG 0.3.10) + alignment precedent: router covers every
  skill on `main` at merge time, same feat commit (0.3.14 hotfix exists
  because S9b skipped this).
- Description law (`02-repo-structure.md` §5): third person, <100 words,
  trigger-noun pushy, `Not for:` naming every owning sibling (shipped-only
  roster). No H1 in skill bodies.
- Provenance law (hardened at S17): any claim not grounded in the corpus ships
  ONLY inside a visibly marked documentation-derived block; API-recall
  ordering/behaviour claims that cannot be corpus-checked are REFUSED (S16
  IncludeMembers precedent; S17 refused behaviour execution order and
  Send-multiple-handlers).
- Budget norm: siblings run 117–450 lines; skill-creator hard bar <500. Do not
  chase the largest-sibling number by cutting content (arbiter ruling, S17).
- Stack: .NET 8, Controllers not Minimal API, Swashbuckle not Scalar, NO API
  versioning, FluentValidation + AutoMapper v12, Redis, Elasticsearch,
  Hangfire; MediatR v12 = in-process messaging, not CQRS.

## HARD CONSTRAINTS

1. One session, one deliverable (+ mandatory router edits, same feat commit).
   Extra requests → log under `## Lane log`, refuse.
2. Prove it: validate + REAL update + `claude plugin details` shows the new
   skill count (16 if a Lane C skill ships next). Install lessons (S16/S17):
   `claude plugin update dotnet-standards@dotnet-standards-dev` (short name
   fails); `details` alone is never proof — verify `installed_plugins.json`
   points at the new cache (S17: gitCommitSha matched the merge commit);
   delete `reference/` from the new cache dir; both manifests must agree on
   the version; check `installed_plugins.json` before deleting ANY cached
   version dir (caches 0.3.7–0.3.15 left unreferenced, untouched).
3. Artifact language English; talk to the user in Vietnamese.
4. End: commit per protocol (lane branch, feat commit, merge into main —
   expect mid-session `main` movement; S17 rule: merge main into the lane
   branch BEFORE router edits). Rewrite THIS file and
   `docs/next-session-prompt-C.md` for Lane C's next session, carrying the
   Lane log; update the LANE BOARD row.

## Lane log

- **S17 (mediatr-messaging, 2026-07-28) — shipped v0.3.16.** Verdicts: P1
  MERGE (api-surface Not-for cut on a verified-false premise — zero controller
  dispatch sites; literal Send/Publish tokens won), P2 MERGE
  (arbiter-corrected the shared unconditional-`<EventName>Handler` blind spot
  with the fan-out exception — 3 corpus events × 2 handlers; A's "next to the
  scan" locative disproved), P3 MERGE (arbiter's own major correction: both
  authors' MS.DI open-generic registration cannot resolve
  `Handler<TData> : IRequestHandler<Message<TData>>` — positional
  substitution, no unification; "arity is the trap" refused, indirection is;
  user notified, no veto), P4 MERGE (Events/-rename inferences from BOTH
  authors refused — only "never create new ones" is user doctrine). Final
  pass PASS + 1 blocking defect (envelope accessibility in examples
  normalized to `public record` — the skill disclaims envelope shape) + a
  second budget pass (553 → 450 lines; arbiter: don't chase 437 by cutting
  content). Full rulings in CHANGELOG 0.3.16.
- S17 exemplars (user-named): apsp `Modules/Vouchers/` (Events/ = old form),
  `Modules/Customers/` (DomainEvents/ = canonical; `HandleZaloAvatarCommand.cs`
  = misfiled-command anti-example), `Infrastructure/Startup.cs` (AddMediatR —
  user-labelled not-best-practice, analysis task), qms
  `Modules/Reports/Startup.cs` (`AddSyncDataHandler` — pattern learned, naming
  not). MediatR 12.3.0 (apsp) / 12.4.1 (qms).
- S17 user rulings: `DomainEvents/` canonical going forward; handler naming
  `<EventName>Handler` → extended to the three-armed law (suffix-replacement
  for requests; descriptive names mandatory on fan-out) after corpus evidence;
  six anti-examples labelled (misfiled request family-of-4, legacy Events/,
  descriptive-name-on-single-handler, suffix-kept `...CommandHandler`,
  log-and-rethrow in a notification handler, mixed accessibility); two
  candidates DECLINED (dead `params Assembly[]` on a registration extension;
  generic handler branching on `typeof(TData)`) — banked for rubrics.
- S17 coordinator catches: the arbiter's drop-the-naming-drift-label
  recommendation refuted by handler-count census (labelled sites are
  single-handler — label narrowed, not dropped); both P3 authors' shared
  "behaviours run in registration order" API recall cut per S16 precedent;
  `Startup`-ambiguity claim verified at 43 declarations before shipping.
- S17 delegation uses (recorded): no background-worker Not-for dangle
  (shipped-only roster); envelope-record accessibility routed to
  module-feature, handler-class accessibility kept here; Queries/ grounded by
  coordinator lookup (2 `IRequest<T>` envelopes, suffix-replacement naming);
  dispatch call-site example added; marker-type invention approved
  (`internal sealed class MessagingAssemblyMarker;` — A's class form over B's
  interface, arbiter reasons); `RegisterGenericHandlers` kept as existence
  note only; genericised logging call in anti-pattern #5; DG behaviours row
  routes to Patterns instead of teaching; `a query` router row gained a
  dispatch arm (arbiter-flagged asymmetry — a coordinator addition beyond the
  original mandate, arbiter-reviewed).
- S17 process events: blanket delegation granted at session start ("nếu có đề
  xuất gì cứ theo bạn…") — R7/R8 carve-outs held, brief confirmations
  accompanied every executed call. All three agents pinged once, continued
  across all pieces + final pass via SendMessage. `main` moved three times
  mid-session; merge-before-router-edits handled it cleanly. Target version
  renumbered twice mid-session (0.3.14 → 0.3.15 → 0.3.16) as other lanes
  shipped — read the LANE BOARD header, not this file, for the current number.
- S17 queued/unresolved: qms-backend scope (one file named; user may widen or
  close it); the seventh-anti-pattern candidate (registration/behaviours have
  no negative example — both authors + arbiter flagged); `IPipelineBehavior`
  references/ candidate if behaviours ever enter the corpus.
- **Carried from S16:** post-close incident lesson — if
  `reference/projects/*/skills/` ever appears, check `git status` of the
  PLUGIN first (the copy may be a move); `apsp-backend/skills/` is the user's
  own pre-plugin folder, leave alone. api-surface reciprocal `Not for:` route
  to automapper-mapping still open (needs an api-surface-owning session).
  automapper references/ future candidates + `dotnet-testing`'s `IMapper`
  substitutability note — in the board's PENDING log.
- **Carried from S15:** rubric-worthy principles ("a Not for: entry is a
  disclaimer, not an ownership assignment"; "a pointer earns its place only
  when it restates a boundary a shipped Not for: itself draws"); mechanism E
  (UserPromptSubmit hook → router) endorsed as small solo follow-up.
- **Carried from S14:** anti-example candidates banked for rubrics (Pattern-3
  catch filter; semaphore cleanup race); harvest lane logs + CHANGELOG before
  re-mining source when rubrics run.
- **Carried, PENDING-flavored:** S11 `CompileQueryAsync(...)` pagination
  extension (needs user to name its file); `background-worker`/
  `http-resilience` briefs @ the S14 lane-file-rewrite commit; lane-log
  consolidation into `03-session-roadmap.md` (solo chore, best after rubrics).
