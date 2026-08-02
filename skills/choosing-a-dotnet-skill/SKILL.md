---
name: choosing-a-dotnet-skill
description: >-
  This skill should be used when working in a .NET, C# or ASP.NET Core codebase
  with no dotnet-standards skill chosen — before reading, searching or listing
  files — or when the owning skill is unclear: two skills seem plausible, a
  "Not for" pointer led to nothing to load, or the task spans several areas.
  Also when brainstorming, writing a spec or plan, or composing subagent
  prompts, to name the skill each step requires. Not for:
  the process skills themselves — brainstorming, planning, TDD — Superpowers;
  a question already matched to one skill — load that skill directly.
---

## First: is the work being planned, or written?

**If a spec, plan, design document or subagent prompt is being written, read
*When the work is being planned* below before using the tables.** That case
routes differently — a plan touches several areas at once, so "one row, then
stop" is the wrong shape and following it is how a convention gets decided with
no skill looking.

## How to use these tables

Find the row that matches the situation, load that one skill, and work from it.
If exactly one base-map row fits, load it and stop — *unless* the work is being
planned rather than written, which is the case above. If none fits, see *When
nothing here fits*.

Each row names a **single entry point, never a sequence**. A task spanning
several areas routes to the skill owning its *first* decision; that skill's own
`Not for:` list carries the work onward. A whole new feature, for instance,
starts at `facade-module-architecture` if its module does not exist yet, and at
`module-feature` once it does.

This table routes — it does not teach. Every row deliberately says less about a
skill than that skill's own description does: enough to choose, never enough to
act on. Having chosen, load the skill.

Rows marked *not yet covered* mean no skill here owns that area yet — see
*Not yet covered*.

## When the work is being planned, not yet written

A generic prompt — *implement feature X* — carries none of the nouns these
skills trigger on, so nothing fires while the spec, the plan, or the subagent
prompt is being written. That is precisely when the conventions get decided,
with no skill looking.

So work step by step. For each spec step, plan step, or subagent prompt — e.g.
while running Superpowers brainstorming, plan writing, or subagent-driven
development — identify the area the step touches and look it up in the tables
below. **Where a shipped skill owns that area, the step must name it, and the
skill must be loaded before the step is written — not when the code is typed.**
A step reading *add the endpoint* leaves whoever executes it matching on
descriptions alone, which is the failure this router exists to prevent; a step
reading *add the endpoint (`api-surface`)* does not. Steps in uncovered areas
have nothing to name — say so in the step if it helps the plan.

A plan touching four areas loads four skills. That is the expected cost of
planning, not a sign of over-reach — the one-row rule governs a question already
in front of you, never a document that decides many.

**A capability the repository does not have yet is the strongest reason to load
its skill, not a reason to skip it.** When the plan introduces MediatR, caching,
a lock or a test library that is absent today, the owning skill governs that
plan from its first line. A `CLAUDE.md` note that the capability is missing
records the tree as it stands; it never retires the skill that owns it.

## Composing with Superpowers process skills

A Superpowers process skill running the session does not suspend this layer.
Three rules, all broken in one real session on 2026-08-02:

- **"Do NOT invoke any other skill" bars implementation skills, not this one.**
  The same ban is stated twice more with the word *implementation* in it. A
  knowledge skill is an input to the design, so load it before a brainstorm
  answer, a plan step or a subagent prompt states a .NET convention.
- **Re-read this file whenever the work changes phase** — design, code, test,
  review. It is a lookup table, not a briefing: consulted once for the first
  question of a session, it will not be there when the work changes nature.
- **A subagent that reviews or tests .NET code is `dotnet-review-flow`'s**, with
  the agents that flow names — never a general-purpose agent carrying a
  hand-written constraint block. A process skill that hard-codes
  `general-purpose` is naming a default, not forbidding these.

When the whole task is a .NET feature or a .NET review rather than one question,
the row to use is `dotnet-feature-flow` or `dotnet-review-flow` below.

## When the harness is not Claude Code

The skills are harness-neutral and ship identically everywhere. The hooks, the
six agents and the two commands do not travel inside the plugin — on Codex they
are installed beside it by `codex/install.sh`, and **whether that ran decides
what this session has**:

| If the Codex kit was installed | If it was not |
|---|---|
| The router nudge fires on the first prompt, as on Claude Code | Nothing announces this router. Consult this file yourself: at session start, and again at every phase change. |
| `/dotnet-feature` and `/dotnet-review` are in the slash menu | Load `dotnet-feature-flow` or `dotnet-review-flow` by name — the commands were only ever thin entries into those two skills |
| The six agent names may resolve for spawning | `dotnet-review-flow` preflight #3 owns this case, and its fallback is four sequential lenses — never one merged pass |

**Do not assume the agents exist merely because the kit was installed.** Codex
surfaces differ in whether custom agents resolve; preflight #3 checks the roster
rather than the install, which is the only check that cannot be fooled.

**The instructions file is `CLAUDE.md` on every harness.** Where a harness reads
`AGENTS.md` — Codex does — that file is a pointer at `CLAUDE.md` carrying no
rules of its own. A repository with neither, or with rules stranded in
`AGENTS.md`, is `claude-md-builder`'s job, not a licence to write a second rule
set for this harness.

## Base map — one area, one skill

Ordered by build sequence: placement → behaviour → messaging → data → HTTP →
mapping → failure → text → capabilities → tests → review → flows → project
memory.

| The question in hand | Load |
|---|---|
| Where a file, project, facade or module belongs; project references; the composition root | `facade-module-architecture` |
| A service and its validation inside a module: rules, `IsExist`/`ThrowIf` guards, MediatR envelopes | `module-feature` |
| Dispatching a message in-process through MediatR: notifications and requests, their handlers and registration, pipeline behaviours | `mediatr-messaging` |
| Repositories and queries, entities and their configurations, migrations, transactions, seeding | `ef-core-data-access` |
| A route, controller action, request or response DTO chain, pagination or search contract, or OpenAPI setup | `api-surface` |
| Mapping one type onto another with AutoMapper: profiles, their conventions, registration | `automapper-mapping` |
| Excel with MiniExcel: exporting rows to .xlsx (plain or via a designed template), importing an uploaded workbook or a zip of workbook plus images, serving or replacing the import template, staging imported rows for confirm | `excel-miniexcel` |
| Calling out over HTTP: the IHttpClientSender chain and HttpResult, content via ToStringContent/ToFormUrlEncodedContent/ToFormDataContent and [FormName], HttpClientSettings partials and httpclient.json, typed AddHttpClient clients, or recreating the sender facade | `http-client-factory` |
| Storing files in S3: the storage facade and its recreation, uploading IFormFile/Stream/directory, bucket keys, pre-signed vs public vs service URLs, `S3FilePath` on responses, attachment downloads, deleting objects, ingesting an external URL | `file-storage` |
| Writing, porting or repairing the list-query extensions themselves - QueryExpressionExtension/PaginationExtension/ApplyQuery source, the $eq...$sw operator table, CustomFilterBinder, or ApplyFilter/ToPagedListAsync not resolving in a project without the pipeline | `list-query-pipeline` |
| Reaching for a helper, utility, extension method or attribute — regex, random string, generated password, client IP, JSON serialize, Expression composition, reusable validation rule — adding to Infrastructure/Facades/Common/, or a project missing a house extension you are about to inline | `common-extensions` |
| Which exception to throw, status codes, how the middleware turns a throw into a response | `error-handling` |
| The text a validator, success path or exception shows the user; message keys | `message-keys` |
| Authentication and authorization: schemes and tokens, permission grants and checks, the current principal, API keys, auth secrets | `auth-and-security` |
| Caching data in Redis: the cache facade, keys, TTL, invalidation | `distributed-caching` |
| Keeping two requests from processing one resource at once: locks, `LockedException` | `distributed-lock` |
| Full-text search: the search facade, documents, indexing, reindexing | `elasticsearch-search` |
| Writing or changing tests: unit, integration, fixtures, test doubles | `dotnet-testing` |
| Reviewing changed code: review depth and blast radius, finding severity, the review report, cleanup and simplification candidates, over-build and unnecessary complexity | `dotnet-code-review` |
| Reviewing a solution's architecture: dependency direction and project references, layer and namespace leaks, placement conformance, the composition root | `dotnet-architecture-review` |
| Reviewing security posture: committed secrets and key handling, missing authorization gates, injection and mass assignment, data exposure through DTOs, logs and error responses | `dotnet-security-review` |
| Reviewing performance: round-trip counts and N+1, page-size and index coverage, blocking calls, cache, lock and search cost | `dotnet-performance-review` |
| Running the test-and-review fleet — parallel tester and reviewer subagents with verified findings — over a diff, a branch, or a set of paths whose code never changed | `dotnet-review-flow` |
| Taking one .NET feature end to end as a single flow: brainstorm, plan, human gates, implement, test loop, review loop, commit | `dotnet-feature-flow` |
| Writing or refreshing the repository's own `CLAUDE.md`: the commands, layout facts and hard rules a session must hold, trimming that file back under 200 lines, or giving the repository the `AGENTS.md` pointer a Codex session reads | `claude-md-builder` |

## When two skills both look right

These tokens mean different things in different areas, so description matching
alone picks wrong. Match the question, not the word.

| The shared token | The split |
|---|---|
| 401 / 403 | thrown as `UnAuthorizedException` / `ForbiddenException` — `error-handling`; putting `[HasPermission]` on an action — `api-surface`; schemes, policies, what that attribute enforces — `auth-and-security` |
| a controller | which folder its file goes in — `facade-module-architecture`; the route, action body and attributes — `api-surface`; `try`/`catch` and building an error inside one — `error-handling` |
| an exception | which to throw and how it becomes a response — `error-handling`; the text it carries — `message-keys`; one raised because a resource was already being processed — `distributed-lock`; where the class itself lives — `facade-module-architecture` |
| mapping / `ProjectTo` | projecting inside a query — `ef-core-data-access`; where the profile file sits beside its DTO — `api-surface`; how to write the mapping itself — `automapper-mapping` |
| "message" | text a user will read — `message-keys`; an in-process command, query or event envelope — `module-feature`; dispatching that envelope and the handler that receives it — `mediatr-messaging` |
| pagination | the request and response contract — `api-surface`; executing the paged read — `ef-core-data-access`; the extension source itself — `list-query-pipeline` |
| a cache that went stale | a Redis value not invalidated — `distributed-caching`; a permission check still passing after a grant changed — `auth-and-security` |
| soft delete / hidden rows | the stamps, the repository filter and the escape hatch — `ef-core-data-access`; why `BaseEntity` carries no flag — `facade-module-architecture` |
| HTTP | an inbound route, controller or DTO — `api-surface`; the outbound call through the sender facade — `http-client-factory` |
| a file over the wire | uploading it to a third-party API or pulling bytes through the sender — `http-client-factory`; object storage and the download-then-store workflow — `file-storage` |
| placement / project references / the composition root | deciding where a file, project or registration goes — `facade-module-architecture`; checking whether what is already there conforms — `dotnet-architecture-review` |
| a query | against the database — `ef-core-data-access`; full-text or index search — `elasticsearch-search`; the in-process query envelope — `module-feature`; dispatching it and its handler — `mediatr-messaging` |
| Redis | storing or invalidating a cached value — `distributed-caching`; making two callers take turns — `distributed-lock` |
| `Repository<T>()` / "repository" | through the data-access wrapper — `ef-core-data-access`; through the search wrapper — `elasticsearch-search`; a brand-new source repository to bootstrap — *not yet covered* |
| secrets / tokens / authorization gates | deciding what the rule is — schemes, grants, the current principal, auth settings — `auth-and-security`; checking whether what is already there is safe — `dotnet-security-review` |
| a convention or a rule | deciding what it should say — the owning knowledge skill in the base map; recording that it governs *this* repository, in its `CLAUDE.md` — `claude-md-builder`; checking whether the code follows it — the four review rubrics |
| a Settings class | where the file lives — `facade-module-architecture`; `DatabaseSettings` — `ef-core-data-access`; `RedisSettings` — `distributed-caching`; `ElasticsearchSettings` — `elasticsearch-search`; `ConcurrencySettings` — `distributed-lock`; `SecuritySettings`, `JwtSettings` — `auth-and-security` |
| `AGENTS.md` / `CLAUDE.md` / "the instructions file" | writing or trimming the rules — `claude-md-builder`, into `CLAUDE.md` always; `AGENTS.md` is that skill's pointer file and holds no rules; a rule's *content* — the owning knowledge skill |
| a validator | where the file sits beside its DTO — `api-surface`; the rule and its guards — `module-feature`; the text a failing rule emits — `message-keys` |
| "this is slow" / performance cost | what the query, cache, lock or search shape should be — its owning skill; grading what code costs in a review — `dotnet-performance-review` |
| "review" | the rubric applied while reading changed code yourself — `dotnet-code-review` and its three sibling lenses; running the subagent fleet with the test loop, over a diff or over unchanged code under given paths — `dotnet-review-flow`; the whole feature process that ends in that review — `dotnet-feature-flow`; the request/receive review discipline — Superpowers |
| spawning a subagent | one that reviews or tests .NET code — `dotnet-review-flow` and the agents it names, never `general-purpose`; one that writes code — an ordinary Superpowers subagent, whose prompt must order the load of the skills this table names for what it touches |
| "this is over-built" / "simplify this" | grading the claim in changed code — `dotnet-code-review` (its simplicity area); executing the cleanup — `/simplify`; cutting speculative steps while the plan is written — `dotnet-feature-flow`; the every-session constraint a repository carries — its own `CLAUDE.md`, built by `claude-md-builder` |

## Not yet covered

No skill in this plugin owns the areas below. Where the second column carries a
name, a sibling's `Not for:` list points at it — but that name is a reservation,
**not a skill: there is nothing to load**. Following such a pointer is a dead
end, and this table is where the dead end resolves.

In these areas, work from the codebase in front of you. Do not derive a
convention from an adjacent skill: another skill's conventions are evidence
about that skill's area, not about this one.

| The area | Reserved name — nothing to load |
|---|---|
| Background jobs: scheduled work, queued work, hosted services | `background-worker` |
| C# idiom in general: language features, nullability, analyzer settings | — |
| Domain modelling: aggregates, value objects, domain events, invariants | — |
| HTTP calls to another service: retry, timeout, circuit breaker, client setup | — |
| Observability: logging, tracing, health checks | `observability` |
| Repository bootstrapping: starting a new solution from nothing — once the projects exist, placement belongs to `facade-module-architecture` | `project-scaffolding` |

## When nothing here fits

If no base-map row matches and no shared token above is the one causing trouble,
that is itself the answer: this plugin does not standardize the question.
Proceed without a skill and stay consistent with the surrounding codebase. Do
not force the question into the nearest row — a row chosen for proximity rather
than fit imports a convention that was never meant to apply.
