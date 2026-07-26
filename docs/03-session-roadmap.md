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
| S7b | `facade-module-architecture` — **rebuild** | Redo from `apsp-backend`, under the **three-way authoring process** below. Q1's answer is settled and is **not** reopened; only its evidence base and details change. First session in which `skill-writer-sp` and `skill-arbiter` are loadable. |
| S8 | `cqrs-feature-slice` | |
| S9 | `ef-core-data-access` | |
| S10 | `distributed-caching` | |
| S11 | `elasticsearch-search` | |
| S12 | `api-surface` | |
| S13 | `error-handling` | |
| S14 | `dotnet-testing` | **Research variant** — no exemplar exists |
| S15 | `choosing-a-dotnet-skill` | Router. Runs after the core skills exist so the decision table has real targets. |
| S16+ | `auth-and-security`, `observability`, `background-worker`, `http-resilience`, `domain-modeling`, `modern-csharp`, `project-scaffolding` | one per session |
| then | `dotnet-code-review`, `dotnet-architecture-review`, `dotnet-security-review`, `dotnet-performance-review` | one per session |

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

---

## End-of-session ritual (every session from S1 onward)

1. Commit the session's deliverable with a clear message.
2. **Update `docs/next-session-prompt.md`** so it contains a complete opening prompt for the next
   session: minimum context, files to read, the single deliverable, and a restatement of the
   one-session-one-deliverable rule and the context-discipline rule.
