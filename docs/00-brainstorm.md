# 00 — Brainstorm Result: the `dotnet-standards` plugin

> **Session:** S0 (planning only — no implementation)
> **Date:** 2026-07-25
> **Reference kit pinned commit:** `cd83d315986c27621da178dad73bd95d503c1540`
> (`codewithmukesh/dotnet-claude-kit`, MIT — commit `cd83d31`, 2026-07-25,
> *"docs: surface the dotnet-claude-kit guide across the README"*)
> **All triage decisions in this project are anchored to that commit.** The kit moves fast;
> re-pin deliberately and record the change in the TRIAGE decision log, never silently.

---

## 1. Purpose

`dotnet-standards` is the **knowledge layer** of a three-tier setup:

| Tier | Owner | Responsibility |
|---|---|---|
| 1. Process | **Superpowers** (obra marketplace) | brainstorm → plan → TDD → review. **Never modified.** |
| 2. Knowledge | **`dotnet-standards`** (this repo) | What the user's .NET code looks like: architecture, CQRS pipeline, EF Core, caching, search, API surface, testing. |
| 3. Context | Per-project `CLAUDE.md` | Which conventions apply to *this* codebase. Hand-written by the user; not shipped by the plugin in v1. |

The plugin knows *how the user writes .NET*. It does not own workflow, and it does not
own project-specific context.

---

## 2. Scope

### In scope
- **Project shapes:** Web API / backend service, and Worker / background-job service.
- **Domains:** CQRS pipeline (MediatR + FluentValidation + AutoMapper), EF Core data access,
  Redis distributed caching, Elasticsearch, API surface & cross-cutting concerns, error handling,
  auth & security, observability (Serilog + OpenTelemetry + health checks), background workers,
  HTTP resilience, domain modeling, modern C#, project scaffolding, testing.
- **Review rubrics** for code quality, architecture, security and performance — supplied *to*
  the Superpowers review process, not as a competing process.

### Out of scope (v1)
| Excluded | Rationale |
|---|---|
| Blazor / .NET frontend | Not part of the user's work. |
| Modular monolith & microservices | Not the user's project shape. |
| CI/CD, Docker, Kubernetes, container publishing | Owned by others; user opted out. |
| .NET Aspire | Not used. |
| Modifying any Superpowers file | Hard constraint, permanent, no exceptions. |
| Shipping a per-project `CLAUDE.md` template | Considered and declined for v1 → backlog. Tier 3 stays hand-written. |

Anything in the reference kit that falls into an excluded area is short-circuited to `skip`
during triage with reason `out-of-scope v1` (see rule **R4** in `01-triage-rules.md`) — no deep
analysis, no context spent.

---

## 3. Packaging decision: gateway skills + `references/`

The user wants coverage comparable to the kit's ~46 skills. Shipping 46 flat sibling skills
creates **description overlap**, and an overlapping description means Claude picks the wrong
skill — which is worse than having no skill at all.

Mechanisms evaluated:

| Option | Mechanism | Verdict |
|---|---|---|
| **A. Hierarchy via `references/`** | ~15 gateway skills; sub-topics live in each skill's `references/*.md`. 30+ topics, ~15 activation surfaces. | **Adopted** |
| B. Per-project `CLAUDE.md` template | Deterministic; independent of description matching. | Declined for v1 → backlog |
| **C. Description discipline** | Every description states triggers **and** anti-triggers ("not for X — use Y instead"). | **Adopted** |
| **D. Router skill** | `choosing-a-dotnet-skill` holds a decision table pointing at the gateway skills. | **Adopted** |
| E. `UserPromptSubmit` hook injecting a skill index | Deterministic, stronger than D. | **Deferred** — Group B component, requires conflict check first (S4) |

Topic coverage is not reduced. Only the number of activation surfaces is.

---

## 4. Target skills — knowledge layer

Ordered by implementation priority. Names marked ⚠️ are provisional.

| # | Skill | Activates when | `references/` sub-topics | Provenance |
|---|---|---|---|---|
| 1 | `facade-module-architecture` ✅ | Creating a project, placing a new file, "where does this belong?" | layering, project references, composition root, dependency rules | `from-my-code` |
| 2 | `cqrs-feature-slice` | Adding or changing a feature: command, query, handler | MediatR, FluentValidation, AutoMapper, pipeline behaviors | `from-my-code` |
| 3 | `ef-core-data-access` | Touching DbContext, entities, migrations, queries | entity configuration, migrations, query patterns, transactions, N+1 | `from-my-code` |
| 4 | `distributed-caching` | Redis, cache-aside, invalidation | key conventions, TTL, invalidation, `IDistributedCache` vs raw client | `from-my-code` |
| 5 | `elasticsearch-search` | Indexing, query DSL, DB→ES sync | index mapping, queries, reindex, sync strategy | `from-my-code` |
| 6 | `api-surface` | Endpoints, routing, DTOs, versioning, OpenAPI | Minimal API vs Controllers, versioning, OpenAPI/Scalar, DTO envelope | `from-my-code` |
| 7 | `error-handling` | Exceptions, status codes, Result types | middleware, ProblemDetails, Result pattern, error mapping | `from-my-code` |
| 8 | `dotnet-testing` | Writing or changing tests | xUnit, mocking, `WebApplicationFactory`, Testcontainers | **`from-kit` + `from-research`** |
| 9 | `auth-and-security` | JWT, policies, secrets | authn/authz, policies, secret handling, security headers | `mixed` |
| 10 | `observability` | Logging, tracing, health | Serilog sinks & enrichers, OpenTelemetry, health checks | `mixed` |
| 11 | `background-worker` | Hosted services, jobs, consumers | `BackgroundService`, scheduling, queue/messaging | `from-my-code` |
| 12 | `http-resilience` | Calling external services | `IHttpClientFactory`, Polly, timeout/retry/circuit-breaker | `from-kit` |
| 13 | `domain-modeling` | Entities, value objects, domain events | aggregates, value objects, domain events, invariants | `from-kit` |
| 14 | `modern-csharp` | Writing or reviewing C# generally | nullable reference types, records, collection expressions, analyzers | `from-kit` |
| 15 | `project-scaffolding` | Bootstrapping a new solution | init, project setup, templates | `from-kit` |

### Special note on #1 and #8

**#1 `facade-module-architecture`** — ✅ **RESOLVED IN S7 (2026-07-26).** The user stated their
real architecture is **not** Clean Architecture, despite the wording of the kickoff prompt, so the
name was a placeholder until Q1 was answered from real code. It now reads: a three-project chain
`Core` → `Infrastructure` → `Web` (plus `Migrators.<Provider>`), whose `Infrastructure` project is
split on two axes — `Facades/` for technical capabilities, `Modules/` for business ones — wired by
per-facade `Startup.cs` extension methods into a single flat composition root. **Not Clean
Architecture and not VSA.** Full evidence in the `TRIAGE.md` decision log under *Q1 — RESOLVED*.

**#8 `dotnet-testing`** — the user does not currently write tests, so there is **no exemplar to
distil**. This is a deliberate gap the user wants filled: the goal is both unit and integration
testing, built from the reference kit plus web research. This is what forced the `provenance`
dimension into the triage rules (rule **R1**).

---

## 5. Target skills — review rubrics

Superpowers owns the review *process* (`requesting-code-review`, `receiving-code-review`).
`dotnet-standards` supplies **.NET-specific rubrics** consumed by that process. This is a
`combine`: no Superpowers file is touched and no competing review workflow is created.

| Skill | Lens | Rubric content | Kit anchor |
|---|---|---|---|
| `dotnet-code-review` | Code quality | C# idiom, async/await misuse, naming, code slop, dead abstractions | `code-review`, `de-sloppify` |
| `dotnet-architecture-review` | Architecture | dependency-direction violations, layer leaks, misplaced features, bloated handlers | `arch-check` + skill #1 |
| `dotnet-security-review` | Security | missing authorization, hard-coded secrets, injection, mass assignment, data exposure via DTOs | `security-scan` + skill #9 |
| `dotnet-performance-review` | Performance | N+1 queries, excess allocation, async blocking, missing DB indexes | `performance-analyst` agent |

These are **rubrics, not commands**. They carry no slash-command name, which sidesteps
collision with the built-in `/code-review` and `/security-review`.

---

## 6. Process-layer candidates (Group B — proposals only)

The user identified three genuine gaps in Superpowers for .NET work. None is decided here;
all are triaged in S4 with a mandatory conflict check.

| Component | Role | Proposed status |
|---|---|---|
| `choosing-a-dotnet-skill` | Router: decision table over the knowledge skills | `rebuild` (written fresh, not copied) |
| `dotnet-build-loop` | Run `dotnet build`, parse `CS####` errors, iterate to green | `combine` — Superpowers has no concept of `dotnet` |
| `.cs` format hook | Run `dotnet format` after edits | `combine` — **conflict check required** |
| .NET specialist agents | Deep investigation without burning main context | selective `keep` — **conflict check required** |

**Known Windows cost.** The kit's hooks are `.sh` files. On Windows, Claude Code executes hooks
through `CMD.exe`, which cannot run `.sh` — it opens them in an editor. Keeping any kit hook
therefore requires shipping a polyglot `run-hook.cmd` wrapper (see `02-repo-structure.md`).
This is a real cost and must be weighed in S4, not discovered later.

---

## 7. Decisions made in this session

1. Scope is Web API + Worker. Blazor, microservices/modular monolith, CI/CD, Docker, K8s and
   Aspire are out for v1.
2. Coverage stays broad (30+ topics), but is packaged as ~15 gateway skills + `references/`
   (mechanism **A**), reinforced by description discipline (**C**) and a router skill (**D**).
3. Four review lenses, delivered as rubrics feeding the Superpowers review process — never as a
   parallel review workflow.
4. `provenance` is added as a dimension orthogonal to triage status.
5. Testing is in scope and will be built from kit + research, not from the user's code.
6. Per-project `CLAUDE.md` templates are backlog, not v1.
7. The architecture skill's name is deferred to S7.

## 8. Open questions

| # | Question | Resolved in |
|---|---|---|
| Q1 | ✅ **CLOSED S7 (2026-07-26).** What *is* the user's real architecture, and what should skill #1 be called? Explicitly **not** Clean Architecture. → **Facade / Module layering**, three projects, two axes inside `Infrastructure`; skill named `facade-module-architecture`. | S7 |
| Q2 | Can a `.cs` format hook coexist with Superpowers' hooks, and is the `run-hook.cmd` cost worth it? | S4 |
| Q3 | Which of the kit's 10 agents (if any) are worth keeping, given Superpowers' review flow? | S4 |
| Q4 | Should the deferred `UserPromptSubmit` skill-index hook (mechanism E) be built, once conflict-checked? | S4 |
| Q5 | Do `auth-and-security` and `observability` have usable exemplars, or do they fall back to `from-kit`? | S3 |
| Q6 | Backlog: ship a per-project `CLAUDE.md` template as tier 3? | post-v1 |

## 9. Environment notes recorded during this session

- The reference kit lives at `reference/dotnet-claude-kit`. (It was briefly nested twice as
  `reference/reference/`; the user flattened it at the end of S0.)
- The exemplar source directory `reference/projects/` currently holds **one** project:
  `apsp-backend`. The "one canonical project per skill" rule (**R7**) is defined but not yet
  exercised.
- `docs/TRIAGE.md` already exists as a committed skeleton. S1 **fills** it; it does not create it.
- Kit inventory at the pinned commit: 46 skills, 10 agents, 8 hook scripts + `hooks.json`,
  6 knowledge files + `knowledge/decisions/`, 5 templates, 1 MCP server (`CWM.RoslynNavigator`),
  `.claude/rules/`, and `.claude-plugin/{plugin.json, marketplace.json}`.
