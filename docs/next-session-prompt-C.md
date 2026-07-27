# Lane C — Infrastructure Services · Session C5: `automapper-mapping` (S16)

> Copy everything below the line into a fresh Claude Code session in
> `D:\agentic-plugin\dotnet-standards`. Lane C runs in parallel with other
> lanes — read `next-session-prompt.md` (the index) for the parallel protocol,
> which binds this session. Written at the close of S15, 2026-07-27, after the
> router shipped (v0.3.10) and the user assigned `automapper-mapping` to Lane C
> by explicit choice (the rubric phase runs as four SOLO sessions per
> `next-session-prompt-rubrics.md`, no longer lane-assigned).

---

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever
be modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `ops-service`, `apsp-backend` (canonical),
`be-booking` (anti-example quarry), `digitalcity-backend` (older, call-site
quarry / extension-only). Triage (`docs/TRIAGE.md`) is closed input.

**This is Lane C.** You own ONLY `skills/automapper-mapping/` , the router's
merge-time edits listed below, and this file. Lane B's `dotnet-testing` may
still be in flight; Lane D (process integration) exists (docs-only so far);
Lane A stopped. PENDING by user direction: `auth-and-security`,
`observability`, `background-worker`, `http-resilience`, `domain-modeling`,
`modern-csharp`, `project-scaffolding`, and now-queued `mediatr-messaging`
(user direction 2026-07-27, after this skill). The four review rubrics run as
solo sessions per their own prompt file. Refuse and log anything outside your
ownership.

**START IN YOUR OWN WORKTREE.** Proven S14/S15:
`git worktree add ../dotnet-standards-lanec-s16 -b lane-c/automapper-mapping main`
and work there. The worktree has no `reference/` (gitignored) — read
`reference/projects/` through the shared checkout path
`D:\agentic-plugin\dotnet-standards\reference\` when the user names exemplars.

## THE DELIVERABLE — `automapper-mapping`

**What this skill owns:** mapping mechanics — writing an AutoMapper profile and
its conventions, profile registration/discovery, mapping configuration. It is
one of the two names the user queued post-S15 (with `mediatr-messaging`); until
S15 it was dangled by shipped `Not for:` lists with no roadmap row.

**Boundary facts already settled elsewhere (do not re-derive, do not
contradict):** `ef-core-data-access` owns *projecting inside a query*
(`ProjectTo`) — its `Not for:` disclaims "mapping profiles — automapper-mapping".
`api-surface` owns *where the profile file sits beside its DTO* (colocated
validator and mapping profile). `module-feature`'s `Not for:` disclaims
"mapping mechanics — automapper-mapping". The router (`choosing-a-dotnet-skill`
v0.3.10) encodes all three in its `mapping / ProjectTo` disambiguation row and
its `Mapping mechanics` uncovered row.

**ROUTER MERGE-TIME EDITS — mandatory, same session, same commit** (the
alignment rule: the router must cover every skill on `main` at merge time; the
maintenance duty is recorded in CHANGELOG 0.3.10):
1. Delete the `Mapping mechanics: …` row from the router's `## Not yet covered`
   table.
2. In the router's `mapping / ProjectTo` disambiguation row, change the third
   arm `how to write the mapping itself — *not yet covered*` to
   `how to write the mapping itself — automapper-mapping`.
3. Decide (through the loop) whether a base-map row is warranted; if added,
   extend the build-sequence order note accordingly.
4. If Lane B merged `dotnet-testing` by then, also perform the pre-written
   testing swap (CHANGELOG 0.3.10 lists the three edits) unless Lane B already
   did it — check first.

## THE THREE-WAY PROCESS — MANDATORY, NOW SKILL-DRIVEN

**Invoke the `three-way-skill-loop` skill at session start and follow it** — it
now defines the loop. Division of labour changed at S15's close (memory
`author-a-delegated`): the main session COORDINATES ONLY; **`skill-writer-a`**
(new agent in `.claude/agents/`) drafts side A, `skill-writer-sp` drafts side B,
`skill-arbiter` verdicts A/B/MERGE/NEITHER with file-verified reasons and MUST
invoke `skill-creator:skill-creator` live (if it reports `Unknown skill`,
restart the parent session — subagent rosters snapshot at parent start; the
user rejects disk-read fallbacks). Ping all three agents with the context
package first; batch authors' `## QUESTIONS` and resolve before dispatching the
arbiter; drafts live in the session scratchpad; announce every agent use; run
agents in the lane worktree (no nested worktrees). Diff every arbiter
rephrasing against the original (S12), verify shared claims (S13b), diff
modality both directions (S13b/S15), and verify the arbiter's self-declared
additions like any author claim — in S15 the arbiter caught the main session's
own falsely-justified H1, so the duty is symmetric.

**STANDING DELEGATION (LAW — memory `delegate-on-recommendation`):** present a
decision WITH a clear recommendation → execute it and report; ask only when
genuinely undecidable. Carve-out: naming canonical sources/exemplars remains
the user's alone (R7). Record each use in the Lane log.

## READING DISCIPLINE

Ask the user for the exemplar list at session start — never select exemplars
yourself. Likely candidates the user may name (do NOT open until named): apsp
mapping profiles, ops-service profiles, be-booking as anti-example quarry.
Widening = announced targeted lookup. Bash find/ls/grep, never Glob, inside
`reference/projects/`. R7: one canonical source per area, user designates,
never average. R8: anti-examples are code the user points at; ask before
labelling. Sanitize: no project names, no business-domain nouns, no real
paths, no secrets in artifacts (S14 caught business nouns slipping into a
draft; always load-bearing).

## SETTLED — DO NOT RELITIGATE

- Everything shipped through **v0.3.10** (read the installed bodies): all
  rulings in CHANGELOG 0.3.7–0.3.10, notably the router's full ruling set —
  route-don't-teach, single entry-point, compose-never-amplify,
  disclaimer-not-ownership principle, body-sourced tokens allowed in
  disambiguation, no section-heading pointers.
- Description law (`02-repo-structure.md` §5): third person `This skill should
  be used when…`, <100 words, trigger-noun pushy, `Not for:` naming every
  sibling that owns an excluded area. This skill's `Not for:` must at minimum
  route: ProjectTo-in-query — ef-core-data-access; profile placement —
  api-surface; feature internals — module-feature; and the router.
- The `references/` mechanism: decision-layer body, depth in references with
  conditional pointers; whether this skill needs references/ goes through the
  loop.
- Stack: Controllers not Minimal API, Swashbuckle not Scalar, NO API
  versioning, FluentValidation + AutoMapper, Redis, Elasticsearch, Hangfire;
  MediatR is in-process messaging, not CQRS.

## HARD CONSTRAINTS

1. One session, one deliverable: `automapper-mapping` (+ the mandatory router
   merge-time edits above, which ship in the same feat commit). Extra requests
   → log under `## Lane log` and refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new
   skill count (11 if dotnet-testing has not merged; 12 if it has); report
   failures honestly. Merge/version protocol: patch bump +1 vs whatever `main`
   carries at merge time, both manifests agree, CHANGELOG at top, one install
   at a time; conflict rule: keep both entries, renumber yours above theirs.
   **Install-state at S15's close:** USER-scope `dotnet-standards 0.3.10`
   from marketplace `dotnet-standards-dev` (source = shared checkout dir);
   `reference/` deleted from the 0.3.10 cache; caches 0.3.7–0.3.9 left
   unreferenced — check `installed_plugins.json` before deleting ANY cached
   version dir (S12 incident); ops-service's local-scope pin assumed alive.
   If the marketplace registration vanishes (S13b incident):
   `claude plugin marketplace add ./` then install.
3. Artifact language English; talk to the user in Vietnamese.
4. End: commit per protocol (lane branch `lane-c/automapper-mapping`, feat
   commit, merge into main — expect mid-session `main` movement). Then rewrite
   THIS file so it opens Lane C's next session (likely `mediatr-messaging` —
   confirm with the user at close), carrying the Lane log forward.

## Lane log

- **S15 (choosing-a-dotnet-skill, 2026-07-27) — shipped v0.3.10.** Verdicts:
  P1 MERGE, P2 MERGE, P3 MERGE; final consistency pass PASS with one defect
  (the main session's own falsely-justified H1 — deleted; 8/9 siblings open at
  `##`, only message-keys carries an H1) + note 1 applied (`## How to use
  these tables`), note 2 declined (kept the `—` empty-name cells), note 3
  no-op. Full ruling list in CHANGELOG 0.3.10.
- S15 goal amendment (user): the router serves TWO users — sibling
  disambiguation AND the process-phase gap (generic prompts like "implement
  feature A" surface trigger nouns only inside specs/plans/subagent prompts,
  where no skill is looking; the router carries the body-scoped obligation:
  steps touching a shipped skill's area MUST name that skill).
- S15 delegation uses (standing delegation, recorded): pending-row treatment
  (topic-noun rows, dangled names printed as reservations-not-loadable, both
  populations, ~10 rows uniform); process-phase = pattern with 3 Superpowers
  phases as e.g.; obligation modality body-scoped (must for shipped-owned,
  relief for uncovered + permission clause "say so in the step if it helps
  the plan"); no section pointers; single SKILL.md no references/; single
  entry-point per row; ConcurrencySettings body-sourcing precedent; ASP.NET
  Core anchor kept; P2 preamble amendment accepted; domain-modelling and
  observability nearest-help pointers dropped (over-read risk); rubric-worthy
  arbiter principles adopted: "a Not for: entry is a disclaimer, not an
  ownership assignment" and "a pointer earns its place only when it restates
  a boundary a shipped Not for: itself draws".
- S15 errors caught by the loop (all fixed pre-write): author A's
  whole-feature row contradicted facade-module-architecture's literal
  description (B's conditional adopted); author B's P1 dropped brainstorming
  from the three ruled phases; B's Repository<T>() evidence misattributed
  (descriptions → bodies; collision real); B's "record that in the step"
  modality drift cut, offered back and adopted as permission; arbiter
  self-corrected its own Redis prep (live ambiguity is cache vs RedLock);
  main session's H1 provenance claim false (caught in final pass).
- S15 census work now in the artifact: six names dangled by shipped `Not for:`
  lists (auth-and-security, observability, background-worker,
  automapper-mapping, mediatr-messaging, project-scaffolding) + four name-less
  pending areas (testing, HTTP resilience, domain modelling, modern C#).
- S15 process events: pings doubled as context-package loads; skill-creator
  invoked live by the arbiter on first try (no restart needed this session);
  mid-session the user created the `three-way-skill-loop` skill and
  `skill-writer-a` agent and saved memory `author-a-delegated` — S16 onward
  the main session coordinates only. Cross-lane: Lane D (process integration)
  opened mid-session; rubric phase re-planned as four solo sessions
  (`next-session-prompt-rubrics.md`); ef-core-data-access v0.3.9 had landed
  before S15 opened (base map built against nine skills).
- S15 user directions logged mid-session: `automapper-mapping` and
  `mediatr-messaging` wanted post-S15 (automapper → this session, S16, by
  user choice at close; mediatr queued); mechanism E (UserPromptSubmit hook
  pointing at the router) endorsed as a small follow-up session — the router
  is written hook-friendly.
- **Carried from S14:** rulings in CHANGELOG 0.3.8 (KeyedLocker scrubbed, two
  providers, RedisSettings connection deviation, ExpiryTime honestly-scoped
  unknown, etc.); anti-example candidates banked for the rubrics (Pattern-3
  catch filter compensating unstarted work + downgrading 423→500; semaphore
  cleanup race); S13's four unruled candidates live in Lane B's log /
  CHANGELOG 0.3.4; harvest lane logs + CHANGELOG before re-mining source when
  the rubrics start.
- **Carried, PENDING-flavored:** S11 deferred `CompileQueryAsync(...)`
  pagination extension (needs the user to name its file; belongs to
  `elasticsearch-search` or a rubric). `background-worker`/`http-resilience`
  briefs live in git history @ the S14 lane-file-rewrite commit.
- Deferred to a solo session (not Lane C's to fix): roadmap/index still say
  "Facades/Cache in one or both projects" for S10 and list ops-service/apsp
  as the only reference projects — superseded; consolidate when lane logs
  fold into `03-session-roadmap.md`.
