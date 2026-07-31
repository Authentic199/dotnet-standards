# ValidationExtension

**When:** a validator needs an answer the request cannot supply — does this id exist, do all these ids exist, is this code already taken.

**Why it is separate from `ValidatorExtension`:** these hit the database. Keeping them in their own file makes the cost visible and keeps the pure-shape rules free of a repository dependency.

Three traps this file exists to close:

- **All, not any.** The naive form `Any(x => ids.Contains(x.Id))` returns true when *one* id matches. `IsExistByIds` compares counts, so it is false unless every id exists — which is what a validator asking "are these all valid references" actually means.
- **A missing id is not a valid id.** `IsExistById` folds `id != null` into the predicate, so a null id returns false rather than matching an arbitrary row.
- **The optional `filter` prevents method multiplication.** Without it every combination grows its own method — `IsExistByIdAndActive`, `IsExistByIdAndNotDeleted`. Instead the caller passes the extra condition and chooses the operator that joins it.

```csharp
using Core.Bases;
using Core.Common.Interfaces;
using Infrastructure.Facades.Persistence.Repositories;
using System.Linq.Expressions;

namespace Infrastructure.Facades.Common.Extensions;

public static class ValidationExtension
{
    public static bool IsExistById<TEntity>(this IRepositoryWrapper repositoryWrapper, Guid? id, Expression<Func<TEntity, bool>>? filter = null, ExpressionOperator exOperator = ExpressionOperator.And)
        where TEntity : BaseEntity
        => repositoryWrapper.Repository<TEntity>().Any(ExpressionExtension.Join(x => id != null && x.Id == id, filter, exOperator));

    public static bool IsExistByIds<TEntity>(this IRepositoryWrapper repositoryWrapper, IEnumerable<Guid> guids, Expression<Func<TEntity, bool>>? filter = null, ExpressionOperator exOperator = ExpressionOperator.And)
        where TEntity : BaseEntity
        => repositoryWrapper.IsExistByIds<TEntity, Guid>(guids, filter, exOperator);

    public static bool IsExistByIds<TEntity, TId>(this IRepositoryWrapper repositoryWrapper, IEnumerable<TId> guids, Expression<Func<TEntity, bool>>? filter = null, ExpressionOperator exOperator = ExpressionOperator.And)
        where TEntity : BaseEntity<TId>
        => repositoryWrapper.Repository<TEntity>().Find(
                ExpressionExtension.Join(
                    x => guids.Contains(x.Id),
                    filter,
                    exOperator
                )
            ).Distinct().Count() == guids?.Count();

    public static bool IsExistedCode<TEntity>(this IRepositoryWrapper repositoryWrapper, string code)
        where TEntity : BaseEntity, ICode
        => repositoryWrapper.Repository<TEntity>().Any(x => x.Code == code);
}
```

## Notes

- **Do not "optimise" `IsExistByIds` into `Any`.** The count comparison is the whole contract. Rewriting it to `Any(x => guids.Contains(x.Id))` turns "all of these exist" into "at least one of these exists" and the change passes every naive test that uses a single id.
- **Pass a non-null, already-materialised collection.** The `guids?` in `guids?.Count()` protects nothing: `guids.Contains(...)` inside the expression throws first when it is evaluated. Guard with `NotNull()`/`NotEmpty()` in the validator, not here. Materialising also matters because `guids` is enumerated twice — once inside the query, once by `Count()`.
- **`.Distinct()` runs over whole entities, not ids.** `Find` returns `IQueryable<TEntity>`, so this translates to a `SELECT DISTINCT` across every mapped column; entities are already key-identified, so it cannot change the count. It is kept for fidelity with the canonical form — if you are recreating this and want it faster, project to the id before the `Distinct()`.
- **The `Guid` overload is a thin forward** to the `TId` one. Keep it: it is what makes `IsExistByIds<TEntity>(ids)` infer without the caller naming `Guid` twice.
- **`ExpressionOperator.And` is the default and is the safe one.** `Or` widens the match — a caller wanting "exists *and* is active" who passes `Or` gets "exists *or* is active", which is nearly always true.
- **A null `filter` costs nothing** — `Join` returns the base expression unchanged.
- **These are synchronous and hit the database once each**, which is sized for a validator. Call `IsExistByIds` once with the whole collection; never loop `IsExistById` over a list.

## Dependencies and registration

| Needs | Contract |
|---|---|
| `IRepositoryWrapper` | `IRepositoryBase<T> Repository<T>()` |
| `IRepositoryBase<T>` | `bool Any(Expression<Func<T, bool>>? expression = default)` and `IQueryable<T> Find(Expression<Func<T, bool>>? expression = default, bool isAsNoTracking = default)` |
| `ExpressionExtension.Join` | `Expression<Func<T, TProperty>> Join<T, TProperty>(Expression<Func<T, TProperty>> baseExpression, Expression<Func<T, TProperty>>? joinExpression, ExpressionOperator exOperator = ExpressionOperator.And)` — returns `baseExpression` unchanged when the join is null. Full body in `references/expression-extension.md`. |
| `ExpressionOperator` | `enum { And = 1, Or = 2 }` — ships with `ExpressionExtension` |
| `BaseEntity` / `BaseEntity<TId>` | base with an `Id`; `BaseEntity` is the `Guid` specialisation |
| `ICode` | `public interface ICode { string? Code { get; } }` |

Static class, no DI registration.
