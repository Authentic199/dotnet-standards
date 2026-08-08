# Entity configuration — the declaration catalogue

What goes inside a `Configure` body once the entity and its configuration file
already exist in the shape `SKILL.md` shows. Everything here is looked up at the
keyboard, with the cursor already inside `Configure`: which base to derive from,
how a relationship is declared, which `OnDelete` to pick, how composite
uniqueness is expressed, how an enum is stored.

The rules that are violated *without the author knowing to look* — one entity one
file, no `HasMaxLength`, no `IsRequired()`, `citext` for human-read text — stay
in `SKILL.md` rather than here, and are not repeated.

## `BaseEntity` and `BaseEntity<TId>`

`BaseEntity` closes `BaseEntity<TId>` over `Guid` and assigns `Id` in its
constructor from a sequential GUID generator, so the id exists the moment you
`new` the entity — children can be wired up, and the id returned, before
anything is saved. The generic base contributes `CreatedAt`, defaulted to
`DateTimeOffset.UtcNow`.

Reach for `BaseEntity<TId>` only if a key genuinely cannot be a GUID — **and
that rule binds every type deriving from the base, not entities alone.**
Response families root at `BaseEntity` too (`api-surface`,
`references/request-response-dtos.md`), so a response written
`: BaseEntity<Guid>` where the closed `BaseEntity` exists is the same defect in a
different layer — the generic form is never a choice when the key is a `Guid`. A
reviewer of either layer flags it.

## The opening line

**Open every `Configure` with `HasBaseEntity().UnderscoreTable()`** — primary
key on `Id`, then the table name snake-cased from the type (`OrderLine`
becomes `order_line`). The two are independent, so one order across the
solution is a convention worth keeping rather than a requirement.

## Case-insensitive text

`HasCitextUnique(x => x.Code)` sets the column type **and** the unique index
together, and takes an optional filter. `HasColumnType("citext")` alone fits a
case-insensitive column that need not be unique — and note what that costs: a
`citext` column declared that way is searchable and unindexed, which is
`dotnet-performance-review` 1.5's finding.

## Relationships and `OnDelete`

**Foreign keys are explicit pairs**: a `Guid CustomerId` — nullable when the
link is optional — beside a nullable reference navigation, declared with
`HasOne`/`WithMany`/`HasForeignKey` rather than left to convention, and
finished with an `OnDelete` chosen on purpose. Collection navigations are
non-nullable and initialized `= default!`.

The question `OnDelete` answers is *what should happen to this row when its
target is deleted?*

| Answer | Behaviour |
|---|---|
| It cannot outlive the target | `Cascade` — child rows, membership and log tables |
| The target must not be deletable while this points at it | `Restrict` — shared catalogue or configuration rows |
| It survives, having kept what it needs | `SetNull` — optional FK on a history row |

`SetNull` needs a nullable FK, only makes sense when the row still carries a
usable snapshot of what it pointed at, and is by a wide margin the rarest of
the three.

## Composite uniqueness

A plain index over an anonymous type —
`builder.HasIndex(x => new { x.OrderId, x.LineNumber }).IsUnique()` — which is
how a natural key gets enforced when the surrogate `Id` is not the real one.

## Enums

**Int-backed with explicit values starting at 1**, so stored numbers never shift
when a member is inserted later, and each member carries an XML doc line. Pin a
default with a property initializer, `HasDefaultValue`, or both: the initializer
covers entities created in code, `HasDefaultValue` rows inserted around it.

The enum's *file* is not this skill's call — every enum a capability owns lives
in `Enums/`, one per file, never inside an entity, response or service file.
That is `facade-module-architecture`'s rule.
