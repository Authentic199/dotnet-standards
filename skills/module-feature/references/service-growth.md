# Growing a service past one file

- [One service, three parts](#one-service-three-parts)
- [What each part declares](#what-each-part-declares)
- [Naming the `<Role>` suffix](#naming-the-role-suffix)
- [Anti-example: prefix-named parts](#anti-example-prefix-named-parts)
- [Dumping ground, moderate](#dumping-ground-moderate)
- [Dumping ground, extreme](#dumping-ground-extreme)
- [Review checklist](#review-checklist)

## One service, three parts

```
Orders/Services/
├── OrderService.cs              # core: marker, base list, fields, constructor
├── OrderService.Fulfillment.cs  # public operations for one role
└── OrderService.Private.cs      # private helpers, no interface
```

### `OrderService.cs` — the core part

```csharp
namespace Infrastructure.Modules.Orders.Services;

public partial interface IOrderService : IScopedService
{
    /// <summary>Create an order and return it.</summary>
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>Read one order.</summary>
    Task<OrderResponse> GetAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>Search orders, newest first unless the request sorts otherwise.</summary>
    Task<PaginationResponse<OrderResponse>> SearchAsync(SearchOrderRequest request, CancellationToken cancellationToken = default);
}

public partial class OrderService : IOrderService
{
    private readonly IRepositoryWrapper repositoryWrapper;
    private readonly IMapper mapper;
    private readonly ICurrentUser currentUser;

    public OrderService(
        IRepositoryWrapper repositoryWrapper,
        IMapper mapper,
        ICurrentUser currentUser)
    {
        this.repositoryWrapper = repositoryWrapper;
        this.mapper = mapper;
        this.currentUser = currentUser;
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (await repositoryWrapper.Repository<Order>().AnyAsync(x => x.Code == request.Code, cancellationToken))
        {
            throw new BadRequestException(Messages<Order>.AlreadyExist(x => x.Code));
        }

        Order order = mapper.Map<Order>(request);
        await repositoryWrapper.Repository<Order>().AddAsync(order, cancellationToken);

        return await GetResponseAsync(order.Id, cancellationToken);
    }

    public async Task<OrderResponse> GetAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await repositoryWrapper.Repository<Order>()
            .Find(x => x.Id == orderId)
            .ProjectTo<OrderResponse>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new BadRequestException(Messages<Order>.NotFound());
    }

    public async Task<PaginationResponse<OrderResponse>> SearchAsync(SearchOrderRequest request, CancellationToken cancellationToken = default)
    {
        IQueryable<OrderResponse> orders = repositoryWrapper.Repository<Order>()
            .Find(isAsNoTracking: true)
            .ProjectTo<OrderResponse>(mapper.ConfigurationProvider);

        return await orders
            .ApplyFilter(request.Filter)
            .ApplySort($"{nameof(OrderResponse.CreatedAt)} {OrderTypeAcronym.Desc}", request.SortQuery)
            .ToPagedListAsync(request.Current, request.PageSize, cancellationToken);
    }
}
```

### `OrderService.Fulfillment.cs` — a part adding public operations

```csharp
namespace Infrastructure.Modules.Orders.Services;

public partial interface IOrderService
{
    /// <summary>Move a paid order to shipped and record who shipped it.</summary>
    Task<OrderResponse> ShipAsync(Guid orderId, ShipOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cancel an order that has not shipped.</summary>
    Task<OrderResponse> CancelAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public partial class OrderService
{
    public async Task<OrderResponse> ShipAsync(Guid orderId, ShipOrderRequest request, CancellationToken cancellationToken = default)
    {
        Order order = await repositoryWrapper.Repository<Order>()
            .Find(x => x.Id == orderId)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new BadRequestException(Messages<Order>.NotFound());

        if (order.Status != OrderStatus.Paid)
        {
            throw new BadRequestException(Messages<Order>.Invalid(x => x.Status));
        }

        order.Status = OrderStatus.Shipped;
        order.ShippedBy = currentUser.Id;

        await repositoryWrapper.Repository<Order>().UpdateAsync(order, cancellationToken);

        return await GetResponseAsync(order.Id, cancellationToken);
    }
}
```

No `: IScopedService`. No `: IOrderService`. No fields. No constructor. The part
adds operations and nothing else, and it reaches the dependencies the core part
already declared.

A write that must load the entity reads it through `Find(…)` and awaits with the
token. `GetByIdAsync` takes `params object[] keyValues` and has no cancellation
overload on the relational repository — passing a token to it compiles and is
then treated as a second key value.

### `OrderService.Private.cs` — the helpers part

```csharp
namespace Infrastructure.Modules.Orders.Services;

public partial class OrderService
{
    private async Task<OrderResponse> GetResponseAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await repositoryWrapper.Repository<Order>()
            .Find(x => x.Id == orderId)
            .Include(x => x.Lines)
            .ProjectTo<OrderResponse>(mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);
    }
}
```

**No partial interface here** — a private member on a public interface is a
contradiction, and the file exists precisely because these members are not part of
the contract. The token stays required, without `= default`: the default exists for
callers outside the type, and inside it every caller already holds one.

## What each part declares

| | Core part | Operations part | Private part |
|---|---|---|---|
| `partial interface I<Name>Service` | yes, **with** `: IScopedService` | yes, **no** base list | never |
| `partial class <Name>Service` | yes, **with** `: I<Name>Service` | yes, **no** base list | yes, **no** base list |
| `private readonly` fields | all of them | none | none |
| constructor | the only one | none | none |
| `public` operations | the module's primary ones | the role's operations | none |
| `private` helpers | those only its own operations use | those only its own operations use | those shared across parts |

What goes wrong when a part overreaches:

| Mistake | What actually happens |
|---|---|
| Second part repeats `: IScopedService` | compiles — and the lifetime now has two homes, so a reviewer must read every part to know how the service is registered |
| Second part repeats `: I<Name>Service` | compiles — same problem for the contract; the core part stops being the answer to "what is this service?" |
| Second part redeclares a field | **compile error** — duplicate member |
| Second part adds a second constructor | compiles — then the container has no rule for choosing between them |
| Private part declares `partial interface` | the "private" helper is now public API |

## Naming the `<Role>` suffix

`<Name>Service.<Role>.cs` — PascalCase, one word wherever possible.

- **The role names a group of operations a caller can ask for**: `Checkout`,
  `Fulfillment`, `Reporting`, `Webhook`, `Import`.
- **`Private` is reserved** for the part that holds only private helpers. A service
  has at most one.
- **The role is not a layer, a technique or a type kind.** `Helper`, `Utils`,
  `Logic`, `Impl`, `Core`, `Main`, `Extensions`, `Part2` are all wrong — they
  describe the file's relationship to the code rather than to the caller.
- **The role never repeats the service name** (`OrderService.Order.cs`).
- **A role you cannot name in one word is the signal to stop splitting parts** and
  ask whether the capability itself has outgrown one module.

## Anti-example: prefix-named parts

```
Orders/Services/
├── Checkout.OrderService.cs     ✗ prefix
├── Fulfillment.OrderService.cs  ✗ prefix
├── OrderService.cs
├── OrderService.Private.cs      ✓ suffix
├── OrderTagService.cs
├── OrderTypeService.cs
└── Reporting.OrderService.cs    ✗ prefix
```

The same folder does it both ways. The prefix form compiles and behaves
identically, and it is still wrong:

- **The parts of one type no longer sort together.** `OrderService.cs` and its three
  operation parts are separated in every file listing by whatever else the folder
  holds — here, by two unrelated services.
- **The file name reads as a different type.** `Checkout.OrderService.cs` scans as a
  type named `Checkout.OrderService`; nothing in the name says "part of
  `OrderService`".
- **It invites a third convention.** A folder with both forms teaches the next
  author that naming is a preference.

Rename to the suffix form when you touch such a file. A rename is safe: no
namespace, type name or registration changes.

## Dumping ground, moderate

A real module whose partials are correctly shaped and whose folder still is not:

```
Orders/Services/
├── OrderService.cs                    ✓ core part
├── OrderService.Checkout.cs           ✓ operations part
├── OrderService.Fulfillment.cs        ✓ operations part
├── OrderService.PartnerWebhook.cs     ✓ operations part
├── OrderRedactionHelper.cs            ✗
├── OrderResponseRedactionHelper.cs    ✗
├── OrderRetentionPolicy.cs            ✗
└── OrderRedactionResponsePolicy.cs    ✗
```

| File | What it is | What it should have been |
|---|---|---|
| `OrderRedactionHelper.cs` | `public static class` with `Mask(…)` overloads that blank fields on already-materialised objects | a private part of the owning service, or the mapping profile that builds the response — response shaping is projection work |
| `OrderResponseRedactionHelper.cs` | `public static class`, generic `MaskIfNeeded<T>` over single / list / paginated responses | same; two static classes doing one job is the folder telling you the projection was never finished |
| `OrderRetentionPolicy.cs` | `public static class`: `GetDeletionCutoff(now, settings)`, `CanRestore(order, now, settings)`, `AnonymizeIdentity(order)` | `CanRestore` and `AnonymizeIdentity` are behaviour of `Order` — the entity owns them; the cutoff is a computed value → the module's `Expressions/` |
| `OrderRedactionResponsePolicy.cs` | an interface marked `IScopedService` plus an `internal class` injecting the repository | it **is** a service by every structural test — only the *name* is wrong. Rename to `OrderRedactionService.cs`, or fold it into the owning service as a part |

The last row is the whole rule in one file: `Services/` is not closed to new types,
it is closed to new *names*. Anything in it must be a `<Name>Service…` file.

## Dumping ground, extreme

The same folder in a module that never stopped: **46 files and two subfolders, of
which six are services.**

```
Orders/Services/
├── OrderService.cs
├── …29 more files at the root…
├── Repricing/           ✗ subfolders are forbidden outright
└── Returns/             ✗
```

| Kind (count) | Representative names | Where each belongs |
|---|---|---|
| Services (6) | `OrderService.cs`, `OrderLifecycleEventService.cs`, `OrderHistoryQueryService.cs` | they stay — several services in one module is normal; what is abnormal is the forty non-services around them |
| Builders (9) | `OrderResponseBuilder.cs`, `OrderPayloadBuilder.cs`, `OrderLockKeyBuilder.cs` | response builders → the mapping profile; payload and key builders → a private part |
| Calculators, counters (6) | `OrderTotalCalculator.cs`, `OrderExpiryDeadlineCalculator.cs` | arithmetic over one entity → the entity; anything the database must evaluate → `Expressions/` |
| Planners (5) | `OrderShipmentPlanner.cs`, `OrderRepricingPlanner.cs` | a suffix part of the service that runs the plan |
| Policies, guards (4) | `OrderActionPolicy.cs`, `OrderUpdateFreezeGuard.cs` | rules about one entity → that entity; precondition guards → the module's `<X>Validation.cs` |
| Validators (3) | `OrderPayloadValidator.cs`, `OrderPlanValidator.cs` | the module's request validators, or `<X>Validation.cs` |
| Mappers (2) | `OrderRejectMessageMapper.cs` | the mapping profile beside the class it maps |
| Record bags (3) | `OrderTotalModels.cs` — a file of bare `record` declarations | `Responses/` if they cross the boundary; private nested types if they do not |
| Settings (1) | `OrderProcessingSettings.cs`, bound from configuration | the module's `Settings/` |
| Resolvers, filters, classifiers (3) | `OrderStatusResolver.cs`, `OrderDerivedStatusFilter.cs` | derived status → `Expressions/`; filters → the query that needs them |
| Loaders, appliers (2) | `OrderDataLoader.cs`, `OrderPlanApplier.cs` | private parts of the service that owns the operation |
| Generators, utilities (2) | `OrderCodeGenerator.cs`, `ParameterReplacer.cs` | code generation → the entity or `Expressions/`; `ParameterReplacer` has no business meaning at all → the technical axis, a facade |

Two things to read out of that table:

- **The module has no `Expressions/`, no validation type, no `Settings/` folder.**
  Every file that needed one went to `Services/` instead, because `Services/`
  already existed. That is how the folder becomes the module's attic.
- **The subfolders are the terminal stage.** Once `Services/` holds enough
  non-services to feel disorganised, the next author organises the junk instead of
  removing it — and `Repricing/` makes the wrong home permanent.

## Review checklist

- Every file in `Services/` is named `<Name>Service.cs` or `<Name>Service.<Role>.cs`.
- No subfolders.
- Exactly one part carries the lifetime marker and the base lists, and it is the
  suffix-less one.
- Exactly one constructor, in that same part.
- The private part declares no `partial interface`.
- Every public operation ends in `CancellationToken cancellationToken = default`
  and returns a response type, not an entity.
- No part is prefix-named.
