# PaginationExtension — canonical listing

Full source for paging, the response envelope, the request base and the filter
model binder. Copy verbatim into
`Infrastructure/Facades/Common/Extensions/PaginationExtension.cs`.

**Prerequisites**
- `Microsoft.EntityFrameworkCore` (for `ToListAsync` / `CountAsync`).
- `Microsoft.AspNetCore.Mvc` and `…Mvc.ModelBinding` (for the model binder).

**Notes for whoever copies this**
- `Current` is 1-based.
- Both async overloads issue a second round trip for the count. It is
  asynchronous and takes the same `CancellationToken`.
- The synchronous `ToPagedList` overloads are for sequences already in memory.
  Do not reach for them to make a `DbSet` compile.
- `QueryContainer` is the base every list request derives from. Add request
  fields in the derived type, not here.
- `PaginationResponse<T>`, `PageInfo` and `QueryContainer` are a **wire
  contract**. Their member names, types and the `int.MaxValue / 2` default are
  published by `api-surface`, `references/request-response-dtos.md`. Recreate
  them exactly; adding, renaming or reordering a member changes the API.
- The binder produces `Dictionary<string, List<string?>>` for a property typed
  `Dictionary<string, List<string>?>?`. That is deliberate and it works —
  nullable *reference* annotations are erased, so both are the same runtime type.
  Do not "align" them.
- `ApplyQuery` takes no `CancellationToken` and passes no `searchFieldExcepts`.
  That is the trade the bundle makes, not an oversight: it exists to fix the call
  order and the fallback sort for endpoints that need neither knob. Where either
  matters, call the four stages directly — that chain is the canonical one, and
  `ef-core-data-access` documents it.

```csharp
using System.ComponentModel.DataAnnotations;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Facades.Common.Extensions;

public static class PaginationExtension
{
    public static PaginationResponse<T> ToPagedList<T>(this IEnumerable<T> entities, int current, int pageSize)
    {
        IEnumerable<T> items = entities.Skip((current - 1) * pageSize).Take(pageSize);
        return new PaginationResponse<T>(items, entities.Count(), pageSize, current);
    }

    public static PaginationResponse<TEntity, TMoreInfo> ToPagedList<TEntity, TMoreInfo>(this IEnumerable<TEntity> entities, int current, int pageSize, TMoreInfo moreInfo)
    {
        IEnumerable<TEntity> items = entities.Skip((current - 1) * pageSize).Take(pageSize);
        return new PaginationResponse<TEntity, TMoreInfo>(items, entities.Count(), pageSize, current, moreInfo);
    }

    public static async Task<PaginationResponse<T>> ToPagedListAsync<T>(this IQueryable<T> entities, int current, int pageSize, CancellationToken cancellationToken = default)
    {
        IEnumerable<T> items = await entities.Skip((current - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PaginationResponse<T>(items, await entities.CountAsync(cancellationToken), pageSize, current);
    }

    public static async Task<PaginationResponse<TEntity, TMoreInfo>> ToPagedListAsync<TEntity, TMoreInfo>(this IQueryable<TEntity> entities, int current, int pageSize, TMoreInfo moreInfo, CancellationToken cancellationToken = default)
    {
        IEnumerable<TEntity> items = await entities.Skip((current - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PaginationResponse<TEntity, TMoreInfo>(items, await entities.CountAsync(cancellationToken), pageSize, current, moreInfo);
    }
}

public class PaginationResponse<T>
{
    public PaginationResponse(IEnumerable<T> items, int totalCount, int pageSize, int current)
    {
        PagedData = items;
        PageInfo = new(totalCount, pageSize, current);
    }

    public IEnumerable<T> PagedData { get; set; }

    public PageInfo PageInfo { get; set; }
}

public sealed class PaginationResponse<T, TMoreInfo> : PaginationResponse<T>
{
    public PaginationResponse(IEnumerable<T> items, int totalCount, int pageSize, int current, TMoreInfo moreInfo)
        : base(items, totalCount, pageSize, current)
    {
        MoreInfo = moreInfo;
    }

    public TMoreInfo MoreInfo { get; set; }
}

public class PageInfo
{
    public PageInfo(int totalCount, int pageSize, int current)
    {
        TotalCount = totalCount;
        PageSize = pageSize;
        Current = current;
    }

    public int TotalCount { get; set; }

    public int PageSize { get; set; }

    public int Current { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNext => Current < TotalPages;

    public bool HasPrevious => Current > 1 && Current <= TotalPages;
}

public class QueryContainer : IValidatableObject
{
    /// <summary>
    /// filter data by operator($eq, $null, $in, $gt, $lt, $lte, $gte, $btw, $ilike, $sw) ex: { filter.propName : "$eq:abc" }
    /// </summary>
    [ModelBinder(BinderType = typeof(CustomFilterBinder))]
    public Dictionary<string, List<string>?>? Filter { get; set; }

    /// <summary>
    /// Number elements on a page.
    /// </summary>
    public int PageSize { get; set; } = int.MaxValue / 2;

    /// <summary>
    /// Pages number to take out of the total pages.
    /// </summary>
    public int Current { get; set; } = 1;

    /// <summary>
    /// Search field. Ex: '["Name","Items.Name"]'.
    /// </summary>
    public string[]? SearchFields { get; set; }

    /// <summary>
    /// Search keyword.
    /// </summary>
    public string? SearchKeyword { get; set; }

    /// <summary>
    /// Sort query string. Ex: 'Name desc,Relation.Name'.
    /// </summary>
    public string? SortQuery { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PageSize <= 0 || PageSize > int.MaxValue / 2)
        {
            yield return new ValidationResult(
                $"{nameof(PageSize)}Invalid",
                new[] { nameof(PageSize) }
            );
        }

        if (Current <= 0 || Current > int.MaxValue / 2)
        {
            yield return new ValidationResult(
                $"{nameof(Current)}Invalid",
                new[] { nameof(Current) }
            );
        }

        long offset = (long)(Current - 1) * PageSize;
        if (offset > int.MaxValue)
        {
            yield return new ValidationResult(
                $"{nameof(Current)}Invalid",
                new[] { nameof(PageSize), nameof(Current) }
            );
        }
    }
}

public class CustomFilterBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        if (bindingContext.HttpContext.Request.QueryString.HasValue)
        {
            const string filterKey = nameof(QueryContainer.Filter);
            Dictionary<string, List<string?>> filterQueries = bindingContext.HttpContext.Request.QueryString.Value![1..]
                .Split('&')
                .Where(x => x.StartsWith(filterKey, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Split('=')[0])
                .ToDictionary(x => x.Key[(filterKey.Length + 1)..], x => x.Select(x =>
                {
                    string[] compareValue = x.Split('=');
                    return compareValue.Length > 1 ? HttpUtility.UrlDecode(compareValue.GetValue(1)?.ToString()) : string.Empty;
                }).ToList());

            bindingContext.Result = ModelBindingResult.Success(filterQueries);
        }

        return Task.CompletedTask;
    }
}
```

## ApplyExtension

`Infrastructure/Facades/Common/Extensions/ApplyExtension.cs`. The four-stage
bundle and the default entry point.

```csharp
using Core.Bases;

namespace Infrastructure.Facades.Common.Extensions;

public static class ApplyExtension
{
    public static Task<PaginationResponse<TResponse>> ApplyQuery<TResponse, TRequest>(this IQueryable<TResponse> entities, TRequest request)
        where TRequest : QueryContainer
        => entities
        .ApplyFilter(request.Filter)
        .ApplySearch(request.SearchKeyword, request.SearchFields)
        .ApplySort($"{nameof(BaseEntity.CreatedAt)} {OrderTypeAcronym.Desc}", request.SortQuery)
        .ToPagedListAsync(request.Current, request.PageSize);

    public static Task<PaginationResponse<TResponse, TMoreInfo>> ApplyQuery<TResponse, TRequest, TMoreInfo>(this IQueryable<TResponse> entities, TRequest request, TMoreInfo moreInfo)
        where TRequest : QueryContainer
        => entities
        .ApplyFilter(request.Filter)
        .ApplySearch(request.SearchKeyword, request.SearchFields)
        .ApplySort($"{nameof(BaseEntity.CreatedAt)} {OrderTypeAcronym.Desc}", request.SortQuery)
        .ToPagedListAsync(request.Current, request.PageSize, moreInfo);
}
```

## Deviations from corpus

| Change | Reason |
|---|---|
| `await entities.CountAsync(cancellationToken)` replaces sync `entities.Count()` in both async overloads | Pre-authorized correction. |
| `Current` failure now yields `$"{nameof(Current)}Invalid"` under `[nameof(Current)]` | Pre-authorized correction of a copy-paste repeated in all six projects. |
| Offset overflow guard added; its prose message replaced by `$"{nameof(Current)}Invalid"` | Guard is pre-authorized and corpus-grounded (one lineage). The prose sentence is not: `message-keys` rule 1 forbids message literals outright. Composed with `nameof`, like its two neighbours; the member-names array still distinguishes it. |
| `PageSize <= 0` retained | Settled; one lineage loosened it to `< 0`, which admits page size zero. |
| `PopulateKeys` member removed, and its `using` with it | Sanitization — it binds `QueryContainer` to a separate facade the recreating project will not have. |
| Untyped `IEnumerable<object> Data` member on `PaginationResponse<T, TMoreInfo>` not carried | Present in one lineage only, never assigned, not part of the wire contract. |
| Doc-comment examples genericized | Sanitization. |
| File named `ApplyExtension.cs`; type `ApplyExtension` | Settled correction of the corpus spelling. Method name `ApplyQuery` unchanged. |
| **Not** changed: `ApplyQuery` takes no `CancellationToken` and no `searchFieldExcepts` | Corpus-faithful. The bundle's whole value is that it is the short form; the four-stage chain is where both knobs live. |
| Explicit `if (…) throw new ArgumentNullException(…)` retained over the terser helper form found in another lineage | R7 — canonical lineage, not averaged. |
