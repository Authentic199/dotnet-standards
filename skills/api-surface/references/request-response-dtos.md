## Request & response DTOs

Requests and responses are inheritance chains with their validator and mapping
profile in the same file. This file works both chains end to end.

### The request chain

```csharp
// Modules/Orders/Requests/OrderRequest.cs — the base: shape + validator, no Profile
[MessageDisplay(nameof(Order))]
public abstract class OrderRequest
{
    public Guid? ProductId { get; set; }
}

public class OrderRequestValidator : AbstractValidator<OrderRequest>
{
    public OrderRequestValidator(IRepositoryWrapper repositoryWrapper)
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage(Messages<Order>.Required(x => x.ProductId))
            .Must(x => repositoryWrapper.IsExistById<Product>(x))
            .WithMessage(Messages<Product>.NotFound());
    }
}
```

- The base is `abstract` — it is never bound to an endpoint, only derived from.
- **`[MessageDisplay(nameof(Order))]` sits here and only here.** It sets the
  `Messages<T>` key prefix for the whole chain; repeating it on a derived request
  is noise, and putting it *only* on a derived request leaves its siblings
  producing keys named after the DTO instead of the entity.
- No `Profile` — this base maps plainly, so it declares none. See *Base profiles*.

```csharp
// Modules/Orders/Requests/CreateOrderRequest.cs
public class CreateOrderRequest : OrderRequest
{
    public string? Code { get; set; }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator(IRepositoryWrapper repositoryWrapper)
    {
        Include(new OrderRequestValidator(repositoryWrapper));

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(Messages<Order>.Required(x => x.Code))
            .Must(x => !repositoryWrapper.IsExistedCode(x!))
            .WithMessage(Messages<Order>.AlreadyExist(x => x.Code));
    }
}

public class CreateOrderRequestMapping : Profile
{
    public CreateOrderRequestMapping()
    {
        CreateMap<CreateOrderRequest, Order>();
    }
}
```

**Contract, validator, profile — that order, one file.** The `Include(...)` is
the first statement in the derived validator: base rules first, then the
properties this request adds. Which rules to write, and how a rule reaches the
database, belong to `module-feature`.

> Samples here use the `Messages<T>.X(selector)` form, which is what the shared
> facade validators themselves use. A second form built from message-type
> constants also exists in the codebase. **Which form to use, and how a key is
> composed, belong to `message-keys`** — this file fixes only where the call
> sits, never the key grammar.

```csharp
// Modules/Orders/Requests/UpdateOrderRequest.cs
public class UpdateOrderRequest : OrderRequest
{
}

public class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderRequestValidator(IRepositoryWrapper repositoryWrapper)
    {
        Include(new OrderRequestValidator(repositoryWrapper));
    }
}

public class UpdateOrderRequestMapping : Profile
{
    public UpdateOrderRequestMapping()
    {
        CreateMap<UpdateOrderRequest, Order>();
    }
}
```

**An empty derived class is the correct answer**, not a placeholder to be
inlined. It gives the update endpoint its own named contract, its own document
schema and its own validator seam — all three of which will diverge from create
eventually, and none of which can be added later without a breaking rename.

### Base profiles — the conditional rule

**Plain base → no profile.** `OrderRequest` above maps property-for-property, so
it declares nothing and each derived request maps itself. An empty base profile
adds a type, a registration and no behaviour.

**Customized base → one profile, ending `.IncludeAllDerived()`:**

```csharp
// Modules/Terminals/Requests/TerminalRequest.cs
[MessageDisplay(nameof(Terminal))]
public abstract class TerminalRequest
{
    public string? Name { get; set; }

    public string? Password { get; set; }
}

public class TerminalRequestMapping : Profile
{
    public TerminalRequestMapping()
    {
        CreateMap<TerminalRequest, Terminal>()
            .ForMember(des => des.Password, opt =>
            {
                opt.Condition(src => src.Password != null);
                opt.MapFrom(src => PasswordHasher.Hash(src.Password));
            })
            .IncludeAllDerived();
    }
}

// and the derived maps stay plain:
CreateMap<CreateTerminalRequest, Terminal>();
CreateMap<UpdateTerminalRequest, Terminal>();
```

`.IncludeAllDerived()` pushes that `ForMember` down to every derived type.
Without it, each derived map repeats the customization, and the day one of them
is edited the two shapes diverge silently. **The test is one question: does this
base map need a `ForMember`, an `Ignore`, or a converter? Yes → base profile
ending `.IncludeAllDerived()`. No → no base profile at all.**

### Search requests

A list endpoint's request derives from `QueryContainer` — usually empty, named
for the endpoint so the document shows a meaningful schema name:

```csharp
public class SearchOrderRequest : QueryContainer
{
}
```

`QueryContainer` supplies the whole list contract, identical across every search
endpoint in the API:

| Member | Purpose |
|---|---|
| `Filter` | `filter.<Prop>=<op>:<value>`, operators `$eq $null $in $gt $lt $lte $gte $btw $ilike $sw`; bound by a custom model binder that reads the raw query string |
| `PageSize` | **Defaults to `int.MaxValue / 2`** — a caller that sends nothing gets everything |
| `Current` | 1-based page number, default 1 |
| `SearchFields` | properties the keyword searches, dotted paths allowed: `["Code","Product.Name"]` |
| `SearchKeyword` | the keyword itself |
| `SortQuery` | `Code desc,Product.Name` |

It is `IValidatableObject` and validates its own paging — a non-positive or
absurd `PageSize`/`Current` is rejected before the query runs. The unbounded
default is deliberate but sharp: an endpoint that must never return everything
needs its own guard, because the contract will not supply one.

**Add a property to the subclass only for a filter the generic contract cannot
express.** A property that duplicates `Filter` or `SortQuery` gives callers two
ways to ask the same question and the service two things to reconcile.

### Bulk requests

```csharp
public class DeleteRangeOrderRequest : RangeGuidRequest
{
}

public class DeleteRangeOrderValidator : AbstractValidator<DeleteRangeOrderRequest>
{
    public DeleteRangeOrderValidator(IRepositoryWrapper repositoryWrapper, ICurrentUser currentUser)
    {
        Guid accountId = currentUser.GetUserId();

        Include(new RangeGuidValidator<Order>(repositoryWrapper, x => x.AccountId == accountId));
    }
}
```

`RangeGuidRequest` is `RangeItemRequest<Guid>` — one `Ids` collection.
`RangeGuidValidator<TEntity>` derives from `RangeItemValidator<TEntity, Guid>`
and checks, in a single `Include`, that `Ids` is non-empty, contains no
duplicates, and that every id exists.

**The optional filter expression is the ownership gate**, and it is the security
boundary of every bulk endpoint. It is passed straight into the existence check,
so omitting it means any authenticated caller holding the bulk permission can
pass someone else's ids: existence passes, and the operation proceeds. Pass the
filter whenever rows belong to someone; omit it only for admin-scoped operations.

### The response ladder

Rooted at `BaseEntity`, one rung deriving from the next:

```csharp
// Modules/Orders/Responses/OrderBaseResponse.cs
public class OrderBaseResponse : BaseEntity
{
    public string Code { get; set; } = default!;

    public OrderStatus Status { get; set; }
}

public class OrderBaseResponseMapping : Profile
{
    public OrderBaseResponseMapping()
    {
        CreateMap<Order, OrderBaseResponse>()
            .ForMember(des => des.Status, opt => opt.MapFrom(OrderExpression.GetStatus))
            .IncludeAllDerived();
    }
}
```

```csharp
// OrderDefaultResponse.cs — the list projection
public class OrderDefaultResponse : OrderBaseResponse
{
    public ProductDefaultResponse? Product { get; set; }
}

public class OrderDefaultResponseMapping : Profile
{
    public OrderDefaultResponseMapping()
    {
        CreateMap<Order, OrderDefaultResponse>();
    }
}
```

```csharp
// OrderResponse.cs — the detail projection
public class OrderResponse : OrderDefaultResponse
{
    public ICollection<ShipmentDefaultResponse>? Shipments { get; set; }
}
```

`Id` and `CreatedAt` are never redeclared — they arrive from `BaseEntity`.
**Unlike the request side, the base rung does carry the `Profile`**, and its
`.IncludeAllDerived()` means the computed `Status` is correct on every rung; add
a rung tomorrow and it is correct there too.

**Search documents root at `ElkBaseEntity`** — a separate root carrying identity
and timestamp for indexed documents, mapped from the module's `Elk`-prefixed
document type, never from the database entity:

```csharp
public class ElkOrderResponse : ElkBaseEntity
{
    public string Code { get; set; } = default!;
}
```

### Anti-example (authorized, real) — sibling duplication

```csharp
public class TerminalBaseResponse : BaseEntity      // ✗ orphan rung: nothing derives from it
{
    public string? Code { get; set; }
    public string? Name { get; set; }
}

public class TerminalDefaultResponse : BaseEntity   // ✗ sibling, not a rung
{
    public string? Code { get; set; }               // ✗ re-declared
    public string? Name { get; set; }               // ✗ re-declared
    public TerminalStatus OperationStatus { get; set; }
}

public class TerminalResponse : TerminalDefaultResponse   // the ladder really starts here
{
    public ICollection<ShipmentDefaultResponse>? Shipments { get; set; }
}
```

Two roots where there should be one. The consequence is already visible in the
real file: the default rung carries the customized mapping (`.IncludeAllDerived()`
included) and the base rung carries a plain one, so the two share property
*names* and not property *values*. A field added to the base rung reaches nobody;
a mapping fixed on the default rung never reaches the base rung's consumers.

The fix is mechanical — `TerminalDefaultResponse : TerminalBaseResponse`, delete
the duplicated properties. **The moment two responses share a property, that
property belongs on a rung both derive from.**

### Pagination

```csharp
public class PaginationResponse<T>
{
    public IEnumerable<T> PagedData { get; set; }

    public PageInfo PageInfo { get; set; }          // TotalCount, PageSize, Current + computed page flags
}

public sealed class PaginationResponse<T, TMoreInfo> : PaginationResponse<T>
{
    public TMoreInfo MoreInfo { get; set; }
}
```

The service builds it; the controller only wraps it:

```csharp
// in the module service
public async Task<PaginationResponse<OrderDefaultResponse>> SearchAsync(SearchOrderRequest request, CancellationToken cancellationToken)
    => await query.ProjectTo<OrderDefaultResponse>(mapper.ConfigurationProvider)
        .ToPagedListAsync(request.Current, request.PageSize, cancellationToken);
```

`ToPagedListAsync` exists on `IQueryable` (paged in the database) and
`ToPagedList` on `IEnumerable` (paged in memory), each with a `moreInfo`
overload. Prefer the `IQueryable` form; the in-memory form enumerates and counts
the whole sequence.

**`MoreInfo` carries a companion object the same search already computed** — a
summary row, totals, or a recommendation derived from the same query:

```csharp
    => await query.ProjectTo<OrderDefaultResponse>(mapper.ConfigurationProvider)
        .ToPagedListAsync(request.Current, request.PageSize, summary, cancellationToken);
```

If the extra data is not a by-product of this search, it is a second endpoint —
`MoreInfo` is not a general-purpose slot, and a controller never assembles it.
Whatever the shape, the endpoint still returns
`ActionResult<SuccessResultWrapper<PaginationResponse<…>>>`: **never a bare
array, never an `Items`/`Total` object, never a bespoke paging DTO.**
