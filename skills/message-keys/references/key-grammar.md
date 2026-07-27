# Message Key Grammar — Reference

Lookup for the exact key a call emits. Every helper builds on the same base,
`Mes.{M}.`, where `{M}` is the `[MessageDisplay]` name on `T`, or `typeof(T).Name`
when the attribute is absent. Tables below show only what follows that base.

| Constant | Value |
|---|---|
| Prefix | `Mes` |
| Delimiter | `.` |
| Success suffix | `Successfully` |
| Fail suffix | `Failed` |

## Success / action helpers

All take `bool success = true`; pass `false` for the failure key.

| Call | Emitted key |
|---|---|
| `Create()` | `Mes.{M}.Create.Successfully` |
| `Create(success: false)` | `Mes.{M}.Create.Failed` |
| `Create("part")` | `Mes.{M}.CreatePart.Successfully` |
| `Create(x => x.PartId)` | `Mes.{M}.CreatePartId.Successfully` |
| `Update()` / `Update("part")` / `Update(x => x.PartId)` | as `Create`, with `Update` |
| `Delete()` / `Delete("part")` | as `Create`, with `Delete` |
| `Import()` / `Import("part")` | as `Create`, with `Import` |
| `Search()` | `Mes.{M}.Search.Successfully` |
| `SendMail()` | `Mes.{M}.SendMail.Successfully` |
| `View("summary")` | `Mes.{M}.ViewSummary.Successfully` |
| `Detail()` | `Mes.{M}.ViewDetail.Successfully` |
| `List()` | `Mes.{M}.ViewList.Successfully` |

Which argument shapes exist per helper — do not assume symmetry:

| Helper | parameterless | `string other` | property selector |
|---|---|---|---|
| `Create` | yes | yes | yes |
| `Update` | yes | yes | yes |
| `Delete` | yes | yes | — |
| `Import` | yes | yes | — |
| `Search` | yes | — | — |
| `SendMail` | yes | — | — |
| `View` | — (suffix required) | yes | — |
| `Detail` / `List` | yes | — | — |

### Traps in this family

**`Detail()` and `List()` are not their own segments.** Both delegate to `View`,
and the result is *concatenated* — `ViewDetail`, `ViewList`, with no delimiter.
Searching a catalogue for `Mes.{M}.Detail` finds nothing.

**String and selector arguments are concatenated, not delimited.** The argument is
Pascalized and glued to the action: `Create("part")` → `CreatePart`, never
`Create.Part`. A selector behaves the same way, using the property's declared
name: `Create(x => x.PartId)` → `CreatePartId`.

**`View` has no parameterless form.** Use `Detail()`, `List()`, or an explicit suffix.

## The `Action` family

Four public overloads — the extension point for actions the named helpers do not cover.

| Call | Emitted key |
|---|---|
| `Action("Approve")` | `Mes.{M}.Approve` |
| `Action("Approve", true)` | `Mes.{M}.Approve.Successfully` |
| `Action("Approve", false)` | `Mes.{M}.Approve.Failed` |
| `Action(MessagesType.NotFound)` | `Mes.{M}.NotFound` |
| `Action(MessagesType.NotFound, "PartId")` | `Mes.{M}.NotFound.PartId` |
| `Action(MessagesType.NotFound, null)` | `Mes.{M}.NotFound` |

**Trap: `success` has no default on `Action(string, bool)`.** Unlike every helper
above, the boolean is not optional here — a separate one-argument overload handles
the bare case. So `Action("Approve")` and `Action("Approve", true)` both compile
and emit **different keys**. Choose deliberately: a bare key for a state or event,
a suffixed pair for an operation that can succeed or fail.

`Action(MessagesType, string?)` appends the property segment only when the string
is non-empty; `null` or `""` yields the bare enum key.

An action the named helpers do not cover starts life as `Action("Approve", true)`.
Once the same action recurs across modules it may be promoted to a dedicated
helper on the facade — permitted, not required (body, Principle 2). Hardcoding
the key remains forbidden either way.

## `MessagesType` — the 15 members

Closed enum. Do not add members. Coverage across the three call shapes is **not**
uniform; this table is the authority, and the compiler is the final one.
`(sel)` appends `.{Property}` from a selector, `(str)` from a string.

| Member | `X()` | `X(sel)` | `X(str)` |
|---|---|---|---|
| `NotAllowed` | yes | yes | yes |
| `Blocked` | yes | — | — |
| `WasUsed` | yes | — | yes |
| `NotFound` | yes | yes | yes |
| `Repeated` | — | yes | yes |
| `Invalid` | — | yes | yes |
| `MustBeEmpty` | — | yes | yes |
| `Required` | — | yes | yes |
| `OverLength` | — | yes | yes |
| `NotEnoughLength` | — | yes | yes |
| `NotWhiteSpace` | — | yes | yes |
| `NotSpecialCharacter` | — | yes | yes |
| `AlreadyExist` | — | yes | yes |
| `Expired` | yes | yes | yes |
| `NotAvailable` | yes | yes | yes |

Notes on the irregular rows:

- **`Blocked` is parameterless-only** — no property can be attached to it.
- **`WasUsed` has no selector overload**, only the parameterless and string forms.
- **The nine validation-flavoured members have no parameterless form.** A property
  segment is mandatory, and that is precisely what makes them field-level errors.

A missing shape is final. Error-state helpers are driven by the closed enum, so
the absence of an overload is a design statement, not a gap to route around.

| Call | Emitted key |
|---|---|
| `NotFound()` | `Mes.{M}.NotFound` |
| `Required(x => x.Name)` | `Mes.{M}.Required.Name` |
| `Invalid(nameof(WidgetPart.PartId))` | `Mes.{M}.Invalid.PartId` |

The string overload has exactly one job: naming a property that does not exist on
`T`. Keep `nameof` so a rename breaks the build rather than the catalogue.

## Mechanism

**Module resolution.** `GetMessageBase()` reads `MessageDisplayAttribute` off
`typeof(T)` by reflection and uses its `Name`; with no attribute it falls back to
`typeof(T).Name`. The attribute is class-targeted, so it sits equally well on a
request type or an entity type. Nothing but the base inserts a leading delimiter,
which is why the concatenation cases above look the way they do.

**Selector resolution.** The selector overloads pull the `MemberInfo` out of the
expression and append its `Name` — the segment is the property's declared name,
not the lambda parameter's, so refactoring the property renames the key.

**Legacy validator extension.** An extension on FluentValidation's rule builder
accepts a bare `MessagesType`:

```csharp
// LEGACY — recognise in existing code; write the explicit lambda form instead.
RuleFor(x => x.Name).NotEmpty().WithMessage(MessagesType.Required);
```

It reads FluentValidation's configured `PropertyName` for the rule and calls the
public `Action(MessagesType, string?)` with `T` bound to the validated type. The
emitted key is therefore identical to the explicit lambda form — the difference is
expressive, not behavioural: the extension fixes `T` to the request and the segment
to the rule's own property, so it cannot express a cross-entity message or a
property that is not the rule's subject (body, Principle 5).
