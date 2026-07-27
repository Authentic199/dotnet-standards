# Lane C — Infrastructure Services · Session C4: `background-worker` (S15)

> Copy everything below the line into a fresh Claude Code session in
> `D:\ALTA\Project\dotnet-standards`. Lane C runs in parallel with lanes A and B —
> read `next-session-prompt.md` (the index) for the parallel protocol, which binds
> this session. Written at the close of S14, 2026-07-27.

---

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever be
modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `ops-service` (reusable base), `apsp-backend` (production;
canonical for the search area per S11 and the lock area per S10/S14),
`be-booking` (anti-example quarry only), `digitalcity-backend` (canonical for the
cache area per S10; introduced by the user to Lane B in S13b as a call-site
quarry; other areas need my designation). Triage (`docs/TRIAGE.md`) is closed
input.

**This is Lane C of three parallel lanes.** You own ONLY
`skills/background-worker/` and this file. Lane A owns `module-feature` (shipped
v0.3.3), `ef-core-data-access` (in flight — untracked scaffold in the shared
checkout), `domain-modeling`, `modern-csharp`. Lane B owns `api-surface`
(v0.3.2), `error-handling` (v0.3.4), `message-keys` (v0.3.7),
`auth-and-security` (opens as B4). The router, testing, scaffolding and review
rubrics are excluded from all lanes. Refuse and log anything outside your
ownership.

**START IN YOUR OWN WORKTREE.** Proven again in S14 — the shared checkout's
branch belongs to whoever grabbed it, and lanes commit mid-session:
`git worktree add ../dotnet-standards-lanec-s15 -b lane-c/background-worker main`
and work there. Note the worktree has no `reference/` (gitignored) — read
exemplars via absolute paths into the shared checkout.

## THE DELIVERABLE — `background-worker`

**What this skill owns:** Hangfire conventions — recurring/queued/delayed jobs,
job registration and placement, the worker capability's facade anatomy, retry
and idempotency doctrine for jobs, and the scaffold-if-missing mechanism (same
guard/prerequisite idiom as the three shipped Lane C skills — read all three
installed bodies before drafting; they are the house style).

**Exemplars: NOT YET NAMED.** Ask me for the list at session start before
reading anything in `reference/projects/`. Likely candidates I may name
(do NOT open unasked): apsp-backend's Hangfire wiring in Infrastructure, job
classes in modules, dashboard/auth configuration in Web, `RedisSettings` reuse
if Hangfire rides Redis storage. Wait for my list.

**Not this skill:** distributed locks, `ConcurrencyHandlers` (`distributed-lock`,
shipped v0.3.8 — jobs that need mutual exclusion route there); caching
(`distributed-caching` v0.3.6); search (`elasticsearch-search` v0.3.5); data
access (Lane A); HTTP/auth surface (Lane B); the Serilog sink (`observability`,
unassigned).

## THE THREE-WAY PROCESS — MANDATORY

Per piece, not per skill: (1) you explain first in Vietnamese, I comment; (2) you
(author A: loads `docs/02-repo-structure.md` §5 + `docs/00-brainstorm.md` §3 + the
kit's skill format — NOT superpowers:writing-skills) and `skill-writer-sp` (author
B) draft independently, neither writes files; (3) `skill-arbiter` verdicts
A/B/MERGE/NEITHER with file-verified reasons; (4) approval per the standing
delegation below; only then write. Both agents exist in `.claude/agents/` —
verify with a ping before relying on them; the ping doubles as the
context-package load, and the same agents continue across pieces via SendMessage.
**The arbiter MUST invoke `skill-creator:skill-creator` live** (installed
user-scope 2026-07-27). A subagent's skill roster is snapshotted from the parent
session's startup state — if the arbiter reports `Unknown skill`, restart the
parent session; do not accept disk-read fallbacks (S14: the user rejected them
and restarted). Agent prompts must carry: the exemplar list I name, all relevant
settled rulings, and equal-source-access discipline. Announce every agent use;
relay milestones; agents end with `## QUESTIONS`. Run agents in the lane
worktree — no nested worktrees for subagents.

**STANDING DELEGATION (ruled S14, now LAW — memory `delegate-on-recommendation`):**
when you present a decision WITH a clear recommendation, execute it and report it
done with the reasoning; ask ONLY when genuinely undecidable. No per-piece
approval gates. **Carve-out the user kept: naming canonical sources/exemplars
remains the user's alone (R7) — never self-select, never average.** Record each
delegation use in the Lane log.

**Process notes that keep working (S10–S14):** batch the authors' `## QUESTIONS`
and resolve them (by delegation or by asking) BEFORE dispatching the arbiter, so
verdicts land ruling-complete; drafts live in the session scratchpad (never
`skills/`) until final; arbiter prompts carry draft file paths plus every ruling
including fresh ones; while one agent runs in the background, write the other
draft and pre-read the next piece's exemplars. **Diff every arbiter rephrasing
against the original before writing (S12 lesson) — S14 confirmed both sides: the
arbiter caught real author errors (a nesting-consequence both authors got wrong,
a sanitization violation, two dead normalization-table rows) and introduced none,
but the diff duty stays.** S14 new lessons: (a) targeted lookups the authors
request (one method body, one csproj line) are cheap and convert inferred claims
into verified ones — approve them; (b) when a canonical call-site defect
interacts with the skill's own doctrine (S14: a catch filter swallowing
LockedException), the resolution ladder is: show-canonical-with-pointer /
user-labels-anti-example / user-authorizes-normalization — S14 chose
normalization and the file must then be self-consistent.

## READING DISCIPLINE

I confirm the exemplar list at session start — ask before reading anything in
`reference/projects/`; never select exemplars yourself. Widening = targeted
lookup, announced (S14: user approved every requested lookup; keep them
one-file-one-claim sized). No bulk scans. Bash find/ls/grep, never Glob, inside
`reference/projects/`. R7: one canonical source per area, I designate; never
average. R8: anti-examples are code I point at; ask before labelling. Sanitize:
no project names, no business-domain names (S14 caught `Coupon`/`Referral`
slipping into a draft — the source's business nouns are ALWAYS forbidden, even
as lock-key helper names), no real paths, no secrets; config JSON holds
credentials — list file NAMES only, never open them.

## SETTLED — DO NOT RELITIGATE

- Everything shipped in `facade-module-architecture` v0.3.0,
  `distributed-caching` v0.3.6, `elasticsearch-search` v0.3.5 and
  `distributed-lock` v0.3.8 (read the installed skills; their
  guard/prerequisite/deviation idioms are the house style). Notable
  cross-skill facts from 0.3.8: the extracted `RedisSettings` section is shared
  substrate (cache owns it, lock reads it — if Hangfire rides Redis storage,
  the same sharing question arises and the answer is the same section);
  `LockedException`/423 and the `ConcurrencyHandlers` capability are
  `distributed-lock`'s — a job that needs mutual exclusion injects
  `IConcurrencyHandler`, it does not grow its own.
- Description law (`02-repo-structure.md` §5): third person `This skill should
  be used when…`, <100 words, trigger-noun pushy, `Not for:` routing list naming
  every sibling that owns an excluded area. All three shipped Lane C skills
  route job nouns (`background jobs, Hangfire`) to `background-worker` — this
  skill must route back.
- The `references/` mechanism: decision-layer body, depth in references with
  conditional pointers; two references files per scaffolding skill is the Lane C
  norm (implementation.md + usage-patterns.md).
- Internal rule numbers never appear in artifacts; a scaffolding skill states a
  generic pre-scaffold guard and STOP prerequisites rather than asserting what
  existing folders contain; deviations from canonical are marked honestly at the
  exact spot; typos and broken behavior in canonical get corrected in the
  scaffold with an honest normalization row (S11 `Querry`; S14 filename typo,
  VN→EN docs, `RedLockAsync` rename, the Pattern-3 catch filter).
- My stack: Redis, Elasticsearch, Hangfire; MediatR is in-process messaging, not
  CQRS.

## HARD CONSTRAINTS

1. One session, one deliverable: `background-worker` only. Extra requests → log
   under `## Lane log` and refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new skill
   count; report failures honestly. Follow the index's merge/version protocol
   (patch bump +1 vs whatever main carries at merge time, both manifests agree,
   CHANGELOG at top, one install at a time — wait if another lane is
   mid-install). **Install-state at S14's close:** active install is USER-scope
   `dotnet-standards 0.3.8` from marketplace `dotnet-standards-dev` (source =
   the shared checkout directory); `reference/` deleted from the 0.3.8 cache
   copy; cache 0.3.7 left in place unreferenced (S13 precedent); ops-service
   still holds a local-scope install pinned to cache 0.3.2 — check
   `installed_plugins.json` before deleting ANY cached version dir. The
   directory-copying installer sweeps untracked WIP from the shared checkout
   into the cache (0.3.7 and 0.3.8 both carry Lane A's `ef-core-data-access`
   scaffold; harmless — their install supersedes). `claude plugin details`
   therefore reports Skills (9) at 0.3.8: 8 real + 1 swept.
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol (lane branch `lane-c/background-worker`, feat
   commit, merge into main — expect mid-session `main` movement; conflict rule:
   keep both CHANGELOG entries, renumber yours above theirs, align cross-skill
   names that changed under you). Then rewrite THIS file so it opens Lane C's
   next session (per the roadmap queue: `http-resilience`, unless the user
   re-orders), carrying the Lane log forward.

## Lane log

- **S14 (distributed-lock, 2026-07-27) — shipped v0.3.8.** Three-way process,
  4 pieces. Verdicts: P1 MERGE, P2 MERGE B-dominant, P3 MERGE B-dominant,
  P4 MERGE B-dominant. User adjudicated through P3; standing delegation granted
  mid-P4 and elevated to law (see process section). skill-creator provenance:
  two arbiter threads had to fall back to disk-reads (subagent skill roster is
  snapshotted at parent-session start); user restarted the parent session and
  the third arbiter thread invoked it live — that thread arbitrated all four
  pieces.
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
  the artifact as "the canonical pins X" (elasticsearch-search precedent);
  `ConcurrencyHandler` stays public non-sealed (canonical fidelity).
- S14 anti-example candidates (flagged, unlabelled, for a future review rubric):
  the canonical Pattern-3 catch filter (`when (ex is not BadRequestException)`
  wrapping `LockedAsync`) compensates work that never started and downgrades
  423→500 — shipped as an authorized normalization, not a BAD/GOOD pair; the
  rubric-shaped rule is "a filter that converts status must exclude exceptions
  that already carry one". Also unlabelled: the KeyedLocker `keys[0]`
  observation is VOID (provider scrubbed; user ruled it appears nowhere).
- S14 cross-lane events: Lane B shipped S13b (`message-keys` v0.3.7) and rewrote
  `CLAUDE.md` to open B4 (`auth-and-security`) mid-session; Lane C renumbered to
  0.3.8 and merged fast-forward with zero conflicts (worktree isolation worked —
  no evacuation, no lost numbers, contrast S11). Lane A's `ef-core-data-access`
  scaffold remains untracked in the shared checkout; untouched by Lane C.
- **Carried:** S11 deferred `CompileQueryAsync(...)` pagination extension over
  the search wrapper — still out of scope until the user names its file. S10/S11
  rulings lists live in this file's git history (@ commit 0cd1c13) and in
  CHANGELOG 0.3.5/0.3.6.
- Deferred to a solo session (not Lane C's to fix): the roadmap/index still say
  "Facades/Cache in one or both projects" for S10 and list ops-service/apsp as
  the only reference projects — superseded by S10/S11 designations; consolidate
  when the lane logs fold into `03-session-roadmap.md`.
