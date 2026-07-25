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

### S6 — Working plugin skeleton

- **Input:** `docs/02-repo-structure.md`; completed TRIAGE.
- **Deliverable:** `.claude-plugin/plugin.json`, `.claude-plugin/marketplace.json`, `README.md`,
  `LICENSE`, `NOTICE` (rule R9), and one trivial smoke-test skill.
- **Done when:** `/plugin marketplace add` + `/plugin install` succeed, Claude Code is restarted,
  and the smoke-test skill demonstrably activates; committed.

---

## Phase 3 — Skill distillation

One skill per session, in priority order.

| Session | Skill | Notes |
|---|---|---|
| S7 | `solution-architecture` | **Also resolves Q1** — the real architecture name and layering. Nothing downstream may assume Clean Architecture until this lands. |
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

### Canonical-source rule (R7) in practice

One skill draws from exactly **one** project, chosen by the user. Other projects are for
comparison only. On divergence, ask *"which one do you want from now on?"* — never average two
conventions. Currently only `apsp-backend` exists, so this rule is defined but not yet exercised.

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

---

## End-of-session ritual (every session from S1 onward)

1. Commit the session's deliverable with a clear message.
2. **Update `docs/next-session-prompt.md`** so it contains a complete opening prompt for the next
   session: minimum context, files to read, the single deliverable, and a restatement of the
   one-session-one-deliverable rule and the context-discipline rule.
