# QueryExpressionExtension — canonical listing

Full source for the filter / search / sort stages. Copy verbatim into
`Infrastructure/Facades/Common/Extensions/QueryExpressionExtension.cs`.

**Prerequisites**
- NuGet `System.Linq.Dynamic.Core`. This listing targets **1.7.x**. On 1.3.x, drop
  the `parsingConfig` argument from the `OrderBy` call and delete the two
  `parsingConfig` lines.
- A base entity type exposing `Id` (referenced as `nameof(BaseEntity.Id)`).
- `PropertyInfoExtension` and `NotSearchableAttribute` — see
  `references/property-info-extension.md`.
- `TypeExtension` — second listing in this file.

**Notes for whoever copies this**
- The `$null` template contains no `[PropName]` token; the key is interpolated
  directly, so the later `[PropName]` replacement is a no-op for that operator.
- The lowered key (`filterItem.Key.ToLower()`) is used **only** to resolve the
  property by reflection. The predicate carries the key as the client sent it.
  Do not pass the lowered key into `GenerateFilterQuery`.
- The generic-type guard in `ApplyFilter` is evaluated on the `PropertyInfo`
  instance, not on `PropertyType`. Changing it to `PropertyType.IsGenericType`
  would exclude every `Nullable<T>` property from filtering.
- Both `TrimEnd` calls take a **character set**, not a suffix. They are safe only
  because every operator template ends in `)`. A new template that ends in a
  letter from those sets cannot rely on this trim.
- The `try`/`catch` around `Where` is the contract: a predicate the parser rejects
  skips that one term and leaves the query otherwise intact.
- `ParsingConfig.Default` is a shared static, and the line under it writes to it.
  That is the corpus form, and it is tolerable only because `false` is the sole
  value ever assigned to that flag anywhere — there is no second writer to race
  with. It is set **unconditionally, outside the loop**: the corpus sets it inside
  the `checkNull` branch, which never covers the `Collection.Max(...)` sort that
  needs it.
- If the project already has a `TypeExtension`, add only the missing members. Do
  not satisfy `IsNullableType()` by importing another library's `*.Internal`
  namespace.

```csharp
using Core.Bases;
using Infrastructure.Facades.Common.Attributes;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using static Infrastructure.Facades.Common.Extensions.PropertyInfoExtension;

namespace Infrastructure.Facades.Common.Extensions;

public static class QueryExpressionExtension
{
    public static readonly ImmutableDictionary<string, string> FilterOperators = ImmutableDictionary.CreateRange(
            new Dictionary<string, string>()
            {
                { FilterOperator.Eq, " ([PropName] == [Value])" },
                { FilterOperator.Null, " == null " },
                { FilterOperator.In, "([PropName] == [Value]) or " },
                { FilterOperator.Gt, " ([PropName] > [Value]) " },
                { FilterOperator.Lt, " ([PropName] < [Value]) " },
                { FilterOperator.Lte, " ([PropName] <= [Value]) " },
                { FilterOperator.Gte, " ([PropName] >= [Value]) " },
                { FilterOperator.Btw, " ( [PropName]  >= [First] and [PropName] <= [Last] ) " },
                { FilterOperator.Ilike, " ([PropName].Contains([Value])) " },
                { FilterOperator.Sw, " ([PropName].StartsWith([Value])) " },
            }
        );

    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> entities, string? keyword, string[]? searchFields, string[]? searchFieldExcepts = default, bool checkNull = false)
    {
        if (!entities.Any() || string.IsNullOrWhiteSpace(keyword))
        {
            return entities;
        }

        Type entityType = entities.ElementType;

        searchFields ??= entityType.GetPropertyRecursiveWithMaxDeep(1, typeof(JsonIgnoreAttribute), typeof(NotSearchableAttribute)).ToArray();

        StringBuilder searchQueryBuilder = new();
        foreach (string searchField in searchFields)
        {
            if (searchFieldExcepts?.Any(x => string.Equals(x, searchField, StringComparison.OrdinalIgnoreCase)) == true)
            {
                continue;
            }

            DataHolder? dataHolder = entityType.GetDataHolders(searchField, false);

            if (dataHolder is not null)
            {
                searchQueryBuilder.Append(dataHolder.PropertyGenericCollection.PropertyName).Append(".Any(").Append(dataHolder.PropertyElement.PropertyName).Append(".ToLower().Contains(@0)) or ");

                continue;
            }

            PropertyInfo? propertyInfo = entityType.GetPropertyRecursive(searchField);

            if (propertyInfo is null || propertyInfo.PropertyType != typeof(string))
            {
                continue;
            }

            if (checkNull)
            {
                searchQueryBuilder.Append("np(").Append(searchField).Append(".ToLower().Contains(@0)) == true or ");
            }
            else
            {
                searchQueryBuilder.Append(searchField).Append(".ToLower().Contains(@0) or ");
            }
        }

        string searchQuery = searchQueryBuilder.ToString().TrimEnd(' ', 'o', 'r', ' ');
        if (string.IsNullOrEmpty(searchQuery))
        {
            return entities;
        }

        return entities.Where(searchQuery, keyword.ToLower());
    }

    public static IEnumerable<T> ApplySearch<T>(this IEnumerable<T> entities, string? keyword, string[]? searchFields, string[]? searchFieldExcepts = default)
    {
        return entities.AsQueryable().ApplySearch(keyword, searchFields, searchFieldExcepts, true).AsEnumerable<T>();
    }

    public static IQueryable<T> ApplySort<T>(this IQueryable<T> entities, string orderByQueryDefault, string? orderByQuery, bool checkNull = false)
    {
        if (!entities.Any())
        {
            return entities;
        }

        orderByQuery ??= orderByQueryDefault;
        string[] orderParams = orderByQuery.Trim().ToLower().Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        StringBuilder orderQueryBuilder = new();
        ParsingConfig parsingConfig = ParsingConfig.Default;
        parsingConfig.RestrictOrderByToPropertyOrField = false;

        Type entityType = entities.ElementType;
        foreach (string orderParam in orderParams)
        {
            string propertyFromQueryName = orderParam.Trim().Split(" ")[0];
            PropertyInfo? propertyInfo = entityType.GetPropertyRecursive(propertyFromQueryName);
            if (
                    propertyInfo != null
                    &&
                    (
                        propertyInfo.PropertyType == typeof(string)
                        || propertyInfo.PropertyType.IsValueType
                        || propertyInfo.PropertyType.IsEnum
                    )
                )
            {
                string sortingOrder = orderParam.EndsWith($" {OrderTypeAcronym.Desc}", StringComparison.OrdinalIgnoreCase) ? "descending" : "ascending";

                if (checkNull)
                {
                    propertyFromQueryName = $"np({propertyFromQueryName})";
                }

                orderQueryBuilder.Append(propertyFromQueryName).Append(' ').Append(sortingOrder).Append(", ");
            }

            DataHolder? dataHolder = entityType.GetDataHolders(propertyFromQueryName);

            if (dataHolder is not null)
            {
                string sortingOrder = orderParam.EndsWith($" {OrderTypeAcronym.Desc}", StringComparison.OrdinalIgnoreCase) ? $"Max({dataHolder.PropertyElement.PropertyName}) descending" : $"Min({dataHolder.PropertyElement.PropertyName}) ascending";

                orderQueryBuilder.Append(dataHolder.PropertyGenericCollection.PropertyName).Append('.').Append(sortingOrder).Append(", ");
            }
        }

        PropertyInfo? idProperty = entityType.GetProperty(nameof(BaseEntity.Id));
        if (idProperty != null)
        {
            orderQueryBuilder.Append(idProperty.Name).Append(' ').Append("descending").Append(", ");
        }

        string orderQuery = orderQueryBuilder.ToString().TrimEnd(',', ' ');
        if (string.IsNullOrEmpty(orderQuery))
        {
            return entities;
        }

        return entities.OrderBy(parsingConfig, orderQuery);
    }

    public static IEnumerable<T> ApplySort<T>(this IEnumerable<T> entities, string orderByQueryDefault, string? orderByQuery)
    {
        return entities.AsQueryable().ApplySort(orderByQueryDefault, orderByQuery, true).AsEnumerable<T>();
    }

    public static IEnumerable<T> ApplyFilter<T>(this IEnumerable<T> entities, IDictionary<string, List<string>?>? filter)
    {
        return entities.AsQueryable().ApplyFilter(filter, true).AsEnumerable<T>();
    }

    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> entities, IDictionary<string, List<string>?>? filter, bool checkNull = false)
    {
        if (!entities.Any() || filter == null || filter.Count == 0)
        {
            return entities;
        }

        foreach (KeyValuePair<string, List<string>?> filterItem in filter)
        {
            Type entityType = entities.ElementType;
            string propertyFromQueryName = filterItem.Key.ToLower();

            PropertyInfo? propertyInfo = entityType.GetPropertyRecursive(propertyFromQueryName);
            if (propertyInfo != null && filterItem.Value != null && !propertyInfo.GetType().IsGenericType)
            {
                foreach (QueryFilterResult queryFilterResult in GenerateFilterQuery(filterItem!, propertyInfo.PropertyType, checkNull))
                {
                    queryFilterResult.Query = queryFilterResult.Query.TrimEnd(' ', 'a', 'n', 'd', 'o', 'r');

                    try
                    {
                        Debug.WriteLine("----> Filter Query: " + queryFilterResult.Query);
                        entities = entities.Where(queryFilterResult.Query, queryFilterResult.Params.ToArray());
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("----> Filter Query Fail: " + ex.GetBaseException());
                    }
                }
            }

            DataHolder? dataHolder = entityType.GetDataHolders(propertyFromQueryName, true);
            if (dataHolder != null && filterItem.Value != null && !dataHolder.PropertyElement.PropertyInfo.PropertyType.IsGenericType)
            {
                KeyValuePair<string, List<string>> filterElementItem = new(dataHolder.PropertyElement.PropertyName, filterItem.Value);
                foreach (QueryFilterResult queryFilterResult in GenerateFilterQuery(filterElementItem, dataHolder.PropertyElement.PropertyInfo.PropertyType, checkNull))
                {
                    StringBuilder filterQueryBuilder = new();
                    filterQueryBuilder
                        .Append(dataHolder.PropertyGenericCollection.PropertyName)
                        .Append(".Any(")
                        .Append(queryFilterResult.Query.TrimEnd(' ', 'a', 'n', 'd', 'o', 'r'))
                        .Append(')');
                    string filterQuery = filterQueryBuilder.ToString();

                    try
                    {
                        Debug.WriteLine("----> Filter Query: " + filterQuery);
                        entities = entities.Where(filterQuery, queryFilterResult.Params.ToArray());
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("----> Filter Query Fail: " + ex.GetBaseException());
                    }
                }
            }
        }

        return entities;
    }

    private static IEnumerable<QueryFilterResult> GenerateFilterQuery(KeyValuePair<string, List<string>> filterItem, Type propertyType, bool checkNull)
    {
        string key = filterItem.Key;
        if (checkNull)
        {
            key = $"np({key})";
        }

        const string suffix = "and ";
        List<QueryFilterResult> queryFilterResults = new();
        foreach (string value in filterItem.Value)
        {
            int indexParam = 0;
            QueryFilterResult queryFilterResult = new();
            List<string> result = value.Split(":").ToList();
            bool isFilterPrefixNot = false;

            if (result[0].Equals(FilterPrefix.Not, StringComparison.OrdinalIgnoreCase))
            {
                result.RemoveAt(0);
                isFilterPrefixNot = true;
            }

            if (result.Count > 1)
            {
                result = new()
                {
                    result[0],
                    string.Join(":", result.Skip(1)),
                };
            }

            switch (result.Count)
            {
                case 1
                when result[0].Equals(FilterOperator.Null, StringComparison.OrdinalIgnoreCase)
                     && (propertyType.IsNullableType() || propertyType.IsClass):
                    queryFilterResult.IsValid = true;
                    queryFilterResult.Query = $"({key} {FilterOperators[FilterOperator.Null]}) {suffix}";
                    break;

                case 2
                when FilterOperators.ContainsKey(result[0].ToLower()):
                    result[0] = result[0].ToLower();
                    queryFilterResult.IsValid = true;
                    switch (result[0])
                    {
                        case FilterOperator.In:
                            foreach (object param in queryFilterResult.Params = result[1].Split(",").Cast<object>().ToList())
                            {
                                string property = FilterOperators[FilterOperator.In]
                                    .Replace("[Value]", $"@{indexParam++}", StringComparison.OrdinalIgnoreCase);

                                if (propertyType != typeof(string))
                                {
                                    property = property.Replace("[PropName]", "[PropName].ToString()", StringComparison.OrdinalIgnoreCase);
                                }

                                queryFilterResult.Query = string.Concat(queryFilterResult.Query, property);
                            }

                            break;

                        case FilterOperator.Btw:
                            string[] btwValue = result[1].Split(',');
                            if (btwValue.Length != 2 || string.IsNullOrEmpty(btwValue[0]) || string.IsNullOrEmpty(btwValue[1]))
                            {
                                queryFilterResult.IsValid = false;
                                break;
                            }

                            queryFilterResult.Query = FilterOperators[FilterOperator.Btw]
                                .Replace("[First]", $"@{indexParam++}", StringComparison.OrdinalIgnoreCase)
                                .Replace("[Last]", $"@{indexParam}", StringComparison.OrdinalIgnoreCase);
                            queryFilterResult.Params.AddRange(btwValue);
                            break;

                        case FilterOperator.Ilike:
                            queryFilterResult.Query = FilterOperators[FilterOperator.Ilike]
                                .Replace("[Value]", $"@{indexParam}", StringComparison.OrdinalIgnoreCase);
                            queryFilterResult.Params.Add(result[1]);
                            break;

                        case FilterOperator.Sw:
                            queryFilterResult.Query = FilterOperators[FilterOperator.Sw]
                                .Replace("[Value]", $"@{indexParam}", StringComparison.OrdinalIgnoreCase);
                            queryFilterResult.Params.Add(result[1]);
                            break;

                        default:
                            queryFilterResult.Query = FilterOperators[result[0]]
                                .Replace("[Value]", $"@{indexParam}", StringComparison.OrdinalIgnoreCase);
                            queryFilterResult.Params.Add(result[1]);
                            break;
                    }

                    break;
            }

            if (isFilterPrefixNot && queryFilterResult.IsValid)
            {
                queryFilterResult.Query = $"!{queryFilterResult.Query}";
            }

            queryFilterResult.Query = queryFilterResult.Query.Replace("[PropName]", key, StringComparison.OrdinalIgnoreCase);
            queryFilterResults.Add(queryFilterResult);
        }

        return queryFilterResults.Where(x => x.IsValid);
    }
}

public static class OrderTypeAcronym
{
    public const string Asc = nameof(Asc);
    public const string Desc = nameof(Desc);
}

public static class FilterOperator
{
    public const string Eq = "$eq";
    public const string Null = "$null";
    public const string In = "$in";
    public const string Gt = "$gt";
    public const string Lt = "$lt";
    public const string Lte = "$lte";
    public const string Gte = "$gte";
    public const string Btw = "$btw";
    public const string Ilike = "$ilike";
    public const string Sw = "$sw";
}

public static class FilterPrefix
{
    public const string Not = "$not";
}

public class QueryFilterResult
{
    public string Query { get; set; } = string.Empty;

    public List<object> Params { get; set; } = new();

    public bool IsValid { get; set; } = false;
}
```

## TypeExtension

`Infrastructure/Facades/Common/Extensions/TypeExtension.cs`. Only the two members
the pipeline calls are listed. `IsNullableType` is used by the `$null` guard in
`GenerateFilterQuery`; `IsCollection` is used by `PropertyInfoExtension`.

The `$null` guard also reads `propertyType.IsClass` — that is the BCL
`Type.IsClass` **property**, not an extension method. Do not add parentheses.

```csharp
using System.Collections;

namespace Infrastructure.Facades.Common.Extensions;

internal static class TypeExtension
{
    /// <summary>
    /// Determines whether the specified type represents a collection type (<see cref="Array"/> or <see cref="IEnumerable"/>).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the specified type represents a collection type; otherwise, false.</returns>
    internal static bool IsCollection(this Type type) => type.IsArray || (type != typeof(string) && type.IsAssignableTo(typeof(IEnumerable)));

    /// <summary>
    /// Determines whether the specified type represents a <see cref="Nullable"/> type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the specified type represents a <see cref="Nullable"/> type; otherwise, false.</returns>
    internal static bool IsNullableType(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
}
```

## Deviations from corpus

| Change | Reason |
|---|---|
| `using AutoMapper.Internal;` removed | Mandate. `IsNullableType` now binds to the local `TypeExtension` in the same namespace. |
| `Console.WriteLine` → `Debug.WriteLine`, `using System.Diagnostics;` added | Settled; grounded in the simplest corpus lineage, which already writes it this way. |
| `$null` predicate written `({key} …)` instead of `(it.{key} …)` | On the `checkNull` path the key is already `np(Foo)`, and `it.np(Foo)` is a member access on a function call. Every other operator template in the same file interpolates the property unqualified, so the unqualified form is the file's own convention. One corpus lineage already writes it without the prefix. |
| `ParsingConfig` added and passed to `OrderBy` | Open call (c). The sort stage emits `Collection.Max(Field)` and `np(Field)`, neither of which is a bare property or field access. Both lines are corpus tokens, taken from the two projects that pin 1.7.1. |
| The flag assignment moved out of `if (checkNull)` to just after `ParsingConfig.Default` | Placement only. The corpus sets it inside a branch that never covers the `Max`/`Min` DataHolder sort, so whether the flag is set depends on which arm an earlier loop iteration happened to take. |
| `TypeExtension` reduced to two members | The other members are not called by any file in this skill. |
| **Not** changed: `entities.Any()` probes | Corpus-faithful in all six projects; the cost model is published by `ef-core-data-access` and graded by `dotnet-performance-review`. |
| **Not** changed: character-set `TrimEnd` calls | Provably latent given the shipped template set. |
| **Not** changed: `propertyInfo.GetType().IsGenericType` | Changing it to `PropertyType` would exclude all `Nullable<T>` properties from filtering. |
