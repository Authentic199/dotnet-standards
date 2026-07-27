# 03 — Session Roadmap

> **Governing principle: one session = one deliverable.** This is a design constraint, not a
> suggestion. The plan deliberately runs across many separate sessions and contexts. A session
> that produces two deliverables has violated the plan even if both are correct.
>
> **Context discipline applies to every session.** Never bulk-scan the reference kit, and never
> bulk-scan `reference/projects/`. Read only (a) the files named in the opening prompt,
> and (b) targeted lookups — grep/glob for a specific symbol, or Roslyn MCP — where each lookup
> is announced with what is being sought and why.

---

## Phases

| Phase | Sessions | Nature |
|---|---|---|
| 0. Planning | S0 | Docs only — complete |
| 1. Triage | S1–S5 | Decisions only, no code |
| 2. Scaffold | S6 | First implementation session |
| 3. Skill distillation | S7+ | One skill per session |
| 4. Process layer | last | Highest conflict risk — deliberately last |

**Gate:** S6 does not start until TRIAGE has zero `pending` rows.

---

## Phase 1 — Triage

### S1 — Populate TRIAGE rows

- **Input:** `docs/00-brainstorm.md`, `docs/01-triage-rules.md`, existing `docs/TRIAGE.md`;
  directory listings of the kit at the pinned SHA (`ls`/`tree` only — no file contents).
- **Deliverable:** `docs/TRIAGE.md` with every kit component enumerated as a row, and the column
  schema extended per rules R1 (Provenance), R2 (Destination) and R8 (Anti-examples).
  **No decisions are made** — every status stays `pending`.
- **Done when:** every component under `skills/`, `agents/`, `hooks/`, `knowledge/`,
  `templates/`, `mcp/` and `.claude/rules/` has exactly one row; the Progress section denominators
  are filled in; the pinned SHA is written into the header; committed.

### S2 — Group A decisions, batch 1

- **Input:** TRIAGE; the `SKILL.md` of only the kit skills in this batch.
- **Batch:** the six core knowledge areas — architecture, CQRS/MediatR, EF Core, caching, API
  surface, error handling.
- **Deliverable:** those rows decided.
- **Done when:** each row has Status + Provenance + Destination + Reason; `adapt` rows respect
  rule R6 (gated on named exemplars); committed.

### S3 — Group A decisions, batch 2

- **Input:** TRIAGE; remaining Group A skills.
- **Deliverable:** all remaining Group A rows decided, applying rule R4 (out-of-scope
  short-circuit) to Blazor / Aspire / Docker / CI-CD / microservices material.
- **Also resolves:** open question **Q5** — whether `auth-and-security` and `observability` have
  usable exemplars or fall back to `from-kit`.
- **Done when:** Group A has zero `pending` rows; committed.

### S4 — Group B decisions

- **Input:** TRIAGE; the kit's `agents/`, `hooks/`, meta-skills and workflow commands; a listing
  of Superpowers' skills, commands and hooks for comparison.
- **Deliverable:** all Group B rows decided.
- **Also resolves:** open questions **Q2** (format hook viability), **Q3** (which agents),
  **Q4** (the deferred `UserPromptSubmit` skill-index hook).
- **Done when:** every `keep`/`combine` row carries all five conflict-check answers (rule R5);
  every hook decision states the `run-hook.cmd` Windows cost; committed.

### S5 — Group C + D decisions

- **Input:** TRIAGE; `mcp/`, `.claude/rules/`, `templates/`, `knowledge/`.
- **Deliverable:** all Group C and D rows decided, each Group D row carrying a Destination
  (skill content · project `CLAUDE.md` material · drop).
- **Done when:** TRIAGE has **zero** `pending` rows anywhere. **This is the gate into Phase 2.**

---

## Phase 2 — Scaffold

### S6 — Working plugin skeleton ✅ (2026-07-26)

- **Input:** `docs/02-repo-structure.md`; completed TRIAGE.
- **Deliverable as originally planned:** `.claude-plugin/plugin.json`,
  `.claude-plugin/marketplace.json`, `README.md`, `LICENSE`, `NOTICE` (rule R9), and one trivial
  smoke-test skill.
- **Deliverable as executed:** the manifests, `README.md`, `CHANGELOG.md`, `NOTICE` (two MIT
  obligations, not one), and the **hook set** — `hooks/run-hook.cmd`, `hooks/post-edit-format`,
  `hooks/hooks.json`, `hooks/README.md` — plus empty `skills/` and `agents/`.
- **Three planned deviations, all deliberate:**
  1. **No smoke-test skill.** By the time S6 ran, triage had produced a *real* smoke test — the
     one surviving hook. A hook that formats a file is stronger proof than a skill that
     activates, and writing a throwaway skill would have been the scope creep the
     one-deliverable rule exists to prevent.
  2. **No `LICENSE`.** Asked and answered during the session: the repo is personal and
     unpublished, so it stays "all rights reserved" by default. `NOTICE` discharges the
     third-party obligations regardless. **Open item — revisit if there is ever a reason to
     publish.**
  3. **No `commands/` directory**, per `02-repo-structure.md` §5.
- **Done when / what was actually verified:** the plugin installs from the local marketplace and
  `claude plugin details` reports `Hooks (1) PostToolUse` with no duplicate-hooks error; the
  wrapper and hook format a real `.cs` file in a throwaway solution, through `cmd.exe` and
  through `bash`, with an explicit argument and with `PostToolUse` stdin JSON, with no `jq`
  installed. Committed.

**Open items carried out of S6 — none of them block S7:**

| Item | Where it is recorded |
|---|---|
| Installing copies the source directory and **ignores `.gitignore`**, so `reference/` (39 MB, incl. the real project checkouts) lands in the plugin cache. Two candidate fixes, neither chosen because both change what §1 specifies. | `02-repo-structure.md` §4 |
| Whether this repo ever gets a `LICENSE`. | above |
| Final end-to-end confirmation that the hook fires **inside a live session** — it cannot fire in the session that installed it. Requires a restart plus one `.cs` edit. | `next-session-prompt.md` |

---

## Phase 3 — Skill distillation

One skill per session, in priority order.

| Session | Skill | Notes |
|---|---|---|
| S7 ◐ | `facade-module-architecture` | **Q1 RESOLVED (2026-07-26)** — Facade / Module layering: `Core` → `Infrastructure` → `Web`, with `Facades/` × `Modules/` inside `Infrastructure`. Not Clean Architecture, not VSA. Shipped with three `references/` files. Also traced A05 and A33's setup sites. **Superseded in part:** distilled from `ops-service`, which the user then identified as a base project rather than production. R7 canonical source re-designated to **`apsp-backend`**, which *confirms* the architecture but changes six details. |
| S7b ✅ | `facade-module-architecture` — **rebuild** (2026-07-26, v0.3.0) | Rebuilt under the **three-way authoring process** — six body pieces + assembly + description, each through A/B drafts, arbiter verdict, user approval. Per-area canonical designation (ops-service base / apsp-backend production / be-booking one anti-example). All six recorded defects fixed; description voice settled (§5 rewritten); 6 `references/` files; verified `Skills (1)`. **From here Phase 3 splits into three parallel lanes — see `next-session-prompt.md` (index) + `-A/-B/-C` files. Lane sessions log deferred notes in their own prompt files, consolidated to this roadmap at a later solo session.** |
| S8 | `cqrs-feature-slice` | |
| S9 | `ef-core-data-access` | |
| S10 | `distributed-caching` | |
| S11 | `elasticsearch-search` | |
| S12 ✅ | `api-surface` | Shipped v0.3.2 (2026-07-26, Lane B) under the three-way process — see CHANGELOG and `next-session-prompt-B.md` Lane log. |
| S13 | `error-handling` | |
| S13b | `message-keys` | **Row added at S12's close by user direction** (lane-ownership exception, explicit). Dedicated skill for the `Messages<T>` key grammar — ruled in S12 OUT of both `api-surface` and `error-handling` so the two never collide; both point here. Sources: `be-booking/CONVENTION.md` "Message Keys" section (verified in S12 to match `apsp-backend` `Facades/Definitions/Messages.cs` key-for-key) plus that file and its call sites. Must settle: `Messages<T>.X(selector)` vs bare `MessagesType` constants in validators (constants more frequent by raw count; `Messages<T>` matches the shared facade and both shipped skill bodies). Runs in Lane B, immediately after S13, which needs it. |
| S14 ✅ | `dotnet-testing` | **Research variant** — no exemplar exists. Shipped v0.3.11 (2026-07-27, Lane B, ran as S15) under the three-way process — see CHANGELOG and `next-session-prompt-B.md` Lane log. |
| S15 | `choosing-a-dotnet-skill` | Router. Runs after the core skills exist so the decision table has real targets. |
| S16+ | `auth-and-security`, `observability`, `background-worker`, `http-resilience`, `domain-modeling`, `modern-csharp`, `project-scaffolding` | one per session |
| S16+ | `distributed-lock` | **Row added at S13's open by user direction** (lane-ownership exception, explicit — `distributed-caching` v0.3.1's `Not for:` already routes here, but no row existed). Owns `ConcurrencyHandlers` internals and **`LockedException` (HTTP 423)** — the fifth sealed exception, verified in S13: all 3 throw sites live in `apsp-backend` `Facades/Common/Services/ConcurrencyHandlers/ConcurrencyHandler.cs`; the exception middleware needed no change for it (growth-by-leaf). `error-handling` (S13) cites it only as the growth worked example and routes lock semantics here, per user ruling at S13 open. |
| then | `dotnet-code-review`, `dotnet-architecture-review`, `dotnet-security-review`, `dotnet-performance-review` | one per session |
| post-rubrics | `architecture-tests` | **Row added at S15's close (user-flagged omission, Lane B log).** Candidate raised and deferred during S15's split discussion: NetArchTest-style tests asserting the dependency direction and layer rules that `facade-module-architecture` legislates (Core depends on nothing; facades never reference modules' internals; envelopes stay `internal sealed`). Natural home: either a small addition to `dotnet-testing`'s references or absorbed into the `dotnet-architecture-review` rubric as its executable arm — decide when the rubric is drafted; do not build before it. |
| post-rubrics | `dotnet-test-report` hook | **Row added at S15's close by user direction (Lane B log).** Group B component: a `PostToolUse` hook matching `dotnet test` that parses TRX/console output and auto-reports which cases ran and which passed, so the agent (and the user) see results without narration. Precedent: the kit's `hooks/post-test-analyze.sh`; requires the Windows polyglot hook wrapper (`02-repo-structure.md` §6); needs the Group B conflict check before build. |

> **REPRIORITIZED 2026-07-27 (S14 close, explicit user direction — lane-ownership
> exception, recorded in the Lane C log).** The user wants the lean plugin shipped
> first. Effective order from here: (1) `dotnet-testing` → **Lane B** (B4) and
> `choosing-a-dotnet-skill` → **Lane C** (S15), promoted out of the solo/excluded
> list, may run in parallel (router aligns its `dotnet-testing` row at merge
> time); Lane A finishes `ef-core-data-access` (already in flight), then stops.
> (2) The four review rubrics run immediately after, one per session. PENDING
> until further notice: `auth-and-security`, `observability`, `background-worker`,
> `http-resilience`, `domain-modeling`, `modern-csharp`, `project-scaffolding`.
> Conventions those pending skills would have owned are folded into the review
> rubrics later if still wanted. Rubric input already banked in the lane logs:
> S13's four unruled candidates (CHANGELOG 0.3.4 / Lane B log) and S14's two
> (Pattern-3 catch filter rule "a filter that converts status must exclude
> exceptions that already carry one"; the semaphore-registry cleanup race) —
> harvest the lane logs + CHANGELOG before re-mining source.
>
> **Lane D added (2026-07-27, S14): Process Integration.** After the rubrics, a
> new lane builds the closed-loop workflows (one command → brainstorm → plan →
> implement → test-loop → review-loop → git, Superpowers called for process,
> this plugin's skills for content), six specialist subagents (4 read-only
> rubric reviewers + testers mirroring `dotnet-testing`'s taxonomy) and the
> SessionStart Superpowers-dependency check. Approved spec:
> `docs/superpowers/specs/2026-07-27-process-integration-design.md`; lane file:
> `next-session-prompt-D.md`. This supersedes the old "process layer (P4) —
> excluded" placeholder: P4 is now a designed lane with a hard sequencing
> constraint (after rubrics + `dotnet-testing`). The "Knowledge only" promise in
> the plugin description is deliberately retired when Lane D ships (user
> ruling).

### The five-step adapt session — standard structure

Applies to **every** `adapt` session without exception.

**Step 1 — INPUT (supplied by the user in the opening prompt)**
A list of exemplar file paths under `reference/projects/`, plus **anti-examples** where
they exist — code the user does *not* want repeated. Claude never selects exemplars on the
user's behalf: a real codebase contains both good code and technical debt, and only the user can
tell them apart.

**Step 2 — PURPOSEFUL READING**
Read only the named files. Any widening is a targeted lookup (grep/glob on a specific symbol, or
Roslyn MCP), announced up front: *what is being sought, and why*. No exploratory scanning.

**Step 3 — DISTIL** → `skills/<name>/assets/`
Rewrite the exemplar into the skill's own reference files:
- reduce to the portion that demonstrates the pattern;
- rename business-domain names to generic ones;
- **sanitize**: remove connection strings, secrets, internal package names, and
  business-specific logic.

The finished skill must be **self-contained**. It may never point at a path inside a real
project.

**Step 4 — REVERSE-CHECK**
Verify line by line that the rules and checklists in `SKILL.md` match the distilled exemplar
code. Saying one thing while the code shows another is a defect, not a detail.

**Step 5 — DONE means all of:**
1. the plugin still installs/builds;
2. the user has approved the distilled version;
3. the canonical source (project → feature) is recorded in the TRIAGE decision log;
4. committed.

### Research variant (`from-research` skills, e.g. S14)

- **Step 1** — the user approves the research scope instead of supplying exemplars.
- **Step 2** — web research plus the kit's own material.
- **Step 3** — write exemplar code from scratch; it must still be self-contained and sanitized.
- **Step 4–5** — unchanged, plus: **cite source URLs inside the skill**, so its provenance is
  auditable later.

### The three-way authoring process — mandatory for every skill from S7b onward

Adopted at the close of S7, after the first skill had already shipped under the old
single-author process. It replaces "Claude writes the skill" in step 3 of the five-step
adapt session. Everything else in that structure is unchanged.

**Why three authors.** A skill written by one author reflects one methodology's blind spots.
Three independent methodologies exist and they genuinely disagree — the repo's own format
rules, Superpowers' `writing-skills`, and Anthropic's official `skill-creator` — so the
disagreements are surfaced and adjudicated instead of being silently resolved by whoever
happened to be writing.

| Author | Loads (the only thing that differs) | Does not load |
|---|---|---|
| **A — main session** | `docs/02-repo-structure.md` §5, `docs/00-brainstorm.md` §3, the reference kit's skill format | **Not** `superpowers:writing-skills` — deliberately, so A and B do not share a methodology |
| **B — `skill-writer-sp` agent** | `superpowers:writing-skills` | Not the repo's format docs beyond what its prompt carries |
| **Arbiter — `skill-arbiter` agent** | Anthropic's official `skill-creator` | Neither author's methodology as its own |

**Equal source access — amended at the user's direction, replacing the original design.**
All three participants read the **same user-named exemplar files** in `reference/projects/`
directly. The first design fed B and the arbiter only material pre-digested by A, which made
the three perspectives fake: every draft inherited A's reading of the code, and the arbiter
judged two drafts that shared one pair of eyes. The user's rule, verbatim in intent: *the
writers and the referee must have equal rights and equal source to trust — equal capability,
differing only in the knowledge loaded into them, so that reasoning and results differ, not
access.* The reading discipline (user names the files; widening requires asking; no bulk
scans; R7; Bash not Glob) binds **all three identically** — it is about who *chooses* the
exemplars, and that remains the user alone.

**The loop, per piece — not per skill:**

1. **A explains first, in Vietnamese.** What it intends to write, why it decided that way,
   what is good about it, and how it combines with the other pieces. The user comments.
2. **A and B each draft the same piece, independently.** Both return text. **Neither writes a
   file.**
3. **The arbiter decides**: `A`, `B`, `MERGE`, or `NEITHER` — never "either is fine" — and
   states *which specific property* decided it, plus what it cut and why.
4. **The user reviews the verdict and the reasons, then approves.** Only then does the main
   session write the file.
5. Repeat until the skill is complete.

**Structure is not pre-decided.** Which sections a skill has — and whether the body follows the
knowledge shape (Core Principles → Patterns → Anti-patterns → Decision Guide) or a
workflow shape (Prerequisites → Steps → Conventions → Examples → Common Mistakes) — is
**an output of the arbiter's analysis, not an input to it**. The user's ruling, verbatim:
*fixing the architecture before any draft or perspective exists is over-engineering.*

**Two live conflicts the arbiter inherits**, both recorded in the TRIAGE decision log:
- **Description voice** — `02-repo-structure.md` §5 says second person (`Use when …`);
  `apsp-backend/skills/skill-creator/SKILL.md` says **third person**, "pushy", **under 100
  words**, explicit trigger phrases. The shipped skill follows §5 and exceeds 100 words.
- **Body shape** — knowledge shape versus the user's own workflow-shaped template.

**The main session still owns the agents' prompts.** Equal access means the agents read the
named exemplars themselves — it does not mean they can find the *rule sets* on their own. A's
prompt to each agent must carry: the user-named exemplar file list, the two live conflicts
below, and the fact that `apsp-backend/skills/` is the highest-tier `from-my-code` source.
Skipping any of these silently converts a three-way decision into a two-way one.

**Sub-agent questions bounce back, turn by turn.** Agents cannot interrupt mid-run; they end
their report with a `## QUESTIONS` section. The main session answers what it can and escalates
only genuine user decisions, then continues the same agent with `SendMessage` so its context
survives. The main session **must announce when an agent is in use** and relay progress at
natural milestones.

**Agent definitions live in `.claude/agents/`, never in the plugin's `agents/`.** They are
tooling for building the plugin, not plugin content — triage settled that exactly one agent
ships (`ef-core-specialist`, B18), and shipping authoring agents to consumers would contradict it.

**A definition does not load in the session that creates it.** Adding an agent, hook or skill
requires a restart before it is dispatchable — the same constraint S6 measured for hooks. Plan
for it: define in one session, exercise in the next.

### The `references/` mechanism — standing instruction (adopted S7b)

Confirmed by the user in S7b as the standard way to keep a gateway skill's body inside
the ~500-line budget, after evaluating the kit's mechanism against Anthropic's
`skill-creator` progressive-disclosure model. It operationalizes what S0 already adopted
as mechanism A (`00-brainstorm.md` §3). Every skill session from S7b onward follows it:

1. **Three tiers.** `description` (~100 words, always in context) → SKILL.md body
   (loads on trigger, **under ~500 lines**) → `references/*.md` (loaded only when the
   agent actually opens them). A reference file over ~300 lines carries a table of
   contents.
2. **`references/` files do not auto-load.** The body must point at them explicitly,
   and every pointer states *when* to open the file ("read `references/x.md` when
   doing Y"). A bare "see references/x.md" is dead weight.
3. **The body stays self-sufficient for placement decisions** — the gateway's core
   job. `references/` holds depth (full code anatomies, extended examples), never the
   rules themselves.
4. **Split along "always needed / sometimes needed"**, not by topic symmetry. If
   answering one common question requires opening more than one reference file, the
   split is wrong.
5. **The split itself is a piece** — it goes through the three-way loop (A proposes,
   B proposes independently, the arbiter decides, the user approves), because
   structure is an output, not an input.
6. Install copies the whole skill directory (measured in S6), so `references/` ships
   with no extra mechanics.

### Canonical-source rule (R7) in practice

One skill draws from exactly **one** project, chosen by the user. Other projects are for
comparison only. On divergence, ask *"which one do you want from now on?"* — never average two
conventions.

**First exercised in S3.** The user named `ops-service` as the canonical source for A05
`authentication` and A33 `serilog` — not `apsp-backend`, which S0 §9 recorded as the only project
present. Two standing consequences:

- **`reference/projects/` holds more than one project.** No session may default to `apsp-backend`;
  every `adapt` row needs its canonical project named explicitly by the user, per row.
- **R7 now has real work to do in S16+.** `observability` is genuinely `mixed`: A33 is
  `from-my-code` while A21 and A26 are `from-kit`. Those must be assembled side by side and
  labelled, never blended into one voice.

---

## Phase 4 — Process layer (last)

Deferred to the end because conflict risk is highest and the decisions from S4 should be
re-validated against a plugin that actually exists.

| Session | Deliverable | Done when |
|---|---|---|
| P4-a | `dotnet-build-loop` skill | Runs `dotnet build`, parses `CS####` errors, iterates; verified not to contradict the Superpowers TDD flow |
| P4-b | `.cs` format hook + `run-hook.cmd` | Fires on Windows through the polyglot wrapper; verified not to collide with a Superpowers hook on the same event |
| P4-c | Selected .NET agents | Each agent's name and instructions re-checked against conflict-check items 4 and 5 |

---

## Backlog (explicitly not v1)

| Item | Why deferred |
|---|---|
| Per-project `CLAUDE.md` template (tier 3) | Considered in S0 and declined; tier 3 stays hand-written |
| `UserPromptSubmit` hook injecting a skill index (mechanism E) | Stronger than the router skill, but Group B — needs S4 conflict check first |
| Blazor, modular monolith / microservices, CI/CD, Docker, K8s, Aspire skills | Out of scope for v1 |
| Promoting `from-kit` skills to `adapt` | Once exemplars exist — tracked via the `upgrade candidate` flag |

## Requests deferred out of S0

S0 was a planning-only session; the following were correctly *not* done here and are recorded
for their proper session:

| Request | Session |
|---|---|
| Create the real `plugin.json` / `marketplace.json` | S6 |
| Write any skill | S7+ |
| Name the architecture skill | S7 |
| Decide anything about hooks or agents | S4 |

## Requests deferred out of S3

| Request | Session | Note |
|---|---|---|
| *"Các reference setup thì quét để xem"* — scan the `Program.cs` / DI registration sites that consume `src/Infrastructure/Facades/Auth/` and `src/Infrastructure/Facades/Logging/` in `ops-service` | **S7** (`auth-and-security`) and **S16+** (`observability`) | Declined in S3 under hard constraint 1: S3 decides rows and does **not** open `reference/projects/`. R6 only requires the user to *name* exemplar paths to unlock `adapt`, which they did — reading them is Step 2 of the five-step adapt session. The paths are recorded in TRIAGE as **named but not verified**; the adapt session must confirm they exist before distilling, and must announce the setup-site trace as a targeted lookup rather than an exploratory scan. |

## Requests deferred out of S7

| Request | Session | Note |
|---|---|---|
| **Distil** A05 `authentication` and A33 `serilog` into content, rather than only tracing their setup sites | **`auth-and-security`** (S16+) and **`observability`** (S16+) | The S6 prompt said S7 "promotes A05 and A33 from recorded paths to distilled content"; the two TRIAGE rows say *tracing* is the S7 task. The one-deliverable rule agrees with TRIAGE, so S7 traced and stopped. The wording drift was flagged to the user in the S7 opening prompt rather than resolved silently, and the user did not override it. **Both rows now carry verified concrete paths, so both sessions start from fact rather than from a named guess.** |
| Fix the `app.Run()` / `ApplicationStopping.Register` ordering bug found in `ops-service` | none — not a `dotnet-standards` task | S7 writes nothing into a real project. Recorded in the TRIAGE decision log so it is not lost. |
| Decide whether `ops-service` should adopt central package management, `global.json`, or `.slnx` | none — each is its own migration | Adjudicated by the user in S7 as **observed conventions, not faults**. `references/solution-layout.md` explicitly instructs against migrating any of them as a side effect of unrelated work. |

## Requests deferred out of S7b

| Request | Session | Note |
|---|---|---|
| Fix `ops-service` analyzer-version drift: `Directory.Build.props` pins Roslynator 4.3.0 / SonarAnalyzer 9.0.0.68202 while five csproj `Update` to 4.5.0 / 9.12.0.78982, leaving the two migrator projects on the old versions — two analyzer versions run at once | none — the user's own project task, not a `dotnet-standards` deliverable | User adjudicated in S7b: **a defect to fix**, not an observed convention. The approved piece-1 text already describes the `Include`-once/`Update`-per-project mechanism correctly and warns the props version is a floor. |
| Fix `ops-service` `tests/Infrastructure.IntegrationTests`: no `ProjectReference` at all (cannot test anything) — plus it is the `net7.0` TFM-drift project | none — the user's own project task | User in S7b: integration testing was never implemented; likely dead scaffolding, fix it, then the recorded architecture stays as the norm ("a test project references `Infrastructure` and nothing else"). |
| `stylecop.json` unwired in `ops-service` (no `AdditionalFiles` item anywhere; only listed in `.sln` Solution Items) | none | User in S7b: leave as-is. The approved piece-1 text documents the observed truth: the file has no build effect until wired. |
| Decide the `JwtSettings` convention: ops (`double` expirations + `GetSecurityKey()`/expiry helpers) vs apsp (`string` expirations, no helpers, extra business schemes) | `auth-and-security` (S16+) | Reported to the user in S7b; deliberately not decided there — it is an auth convention, not an architecture one. |
| Document that mapping travels with its source class — the standard module has **no `Mappings/` folder** (apsp `Modules/Users/Mappings/` is non-standard) | the AutoMapper/mapping skill session | User ruling in S7b. The rebuilt architecture skill lists the standard module folders without `Mappings/`. |
| Repeat the "module `Services/` is not a dumping ground" convention at file-creation level | S8 `cqrs-feature-slice` | User in S7b authorized the anti-examples (apsp `Modules/Customers/Services`, be-booking `Modules/Campaigns/Services`) and asked that later stages carry the convention so it never needs re-flagging. |
| Clean up `ops-service/src/Core/` defects surfaced by S7b verification | none — the user's own project tasks | (a) `ForbiddenException.cs` / `UnAuthorizedException.cs` declare `namespace Infrastructure.Exceptions.HttpExceptions` inside Core; (b) `HttpCustomException.Value` is dead code — set, never read (middleware reads `StatusCode` + `Message`); (c) `new HttpCustomException()` is public/non-abstract and yields `StatusCode = 0`; (d) `UnAuthorizedException` is the one unsealed concrete exception — now non-compliant with the S7b sealed rule; (e) legacy `[Serializable]`/`SerializationInfo` ceremony on all six exception files — S7b ruled new exceptions drop it (SYSLIB0051), so existing files are cleanup candidates; (f) unused `using System.Net;` in `CustomException.cs`; mixed file-/block-scoped namespaces in one folder. |
| `Core/Helpers/` (PropertyFlatten, ReflectionHelper) | undocumented by user ruling | S7b: the skill says nothing about `Helpers/` at all — dropped from the folder tree and the body. Revisit only if a future skill needs it. |
| Detail the `ElkEntities/` convention (Elk-prefixed search entities when a module has an Elasticsearch projection — never reuse DB entities) | **S11 `elasticsearch-search`** | User ruling in S7b: the architecture skill only introduces the folder so placement is unambiguous; the how belongs to S11. |
| Settle the controller file-writing conventions: expression-bodied vs block-bodied endpoints, long-signature wrapping, and enforcement of the unified partial rule (base list only in the suffix-less core file — user ruling S7b) | **S12 `api-surface`** | The canonical controllers mix both body styles and one module carries its base list on a non-core part; S7b ruled the flags "skip" for the architecture skill but the API-design session owns the definitive convention. Also account for the multi-permission/multi-scheme `[HasPermission]` overload (real second shape the single-permission sample does not show). **Cross-skill contract set by the S7b description verdict:** `api-surface` claims routes, DTOs, versioning, OpenAPI and endpoint-writing conventions — it must NOT claim controller *placement*, which `facade-module-architecture` owns. |

---

## End-of-session ritual (every session from S1 onward)

1. Commit the session's deliverable with a clear message.
2. **Update `docs/next-session-prompt.md`** so it contains a complete opening prompt for the next
   session: minimum context, files to read, the single deliverable, and a restatement of the
   one-session-one-deliverable rule and the context-discipline rule.
