> **OPENER — 2026-07-27, S16 close (Lane C).** This file opens **Lane C's S17:
> `mediatr-messaging`** — the second of the two names the user queued post-S15
> (confirm the choice with the user at session start; the rubric phase is the
> standing alternative — its own prompt file is `docs/next-session-prompt-rubrics.md`
> and Lane B's closed status lives in `docs/next-session-prompt-B.md`). Lane C's
> lane file `docs/next-session-prompt-C.md` mirrors this brief.

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever
be modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `apsp-backend` (production, canonical), `ops-service`
(reusable base), `be-booking` (anti-example quarry), `digitalcity-backend`
(older quarry, extension-only). Triage (`docs/TRIAGE.md`) is closed input.

**This is Lane C.** You own ONLY `skills/mediatr-messaging/`, the router's
merge-time edits listed below, and this file. Shipped through **v0.3.12**
(12 skills — `automapper-mapping` landed at S16). PENDING by user direction:
`auth-and-security` (Lane A's), `observability`, `background-worker`,
`http-resilience`, `domain-modeling`, `modern-csharp`, `project-scaffolding`.
The four review rubrics run as solo sessions per
`docs/next-session-prompt-rubrics.md`. Refuse and log anything outside your
ownership.

**START IN YOUR OWN WORKTREE** (proven S14/S15/S16):
`git worktree add ../dotnet-standards-lanec-s17 -b lane-c/mediatr-messaging main`.
The worktree has no `reference/` — read exemplars through the shared checkout
path `D:\agentic-plugin\dotnet-standards\reference\`.

## THE DELIVERABLE — `mediatr-messaging`

**What this skill owns:** the messaging pipeline — dispatch, pipeline
behaviours, handler registration/discovery, notification vs request semantics.
Stack fact (SETTLED): MediatR is **in-process messaging, not CQRS**.

**Boundary facts settled elsewhere (do not re-derive, do not contradict):**
`module-feature` owns the THIN ENVELOPE itself (its `Not for:` routes
"messaging pipeline — mediatr-messaging"; the router's uncovered row repeats
"the thin envelope itself belongs to `module-feature`"). `module-feature` also
owns the service-call-vs-message decision. Exception flow — `error-handling`;
validation rules — `module-feature`; message text — `message-keys`.

**ROUTER MERGE-TIME EDITS — mandatory, same session, same commit** (alignment
rule, CHANGELOG 0.3.10): delete the `Messaging pipeline: …` row from
`## Not yet covered`; decide (through the loop) the base-map row and whether
the `"message"` disambiguation row needs a third arm for the pipeline; extend
the order-note only if a row is added (S16 precedent: coordinator additions
are reviewed by the arbiter in the final pass as author content).

## THE THREE-WAY PROCESS — MANDATORY, SKILL-DRIVEN

**Invoke `three-way-skill-loop` at session start** — it defines the loop; the
main session COORDINATES ONLY (memory `author-a-delegated`). Author A =
`skill-writer-a`, Author B = `skill-writer-sp`, arbiter = `skill-arbiter`
(invokes `skill-creator:skill-creator` LIVE; `Unknown skill` → restart parent
session). Ping all three with the context package first; batch authors'
`## QUESTIONS`; drafts to the arbiter **VERBATIM — never summarized, not even
partially bracket-condensed (S16: the coordinator itself slipped here and had
to resend full texts before the verdict)**. Verify arbiter self-declared
additions; diff rephrasings (S12); verify SHARED claims (S13b; S16 caught two
shared errors: the two-level IncludeAllDerived model and the wrong
static-expression declaration shape); diff modality both directions (S13b/S15;
S16 cut two dilutions and one scope-widening). Run agents in the lane worktree.

**STANDING DELEGATION (LAW):** execute clear recommendations, report them, log
each use; ask only the genuinely undecidable. Carve-outs remain the user's
alone: naming canonical sources/exemplars (R7), labelling anti-examples (R8).
S16 refinement: when the user grants blanket delegation mid-session, brief
confirmations still accompany every executed call in the report so vetoes stay
cheap.

## READING DISCIPLINE

Ask the user for the exemplar list at session start — never select exemplars
yourself. Likely candidates (do NOT open until named): apsp/digitalcity
MediatR handlers, pipeline behaviours, `AddMediatR` wiring, notification
publishers. Widening = announced targeted lookup. Bash find/ls/grep, never
Glob, inside `reference/projects/`. **S16 caveat: `apsp-backend/.claude/worktrees/`
holds four duplicate checkouts — exclude them or every census is inflated ~5×
(the arbiter nearly hit this).** R7: one canonical source per area, never
average. R8: anti-examples are code the user points at; ask before labelling.
Sanitize: no project names, no business-domain nouns, no real paths, no
secrets. Neutral placeholder set: `Entity`/`EntityBaseResponse`/
`CreateEntityRequest`/`Wrapper` (near-domain nouns like `Item`/`Lines` were
rejected in S16).

## SETTLED — DO NOT RELITIGATE

- Everything in shipped bodies through **v0.3.12** (read them as baseline),
  incl. `automapper-mapping`'s full ruling set in CHANGELOG 0.3.12 (placement
  law generalized to declaring-file-of-source; `<DtoTypeName>Mapping`;
  projection-reachability prohibition broad/transitive with bare permission
  for non-query maps; IncludeAllDerived at every level with config to hand
  down; static shared computation = `internal static readonly Expression`
  FIELD; clean ConvertUsing form; ReverseMap unruled; PreCondition
  extension-only).
- Router rulings (CHANGELOG 0.3.10) + S16 alignment precedent: router covers
  every skill on `main` at merge time; testing swap done at S16.
- Description law (`02-repo-structure.md` §5): third person, <100 words,
  trigger-noun pushy, `Not for:` naming every owning sibling. No H1 in skill
  bodies.
- The `references/` mechanism: splits go through the loop; S16 shipped a
  single SKILL.md with recorded future candidates.
- Stack: .NET 8, Controllers not Minimal API, Swashbuckle not Scalar, NO API
  versioning, FluentValidation + AutoMapper v12 (single-arg
  `MapperConfiguration`), Redis, Elasticsearch, Hangfire; MediatR =
  in-process messaging, not CQRS.

## HARD CONSTRAINTS

1. One session, one deliverable: `mediatr-messaging` (+ mandatory router
   edits, same feat commit). Extra requests → log under `## Lane log`, refuse.
2. Prove it: validate + REAL reinstall + `claude plugin details` shows 13
   skills. **S16 install lessons:** `claude plugin install` on an installed
   plugin reports "already installed" and does NOT refresh — verify
   `installed_plugins.json`, then `claude plugin update
   dotnet-standards@dotnet-standards-dev` (short name fails); `claude plugin
   details` can read the SOURCE manifest and show the new version while the
   registry still points at the old cache — never accept it alone as proof.
   Delete `reference/` from the new cache dir (installer sweeps it; S16
   confirmed). Both manifests must agree — Lane B left `marketplace.json` at
   0.3.10 in the 0.3.11 ship; S16 fixed and aligned both at 0.3.12. Check
   `installed_plugins.json` before deleting ANY cached version dir; caches
   0.3.7–0.3.11 left unreferenced.
3. Artifact language English; talk to the user in Vietnamese.
4. End: commit per protocol (lane branch, feat commit, merge into main —
   expect mid-session `main` movement; S16 saw two moves including Lane B's
   close OVERWRITING this opener mid-session; conflict rule: keep both
   CHANGELOG entries, renumber yours above theirs). Rewrite THIS file and
   `docs/next-session-prompt-C.md` for Lane C's next session, carrying the
   Lane log.

## Lane log

- **S16 post-close incident (2026-07-27 night, resolved):** after Lane A's
  0.3.13 ship, the plugin's ENTIRE `skills/` tree was found MOVED (not copied)
  to `reference/projects/digitalcity-backend/skills/` — untracked there,
  content == HEAD modulo CRLF, mover unknown (a digitalcity-side session is
  suspected; not Lane C, which closed at ~19:43). Fixed: `git restore skills/`
  in the plugin (lossless), stray copy deleted on user order. Lesson: if
  `reference/projects/*/skills/` ever appears, check `git status` of the
  PLUGIN first — the copy may be a move. NOT the same thing:
  `apsp-backend/skills/` (user's own pre-plugin skills, dated 2026-07-07 —
  leave alone).
- **S16 post-close audit finding (open, Lane A's to fix):** the 0.3.13
  auth-and-security ship SKIPPED the router merge-time edits — the
  `Permission and identity` row still sits in `## Not yet covered` pointing
  at a skill that now loads, the `401/403` row's third arm still says *not
  yet covered*, and no base-map row exists. Violates the alignment rule
  (CHANGELOG 0.3.10). Logged here because Lane C found it; the fix belongs to
  an auth-owning or hotfix session unless the user directs otherwise.
- **S16 (automapper-mapping, 2026-07-27) — shipped v0.3.12.** Verdicts: P1
  MERGE, P2 MERGE (arbiter-corrected P4: IncludeAllDerived at every level
  with config to hand down — shared author blind spot, corpus four-level
  chain), P3 MERGE (arbiter-corrected static-expression FIELD shape — second
  shared blind spot; ReverseMap omitted, A's placement argument disproved at
  the only canonical site), P4 MERGE (two anti-patterns cut for budget,
  recovered as DG rows). Final pass PASS + 1 defect (arbiter's own `dest`/`des`
  normalization miss — fixed). No `references/`. Full rulings in CHANGELOG
  0.3.12.
- S16 exemplars (user-named): apsp `Modules/Customers/Responses/
  CustomerBaseResponse.cs` + `CustomerDetailResponse.cs`, all of
  `Modules/Devices/Requests/Devices/` + `Modules/Devices/Responses/Devices/`;
  digitalcity `DetectHistories/Requests/HandleIncidentRequest.cs`,
  `PutDetectHistoryRequest.cs`, `Response/DetailRecognitionOutcomeResponse.cs`,
  `Response/Core/ElkObjectDectectBaseResponse.cs`. Registration lookup
  (announced): `Facades/Mapping/MappingProfile.cs` (empty marker) +
  `Infrastructure/Startup.cs:52`.
- S16 user rules (verbatim doctrine): profile never in a separate folder;
  source-file placement with entity→response exception (later generalized,
  user-approved, to "file where the source type is declared" — covers the
  enum-in-request-file case); IncludeAllDerived/IncludeMembers/ProjectTo the
  common tools; maps used in ProjectTo must not use AfterMap/ConvertUsing,
  non-query maps may. User confirmed the BROAD "reachable from" reading after
  the coordinator flagged it as a scope-widening of the original wording.
- S16 anti-examples user-confirmed: profile name mismatch
  (`CustomerDefaultResponseMapping` in `CustomerDetailResponse.cs:19`; second
  family instance `DeviceBaseMapping` dropping "Response"); ForMember on
  computed get-only (`DeviceResponse.cs:19` `CurrentTransfer` + `:33`
  ForMember — coordinator-verified after arbiter greps missed the
  expression-bodied form). Shipped as anti-patterns: those two + the
  `dest = src switch` ConvertUsing no-op (verified quirk,
  `HandleIncidentRequest.cs:50`) + delegate-on-projection-reachable.
- S16 delegation uses (recorded): Not-for roster shipped-only (no
  mediatr dangle); `<DtoTypeName>Mapping` canonical (13 vs 3);
  api-surface split wording (THAT-beside-DTO theirs / WHICH-file ours);
  IncludeMembers grounded by coordinator grep (14 sites, shape-only use);
  rule-1 generalization; inline routing pointers kept; MappingProfile name
  kept; version note in Patterns only; prohibition NOT extended to
  Condition/PreCondition (delegates too, but user doctrine names two — a
  future-session candidate if the user wants it); ConvertUsing two-param kept
  for corpus fidelity; 17th DG row dropped; references/ not needed;
  base-map row + order-note "mapping" insertion added by coordinator,
  arbiter-reviewed in final pass.
- S16 loop catches: three B naming drifts (`<DestinationTypeName>`, ×3,
  arbiter-corrected each); B re-imported its own P2-cut dilution sentence in
  P3 (coordinator caught pre-verdict); A's AddCollectionMappers "every
  collection map" overreach (verified: 9 opt-in `EqualityComparison` sites);
  A's single-param ConvertUsing inconsistency; both authors' two-level
  IncludeAllDerived model and wrong static-expression shape (shared blind
  spots, corpus-corrected); arbiter's moot enum-placement flag (coordinator
  corrected — generalized rule covers it); IncludeMembers ordering semantics
  asserted by both from API memory — refused into the artifact, recorded as
  the strongest references/ candidate.
- S16 process events: coordinator VIOLATED the forward-verbatim rule at P4
  (bracket-condensed sections), self-caught, resent full texts before the
  verdict — rule now hardened above. User granted blanket delegation
  mid-session ("làm theo khuyến nghị, chỉ hỏi khi thật sự cần") — R7/R8
  carve-outs held. All three agents pinged once and continued across all four
  pieces via SendMessage without respawn.
- S16 queued/unresolved: api-surface reciprocal `Not for:` route to
  automapper-mapping (its description claims "colocated validator and mapping
  profile" — outside Lane C ownership, needs an api-surface-owning session);
  references/ future candidates (troubleshooting catalogue; IncludeMembers
  precedence semantics; value/type-converter material); `dotnet-testing`'s
  `IMapper` substitutability note untouched.
- **Carried from S15:** router serves sibling disambiguation AND the
  process-phase gap; rubric-worthy principles ("a Not for: entry is a
  disclaimer, not an ownership assignment"; "a pointer earns its place only
  when it restates a boundary a shipped Not for: itself draws"); mechanism E
  (UserPromptSubmit hook → router) endorsed as small follow-up session.
- **Carried from S14:** CHANGELOG 0.3.8 rulings; anti-example candidates
  banked for rubrics (Pattern-3 catch filter; semaphore cleanup race);
  harvest lane logs + CHANGELOG before re-mining source when rubrics start.
- **Carried, PENDING-flavored:** S11 `CompileQueryAsync(...)` pagination
  extension (needs user to name its file); `background-worker`/
  `http-resilience` briefs @ the S14 lane-file-rewrite commit; roadmap/index
  stale references to "Facades/Cache in one or both projects" — consolidate
  when lane logs fold into `03-session-roadmap.md`.
