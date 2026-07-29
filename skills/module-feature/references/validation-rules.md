# Validation rules: predicates and guards

- [One file, two callers](#one-file-two-callers)
- [Naming law](#naming-law)
- [Shape A: predicates a validator asks](#shape-a-predicates-a-validator-asks)
  - [Uniqueness and the `exceptId` parameter](#uniqueness-and-the-exceptid-parameter)
  - [Existence: one row, or a whole collection](#existence-one-row-or-a-whole-collection)
  - [A conflict predicate with optional inputs](#a-conflict-predicate-with-optional-inputs)
- [Shape B: guards a service or handler demands](#shape-b-guards-a-service-or-handler-demands)
- [The message a guard chooses](#the-message-a-guard-chooses)
- [Conditional and cross-field rules](#conditional-and-cross-field-rules)
- [Anti-example: a prefixed validation file](#anti-example-a-prefixed-validation-file)
- [Review checklist](#review-checklist)

## One file, two callers

```
Orders/
├── Validations/
│   └── OrderValidation.cs     # both shapes, one public static class
├── Requests/
├── Responses/
└── Services/
```

A module's checks split by **who is asking**, not by what they check.

| | Shape A — predicate | Shape B — guard |
|---|---|---|
| Caller | a request validator, from `.Must(...)` | a service operation or a handler |
| Signature | `public static bool IsExist<Thing>(this IRepositoryWrapper …)` | `public static void ThrowIf<Condition>(…)` |
| Extension method? | yes — on `IRepositoryWrapper`, so a validator reaches it through the dependency it already holds | no — a plain static, taking what the caller already loaded |
| On failure | returns `false`; the `RuleFor` chain supplies the message | throws `BadRequestException` with the message it chose |
| Answers | one question | a sequence of questions, in order |

Both live in the same `<X>Validation.cs`. **Neither is a fallback for the other**, and a
module with both is normal: the request is validated before the operation starts, and the
operation still has preconditions the request could not have known about.

```csharp
// Validations/OrderValidation.cs
using Core.Common.Exceptions;

namespace Infrastructure.Modules.Orders.Validations;

public static class OrderValidation
{
    public static bool IsExistOrderCode(this IRepositoryWrapper repositoryWrapper, string code, Guid? exceptId = default)
    {
        return repositoryWrapper
            .Repository<Order>()
            .IsExistByUnique(x => x.Code, code, exceptId);
    }

    public static void ThrowIfOrderNotPayable(Order order, DateTimeOffset now)
    {
        if (order.Status != OrderStatus.Confirmed)
        {
            throw new BadRequestException(Messages<Order>.NotAvailable());
        }

        if (order.PaidAt.HasValue)
        {
            throw new BadRequestException(Messages<Order>.WasUsed());
        }

        if (order.ExpiresAt.HasValue && order.ExpiresAt <= now)
        {
            throw new BadRequestException(Messages<Order>.Expired());
        }
    }
}
```

Called from each side:

```csharp
// in the request validator — asking
RuleFor(x => x.Code)
    .NotEmpty().WithMessage(Messages<Order>.Required(x => x.Code))
    .Must(code => !repositoryWrapper.IsExistOrderCode(code!))
    .WithMessage(Messages<Order>.AlreadyExist(x => x.Code));

// in the service operation — demanding
Order order = await repositoryWrapper.Repository<Order>()
    .Find(x => x.Id == orderId)
    .FirstOrDefaultAsync(cancellationToken) ?? throw new BadRequestException(Messages<Order>.NotFound());

OrderValidation.ThrowIfOrderNotPayable(order, DateTimeOffset.UtcNow);
```

**A guard belongs in this file even when one method calls it.** It is not a private helper
that got promoted; it is the module's statement of when an operation may proceed, and the
second caller — a bulk action, a handler — arrives later and must find it already written.

**Predicates are synchronous.** A `.Must(...)` rule runs synchronously, so a predicate uses
the repository's synchronous reads — `Any`, `Count`, `GetById`, `Find(...).FirstOrDefault()`
— and takes no cancellation token; there is no asynchronous operation to cancel. Do not
reach for `.GetAwaiter().GetResult()` on an async repository call to fit this shape: that
blocks a request thread to answer a question the synchronous read already answers.

## Naming law

| The member | Name | Returns |
|---|---|---|
| "does a row with this id exist?" | `IsExist<Thing>` | `bool` |
| "does a row with this property value exist?" | `IsExist<Thing><Property>` | `bool` |
| any other yes/no question | the natural `Is…` / `Has…` form — `IsAreaInRegion`, `HasArea`, `HasActiveCodeSpaceConflict` | `bool` |
| "reject the operation if this holds" | `ThrowIf<Condition>` | `void` |

**A predicate never throws, and a guard never returns `bool`.** They are the two halves of
one boundary:

- A predicate that throws takes the message away from the validator. The validator's whole
  job is to attach *this* message to *this* property and keep collecting the other failures;
  a predicate that throws mid-rule ends the run with one error and no property attached.
- A guard that returns `bool` pushes the decision back to every caller. Two services then
  answer "what does `false` mean here?" differently, and the second one written gets it wrong.

`IsExisted<Thing>` is drift — the tense reads as a past event rather than a present-tense
question, and the facade's own helpers (`IsExistByIds`, `IsExistByUnique`) speak the
`IsExist…` form. The `Id` suffix is noise when the parameter is already an id:
`IsExistCategory(Guid id)`, not `IsExistCategoryId(Guid id)`. Reserve the property segment
for when it distinguishes something — `IsExistOrderCode`. Rename when you touch the file; a
static method rename is safe.

## Shape A: predicates a validator asks

### Uniqueness and the `exceptId` parameter

```csharp
public static bool IsExistOrderCode(this IRepositoryWrapper repositoryWrapper, string code, Guid? exceptId = default)
{
    return repositoryWrapper
        .Repository<Order>()
        .IsExistByUnique(x => x.Code, code, exceptId);
}
```

`IsExistByUnique` is a facade extension with this signature:

```csharp
public static bool IsExistByUnique<T, TProperty>(
    this IRepositoryBase<T> repositoryBase,
    Expression<Func<T, TProperty>> propertySelector,
    object uniqueValue,
    Guid? exceptId = null)
    where T : BaseEntity
```

Four things follow from it:

- **It extends `IRepositoryBase<T>`, not the wrapper.** You reach it through
  `repositoryWrapper.Repository<Order>()`. That is why the module-level helper exists: it is
  the one line that gives a validator a call it can make directly.
- **`exceptId` is what makes one helper serve both actions.** Omit it on create; pass the id
  of the record being updated on update, so a record does not collide with itself. Two
  helpers — one per action — duplicate the uniqueness definition, and the pair diverges the
  first time the column changes.
- **The value parameter is `object`.** A wrong-typed value compiles and fails at run time, so
  the selector and the argument have to agree by inspection.
- **The selector must name a `string`, a value type or an enum.** Anything else — a
  navigation, a collection, an owned type — throws `InvalidOperationException` when the rule
  runs, not when it compiles. For `string` values the comparison is case-insensitive: both
  sides are lowered.

Negate at the call site, not in the helper. The helper answers "does one exist?"; whether
that is a failure depends on the rule:

```csharp
.Must(code => !repositoryWrapper.IsExistOrderCode(code!))                        // create: must be free
.Must((request, code) => !repositoryWrapper.IsExistOrderCode(code!, request.Id)) // update: free, ignoring itself
.Must(id => repositoryWrapper.IsExistCategory(id!.Value))                        // reference: must be taken
```

An `IsNotExist…` twin is the wrong fix: it doubles the surface and leaves the reader asking
which of the pair is authoritative.

### Existence: one row, or a whole collection

```csharp
public static bool IsExistCategory(this IRepositoryWrapper repositoryWrapper, Guid id)
{
    return repositoryWrapper
        .Repository<Category>()
        .GetById(id) != default;
}

public static bool IsExistTagCodes(this IRepositoryWrapper repositoryWrapper, ICollection<string> codes)
    => repositoryWrapper.Repository<Tag>().Count(x => codes.Contains(x.Code!)) == codes.Count;
```

- **Count-and-compare answers "all of them" in one round trip.** Looping the collection and
  asking per item is N queries behind an innocent-looking `All(...)`.
- **It fails closed on duplicates**: two copies of one code make the count fall short and the
  rule rejects the request. Right outcome, wrong message — pair the rule with `.NotDuplicate()`
  so the caller hears *why*.
- **A collection of ids needs no helper at all** when the whole request is a range:
  `RangeItemValidator<TEntity, TId>` already brings not-empty, no-duplicates and
  all-ids-exist in one `Include`.

### A conflict predicate with optional inputs

Not every predicate is an existence check. This one answers "would this order overlap an
active one already claiming the same code space?" — the rule that cannot be a column
constraint.

```csharp
public static bool HasActiveCodeSpaceConflict(
    this IRepositoryWrapper repositoryWrapper,
    int? codeLength,
    OrderCodeCharacterType? codeCharacterType,
    string? prefix,
    DateTimeOffset? startAt,
    DateTimeOffset? endAt,
    Guid? excludedOrderId = null)
{
    if (!codeLength.HasValue
        || !codeCharacterType.HasValue
        || !startAt.HasValue
        || !endAt.HasValue
        || string.IsNullOrWhiteSpace(prefix))
    {
        return false;
    }

    string normalizedPrefix = prefix.ToUpperInvariant();

    return repositoryWrapper.Repository<Order>().Any(x =>
        (!excludedOrderId.HasValue || x.Id != excludedOrderId.Value)
        && x.Status == OrderStatus.Active
        && x.CodeLength == codeLength.Value
        && x.CodeCharacterType == codeCharacterType.Value
        && x.Prefix == normalizedPrefix
        && x.StartAt < endAt.Value
        && startAt.Value < x.EndAt);
}
```

- **An undecidable predicate returns `false`; it never throws and never guesses.** Every input
  is nullable because the validator runs on a request whose members may all be missing. When
  an input it needs is absent, the predicate declines to answer — and the `NotEmpty` rule that
  owns that property reports the real problem. A predicate that threw on a null input would
  replace a clear "Code length is required" with an exception.
- **Normalization belongs inside the predicate.** Casing the prefix once, next to the
  comparison it feeds, is what keeps the stored form and the compared form in agreement.
- **The exclusion parameter is `exceptId` spelled out by hand**, because the query is not a
  single-column lookup: `(!excludedOrderId.HasValue || x.Id != …)` matches everything on
  create and skips one row on update.
- **The overlap test is two comparisons, not one.** `x.StartAt < endAt && startAt < x.EndAt`
  is the whole rule; one comparison misses the containment cases.

A predicate this wide is the ceiling of the shape. If it needs a second query, or a message
of its own, it is a guard.

## Shape B: guards a service or handler demands

```csharp
public static void ThrowIfShipmentNotDispatchable(Shipment shipment, Guid carrierId, DateTimeOffset now)
{
    if (shipment.Order != null && shipment.Order.OrderCarriers?.Any(x => x.CarrierId == carrierId) != true)
    {
        throw new BadRequestException(Messages<Carrier>.NotAllowed(nameof(Order)));
    }

    if (shipment.Order?.Status != OrderStatus.Active)
    {
        throw new BadRequestException(Messages<Order>.NotAvailable());
    }

    if (shipment.DispatchedAt.HasValue)
    {
        throw new BadRequestException(Messages<Order>.WasUsed());
    }

    if (shipment.ExpiresAt.HasValue && shipment.ExpiresAt <= now)
    {
        throw new BadRequestException(Messages<Order>.Expired());
    }
}

public static void ThrowIfShipmentInvalidForDispatch(Shipment? shipment)
{
    if (shipment is null)
    {
        return;
    }

    if (shipment.Order is null)
    {
        throw new BadRequestException(Messages<Order>.NotFound());
    }

    if (shipment.Order.Status != OrderStatus.Active)
    {
        throw new BadRequestException(Messages<Order>.NotAvailable());
    }
}
```

- **One condition, one message, in order.** The first condition that holds ends the guard, so
  the sequence *is* the priority order: most specific first, most generic last. Four conditions
  collapsed into one `if` with one message is a guard that can only say "no".
- **A guard is not an extension method.** It takes the entity the caller already loaded, with
  the navigations it needs already included, so it costs no query and cannot be mistaken for a
  repository call. If a guard needs to query, the operation should have loaded that data first.
- **Time comes in as a parameter.** `DateTimeOffset now` from the caller means every condition
  in one operation is judged against the same instant, and the guard is inspectable without
  controlling the clock.
- **`null` in, `return` out — when nothing was claimed there is nothing to check.**
  `ThrowIfShipmentInvalidForDispatch` takes a nullable input precisely because the optional
  path is legitimate: no shipment is not an error. Say that with an early `return`, never by
  making the parameter non-nullable and forcing every caller to branch before calling.
- **A guard may throw a message about a different entity.** The first condition concerns
  whether *this carrier* may carry *this order*, so the message is `Messages<Carrier>` — the
  entity whose rule was broken, not the entity the file is named after.
- **The name describes the rejected state, not the check.** `ThrowIfShipmentNotDispatchable`
  says what happens and when; `ValidateShipment` or `CheckShipment` says neither.

## The message a guard chooses

Every message, in a rule and in a guard, comes from `Messages<T>` where `T` is the **entity**.
A guard reaches for the state members a request validator never needs — `NotAvailable()`,
`WasUsed()`, `Expired()`, `NotAllowed(name)`, and `NotFound()` for the entity itself. Which
overload takes a selector and which takes a `string`, and why the selector is an expression
over the entity, is owned by `references/request-response-families.md`.

## Conditional and cross-field rules

The rule always stays in the validator; the predicate only answers. Three forms:

```csharp
RuleFor(x => x.Reference)
    .NotEmpty().When(_ => action == "Create", ApplyConditionTo.CurrentValidator)
    .WithMessage(Messages<Shipment>.Required(x => x.Reference))
    .MaximumLength(64).WithMessage(Messages<Shipment>.OverLength(x => x.Reference));

RuleFor(x => x.RegionId)
    .NotEmpty().WithMessage(Messages<Shipment>.Required(x => x.RegionId))
    .Must(id => repositoryWrapper.IsExistRegion(id!.Value))
    .WithMessage(Messages<Shipment>.NotFound(x => x.RegionId));

When(x => x.RegionId != null && repositoryWrapper.HasArea(x.RegionId.Value), () =>
{
    RuleFor(x => x.AreaId)
        .NotEmpty().WithMessage(Messages<Shipment>.Required(x => x.AreaId))
        .Must(id => repositoryWrapper.IsExistArea(id!.Value))
        .WithMessage(Messages<Shipment>.NotFound(x => x.AreaId))
        .Must((request, id) => repositoryWrapper.IsAreaInRegion(id!.Value, request.RegionId!.Value))
        .WithMessage(Messages<Shipment>.Invalid(x => x.AreaId));
});
```

- **`.When(…, ApplyConditionTo.CurrentValidator)` conditions one rule** — an action-only rule,
  or one that applies only when the member was supplied. Why the second argument is not
  optional is in `references/request-response-families.md`.
- **`.Must((request, value) => …)` is how a rule reads a sibling member**, and it must come
  after the rules that establish that sibling: `IsAreaInRegion(id, request.RegionId!.Value)`
  dereferences `RegionId` with `!`, which is only honest because the `RegionId` rule above
  already required it. Order the chain so each rule may assume what the rules above enforced.
- **A `When(condition, () => { … })` block conditions two or more rules together**, and its
  condition may read the database: `HasArea(...)` is a predicate in this same file — a block
  condition is a rule like any other. Read it as the sentence it is: *if a region was chosen
  and that region is subdivided, then an area is required and must belong to it.* Repeating
  one `When` on three rules instead lets the three copies drift apart. A block holding a
  single rule is `.When(…, ApplyConditionTo.CurrentValidator)` written the long way.

## The facade's rule helpers come first

Before writing a `.Matches(...)` regex, an inline lambda or a one-off predicate
into a rule chain, open `Facades/Common/Extensions/ValidatorExtension.cs` and
look for the rule already written — `Required()`, the character-class helpers
and their siblings. The helper is the rule's one definition; every hand-rolled
copy is a second one, and the copies drift the day the rule changes.

**When the helper itself is wrong, the order is fixed:** warn the user and
change nothing on your own; fix the helper only once the user approves; migrate
the hand-rolled call sites onto it only after the fix. Migrating first launders
the helper's defect into every caller — a validator moved onto a broken helper
is a rule that just got quietly weaker.

## Anti-example: a prefixed validation file

```
Orders/Validations/
├── GlobalOrderValidation.cs    ✗ prefixed
└── OrderValidation.cs          ✓
```

**Every method in that file is correct** — the predicates are named right, return `bool`, and
use the facade's uniqueness helper with `exceptId`. Only the type name is wrong, and it is
still worth fixing:

- **"Global" describes nothing observable.** A `public static class` is already reachable from
  anywhere in the assembly, so the prefix distinguishes it from nothing, and a name true of
  every candidate carries no information.
- **It creates a second home with no rule for choosing between them.** Once a module has both
  files, the next predicate goes wherever its author looked first, and neither file can be
  read as the module's rules.
- **The parts of one concern stop sorting together** — `OrderService`, `OrderExpression`,
  `OrderValidation` — exactly as with prefix-named service parts.

Rename to `<X>Validation.cs` when you touch it. The rename is safe: a static class is never
registered and never injected, so only the type name and its call sites change.

## Review checklist

- The module has one `Validations/<X>Validation.cs`, a `public static class`, no prefix.
- Every existence predicate is `IsExist<Thing>` or `IsExist<Thing><Property>`, returns `bool`,
  and throws nothing. No `IsExisted…`, no `Id` suffix on an id parameter.
- Every guard is `ThrowIf<Condition>`, returns `void`, throws `BadRequestException` with a
  `Messages<T>` message, and returns no flag.
- Predicates take no cancellation token and use the synchronous repository reads.
- Uniqueness goes through `IsExistByUnique(selector, value, exceptId)` — one helper serving
  create and update, not two — and the selector names a string, value type or enum.
- A predicate with optional inputs returns `false` when it cannot decide.
- Guards are plain statics taking already-loaded entities, and take `now` as a parameter.
- Each guard condition has its own message, ordered most specific first.
- Rules that read a sibling member use `.Must((request, value) => …)` and appear after the
  rules that establish that sibling.
- Two or more rules sharing a condition are inside one `When(condition, () => { … })` block.
- No check that needs no database access has left the validator, and no computed business
  value has been written as a predicate instead of an `Expressions/` member.
- No hand-rolled regex or predicate duplicates a `ValidatorExtension` helper — and a wrong
  helper is reported first, fixed on approval, and only then adopted.
