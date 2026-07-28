---
name: mediatr-messaging
description: >-
  This skill should be used when in-process messaging runs through MediatR in a
  .NET solution: dispatching with Send or Publish through IMediator, writing an
  INotificationHandler or IRequestHandler, adding an IPipelineBehavior, wiring
  AddMediatR and RegisterServicesFromAssembly, registering an open-generic
  handler, choosing the folder an event or command file belongs in —
  DomainEvents, Commands — naming an event and its handler, or choosing a
  request over a notification. Not for: the envelope record itself, validation
  rules, service call versus message — module-feature; exception flow —
  error-handling; message text — message-keys; unclear ownership —
  choosing-a-dotnet-skill.
---

## Core Principles

### 1. MediatR is in-process messaging, not CQRS

A message is dispatched, handled and awaited inside one process, one request
scope, one call stack. `Send` and `Publish` are method calls with a lookup in
front of them: no broker, no queue, no serialization, no retry, no delivery
guarantee.

**A message buys decoupling, not
durability** — reach for one when a capability must trigger another without
taking a reference to it, never to make work reliable or asynchronous. And **the
request/notification split is not a read/write split**: no separate model, no
separate store, no eventual consistency.

Whether a message is warranted at all — message versus direct service call — is
`module-feature`'s ruling, not this skill's.

### 2. `Publish` fans out; `Send` has exactly one handler

- **`Publish` / `INotification`** — announces something already true. Any number
  of handlers react, including none; nothing is returned.
- **`Send` / `IRequest`** — delegates one job to one owner and may return a
  result. A missing handler fails at dispatch time; a second registration does
  not give you a second handler.

Decide with one question: **would a second handler on this message be a bug or a
feature?** If a feature, it is a notification.

> **Documentation-derived, not corpus-derived.** MediatR's default publisher runs
> notification handlers sequentially and stops at the first exception, so a
> throwing handler can prevent later ones from running. Write notification
> handlers to be independently survivable.

### 3. Dispatch through `IMediator`, from the capability layer

Inject `IMediator`. Dispatch sites belong in services, facades and background
workers — the layers that own behaviour.

Every dispatch site in the corpus follows this, and none sits in a controller.
Treat that as the house default, not an absolute bar: dispatching from a
controller lets the transport decide which internal capabilities run.

### 4. Envelope and handler share one file, in a folder named for the message kind

- `DomainEvents/` holds `INotification` envelopes. `Events/` is the older name
  for the same thing — do not create new ones, and do not read the two as a
  distinction.
- `Commands/` and `Queries/` hold `IRequest` envelopes. **A request-shaped type
  does not belong in the event folder**, however event-like its name sounds: the
  folder is what tells a reader which dispatch semantics apply before they open
  anything.

> This folder rule is the standard going forward, not a description of what is
> there. Existing modules do file requests under `DomainEvents/`; that is the
> thing being corrected, not a precedent.

Colocation is the point: for a notification the handler is often the only
documentation of what publishing causes. The file is named for the envelope.

The **shape of the envelope record itself** — declaration form, accessibility,
what may sit on it — belongs to `module-feature`.

### 5. A handler's name derives from its message, unless the message fans out

For a request, replace the kind suffix with `Handler`: `CreateEntityCommand` →
`CreateEntityHandler`, `SearchEntityQuery` → `SearchEntityHandler`. Do not keep
the suffix and append (`CreateEntityCommandHandler`).

The same applies to a notification **with one handler**: `EntityUpdatedEvent` →
`EntityUpdatedHandler`.

**A notification with several handlers cannot use that form** — the names would
collide. Name each one for what it does (`CacheEntityTokenHandler`,
`RevokeSiblingSessionsHandler`), which is the honest signal that this message has
a fan-out.

### 6. Handler classes are `internal sealed` — *recommendation*

> **Recommendation, not a unanimous convention.** The canonical project runs
> roughly 3-to-1 in favour of this form (a second project in the corpus is
> uniformly `public`); no call site depends on either.

Nothing outside the assembly ever names a handler: it is discovered by scanning
and invoked through its interface. **`internal`** states that, so a handler can be
renamed or replaced without an API-surface question, where `public` advertises a
type that has no callers and invites one to be written. **`sealed`** because
behaviour is composed by adding handlers, not by subclassing one. Handler class
only — the envelope record is `module-feature`'s.

### 7. One assembly scan discovers handlers; open generics need an explicit line

Handler discovery is a scanning concern, not a per-handler one — a new handler
must never require a registration edit. Nothing resolves a handler by name, so
anything implementing the handler contracts is meant to be found; a
hand-maintained list only falls behind.

The corollary: **anything the scan cannot see must be registered deliberately.**
An open-generic handler is the usual case — missed by the scan, failing at
dispatch time rather than at startup. Such a registration is **defined by the
module that owns the handler and invoked from the composition root**, and where
the handler's type parameter is nested inside the message type it also needs a
container that unifies generic arguments (*Patterns*).

## Patterns

### Quick reference

| You are writing | Envelope | Handler contract | Folder | Handler name |
|---|---|---|---|---|
| Something happened; 0..n reactors | `record X : INotification` | `INotificationHandler<X>` | `DomainEvents/` | `<EventName>Handler` |
| …and a second handler exists | — | — | — | descriptive, one per handler |
| Do this one thing, no result | `record X : IRequest` | `IRequestHandler<X>` | `Commands/` | kind suffix → `Handler` |
| Do this one thing, return a result | `record X : IRequest<TResult>` | `IRequestHandler<X, TResult>` | `Commands/` or `Queries/` | kind suffix → `Handler` |

### Notification envelope and handler

```csharp
// Modules/<Module>/DomainEvents/EntityActivatedEvent.cs

public record EntityActivatedEvent(Guid EntityId, DateTimeOffset OccurredAt) : INotification;

internal sealed class EntityActivatedHandler : INotificationHandler<EntityActivatedEvent>
{
    private readonly IEntityHistoryService entityHistoryService;

    public EntityActivatedHandler(IEntityHistoryService entityHistoryService)
        => this.entityHistoryService = entityHistoryService;

    public Task Handle(EntityActivatedEvent notification, CancellationToken cancellationToken)
        => entityHistoryService.RecordAsync(notification.EntityId, notification.OccurredAt, cancellationToken);
}
```

Dispatch from a service, facade or worker:

```csharp
await mediator
    .Publish(new EntityActivatedEvent(entity.Id, DateTimeOffset.UtcNow), cancellationToken)
    .ConfigureAwait(false);
```

`Publish` is fire-and-*wait*: the returned task completes when the handlers have
run, not when the message is queued.

> **From MediatR documentation, not from this codebase.** The default publisher
> runs handlers one after another on the calling thread and stops at the first
> handler that throws, so later handlers never run. Do not rely on a handler
> executing merely because it is registered, and do not let one handler's failure
> become the mechanism that cancels another's work.

### When several handlers take the same notification

The derived name can only belong to one of them — the trigger is the handler
count, not taste.

```csharp
public record EntityStatusChangedEvent(Guid EntityId) : INotification;

internal sealed class IndexEntityStatusHandler : INotificationHandler<EntityStatusChangedEvent> { }

internal sealed class ExpireEntityCacheHandler : INotificationHandler<EntityStatusChangedEvent> { }
```

### Request envelope and handler

```csharp
// Modules/<Module>/Commands/CreateEntityCommand.cs

public record CreateEntityCommand(CreateEntityRequest Request) : IRequest<EntityBaseResponse>;

internal sealed class CreateEntityHandler : IRequestHandler<CreateEntityCommand, EntityBaseResponse>
{
    public Task<EntityBaseResponse> Handle(CreateEntityCommand request, CancellationToken cancellationToken)
        => entityService.CreateAsync(request.Request, cancellationToken);
}
```

```csharp
EntityBaseResponse response = await mediator
    .Send(new CreateEntityCommand(request), cancellationToken)
    .ConfigureAwait(false);
```

Nothing coming back: `record X : IRequest` pairs with `IRequestHandler<X>` and a
plain `Task Handle`.

### Registering handlers: the `AddMediatR` call

One call, at the composition root, scanning the assembly that holds the handlers:

```csharp
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<MessagingAssemblyMarker>());
```

```csharp
// Infrastructure/MessagingAssemblyMarker.cs
namespace Infrastructure;

// Anchors assembly scanning. Deliberately empty; do not add members.
internal sealed class MessagingAssemblyMarker;
```

Prefer this over `typeof(SomeClass).Assembly`, for two reasons:

- **A class name you reach for is rarely unique.** `Startup` is declared dozens
  of times across facades and modules of one assembly, so `typeof(Startup)` binds
  through whatever `using` directives are in the file — and when it later binds
  elsewhere, nothing fails loudly.
- **A missing handler is silent, not loud.** Scanning the wrong assembly produces
  no startup error; a notification with zero handlers is legal and no-ops. The
  symptom surfaces later as work that never happened.

Handlers spread across assemblies go in **one** call —
`RegisterServicesFromAssemblies(...)` — not repeated `AddMediatR` calls. Put it
with the other cross-cutting registrations; buried mid-chain it is invisible.

> **MediatR v12 configuration — from documentation, beyond what this codebase
> exercises.** `MediatRServiceConfiguration` also exposes `AddBehavior` /
> `AddOpenBehavior` (pipeline behaviours), `NotificationPublisher` (how `Publish`
> fans out), and `Lifetime`.
>
> **Leave `Lifetime` alone.** Handlers are transient by default and hold no state
> between messages; the scoped services they inject still resolve from the ambient
> scope. Widening it changes nothing a handler can observe.
>
> From **12.4** a `RegisterGenericHandlers` flag exists (default `false`). Verify it
> against your pinned version — and note it does not replace the registration below
> when the handler's type parameter is nested inside the message type.

### Registering open-generic handlers

A handler written once against a type parameter — one implementation serving every
message of that shape — is not found by assembly scanning. The failure surfaces at
dispatch time, not at startup.

The shape that matters is this one, where the handler's type parameter sits **inside
the message type**:

```csharp
// Modules/<Module>/.../Handlers/ProcessEntityBatchMessage.cs

public record ProcessEntityBatchMessage<TData>(params TData[] Sources) : IRequest
    where TData : class;

public class ProcessEntityBatchHandler<TData>(IEntityService entityService)
    : IRequestHandler<ProcessEntityBatchMessage<TData>>
    where TData : class
{
    public Task Handle(ProcessEntityBatchMessage<TData> request, CancellationToken cancellationToken)
        => entityService.ProcessAsync(request.Sources, cancellationToken);
}
```

**This shape needs a container that unifies generic arguments.** Resolving
`IRequestHandler<ProcessEntityBatchMessage<Entity>>` requires inferring `TData =
Entity` by matching the request against the handler's *implemented interface*. The
built-in container does not do that — it substitutes type arguments positionally,
so it would try to construct `ProcessEntityBatchHandler<ProcessEntityBatchMessage<Entity>>`
and fail. Note that the arity matches and it still fails; arity is not the trap,
indirection is. So the registration goes through a container that does:

```csharp
// Modules/<Module>/Startup.cs — the module declares what it registers
internal static ContainerBuilder AddEntityBatchHandlers(this ContainerBuilder container)
{
    container.RegisterGeneric(typeof(ProcessEntityBatchHandler<>))
        .As(typeof(IRequestHandler<>))
        .InstancePerDependency();

    return container;
}

// composition root: container.AddEntityBatchHandlers();
```

The registration is **defined by the module that owns the handler and invoked from
the composition root** — not next to the `AddMediatR` scan, which would put a
module's internals in the root. `.As(typeof(IRequestHandler<>))` names the
one-argument, no-result contract; a generic handler returning a value targets
`IRequestHandler<,>`.

> The container API above is one library's; what carries across is the division of
> responsibility — module defines, root invokes, open generic against the handler
> contract.

### Pipeline behaviours

> **Entirely documentation-derived. There are no pipeline behaviours in this
> codebase**, so none of this describes a convention. Check first whether the
> concern already has a home — request middleware, the validation layer and the
> exception pipeline all run around this one.

A behaviour wraps every dispatched **request** — not notifications.

```csharp
internal sealed class ExampleBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        => await next().ConfigureAwait(false);
}
```

Register it inside the same `AddMediatR` call:
`cfg.AddOpenBehavior(typeof(ExampleBehavior<,>));`.

`AddOpenBehavior` takes the open generic, `AddBehavior<T>` a closed one. `next()`
must be awaited exactly once — skipping it silently cancels the handler.

## Anti-patterns

### A request type filed in the event folder

```csharp
// DomainEvents/CreateEntityCommand.cs
public record CreateEntityCommand(string Name) : IRequest;
```

`DomainEvents/` is what a reader scans to learn what a module broadcasts, and the
folder is the only thing they see before opening the file. A request filed there
is invisible as work-to-be-done and misreports the module's surface as an
announcement it never makes. This is not a one-off slip — it recurs.

**Instead:** the interface decides the folder, not the name. `IRequest` and
`IRequest<T>` → `Commands/` or `Queries/`; `DomainEvents/` holds `INotification`
only.

### The legacy `Events/` folder name

```
Modules/<Module>/Events/          // old form
Modules/<Module>/DomainEvents/    // convention
```

Both hold the same thing — so every search for a module's events has to be run
twice.

**Instead:** `DomainEvents/` — **never create a new `Events/` folder.** What to do
about the ones that already exist is not settled here; leaving them is not a
defect this skill will flag.

### A descriptive handler name on a single-handler message

```csharp
public record EntityStatusUpdatedEvent(Guid EntityId) : INotification;

internal sealed class UpdateEntityHistoryHandler       // only handler for this event
    : INotificationHandler<EntityStatusUpdatedEvent>
{
}
```

With one handler the derived name is available and free, and a descriptive name
signals *"I am one of several"* — it sends a reader looking for siblings that do
not exist.

**Instead:** `EntityStatusUpdatedHandler`. Derive the name while the message has
one handler, and rename to descriptive names at the moment a second handler is
added — that rename *is* the signal.

### A handler name that appends to the kind suffix instead of replacing it

```csharp
internal sealed class ViewEntityListCommandHandler     // suffix kept and extended
    : IRequestHandler<ViewEntityListCommand>
{
}
```

`Command` is already stated by the type the handler closes over; carrying it into
the class name says it twice, and the two copies drift.

**Instead:** replace the kind suffix — `ViewEntityListHandler`. Substitution, never
addition.

### Log-and-rethrow inside a notification handler

```csharp
try
{
    await entityHistoryService.RecordAsync(notification.EntityId, cancellationToken)
        .ConfigureAwait(false);
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed handling {Event}", nameof(EntityActivatedEvent));
    throw;
}
```

The catch changes nothing: it does not decide, does not recover, does not enrich —
it rethrows the same exception the pipeline was already going to surface — and a
handler's failure is not local to that handler (see the publisher note under
*Patterns*).

**Instead:** let it throw. A notification handler's body is the work, not a
try/catch around the work. If a failure genuinely needs a decision — swallow,
translate, compensate — that decision is exception-flow design and belongs to
**error-handling**.

### Mixed handler accessibility in one folder

```csharp
internal sealed class EntityActivatedHandler : INotificationHandler<EntityActivatedEvent> { }

public class EntityArchivedHandler : INotificationHandler<EntityArchivedEvent> { }
```

Nothing distinguishes these two handlers — the container resolves both the same
way — so the difference in their declarations reads as meaning that is not there.

**Instead:** pick one form per folder and hold it. This skill recommends
`internal sealed` (see *Core Principles*); a folder that is uniformly `public` is
consistent and fine. The defect is the mix, not either form.

## Decision Guide

| Question | Answer |
|---|---|
| Notification or request? | Would a second handler on this message be a feature or a bug? Feature → `INotification` + `Publish`. Bug → `IRequest`/`IRequest<T>` + `Send`. |
| Which folder? | `INotification` → `DomainEvents/`. `IRequest`/`IRequest<T>` → `Commands/`, or `Queries/` when it only reads. The interface decides, not the name. |
| What is the handler called? | Request → replace the kind suffix with `Handler`. Notification with one handler → `<EventName>Handler`. Notification with several → a descriptive name per handler. |
| Envelope and handler in one file? | Yes, named for the envelope — including when several handlers share the notification. |
| Where does it get dispatched? | Through `IMediator`, from a service, facade or worker. Controllers dispatching is against the house default. |
| Scan or register explicitly? | The assembly scan handles ordinary handlers — never edit registration to add one. A handler whose type parameter sits *inside* the message type needs explicit open-generic registration, defined in the owning module and invoked from the composition root. |
| Which container for that? | One that unifies a nested generic argument. The built-in container substitutes positionally and cannot resolve that shape. |
| Handler not resolved at dispatch? | It is open-generic, or the owning module's registration method is never invoked. The indirection is the trap, not the code. |
| Add a pipeline behaviour? | Check first whether request middleware, the validation layer or the exception pipeline already owns the concern — they usually do. See *Patterns*; behaviours are a new capability here, not an existing convention. |
| What shape should the envelope record be? Does it need validation? Should this be a service call instead of a message at all? | **module-feature** |
| Catch, wrap, translate or swallow an exception; which exception type; what the client receives | **error-handling** |
| The text of any message a user sees | **message-keys** |
| Still unclear which skill owns this | **choosing-a-dotnet-skill** |
