## Infrastructure — the Modules axis

A module is **one business capability**. It owns that capability's entities,
contracts and behaviour, and it is the only place business meaning lives.

A module has **no facade-style `Startup.cs`**. It never registers itself — its
services are picked up by the marker scan (see *Infrastructure — the Facades
axis*). The single exception is `Settings/Startup.cs`, which exists only to bind
that module's options (see the settings table in the same section).

### The standard structure — three tiers of growth

Create a folder when its trigger is real, never in advance: an empty `Commands/`
is noise.

```
Modules/Orders/
├── Entities/            # EF entities of this capability        ┐ tier 1 —
├── Requests/            # incoming DTOs                         │ every
├── Responses/           # outgoing DTOs (+ their Profile)       │ module
├── Services/            # the business services                 ┘
├── Seeders/             # + the module ships reference data     ┐ tier 2 —
├── Validations/         # + a rule must hit the database        ┘ as needed
├── Commands/            # + the module is driven through        ┐
├── Queries/             #   MediatR                             │
├── Events/              #                                       │ tier 3 —
├── Settings/            # + it binds its own options section    │ grown
├── Enums/               # + enums of the capability             │ modules
├── Expressions/         # + write-once reusable expressions     │
└── ElkEntities/         # + Elk-prefixed search documents       ┘
```

`Requests/` and `Responses/` may be subfoldered by theme once they grow
(`Requests/Approvals/`, `Requests/Profiles/`). `Services/` never subfolders.

Every enum the capability owns lives in **`Enums/`** — never declared inside an
entity, response or service file. One folder answers "what states does this
capability have?", and an enum that is reachable from a response is part of the
module's contract, not an implementation detail of one class.

`Validations/` holds **global validations**, one `<X>Validation.cs` file per
concern: static extension methods on `IRepositoryWrapper` that FluentValidation
rules call when a check needs the database. The rule stays in the validator;
only the query lives here.

```csharp
public static class OrderValidation
{
    public static bool IsExistOrderCode(this IRepositoryWrapper repositoryWrapper,
        string code, Guid? exceptId = default)
        => repositoryWrapper.Repository<Order>().IsExistByUnique(x => x.Code, code, exceptId);
}
```

### One service file = interface + implementation

The interface and its implementation live in the **same file**, and the lifetime
marker sits on the **interface** — that is what the scan binds:

```csharp
public interface IOrderService : IScopedService
{
    Task<OrderDetailResponse> CreateAsync(CreateOrderRequest request);
    Task<OrderDetailResponse> GetDetailAsync(Guid orderId);
}

public class OrderService : IOrderService
{
    private readonly IRepositoryWrapper repositoryWrapper;
    private readonly IMapper mapper;

    public OrderService(IRepositoryWrapper repositoryWrapper, IMapper mapper) { /* … */ }

    public async Task<OrderDetailResponse> CreateAsync(CreateOrderRequest request) { /* … */ }
}
```

The scan registers `IOrderService` → `OrderService`, scoped — no registration
line anywhere. Do not split the interface into its own file and do not put it in
`Core`: the contract and its implementation are read together, so they live
together.

### When a service grows: partial files, suffix-named

One class, one folder, parts named by functional role — **suffix** form, so
every part of one service sorts adjacent on disk:

```
Services/
├── OrderService.cs            # interface + ctor + the main surface
├── OrderService.Approval.cs   # the approval-related operations
├── OrderService.Profile.cs    # the profile-related operations
└── OrderService.Private.cs    # shared private helpers
```

Splitting the class means **the interface becomes partial too**:

- The core file declares `public partial interface IOrderService : IScopedService`
  and `public partial class OrderService : IOrderService`. The marker and the
  base lists appear here and nowhere else.
- A part that adds **public operations** declares both
  `public partial interface IOrderService` and `public partial class OrderService`
  — its own slice of the contract next to its own slice of the implementation,
  base lists not repeated.
- A part with only **private helpers** declares just
  `public partial class OrderService`; it adds nothing to the contract.

### `Services/` holds services and nothing else

The service is the meaning-bearing business unit. `Services/` is not a folder for
"things the service uses".

**Every file in `Services/` is named `<Name>Service.cs` or
`<Name>Service.<Role>.cs`, and every type it declares is `<Name>Service` or
`I<Name>Service`.** A file that fails this test is not a service — a policy,
calculator, planner, builder, mapper, helper, guard, resolver or model bag does
not belong here, however closely it works with one.

Where it goes instead:

- A genuine business rule belongs **inside the service**, or as a method on the
  **entity** that owns it, backed by `Expressions/` (below).
- A reusable technical mechanism belongs on the **Facades axis** — answer the two
  placement questions in *Infrastructure — the Facades axis*.

**Red flags — stop and re-place the file:**

- The file you are creating in `Services/` is not named `…Service` /
  `…Service.<Role>`.
- You are about to add a subfolder under `Services/`.
- You are extracting logic out of a service "to keep the service small". Make it
  a suffix-named **partial** instead.

**Anti-example (mild, real).** A module with a correctly split service, plus four
files that crept in later:

```
Modules/Tickets/Services/
  TicketService.cs                 the service
  TicketService.Auth.cs            partial, suffix-named
  TicketService.Profile.cs         partial
  TicketService.Webhook.cs         partial
  TicketPrivacyPolicy.cs           ✗ static policy — not a service
  TicketResponsePolicy.cs          ✗ static policy — not a service
  TicketDataMaskingHelper.cs       ✗ helper — not a service
  TicketResponseMaskingHelper.cs   ✗ helper — not a service
```

Every stray file was locally defensible, and each one made the folder mean less.
At four in, `Services/` no longer answers "what can this module do?" at a glance.

**Anti-example (severe, real).** The same drift left unchecked: **46 files** in
one `Services/` folder — planners, calculators, builders, validators, response
mappers, key builders, static policies, model bags, a settings class, plus two
nested subfolders of more of the same. Only about six of the forty-six even
carry a service marker — and five of those are themselves fragments split off
the module's one real service; the rest is furniture. At this size the folder
tells you nothing: it is where the module's files went, not where its services
are.

### Commands, queries, events — MediatR as messaging, not CQRS

**MediatR here is in-process messaging, not CQRS.** `Commands/` and `Queries/`
are not a write model and a read model, they imply no separate stores, and they
carry no read/write separation. They are named message envelopes.

Each message is **thin**: a sealed record plus a handler that delegates straight
to the module's service, both in one file. All the logic stays in the service.

```csharp
internal sealed record ApproveOrderCommand(Guid ActorId, Guid OrderId)
    : IRequest<OrderDetailResponse>;

internal sealed class ApproveOrderHandler
    : IRequestHandler<ApproveOrderCommand, OrderDetailResponse>
{
    private readonly IOrderService orderService;

    public ApproveOrderHandler(IOrderService orderService) => this.orderService = orderService;

    public async Task<OrderDetailResponse> Handle(ApproveOrderCommand request, CancellationToken cancellationToken)
        => await orderService.ApproveAsync(request.ActorId, request.OrderId, cancellationToken)
            .ConfigureAwait(false);
}
```

`Events/` is the same shape with `INotification` / `INotificationHandler<T>`.

### `Expressions/` — write the rule once, use it three ways

A value or predicate that is **computed, not stored** goes in `Expressions/` as a
`public static Expression<Func<TEntity, TResult>>` on a static class:

```csharp
public static class OrderExpression
{
    public static Expression<Func<Order, bool>> IsExpired => x =>
        x.EndAt.HasValue && DateTimeOffset.UtcNow > x.EndAt;

    public static Expression<Func<Order, OrderStatus>> GetStatus => x =>
        x.AdminStatus == OrderAdminStatus.Inactive ? OrderStatus.Inactive
        : DateTimeOffset.UtcNow >= x.EndAt ? OrderStatus.Expired
        : OrderStatus.Active;
}
```

One definition, three call sites:

1. **In a mapping `Profile`** — `.MapFrom(OrderExpression.GetStatus)` — so the
   projection stays SQL-translatable.
2. **As an entity method**, for an object already in memory —
   `public bool IsExpired() => OrderExpression.IsExpired.Compile().Invoke(this);`
3. **In a query predicate** on `IQueryable` — `.Where(…)` or repository
   `Find(…)` — so EF Core translates it to SQL. *(Intended use; adopt it for new
   query code.)*

One definition means the status a query filters on, the status a response shows
and the status an entity method reports can never disagree.

### Mapping placement

**There is no `Mappings/` folder.** The AutoMapper `Profile` for a response lives
in the **same file as the response class it maps**, below it:

```csharp
public class OrderBaseResponse : BaseEntity { /* … */ }

public class OrderBaseResponseMapping : Profile
{
    public OrderBaseResponseMapping()
    {
        CreateMap<Order, OrderBaseResponse>()
            .ForMember(x => x.Status, opt => opt.MapFrom(OrderExpression.GetStatus))
            .IncludeAllDerived();
    }
}
```

The contract and its projection then cannot drift apart silently — you cannot add
a property and forget the mapping, because both are on the same screen. Only
placement is fixed here; how to write the mapping belongs to the mapping skill.

### `ElkEntities/`

When a module is projected into Elasticsearch, its search documents live in
`ElkEntities/` as separate types prefixed `Elk` (`ElkOrder`). Never index a
database entity and never reuse one as a search document. How the projection is
built and queried belongs to the `elasticsearch-search` skill; the folder is
named here so placement is never in doubt.
