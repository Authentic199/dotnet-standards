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
   when the attribute is absent. That fallback is a trap, not a feature.
4. **Two generics, two jobs — requests type validator messages, entities type
   outcome messages.** A property selector can only compile against the type
   being validated, so a validator message must be typed to the request; the
   module segment must still read as the entity, so the request carries
   `[MessageDisplay]`. A success or failure message has no selector and no
   reason to name the request — it is typed to the entity, whose own type name
   is already the module.
5. **One style for validator messages.** Write `Messages<TRequest>.X(x => x.Prop)`.
   The `WithMessage(MessagesType.X)` extension infers `T` and the property name
   and emits an identical key, but it is legacy: recognise it when reading,
   never write it new. Two spellings of one key make the key space unsearchable.

## Which form where

| Situation | Form |
|---|---|
| Validator rule on the request's own property | `Messages<TRequest>.X(x => x.Prop)` |
| Rule about a property that is not on the request | `Messages<TRequest>.X(nameof(OtherEntity.Prop))` |
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
[MessageDisplay(nameof(Widget))]
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
            .NotEmpty().WithMessage(Messages<CreateWidgetRequest>.Required(x => x.Name))
            .MaximumLength(256).WithMessage(Messages<CreateWidgetRequest>.OverLength(x => x.Name));

        // Existence of another entity is that entity's message, not the request's.
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage(Messages<CreateWidgetRequest>.Required(x => x.CategoryId))
            .Must(id => repositoryWrapper.IsExistCategory(id!.Value))
            .WithMessage(Messages<Category>.NotFound());
    }
}
```

Keys emitted: `Mes.Widget.Required.Name`, `Mes.Widget.OverLength.Name`,
`Mes.Widget.Required.CategoryId`, `Mes.Category.NotFound`. Note the third — the
property segment stays on the request's own field, because that is the field the
client must highlight; only the existence check speaks as the other entity.

A property that lives on a related entity and has no counterpart on the request
is named with the string overload, keeping `nameof` so a rename still breaks the
build:

```csharp
Messages<CreateWidgetRequest>.Invalid(nameof(WidgetPart.PartId));   // Mes.Widget.Invalid.PartId
```

Outcome messages are entity-typed. The success key is the controller's second
wrapper argument; the failure key is thrown from the service:

```csharp
// controller
=> OkWrapper(await widgetService.CreateAsync(request, cancellationToken), Messages<Widget>.Create());   // Mes.Widget.Create.Successfully

// service, when the operation could not be completed
throw new InternalServerException(Messages<Widget>.Create(false));                                       // Mes.Widget.Create.Failed
```

Older code types the helper to the entity when validating a request's own
properties — `Messages<Widget>.Required(x => x.Name)`. It emits the same key, but
is **superseded** by the request-typed call plus `[MessageDisplay]`. Do not copy
it into new validators.

## Anti-patterns

- **A request class without `[MessageDisplay]`.** The module segment falls back
  to the type name and the transport type leaks into every key —
  `Mes.CreateWidgetRequest.Required.Name`. Nothing fails; the key is simply
  wrong, differs from every other key for the same entity, and changes again the
  day someone renames the class. Every request class that appears inside
  `Messages<>` carries the attribute.
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
