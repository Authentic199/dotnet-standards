---
name: elasticsearch-search
description: >-
  This skill should be used when Elasticsearch or full-text search enters a .NET
  solution: scaffolding the search facade, injecting IElasticSearchWrapper,
  calling Repository<T>(), adding an Elk document under ElkEntities, writing an
  IndexSettingsMapper or index prefix, wiring ElasticsearchSettings and
  AddElasticsearch, building a search descriptor, FirstOrDefaultAsync,
  reindexing after an entity change, or intending to index a DB entity. Not for:
  cache keys, TTL, IRedisCacheService — distributed-caching; distributed locks,
  ConcurrencyHandlers — distributed-lock; background jobs, Hangfire —
  background-worker; DbContext, repositories, EF queries — ef-core-data-access;
  search request DTOs, pagination contracts — api-surface; Serilog Elasticsearch
  sink — observability; folder placement, composition root —
  facade-module-architecture.
---

## Overview

One capability owns Elasticsearch, and it lives in **two folders that stay
separate**:

- `Infrastructure/Facades/Persistence/ElasticSearch/` — the data surface every
  consumer calls: `IElasticSearchWrapper`, handing out one
  `IElasticSearchRepositoryBase<T>` per document type.
- `Infrastructure/Facades/ElasticSearch/` — the wiring: client registration
  (`Startup.AddElasticsearch`) and the index builders (`IndexSettingsMapper<T>`).

Nothing else in the solution touches `IElasticClient`.

**Elasticsearch is a projection; the database is the source of truth.** The index
holds `Elk*` documents mapped from entities and is rebuildable from the database
at any time. When the two disagree, the database is right and the index is the
thing that gets repaired.

**Never index a database entity.** Only `Elk*` documents, declared in a module's
`ElkEntities/` folder, are ever handed to the wrapper. A persisted entity carries
navigation graphs, lazy proxies and a schema owned by migrations; index it and the
search mapping silently follows every migration, and nothing warns you. Project
into an `Elk*` document first, always.

*(The canonical stack is .NET 7 with the NEST 7 client, and that is what this
skill teaches. NEST is retired upstream in favour of the newer Elasticsearch .NET
client — noted so nobody reads it here as a current recommendation; this skill
offers no migration advice.)*

Answer "should this be indexed, and how do I query it?" from this file alone. Open
a `references/` file only under the conditions stated below.

## Before scaffolding: find the search capability that may already be there

Search the loading project for **any** existing Elasticsearch capability before
creating one:

- a folder whose files own `IElasticClient`, under `Facades/`,
  `Facades/Persistence/` or a module;
- an `AddElasticsearch`-shaped line in the `AddInfrastructure` chain, or in a
  facade `Startup.cs` it calls.

**If you find one, use it in place** — add the document, the mapper or the query
you need to the capability that already exists. A second search capability beside
the first gives the project two index-prefix conventions, two connection policies
and two sets of type mappings against the same cluster; the first symptom is a
document written to an index nobody reads. This skill describes the capability it
scaffolds; it makes no claim about what an existing folder contains, so read that
folder before extending it.

Only when the search finds nothing, scaffold — and scaffold **first**, before the
consuming code. Do not register or inject `IElasticClient` inside a module, do not
write a module-local search wrapper, and do not "temporarily" query the cluster
over `HttpClient` while the capability is missing.

## Placement & anatomy

Two folders, split exactly as the canonical project splits them. The split is
historical and it is kept: teaching it as it stands means your files land where an
existing project's files already are. Each half also has a distinct dependent set,
which is why the seam is stable — the data surface is imported by consumers, the
wiring by the composition root and nothing else.

```
Facades/Persistence/ElasticSearch/     # the data surface — what consumers import
├── ElasticSearchWrapper.cs            # IElasticSearchWrapper + implementation, one file
└── ElasticSearchRepositoryBase.cs     # IElasticSearchRepositoryBase<T> + implementation, one file

Facades/ElasticSearch/                 # the wiring — imported by the composition root
├── Startup.cs                         # public static Startup → AddElasticsearch()
├── ElkBaseEntity.cs                   # the base every Elk* document inherits
└── Builders/
    ├── IIndexSettingsMapper.cs        # non-generic scan target: IndexPrefix + Map(connectionSettings)
    └── IndexSettingsMapper.cs         # abstract IndexSettingsMapper<T> : IIndexSettingsMapper
```

- **`ElkBaseEntity` is scaffolded here, in the facade that owns search.** *(Deviation,
  deliberate: the canonical declares it inside one module's `ElkEntities/` folder, so
  every other module's documents inherit across a module boundary and depend on
  whichever module needed a base first. A shared base belongs to the capability.)*
- **Namespaces match their folders** — `Infrastructure.Facades.Persistence.ElasticSearch`
  and `Infrastructure.Facades.ElasticSearch`. *(Deviation, deliberate: the canonical
  wrapper and repository base both declare `Infrastructure.Persistence.Repositories.ElasticSearch`,
  which matches neither their folder nor the rest of the solution. In an existing
  project, keep whatever namespace its files already declare — renaming one is a
  separate mechanical commit, not part of this work.)*

**`ElasticsearchSettings` stays nested inside the persistence facade's
`DatabaseSettings`.** The scaffold **adds** the nested class and the
`ElasticsearchSettings` property to the existing `DatabaseSettings.cs` and changes
nothing else in that file.

- **No options block of its own, and no new configuration topic.** It rides the
  binding the persistence facade already performs; the client factory reads
  `IOptions<DatabaseSettings>.Value.ElasticsearchSettings` at resolve time. Nothing
  here reads `IConfiguration` directly.
- **Validation is one line** — `Validate` returns `validationContext.Required()`, so a
  missing node list, prefix or default size fails at host start naming the property.
  That nested `Validate` runs because the persistence binding chains
  `ValidateDataAnnotationsRecursively()`; the non-recursive variant stops at the root
  and the whole search section would go unchecked.
- *(Deliberate divergence from this plugin's `distributed-caching` skill, which
  extracts `RedisSettings` out of `DatabaseSettings` on the rule that settings follow
  their service. Search keeps the nesting the canonical project uses: one less file,
  one less binding, no new topic, for settings with exactly one reader. The cost is
  real and stated once — the section name here no longer equals the settings type
  name.)*

**Two registrations, in two files, one capability.** The client registers where the
wiring lives; the wrapper registers where the surface lives, beside the repository
wrapper it mirrors.

| Registration | Where | Lifetime, and why |
|---|---|---|
| `IElasticClient` | `AddElasticsearch()` in this facade's `Startup` | Singleton from a factory — one client per process. The factory runs on first resolve, not at registration. |
| `IElasticSearchWrapper` | the persistence facade's `Startup`, beside its repository-wrapper registration | Scoped. It caches one repository per document type in a non-synchronized `Hashtable` whose check-then-add is not atomic, so the cache must stay inside one request. **Do not promote it to singleton.** |

**Resolving the client is not free, and it is not lazy in the way the registration
suggests.** The first resolve builds the connection settings, then scans the
Infrastructure assembly for `IIndexSettingsMapper` implementations and calls each one
— blocking cluster calls that create or amend indices. Composition waits on
Elasticsearch at that moment, and an unreachable cluster surfaces there rather than
at first query. That cost is the trade for never querying an index that does not
exist.

**Composition is one line, `.AddElasticsearch()`, appended to the `AddInfrastructure`
chain.** It takes no argument, and its position is free — the line registers a
factory, and the options it reads are resolved later.

**Read `references/implementation.md` when** you are scaffolding the capability, or
writing or reviewing the wrapper, the repository base, `ElasticsearchSettings` or
this capability's `Startup.cs` — it carries the full file bodies, the connection
policy and the mapper scan.

## Prerequisites — stop if any is missing

This capability is not self-contained. It needs three pieces that already have an
owner elsewhere in the solution:

| Prerequisite | Where it lives | Used for |
|---|---|---|
| the persistence facade that owns `DatabaseSettings` | `Facades/Persistence/` | `ElasticsearchSettings` nests in it and rides its binding; the entities it persists are the source of truth every document is projected from |
| the `Required()` validation helper | `Facades/Common/Extensions/` | `ElasticsearchSettings.Validate` |
| AutoMapper profile scanning | the composition root's `AddAutoMapper(…)`, given a marker type from the assembly holding the profiles | the entity → `Elk*` document profiles that keep each projection in one place |

**If any is absent from the loading project, stop. Report what is missing, propose
options — introduce the shared piece first, or narrow the task — and wait for a
decision.** Do not stand up a second settings root, do not swap `Required()` for
hand-written attributes, and do not hand-map entities to documents at the call site.
Each of those makes search configure, validate or project differently from the rest
of the solution, and the divergence surfaces months later as a startup failure or a
document whose shape nobody can account for.

**Package quick-check, not a stop:** `NEST` (the client), `Humanizer` (index naming)
and `NewId` (document ids). Verify they resolve before writing files; a missing
package reference is not a decision to escalate. **Do not hand-roll `Underscore()` or
`Camelize()`** — the index *name* comes out of `Underscore()`, and a near-miss
implementation disagrees on acronyms and casing, after which writes and reads land in
two different indices that both exist and both look fine.

## ElkEntities convention

**Project into an `Elk*` document via its colocated profile; never hand the wrapper an
entity.** This is where that gets tempting: the entity is already loaded, its shape
already looks right, and `Repository<Order>()` compiles. The index mapping then follows
migrations instead of your design, and the first symptom is a query returning a shape
nobody chose.

Documents live one per file in `Modules/<Module>/ElkEntities/`, named `Elk<Entity>`. A
file carries up to three colocated pieces:

| Piece | Declared as | Needed for |
|---|---|---|
| the document | `ElkOrder : ElkBaseEntity`, with `[ElasticsearchType(IdProperty = nameof(Id))]` | every document |
| the projection | `ElkOrderMapping : AutoMapper.Profile` — `CreateMap<Order, ElkOrder>()` | every document projected from an entity |
| the index mapper | `ElkOrderMapper : IndexSettingsMapper<ElkOrder>` | **root documents only** |

**Root or embedded — decide first.** A *root* document has the colocated mapper,
therefore owns an index, and is written through `Repository<ElkOrder>()`. An *embedded*
document has no mapper and no index; it exists only as a property of a root document,
denormalized for search, and is never passed to `Repository<T>()`. The mapper's presence
is the entire declaration — adding one promotes a document to root, and nothing else in
the codebase records the difference. Writing an embedded document directly does not
fail: it lands in an index nobody provisioned, with a dynamic mapping, and the call
returns success.

- **`[ElasticsearchType(IdProperty = nameof(Id))]` goes on every document.** The
  repository base reads it reflectively to find — and, when unset, fill — the id, and
  throws on the first direct write without it. *(The canonical is inconsistent here:
  some documents omit it and survive only because they are never written directly.
  Declare it everywhere, so the day an embedded document gains a mapper nothing
  breaks.)*
- **`[Keyword]` on every exact-match field** — reference guids, codes, anything a `term`
  query filters on. Analyzed text is the default and is right only for free-text search
  fields.
- **Relations are other `Elk*` documents, never entities.** A relation is denormalized
  at projection time; importing another module's `Elk*` document for it is legal and
  expected.
- **The profile is the only place the entity → document projection is defined.** A call
  site that hand-builds an `ElkOrder` from an `Order` bypasses it, and the same entity
  starts mapping two different ways in two features.
- *(Drift, noted once: a couple of canonical documents sit in their module's `Entities/`
  folder beside the DB entities. Put new ones in `ElkEntities/` — the folder is what
  keeps "what the database owns" and "what the index owns" from sitting one careless
  `Repository<T>()` apart.)*

**Read `references/usage-patterns.md` when** you are adding or reviewing an `Elk*`
document, its mapper or its mapping profile — it carries one document, its mapper and
its profile end to end.

## Query & consumption

Modules inject `IElasticSearchWrapper` and take a repository at the call site —
`elasticSearchWrapper.Repository<ElkOrder>()`. Never inject `IElasticClient`, and never
take `IElasticSearchRepositoryBase<T>` as a constructor parameter: it is not registered
in the container, and the wrapper is its only factory.

Pick the member from the question you are answering, not from habit:

| Question | Member | Watch for |
|---|---|---|
| "Give me the document with this id." | `GetByIdAsync` | a miss is `null`, not an exception |
| "Give me the documents with these ids." | `GetByIdManyAsync` | **string ids only** — it casts the sequence unconditionally, so a `Guid` list throws at runtime; it is also the one member that skips `ThrowIfFailure`, so a rejected request can come back empty instead of throwing |
| "One document matches — give me it." | `FirstOrDefaultAsync` | the `terminateAfter` trap below |
| "Give me every document matching this query." | `SearchAsync` | a materialized list; the size is whatever your descriptor says |
| "How many match?" | `CountAsync` | a `long`, no documents transferred |
| "Does one exist?" | `ExistsAsync` (by id or by query) | cheaper than fetching and null-checking |
| "Flip a field on every matching document, in place." | `UpdateByQueryAsync` | a script, not a document — camelCase field names, numeric enums |
| "Remove every document matching this query." | `DeleteByQueryAsync` | same script rules, and it deletes exactly what the query matches |

**The `terminateAfter` trap.** `FirstOrDefaultAsync` defaults to `terminateAfter: 1`, and
that does not mean "the first in my sort order". It is a per-shard stop condition: each
shard abandons the search as soon as it has one hit, so the sort is applied to a set that
was already truncated. You get *an* order, it looks plausible, and it is not reliably the
newest — nothing fails, and only on a multi-shard index. **Pass `terminateAfter: null`
whenever a sort decides which document is the right one**; keep the default only when at
most one document can match.

**Refresh is a default, and sometimes the wrong one.** Single-document writes and
`AddRangeAsync` complete with `Refresh.WaitFor`, so what they wrote is searchable by the
time the call returns. `UpdateRangeAsync` and `UpsertRangeAsync` default to
`Refresh.False` — that is what makes bulk indexing affordable, and it means a query
issued on the next line may not see the write. **When the next line queries what the
previous line wrote, pass a `Refresh` value deliberately** rather than inheriting the
default.

**Failures are exceptions; a miss is empty.** The client is built with `ThrowExceptions`
and the repository routes responses through `ThrowIfFailure`, so a rejected query, an
unreachable cluster or a malformed script throws at your call site, where the solution's
exception handling already knows what to do with it. **Do not wrap a read in `try/catch`
to turn that into an empty result** — a swallowed search failure presents as "no matches",
the one wrong answer nobody investigates. A document that does not exist is already `null`
from `GetByIdAsync` and an empty sequence from `SearchAsync`; neither needs defending.

**Write-back order is law: the database first, then the index.** The index is rebuildable
from the database and the database is not rebuildable from the index, so a failed index
write after a committed row is lag you can repair, and the reverse is a document
describing a row that never existed. Re-indexing belongs in an in-process notification
handler reacting to a domain event, not inline in the code that just wrote the row: the
handler re-reads the source of truth, tolerates a missing document, and overwrites
idempotently.

**Large result sets are a background-job concern.** `SearchAsync(scrollTime: "…")` drains
a scroll into one list — correct for an export or an index rebuild, wrong for a request
path. `GetPointInTimeAsync` / `ClosePointInTimeAsync` exist for pagination that must stay
consistent across pages. Page size always comes from the descriptor you write;
`DefaultSize` is configured and validated but no call site reads it, so it is not a
fallback you can code against.

**Read `references/usage-patterns.md` when** you are writing or reviewing a call
site, choosing between repository members, or sequencing an index update after a
database write — it carries the read and the re-index patterns end to end.

## Not this skill

Cache keys, TTL and `IRedisCacheService` → `distributed-caching`. Mutual exclusion
and `ConcurrencyHandlers` → `distributed-lock`. Recurring and queued jobs, and
Hangfire → `background-worker`. `DbContext`, entities, migrations and the EF
queries the projection is built from → `ef-core-data-access`. Search request DTOs
and pagination contracts on the HTTP surface → `api-surface`. The Serilog
Elasticsearch sink and anything that writes logs to a cluster → `observability`.
Where the capability folders sit, facade anatomy and the composition root →
`facade-module-architecture`.
