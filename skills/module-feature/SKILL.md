---
name: module-feature
description: >-
  This skill should be used when writing a feature in a .NET module: a service pairing interface/implementation behind IScopedService,
  suffix partials under Services/, its request/response types,
  FluentValidation rules, IsExist predicates, ThrowIf guards in
  <X>Validation.cs, a thin MediatR command/query/event envelope, or service call versus message. Not for: placement —
  facade-module-architecture; DbContext, entities, migrations —
  ef-core-data-access; routes, endpoints, DTO chains — api-surface; mapping
  mechanics — automapper-mapping; messaging pipeline — mediatr-messaging;
  Redis — distributed-caching; Elasticsearch — elasticsearch-search; Hangfire
  jobs — background-worker; exception middleware — error-handling; Messages<T>
  text — message-keys; Excel parsing, imports — excel-miniexcel; reusable rule methods, existence-check extensions — common-extensions.
---

## Overview

**The module's service is the only entry point to the capability** — a
controller, a message handler or another module's service calls it, and nothing
reaches past it into the repository, the mapper or the entity. **Every other
file in the module is a contract around that service**: requests say what a
caller may ask, responses say what it gets back, `Validations/` says when the
operation may proceed, and a message envelope says how a distant caller triggers
it — each exists so the service can own the behaviour without leaking it.

This skill answers what goes **in** a feature's files; its placement sibling,
`facade-module-architecture`, answers where the files go. Answer feature-writing
questions from this file alone; open a `references/` file only when the
condition closing its section holds.

### Write order

| You are adding | The file you write | Section |
|---|---|---|
| an operation on an existing capability | `Services/<Name>Service.cs` | *The service file* |
| a new input shape | `Requests/<X>Request.cs` | *Requests and responses* |
| a new output shape | `Responses/<X>Response.cs` | *Requests and responses* |
| a check that must run before the work | the request's validator, or `Validations/<X>Validation.cs` | *Where a validation rule lives* |
| an operation another module must trigger | an envelope in `Commands/`, `Queries/` or `Events/` | *Call the service, or send a message?* |

## The service file

A feature's behaviour lives in **one file** — `Services/<Name>Service.cs`,
declaring the public interface and then the class that implements it.

```csharp
public interface IOrderService : IScopedService
{
    /// <summary>Create an order and return it.</summary>
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>Read one order.</summary>
    Task<OrderResponse> GetAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public class OrderService : IOrderService
{
    private readonly IRepositoryWrapper repositoryWrapper;
    private readonly IMapper mapper;

    public OrderService(IRepositoryWrapper repositoryWrapper, IMapper mapper) { /* assign every field */ }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (await repositoryWrapper.Repository<Order>().AnyAsync(x => x.Code == request.Code, cancellationToken))
        {
            throw new BadRequestException(Messages<Order>.AlreadyExist(x => x.Code));
        }

        Order order = mapper.Map<Order>(request);
        await repositoryWrapper.Repository<Order>().AddAsync(order, cancellationToken);

        return await GetAsync(order.Id, cancellationToken);
    }
}
```

- **The lifetime marker goes on the interface** — the scan binds `IOrderService` →
  `OrderService` from it, so a marker on the class registers nothing. The interface
  is `public` and each of its operations carries an XML `<summary>`, written in
  English — the interface is the feature's table of contents, and a doc comment
  is not a task tracker. The class may be `public` or `internal`.
- **One constructor takes every dependency** and assigns every `private readonly`
  field. `IOptions<T>` is unwrapped to `.Value` there, so the field holds the
  settings type and no method ever sees the wrapper.
- **`CancellationToken cancellationToken = default` is the last parameter of every
  operation**, on the interface and the implementation, and is forwarded to every
  awaited call. A service without it is drift — add it when you touch the file.
- **Every operation returns a response type, never an entity** — `Task<OrderResponse>`,
  `Task<PaginationResponse<OrderResponse>>`, never `Task<Order>`. A write returns by
  re-reading through the projection, not by returning the tracked instance.
- **A method owns its guard clauses, its write and its projection.** It throws
  `BadRequestException` with a `Messages<T>` message before touching state, writes,
  then projects. Owning all three is what keeps a service one file: move guards or
  projection into a helper class and the next reader must assemble the operation
  from three files to see what it does. (A precondition guard may delegate to the
  module's static validation type — see *Where a validation rule lives*.)
- **A service never normalizes an input string — no `Trim()`, no `ToLower()`, no
  `Replace` on a request property.** The guard above compares `request.Code` as it
  arrived, and the mapper stores the same value. The moment a guard trims and the
  write does not, the uniqueness check answers about one string while the row holds
  another — and the usual repair, an assignment like `request.Name = request.Name!.Trim()`
  before the map, only spreads the same call to a second site. Whitespace that must
  not arrive is a **validator rule** (`.NotWhiteSpace()`, `.NotEmpty()`), rejected at
  the boundary with a `Messages<T>` message, not silently swallowed mid-operation.
  `Trim()` is a *parsing* call: it belongs where a string was just split or sliced —
  a header token, a comma-separated list — and nowhere else in a module.

**Inside the class the order is fixed: fields, the constructor, then members.**
A static member may sit above the non-private methods; it never sits above the
constructor. The constructor is the type's signature — its parameters say what
the type needs — and burying it under other members makes every reader scroll
for the one declaration that explains the class.

## When a service outgrows one file

Split it into **partial parts of the same type, named by suffix** —
`<Name>Service.<Role>.cs`, all in `Services/`, all in the same namespace. The
suffix-less file stays the core part.

| Part | Declares | Never redeclares |
|---|---|---|
| `OrderService.cs` — core | `public partial interface IOrderService : IScopedService`, `public partial class OrderService : IOrderService`, every field, the one constructor | — |
| `OrderService.<Role>.cs` — public operations | `public partial interface IOrderService` and `public partial class OrderService`, **both with no base list** | the marker, `: IOrderService`, fields, the constructor |
| `OrderService.Private.cs` — helpers | `public partial class OrderService` only — **no partial interface** | anything the core part declares |

**The core part is the only one that carries a base list.** Repeating
`: IScopedService` or `: IOrderService` in a second part still compiles, which is
exactly why it is a review finding: the lifetime and the contract then have no
single home. The prefix form `Checkout.OrderService.cs` is wrong for the same
reason — it scatters one type across the alphabet.

A `<Role>` names a group of operations a caller can ask for (`Checkout`,
`Fulfillment`, `Reporting`, `Webhook`) or is the literal `Private`. A part named
after a layer — `Helper`, `Logic`, `Extensions` — is a dumping ground with the
`partial` keyword on it.

A `<Role>` may also be **another module's concept**: when this module's surface
grows a group of operations composing foreign data — an order's shipments —
they are `OrderService.Shipments.cs`, never a two-module `OrderShipmentService`.
Such a part follows *Call the service, or send a message?* to the letter: its
only reach into the foreign module is a `Send` of that module's envelope, and
the foreign logic stays in the foreign service. The controller face of the same
rule is `api-surface`'s — there is no two-module controller either.

**`Services/` holds services and nothing else** — no subfolders, and no file whose
name is not `<Name>Service…`.

| "But…" | Where it actually goes |
|---|---|
| "it keeps the service small" | a suffix part — `OrderService.<Role>.cs` |
| "it is pure, so it is not a service" | the entity the rule is about |
| "it is only a computed value" | the module's `Expressions/` |
| "it is only a mapper / a response builder" | the mapping profile beside the class it maps |
| "it is a reusable mechanism" | the technical axis — a facade |
| "it is only a bag of records / a settings class" | `Responses/` or the module's `Settings/` |

**Read `references/service-growth.md` when** a service no longer fits one file,
when you are naming a new service part, or when you are reviewing a `Services/`
folder that contains a file whose name is not `<Name>Service…`.

## Requests and responses

**A class and everything that travels with it live in one file.** A request file
declares the request, its `AbstractValidator<T>` and — when the request maps to an
entity — its `Profile`; a response file declares the response, then its `Profile`
below it. **There is no `Mappings/` folder**: a projection kept away from the contract
it projects drifts from it in silence. Unlike `Services/`, both folders may grow theme
subfolders (`Requests/<Theme>/`) once a family warrants them.

```csharp
// Requests/Orders/OrderRequest.cs
public class OrderRequest
{
    public string? Name { get; set; }

    public Guid? CategoryId { get; set; }
}

public class OrderRequestValidator : AbstractValidator<OrderRequest>
{
    public OrderRequestValidator(IRepositoryWrapper repositoryWrapper, IActionAccessorService actionAccessorService)
    {
        string? action = actionAccessorService.GetAction();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(Messages<Order>.Required(x => x.Name))
            .MaximumLength(255).WithMessage(Messages<Order>.OverLength(x => x.Name));

        RuleFor(x => x.CategoryId)
            .NotEmpty().When(_ => action == "Create", ApplyConditionTo.CurrentValidator)
            .WithMessage(Messages<Order>.Required(x => x.CategoryId))
            .Must(id => repositoryWrapper.IsExistCategory(id!.Value))
            .WithMessage(Messages<Category>.NotFound());
    }
}

public class OrderRequestMapping : Profile
{
    public OrderRequestMapping()
    {
        CreateMap<OrderRequest, Order>();
    }
}
```

- **The validator takes what it needs through its constructor** — the repository
  abstraction for a rule that must ask the database, an action accessor for a rule that
  binds to one action only. Every message comes from `Messages<T>`, and `T` is the
  **entity**: the selector must name a member the entity has, and a request-only member
  takes the `string` overload instead. **When the rule checks a *different* entity, `T`
  is that entity and the call takes no selector** — `Messages<Category>.NotFound()`, not
  `Messages<Order>.NotFound(x => x.CategoryId)`. The surrounding `Required()` still
  speaks as the owning entity, so the client keeps the field to highlight.
- **A response that projects an entity derives `BaseEntity`** — `Id` and `CreatedAt`
  arrive by inheritance and are never redeclared. A summary, a bulk result or a
  third-party payload projects no entity and is a plain class. Tiers are named by
  suffix and each derives the one above:
  `<X>BaseResponse` ← `<X>DefaultResponse` ← `<X>Response` ← `<X>DetailResponse`.
- **A computed member with business meaning is projected from the module's
  `Expressions/` via `MapFrom`**, never re-derived inline; inline `ForMember` is shape
  glue only — hopping a link table, wrapping a stored file key. Mapping mechanics
  beyond this belong to the `automapper-mapping` skill.
- **A derived type declares its own `Profile`, and its validator reuses the base's
  rules** — `Include(new OrderRequestValidator(...))`, never a restatement. A base
  request declares a `Profile` only when its map is customized, and then it ends
  with `.IncludeAllDerived()` — that is what carries the customization down (the
  DTO chain law lives with `api-surface`).

**Read `references/request-response-families.md` when** the request or response you
are writing derives from an existing one, when a third variant of the same shape
appears, or when a response needs a computed value it does not store.

## Where a validation rule lives

Four kinds of check, four homes — each recognizable before you write it:

| The check | Where it lives |
|---|---|
| shape, range, format, enum, an id collection | inline in the request's validator |
| must read the database to answer — uniqueness, existence, a conflict | a `bool` predicate in the module's `Validations/<X>Validation.cs`, called from a `.Must(...)` rule |
| a precondition a service or a handler must throw on | a `ThrowIf…` guard in that **same** `<X>Validation.cs` |
| really a value the query computes | the module's `Expressions/` |

**`<X>Validation.cs` is one file serving two callers.** A validator *asks*: it calls a
predicate extension on the repository abstraction and turns `false` into its own
message. A service or a handler *demands*: it calls a `ThrowIf…` guard that throws
`BadRequestException` itself and leaves the caller nothing to decide. Neither shape is
a fallback for the other, and the boundary is symmetric — **a guard never returns
`bool`; a predicate never throws.** The file is named for the entity, with no prefix.

**The line between the middle two rows is what the request can answer about itself.**
Anything the request carries is a validator rule. A guard is for state the request
cannot know — an entity it names only by id, a status that can change between the
validation and the write. A service that re-checks a validator rule is duplicating a
rule that has already run.

**Read `references/validation-rules.md` when** a check needs to read the database, when
a service or handler must throw before doing work, or when you are creating or
extending a module's `<X>Validation.cs`.

## Call the service, or send a message?

**Inside the module and from the HTTP surface, call the service interface directly;
reach another module's capability by sending a message.** The dispatcher is what keeps
each service the owner of its capability: a service that needs a foreign one names a
message, never the foreign service interface.

| You need | Do this |
|---|---|
| a capability of the module you are in | call the service interface — a controller injects it, a service calls its own method |
| a capability another module owns | inject `IMediator`, `Send` that module's envelope |
| an after-the-fact effect somewhere else | `Publish` a notification and move on |

**An envelope is one file: a message, then its handler.** The message is a record carrying
its parameters as positional members and implementing `IRequest<TResponse>`; below it the
handler implements `IRequestHandler<TMessage, TResponse>`, injects the service of the
module that **owns** the capability, and forwards to it. The file lives in the owning
module's `Commands/` or `Queries/`, never in the module that sends — named verb-first with
a `Command` or `Query` suffix, the handler that same name with the suffix replaced.

**An after-the-fact effect is a notification instead** — the same file shape with
`INotification` and `INotificationHandler<T>`, no return value, dispatched with `Publish`.
An event is named for what happened, and the handler beside it injects the *consuming*
module's service, so the bridge between two modules is one readable file.

**This is in-process messaging, not CQRS.** No write model, no read model, no separate
stores — every envelope ends in one call to one service method against the same database.
`Commands/` and `Queries/` are not a write/read split either: an operation that reads a
value and then clears it belongs in `Commands/`, because it changes state.

**The one absolute: every handler delegates to the module's service.** A handler that
injects the repository, queries it and hands back an entity has moved part of one module's
behaviour into a file another module reads through — the service stops being the module's
whole surface, and an entity crosses a boundary that responses exist to hold. Guards,
projection and transactions stay in the service; the handler is a doorway, and its body is
one line.

**Envelopes are `internal sealed`.** The HTTP project is a separate assembly, so an
`internal` message is invisible to controllers — a controller *cannot* `Send` one, which is
what makes the direct-call rule enforceable rather than stylistic. Dispatch mechanics,
registration and pipeline behaviours belong to the `mediatr-messaging` sibling; what a
handler's service does with a mapper belongs to `automapper-mapping`.

**Read `references/mediatr-envelopes.md` when** you are adding a file under a module's
`Commands/`, `Queries/` or `Events/` folder, or when a service needs a capability that
belongs to a different module.

## Not this skill

Which project or folder a file belongs in, and how a module is wired →
`facade-module-architecture`. Entities, `DbContext`, migrations and queries →
`ef-core-data-access`. Routes, endpoint conventions, OpenAPI and the DTO
base-class chain law → `api-surface`. Converters, resolvers and `ProjectTo`
internals → `automapper-mapping`. Pipeline behaviours and the bus itself →
`mediatr-messaging`. Cache keys and invalidation → `distributed-caching`. Search
indexes and `Elk`-prefixed documents → `elasticsearch-search`. Recurring and
queued jobs → `background-worker`. The exception middleware and result wrappers →
`error-handling`. The text behind `Messages<T>` → `message-keys`.
