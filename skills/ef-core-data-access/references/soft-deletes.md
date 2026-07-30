# Soft delete implementation

These files are accumulated wisdom, not a sketch to improve on. They are carried
between projects unchanged because every edge — the reflection guard for entities
that never opted in, the expression-tree rewrite that powers the escape hatch,
the SQL constant that keeps partial indexes consistent — was paid for once
already.

**When the project lacks them, recreate them from this file** into
`Infrastructure.Facades.Common.SoftDeletes`, then wire the repository as shown
below. **When the project already has them, use them as they stand.** Do not
inline a bespoke `x.DeleteAt == null` at a call site, do not add a
`bool IsDeleted` to an entity, and do not write a narrower version because the
current feature needs only part of it — the next feature needs the other part,
and by then there are two implementations of one rule.

| File | Holds |
|---|---|
| `ISoftDelete.cs` | The deletion stamp and the SQL constant for partial indexes |
| `IHidden.cs` | The hidden stamp and the predicate helper that applies it |
| `GlobalQueryFilterExtension.cs` | `IgnoreGlobalQueryFilter` — the read-past-the-filter escape hatch |
| `RemoveGlobalQueryFilterNodeVisitor.cs` | The expression visitor that hatch is built on |

## Before scaffolding, check twice

1. **Does anything already own `ISoftDelete`, `IHidden`, or an
   `IgnoreGlobalQueryFilter` extension?** It may sit under a different name.
2. **Does `RepositoryBase` already compose anything into `Find`/`Count`/`Any`?**

Either hit means the capability exists — extend it in place rather than
scaffolding beside it. Two filters over the same entity is not a duplicate that
fails loudly; it is one query that filters and one that does not.

Note what the wiring below does the first time it lands: every existing `Find`,
`Count` and `Any` in the solution starts filtering the newly stamped entity at
once, because the composition is generic over `T`. On a new capability that is
the point. On a table the solution already queries it is a change worth
announcing before making it.

## The one dependency outside the folder

`ExpressionExtension.Join` ANDs two predicates into a single lambda over a fresh
parameter:

```csharp
public static Expression<Func<T, TProperty>> Join<T, TProperty>(
    Expression<Func<T, TProperty>> baseExpression,
    Expression<Func<T, TProperty>>? joinExpression,
    ExpressionOperator exOperator = ExpressionOperator.And)
```

It returns `baseExpression` untouched when the join expression is null.
`common-extensions` owns that file — recreate it from there, not from here.

## ISoftDelete.cs

```csharp
namespace Infrastructure.Facades.Common.SoftDeletes;

public interface ISoftDelete
{
    /// <summary>
    /// Set when the row is deleted; null while the row is live.
    /// </summary>
    public DateTimeOffset? DeleteAt { get; set; }

    public const string SqlFilter = $@"""{nameof(DeleteAt)}"" is null";
}
```

`SqlFilter` evaluates to `"DeleteAt" is null` — the property name in double
quotes, because `UnderscoreTable` snake-cases the table name and leaves column
names as they are. The `const` is what makes this interface worth more than a
property: every partial unique index in the solution cites one string, so the
column name and the index filter cannot drift apart, and writing it as an
interpolated `nameof` carries the filter along when the property is renamed.

## IHidden.cs

```csharp
using Infrastructure.Facades.Common.Extensions;
using System.Linq.Expressions;
using System.Reflection;

namespace Infrastructure.Facades.Common.SoftDeletes;

public interface IHidden
{
    /// <summary>
    /// Set while the row is withheld from reads; null when it is visible.
    /// </summary>
    public DateTimeOffset? HiddenAt { get; set; }
}

public static class HiddenExtension
{
    public static Expression<Func<T, bool>> HiddenObject<T>(this Expression<Func<T, bool>>? predicate)
        where T : class
    {
        Type type = typeof(T);
        if (type.IsAssignableTo(typeof(IHidden)))
        {
            ParameterExpression parameter = Expression.Parameter(type, "x");
            PropertyInfo hiddenAt = type.GetProperty(nameof(IHidden.HiddenAt), BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!;
            BinaryExpression condition = Expression.Equal(Expression.Property(parameter, hiddenAt), Expression.Constant(null, hiddenAt.PropertyType));
            return ExpressionExtension.Join(Expression.Lambda<Func<T, bool>>(condition, parameter), predicate);
        }

        return predicate ?? (_ => true);
    }
}
```

Three details are load-bearing:

- **The `IsAssignableTo` guard** is what lets the repository call this
  unconditionally for every `T`. An entity that never opted in gets its own
  predicate straight back.
- **It never returns null.** A caller passing no predicate for a non-hidden
  entity gets `_ => true`, so the repository always has something to compose a
  `Where` from and needs no null branch after the call.
- **The condition is built by reflection rather than as a typed lambda**,
  because `T` is only constrained to `class` at this point — an
  `x => x.HiddenAt == null` lambda would not compile here.

## GlobalQueryFilterExtension.cs

```csharp
namespace Infrastructure.Facades.Common.SoftDeletes;

public static class GlobalQueryFilterExtension
{
    public static IQueryable<T> IgnoreGlobalQueryFilter<T>(this IQueryable<T> query, params Type[] ignoreGlobalTypes)
        => (IQueryable<T>)query.Provider.CreateQuery(new RemoveGlobalQueryFilterNodeVisitor(ignoreGlobalTypes).Visit(query.Expression));
}
```

It rewrites `query.Expression` and rebuilds the queryable from the provider, so
it operates on everything composed up to that call and nothing after it. Because
the stamp check is an ordinary node rather than a model-level filter, removing it
is an ordinary visit — that is the payoff of injecting in the repository.

## RemoveGlobalQueryFilterNodeVisitor.cs

```csharp
using System.Linq.Expressions;
using System.Reflection;

namespace Infrastructure.Facades.Common.SoftDeletes;

public class RemoveGlobalQueryFilterNodeVisitor : ExpressionVisitor
{
    private readonly Type[] ignoreTypes;

    public RemoveGlobalQueryFilterNodeVisitor(params Type[] ignoreTypes)
    {
        this.ignoreTypes = ignoreTypes;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.Left is MemberExpression memberExpression
            && memberExpression.Member is PropertyInfo propertyInfo
            && ignoreTypes.Any(ignore => (propertyInfo.DeclaringType ?? propertyInfo.ReflectedType)?.IsAssignableTo(ignore) == true)
            && ignoreTypes.SelectMany(ignore => ignore.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name)).Contains(propertyInfo.Name)
            )
        {
            return Expression.Constant(true);
        }

        return base.VisitBinary(node);
    }
}
```

The match is on the **property**, not on who injected it: a binary node whose
left side reads a property declared by a type assignable to one of the named
interfaces, and whose name that interface also declares, collapses to `true`.
That is why the call takes `typeof(IHidden)` rather than the entity type — the
interface is what supplies the property name — and why a condition on the same
property that you wrote yourself is cleared along with the injected one.

Both conditions in that test earn their place: the first says *this property is
declared by a type assignable to the interface you named*, the second says *this
property is one the interface itself declares*. Drop the second and every
comparison on every property of a stamped entity is erased. It replaces the node
with `true` rather than removing it, so the surrounding `AndAlso` keeps two
operands and the tree stays well-formed.

## Wiring the repository

The four files above are shared substrate, copied unchanged. The code below is
project glue: it is the soft-delete portion of the five read members in whatever
`RepositoryBase<T>` the project already has, merged into what those members do
today rather than replacing them. `IRepositoryBase<T>` does not change — the
signatures are identical and no call site is touched.

```csharp
public virtual IQueryable<T> Find(Expression<Func<T, bool>>? expression = default, bool isAsNoTracking = default)
{
    expression = ApplySoftDelete(expression).HiddenObject();

    return isAsNoTracking
        ? dbContext.Set<T>().AsNoTracking().Where(expression)
        : dbContext.Set<T>().Where(expression);
}

public virtual int Count(Expression<Func<T, bool>>? expression = default)
    => dbContext.Set<T>().Count(ApplySoftDelete(expression).HiddenObject());

public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? expression = default, CancellationToken cancellationToken = default)
    => await dbContext.Set<T>().CountAsync(ApplySoftDelete(expression).HiddenObject(), cancellationToken);

public virtual bool Any(Expression<Func<T, bool>>? expression = default)
    => dbContext.Set<T>().Any(ApplySoftDelete(expression).HiddenObject());

public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>>? expression = default, CancellationToken cancellationToken = default)
    => await dbContext.Set<T>().AnyAsync(ApplySoftDelete(expression).HiddenObject(), cancellationToken);

private static Expression<Func<T, bool>> ApplySoftDelete(Expression<Func<T, bool>>? predicate)
{
    Type type = typeof(T);
    if (type.IsAssignableTo(typeof(ISoftDelete)))
    {
        ParameterExpression parameter = Expression.Parameter(type, "x");
        PropertyInfo deleteAt = type.GetProperty(nameof(ISoftDelete.DeleteAt), BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!;
        BinaryExpression condition = Expression.Equal(Expression.Property(parameter, deleteAt), Expression.Constant(null, deleteAt.PropertyType));
        return ExpressionExtension.Join(Expression.Lambda<Func<T, bool>>(condition, parameter), predicate);
    }

    return predicate ?? (_ => true);
}
```

`ApplySoftDelete` is `ISoftDelete`'s mirror of `HiddenObject` and stays private
to the repository. Chain them in that order — `ApplySoftDelete(expression)`
first, `.HiddenObject()` on the result — so both conditions AND together with
whatever the caller passed. Because neither helper returns null, `Find` always
composes a `Where`, and there is no null branch to write after the chain.

The write members are deliberately left alone: `DeleteAsync` and
`DeleteRangeAsync` still issue a real `Remove`, because entities that are
genuinely removable still exist, and stamping is the caller's decision rather
than a repository override. `GetById`/`GetByIdAsync` and the raw-SQL pair are
not wired either — they compose no predicate, so each returns marked rows, and a
caller that needs the stamp honoured uses `Find(x => x.Id == id)`.

## The entity's opt-in

```csharp
public class Order : BaseEntity, ISoftDelete, IHidden
{
    /// <summary>
    /// Order code, unique among live orders.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Set when the order is deleted; null while it is live.
    /// </summary>
    public DateTimeOffset? DeleteAt { get; set; }

    /// <summary>
    /// Set while the order is withheld from reads; null when it is visible.
    /// </summary>
    public DateTimeOffset? HiddenAt { get; set; }

    public Order Hidden(bool enable)
    {
        HiddenAt = enable ? DateTimeOffset.UtcNow : null;
        return this;
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasBaseEntity().UnderscoreTable();

        builder.HasCitextUnique(x => x.Code, ISoftDelete.SqlFilter);

        builder.HasIndex(x => new { x.CustomerId, x.Number })
            .IsUnique()
            .HasFilter(ISoftDelete.SqlFilter);
    }
}
```

The stamps are ordinary mapped properties — nothing configures them, and nothing
should. Every unique index on the entity takes the filter; an unfiltered one is
the defect that surfaces weeks later as "this code is taken" against a row nobody
can see.

## Checklist

1. The four files exist under `Infrastructure.Facades.Common.SoftDeletes`, whole.
2. `ExpressionExtension.Join` exists — see `common-extensions`.
3. `RepositoryBase<T>` applies both helpers in `Find`, `Count`, `CountAsync`,
   `Any` and `AnyAsync`; `IRepositoryBase<T>` is unchanged.
4. Each opting-in entity names the interface and declares the stamp itself —
   nothing moves to `BaseEntity`, and no `bool IsDeleted` appears anywhere.
5. Every unique index on a stamped entity carries `ISoftDelete.SqlFilter`, and a
   migration carries it.
6. Delete paths stamp and call `UpdateAsync`/`UpdateRangeAsync`; `DeleteAsync`
   appears on no stamped entity.
7. Child collections filtered inside an `Include` or aggregated inside a
   computed expression write the stamp check by hand.
