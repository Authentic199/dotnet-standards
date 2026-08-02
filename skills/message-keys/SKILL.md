---
name: message-keys
description: >-
  This skill should be used when writing or reviewing any user-facing message
  string in a .NET service — a validator's WithMessage(...), a success message,
  or the message a thrown exception carries — or when the work touches
  Messages<T>, MessagesType, the legacy WithMessage(MessagesType) extension,
  [MessageDisplay] on a request class, a key shaped Mes.Module.Action.Property,
  or a hardcoded message literal. Also when picking the key for a new action or
  a cross-entity existence check. Not for: exception flow, middleware envelope —
  error-handling; endpoints, wrappers, ProducesResponseType — api-surface;
  validator structure, services, MediatR envelopes — module-feature; JWT
  schemes, policies, [HasPermission] — auth-and-security.
---

# Message Keys

## Overview

Every user-facing string is a **key**, never a sentence: `Mes.Widget.Required.Name`.
The key is the contract between the API and whatever renders text to a human —
translation catalogue, client, log reader. Wording lives outside the codebase;
the code's only job is to emit a stable, well-formed key.

## Core Principles

1. **Every key is generated.** Never write a message literal — not prose
   (`"Widget not found"`) and not a hand-typed key (`"Mes.Widget.NotFound"`).
   The second is worse, because it looks correct while staying invisible to the
   catalogue and drifting silently when a class is renamed.
2. **The enum is closed; the action family grows only on proven reuse.** Never
   hardcode a key to escape the family, and never add members to `MessagesType`.
   An action the family does not name starts as `Messages<T>.Action("Approve", true)`
   — same grammar, carrying the `.Successfully`/`.Failed` suffix. The `true` is
   not redundant: the single-argument `Action("Extend")` binds a different
   overload and emits a bare key with no suffix. When the same action recurs
   across modules — Approve, Reject and Cancel are the typical cases — it is
   permitted to promote it to a dedicated helper on the messages facade, so that
   every module emits the identical key shape. Promotion answers proven
   recurrence, never anticipation.
3. **Key anatomy is `Mes.{Module}.{Rest}`.** The module segment comes from
   `[MessageDisplay(nameof(Entity))]` on `T`, falling back to `typeof(T).Name`
   when the attribute is absent. With `T` an entity the fallback *is* the
   intended path — `Messages<Order>` yields `Mes.Order` with no attribute
   anywhere. It is a trap only when `T` is a request, where the fallback leaks
   the transport type into the key.
4. **`T` is the entity — in validator messages and outcome messages alike.**
   `Messages<Order>.Required(x => x.Code)` is the form, written inside
   `OrderRequestValidator` and everywhere else. The selector is an expression
   over `Messages<T>`'s own `T`, chosen freely at the call site — it does **not**
   have to be the type being validated, and a validator returns a plain string
   from `WithMessage(...)` either way. Two shifts, and only two:
   - **A rule checking a *different* entity speaks as that entity, without a
     selector** — `Messages<Category>.NotFound()`.
   - **A request with no owning entity** — a Facades-tier request such as a media
     upload — has nothing to type against, so it types to itself and carries
     `[MessageDisplay(nameof(Media))]` to keep the transport name out of the key.
     This is the *only* case that needs the attribute; on a module request whose
     messages are entity-typed the attribute never executes.
5. **One style for validator messages.** Write `Messages<Entity>.X(x => x.Prop)`.
   The `WithMessage(MessagesType.X)` extension infers `T` and the property name
   and emits an identical key, but it is legacy: recognise it when reading,
   never write it new. Two spellings of one key make the key space unsearchable.

## Which form where

| Situation | Form |
|---|---|
| Validator rule on the request's own property | `Messages<Entity>.X(x => x.Prop)` |
| Rule about a property the entity does not have | `Messages<Entity>.X(nameof(OtherEntity.Prop))` |
| Validator on a Facades request with no entity behind it | `Messages<TRequest>.X(x => x.Prop)` + `[MessageDisplay]` on that request |
| Checking that a *different* entity exists | `Messages<OtherEntity>.NotFound()` |
| Success message on a controller action | `Messages<Entity>.Create()` / `.Update()` / `.Delete()` / `.Search()` / `.Detail()` / `.List()` |
| Failure of an operation, thrown from a service | the same call with `success: false` → `.Failed` |
| An action with no dedicated helper | `Messages<T>.Action("Approve", true)` — may be promoted to a facade helper once it recurs across modules |

Overload coverage across the enum is not uniform — some members lack the
parameterless form, others the selector form. When a call does not compile,
consult `references/key-grammar.md`. A missing shape is not an invitation to add
one: error-state helpers are driven by the closed enum, and only action helpers
grow, and only on recurrence.

## Patterns

Request, attribute and validator together:

```csharp
public class CreateWidgetRequest
{
    public string? Name { get; set; }

    public Guid? CategoryId { get; set; }
}

public class CreateWidgetValidator : AbstractValidator<CreateWidgetRequest>
{
    public CreateWidgetValidator(IRepositoryWrapper repositoryWrapper)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(Messages<Widget>.Required(x => x.Name))
            .MaximumLength(256).WithMessage(Messages<Widget>.OverLength(x => x.Name));

        // Existence of another entity is that entity's message, not this one's.
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage(Messages<Widget>.Required(x => x.CategoryId))
            .Must(id => repositoryWrapper.IsExistCategory(id!.Value))
            .WithMessage(Messages<Category>.NotFound());
    }
}
```

Keys emitted: `Mes.Widget.Required.Name`, `Mes.Widget.OverLength.Name`,
`Mes.Widget.Required.CategoryId`, `Mes.Category.NotFound`. No `[MessageDisplay]`
anywhere — `Messages<Widget>` already yields `Mes.Widget` from the type name.
Note the third: the property segment stays on the field the client must
highlight, and only the existence check speaks as the other entity.

**The request with no entity behind it** — Facades tier, nothing to type
against — is the one place the attribute earns its keep:

```csharp
[MessageDisplay(nameof(Media))]
public class MediaUploadRequest
{
    public IFormFile? File { get; set; }
}

// inside its validator — T is the request, because no entity models this
Messages<MediaUploadRequest>.Required(x => x.File);      // Mes.Media.Required.File
```

Without the attribute that key would read `Mes.MediaUploadRequest.Required.File`.

A property that lives on a related entity and has no counterpart on `T` is named
with the string overload, keeping `nameof` so a rename still breaks the build:

```csharp
Messages<Widget>.Invalid(nameof(WidgetPart.PartId));   // Mes.Widget.Invalid.PartId
```

Outcome messages are entity-typed. The success key is the controller's second
wrapper argument; the failure key is thrown from the service:

```csharp
// controller
=> OkWrapper(await widgetService.CreateAsync(request, cancellationToken), Messages<Widget>.Create());   // Mes.Widget.Create.Successfully

// service, when the operation could not be completed
throw new InternalServerException(Messages<Widget>.Create(false));                                       // Mes.Widget.Create.Failed
```

> **Corrected 2026-08-02, from field evidence.** This section previously called
> the entity-typed validator message *superseded* by a request-typed call plus
> `[MessageDisplay]`, and told readers not to copy it into new validators. That
> was backwards: `Messages<Widget>.Required(x => x.Name)` is the house form, and
> a session that trusted the old text rewrote correct examples in
> `module-feature` into wrong ones before the error was caught. The claim rested
> on the premise that *a property selector can only compile against the type
> being validated* — false. The selector is an expression over `Messages<T>`'s
> own `T`, picked at the call site; `WithMessage` takes the resulting string and
> never sees the selector. Request-typed calls are the narrow Facades exception
> above, not the rule.

## Anti-patterns

- **A request that appears inside `Messages<>` without `[MessageDisplay]`.** The
  module segment falls back to the type name and the transport type leaks into
  every key — `Mes.CreateWidgetRequest.Required.Name`. Nothing fails; the key is
  simply wrong, differs from every other key for the same entity, and changes
  again the day someone renames the class. **Only a request that is itself the
  `T`** carries the attribute — the Facades-tier case. Putting it on a module
  request whose messages are entity-typed is harmless but dead: nothing ever
  reads it.
- **A hardcoded message or key literal** — bypasses the grammar, unknown to the
  catalogue.
- **A new `WithMessage(MessagesType.X)` call** — legacy form (Principle 5).
- **Growing the generator for a one-off** — a dedicated helper for an action only
  one module performs, or a new enum member for a one-off state, converts a
  bounded key space into an open one. Promotion needs recurrence.

## Going deeper

Read `references/key-grammar.md` when you need the exact emitted string for a
call, when the overload you want does not compile, or when you are composing
anything beyond the everyday forms above: the full success/action overload list
(including `Detail()`/`List()` emitting concatenated `ViewDetail`/`ViewList`, and
the Pascalized-concatenation `Create("part")` variants), the 15-member
`MessagesType` matrix showing which of the three call shapes each member actually
compiles with, and the mechanism behind `GetMessageBase()`, the attribute
fallback and the legacy extension's plumbing.
