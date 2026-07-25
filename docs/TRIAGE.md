# TRIAGE — dotnet-claude-kit → dotnet-standards

> Reference repo: codewithmukesh/dotnet-claude-kit (MIT)
> Pinned commit SHA: `cd83d315986c27621da178dad73bd95d503c1540`
> (2026-07-25, *"docs: surface the dotnet-claude-kit guide across the README"*)
> Status values: `pending` | `keep` | `keep-tweak` | `adapt` | `rebuild` | `skip` | `combine`
> Provenance values (R1): `from-my-code` | `from-kit` | `from-research` | `mixed`
> Rule: every non-pending row MUST have a Reason. Group B rows with `keep`/`combine` MUST answer all five conflict-check items (R5). Group A rows MUST carry a Destination (R2).

## How to read this file

**Populated in S1 (scaffolding only).** Every row below is `pending`. No disposition has been
decided. S1 enumerated components and assigned them to groups; S2–S5 decide them.

### Enumeration conventions

These conventions were chosen in S1 so that "exactly one row per component" is unambiguous:

| Kit area | Row granularity | Rows |
|---|---|---|
| `skills/` | one row per **skill directory**. A skill's own `references/*.md` is content of that skill and shares its row — noted in the Summary as `(+ref: <file>)`. | 47 |
| `agents/` | one row per agent file | 10 |
| `hooks/` | one row per file, including `hooks.json` and `README.md` | 9 |
| `knowledge/` | one row per file, ADRs under `decisions/` enumerated individually | 12 |
| `templates/` | one row per **template directory** (each holds `CLAUDE.md` + `README.md`) | 5 |
| `mcp/` | one row for the whole server project | 1 |
| `.claude/rules/` | one row per rule file | 10 |

### Group assignment is provisional

Rows marked **⇄** sit on the Group A / Group B boundary — the kit ships them as skills, but they
read as workflow or review commands. They are filed where their *likely destination* lives
(rubric/knowledge → A, process → B). Moving a ⇄ row between groups during S2–S4 is expected and
costs nothing; it is not a re-decision.

### Not enumerated in S1

Present in the kit at the pinned SHA but outside the seven areas S1 was scoped to. Recorded here
so nothing is silently lost — **no disposition implied**:

- `mcp-configs/` (`README.md`, `mcp-servers.json`) and root `.mcp.json` — **candidate additions to Group C**; raise in S5.
- `.claude-plugin/` (`plugin.json`, `marketplace.json`) — kit packaging; `dotnet-standards` writes its own in S6.
- Root docs: `CLAUDE.md`, `AGENTS.md`, `README.md`, `CHANGELOG.md`, `CONTRIBUTING.md`, `LICENSE` — `LICENSE` is needed for the R9 `NOTICE` obligation in S6.
- Root config: `.editorconfig`, `.gitattributes`, `.gitignore`, `opencode.json` — `.editorconfig` is a **candidate addition to Group D**; raise in S5.
- Other-agent ports: `.codex/`, `.cursor/`, `.opencode/`, `.github/`, `docs/` — no relevance to this plugin.

## Progress

- Group A (knowledge skills + review-rubric anchors): **11/34 decided** — batch 1 complete (S2)
- Group B (process layer: meta/workflow skills, agents, hooks): **0/32 decided**
- Group C (MCP): **0/1 decided**
- Group D (rules, knowledge files, templates): **0/27 decided**
- **Total: 11/94 decided**

> Denominators changed in S2: A30 `scaffold` moved from Group A to Group B as **B32**. Group A 35→34,
> Group B 31→32, total unchanged at 94.
>
> **Batch 1 (S2)** decided A01, A03, A06, A08, A17, A18, A23, A25, A28, A31, A35 — 7 `keep-tweak`,
> 4 `skip`, and no `adapt`, `rebuild` or `combine`. Batch 2 (S3) takes the remaining 23 Group A rows.

---

## Group A — Knowledge skills

> Columns per rule R1 (Provenance), R2 (Destination — gateway skill + `references/*.md` file),
> R6 (Upgrade candidate), R7 (Canonical source), R8 (Anti-examples).
> For `adapt`/`rebuild` rows: fill Canonical source (project → feature/paths) and tick Sanitized
> after distillation review. `adapt` is gated on the user naming exemplar files (R6).
> Out-of-scope areas (Blazor, modular monolith/microservices, CI/CD, Docker, K8s, container
> publishing, Aspire) are short-circuited to `skip` with reason `out-of-scope v1` (R4) — no deep
> reading.

| # | Path | Summary | Status | Provenance | Destination (gateway → references file) | Canonical source (project → feature/paths) | Anti-examples | Sanitized? | Reason / Notes | Upgrade candidate? |
|---|---|---|---|---|---|---|---|---|---|---|
| A01 | `skills/api-versioning/` | Asp.Versioning: URL segment, header and query strategies, deprecation, OpenAPI integration. | keep-tweak | from-kit | `api-surface` → `references/versioning.md` | — | — | ☐ | R6: no exemplars — the user's API has no versioning today, so this is reference material, not the user's convention. Not skipped: brainstorm §4 #6 lists versioning inside `api-surface`. Tweak: the samples wire versions through `MapOrderEndpointsV1()` extension methods, which contradicts both the kit's own `IEndpointGroup` rule and the user's Controllers stack — rewrite for controllers before shipping. | yes — promote to `adapt` when versioning is introduced |
| A02 ⇄ | `skills/arch-check/` | Conformance check of a codebase against its declared architecture via Roslyn MCP — dependency direction, layer violations, module leaks, cycles. | pending | pending | pending | — | — | ☐ | Review-rubric anchor for `dotnet-architecture-review` (brainstorm §5). | pending |
| A03 | `skills/architecture-advisor/` | Questionnaire that recommends VSA / Clean / DDD+Clean / Modular Monolith. | skip | — | — (none) | — | — | ☐ | A four-way architecture selector. `dotnet-standards` encodes exactly one architecture — the user's — so the selector has no consumer among the 15 gateways (§4). **Not an R4 short-circuit**: architecture selection is not in the §2 exclusion list, so this is a reasoned skip and must not be read as one. Half the content (Modular Monolith, microservices evolution paths) would be R4 material regardless. | no |
| A04 | `skills/aspire/` | .NET Aspire AppHost, service defaults, service discovery, Aspire dashboard. | pending | pending | pending | — | — | ☐ | R4 short-circuit candidate — Aspire is out of scope v1. | pending |
| A05 | `skills/authentication/` | JWT bearer, OIDC, ASP.NET Identity, authorization policies, role/claim auth, API keys. | pending | pending | pending | — | — | ☐ | Touches open question Q5 (exemplars vs `from-kit`). | pending |
| A06 | `skills/caching/` | HybridCache (kit default), output and response caching, distributed cache patterns, stampede protection. | keep-tweak | from-kit | `distributed-caching` → `references/hybridcache-and-output-caching.md` | — | — | ☐ | R6: no exemplars named. The kit defaults to HybridCache; the user's stack is Redis distributed caching (§2), and the kit demotes manual `IDistributedCache` to "legacy" — a live divergence to resolve in S7, not by triage. The Redis cache-aside substance the user actually runs is **not** captured by this row. S1's R3 combine proposal (kit `caching` + Redis material → one gateway) is **rejected in S2**; 1-1 mapping retained. | yes |
| A07 | `skills/ci-cd/` | GitHub Actions and Azure DevOps YAML pipelines: build, test, publish, deploy. | pending | pending | pending | — | — | ☐ | R4 short-circuit candidate — CI/CD is out of scope v1. | pending |
| A08 | `skills/clean-architecture/` | 4-project layout (Domain, Application, Infrastructure, Api), dependency inversion, use-case handlers, infrastructure as plugin. | skip | — | — (none) | — | — | ☐ | The user's architecture is explicitly **not** Clean Architecture and stays unnamed until S7 (Q1). With `combine` rejected in S2, the only way to retain A08 is to ship a CA `references/` file inside `solution-architecture ⚠️` — which would pre-empt Q1. The four architecture-neutral anti-pattern blocks (anemic domain model, DbContext in the Domain layer, fat endpoints, repository-per-entity) are preserved in the S2 decision log as candidate R8 material. | no — re-raise in S7 only if Q1 warrants |
| A09 ⇄ | `skills/code-review/` | Roslyn-MCP multi-dimensional review with blast-radius prioritisation and severity-rated findings. | pending | pending | pending | — | — | ☐ | Review-rubric anchor for `dotnet-code-review` (brainstorm §5). Name collides with the built-in `/code-review` — rubric, not command. | pending |
| A10 | `skills/configuration/` | Options pattern, `IOptions` vs `IOptionsSnapshot`, secrets management, environment-based configuration. | pending | pending | pending | — | — | ☐ | — | pending |
| A11 | `skills/container-publish/` | Dockerfile-less SDK container publishing: MSBuild properties, chiseled images, multi-arch, registry push. | pending | pending | pending | — | — | ☐ | R4 short-circuit candidate — container publishing is out of scope v1. | pending |
| A12 | `skills/ddd/` | Tactical DDD: aggregates, aggregate roots, value objects, domain events, domain services, strongly-typed IDs, repositories. | pending | pending | pending | — | — | ☐ | — | pending |
| A13 ⇄ | `skills/de-sloppify/` | 7-step cleanup pipeline: format, unused usings, analyzer warnings, dead code, TODOs, sealed audit, CancellationToken propagation. (+ref: `references/cleanup-steps.md`) | pending | pending | pending | — | — | ☐ | Review-rubric anchor for `dotnet-code-review` (brainstorm §5). | pending |
| A14 | `skills/dependency-injection/` | Service lifetimes, keyed services, decorator and factory patterns, captive-dependency pitfalls. | pending | pending | pending | — | — | ☐ | — | pending |
| A15 | `skills/docker/` | Multi-stage Dockerfile builds, .NET container images, non-root user, health checks, `.dockerignore`, compose for local dev. | pending | pending | pending | — | — | ☐ | R4 short-circuit candidate — Docker is out of scope v1. | pending |
| A16 ⇄ | `skills/dotnet-init/` | Interactive project initialisation: detects project type, asks architecture questions, generates a tailored `CLAUDE.md`. | pending | pending | pending | — | — | ☐ | Generates tier-3 `CLAUDE.md` — templates are backlog, not v1 (brainstorm §2). | pending |
| A17 | `skills/ef-core/` | DbContext configuration, migrations workflow, interceptors, compiled queries, `ExecuteUpdateAsync`/`ExecuteDeleteAsync`, value converters, query optimisation. | keep-tweak | from-kit | `ef-core-data-access` → `references/dbcontext-and-queries.md` | — | — | ☐ | R6: no exemplars named, so `adapt` is unavailable. **Highest-priority upgrade candidate in batch 1** — the user has substantial real EF Core code. Kit content is .NET 10-current and architecture-neutral, so it survives Q1. Tweaks: Npgsql-specific samples must be checked against the user's provider; the kit's "never wrap DbContext in a repository" stance is an opinion that must be confirmed against the user's code before it becomes a rule of this plugin. | yes — priority 1 |
| A18 | `skills/error-handling/` | Result pattern, ProblemDetails (RFC 9457), global exception handling, FluentValidation, structured error responses. | keep-tweak | from-kit | `error-handling` → `references/result-and-problemdetails.md` | — | — | ☐ | R6: no exemplars named. Kit content is strong and standards-anchored (RFC 9457). **Transport mismatch:** `ValidationFilter<T>`, `ToProblemDetails()` and the endpoint-filter wiring all assume Minimal API, but the user runs Controllers (recorded S2) — delivery must be re-expressed via `ProblemDetailsFactory` / action filters / `[ApiController]` model-state handling. The Result pattern itself is transport-neutral and carries over unchanged. | yes |
| A19 ⇄ | `skills/health-check/` | 8-dimension A–F project report card (build, quality, architecture, coverage, dead code, API surface, security, docs) via Roslyn MCP. (+ref: `references/grading-rubric.md`) | pending | pending | pending | — | — | ☐ | Project-assessment workflow; may belong in Group B. Distinct from ASP.NET health-check *endpoints*, which live in `skills/logging/`. | pending |
| A20 | `skills/httpclient-factory/` | Named, typed and keyed HTTP clients, DelegatingHandlers, `Microsoft.Extensions.Http.Resilience`, testing patterns. | pending | pending | pending | — | — | ☐ | Overlaps A29 `resilience` — combine candidate (R3). | pending |
| A21 | `skills/logging/` | Observability glue: ASP.NET health check endpoints, correlation IDs, log-level strategy; delegates deep setup to `serilog` and `opentelemetry`. | pending | pending | pending | — | — | ☐ | Touches open question Q5. Combine candidate with A33/A26 (R3). | pending |
| A22 | `skills/messaging/` | Wolverine and MassTransit, outbox pattern, saga/choreography, RabbitMQ and Azure Service Bus configuration. | pending | pending | pending | — | — | ☐ | — | pending |
| A23 | `skills/minimal-api/` | Minimal APIs: `MapGroup`, endpoint filters, `TypedResults`, OpenAPI metadata, parameter binding, route conventions. | keep-tweak | from-kit | `api-surface` → `references/minimal-api.md` | — | — | ☐ | **The user's API is Controllers (MVC), not Minimal API** (recorded S2). `IEndpointGroup` auto-discovery, `TypedResults` and endpoint filters describe a shape the user does not use — kept as reference material only, never as the user's convention. Consequence: `api-surface` needs a new `references/controller-conventions.md` written from the user's code. That is a *new* component with no kit row; raise it in S3, do not decide it here. | yes — but the upgrade path is a controllers rewrite, not a distillation of this file |
| A24 | `skills/modern-csharp/` | C# 14 / .NET 10 features: primary constructors, collection expressions, the `field` keyword, extension members, records, pattern matching, spans, raw strings. | pending | pending | pending | — | — | ☐ | — | pending |
| A25 | `skills/openapi/` | Built-in .NET 10 OpenAPI: document generation, document/operation/schema transformers, security schemes, XML comments, build-time generation. | keep-tweak | from-kit | `api-surface` → `references/openapi.md` | — | — | ☐ | Built-in .NET 10 OpenAPI is framework fact, not a per-project convention, so the kit is the right source and no exemplar is needed. **Divergence:** the user runs Swashbuckle (see A31), which this skill's central claim explicitly rejects — flagged for S7; S2 does not migrate a toolchain by triage. Tweak: strip the `TypedResults`-only framing, since controllers use `[ProducesResponseType]`. S1's R3 combine with A31 is **rejected** and moot — A31 is skipped. | no — framework knowledge, not exemplar-derived |
| A26 | `skills/opentelemetry/` | Traces, metrics and logs via the OpenTelemetry SDK with OTLP export; custom `ActivitySource`, `IMeterFactory`, resource configuration. | pending | pending | pending | — | — | ☐ | Touches open question Q5. | pending |
| A27 | `skills/project-setup/` | Tech-stack selection advisor: recommended defaults for database, auth, caching, messaging, observability, resilience, with rationale. | pending | pending | pending | — | — | ☐ | — | pending |
| A28 | `skills/project-structure/` | `.slnx`, `Directory.Build.props`, central package management via `Directory.Packages.props`, global usings, naming conventions. | keep-tweak | from-kit | `solution-architecture ⚠️` → `references/solution-layout.md` | — | — | ☐ | R6: no exemplars named. Architecture-neutral (`.slnx`, `Directory.Build.props`, central package management, global usings, naming), so it survives Q1 intact — **the only batch-1 architecture row that does**. Tweaks: strip the illustrative `Directory.Packages.props` versions (the kit marks them illustrative itself) and replace the `Mediator.*` entries with MediatR to match the user's stack (§2). The destination gateway name stays provisional until S7. | yes |
| A29 | `skills/resilience/` | Polly v8: retry, circuit breaker, timeout, fallback, rate limiter, hedging, composing resilience pipelines. | pending | pending | pending | — | — | ☐ | Overlaps A20 `httpclient-factory` — combine candidate (R3). | pending |
| A31 | `skills/scalar/` | Scalar API reference UI: setup, themes, authentication prefill, multiple documents, layout, security. | skip | — | — (none) | — | — | ☐ | The user's API docs UI is Swagger UI / Swashbuckle, not Scalar (recorded S2) — no consumer. Recorded as a divergence from both the kit and the .NET 10 default (Swashbuckle was dropped from the templates in .NET 9); resolving it is the user's call in S7, not a triage decision. S1's R3 combine proposal A25 + A31 is **rejected**. | no — re-raise if the user adopts Scalar |
| A32 ⇄ | `skills/security-scan/` | 6-layer security scan: vulnerable packages, secrets detection, OWASP code patterns, auth configuration, CORS policy, data protection. (+ref: `references/scan-layers.md`) | pending | pending | pending | — | — | ☐ | Review-rubric anchor for `dotnet-security-review` (brainstorm §5). Name adjacent to the built-in `/security-review` — rubric, not command. | pending |
| A33 | `skills/serilog/` | Two-stage bootstrap, appsettings configuration, enrichers, sinks, request logging, destructuring, Serilog.Expressions. | pending | pending | pending | — | — | ☐ | Touches open question Q5. | pending |
| A34 | `skills/testing/` | xUnit v3, `WebApplicationFactory` integration tests, Testcontainers, Verify snapshots, AAA pattern. | pending | pending | pending | — | — | ☐ | Deliberate gap: the user writes no tests, so there is no exemplar to adapt (brainstorm §4). Provenance is expected to be `from-kit + from-research`. | pending |
| A35 | `skills/vertical-slice/` | Vertical Slice Architecture: feature folders, endpoint grouping, handler patterns for Mediator, Wolverine and raw handler classes. | skip | — | — (none) | — | — | ☐ | Two halves, both unavailable. The handler-pattern half (Mediator / Wolverine / raw) was the only kit skeleton available to `cqrs-feature-slice`, and S2 decided that gateway is **`rebuild`** (see decision log) — so it is not carried. The feature-folder-layout half is Q1 territory and must not pre-empt S7. Nothing remains to keep. | no — re-raise in S7 if Q1 resolves to a feature-folder architecture |

---

## Group B — Process layer (compare against Superpowers per component)

> Never default to `skip` (rules §4). Each row is compared against the equivalent Superpowers
> capability and assigned `skip` / `keep` / `combine`.
> **R5 — every `keep` and `combine` MUST answer all five conflict-check items** in the row:
> (1) hook events · (2) slash-command names, incl. Claude Code built-ins · (3) skill names ·
> (4) contradicts brainstorm → plan → TDD → review? · (5) agent names.
> An unresolvable conflict downgrades the row to `skip`.
> **Golden rule:** any extension lives inside `dotnet-standards`. No Superpowers file is ever modified.
> **Windows cost:** kit hooks are `.sh`; Claude Code on Windows runs hooks through `CMD.exe`, which
> cannot execute them. Keeping any hook requires shipping a polyglot `run-hook.cmd` wrapper and
> depends on Git for Windows. This cost must appear in the Reason of every hook row.

### B.1 — Meta / workflow skills

| # | Path | Summary | Superpowers equivalent? | Conflict check (R5: 1 hooks · 2 commands · 3 skills · 4 instructions · 5 agents) | Status | Reason |
|---|---|---|---|---|---|---|
| B01 | `skills/build-fix/` | Bounded autonomous build-fix and test-fix loops: `dotnet build`, parse, categorise, fix, rebuild, with progress detection and fail-safe guards. | pending | pending | pending | Brainstorm §6 flags this as the `dotnet-build-loop` candidate — Superpowers has no concept of `dotnet`. |
| B02 | `skills/checkpoint/` | Mid-session save point: descriptive git commit plus a short handoff note, then keep working. | pending | pending | pending | — |
| B03 | `skills/convention-learner/` | Detects and enforces project-specific conventions (naming, folder structure, test organisation, style) by analysing the existing codebase. | pending | pending | pending | — |
| B04 | `skills/instinct-system/` | Confidence-scored instincts, user corrections captured as permanent rules, organic discoveries logged; status/export/import modes. | pending | pending | pending | Writes to `.claude/instincts.md`, `MEMORY.md`, `.claude/learning-log.md` — check item 4 carefully. |
| B05 ⇄ | `skills/migrate/` | Guided migration workflow: EF Core schema migrations, .NET version upgrades, NuGet updates, each with rollback and verification. | pending | pending | pending | Half knowledge (EF Core migrations), half workflow; may belong in Group A. |
| B06 ⇄ | `skills/outdated/` | Dependency health report: outdated and vulnerable NuGet packages plus commercial-licence traps, via the `get_nuget_packages` MCP tool. | pending | pending | pending | Depends on the Group C MCP server. |
| B07 | `skills/plan/` | Architecture-aware plan mode producing structured implementation plans before any code. | pending | pending | pending | Direct overlap with `superpowers:writing-plans` and Claude Code plan mode — conflict check item 3 is the crux. |
| B08 | `skills/spec/` | Structured questioning that turns a vague idea into a persisted spec with acceptance criteria under `docs/specs/`. | pending | pending | pending | Overlaps `superpowers:brainstorming`. |
| B09 | `skills/tdd/` | Guided red-green-refactor workflow using xUnit v3, `WebApplicationFactory`, Testcontainers and Verify. | pending | pending | pending | Direct overlap with `superpowers:test-driven-development`; the .NET tooling substance may belong in A34 instead. |
| B10 | `skills/verify/` | 7-phase verification pipeline (build, analyzers, antipattern detection, tests, security, formatting, diff review) with PASS/FAIL gates and short-circuit. | pending | pending | pending | Overlaps `superpowers:verification-before-completion`. |
| B11 | `skills/workflow-mastery/` | Claude Code workflow guidance for .NET: worktrees, plan mode, verification loops, formatting hooks, permissions, subagents, context discipline. | pending | pending | pending | Broad overlap with several Superpowers skills. |
| B12 | `skills/wrap-up/` | End-of-session handoff ritual into `.claude/handoff.md`, plus the session-start protocol that loads it back. | pending | pending | pending | Overlaps `superpowers:finishing-a-development-branch`. |
| B32 ⇄ | `skills/scaffold/` | Architecture-aware feature-slice generation (endpoint, handler, validator, DTOs, EF configuration, integration tests) with a completeness checklist. (+ref: `references/architecture-patterns.md`) | pending | pending | pending | **Moved from Group A (was A30) in S2** — the ⇄ marker anticipated this and the move costs nothing. It is a workflow orchestrator by the kit's own definition (What/When/How/Example/Related body, ≤200-line budget, registers `/scaffold`), so it needs the full R5 conflict check: Step 2 "Clarify scope" overlaps `superpowers:brainstorming`, Step 6 "Verify" overlaps `superpowers:verification-before-completion`, and `/scaffold` is a slash-command name. Its durable knowledge is the 9-item feature-completeness checklist; its CQRS substance lives in `references/architecture-patterns.md`, not in `SKILL.md`. Numbering is non-contiguous by design — B32 keeps the audit trail to A30. |

### B.2 — Agents

> Resolves open question **Q3** — which of the 10 agents (if any) survive Superpowers' review flow.
> Conflict-check item 5 (agent-name collision) applies to every row.

| # | Path | Summary | Superpowers equivalent? | Conflict check (R5) | Status | Reason |
|---|---|---|---|---|---|---|
| B13 | `agents/api-designer.md` | Minimal-API design expert: REST conventions, endpoint and contract shapes, versioning, OpenAPI, authorization policies. | pending | pending | pending | — |
| B14 | `agents/build-error-resolver.md` | Autonomous build fixer: parses `dotnet build` errors, categorises, applies known fix patterns, rebuilds until green. | pending | pending | pending | Pairs with B01. |
| B15 | `agents/code-reviewer.md` | Multi-dimensional .NET review (correctness, maintainability, performance, security, conventions) powered by Roslyn MCP. | pending | pending | pending | Overlaps the Superpowers review flow and A09. |
| B16 | `agents/devops-engineer.md` | Deployment and infrastructure: Docker multi-stage builds, GitHub Actions / Azure DevOps pipelines, Aspire orchestration. | pending | pending | pending | R4 short-circuit candidate — its entire subject area is out of scope v1. |
| B17 | `agents/dotnet-architect.md` | Structure and architecture decision-maker: drives the architecture-advisor questionnaire, recommends VSA / Clean / DDD / Modular Monolith. | pending | pending | pending | — |
| B18 | `agents/ef-core-specialist.md` | EF Core database expert: DbContext design, LINQ efficiency, migrations, interceptors, compiled queries. | pending | pending | pending | — |
| B19 | `agents/performance-analyst.md` | Performance expert: bottlenecks, caching strategy, async/await correctness, allocation reduction. | pending | pending | pending | Kit anchor for the `dotnet-performance-review` rubric (brainstorm §5). |
| B20 | `agents/refactor-cleaner.md` | Cleanup specialist: finds dead code and unused types via Roslyn MCP, removes them with verification between steps. | pending | pending | pending | Pairs with A13. |
| B21 | `agents/security-auditor.md` | Security expert: vulnerability review, auth design (JWT/OIDC/Identity), secrets management, OWASP practice. | pending | pending | pending | Pairs with A32. |
| B22 | `agents/test-engineer.md` | Testing expert: strategy, `WebApplicationFactory` and Testcontainers integration tests, xUnit v3, Verify snapshots. | pending | pending | pending | Pairs with A34 — relevant to the deliberate testing gap. |

### B.3 — Hooks

> Resolves open questions **Q2** (format-hook viability) and **Q4** (the deferred
> `UserPromptSubmit` skill-index hook, mechanism E — a *new* component, not a kit row).
> Every `keep`/`combine` row must state the `run-hook.cmd` Windows cost in its Reason.

| # | Path | Summary | Superpowers equivalent? | Conflict check (R5) | Status | Reason |
|---|---|---|---|---|---|---|
| B23 | `hooks/hooks.json` | Hook registration manifest binding the seven scripts to Claude Code events. | pending | pending | pending | Determines conflict-check item 1 for every other hook row. |
| B24 | `hooks/post-edit-format.sh` | Auto-formats changed `.cs` files after an edit. | pending | pending | pending | Brainstorm §6 `.cs` format hook candidate — conflict check mandatory; Windows `.sh` cost applies. |
| B25 | `hooks/post-scaffold-restore.sh` | Restores NuGet packages after `.csproj` changes. | pending | pending | pending | Windows `.sh` cost applies. |
| B26 | `hooks/post-test-analyze.sh` | Analyses test results and prints an actionable summary. | pending | pending | pending | Windows `.sh` cost applies. |
| B27 | `hooks/pre-bash-guard.sh` | Blocks destructive Bash operations. | pending | pending | pending | Windows `.sh` cost applies. |
| B28 | `hooks/pre-build-validate.sh` | Validates project structure against the expected architecture before build. | pending | pending | pending | Depends on an architecture that is not yet named (Q1). Windows `.sh` cost applies. |
| B29 | `hooks/pre-commit-antipattern.sh` | Detects anti-patterns in staged C# files. | pending | pending | pending | Windows `.sh` cost applies. |
| B30 | `hooks/pre-commit-format.sh` | Verifies code formatting before commit. | pending | pending | pending | Windows `.sh` cost applies. |
| B31 | `hooks/README.md` | The kit's own documentation of its hook set. | pending | pending | pending | Documentation, not a runtime component. |

---

## Group C — MCP

> Default per rules §5: `keep` as an **externally installed dotnet tool**, not copied into the
> plugin. If kept, record the install command and the `.mcp.json` shape in the destination skill's
> `references/` so a future project can wire it up without rediscovery.

| # | Component | Status | Notes |
|---|---|---|---|
| C01 | `mcp/CWM.RoslynNavigator/` — Roslyn-backed MCP server: ~22 navigation and analysis tools (find symbol/references/callers/implementations/overrides, type hierarchy, dependency and project graphs, DI registrations, endpoint map, public API, diagnostics, dead code, NuGet packages, test-coverage map) plus 10 anti-pattern detectors. | pending | Default: keep as external dotnet tool, not copied into the plugin. Prerequisite for A02, A09, A19, A32, B06, B15, B20. Root `.mcp.json` and `mcp-configs/` are not enumerated in S1 — raise them here in S5. |

---

## Group D — Rules, knowledge & templates

> Evaluated individually (rules §6). The kit's own rules mechanism is not preserved by default.
> Every row records a Destination: **skill content** · **project `CLAUDE.md` material** · **drop**.
> Note: shipping a per-project `CLAUDE.md` template is backlog, not v1 (brainstorm §2) — a
> Destination of "project `CLAUDE.md` material" means *recorded for tier 3*, not *shipped*.

### D.1 — `.claude/rules/`

| # | Path | Summary | Status | Destination | Reason |
|---|---|---|---|---|---|
| D01 | `.claude/rules/agents.md` | Agent and tool usage rules. | pending | pending | — |
| D02 | `.claude/rules/architecture.md` | Architecture rules. | pending | pending | Gated on Q1 — the user's architecture is unnamed until S7. |
| D03 | `.claude/rules/coding-style.md` | C# coding style rules. | pending | pending | — |
| D04 | `.claude/rules/error-handling.md` | Error handling rules. | pending | pending | — |
| D05 | `.claude/rules/git-workflow.md` | Git workflow rules. | pending | pending | Process layer — likely owned by Superpowers. |
| D06 | `.claude/rules/hooks.md` | Hook authoring rules. | pending | pending | Relevant to the B.3 decisions. |
| D07 | `.claude/rules/packages.md` | Package management rules. | pending | pending | — |
| D08 | `.claude/rules/performance.md` | Performance rules. | pending | pending | Candidate material for the `dotnet-performance-review` rubric. |
| D09 | `.claude/rules/security.md` | Security rules. | pending | pending | Candidate material for the `dotnet-security-review` rubric. |
| D10 | `.claude/rules/testing.md` | Testing rules. | pending | pending | Relevant to the deliberate testing gap (A34). |

### D.2 — `knowledge/`

| # | Path | Summary | Status | Destination | Reason |
|---|---|---|---|---|---|
| D11 | `knowledge/breaking-changes.md` | Breaking changes: .NET 9 → .NET 10 migration guide. | pending | pending | — |
| D12 | `knowledge/common-antipatterns.md` | Catalogue of common .NET anti-patterns. | pending | pending | Strong R8 anti-example material for the review rubrics. |
| D13 | `knowledge/common-infrastructure.md` | Common infrastructure building blocks. | pending | pending | — |
| D14 | `knowledge/dotnet-whats-new.md` | What's new in .NET 10 and C# 14. | pending | pending | Overlaps A24 `modern-csharp`. |
| D15 | `knowledge/mediatr-to-mediator-migration.md` | MediatR → Mediator migration guide (commercial-licence driven). | pending | pending | Directly relevant: the user's CQRS pipeline is MediatR-based (brainstorm §2). |
| D16 | `knowledge/package-recommendations.md` | Vetted NuGet package recommendations. | pending | pending | — |

### D.3 — `knowledge/decisions/` (ADRs)

> These are the *kit author's* architectural decisions, not the user's. Each is judged on whether
> it matches the user's real conventions — several are known to diverge.

| # | Path | Summary | Status | Destination | Reason |
|---|---|---|---|---|---|
| D17 | `knowledge/decisions/001-vsa-default.md` | ADR-001: Vertical Slice Architecture as the default. | pending | pending | The user's architecture is explicitly not yet named (Q1) — do not inherit this default. |
| D18 | `knowledge/decisions/002-result-over-exceptions.md` | ADR-002: Result pattern over exceptions for control flow. | pending | pending | Relevant to the `error-handling` gateway. |
| D19 | `knowledge/decisions/003-ef-core-default-orm.md` | ADR-003: EF Core as the default ORM. | pending | pending | — |
| D20 | `knowledge/decisions/004-hybrid-cache-default.md` | ADR-004: HybridCache over manual `IDistributedCache` patterns. | pending | pending | The user's stack uses Redis distributed caching — likely divergence, see A06. |
| D21 | `knowledge/decisions/005-multi-architecture.md` | ADR-005: multi-architecture support. | pending | pending | `dotnet-standards` targets exactly one architecture — the user's. |
| D22 | `knowledge/decisions/template.md` | Blank ADR template. | pending | pending | — |

### D.4 — `templates/`

> Each template directory holds a `CLAUDE.md` and a `README.md`.

| # | Path | Summary | Status | Destination | Reason |
|---|---|---|---|---|---|
| D23 | `templates/blazor-app/` | Blazor application project template. | pending | pending | R4 short-circuit candidate — Blazor is out of scope v1. |
| D24 | `templates/class-library/` | Class library / NuGet package project template. | pending | pending | — |
| D25 | `templates/modular-monolith/` | Modular monolith project template. | pending | pending | R4 short-circuit candidate — modular monolith is out of scope v1. |
| D26 | `templates/web-api/` | Web API project template. | pending | pending | Matches an in-scope project shape (brainstorm §2). Tier-3 templates are backlog, not v1. |
| D27 | `templates/worker-service/` | Worker service project template. | pending | pending | Matches an in-scope project shape (brainstorm §2). Tier-3 templates are backlog, not v1. |

---

## Decision log (append-only)

| Date | Session | Component | Decision | Why |
|---|---|---|---|---|
| 2026-07-25 | S1 | Kit inventory — `skills/` | Inventory correction, no disposition: **47** skill directories at the pinned SHA, not 46 as stated in `00-brainstorm.md` §9. Plus 4 `references/*.md` files inside 4 of those skills. | Counted from `git ls-tree -r HEAD` at `cd83d31`. Group A/B denominators are built on 47. |
| 2026-07-25 | S1 | Kit inventory — `hooks/` | Inventory correction, no disposition: **7** `.sh` scripts at the pinned SHA, not 8 as stated in `00-brainstorm.md` §9. Directory holds 9 files total (7 scripts + `hooks.json` + `README.md`). | Counted from `git ls-tree -r HEAD` at `cd83d31`. Group B.3 has 9 rows. |
| 2026-07-25 | S1 | Enumeration scope | Recorded, no disposition: `mcp-configs/`, root `.mcp.json` and root `.editorconfig` are outside the seven areas S1 was scoped to but are plausible Group C / Group D members. | S1's scope was fixed to `skills/`, `agents/`, `hooks/`, `knowledge/`, `templates/`, `mcp/`, `.claude/rules/`. Raise these in S5 rather than losing them. |
| 2026-07-25 | S2 | Gateway `cqrs-feature-slice` (brainstorm §4 #2) | **`rebuild`.** The gateway is written from scratch; no kit component supplies its skeleton. | The kit ships no `cqrs` or `mediatr` skill. The nearest skeleton was A35 `vertical-slice` Pattern A (nested static class holding `Command`/`Response`/`Validator`/`Handler`, plus a `ValidationBehavior` pipeline). The user chose `rebuild` over `adapt`-on-A35: the kit's pipeline is **Mediator** (source-generated) while the user's is **MediatR**, and the kit has **no AutoMapper anywhere** — it prescribes direct `.Select()` projection instead, which is the opposite of the user's stack (§2: MediatR + FluentValidation + AutoMapper). The skeleton would have had to be rewritten in its two load-bearing places, so it is not a skeleton. Consequences: A35 becomes `skip`; D15 `knowledge/mediatr-to-mediator-migration.md` is now **less** relevant, not more (the user is staying on MediatR, not migrating away) — revisit in S5; the AutoMapper vs projection divergence is an R7 "which one from now on?" question owed to S8. |
| 2026-07-25 | S2 | A30 `skills/scaffold/` | **Moved Group A → Group B as B32.** No disposition; S4 decides it. | The row carried a ⇄ marker and moving it costs nothing (see "Group assignment is provisional"). It is a workflow orchestrator by the kit's own definition, and three R5 conflict-check items fire on it at once (Step 2 vs `superpowers:brainstorming`, Step 6 vs `superpowers:verification-before-completion`, `/scaffold` as a slash-command name). Deciding it in Group A would have skipped a mandatory conflict check. Its 9-item feature-completeness checklist is the knowledge worth salvaging and its per-architecture code templates live in `references/architecture-patterns.md`, which S2 did not read. |
| 2026-07-25 | S2 | R3 combine proposals from S1 | **Rejected.** 1-1 mapping retained: one kit skill → one `references/*.md` file. | S1 proposed A25 `openapi` + A31 `scalar` → one `api-surface` gateway, and A06 `caching` → `distributed-caching`. S2 also considered widening to A01 + A23 + A25 + A31 → one `api-surface`. The user rejected all of them in favour of traceability: every kit row maps to exactly one destination file, so any decision can be audited back to its source at the pinned SHA. Packaging mechanism **A** is unaffected — many kit skills still land in one gateway skill, they simply keep separate `references/` files. |
| 2026-07-25 | S2 | Group A architecture material (A03, A08, A35) | **Three of the four batch-1 architecture rows are `skip`.** Only A28 `project-structure` survives. | Not an accident of triage: the kit is multi-architecture **by design** (ADR-005, D21 — it supports VSA, CA, DDD+CA and Modular Monolith and ships an advisor to choose between them), while `dotnet-standards` encodes exactly one architecture, the user's, which is unnamed until S7. A selector (A03) has no consumer; a CA prescription (A08) and a VSA prescription (A35) would each pre-empt Q1. A28 survives precisely because it is architecture-neutral. The `solution-architecture ⚠️` gateway therefore has almost no kit inheritance and will be substantially `from-my-code` in S7. |
| 2026-07-25 | S2 | A08 `clean-architecture` — salvaged material | Recorded, no disposition: four architecture-neutral anti-pattern blocks are preserved here so the `skip` does not destroy them. | The blocks are: **anemic domain model** (entity as data bag, logic in the handler), **DbContext in the Domain layer** (Domain depending on Infrastructure), **fat endpoints** (business logic in the route handler), and **repository-per-entity** (duplicating `DbSet<T>`). None prescribes a layering, so none pre-empts Q1. Candidate R8 material for `solution-architecture ⚠️` and for the `dotnet-architecture-review` rubric; re-raise in S7. |
| 2026-07-25 | S2 | User stack facts | Recorded, no disposition — these drive S3 and S7: **Controllers (MVC)**, not Minimal API · **Swagger UI / Swashbuckle**, not Scalar · **no API versioning** · MediatR + FluentValidation + AutoMapper (§2). | Stated by the user in S2. Two are live divergences from both the kit and the .NET 10 defaults: Swashbuckle was dropped from the templates in .NET 9 and the kit forbids it outright, and the kit treats Minimal API as the default with controllers as legacy. S2 does **not** migrate the user's toolchain by triage — the "keep Swashbuckle or move to built-in OpenAPI + Scalar" and "controllers or Minimal API from now on" questions are R7 decisions owed to S7. Immediate consequence: `api-surface` needs a `references/controller-conventions.md` with no kit row behind it — a new component, raise in S3. |
| 2026-07-25 | S2 | R6 gate | No exemplar files were named, so **no batch-1 row is `adapt`.** A01, A06, A17, A18, A23, A28 are `keep-tweak` + `upgrade candidate`. | The user chose to close S2 without naming paths in `reference/projects/apsp-backend`. R6 is explicit that the fallback is `keep-tweak` + `upgrade candidate`, not `adapt`. Promotion to `adapt` happens in the adapt sessions (S7–S8) when files are named. A17 `ef-core` is flagged priority 1 for promotion. S2 did not open `reference/projects/`. |
| 2026-07-25 | S2 | Context discipline (hard constraint 2) | Recorded, no disposition: reading kit `SKILL.md` files caused the harness to auto-inject the kit's root `CLAUDE.md` and all ten `.claude/rules/*.md` files into the session context. | Unintended widening, not an announced one. Effect: S2 saw the content behind rows D01–D10 before Group D is scheduled to be triaged. **No Group D disposition was made** and none may be inferred from S2. Future sessions reading kit skill files should expect the same injection and budget for it. |
