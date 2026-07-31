# ExpressionExtension

**When:** composing a filter predicate from parts that may or may not be present, or folding
several numeric member selectors into one lambda.

**Why:** an optional filter written as `if (x is not null) query = query.Where(...)` scatters the
predicate across the method. `AndJoin` / `OrJoin` fold an optional predicate into a base one and
return the base unchanged when the operand is `null`, so composition stays a single expression.
`Combine` folds several member selectors into one lambda over one parameter.

> **Corrected canon.** This file merges two corpus variants.
>
> - `Join`, `Combine`, the private helpers, `ParameterReplacer` and both enums are transcribed
>   unchanged from the canonical project.
> - `AndJoin`, `OrJoin` and `ToPredicate` are merged in from a second project in the corpus.
>   **Only those three members cross over.** That variant also carries entity-typed predicate
>   builders and needs five module-namespace `using` directives to compile — module code wearing
>   a base-file name, deliberately excluded.
> - Two XML doc comments on `ExpressionOperator` were written in the team's spoken language and
>   are translated to English here. No code changed.

```csharp
using System.Linq.Expressions;

namespace Infrastructure.Facades.Common.Extensions;

public static class ExpressionExtension
{
    public static Expression<Func<T, TProperty>> Join<T, TProperty>(Expression<Func<T, TProperty>> baseExpression, Expression<Func<T, TProperty>>? joinExpression, ExpressionOperator exOperator = ExpressionOperator.And)
    {
        // Null-tolerant by design: no join operand means the base expression is returned untouched.
        if (joinExpression != null)
        {
            ParameterExpression parammeter = Expression.Parameter(typeof(T), "x");
            Expression bodyExpr = exOperator switch
            {
                ExpressionOperator.Or => Expression.OrElse(Expression.Invoke(baseExpression, parammeter), Expression.Invoke(joinExpression, parammeter)),
                ExpressionOperator.And => Expression.AndAlso(Expression.Invoke(baseExpression, parammeter), Expression.Invoke(joinExpression, parammeter)),
                _ => throw new NotSupportedException($"{nameof(ExpressionOperator)} not supported"),
            };
            baseExpression = Expression.Lambda<Func<T, TProperty>>(bodyExpr, parammeter);
        }

        return baseExpression;
    }

    // MERGED from a second project in the corpus — these three members only.
    public static Expression<Func<T, TProperty>> AndJoin<T, TProperty>(this Expression<Func<T, TProperty>> baseExpression, Expression<Func<T, TProperty>>? expression)
        => Join(baseExpression, expression, ExpressionOperator.And);

    public static Expression<Func<T, TProperty>> OrJoin<T, TProperty>(this Expression<Func<T, TProperty>> baseExpression, Expression<Func<T, TProperty>>? expression)
        => Join(baseExpression, expression, ExpressionOperator.Or);

    public static Predicate<T> ToPredicate<T>(this Expression<Func<T, bool>> expression)
    {
        return new Predicate<T>(expression.Compile());
    }

    // END of the merged members.

    public static Expression<Func<T, TProperty?>> Combine<T, TProperty>(
        Operation operation,
        params Expression<Func<T, TProperty?>>[] expressions)
        where TProperty : struct
    {
        if (expressions == null || expressions.Length == 0)
        {
            throw new ArgumentException("At least one expression is required", nameof(expressions));
        }

        var parameter = Expression.Parameter(typeof(T), "src");

        Expression? body = null;

        foreach (var expression in expressions)
        {
            // Rebind each operand onto the one shared parameter, so the result is a single
            // lambda over `src` rather than a tree of nested lambda invocations.
            var replacedBody = ReplaceParameter(expression.Body, expression.Parameters[0], parameter);

            // Ensure null-safe operation using Expression.Coalesce to handle nullable types
            var wrappedBody = Expression.Convert(replacedBody, typeof(TProperty?));

            if (body == null)
            {
                body = wrappedBody;
            }
            else
            {
                body = ApplyOperation(
                    operation,
                    Expression.Coalesce(body, Expression.Constant(default(TProperty?), typeof(TProperty?))),
                    Expression.Coalesce(wrappedBody, Expression.Constant(default(TProperty?), typeof(TProperty?))));
            }
        }

        return Expression.Lambda<Func<T, TProperty?>>(body!, parameter);
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression toReplace, ParameterExpression replaceWith)
    {
        return new ParameterReplacer(toReplace, replaceWith).Visit(expression);
    }

    private static Expression ApplyOperation(Operation operation, Expression left, Expression right)
    {
        return operation switch
        {
            Operation.Add => Expression.Add(left, right),
            Operation.Subtract => Expression.Subtract(left, right),
            Operation.Multiply => Expression.Multiply(left, right),
            Operation.Divide => Expression.Divide(left, right),
            _ => throw new InvalidOperationException("Unsupported operation"),
        };
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression toReplace;
        private readonly ParameterExpression replaceWith;

        public ParameterReplacer(ParameterExpression toReplace, ParameterExpression replaceWith)
        {
            this.toReplace = toReplace;
            this.replaceWith = replaceWith;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == toReplace ? replaceWith : base.VisitParameter(node);
        }
    }
}

public enum ExpressionOperator
{
    /// <summary>
    /// And.
    /// </summary>
    And = 1,

    /// <summary>
    /// Or.
    /// </summary>
    Or = 2,
}

public enum Operation
{
    Add,
    Subtract,
    Multiply,
    Divide
}
```

## Notes

- **`Join` is not an extension method and is deliberately null-tolerant — do not add a throwing
  guard.** Returning the base expression unchanged on a `null` operand is the whole mechanism
  behind the optional filter. Call the `AndJoin` / `OrJoin` wrappers; `Join` stays public only so
  a caller already holding an `ExpressionOperator` value can pass it through without a branch.
- **`Join`'s `TProperty` is unconstrained, but the operators it builds are boolean.**
  `Expression.AndAlso` / `Expression.OrElse` require boolean operands, so instantiating `Join`
  with any other `TProperty` fails while the tree is being built — at run time, not at compile
  time. The compiler will not stop you.
- **`Combine` rebinds; `Join` invokes.** `Combine` rewrites each operand onto one shared
  parameter through `ParameterReplacer`, so the result is a single lambda over `src`. `Join`
  wraps both operands in `Expression.Invoke`, so the result contains nested lambda invocations.
  Extend `Combine`'s approach when you add a member here.
- **`Combine`'s coalesce guard substitutes nothing, and the first operand is not coalesced at
  all.** The fallback constant is `default(TProperty?)`, which is itself `null`; only the
  accumulator and each subsequent operand pass through `Coalesce`. If you need a zero fallback,
  put it in the member selector you pass in.
- **`Combine` throws on an empty or null array**, so a caller building the operand list
  dynamically must guard the empty case itself.
- **`ToPredicate` compiles the expression** into an in-memory delegate and pays a fresh compile
  on every call. Never call it inside a loop or per request — hoist it to a static field.
- **Both enums are top-level types in this file, not nested in the class.** Anything importing
  the namespace sees `Operation` and `ExpressionOperator` unqualified. `Operation` in particular
  is a short, collision-prone name; check for an existing type of that name before dropping this
  file into a project.
- **The mechanical test for whether this file belongs in the base slot: if it needs a `using` for
  a module namespace, it is not a base file.** The corpus holds a variant of this exact file that
  fails the test — it grew entity-typed predicate builders and now imports five module
  namespaces, making the `Common` folder depend on the modules that are supposed to depend on it.
  Add generic members here; put entity-shaped predicates in the module that owns the entity.

## Dependencies and registration

- `System.Linq.Expressions` only. No package reference, no project reference.
- Static class — **no DI registration**.
