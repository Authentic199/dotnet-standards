# ValidatorExtension

**When:** the same FluentValidation check is written out longhand in more than one request validator — a content-type test, a duplicate check, a format rule.

**Why:** a named rule reads as vocabulary at the call site (`RuleFor(x => x.Phone).IsValidPhoneNumber()`) instead of a `Must` lambda the next reader has to decode, and the check has exactly one definition to correct.

Shape rules that make the file usable:

- Extend `IRuleBuilder<T, TProperty>` and return `IRuleBuilderOptions<T, TProperty>`, so the caller can keep chaining `.WithMessage(...)`, `.When(...)`.
- A **modifier** extends `IRuleBuilderOptions` instead, because `.When` only exists there. `WhenHttpMethod` is the one such member; it terminates the chain for that rule.
- Property types are nullable (`string?`, `ICollection<T>?`) and each rule makes a **deliberate** decision about null. Nullness is `NotNull()`'s job, not this file's — see the null map in the notes.
- **No rule method states pattern text.** Every pattern comes from `RegexExtension`.

> **Corrected canon.** Two deviations from the corpus form:
> 1. Every inline regex literal is replaced by a `RegexExtension` field, and `NotSpecialCharacter` calls `RegexExtension.AcceptedCharactersPattern(...)` instead of composing its pattern locally. **Recreate `references/regex-extension.md` first — this file does not compile without it.** A useful mechanical check: if `System.Text.RegularExpressions` appears in this file's using list, a pattern is still hiding in it.
> 2. `WhenHttpMethod` is restored from a second project in the corpus. It is the only generic, module-free member of that copy; the rest of it is typed to that project's entities.
>
> Deliberately not reproduced: rules typed to a specific project's request/entity types; a forward-slash variant of `NotSpecialCharacter` that was the same method with one hard-coded accepted set; a permissive `^[+]?[0-9]{5,15}$` any-country phone rule that accepts almost anything; and a DataAnnotations `Required` helper that shares the corpus file but belongs to a different validation stack.

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;

namespace Infrastructure.Facades.Common.Extensions;

public static class ValidatorExtension
{
    public static IRuleBuilderOptions<T, IFormFile?> IsValidContentType<T>(this IRuleBuilder<T, IFormFile?> ruleBuilder, ICollection<string> contentTypes)
    {
        return ruleBuilder.Must(file => file is null || contentTypes.Any(x => file.ContentType.StartsWith(x)));
    }

    public static IRuleBuilderOptions<T, IFormFile?> IsValidContentType<T>(this IRuleBuilder<T, IFormFile?> ruleBuilder, params string[] contentTypes)
    {
        return ruleBuilder.Must(file => file is null || Array.Exists(contentTypes, x => file.ContentType.StartsWith(x)));
    }

    public static IRuleBuilderOptions<T, ICollection<TProperty>?> NotDuplicate<T, TProperty>(this IRuleBuilder<T, ICollection<TProperty>?> ruleBuilder)
    {
        return ruleBuilder.Must(collections => collections != null && collections.Distinct().Count() == collections.Count);
    }

    public static IRuleBuilderOptions<T, ICollection<TProperty>?> NotDuplicateBy<T, TProperty, TBy>(this IRuleBuilder<T, ICollection<TProperty>?> ruleBuilder, Expression<Func<TProperty, TBy>> propertyLambda)
        where TProperty : class
    {
        PropertyInfo propertyInfo = propertyLambda.GetPropertyFromExpression();
        return ruleBuilder.Must(collections =>
        {
            if (collections == null)
            {
                return false;
            }

            IEnumerable<object?> byValues = collections.Select(element => propertyInfo.GetValue(element));
            return byValues.Distinct().Count() == collections.Count;
        });
    }

    public static IRuleBuilderOptions<T, ICollection<TProperty>?> GreaterOrEqualTo<T, TProperty>(this IRuleBuilder<T, ICollection<TProperty>?> ruleBuilder, int value)
    {
        return ruleBuilder.Must(collections => collections == null || collections.Count >= value);
    }

    public static IRuleBuilderOptions<T, ICollection<TProperty>?> LessThanOrEqualTo<T, TProperty>(this IRuleBuilder<T, ICollection<TProperty>?> ruleBuilder, int value)
    {
        return ruleBuilder.Must(collections => collections == null || collections.Count <= value);
    }

    public static IRuleBuilderOptions<T, string?> IsValidPhoneNumber<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Matches(RegexExtension.VnPhoneNumber);
    }

    public static IRuleBuilderOptions<T, string?> IsValidPassword<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Matches(RegexExtension.NistPassword);
    }

    /// <summary>
    /// Rejects strings containing characters outside letters, digits and an optional accepted set.
    /// </summary>
    /// <typeparam name="T"> request </typeparam>
    public static IRuleBuilderOptions<T, string?> NotSpecialCharacter<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        string? acceptCharacter = null)
    {
        // Corrected canon: the pattern was composed here (Regex.Escape + string interpolation).
        // It now lives with every other pattern; escaping is part of the builder.
        return ruleBuilder.Matches(RegexExtension.AcceptedCharactersPattern(acceptCharacter));
    }

    public static IRuleBuilderOptions<T, string?> NotWhiteSpace<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Matches(RegexExtension.NonWhitespace);
    }

    public static IRuleBuilderOptions<T, string?> IsDigit<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Matches(RegexExtension.Digits);
    }

    public static IRuleBuilderOptions<T, string?> IsIdentifierNumber<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Matches(RegexExtension.IdentifierNumber);
    }

    public static IRuleBuilderOptions<T, string?> IsNumberOnly<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Matches(RegexExtension.NumberOnly);
    }

    public static IRuleBuilderOptions<T, string?> IsValidUri<T>(this IRuleBuilder<T, string?> builder, params string[] validSchemes)
    {
        HashSet<string> schemeSet = new(validSchemes);
        return builder.Must(
            uriString =>
                !string.IsNullOrWhiteSpace(uriString) &&
                HttpUtility.UrlDecode(uriString) is { } uriDecode &&
                Uri.IsWellFormedUriString(uriDecode, UriKind.Absolute) &&
                Uri.TryCreate(uriDecode, UriKind.Absolute, out Uri? uri) &&
                (schemeSet.Count == 0 || schemeSet.Contains(uri.Scheme))
            );
    }

    /// <summary>
    /// Applies the preceding rule only for the given HTTP verb — for request types shared
    /// between create and update, where a field is required on one and optional on the other.
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty?> WhenHttpMethod<T, TProperty>(this IRuleBuilderOptions<T, TProperty?> ruleBuilder, HttpMethod httpMethod, IActionContextAccessor? actionContextAccessor, ApplyConditionTo applyConditionTo = ApplyConditionTo.CurrentValidator)
    {
        return ruleBuilder.When(_ => actionContextAccessor?.HttpMethod() == httpMethod.Method, applyConditionTo);
    }
}
```

## Notes

**Null decisions, per rule.** Getting these wrong silently changes which requests are rejected:

| Rule | Null property |
|---|---|
| `IsValidContentType` | passes — `NotNull()` is the caller's rule to add |
| `GreaterOrEqualTo`, `LessThanOrEqualTo` | pass |
| `NotDuplicate`, `NotDuplicateBy` | **fail** — a duplicate check on nothing is treated as a defect |
| every `Matches`-based rule | passes — FluentValidation skips `Matches` on null |
| `IsValidUri` | **fails** — the only rule that rejects null itself |

The asymmetry between `NotDuplicate` and the count rules is intentional, not an oversight; keep it, so callers who copy an existing validator keep getting the same answers.

- **`NotDuplicateBy` resolves the `PropertyInfo` once**, outside the `Must` predicate. Moving `GetPropertyFromExpression()` inside would re-reflect on every validated request.
- **`WhenHttpMethod` extends `IRuleBuilderOptions`, so it terminates the chain** for that rule — put it last. `ApplyConditionTo.CurrentValidator` scopes the condition to the immediately preceding rule; the alternative, `AllValidators`, reaches backwards over every rule already declared for that property, which is almost never what the call site means.
- **`WhenHttpMethod` fails open.** A null accessor makes the condition false and the rule is skipped. If the rule is a security check rather than a shape check, do not rely on this — assert the accessor is registered.

## Dependencies and registration

| Needs | Where |
|---|---|
| `RegexExtension` fields and `AcceptedCharactersPattern` | `references/regex-extension.md` — apply first |
| `GetPropertyFromExpression` | `PropertyInfoExtension` in the same namespace; signature `public static PropertyInfo GetPropertyFromExpression<T, TProperty>(this Expression<Func<T, TProperty>> expr)`, throwing `ArgumentException` when the lambda is not a property access. No reference file — if the project has no equivalent, drop `NotDuplicateBy`. |
| `IActionContextAccessor.HttpMethod()` | `references/action-context-extension.md` |

Packages: `FluentValidation`; `Microsoft.AspNetCore.Http` (`IFormFile`); `Microsoft.AspNetCore.Mvc.Core` (`IActionContextAccessor`). `System.Web` and `System.Net.Http` come from the shared framework.

`IActionContextAccessor` is **not** registered by ASP.NET Core by default — add it, or `WhenHttpMethod` silently skips every rule it guards:

```csharp
services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
```

The accessor extension is two lines if you would rather not recreate the whole file — but prefer recreating it from `references/action-context-extension.md`, which carries the rest of that file's members:

```csharp
public static string HttpMethod(this IActionContextAccessor actionContextAccessor)
    => actionContextAccessor.ActionContext?.HttpContext.Request.Method ?? string.Empty;
```

If the project has no request type shared between create and update, drop `WhenHttpMethod`, the accessor registration and the snippet together.
