---
name: choosing-a-dotnet-skill
description: >-
  This skill should be used when working in a .NET, C# or ASP.NET Core codebase
  and it is unclear which dotnet-standards skill owns the question — two skills
  seem plausible, no skill self-triggered on the convention, a "Not for" pointer
  led to a skill that does not load, or the task spans several areas. Also when
  brainstorming, writing a spec or plan, or composing subagent prompts for
  generic .NET work, to name the skill each step requires. Not for:
  process-layer workflow — Superpowers; a question already matched to one
  skill — load that skill directly.
---

## How to use these tables

Find the row that matches the situation, load that one skill, and work from it.
If exactly one base-map row fits, load it and stop. If none fits, see *When
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

## Base map — one area, one skill

Ordered by build sequence: placement → behaviour → data → HTTP → failure → text
→ capabilities.

| The question in hand | Load |
|---|---|
| Where a file, project, facade or module belongs; project references; the composition root | `facade-module-architecture` |
| A service and its validation inside a module: rules, `IsExist`/`ThrowIf` guards, MediatR envelopes | `module-feature` |
| Repositories and queries, entities and their configurations, migrations, transactions, seeding | `ef-core-data-access` |
| A route, controller action, request or response DTO chain, pagination or search contract, or OpenAPI setup | `api-surface` |
| Which exception to throw, status codes, how the middleware turns a throw into a response | `error-handling` |
| The text a validator, success path or exception shows the user; message keys | `message-keys` |
| Caching data in Redis: the cache facade, keys, TTL, invalidation | `distributed-caching` |
| Keeping two requests from processing one resource at once: locks, `LockedException` | `distributed-lock` |
| Full-text search: the search facade, documents, indexing, reindexing | `elasticsearch-search` |

## When two skills both look right

These tokens mean different things in different areas, so description matching
alone picks wrong. Match the question, not the word.

| The shared token | The split |
|---|---|
| 401 / 403 | thrown as `UnAuthorizedException` / `ForbiddenException` — `error-handling`; putting `[HasPermission]` on an action — `api-surface`; schemes, policies, what that attribute enforces — *not yet covered* |
| a controller | which folder its file goes in — `facade-module-architecture`; the route, action body and attributes — `api-surface`; `try`/`catch` and building an error inside one — `error-handling` |
| an exception | which to throw and how it becomes a response — `error-handling`; the text it carries — `message-keys`; one raised because a resource was already being processed — `distributed-lock`; where the class itself lives — `facade-module-architecture` |
| mapping / `ProjectTo` | projecting inside a query — `ef-core-data-access`; where the profile file sits beside its DTO — `api-surface`; how to write the mapping itself — *not yet covered* |
| "message" | text a user will read — `message-keys`; an in-process command, query or event envelope — `module-feature` |
| pagination | the request and response contract — `api-surface`; executing the paged read — `ef-core-data-access` |
| a query | against the database — `ef-core-data-access`; full-text or index search — `elasticsearch-search`; the in-process query envelope — `module-feature` |
| Redis | storing or invalidating a cached value — `distributed-caching`; making two callers take turns — `distributed-lock` |
| `Repository<T>()` / "repository" | through the data-access wrapper — `ef-core-data-access`; through the search wrapper — `elasticsearch-search`; a brand-new source repository to bootstrap — *not yet covered* |
| a Settings class | where the file lives — `facade-module-architecture`; `DatabaseSettings` — `ef-core-data-access`; `RedisSettings` — `distributed-caching`; `ElasticsearchSettings` — `elasticsearch-search`; `ConcurrencySettings` — `distributed-lock` |
| a validator | where the file sits beside its DTO — `api-surface`; the rule and its guards — `module-feature`; the text a failing rule emits — `message-keys` |

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
| Mapping mechanics: writing a profile and its conventions | `automapper-mapping` |
| Messaging pipeline: dispatch, behaviours, handler registration — the thin envelope itself belongs to `module-feature` | `mediatr-messaging` |
| Observability: logging, tracing, health checks | `observability` |
| Permission and identity: JWT schemes, policies, what `[HasPermission]` enforces, secret handling | `auth-and-security` |
| Repository bootstrapping: starting a new solution from nothing — once the projects exist, placement belongs to `facade-module-architecture` | `project-scaffolding` |
| Testing: unit and integration tests, fixtures, test doubles | — |

## When the work is being planned, not yet written

A generic prompt — *implement feature X* — carries none of the nouns these
skills trigger on, so nothing fires while the spec, the plan, or the subagent
prompt is being written. That is precisely when the conventions get decided,
with no skill looking.

So work step by step. For each spec step, plan step, or subagent prompt — e.g.
while running Superpowers brainstorming, plan writing, or subagent-driven
development — identify the area the step touches and look it up above. **Where
a shipped skill owns that area, the step must name it.** A step reading *add the
endpoint* leaves whoever executes it matching on descriptions alone, which is
the failure this router exists to prevent; a step reading *add the endpoint
(`api-surface`)* does not. Steps in uncovered areas have nothing to name — say
so in the step if it helps the plan.

## When nothing here fits

If no base-map row matches and no shared token above is the one causing trouble,
that is itself the answer: this plugin does not standardize the question.
Proceed without a skill and stay consistent with the surrounding codebase. Do
not force the question into the nearest row — a row chosen for proximity rather
than fit imports a convention that was never meant to apply.
