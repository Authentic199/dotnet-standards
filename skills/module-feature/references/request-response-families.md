# Request and response families

- [The file is the unit](#the-file-is-the-unit)
- [A base request and its derived requests](#a-base-request-and-its-derived-requests)
- [The two shared bases every module reuses](#the-two-shared-bases-every-module-reuses)
- [Response tiers](#response-tiers)
- [Which base type a response gets](#which-base-type-a-response-gets)
- [Computed members: `Expressions/` or shape glue](#computed-members-expressions-or-shape-glue)
- [Anti-example: a computed value re-derived in the profile](#anti-example-a-computed-value-re-derived-in-the-profile)
- [Anti-example: a response named tier-first](#anti-example-a-response-named-tier-first)
- [When a family grows: theme subfolders](#when-a-family-grows-theme-subfolders)
- [Review checklist](#review-checklist)

## The file is the unit

| File | Declares, in this order |
|---|---|
| `Requests/<Theme>/<X>Request.cs` | `<X>Request`, `<X>RequestValidator : AbstractValidator<<X>Request>`, `<X>RequestMapping : Profile` if the request maps |
| `Responses/<Theme>/<X>Response.cs` | `<X>Response`, then `<X>ResponseMapping : Profile` below it |

Nothing else goes in these files, and these declarations go nowhere else. **There is
no `Mappings/` folder** — the projection is part of the contract, and a contract whose
projection lives three folders away drifts from it without a single failing build.

**A `Profile` is named for the class it maps, in full: `<ClassName>Mapping`.**
`OrderResponseMapping`, `CreateOrderRequestMapping`. Shortening it to `OrderMapping`
because the file is "obviously about orders" costs you the ability to find the profile
for one specific type, and a module with four response tiers then has four profiles
whose names do not say which tier they configure.

A validator is discovered by the validation facade's assembly scan, which is what makes
constructor injection work — so you never register one and almost never construct one.
The two places a validator is constructed by hand are `Include(new <Base>Validator(...))`,
to reuse another validator's rules on the same request, and
`.SetValidator(new <Member>Validator())`, to validate a nested member with its own
validator. Anywhere else, the scan already did it.

## A base request and its derived requests

One shape, one file per action that uses it.

```csharp
// Requests/Orders/OrderRequest.cs — the shared shape
public abstract class OrderRequest
{
    public string? Name { get; set; }

    public Guid? CategoryId { get; set; }

    public OrderStatus Status { get; set; }
}

public class OrderRequestValidator : AbstractValidator<OrderRequest>
{
    public OrderRequestValidator(IRepositoryWrapper repositoryWrapper, IActionAccessorService actionAccessorService)
    {
        string? action = actionAccessorService.GetAction();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(Messages<Order>.Required(x => x.Name))
            .MaximumLength(256).WithMessage(Messages<Order>.OverLength(x => x.Name));

        RuleFor(x => x.CategoryId)
            .NotEmpty().When(_ => action == "Create", ApplyConditionTo.CurrentValidator)
            .WithMessage(Messages<Order>.Required(x => x.CategoryId))
            .Must(id => repositoryWrapper.IsExistCategory(id!.Value))
            .WithMessage(Messages<Order>.NotFound(x => x.CategoryId));

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage(Messages<Order>.Required(x => x.Status))
            .IsInEnum().WithMessage(Messages<Order>.Invalid(x => x.Status));
    }
}

// No Profile here — this base maps plainly, so it declares none; each derived
// request maps itself. A base declares a Profile only when its map needs a
// ForMember, an Ignore or a converter, and then it ends `.IncludeAllDerived()`
// so the customization flows down (the chain law and the customized-base
// worked example live with api-surface's request-response-dtos.md).
```

```csharp
// Requests/Orders/CreateOrderRequest.cs — the create action adds one member and one rule
public class CreateOrderRequest : OrderRequest
{
    public string? Code { get; set; }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator(IRepositoryWrapper repositoryWrapper, IActionAccessorService actionAccessorService)
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(Messages<Order>.Required(x => x.Code))
            .Must(code => !repositoryWrapper.IsExistOrderCode(code!))
            .WithMessage(Messages<Order>.AlreadyExist(x => x.Code));

        Include(new OrderRequestValidator(repositoryWrapper, actionAccessorService));
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

`UpdateOrderRequest` is the same three parts with an empty class body: it adds no
member, its validator is one `Include`, and its map is one bare `CreateMap`.

What that family is saying:

- **A derived request with no members of its own is normal and correct.** It exists so
  the action has its own type to bind, its own validator to run and its own map to
  project through. Do not collapse two actions onto one request to avoid an empty class.
- **`Include(new <Base>Validator(...))` is how a derived validator reuses base rules.**
  Never restate a base rule in a derived validator: two copies of `NotEmpty` on the same
  property diverge the first time a length limit changes.
- **A derived validator takes the base's dependencies even when it does not use them
  itself** — `IActionAccessorService` above is accepted only to be forwarded into
  `Include`. That signature is telling you where the rules really live.
- **A rule that applies to one action only is written once, in the base, conditioned on
  the action** — `.When(_ => action == "Create", ApplyConditionTo.CurrentValidator)`.
  `ApplyConditionTo.CurrentValidator` scopes the condition to the one validator call it
  follows; the default scopes it to every validator in that `RuleFor` chain, which
  silently disables the rest of the chain along with it.
- **`Messages<T>` names the entity, not the request.** Its selector is an expression over
  `T`, so `Messages<Order>.Required(x => x.Name)` requires `Name` on `Order`. A member
  that exists only on the request takes the `string` overload — the shared range
  validator does exactly this for the id collection it validates.
- **Every derived request declares its own `Profile`; the base declares one only
  when customized.** An empty base profile adds a type, a registration and no
  behaviour — a plain-mapping base declares none. A base whose map needs a
  `ForMember`, an `Ignore` or a converter declares one profile ending
  `.IncludeAllDerived()`, and *that* is what pushes the customization down onto
  every derived map — reading a derived map in isolation then tells you less than
  the base map does. The chain law and the customized-base worked example live
  with `api-surface`.

**Anti-example — one family, three mapping fates.** A real module's plain base
declares a profile anyway; its update request declares its own bare map, and its
create request declares none at all:

```csharp
public class OrderRequestMapping : Profile
{
    public OrderRequestMapping()
    {
        CreateMap<OrderRequest, Order>();          // plain — this profile adds nothing
    }
}

public class UpdateOrderRequestMapping : Profile
{
    public UpdateOrderRequestMapping()
    {
        CreateMap<UpdateOrderRequest, Order>();    // inherits nothing from the base map
    }
}

// CreateOrderRequest declares no Profile at all — it maps only through the
// mapper's runtime fallback to the base map.
```

Three types, three different answers to "who maps this request": a base profile
that adds no behaviour, a derived map that inherits nothing, and a derived type
riding a runtime fallback. All of it works today — and the day the base map gains
its first `ForMember`, create picks it up and update silently does not: two
sibling actions projecting the same entity through diverging configurations, with
no build error and no test that notices. The fix is the conditional law: a plain
base declares **no** profile and every derived request maps itself; a base that
needs customization declares **one** profile ending `.IncludeAllDerived()`.
Either way, every request type has exactly one obvious map.

## The two shared bases every module reuses

Two request shapes are the same in every module, so neither is written from scratch.

### `Search<X>Request : QueryContainer`

```csharp
// Requests/Orders/SearchOrderRequest.cs
public class SearchOrderRequest : QueryContainer
{
}
```

`QueryContainer` comes from the pagination extension on the technical axis and carries
paging, sorting and filtering. **The empty derived class is the point**: the endpoint and
the service get a named type per module, and the module has one obvious place to put its
own search members the day it needs them. Do not take `QueryContainer` directly in a
service signature.

### `DeleteRange<X>Request : RangeItemRequest<Guid>`

```csharp
// Requests/Orders/DeleteRangeOrderRequest.cs
public class DeleteRangeOrderRequest : RangeItemRequest<Guid>
{
}

public class DeleteRangeOrderRequestValidator : AbstractValidator<DeleteRangeOrderRequest>
{
    public DeleteRangeOrderRequestValidator(IRepositoryWrapper repositoryWrapper)
    {
        Include(new RangeItemValidator<Order, Guid>(repositoryWrapper));
    }
}
```

`RangeItemRequest<T>` carries the id collection; `RangeItemValidator<TEntity, TId>` is
constructed with the repository abstraction and an optional filter expression, and one
`Include` line buys three rules:

| Rule | What it rejects |
|---|---|
| not empty | a delete call with no ids |
| no duplicates | the same id listed twice |
| all ids exist | a batch that would half-succeed |

**The name is the standard: `DeleteRange<X>Request`.** `DeleteMany<X>Request`, or a
hand-rolled request holding `List<Guid> Ids`, is drift — and a hand-rolled one silently
loses all three rules above. Pass the optional filter expression when the batch must be
constrained further: deletable states only, one tenant only.

## Response tiers

A module that returns one entity at several fidelities names the tiers by **suffix**, and
**each tier derives the one above it**.

| Type | Derives | Carries | Returned by |
|---|---|---|---|
| `OrderBaseResponse` | `BaseEntity` | identity plus the two or three members that name an order to a human | other modules' responses, embedded |
| `OrderDefaultResponse` | `OrderBaseResponse` | every stored member a list needs | search / list operations |
| `OrderResponse` | `OrderDefaultResponse` | the default tier plus navigations | get-by-id |
| `OrderDetailResponse` | `OrderResponse` | the full tier plus heavy child collections | one detail screen |

Not every module needs four. Start at `<X>Response`; add a tier when a **second caller
needs a different fidelity of the same entity**, never in advance. A module whose `Base`
and `Default` tiers both derive `BaseEntity` directly is an unfinished chain, not a second
convention — reattach it when you touch the file.

```csharp
// Responses/Orders/OrderDefaultResponse.cs
public class OrderDefaultResponse : OrderBaseResponse
{
    public string? Note { get; set; }

    public OrderStatus Status { get; set; }

    public StoredFilePath? Attachment { get; set; }

    public int OpenAlertCount { get; set; }
}

public class OrderDefaultResponseMapping : Profile
{
    public OrderDefaultResponseMapping()
    {
        CreateMap<Order, OrderDefaultResponse>()
            .ForMember(x => x.Attachment, opt => opt.MapFrom(src => new StoredFilePath(src.Attachment, true)))
            .ForMember(x => x.OpenAlertCount, opt => opt.MapFrom(OrderExpression.OpenAlertCount))
            .IncludeAllDerived();
    }
}
```

```csharp
// Responses/Orders/OrderResponse.cs
public class OrderResponse : OrderDefaultResponse
{
    public CategoryResponse? Category { get; set; }

    public ICollection<TagResponse>? Tags { get; set; }
}

public class OrderResponseMapping : Profile
{
    public OrderResponseMapping()
    {
        CreateMap<Order, OrderResponse>()
            .ForMember(x => x.Tags, opt => opt.MapFrom(src => src.OrderTags!.Select(x => x.Tag)))
            .IncludeAllDerived();
    }
}
```

- **A derived map configures its own members only.** `OrderResponse`'s map says nothing
  about `Attachment` or `OpenAlertCount` because the base map already configured them and
  `.IncludeAllDerived()` carries that configuration down.
- **`.IncludeAllDerived()` goes on every map that has a map derived from it.** Omit it on
  one tier and every tier below loses that tier's configuration without a build error.
- **A tier adds members; it never redefines one.** If a tier needs a member shaped
  differently from the tier above, the two are not tiers of one shape — split them.
- **`ForMember` cannot write a get-only property.** A computed member declared as
  `public T Foo => …` is not a mapping target; configuring one is dead code that reads as
  live configuration. Give the member a setter and map it, or leave it computed and
  configure nothing.

## Which base type a response gets

| The response is | Base type |
|---|---|
| a projection of an entity | `BaseEntity` |
| a projection of an entity with a non-`Guid` key | `BaseEntity<TId>` |
| a count or aggregate summary for a screen | plain class |
| the result of a bulk operation (succeeded, failed, reasons) | plain class |
| a third-party payload passed through or returned | plain class |

**`Id` and `CreatedAt` arrive by inheritance and are never redeclared.** A response that
declares its own `Id` shadows the inherited one, and the two can be mapped from different
sources without a warning.

```csharp
// Responses/Orders/OrderSearchSummaryResponse.cs — no entity behind it, no base, no profile
public class OrderSearchSummaryResponse
{
    public int TotalOrder { get; set; }
}
```

A plain class with no `Profile` is complete as written: nothing projects into it, the
service composes it. Adding a base type would invent an identity the payload does not have.

## Computed members: `Expressions/` or shape glue

A response member that is not stored is computed at projection time. There are two kinds,
and only one of them may be written inline.

| The member | Where it is defined |
|---|---|
| has a name a stakeholder would recognise | `Expressions/` |
| could be filtered or sorted on by a query | `Expressions/` |
| could be needed by an entity method or a second response | `Expressions/` |
| only reshapes what the projection already has — a link-table hop, a stored key wrapped in its path type | inline `ForMember` |

```csharp
// Expressions/OrderExpression.cs — one definition, three possible call sites
public static class OrderExpression
{
    public static Expression<Func<Order, int>> OpenAlertCount =>
        order => order.Alerts.Count(x => x.Source == AlertSource.Order && x.Status == AlertStatus.Unprocessed);

    public static Expression<Func<Order, decimal>> CurrentPackedWeight =>
        order => order.Shipments!.OrderByDescending(x => x.CreatedAt).Select(x => x.PackedWeight).FirstOrDefault();
}
```

```csharp
// Responses/Orders/OrderDefaultResponse.cs — the profile names the expression, nothing more
CreateMap<Order, OrderDefaultResponse>()
    .ForMember(x => x.OpenAlertCount, opt => opt.MapFrom(OrderExpression.OpenAlertCount))
    .ForMember(x => x.CurrentPackedWeight, opt => opt.MapFrom(OrderExpression.CurrentPackedWeight))
    .ForMember(x => x.Attachment, opt => opt.MapFrom(src => new StoredFilePath(src.Attachment, true)))
    .IncludeAllDerived();
```

The last `ForMember` is shape glue and stays inline: it wraps a stored key in the type
that knows how to render it, and it decides nothing.

## Anti-example: a computed value re-derived in the profile

The same members, written directly into the profile:

```csharp
public class OrderDefaultResponseMapping : Profile
{
    public OrderDefaultResponseMapping()
    {
        decimal defaultPackedWeight = 0;
        decimal defaultDeclaredWeight = 0;

        CreateMap<Order, OrderDefaultResponse>()
            .ForMember(x => x.OpenAlertCount, opt => opt.MapFrom(src => src.Alerts.Count(x => x.Source == AlertSource.Order && x.Status == AlertStatus.Unprocessed)))
            .ForMember(x => x.CurrentPackedWeight, opt => opt.MapFrom(src => src.Shipments!.Any() ? src.Shipments!.OrderByDescending(x => x.CreatedAt).FirstOrDefault()!.PackedWeight : defaultPackedWeight))
            .ForMember(x => x.CurrentDeclaredWeight, opt => opt.MapFrom(src => src.Shipments!.Any() ? src.Shipments!.OrderByDescending(x => x.CreatedAt).FirstOrDefault()!.DeclaredWeight : defaultDeclaredWeight))
            .IncludeAllDerived();
    }
}
```

It compiles, it projects, and it is still wrong:

- **"An open alert" and "the current shipment" are business definitions living in a
  mapping profile.** The next place that needs either — a search filter, a sort, an entity
  method, a second response tier — will re-derive it, and the two derivations will
  disagree the day the alert statuses change.
- **The two "current shipment" members repeat the same ordering twice**, so the definition
  is already duplicated inside the one file that introduced it.
- **The local defaults are the tell.** A `Profile` constructor that needs local variables
  to express a member is a computation that has outgrown the profile. In `Expressions/`,
  `FirstOrDefault()` over a projected value supplies the same default without them.
- **The ternary over `Any()` is not free**: the ordering is written twice for the database
  to evaluate, where the expression form writes it once.

Move each one to `Expressions/`, then have the profile name it. Nothing about the response
class changes.

## Anti-example: a response named tier-first

```
Orders/Responses/
├── DetailOrderResponse.cs          ✗ tier-first
├── SearchOrderSummaryResponse.cs   ✗ tier-first
├── OrderBaseResponse.cs            ✓
└── OrderResponse.cs                ✓
```

**A request is named for its action; a response is named for its entity.**
`CreateOrderRequest`, `SearchOrderRequest`, `DeleteRangeOrderRequest` all lead with the
verb, because a request exists to serve one action. A response exists to be one entity at
one fidelity, so the entity leads and what it is follows: `OrderDetailResponse`,
`OrderSearchSummaryResponse`.

The tier-first form compiles and behaves identically, and it is still a review finding:

- **The responses of one entity stop sorting together.** `OrderResponse` and its detail
  tier are separated in every file listing by whatever else the folder holds.
- **The name reads as a different concept.** `DetailOrderResponse` scans as a response
  about a "detail order"; nothing in it says "the detail tier of `OrderResponse`".
- **It invites a third convention.** A folder holding both forms teaches the next author
  that the naming is a preference.

Rename when you touch the file. Nothing but the type name and its references changes.

## When a family grows: theme subfolders

```
Orders/
├── Requests/
│   ├── Orders/        # the module's own aggregate
│   ├── Shipments/
│   ├── Returns/
│   └── Webhooks/
├── Responses/
│   ├── Orders/
│   ├── Shipments/
│   └── Returns/
└── Services/          # never subfoldered — see "When a service outgrows one file"
```

- **Subfolder by theme, never by verb.** `Orders/`, `Shipments/`, `Returns/` are shapes;
  `Create/`, `Search/`, `Bulk/` are actions, and the action is already in the file name.
- **The module's own aggregate gets a subfolder too**, named after it, once any sibling
  theme exists. A single un-nested file beside four subfolders is the one nobody finds.
- **Subfolder when a theme has its own base-and-derived family**, not before. A module
  with five request files keeps them flat.
- **The namespace follows the folder** — `…Modules.Orders.Requests.Shipments` — so a move
  is a namespace change and every reference must move with it.
- **This is where `Requests/` and `Responses/` differ from `Services/`.** `Services/`
  forbids subfolders because a subfolder there hides files that are not services;
  `Requests/` and `Responses/` allow them because every file inside is still a request or
  a response, and the folder only groups a family.

## Review checklist

- Every request file declares the request, its validator, and its `Profile` if it maps —
  and nothing else. Every response file declares the response and its `Profile` below it.
- No `Mappings/` folder anywhere in the module.
- Every `Profile` is named `<ClassName>Mapping` in full.
- A derived validator reuses base rules through `Include(...)` and restates none of them.
- Every derived request declares its own map; a base request declares a profile only
  when its map is customized, and then it ends `.IncludeAllDerived()`.
- Every response rung that has a derived rung declares `.IncludeAllDerived()`.
- Every response that projects an entity derives `BaseEntity`, and each tier derives the
  tier above it; none redeclares `Id` or `CreatedAt`; summary, bulk-result and third-party
  payload shapes are plain classes.
- Every computed member with a business name is projected from `Expressions/`; inline
  `ForMember` appears only for a link-table hop or a stored key wrapped in its path type.
- No local variable is captured in a `Profile` constructor, and no `ForMember` targets a
  get-only property.
- Bulk delete is `DeleteRange<X>Request : RangeItemRequest<Guid>` with a one-line
  validator; search is `Search<X>Request : QueryContainer`.
- Response type names start with the entity; tier words are suffixes.
- Theme subfolders are named after shapes, not verbs.
