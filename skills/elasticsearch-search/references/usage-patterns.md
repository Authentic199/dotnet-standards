# Usage patterns

How to author `Elk*` documents and consume `IElasticSearchWrapper` from a module —
first how a document, its profile and its mapper are written, then how they are read
and kept in step with the database.

Modules inject `IElasticSearchWrapper`. Never `IElasticClient`, never a module-local
search helper.

| You are adding | Write | Index |
|---|---|---|
| a concept call sites will query directly | a root document: class + profile + mapper | its own |
| a shape that only ever appears inside another document | an embedded document: class + profile | none |
| search over a DB entity | an `Elk*` projection of it — never the entity | — |

---

## Creating a document

One file in the module's `ElkEntities/` folder carries the document and everything that
defines it. A root document, end to end:

```csharp
using Infrastructure.Facades.ElasticSearch;
using Infrastructure.Facades.ElasticSearch.Builders;
using Infrastructure.Modules.Orders.Entities;
using Nest;

namespace Infrastructure.Modules.Orders.ElkEntities;

[ElasticsearchType(IdProperty = nameof(Id))]
public class ElkOrder : ElkBaseEntity
{
    [Keyword]
    public string Code { get; set; } = default!;

    [Keyword]
    public Guid CustomerId { get; set; }

    public string? Note { get; set; }

    public OrderStatus Status { get; set; }

    public DateTimeOffset PlacedAt { get; set; }

    // Embedded: indexed as part of this document, never written on its own.
    public List<ElkOrderLine> Lines { get; set; } = new();
}

public class ElkOrderMapping : AutoMapper.Profile
{
    public ElkOrderMapping()
    {
        CreateMap<Order, ElkOrder>();
    }
}

public class ElkOrderMapper : IndexSettingsMapper<ElkOrder>
{
    public override ITypeMapping Configure(TypeMappingDescriptor<ElkOrder> descriptor)
        => descriptor.AutoMap();
}
```

And the embedded document it carries — same folder, own file, **no mapper**:

```csharp
using Infrastructure.Facades.ElasticSearch;
using Infrastructure.Modules.Catalog.ElkEntities;
using Infrastructure.Modules.Orders.Entities;
using Nest;

namespace Infrastructure.Modules.Orders.ElkEntities;

[ElasticsearchType(IdProperty = nameof(Id))]
public class ElkOrderLine : ElkBaseEntity
{
    [Keyword]
    public Guid CatalogItemId { get; set; }

    // Denormalized from another module — an Elk* document, never the DB entity.
    public ElkCatalogItem? CatalogItem { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}

public class ElkOrderLineMapping : AutoMapper.Profile
{
    public ElkOrderLineMapping()
    {
        CreateMap<OrderLine, ElkOrderLine>();
    }
}
```

Why this shape holds:

- **The profile is the only place the projection exists.** One
  `CreateMap<Order, ElkOrder>()`, found by the AutoMapper scan, used by every call site.
  Hand-mapping an entity into a document at a call site compiles and works — until a
  property is added and the two mappings disagree, one feature indexing it and the other
  silently not. Write `AutoMapper.Profile` out in full, as the canonical does: a bare
  `Profile` is ambiguous the moment another `Profile` type is in scope.
- **The absence of `ElkOrderLineMapper` is the entire declaration that this document is
  embedded.** It still carries `[ElasticsearchType(IdProperty = nameof(Id))]` — an
  embedded document is promoted to root the day someone adds a mapper, and requiring the
  attribute everywhere removes a failure that would otherwise appear at that moment as a
  throw on the first write.
- **`Configure` is `descriptor.AutoMap()`.** The attributes on the class *are* the
  mapping; that is why they sit on the properties and not in the mapper. Hand-writing
  `Configure` lets the class and the mapper disagree about the same field — do it only
  for what an attribute cannot express, such as a custom analyzer.
- **A mapper only counts if the startup scan finds it:** it must compile into the
  Infrastructure assembly and expose a public parameterless constructor, and its index
  name is derived from the configured prefix and the type name (see
  `implementation.md`). A mapper the scan misses produces no error and no index, and the
  first write creates one with a dynamic mapping instead of yours. *(Canonical mapper
  class names vary; `Elk<Entity>Mapper` is the form this skill uses.)*
- **A document with no entity behind it has no profile.** A document assembled in code
  from a payload rather than projected from a row skips the profile and keeps the
  mapper. That is the one case where the second piece is genuinely absent.

### Mapping fields

| Field | Attribute | Why |
|---|---|---|
| reference guid — `CustomerId`, `CatalogItemId` | `[Keyword]` | matched exactly, never read as prose |
| code, identifier, machine-readable string | `[Keyword]` | analysis splits it and it stops matching |
| free text a human types into a search box | none — analyzed `text` is the default | analysis is the point |
| numbers, dates, booleans | none | not analyzed either way; `[Keyword]` says nothing about them |

Rule of thumb: **if the value would ever appear in a `term` query, it is `[Keyword]`.**
`ElkBaseEntity` already applies this to `Id`.

**Enums index as their numeric value.** The serializer writes the underlying number, so
a `term` query and an update script both compare against the same number — the canonical
write-back script assigns `ctx._source.status={(int)OrderStatus.Placed}` for exactly that
reason. If a human-readable value has to be searchable, project it into a separate
`[Keyword]` string property rather than reconfiguring the serializer; the enum property
keeps working for everything else.

A denormalized copy is a copy. It goes stale the moment its source row changes, and
nothing tells you — keeping it in step is a write-side decision, and it is the subject of
the rest of this file.

---
## Reading and writing back

Nothing in Elasticsearch notices when the source row behind a denormalized copy changes —
no foreign key, no cascade, no error. Something in the application has to notice and
re-project, and this half of the file is that something, together with the reads it feeds.

Consumers take a repository per document type at the call site. A service that indexes
usually holds three things: the search wrapper, the database repository wrapper the
projection is built from, and `IMapper`.

```csharp
public class OrderIndexService : IOrderIndexService
{
    private readonly IElasticSearchWrapper elasticSearchWrapper;
    private readonly IRepositoryWrapper repositoryWrapper;
    private readonly IMapper mapper;

    // constructor assigns the three
}
```

---

## Reading

### By id, as an idempotency guard

The commonest read is not a search — it is "do I already have this?" before doing work.

```csharp
public async Task<ElkOrder> IndexAsync(Guid orderId, CancellationToken cancellationToken = default)
{
    ElkOrder? existing = await elasticSearchWrapper
        .Repository<ElkOrder>()
        .GetByIdAsync(orderId, cancellationToken)
        .ConfigureAwait(false);

    if (existing is not null)
    {
        return existing;
    }

    // The document is projected from the database row, never from the caller's arguments.
    ElkOrder document = await repositoryWrapper
        .Repository<Order>()
        .Find(x => x.Id == orderId, isAsNoTracking: true)
        .ProjectTo<ElkOrder>(mapper.ConfigurationProvider)
        .FirstOrDefaultAsync(cancellationToken)
        .ConfigureAwait(false)
        ?? throw new BadRequestException("Order not found.");

    return await elasticSearchWrapper
        .Repository<ElkOrder>()
        .AddAsync(document, cancellationToken)
        .ConfigureAwait(false);
}
```

Three things this shape gets right:

- **`GetByIdAsync` returning `null` is the normal miss**, not an exception, so the guard is
  a plain `is not null` and needs no `try/catch`.
- **`ProjectTo` reads the profile**, so the entity → document projection stays in the one
  colocated place instead of being retyped here. The read is `isAsNoTracking` because
  nothing is being mutated.
- **The document is built from the database row**, not from the parameters the caller
  passed. Every projection in this file obeys that rule, including the re-index handler
  below.

### By query, when a sort decides the answer

```csharp
ElkOrder? newest = await elasticSearchWrapper
    .Repository<ElkOrder>()
    .FirstOrDefaultAsync(
        x => x.Query(q => q.Term(t => t.CustomerId, customerId))
              .Sort(s => s.Descending(o => o.PlacedAt)),
        terminateAfter: null,
        cancellationToken)
    .ConfigureAwait(false);
```

`terminateAfter: null` is the load-bearing argument. Leave it at its default of `1` and
each shard stops at its first hit, so the sort is applied to a set that was already
truncated — you get *an* order, not the newest one. The bug is invisible on a single-shard
development index and intermittent in production. Leave the default in place only when the
query can match at most one document, which is the case worth a comment at the call site.

### Everything matching

```csharp
IEnumerable<ElkOrder> placed = await elasticSearchWrapper
    .Repository<ElkOrder>()
    .SearchAsync(
        x => x.Query(q => q.Term(t => t.Status, (int)OrderStatus.Placed))
              .Size(pageSize)
              .From(pageIndex * pageSize),
        cancellationToken: cancellationToken)
    .ConfigureAwait(false);
```

`SearchAsync` materializes the hits into a list before returning, so there is no lazy
cursor to leak. **The size is whatever the descriptor says** — omit `.Size(…)` and you get
the cluster's default of 10. The configured `DefaultSize` is validated at startup but no
call site reads it, so pass the page size in explicitly rather than treating it as a
fallback.

For an export or an index rebuild, `SearchAsync(selector, scrollTime: "2m")` loops the
scroll internally and hands back every document in one list — right for a background job,
wrong for a request path, and it holds the whole result set in memory. When paging must
stay consistent while documents are written underneath it, open a point in time with
`GetPointInTimeAsync` and close it with `ClosePointInTimeAsync`.

---

## Re-indexing after a database write

**The database is written first, and the index is repaired from it.** That is the whole
contract. Reverse it and an index write can land while the transaction behind it rolls
back, leaving a document that describes a row that never existed — with nothing to compare
it against, because the index is the only place it was ever written.

Re-indexing lives in an **in-process notification handler**. The service that changed the
row publishes a domain event; a handler subscribed to it repairs the projection. These are
published through MediatR's `INotification` — in-process messaging, same process and
usually the same request, not a message broker and not a CQRS boundary.

Why a handler rather than three more lines in the service: the service is about the
business change, the projection is a consequence of it, and several documents may embed
copies of the same row. One event, one handler each, each independently correct.

```csharp
public record OrderStatusChangedEvent(Guid OrderId, OrderStatus Status) : INotification;

public class ReindexOrderHandler : INotificationHandler<OrderStatusChangedEvent>
{
    private readonly IElasticSearchWrapper elasticSearchWrapper;
    private readonly IRepositoryWrapper repositoryWrapper;

    // constructor assigns both

    public async Task Handle(OrderStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        // 1. Re-read the source of truth. The payload says what happened, not what is true.
        Order? order = await repositoryWrapper
            .Repository<Order>()
            .Find(x => x.Id == notification.OrderId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            return;
        }

        // 2. Then repair the projection. No document is not an error.
        ElkOrder? document = await elasticSearchWrapper
            .Repository<ElkOrder>()
            .GetByIdAsync(notification.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (document is null)
        {
            return;
        }

        document.Status = order.Status;
        document.Note = order.Note;

        await elasticSearchWrapper
            .Repository<ElkOrder>()
            .UpdateAsync(document, cancellationToken)
            .ConfigureAwait(false);
    }
}
```

Four properties of that handler are the pattern, not this example's details:

- **Re-read the source of truth; never trust the event payload.** The payload carries an id
  and enough context to route. By the time the handler runs the row may have moved on, and
  writing the payload's values into the index makes the two disagree in a way only a
  rebuild will find. Note the handler assigns `order.Status`, not `notification.Status`.
- **A missing document is a `return`, not an error.** The row may legitimately have no
  projection yet — indexing is asynchronous with respect to creation, and a document may
  have been reaped by a cleanup. Throwing turns a benign race into a failed request or a
  retry loop, and repairs nothing.
- **The overwrite is idempotent.** The handler assigns current values and writes them;
  running it twice produces the same document. In-process events can be raised more than
  once for one logical change, and re-delivery must be boring.
- **The handler is not the primary writer, and database-first still holds inside it.** The
  service that made the business change already committed it. If the handler still owes a
  durable field — a timestamp it alone can set — that row update happens before the index
  is touched, so the projection is built from the committed row.

The index write is not part of the database transaction and cannot be rolled back with it.
That is the same law read from the other end: when the mutation runs inside an explicit
transaction, raise the event **after the commit**, so nothing re-projects from a row that
may still disappear.

---

## Server-side updates with `UpdateByQueryAsync`

When the change is a single field across documents you do not need to load, hand the
cluster a script instead of the documents.

```csharp
await elasticSearchWrapper
    .Repository<ElkOrder>()
    .UpdateByQueryAsync(
        x => x.Query(q => q.Term(t => t.Id, orderId))
              .Script(scr => scr.Source(
                  $"ctx._source.status={(int)OrderStatus.Cancelled};ctx._source.note=null")),
        cancellationToken);
```

Everything inside that string is outside the type system. Three consequences, all of which
produce a silently wrong document rather than an error:

- **Field names are the JSON names, camelCase** — `status`, `note`, exactly as the
  serializer writes them. They are *not* the C# property names, and renaming the C#
  property does not touch this string.
- **Enums are numeric.** The document stores the underlying value, so the script must write
  a number — hence the `(int)` cast, which also keeps the C# enum as the single source of
  the value instead of hard-coding a digit in a string.
- **A typo creates a field.** `ctx._source.statuss = 2` does not fail; it adds `statuss` to
  the document and, on a dynamic mapping, to the index. The document keeps its old
  `status`, every query still reads the old value, and nothing reports a problem.

Reserve this path for field flips and bulk cleanups. If more than one field changes, or if
the change reaches into embedded copies, re-project the whole root document from the
database instead — a typed overwrite you can read is worth more than a round trip you
saved.

`DeleteByQueryAsync` has the same shape and the same missing safety net: it is a query, and
a query that matches more than you meant deletes more than you meant. Run the same
predicate through `CountAsync` first whenever it is not trivially exact.

---

## Anti-pattern: the blocking pair

The repository surface in an existing project may carry two synchronous members —
`Search(…, out ISearchResponse<T>?, …)` and `BulkAll(…)`. Both block the calling thread:
`Search` calls the synchronous client (and, with a scroll time, loops synchronously), and
`BulkAll` subscribes to an observable and waits on a `CountdownEvent` until the entire bulk
finishes.

**BAD** — either one on a request path.

```csharp
IEnumerable<ElkOrder> orders = elasticSearchWrapper
    .Repository<ElkOrder>()
    .Search(x => x.Query(q => q.Term(t => t.CustomerId, customerId)), out ISearchResponse<ElkOrder>? response);

elasticSearchWrapper
    .Repository<ElkOrder>()
    .BulkAll(documents, x => x.Index(indexName));
```

**GOOD** — the async members, and an explicit batch loop instead of the observable.

```csharp
private const int BatchSize = 500;

IEnumerable<ElkOrder> orders = await elasticSearchWrapper
    .Repository<ElkOrder>()
    .SearchAsync(
        x => x.Query(q => q.Term(t => t.CustomerId, customerId)),
        cancellationToken: cancellationToken)
    .ConfigureAwait(false);

foreach (ElkOrder[] batch in documents.Chunk(BatchSize))
{
    await elasticSearchWrapper
        .Repository<ElkOrder>()
        .UpsertRangeAsync(batch, Refresh.False, cancellationToken)
        .ConfigureAwait(false);
}
```

Under any real concurrency each blocked call holds a thread-pool thread for the whole round
trip, and thread-pool starvation does not present as a search problem — it presents as the
whole application slowing down, with the search calls looking fine in isolation. `Search`
additionally takes no `CancellationToken` at all, so a client that walks away cannot stop
it. Keep `Refresh.False` on the batch loop, and pass a refresh deliberately on the final
batch only if something queries the index straight after.

**Red flag:** you are about to call a repository member whose name has no `Async` suffix.

These two members are **omitted from the scaffold** in `implementation.md`. In a project
that already has them, leave the members in place — removing public surface is a separate,
mechanical decision — and stop calling them.
