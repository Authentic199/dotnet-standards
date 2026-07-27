# Query conventions

Two shapes cover almost every read: a paged search driven entirely by the
query string, and a fetch of one record by id. Both start at `Find`, project
immediately, and never materialize an entity.

## The search shape

```csharp
public async Task<PaginationResponse<OrderResponse>> SearchAsync(QueryContainer request, CancellationToken cancellationToken = default)
    => await repositoryWrapper.Repository<Order>().Find(isAsNoTracking: true)
        .ProjectTo<OrderResponse>(mapper.ConfigurationProvider)
        .ApplyFilter(request.Filter)
        .ApplySearch(request.SearchKeyword, request.SearchFields, request.Filter?.Keys.ToArray())
        .ApplySort($"{nameof(BaseEntity.CreatedAt)} desc", request.SortQuery)
        .ToPagedListAsync(request.Current, request.PageSize, cancellationToken: cancellationToken);
```

The order is the design:

- `Find(isAsNoTracking: true)` with no predicate — the whole set is the
  starting point and the client narrows it.
- `ProjectTo` **first**, so everything after it composes over the projected
  response. Filter keys, search fields and sort fields are `OrderResponse`
  property names — the vocabulary the client already sees in the payload — and
  renaming an entity property cannot silently break a saved client query.
- `ApplyFilter` reads the operator-prefixed values described below.
- `ApplySearch(keyword, fields, excepts)` — the third argument passes the
  filter's own keys as exclusions, so a field the client filtered explicitly
  is not also swept by the free-text keyword. Only `string` properties
  participate; anything else named in `SearchFields` is skipped silently.
- `ApplySort(fallback, clientQuery)` — the fallback applies when `SortQuery`
  is absent, and `CreatedAt desc` is the canonical one. The helper appends
  `Id descending` as a final tiebreaker either way, which is what keeps paging
  stable across requests.
- `ToPagedListAsync` last, so paging applies to the filtered, sorted result.

## The get shape

```csharp
public async Task<OrderResponse> GetAsync(Guid orderId, CancellationToken cancellationToken = default)
    => await repositoryWrapper.Repository<Order>().Find(x => x.Id == orderId)
        .ProjectTo<OrderResponse>(mapper.ConfigurationProvider)
        .FirstOrDefaultAsync(cancellationToken)
    ?? throw new BadRequestException(Messages<Order>.NotFound());
```

The predicate goes in `Find`, the projection follows, and the miss throws
rather than returning null. No `Include`: `ProjectTo` reads the response
mapping and generates exactly the joins it needs. Reach for `GetByIdAsync`
instead when you want the tracked entity in order to change it.

## What the client sends

`QueryContainer` binds from the query string; every member is optional.

| Property | Query string | Notes |
|---|---|---|
| `Filter` | `filter.Status=$eq:2` | Custom model binder; values carry an operator |
| `SearchKeyword` | `searchKeyword=north` | Free text, matched case-insensitively |
| `SearchFields` | `searchFields=Code&searchFields=Customer.Name` | Nested paths allowed |
| `SortQuery` | `sortQuery=Code desc,Customer.Name` | Comma-separated, `desc` optional |
| `Current` | `current=2` | 1-based, defaults to 1 |
| `PageSize` | `pageSize=20` | Defaults to `int.MaxValue / 2` |

That `PageSize` default means a request naming no page size gets the whole
set. Decide per endpoint whether that is acceptable; for a collection that
grows without bound it is not. `QueryContainer` is `IValidatableObject`, so
non-positive paging values are rejected at binding rather than deep in the
query.

## Filter operators

The value is `<operator>:<value>`, optionally prefixed with `$not:` to negate
the whole condition (`filter.Status=$not:$eq:2`).

| Operator | Meaning | Example |
|---|---|---|
| `$eq` | equals | `filter.Status=$eq:2` |
| `$in` | any of — comma-separated | `filter.Status=$in:1,2` |
| `$null` | is null — **no trailing colon**, and the property must be nullable or a reference type | `filter.CompletedAt=$null` |
| `$gt` `$gte` `$lt` `$lte` | comparison | `filter.Total=$gte:100` |
| `$btw` | between, inclusive — exactly two comma-separated values | `filter.CreatedAt=$btw:2024-01-01,2024-02-01` |
| `$ilike` | contains | `filter.Code=$ilike:ord` |
| `$sw` | starts with | `filter.Code=$sw:ORD` |

Only the value is split on the first colon, so a value may itself contain
colons — a timestamp survives intact. Repeating a key applies each condition
as a separate `Where`, so repeats are ANDed; `$in` is how one field expresses
OR. A condition that fails to compile is caught and skipped, which means a
malformed filter widens the result set rather than failing the request —
worth checking when a search returns more than it should.

## What comes back

`PaginationResponse<T>` carries `PagedData` plus a `PageInfo` of `TotalCount`,
`PageSize`, `Current`, `TotalPages`, `HasNext`, `HasPrevious`. When the screen
also needs an aggregate — a sum, counts by status — `PaginationResponse<T,
TMoreInfo>` adds a `MoreInfo` payload alongside the page instead of a second
endpoint.

**Know the cost.** `ToPagedListAsync` runs the page query (`Skip`/`Take`) and
then a separate `Count()` over the same composed queryable, so the total
reflects the filters. On top of that, `ApplyFilter`, `ApplySearch` and
`ApplySort` each open by evaluating `entities.Any()` before composing
anything, and that check runs whether or not the client supplied a filter,
keyword or sort. A search endpoint written in the canonical shape above is
therefore five round trips, not two — three probes, the page, and the count.
Budget for that on hot list endpoints.
