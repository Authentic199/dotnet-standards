# Anti-patterns in existing copies

The pipeline is copied, not built (Principle 1), and copies accumulate. The six
shapes below are the ones that survive a port and are never noticed afterwards:
none of them fails a build or a request. Each is described as a shape, not as an
accusation — you will meet them in a file you inherit, and possibly in one you
are about to copy from.

Most of the replacements are already shipped. `references/query-expression-extension.md`,
`references/property-info-extension.md` and `references/pagination-extension.md`
carry the canonical listings, and each closes with a *Deviations from corpus*
table recording which of these were corrected there and which were deliberately
left alone. Read that table before "fixing" anything named here.

## Console diagnostics inside the filter loop

```csharp
// BAD — inside the filter stage, and again in the collection arm
try
{
    Console.WriteLine("----> Filter Query: " + queryFilterResult.Query);
    entities = entities.Where(queryFilterResult.Query, queryFilterResult.Params.ToArray());
}
catch (Exception ex)
{
    Console.WriteLine("----> Filter Query Fail: " + ex.GetBaseException());
}
```

The fuller copies carry four of these, because the collection arm repeats the
whole `try`/`catch`. The success-path write is the expensive one: it runs once
per surviving filter term on **every** list request, unconditional, with no
switch to turn it off, and it makes the filter stage the only stage in the
pipeline that writes anything at all. What it writes carries no level, no
timestamp and no correlation, so there is nothing structured to filter or query
on later.

What it prints is at least bounded. Principle 4 puts every caller-supplied
*value* into the params array as `@0`, `@1`, …, so the composed predicate carries
resolved property paths and operator shapes only — the line leaks the client's
field names, not the client's data. This entry is about noise, not disclosure.

Some copies moved the success-path line off the console and left the catch arm
on it. That is half a fix, and the half still firing is the half that fires when
something is wrong.

**Write instead:** recreate from `references/query-expression-extension.md`. That
listing calls `Console.WriteLine` in neither arm, and its deviation table records
the change as settled. In a copy you already have, it is a two-line edit that
changes nothing a client can observe.

## The catch arm that leaves no record

```csharp
// BAD — the drop is right; the silence is not
catch (Exception ex)
{
    Console.WriteLine("----> Filter Query Fail: " + ex.GetBaseException());
}
```

Read this entry carefully, because the obvious complaint about it is the wrong
one. The queryable is left untouched, the loop continues, the method returns
normally and the endpoint answers 200 with a wider row set — and **all of that is
the contract**, stated in Principle 3 and defended by the anti-pattern *Turning a
dropped term into an exception*. Do not convert this arm into a throw.

What is wrong is that the arm makes a *rejected* predicate indistinguishable from
a *deliberately dropped* term. A term the guards drop leaves no trace by design;
a term the expression parser rejected leaves none either, because the only record
goes somewhere the application's own diagnostics do not. Two very different
events — a client typo, which is expected and uninteresting, and a predicate this
file composed incorrectly, which is a defect in the pipeline — reach the same
outcome, and nothing counts either. A malformed template introduced by an edit to
the operator table, or a parser regression arriving with a package upgrade, can
therefore ship, widen every affected list endpoint, and surface much later as a
user reporting that the filter stopped narrowing.

**Write instead:** keep the catch, keep the drop, change the destination.
`references/query-expression-extension.md` ships the dependency-free form, which
at least keeps the console out of it. A project that wants filter-parse failures
visible in production routes this one line through the logging abstraction its
own modules already use. These are static extension methods with nothing
injected, so that record arrives ambiently or through a delegate the caller hands
in — not through a new DI dependency added to a file whose whole value is that it
copies cleanly (SKILL.md, *Recreating the pipeline*, step 5). One edit, one file,
every list endpoint at once: the same "change it here, once" rule Principle 3
states for the drop itself.

## A character set standing in for a suffix

```csharp
// BAD — intended to strip a trailing " and " / " or "
queryFilterResult.Query = queryFilterResult.Query.TrimEnd(' ', 'a', 'n', 'd', 'o', 'r');

string searchQuery = searchQueryBuilder.ToString().TrimEnd(' ', 'o', 'r', ' ');
```

Each loop appends a separator after every term and the last one has to come off.
What is written is not "remove that separator" but "remove any trailing run of
these characters" — a different instruction that happens to agree with the
intended one on the current inputs.

**This is latent, not live.** Every predicate the shipped operator templates
compose ends in `)`, which is in neither character set, so the filter-side trim
halts one character into the predicate and no shipped copy is corrupted today.
The search side reaches the same result by a thinner margin: the null-safe term
ends `== true or `, so the trim eats ` or ` and then stops on the `e` of `true`.
One letter, and nothing states it.

The defect is that this correctness is a property of the current template set and
is written down nowhere in the code. `[PropName]` is substituted **before** the
trim runs, so the string under the trim already contains a resolved property
name. The day a new template, a sort token or an appended field name ends in a
bare word, the trim eats letters off a real identifier: a path ending `...Ordered`
is handed on as `...Ordere`, which either fails in the parser — landing in the
catch arm above, where nothing records it — or resolves to a different, shorter
property and silently filters on the wrong column. The two defects compose, which
is why they are adjacent entries here.

```csharp
// GOOD — strip the thing you appended, by what it is
const string separator = " and ";
if (query.EndsWith(separator, StringComparison.Ordinal))
{
    query = query[..^separator.Length];
}
```

Better still, do not emit the separator after the last term. Either form survives
a new operator; the character set does not, and it fails without a build error.
`references/query-expression-extension.md` flags the same point in its copying
notes, and its deviation table records that the corpus form was deliberately kept.

## A guard evaluated on the reflection object, not on the property

```csharp
// BAD — GetType() is the type of the PropertyInfo instance
if (propertyInfo != null && filterItem.Value != null && !propertyInfo.GetType().IsGenericType)
```

`GetType()` here returns the runtime type of the reflection object itself, not
the type of the property it describes. Whatever the property is, that test is the
same test, so the condition is constant with respect to the thing it appears to
check and has never excluded anything. Every lineage carries it; the leaner ones
spell it inverted, as an early-`continue` guard, which reads as even more
deliberate.

The asymmetry is the tell. The very next branch of the same method guards the
real thing:

```csharp
// the same intent, written correctly, in the same method
if (dataHolder != null && filterItem.Value != null
    && !dataHolder.PropertyElement.PropertyInfo.PropertyType.IsGenericType)
```

So inside one method a generic-typed property — `Nullable<T>` among them — is
excluded on the collection path and admitted on the top-level path. Nobody chose
that split; the two lines were written to say the same thing and one of them does
not.

**Do not "fix" it by swapping in `PropertyType`.** That is a live behaviour
change, not a typo repair: `Nullable<T>` is a generic type, so the corrected
guard would exclude every nullable value-typed property from filtering, on every
list endpoint at once. `references/query-expression-extension.md` declines the
change for exactly that reason, in its copying notes and again in its deviation
table. Inherit the expression as it stands.

The lesson is for the next guard you write, not for the one you are holding. A
dead guard is worse than no guard, because a reviewer reads it as a deliberate
safety check and stops asking what happens downstream. Write the condition you
actually mean — and if a type exclusion is genuinely wanted here, name the types
to exclude, because `IsGenericType` sweeps `Nullable<T>` in with them. `GetType()`
called on a `PropertyInfo`, `MemberInfo` or `Type` is almost always a slip for the
thing that object describes.

## Domain knowledge welded into the reflection walk

```csharp
// BAD — inside the walk that discovers the DEFAULT search-field set
if (propertyInfo.CustomAttributes.Any(x => typeIgnores.Contains(x.AttributeType))
    || propertyInfo.PropertyType == typeof(SomeConcreteType)
    || propertyInfo.Name.Equals("SomeLiteralPropertyName", StringComparison.OrdinalIgnoreCase))
{
    continue;
}
```

Two of the three disjuncts are one project's policy — a concrete type and a
literal property name — ORed onto the attribute test that was the entire
extension point. This is SKILL.md's *Baking domain knowledge into the stages* in
its most expensive form; the entry is here for the two things that bullet does
not make concrete:

- **The effect is global and its cause is invisible.** This walk is the default
  search-field discovery for **every** list endpoint that passes no
  `SearchFields`, so two named things drop out of free-text search everywhere at
  once. It presents as "search doesn't find that field", with nothing in
  `ApplySearch`, in the request or on the response type to explain it.
  Attribute-driven exclusions are legible on the response type; this one is
  legible nowhere.
- **The file stops copying.** It welds a general-purpose utility that
  `common-extensions` owns to one project's types, so the next project either
  drags that type along or deletes the clause and silently changes which fields
  are searched.

**Write instead:** `[NotSearchable]` on the property, and the ignore attributes
passed in from the search stage as arguments — a caller's policy expressed at the
caller. `references/property-info-extension.md` reduces the check to the
attribute test and records the removal in its deviation table.

## A dead member added to the paged envelope

```csharp
// BAD — one lineage only, on the two-generic subclass
public sealed class PaginationResponse<T, TMoreInfo> : PaginationResponse<T>
{
    public TMoreInfo MoreInfo { get; set; }

    public IEnumerable<object> Data { get; internal set; }   // never assigned, never read
}
```

Nothing in the solution assigns it — the envelope's only assignment is
`PagedData = items` in the constructor — nothing reads it, and `internal set`
puts it out of reach of any module that might have filled it, so it cannot even
be a hook someone else uses. It sits on the two-generic subclass and not on the
single-generic base, so the two paged shapes the same API returns no longer agree
on their member set. `IEnumerable<object>` carries no schema, so nothing could be
written against it even if it were filled.

The pipeline-side rule is the one to take away: **a member added to a recreated
pipeline type is a change to the wire contract**, whatever that member does — and
this one does nothing. What may appear on the paged envelope is not this skill's
to decide; it is owned by `api-surface` (`references/request-response-dtos.md`),
which SKILL.md already routes to under *Paging closes the chain*. Take a proposed
member there before writing it here, and never add one to one arity and not the
other.

**Write instead:** recreate from `references/pagination-extension.md`, whose
deviation table records that this member is not carried. Removing it from a copy
that has it is itself a contract question — ask there, do not settle it in an
extension file.
