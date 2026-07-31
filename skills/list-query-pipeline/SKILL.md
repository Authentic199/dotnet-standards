---
name: list-query-pipeline
description: >-
  This skill should be used when the list-query extensions themselves must be
  written, ported or repaired in a .NET API: QueryExpressionExtension,
  PaginationExtension, ApplyQuery, the $eq/$in/$btw/$ilike/$sw operators and
  the $not prefix, System.Linq.Dynamic.Core predicate strings, np() null
  propagation, GetPropertyRecursive, nested-collection search and sort,
  [NotSearchable]/[NotSearch], CustomFilterBinder, PaginationResponse,
  PageInfo, QueryContainer — or when ApplyFilter/ToPagedListAsync does not
  resolve. Not for: pipeline call sites, repository queries —
  ef-core-data-access; search service methods, validators — module-feature;
  list endpoints, request DTO chains — api-surface; full-text index queries —
  elasticsearch-search; regex, serializer, shared helpers — common-extensions;
  file placement — facade-module-architecture.
---

## Overview

Every list endpoint in the house speaks one query grammar — `filter.Prop=$eq:1`,
`searchKeyword`, `sortQuery`, `current`/`pageSize` — and that grammar is
implemented once, as four composable stages over a single `IQueryable<T>`:
filter, search, sort, page.

This skill owns the stages themselves. Call sites, service shapes and the wire
contract belong to the siblings named in the description; read them before
changing anything a client can observe.

| File under `Infrastructure/Facades/Common/` | Owns |
|---|---|
| `Extensions/QueryExpressionExtension.cs` | `ApplyFilter` / `ApplySearch` / `ApplySort`, the operator table, `FilterOperator`, `FilterPrefix`, `OrderTypeAcronym`, `QueryFilterResult` |
| `Extensions/PaginationExtension.cs` | `ToPagedList` / `ToPagedListAsync`, `PaginationResponse<T>`, `PaginationResponse<T,TMoreInfo>`, `PageInfo`, `QueryContainer`, `CustomFilterBinder` |
| `Extensions/PropertyInfoExtension.cs` + `Attributes/NotSearchableAttribute.cs` | name resolution (`GetPropertyRecursive`, `GetPropertyRecursiveWithMaxDeep`, `GetDataHolders`) and the search opt-out attribute |

A fifth, optional file bundles the four calls into one: `Extensions/ApplyExtension.cs`,
holding `ApplyQuery`.

## Core Principles

1. **Recreate the pipeline; never re-derive it.** These files are accumulated
   project wisdom, and a new project usually starts without them. When the
   current project has no `QueryExpressionExtension`, copy the source out of
   `references/` and change nothing but the namespace and the base-entity type
   name. Re-deriving a filter parser produces a *second* query grammar: the
   operator set, the `$not` prefix and the paging envelope are a contract every
   client and every other endpoint already speaks, and a near-miss
   implementation is worse than none, because it fails only at the edges.

2. **The grammar is closed, and it is closed here.** Ten operators — `$eq $null
   $in $gt $lt $lte $gte $btw $ilike $sw` — plus the `$not` prefix. A module
   that needs a condition the grammar cannot express does not invent a magic
   filter value and interpret it in a service. It either composes an ordinary
   `Where` before the chain, or the operator is added *in this file* and in the
   contract table `api-surface` publishes. One grammar, one place, one document.

3. **Every stage is total: bad input narrows nothing, and nothing throws.** An
   unresolvable field name, an unknown operator token, a `$btw` without exactly
   two non-empty parts, a `$null` on a non-nullable value type, a sort field
   that is neither string nor value type nor enum — each is dropped mid-loop and
   the chain continues, returning its input unchanged when nothing survives.
   This is the contract, not an oversight: `filter.nosuchfield=$eq:1` yields the
   unfiltered page, not a 500, and that is what lets one identical four-call
   chain serve every list endpoint with no `if (request.Filter is not null)` at
   any call site. The consequence is stated openly, not hidden — an unrecognised
   condition **widens** the result set (`ef-core-data-access`,
   `references/query-conventions.md`). A project that judges this failure mode
   wrong changes it **here, once**, and the siblings that publish the behaviour
   change in the same commit; never per module.

4. **Reflection gates the names; parameters carry the values.** No stage trusts
   a caller-supplied field name: `GetPropertyRecursive` resolves every filter
   key, sort key and search field against the element type before a single
   character of predicate is built, one dotted segment at a time, with
   `BindingFlags.IgnoreCase`. A name that does not resolve produces no
   predicate. Only such a resolved *property name* is ever concatenated into the
   predicate string; every caller-supplied *value* becomes `@0`, `@1`, … and
   travels in the params array. This is the one rule in these files that is a
   security boundary rather than a style preference.

5. **Two overload families exist because null behaves differently in each.**
   The `IQueryable<T>` overloads compose SQL, where a null navigation yields
   NULL. The `IEnumerable<T>` overloads run LINQ-to-Objects, where the same
   access throws — so each delegates to its `IQueryable` twin with
   `checkNull: true`, which wraps member accesses in `System.Linq.Dynamic.Core`'s
   `np(...)`. Prefer the `IQueryable` form; the in-memory form is for a sequence
   you already hold and that is not backed by a database.

## Patterns

### The operator table is data, not code

Operators are a dictionary of predicate templates. Adding an operator is adding
a row, plus an arm in the switch only when its value needs special parsing.

```csharp
public static readonly ImmutableDictionary<string, string> FilterOperators = ImmutableDictionary.CreateRange(
    new Dictionary<string, string>
    {
        { FilterOperator.Eq,    " ([PropName] == [Value])" },
        { FilterOperator.Null,  " == null " },
        { FilterOperator.In,    "([PropName] == [Value]) or " },
        { FilterOperator.Btw,   " ( [PropName]  >= [First] and [PropName] <= [Last] ) " },
        { FilterOperator.Ilike, " ([PropName].Contains([Value])) " },
        { FilterOperator.Sw,    " ([PropName].StartsWith([Value])) " },
        // $gt $lt $lte $gte take the $eq shape with the comparison swapped
    });
```

`[Value]`/`[First]`/`[Last]` are replaced by `@n` first; `[PropName]` is
substituted **last**, so a property name can never collide with a parameter
token.

### The filter grammar

One term is `[$not:]<operator>[:<value>]`, carried on a query-string key of the
form `filter.<PropertyPath>`.

| Token | Example | Predicate |
|---|---|---|
| `$eq` | `$eq:abc` | equality |
| `$null` | `$null` | is-null; applied only when the property is nullable or a reference type, otherwise dropped |
| `$in` | `$in:1,2,3` | membership over the comma-split list |
| `$gt` `$lt` `$gte` `$lte` | `$gte:10` | comparison |
| `$btw` | `$btw:2024-01-01,2024-12-31` | inclusive range; needs exactly two non-empty parts or the term is dropped |
| `$ilike` | `$ilike:abc` | `Contains(...)` |
| `$sw` | `$sw:ab` | `StartsWith(...)` |
| `$not:` prefix | `$not:$eq:abc` | the whole predicate, negated with `!` |

Four rules that are easy to get wrong when porting:

- **Only the first colon after the operator splits.** The remainder is rejoined,
  so a value may itself contain colons (`$eq:2024-01-01T08:30:00`).
- **Repeating a key ANDs its terms.** `?filter.Amount=$gte:10&filter.Amount=$lte:99`
  becomes two chained `Where` calls. Within a single `$in`, the values are ORed.
- **`$ilike` and `$sw` apply no case folding.** The token name misleads: the
  templates carry no `ToLower`. Only keyword search lowercases, on both the
  property and the keyword. Whatever case behaviour `$ilike` shows comes from
  the database collation, not from this code.
- **Filter keys are lowercased and resolved case-insensitively**, so
  `filter.name` and `filter.Name` are the same term.

Parsing one value is three steps in order — strip a leading `$not`, split once,
then branch on how many parts remain. `$null` is the only single-part operator,
and its guard is `propertyType.IsNullableType() || propertyType.IsClass`.
`IsNullableType()` is a two-line `Type` predicate in a sibling `TypeExtension`
in the same namespace: **it is project code and the recreation must carry it.**
`IsClass` there is the framework's own `Type.IsClass` property — do not
substitute a same-named extension method, which would narrow the guard.

> **Porting note.** The corpus carries two `$in` predicate strategies: an OR
> chain of equalities with a `.ToString()` coercion for non-string properties,
> and a single `@0.Contains(it.[PropName])` against the split array. The **wire**
> grammar is identical in both. Do not port an `$in` fix between projects
> without reading both sides.

`CustomFilterBinder` is what makes the nested `filter.X` shape bind at all —
default model binding cannot produce `Dictionary<string, List<string>>` from a
dotted, repeated query key. It reads the raw query string, keeps `&`-segments
whose key starts with `Filter`, groups repeats by the part left of `=`, strips
the `filter.` prefix from the key, and URL-decodes each value; with no query
string it does nothing. It is wired by attribute, which is why every list
request inherits it for free:

```csharp
[ModelBinder(BinderType = typeof(CustomFilterBinder))]
public Dictionary<string, List<string>?>? Filter { get; set; }
```

### Keyword search and the default field set

When the request supplies `SearchFields`, only those are searched. When it does
not, the set is derived by reflection from the element type — and only `string`
properties are ever keyword-searched:

```csharp
searchFields ??= entityType
    .GetPropertyRecursiveWithMaxDeep(1, typeof(JsonIgnoreAttribute), typeof(NotSearchableAttribute))
    .ToArray();
```

Depth **1** — own properties plus one level of navigation — is the canonical
default; it bounds both the walk and the query it produces. The walk always
appends `[NotMapped]` to whatever ignore types it is handed, and rejects a type
argument that is not an attribute. Keep a property out of the derived set with
the attribute, not with a code change in `ApplySearch`:

```csharp
public class EntityBaseResponse
{
    public string Name { get; set; } = string.Empty;

    [NotSearchable]
    public string InternalNote { get; set; } = string.Empty;
}
```

Each accepted field contributes one `or` term — `Field.ToLower().Contains(@0)`,
or `np(Field.ToLower().Contains(@0)) == true` on the `checkNull` path — and the
keyword is lowered once and passed as `@0` for all of them. `searchFieldExcepts`
drops individual fields at one call site without touching the type.

**Pass the exclusion attributes in from the search stage**, as arguments to
`GetPropertyRecursiveWithMaxDeep`, rather than hard-coding them inside that
method. `PropertyInfoExtension` is a general-purpose reflection utility that
`common-extensions` owns; keeping the search-specific attribute at the call site
is what keeps it general — and a hard-coded one is invisible to anyone reading
`ApplySearch`.

> **Two spellings exist in the wild.** `NotSearchableAttribute` is the name to
> recreate. Older projects ship `NotSearchAttribute` instead, appended *inside*
> the walk rather than passed in, and some ship both files with only one wired.
> When you meet one, check which name the search stage actually honours before
> adding attributes — and do not rename an established project's attribute as a
> side effect of an unrelated task.

### Nested paths and collections

Dotted paths resolve through single-valued navigations for filter, search and
sort alike (`Relation.Name`). Collection navigations are a separate mechanism:
`GetDataHolders` recognises a path whose head is a generic collection of
entities and splits it into the collection property and the element property, so
each stage can emit the shape it needs.

```csharp
// search  →  Items.Any(Name.ToLower().Contains(@0)) or
// filter  →  Items.Any(<the operator predicate for Name>)
// sort    →  Items.Max(Name) descending      (Min/ascending for the other direction)
```

Not every copy has this. In the simplest lineage a collection path resolves to
nothing and is dropped.

> **Version-sensitive, and not caused by the collection arm itself.** Newer
> `System.Linq.Dynamic.Core` rejects an aggregate in `OrderBy` unless the parser
> is relaxed: projects on 1.7.x build a `ParsingConfig` with
> `RestrictOrderByToPropertyOrField = false` and pass it to `OrderBy`, while
> projects on 1.3.x emit the same `Max`/`Min` sort with no config at all. Match
> the package version in the project you are writing for.

### Stable ordering

After the requested terms, `ApplySort` appends the element type's `Id`
descending as a final tiebreaker whenever the type has an `Id`. Without it, two
rows sharing a `CreatedAt` can swap places between page 1 and page 2 and the
client silently loses or repeats a row. Sort syntax is
`Field[ desc][,Field2[ desc]]`, direction defaults to ascending, and the
acronyms live in `OrderTypeAcronym`. The first argument to `ApplySort` is a
fallback used only when the request carries no `SortQuery`.

### Paging closes the chain

```csharp
public static async Task<PaginationResponse<T>> ToPagedListAsync<T>(
    this IQueryable<T> entities, int current, int pageSize, CancellationToken cancellationToken = default)
{
    IEnumerable<T> items = await entities.Skip((current - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    return new PaginationResponse<T>(items, await entities.CountAsync(cancellationToken), pageSize, current);
}
```

`Current` is 1-based. Two statements, two round trips: the page, then a count
over the *same composed queryable*, so the total reflects the filters rather
than the table. The count is a second database call either way — in an async
overload, make it an asynchronous one. The `TMoreInfo` overload is the same body
with one extra constructor argument, and `ToPagedList` is the in-memory mirror.

`PaginationResponse<T>` carries the page and a `PageInfo`;
`PaginationResponse<T,TMoreInfo>` is sealed, derives from it, and adds a
`MoreInfo` payload. Those member names are a wire contract owned by
`api-surface` (`references/request-response-dtos.md`) — recreate the shape
exactly; do not add, rename or reorder a member here. What the composed chain
*costs* per request is likewise not this skill's to re-derive: it is stated in
`ef-core-data-access` (`references/query-conventions.md`) and graded by
`dotnet-performance-review`. A change here that moves that number changes those
sentences in the same commit.

### `QueryContainer` validates its own paging

```csharp
public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
{
    if (PageSize <= 0 || PageSize > int.MaxValue / 2)
    {
        yield return new ValidationResult($"{nameof(PageSize)}Invalid", [nameof(PageSize)]);
    }

    if (Current <= 0 || Current > int.MaxValue / 2)
    {
        yield return new ValidationResult($"{nameof(Current)}Invalid", [nameof(Current)]);
    }
}
```

Zero is rejected, not read as "no paging" — the "give me everything" case is the
deliberately huge `int.MaxValue / 2` default, which `api-surface` documents as a
known hazard. Each message names **its own** member: a `Current` violation
reported under the `PageSize` key sends the client to the wrong field. Guard the
computed offset `(Current - 1) * PageSize` against overflowing `int` before it
reaches `Skip`.

### `ApplyQuery`, when a project wants one call

```csharp
public static Task<PaginationResponse<TResponse>> ApplyQuery<TResponse, TRequest>(
    this IQueryable<TResponse> entities, TRequest request)
    where TRequest : QueryContainer
    => entities
        .ApplyFilter(request.Filter)
        .ApplySearch(request.SearchKeyword, request.SearchFields)
        .ApplySort($"{nameof(BaseEntity.CreatedAt)} {OrderTypeAcronym.Desc}", request.SortQuery)
        .ToPagedListAsync(request.Current, request.PageSize);
```

The bundle fixes the order and the fallback sort so no call site can get them
wrong, at the cost of the knobs the explicit chain exposes — `searchFieldExcepts`,
a per-endpoint fallback sort, and a `CancellationToken`, which neither overload
takes. It is a convenience over the canonical chain, not a replacement: a
project may ship it, and a project that does not is missing nothing. Where
cancellation matters, call the four stages. The spelling is
`ApplyExtension.cs` / `ApplyQuery`; a misspelt file you meet in an existing
project is the same file, and correcting it is a rename, not a redesign.

### Recreating the pipeline in a project that lacks it

1. Copy all four extension listings from `references/` — `QueryExpressionExtension`,
   `TypeExtension`, `PaginationExtension`, `PropertyInfoExtension` — into
   `Infrastructure/Facades/Common/Extensions/`, and the attribute into
   `Infrastructure/Facades/Common/Attributes/`. `TypeExtension` is the one that
   gets missed: it is listed inside `references/query-expression-extension.md`,
   and without it `IsNullableType()` and `IsCollection()` do not resolve.
2. Add `Extensions/ApplyExtension.cs` if the project wants the bundle.
3. Reference `System.Linq.Dynamic.Core`; on 1.7.x keep the `ParsingConfig`
   relaxation for collection sorts.
4. Point the three project anchors at this project's own types: the namespace
   root, the base-entity references (`Id`, `CreatedAt`), and the marker interface
   `IsUserDefineType` tests. **Nothing else in the copy should need editing.**
5. Drop members that depend on facades this project does not have, rather than
   pulling in a facade to satisfy them.
6. Carry over no type or property name from the source project. A copied skip
   list naming a concrete type is a caller's policy that leaked in — express it
   as `[NotSearchable]` or as a parameter.

## Anti-patterns

- **A bespoke filter parser at a call site.** A service that reads
  `request.Filter["Status"]` and builds its own `Where`, or a module-local
  `BuildFilterQuery`, is a second grammar with a different operator set and a
  different failure mode that no client can discover and no reviewer can check.
  If the pipeline is missing, recreate it; if it is present, call it.

  ```csharp
  // BAD — module-local reinterpretation of a shared grammar
  if (request.Filter?.TryGetValue("Status", out var raw) == true)
      query = query.Where(x => x.Status == int.Parse(raw[0].Split(':')[1]));

  // GOOD
  query = query.ApplyFilter(request.Filter);
  ```

- **Interpolating a caller's value into the predicate.** Any arm that writes the
  payload into the query string instead of adding it to `Params` and emitting
  `@n` hands a user-controlled fragment to the expression parser. Principle 4
  has no exceptions, including for values that "are always numeric".

  ```csharp
  // BAD
  result.Query = $"({key} == \"{parts[1]}\")";

  // GOOD
  result.Query = FilterOperators[FilterOperator.Eq].Replace("[Value]", $"@{indexParam}", StringComparison.OrdinalIgnoreCase);
  result.Params.Add(parts[1]);
  ```

- **Paging after materialising.** `ToListAsync()`, `AsEnumerable()` or a
  `.ToList()` "to make the types line up" before `Skip`/`Take` transfers the
  whole set and pages the copy. The `IEnumerable` overloads exist for sequences
  that are already materialised; they are not a type-error escape hatch on a
  `DbSet`.

- **Turning a dropped term into an exception.** Making an unresolvable field or
  an unknown token throw converts every stale bookmark and client typo into a
  500. Terms are dropped by design — Principle 3 says where that decision is
  allowed to change, and it is not here-and-now at one endpoint.

- **A sort with no tiebreaker.** Dropping the appended `Id descending` while
  recreating `ApplySort` looks like a simplification and is a paging bug that
  surfaces only under duplicate sort keys.

- **Redesigning the wire contract while porting.** Renaming the page members,
  flattening `PageInfo`, folding `MoreInfo` into the root, or inventing an
  eleventh operator token breaks every client, every sibling skill that
  documents the shape, and the Swagger comment on `Filter`. Extend with a
  `TMoreInfo` payload, not by reshaping.

- **Baking domain knowledge into the stages.** A concrete entity type in an
  exclusion check, or a literal property name compared by string, welds a
  generic file to one project and blocks the copy into the next. Take it as a
  parameter or an attribute.

- **Two names for one attribute in one project.** Both spellings present and
  only one wired means half the annotations in the codebase do nothing, and
  nothing reports it.

- **Restating an owned contract elsewhere.** Service `SearchAsync` shapes and
  `Search<X>Request` belong to `module-feature`; call placement and cost to
  `ef-core-data-access`; the response envelope to `api-surface`;
  relevance-ranked full-text search to `elasticsearch-search`. Point at them.

### Rationalizations, and what is actually true

| Rationalization | Reality |
|---|---|
| "This project doesn't have the extensions, so I'll write the filtering inline for this one endpoint." | That is precisely the moment to recreate. One endpoint becomes three, each with different operator spellings. |
| "It's only one list endpoint — the full pipeline is overkill." | The pipeline is a copy, not a build. Copying three files costs less than designing a filter grammar. |
| "The client sent a bad field name, so it should get a 400." | Silent-drop is the contract every existing client is written against. Changing it is a decision for the whole pipeline, not a fix at one call site. |
| "I'll add `$contains` — it reads better than `$ilike`." | A new token is invisible to every other project and to the documented operator list. Use the ten that exist. |
| "I'll copy it from the last project I saw it in." | That copy carries that project's types and that project's drift. Copy from `references/`. |

### Red flags — stop

- `ToList` / `ToListAsync` / `AsEnumerable` appearing *before* `Skip`, `Take` or `ToPagedListAsync`
- A string built with `+` or interpolation to hold a request value inside a predicate
- A `switch` over operator names anywhere outside these files
- A concrete entity or domain type name inside `QueryExpressionExtension` or `PropertyInfoExtension`
- `try { … } catch { }` added around a stage to make a malformed term "work"

## Decision Guide

| Situation | Do |
|---|---|
| `ApplyFilter` / `ToPagedListAsync` does not resolve | Check for `using Infrastructure.Facades.Common.Extensions;` first. If the files genuinely are absent, recreate all three from `references/` — never write a local substitute |
| The project already has the pipeline | Use it as-is. Port a fix from `references/` only for the specific defect you are there to fix |
| A client needs a condition the ten operators cannot express | Compose an ordinary `Where` before the chain; add an operator only if it is general, and update `api-surface`'s table in the same change |
| A string property must never be swept by free-text search | `[NotSearchable]` on the response property — not a code change in `ApplySearch`, and not `searchFieldExcepts` at one call site |
| One call site must skip a field the others search | `searchFieldExcepts`. That is what the parameter is for |
| The project ships the other attribute spelling | Recreate *new* files as `NotSearchableAttribute`; leave an established name alone mid-task, and check which one the search stage actually honours |
| Filtering or searching across a one-to-many | Needs the `GetDataHolders` collection form. If the project's copy is single-valued-only, port the collection form from `references/` rather than special-casing the endpoint |
| Sorting by an aggregate over a collection throws in the parser | Check the `System.Linq.Dynamic.Core` version: 1.7.x needs a `ParsingConfig` with `RestrictOrderByToPropertyOrField = false` built **and passed** to `OrderBy` |
| A filter silently does nothing | In order: does the property resolve on the *projected* element type, not the entity? is the token one of the ten? is `$btw` carrying exactly two non-empty parts? is `$null` sitting on a non-nullable value type? |
| Paging returns duplicate or shifting rows across pages | The `Id` tiebreaker was dropped from `ApplySort`, or the element type has no `Id`. Restore a unique final sort key |
| An in-memory sequence, not a database query | The `IEnumerable` overloads — they set `checkNull` for you, which is what emits `np()` |
| An `IQueryable` from EF Core | The `IQueryable` overloads, `checkNull` left at its default. `np()` in translated SQL buys nothing |
| Writing the service method or its request class | `module-feature` — this skill owns neither `SearchAsync` nor `Search<X>Request` |
| Choosing where the call lives, or what the chain costs | `ef-core-data-access` (`references/query-conventions.md`) and `dotnet-performance-review`. Not here |
| Changing anything a client can see — a member, a default, an operator | `api-surface` (`references/request-response-dtos.md`) owns the contract; change it there first |
| Relevance ranking, fuzzy matching, analyzers | `elasticsearch-search`. This pipeline composes SQL predicates and has no notion of a score |

## Going deeper

Read the reference file that matches what you are holding:

- `references/query-expression-extension.md` — the complete
  `QueryExpressionExtension`, the operator grammar end to end, and the
  `IEnumerable`/`IQueryable` overload pairs. Read it to recreate or repair
  filtering, searching or sorting.
- `references/pagination-extension.md` — the complete `PaginationExtension`:
  the paging overloads, `PaginationResponse`, `PageInfo`, `QueryContainer`,
  `CustomFilterBinder`, and the `ApplyQuery` bundle.
- `references/property-info-extension.md` — the complete `PropertyInfoExtension`
  (`GetPropertyRecursive`, `GetPropertyRecursiveWithMaxDeep`, `GetDataHolders`,
  `DataHolder`) and `NotSearchableAttribute`. Both files above depend on it;
  `common-extensions` owns the general-utility view of the same type.
