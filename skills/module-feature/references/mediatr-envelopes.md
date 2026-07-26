# MediatR envelopes: commands, queries and events

- [The rule, and the topology it creates](#the-rule-and-the-topology-it-creates)
- [Anatomy of a command envelope](#anatomy-of-a-command-envelope)
- [A query envelope](#a-query-envelope)
- [Naming](#naming)
- [Visibility: `internal sealed`](#visibility-internal-sealed)
- [Events: the notification shape](#events-the-notification-shape)
- [Send, Publish, and who holds `IMediator`](#send-publish-and-who-holds-imediator)
- [The cancellation token](#the-cancellation-token)
- [Anti-example: a handler that bypasses the service](#anti-example-a-handler-that-bypasses-the-service)
- [Anti-example: a public, unsealed event pair](#anti-example-a-public-unsealed-event-pair)
- [Review checklist](#review-checklist)

## The rule, and the topology it creates

**Call the service directly inside your own module; send a message to reach another
module's capability.** One decision, one consequence, and the folder layout follows from
it:

```
Orders/                                   # owns the order lifecycle
├── Commands/
│   └── ConfirmOrderForCarrierCommand.cs  # message + handler → IOrderService
├── Queries/
│   └── SearchAvailableOrdersForCarrierQuery.cs
├── Events/
│   └── OrderConfirmedSucceededEvent.cs   # event + handler → IOrderHistoryService
└── Services/
    └── OrderService.cs                   # holds IMediator; Sends into other modules

Carriers/
└── Services/
    └── CarrierService.Profile.cs         # holds IMediator; Sends ConfirmOrderForCarrierCommand
```

Read the arrows carefully, because the direction is the part that gets built backwards:

| | Where the file lives | Who injects `IMediator` | Who the handler injects |
|---|---|---|---|
| command / query | the module that **owns** the capability | the **foreign** module's service, which `Send`s | the **owning** module's service |
| event | the module that **raises** it | that same module's service, which `Publish`es | the **consuming** module's service |

So a request envelope points *inward* — a foreign module asks, and the handler answers
by calling the owner's service. An event points *outward* — the owner announces, and the
handler beside the announcement calls the consumer. Either way the handler and the
service it calls belong to the same module as each other, and the envelope file is the
only place the two module names appear together.

## Anatomy of a command envelope

```csharp
// Orders/Commands/ConfirmOrderForCarrierCommand.cs
using Infrastructure.Modules.Orders.Responses;
using Infrastructure.Modules.Orders.Services;
using MediatR;

namespace Infrastructure.Modules.Orders.Commands;

internal sealed record ConfirmOrderForCarrierCommand(Guid CarrierId, Guid OrderId) : IRequest<OrderResponse>;

internal sealed class ConfirmOrderForCarrierHandler : IRequestHandler<ConfirmOrderForCarrierCommand, OrderResponse>
{
    private readonly IOrderService orderService;

    public ConfirmOrderForCarrierHandler(IOrderService orderService)
    {
        this.orderService = orderService;
    }

    public async Task<OrderResponse> Handle(ConfirmOrderForCarrierCommand request, CancellationToken cancellationToken)
    {
        return await orderService.ConfirmAsync(request.CarrierId, request.OrderId, cancellationToken);
    }
}
```

Everything in that file is load-bearing:

- **One file, two types, no third.** The message and the handler are read together or not
  at all; splitting them across `Commands/` and some `Handlers/` folder doubles the
  navigation for zero benefit and lets one drift from the other.
- **The message is a positional record, not a class.** Its members are the arguments of
  the call, it has no behaviour and no validation, and value equality comes free. There
  is no `Validations/` entry for a message: the request object it carries was validated
  at the HTTP boundary before the service that sends ever ran.
- **The handler's constructor takes the owning module's service and nothing else.** No
  repository, no mapper, no `IMediator`. The moment a second dependency appears, ask what
  the handler is doing that the service should be doing.
- **The body is one `return await` line.** `request.X` unpacked into the service call, the
  token forwarded, nothing computed on the way in and nothing shaped on the way out.
- **The return type is the service's return type** — a response, never an entity.
- **A parameter may be a whole request object**: `ConfirmOrderForCarrierCommand(Guid
  CarrierId, ConfirmOrderRequest Request)` when the foreign module is forwarding a body
  it received. The record then carries an id it resolved plus the payload it was given.

> The canonical writes `.ConfigureAwait(false)` on the awaited call in handlers. That is a
> repo-wide style choice, orthogonal to the envelope shape; the samples here omit it, as
> the other sections of this skill do.

## A query envelope

Identical shape; only the return type is bigger.

```csharp
// Orders/Queries/SearchAvailableOrdersForCarrierQuery.cs
using Infrastructure.Facades.Common.Extensions;
using Infrastructure.Modules.Orders.Responses;
using Infrastructure.Modules.Orders.Services;
using MediatR;

namespace Infrastructure.Modules.Orders.Queries;

internal sealed record SearchAvailableOrdersForCarrierQuery(
    Guid CarrierId,
    QueryContainer Query) : IRequest<PaginationResponse<OrderResponse>>;

internal sealed class SearchAvailableOrdersForCarrierHandler
    : IRequestHandler<SearchAvailableOrdersForCarrierQuery, PaginationResponse<OrderResponse>>
{
    private readonly IOrderService orderService;

    public SearchAvailableOrdersForCarrierHandler(IOrderService orderService)
    {
        this.orderService = orderService;
    }

    public async Task<PaginationResponse<OrderResponse>> Handle(
        SearchAvailableOrdersForCarrierQuery request,
        CancellationToken cancellationToken)
    {
        return await orderService.SearchAvailableForCarrierAsync(request.CarrierId, request.Query, cancellationToken);
    }
}
```

**`QueryContainer` travels as a member, not unpacked.** Paging, sorting, filtering and
search fields arrive as one object from the HTTP surface and are handed on unchanged; a
message that spreads them into six positional members has to be edited every time the
pagination facade grows a knob.

**`Commands/` versus `Queries/` is not the CQRS split.** There is one model and one
database; the folder is a reading aid. Put an envelope in `Queries/` when the operation
only reads, in `Commands/` when it changes anything — including a read-and-clear
operation whose name starts with `Get`. If you are unsure, it changes something.

## Naming

| Kind | Contract | Message name | Handler name |
|---|---|---|---|
| command | `IRequest<T>` | verb-first + `Command` — `ConfirmOrderForCarrierCommand` | `ConfirmOrderForCarrierHandler` |
| query | `IRequest<T>` | verb-first + `Query` — `SearchAvailableOrdersForCarrierQuery` | `SearchAvailableOrdersForCarrierHandler` |
| event | `INotification` | subject + what happened + `Event` — `OrderConfirmedSucceededEvent` | `OrderConfirmedSucceededHandler` |

**The handler's name is the message's name with the kind word replaced by `Handler`.**
Not shortened, not re-described, not renamed after what the handler happens to call. The
pair sorts together, greps together, and a reader who has the message name never has to
search for its handler.

**A request names the call; an event names the fact.** `ConfirmOrderForCarrier` is an
instruction in the imperative, and the `For<Actor>` segment says who it is being done on
behalf of — the same operation for a different actor is a different envelope, because the
identity resolution differs. `OrderConfirmedSucceeded` is past tense about something that
already happened; nobody can decline it.

**Do not restate the module in the message name.** The file is already under
`Orders/Commands/` and the namespace already says `Modules.Orders.Commands` —
`OrderConfirmOrderCommand` says it three times.

## Visibility: `internal sealed`

**Every message and every handler is `internal sealed`.** Both words earn their place:

- **`internal` is the enforcement mechanism for the whole decision rule.** Modules share
  one assembly, so `internal` does not hide an envelope from another module — it hides it
  from the **HTTP project, which is a separate assembly**. A controller therefore cannot
  construct the message at all, and so cannot `Send` its way past a service into another
  module's internals. Make one envelope `public` and that door is open for every
  controller thereafter. The rule "controllers call services" stops being a convention
  someone has to remember and becomes a thing the compiler settles.
- **`sealed` says this record is the message, not a base for one.** Message hierarchies
  are how dispatch becomes ambiguous: MediatR resolves a handler by closed type, and a
  derived message with no handler of its own fails at run time, in a `Publish` that
  silently does nothing or a `Send` that throws where it was dispatched rather than where
  it was defined.

The type the handler *calls* is public — `IOrderService` is the module's surface. The
envelope is internal because it is the module's plumbing.

## Events: the notification shape

```csharp
// Orders/Events/OrderConfirmedSucceededEvent.cs
using Infrastructure.Facades.Logging;
using Infrastructure.Modules.OrderHistories.Services;
using MediatR;

namespace Infrastructure.Modules.Orders.Events;

internal sealed record OrderConfirmedSucceededEvent(Guid OrderId, int ReservedCredit) : INotification;

internal sealed class OrderConfirmedSucceededHandler : INotificationHandler<OrderConfirmedSucceededEvent>
{
    private readonly IOrderHistoryService orderHistoryService;

    public OrderConfirmedSucceededHandler(IOrderHistoryService orderHistoryService)
    {
        this.orderHistoryService = orderHistoryService;
    }

    public async Task Handle(OrderConfirmedSucceededEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await orderHistoryService.CreateAsync(notification.OrderId, notification.ReservedCredit, cancellationToken);
        }
        catch (Exception ex)
        {
            LogExtension.Error(nameof(Handle), ex, nameof(OrderConfirmedSucceededHandler));
            throw;
        }
    }
}
```

- **The parameter is `notification`, and there is no return type.**
  `INotificationHandler<T>.Handle` returns `Task`. A "notification" that the publisher
  needs an answer from is a request wearing the wrong interface — use `IRequest<T>` and
  `Send`.
- **The handler injects the *consuming* module's service.** This file sits in `Orders/`
  and calls into `OrderHistories/`: the event file is deliberately the one place the two
  modules meet, so the coupling is visible in a single ten-line file rather than spread
  across two services.
- **Catch, log, rethrow — that exact sequence.** `LogExtension.Error(nameof(Handle), ex,
  nameof(<Handler>))` names the method and the handler type, which is what makes one line
  of a log locate the failure; `throw;` — bare, never `throw ex;` — preserves the stack.
  Swallowing the exception instead is the thing to weigh consciously: `Publish` runs
  handlers inside the publisher's call, so a rethrow *does* fail the operation that
  published. Rethrow when the effect is part of the operation's meaning; if it genuinely
  is not, that is a background job, not a notification.
- **The event carries values, not entities.** Ids and scalars only. A tracked entity
  handed to a handler is a second context's problem and a stale read waiting to happen.

**When more than one module can raise the same event, the file stays with the entity it
is about**, and the consuming handler stays beside it. What must never split is the event
and its handler.

## Send, Publish, and who holds `IMediator`

| | `Send` | `Publish` |
|---|---|---|
| Contract | `IRequest<T>` | `INotification` |
| Handlers | exactly one | zero or more |
| Returns | `T` | nothing |
| Means | "do this and give me the result" | "this happened" |
| Missing handler | throws at dispatch | silently no-ops |

**A service holds `IMediator`; a controller never does.** It is a constructor dependency
like any other, assigned to a `private readonly` field:

```csharp
// Orders/Services/OrderService.cs — the owning module reaching two other modules
private readonly IRepositoryWrapper repositoryWrapper;
private readonly IMediator mediator;

private async Task<Guid> ConfirmInternalAsync(Order order, Guid carrierId, DateTimeOffset now, CancellationToken cancellationToken)
{
    OrderValidation.ThrowIfOrderNotPayable(order, now);

    int availableCredit = await mediator.Send(new GetAvailableCreditQuery(carrierId), cancellationToken);
    if (availableCredit < order.CreditRequired)
    {
        throw new BadRequestException(Messages<Order>.NotEnoughLength(nameof(Order.CreditRequired)));
    }

    await mediator.Send(new ReserveCreditCommand(carrierId, order.CreditRequired), cancellationToken);

    OrderConfirmation confirmation = await CreateConfirmationAsync(order, carrierId, now, cancellationToken);

    await mediator.Publish(new OrderConfirmedSucceededEvent(confirmation.Id, order.CreditRequired), cancellationToken);
    await repositoryWrapper.CommitTransactionAsync(cancellationToken);

    return confirmation.Id;
}
```

- **A module both sends and receives.** `Orders` owns envelopes other modules send to it,
  *and* reaches two further modules through envelopes they own. There is no layer here and
  no direction of travel — only "who owns this capability".
- **The envelope is constructed inline at the call site.** `new <X>Command(...)` inside
  `Send`; no local, no builder, no factory.
- **Where the `Publish` sits relative to a transaction is a consequence, not a rule.**
  Because `Publish` runs its handlers on the caller's stack, a `Publish` issued inside an
  open transaction lets a rethrowing handler take the commit down with it. That is what
  you want when the effect is part of what the operation means, and wrong when it is
  not — so it is a decision per operation, not a shape to copy. Where a transaction
  begins and ends is `ef-core-data-access`'s rule, not this skill's.
- A controller that needs `IMediator` is a design error, and the fix is the service
  operation it is missing, not an injection in the controller. Where the rule holds, the
  evidence is plain: no controller in the HTTP project holds `IMediator` at all — only
  services do.

## The cancellation token

```csharp
public async Task<OrderResponse> Handle(ConfirmOrderForCarrierCommand request, CancellationToken cancellationToken)
{
    return await orderService.ConfirmAsync(request.CarrierId, request.OrderId, cancellationToken);
}
```

- **`Handle`'s token has no `= default`** — it is an interface method MediatR always calls
  with a token, unlike a service operation, whose last parameter is
  `CancellationToken cancellationToken = default`.
- **It flows straight through into the service call.** The handler adds nothing to it and
  never drops it.
- **Never pass `CancellationToken.None` at a `Send` or `Publish` site.** The caller has a
  token; substituting `None` means the work continues after the client is gone, and it
  hides the fact from every layer below.
- **The token is never a member of the message.** It is an ambient concern of the
  dispatch, not data the envelope carries.

## Anti-example: a handler that bypasses the service

```csharp
// Orders/Queries/GetAvailableOrderQuery.cs                                   ✗
internal sealed record GetAvailableOrderQuery(string Code, Guid CarrierId) : IRequest<Order>;

internal sealed class GetAvailableOrderHandler : IRequestHandler<GetAvailableOrderQuery, Order>
{
    private readonly IRepositoryWrapper repositoryWrapper;

    public GetAvailableOrderHandler(IRepositoryWrapper repositoryWrapper)
    {
        this.repositoryWrapper = repositoryWrapper;
    }

    public async Task<Order> Handle(GetAvailableOrderQuery request, CancellationToken cancellationToken)
    {
        Order order = await repositoryWrapper
            .Repository<Order>()
            .Find(x => x.Code == request.Code)
            .Include(x => x.Shipment).ThenInclude(x => x!.OrderCarriers)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BadRequestException(Messages<Order>.NotFound());

        OrderValidation.ThrowIfOrderNotPayable(order, DateTimeOffset.UtcNow);

        return order;
    }
}
```

Every individual line is defensible, which is why this shape spreads. Four things are
wrong with the file:

- **The handler is a second service.** It loads, guards and returns — that is an
  operation, and it now exists outside `Services/`, where nothing else in the module will
  find it. The next caller that needs "the available order for this code" either
  duplicates the query or `Send`s a query to obtain data it should have asked the service
  for.
- **It returns an entity across a module boundary.** The two foreign modules that send
  this query receive a tracked `Order` with two navigations included, and each one maps
  it itself. Which navigations are loaded is now part of the contract, invisibly: add a
  `.Include` for one caller and you have changed the payload for all of them, and drop
  one and the other caller gets a lazy-load exception far from here.
- **The guard runs in the wrong place.** `ThrowIfOrderNotPayable` is `Orders`' rule about
  when an order may proceed. Called from here, it protects this one path and not the
  service's own — so the module has two answers to "may this order proceed?" and only one
  of them is in the file people read.
- **The clock is captured inside the handler.** `DateTimeOffset.UtcNow` here is a
  different instant from the one the sending service already computed, so two checks in
  one logical operation are judged against two times.

Corrected — the envelope stays exactly where it is, and everything else moves into the
service that owns it:

```csharp
// Orders/Queries/GetAvailableOrderQuery.cs                                   ✓
internal sealed record GetAvailableOrderQuery(string Code, Guid CarrierId) : IRequest<OrderResponse>;

internal sealed class GetAvailableOrderHandler : IRequestHandler<GetAvailableOrderQuery, OrderResponse>
{
    private readonly IOrderService orderService;

    public GetAvailableOrderHandler(IOrderService orderService)
    {
        this.orderService = orderService;
    }

    public async Task<OrderResponse> Handle(GetAvailableOrderQuery request, CancellationToken cancellationToken)
    {
        return await orderService.GetAvailableAsync(request.Code, request.CarrierId, cancellationToken);
    }
}
```

`IOrderService` gains one documented operation —
`GetAvailableAsync(string code, Guid carrierId, CancellationToken cancellationToken = default)`
— which loads, calls the guard with a single `now`, and projects to `OrderResponse`. The
foreign modules stop mapping an entity they do not own, and the module's rule has one
home again.

**If the correction feels like a lot of moving for a small query, that is the signal to
read, not to skip it:** the amount of behaviour that has to move is exactly the amount of
behaviour that had escaped the module.

## Anti-example: a public, unsealed event pair

```csharp
// Orders/Events/OrderPaidStatusEvent.cs                                      ✗
public record OrderPaidStatusEvent(Guid OrderId, OrderPaymentStatus PaymentStatus) : INotification;

public class UpdateOrderHistoryHandler : INotificationHandler<OrderPaidStatusEvent>
{
    private readonly IRepositoryWrapper repositoryWrapper;
    private readonly IElasticSearchWrapper elasticSearchWrapper;

    // ... 70 lines: loads the order, writes a timestamp, then updates a search document
}
```

One file, three drifts:

- **`public` on both types.** The envelope is now constructible from the HTTP project, so
  a controller can publish it directly, and the visibility rule that made "controllers
  call services" enforceable no longer holds for this event. Nothing has to go wrong for
  this to be a problem; it is the guarantee that is gone.
- **Neither type is `sealed`.** The message invites a derived event with no handler; the
  handler invites an override that changes what the event means for one caller.
- **The handler's name does not match its event.** `UpdateOrderHistoryHandler` handling
  `OrderPaidStatusEvent` is unfindable from the message and, being `public` and unsealed,
  is also the type someone will inherit from. It should be `OrderPaidStatusHandler`.

Under it sits the same bypass as the previous anti-example, at greater scale: two data
stores written directly, seventy lines of behaviour that no service owns.

```csharp
// Orders/Events/OrderPaidStatusEvent.cs                                      ✓
internal sealed record OrderPaidStatusEvent(Guid OrderId, OrderPaymentStatus PaymentStatus) : INotification;

internal sealed class OrderPaidStatusHandler : INotificationHandler<OrderPaidStatusEvent>
{
    private readonly IOrderHistoryService orderHistoryService;

    public OrderPaidStatusHandler(IOrderHistoryService orderHistoryService)
    {
        this.orderHistoryService = orderHistoryService;
    }

    public async Task Handle(OrderPaidStatusEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await orderHistoryService.SetPaymentStatusAsync(notification.OrderId, notification.PaymentStatus, cancellationToken);
        }
        catch (Exception ex)
        {
            LogExtension.Error(nameof(Handle), ex, nameof(OrderPaidStatusHandler));
            throw;
        }
    }
}
```

The branching on status, both writes and the search-document update all move into
`IOrderHistoryService.SetPaymentStatusAsync` — where `OrderHistories`' other operations
already live, and where the next caller will look for them.

**Fix visibility when you touch the file.** Narrowing `public` to `internal` is a
compile-time change with a bounded blast radius: if something outside the assembly was
using it, the build says so immediately, and what it says is that the rule was already
being broken.

## Review checklist

- The envelope file lives in the module that **owns** the capability, under `Commands/`,
  `Queries/` or `Events/` — not in the module that sends it.
- One file holds the message and its handler; there is no separate handlers folder.
- Message and handler are both `internal sealed`.
- The message is a positional record implementing `IRequest<T>` or `INotification`, and
  carries ids and scalars — no entities, no cancellation token.
- The handler's name is the message's name with `Command`/`Query`/`Event` replaced by
  `Handler`.
- The handler's name is its message's name with the kind word replaced. A handler named
  for the work it does instead — `UpdateHistoryHandler` on a `…StatusEvent` — is usually
  a handler that *is* doing the work rather than delegating it.
- The handler's only dependency is a service, and its body is one delegating line —
  no `IRepositoryWrapper`, no `IMapper`, no `IMediator`.
- The handler returns a response type, never an entity.
- Notification handlers wrap the call in `try { … } catch (Exception ex) {
  LogExtension.Error(nameof(Handle), ex, nameof(<Handler>)); throw; }`.
- `Handle`'s `CancellationToken` has no default and is forwarded to the service call.
- Every `Send` / `Publish` site passes the caller's token, never `CancellationToken.None`.
- `IMediator` is injected into services only. No controller holds it.
