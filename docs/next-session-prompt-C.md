# Lane C — Infrastructure Services · Session C4: `choosing-a-dotnet-skill` (S15, REPRIORITIZED)

> Copy everything below the line into a fresh Claude Code session in
> `D:\ALTA\Project\dotnet-standards`. Lane C runs in parallel with lanes A and B —
> read `next-session-prompt.md` (the index) for the parallel protocol, which binds
> this session. Written at the close of S14, 2026-07-27, then REWRITTEN the same
> day when the user reprioritized: ship the lean plugin first. `background-worker`
> and `http-resilience` are PENDING; this lane's next deliverable is the ROUTER.

---

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever be
modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored) — **this session should not need to open them at all**: the
router's source material is the plugin itself. Triage (`docs/TRIAGE.md`) is closed
input.

**This is Lane C of three parallel lanes.** You own ONLY
`skills/choosing-a-dotnet-skill/` and this file. Lane B's B4 is now
`dotnet-testing` (promoted alongside this router — may run concurrently). Lane A
finishes `ef-core-data-access` (in flight), then stops. PENDING by user direction
(2026-07-27): `auth-and-security`, `observability`, `background-worker`,
`http-resilience`, `domain-modeling`, `modern-csharp`, `project-scaffolding`.
After this router and `dotnet-testing` ship, **the four review rubrics run next**.
Refuse and log anything outside your ownership.

**START IN YOUR OWN WORKTREE.** Proven in S14:
`git worktree add ../dotnet-standards-lanec-s15 -b lane-c/choosing-a-dotnet-skill main`
and work there. The worktree has no `reference/` (gitignored) — irrelevant this
session unless the user names something.

## THE DELIVERABLE — `choosing-a-dotnet-skill`

**What this skill owns:** the ROUTER — mechanism D from `00-brainstorm.md` §3
(adopted). A decision table that takes the situation a user or agent is in and
points at the right gateway skill. It exists because description-matching alone
under-triggers; the router is the deterministic-ish fallback an agent can load
when unsure which sibling owns a question.

**Source material (no exemplar list needed — but confirm at session start):**
the INSTALLED skill bodies and their descriptions — at S14's close that is 8 real
skills (`facade-module-architecture`, `api-surface`, `error-handling`,
`message-keys`, `module-feature`, `distributed-caching`, `elasticsearch-search`,
`distributed-lock`) plus whatever has shipped since (check `main`'s `skills/` and
CHANGELOG first: Lane A's `ef-core-data-access` and Lane B's `dotnet-testing` may
land mid-session — the table must cover every skill actually on `main` at merge
time, per the alignment rule). The `Not for:` lists already encode pairwise
routing; the router composes them into one table and resolves the situations the
pairwise lists cannot (three-way overlaps, "I don't know the noun yet" entries).

**Design constraints queued for this session:**
1. Rows for PENDING skills: the table routes ONLY to skills that exist. Where a
   shipped skill's `Not for:` names a pending sibling (`background-worker`,
   `auth-and-security`, `observability`), the router needs a ruled answer for
   what to say — recommend: an honest "not yet covered by this plugin" row, never
   a phantom target. Put the options to the user with a recommendation.
2. `dotnet-testing` row: if Lane B has not merged yet, draft the row from their
   in-flight description and align wording at merge time (index caveat).
3. The router must not contradict the description law — it supplements
   descriptions, it does not replace them; keep it a decision table, not a
   summary of each skill's content (no summary-shaped text an agent could follow
   instead of loading the skill).
4. Description of the router itself still follows §5 law (third person, <100
   words, `Not for:` — what does the ROUTER exclude? Recommend: process-layer
   questions → Superpowers; anything already confidently matched → load that
   skill directly).

**Not this skill:** the four review rubrics (next phase); every content area the
gateway skills own (route, don't teach); the `UserPromptSubmit` hook variant
(mechanism E — deferred Group B, do not build it here).

## THE THREE-WAY PROCESS — MANDATORY

Per piece, not per skill: (1) you explain first in Vietnamese, I comment; (2) you
(author A: loads `docs/02-repo-structure.md` §5 + `docs/00-brainstorm.md` §3 + the
kit's skill format — NOT superpowers:writing-skills) and `skill-writer-sp` (author
B) draft independently, neither writes files; (3) `skill-arbiter` verdicts
A/B/MERGE/NEITHER with file-verified reasons (verification here = against the
installed skill bodies, not reference/projects); (4) approval per the standing
delegation below; only then write. Both agents exist in `.claude/agents/` —
verify with a ping; the ping doubles as the context-package load; continue via
SendMessage. **The arbiter MUST invoke `skill-creator:skill-creator` live**
(installed user-scope 2026-07-27; subagent skill rosters snapshot at parent
session start — if it reports `Unknown skill`, restart the parent session; the
user rejected disk-read fallbacks in S14). Announce every agent use; relay
milestones; agents end with `## QUESTIONS`. Run agents in the lane worktree — no
nested worktrees for subagents.

**STANDING DELEGATION (ruled S14, LAW — memory `delegate-on-recommendation`):**
when you present a decision WITH a clear recommendation, execute it and report it
done with the reasoning; ask ONLY when genuinely undecidable. No per-piece
approval gates. **Carve-out: naming canonical sources/exemplars remains the
user's alone (R7).** Record each delegation use in the Lane log.

**Process notes that keep working (S10–S14):** batch the authors' `## QUESTIONS`
and resolve them BEFORE dispatching the arbiter; drafts live in the session
scratchpad until final; arbiter prompts carry draft file paths plus every ruling;
while one agent runs in the background, write the other draft. Diff every arbiter
rephrasing against the original before writing (S12 lesson; S14 re-confirmed —
the arbiter caught real author errors and introduced none, but the duty stays).

## READING DISCIPLINE

This session's corpus is the plugin itself — read installed skills freely. For
anything in `reference/projects/`: ask first, as always; widening = announced
targeted lookup. Sanitize as always (S14 caught source business nouns slipping
into a draft — the rule is always load-bearing).

## SETTLED — DO NOT RELITIGATE

- Everything shipped through v0.3.8 (read the installed skills; do not contradict
  any `Not for:` routing — the router COMPOSES them, it does not re-litigate
  ownership; a boundary the descriptions already draw is settled).
- Description law (`02-repo-structure.md` §5) — binds the router's own
  description too.
- The `references/` mechanism: decision-layer body, depth in references with
  conditional pointers. Whether the router needs references at all is an open
  design question — a single-table SKILL.md may be the right shape; put it
  through the loop.
- My stack: Controllers not Minimal API, Swashbuckle not Scalar, NO API
  versioning, FluentValidation + AutoMapper, Redis, Elasticsearch, Hangfire;
  MediatR is in-process messaging, not CQRS.

## HARD CONSTRAINTS

1. One session, one deliverable: `choosing-a-dotnet-skill` only. Extra requests →
   log under `## Lane log` and refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new skill
   count; report failures honestly. Merge/version protocol: patch bump +1 vs
   whatever `main` carries at merge time, both manifests agree, CHANGELOG at top,
   one install at a time — with Lane B's `dotnet-testing` possibly mid-flight,
   EXPECT a renumber and a CHANGELOG conflict; the rule (keep both entries,
   renumber yours above) handled it painlessly in S11 and S14.
   **Install-state at S14's close:** active install USER-scope
   `dotnet-standards 0.3.8` from marketplace `dotnet-standards-dev` (source = the
   shared checkout directory); `reference/` deleted from the 0.3.8 cache copy;
   cache 0.3.7 left unreferenced; ops-service holds a local-scope install pinned
   to cache 0.3.2 — check `installed_plugins.json` before deleting ANY cached
   version dir. The directory-copying installer sweeps untracked WIP from the
   shared checkout (0.3.7/0.3.8 both carry Lane A's `ef-core-data-access`
   scaffold; harmless). Details reported Skills (9) at 0.3.8: 8 real + 1 swept.
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol (lane branch `lane-c/choosing-a-dotnet-skill`, feat
   commit, merge into main). Then rewrite THIS file so it opens Lane C's next
   session — per the reprioritized order that is **the first review rubric the
   user assigns to this lane** (confirm which at session end), carrying the Lane
   log forward.

## Lane log

- **S14 addendum (2026-07-27, post-ship):** user REPRIORITIZED the whole roadmap
  at S14's close (recorded here as the lane-ownership exception it is — the
  reorder touched the index, the roadmap, and Lane B's two prompt files by
  explicit user direction): `dotnet-testing` → Lane B (B4),
  `choosing-a-dotnet-skill` → Lane C (this session); everything else unshipped is
  PENDING; the four review rubrics run immediately after the promoted pair. The
  user's goal: ship the lean plugin first. Rules the pending skills would have
  owned fold into the review phase later if still wanted. Lane assignment of the
  promoted pair was decided under the standing delegation (router → C because
  Lane C carries the densest shipped routing surface; testing → B because the
  research variant is self-contained and B was ready to start).
- **S14 (distributed-lock, 2026-07-27) — shipped v0.3.8.** Three-way process,
  4 pieces. Verdicts: P1 MERGE, P2 MERGE B-dominant, P3 MERGE B-dominant,
  P4 MERGE B-dominant. User adjudicated through P3; standing delegation granted
  mid-P4 and elevated to law. skill-creator provenance: two arbiter threads had
  to fall back to disk-reads (subagent skill roster snapshots at parent-session
  start); user restarted the parent session and the third arbiter thread invoked
  it live — that thread arbitrated all four pieces.
- S14 exemplars used (user-named): apsp
  `Infrastructure/Facades/Common/Services/ConcurrencyHandlers/` (all 4 files) +
  `Core/Common/Exceptions/LockedException.cs` + the three production call sites
  (redeem-shaped, payment-shaped multi-key, history-shaped compensation) +
  `Infrastructure/Startup.cs` composition line. User-approved targeted lookups:
  `ValidatorExtension.Required()` body (verified: non-string properties compare
  against `Activator.CreateInstance` type default → initializers satisfy
  validation when the section is absent); Infrastructure csproj (verified:
  `RedLock.net` 2.3.2, `StackExchange.Redis` 2.6.122 pinned).
- **S14 rulings recorded for reuse** (full list in CHANGELOG 0.3.8): KeyedLocker
  scrubbed entirely; both providers taught, SemaphoreSlim single-instance-only,
  in-memory path ignores options and never throws LockedException; connection
  from the extracted `RedisSettings` section (deviation, cache-capability
  prerequisite); `ConcurrencySettings.Provider` dead config, honest note, no
  fallback; semaphore cleanup race honest-note-only; normalizations: filename
  typo, VN→EN docs, `RedLock`→`RedLockAsync`, Pattern-3 filter
  `+ and not LockedException`; ExpiryTime taught as documented intent with
  auto-renewal explicitly unverified (RedLock.net 2.3.2 — ships as an honestly
  scoped unknown); placement asymmetry (lock `Common/Services/`, cache
  `Common/`) named once, both canonical; keys `{Noun}:{id}`, two ids legal for
  pair-resources, bare-Guid call site = drift noted once; version pins named in
  the artifact as "the canonical pins X"; `ConcurrencyHandler` stays public
  non-sealed.
- S14 anti-example candidates (flagged, unlabelled, banked for the rubrics —
  which are now NEXT): the canonical Pattern-3 catch filter
  (`when (ex is not BadRequestException)` wrapping `LockedAsync`) compensates
  work that never started and downgrades 423→500 — shipped as an authorized
  normalization; the rubric-shaped rule is "a filter that converts status must
  exclude exceptions that already carry one". Plus the semaphore-registry
  cleanup race. S13's four unruled candidates live in Lane B's log / CHANGELOG
  0.3.4. Harvest lane logs + CHANGELOG before re-mining source when the rubrics
  start.
- S14 cross-lane events: Lane B shipped S13b (`message-keys` v0.3.7) and rewrote
  `CLAUDE.md` mid-session; Lane C renumbered to 0.3.8 and merged fast-forward
  with zero conflicts (worktree isolation worked — contrast S11). Lane A's
  `ef-core-data-access` scaffold remains untracked in the shared checkout;
  untouched by Lane C.
- **Carried, now PENDING-flavored:** S11 deferred `CompileQueryAsync(...)`
  pagination extension (out of scope until the user names its file — if it ever
  ships it belongs to `elasticsearch-search` as a targeted addition, or to a
  review rubric). `background-worker` and `http-resilience` briefs from the
  pre-reprioritization version of this file live in git history (@ the S14
  lane-file-rewrite commit); resurrect from there when the user unfreezes them.
- Deferred to a solo session (not Lane C's to fix): the roadmap/index still say
  "Facades/Cache in one or both projects" for S10 and list ops-service/apsp as
  the only reference projects — superseded by S10/S11 designations; consolidate
  when the lane logs fold into `03-session-roadmap.md`.
- **S15 mid-session user direction (2026-07-27): the user wants
  `automapper-mapping` and `mediatr-messaging` built after this session.** Both
  were dangled by shipped `Not for:` lists with no roadmap row until now; queue
  them into the post-S15 prioritization next to the review rubrics (user to
  order at S15 close). The router still ships them as *not yet covered* rows —
  the artifact draws no pending-vs-unplanned distinction by ruling.
- S15 mechanism E note: the user endorsed reviving the `UserPromptSubmit`
  router-pointer hook (mechanism E, deferred Group B) as a follow-up after the
  router ships; small session (a hooks/hooks.json entry + one inject line).
  The router is written hook-friendly by design.
