---
name: common-extensions
description: >-
  This skill should be used when reaching for a helper, utility, extension
  method or attribute in a .NET solution: regex, random string, generated
  password, IP address, JSON serialize/deserialize, Expression composition,
  reusable existence check or FluentValidation rule method; when adding to
  Infrastructure/Facades/Common; before writing an inline helper at a call
  site; or when a project lacks an extension it needs. Not for: entity
  configuration, repository base — ef-core-data-access; filter, sort,
  pagination — list-query-pipeline; S3, file keys, media — file-storage;
  Excel, import templates, zip — excel-miniexcel; typed HttpClients —
  http-client-factory; API-key filter — auth-and-security; feature-specific
  validators, expressions — module-feature.
---

## Core Principles

### 1. Search `Common/` before writing a helper

Before writing a regex, a random-string loop, a JSON round-trip, an IP getter, a
reusable predicate or a validation rule method, search the solution's
`Infrastructure/Facades/Common/` — in this order:

```
Common/Extensions/    # first, always
Common/Services/      # helpers that need something from DI
Common/Attributes/    # declarative metadata
```

Search by **capability, not filename** — filenames are an unreliable index:

```bash
grep -ril "regex\|random\|serialize\|ipaddr\|isexist" src/Infrastructure/Facades/Common/
```

The corpus carries the random helper as `RamdomExtentions.cs` in every project,
and the existence-check helper as both `ValidationExtension.cs` and
`ValidationExtention.cs`. When you grep for a name rather than a capability, grep
for both spellings — otherwise you conclude "it isn't here" and write the copy.

**Why this is mandatory and not advice:** a project's `Common/Extensions/` holds
between 13 and 31 files, so the helper is usually already one of them.
`PathExtension.cs` is byte-identical in all six corpus projects; the random helper
is byte-identical in four of six. The cost of a second copy is not the duplicated
lines — it is that the two copies drift, and a fix applied to one silently misses
every call site bound to the other.

### 2. The ladder: reuse → promote → inline

Walk the rungs in order; descend only when the rung above genuinely fails.

1. **Reuse.** It exists — call it. Extend it in place if it is nearly right. Do
   not wrap it, and do not copy one method out of it.
2. **Promote.** The moment a helper is wanted at a *second* call site, or encodes
   a rule, format or algorithm rather than one line of local glue, it moves into
   the owning `Common/Extensions/` file, generic and module-free, and every call
   site switches to it. Leaving the original copy "for now" cancels the benefit.
3. **Inline.** Last resort, for glue used once, in one method, that no other
   feature could want.

**Why the bar for rung 3 is this high:** "it's a one-off" is the rationalization
behind every duplicate in the corpus. If you are about to paste it, you are on
rung 2.

### 3. A missing extension is recreated, not improvised

A newer or smaller project frequently **lacks** an extension it needs. That is a
gap in the project, never evidence that the house has no answer.

- Recreate the file from this skill's `references/`, verbatim, at
  `Common/Extensions/<Name>.cs` — canonical name, canonical namespace.
- Bring its dependencies with it: the password generator calls the random
  extension, and the canonical rule methods read regex fields. Recreating one
  without the other reintroduces the literals.
- Do not write a trimmed "just the method I need" version. The next feature needs
  the method you trimmed, and it gets written inline.
- Never copy from, or cite a path into, another repository. The `references/`
  files are the source, already sanitized and portable.

**Why verbatim:** the canon carries decisions that are invisible at a glance — the
proxy-header chain in the IP resolver, the unique-character loop in the password
generator, the null-safe coalescing in the arithmetic combiner. A rewrite from
memory looks equivalent and behaves differently exactly where it matters.

### 4. A base `Common/` file never names a module

`Common/Extensions/` holds building blocks: type parameters, framework types,
`Core` base types. Anything naming a module entity, request, enum or `Include`
chain is a **feature** helper and belongs beside its feature —
`Modules/<Feature>/Expressions/`, or that feature's validator.

The test is mechanical: **if the file needs a `using` for a module namespace, it
is not a base file.**

**Why, measured:** one corpus variant of the expression extension is 169 lines, of
which about 27 are the generic core; the rest imports the entities and requests of
**four** business modules. The same file without them is 114 lines. Two things
break at once — every project wanting the generic `Join` inherits four modules'
compile-time dependencies, and the file can no longer be lifted into a new
solution at all, which is the only reason it lives in `Common/`.

### 5. One home per shape — extension, service, attribute

| The helper… | Ships as | Where |
|---|---|---|
| needs nothing injected | `public static class <Name>Extension` | `Common/Extensions/` |
| must resolve services or own a lifetime | interface + class, marker-typed (`IScopedService` / `ITransientService`) | `Common/Services/` |
| is declarative metadata read by reflection | `Attribute` subclass, one per file | `Common/Attributes/` |

**Why:** a static extension needs no registration, no constructor injection and no
interface, and stays callable from validators, converters and other extensions —
places where an injected dependency is simply unavailable. Making it a service
buys a DI dependency and buys nothing back. Conversely, anything that must resolve
`IValidator<T>` or manage a scope cannot be static, so it earns a service.
`Common/Attributes/` exists in all six corpus projects with a stable core — an
email-template attribute and a form-name attribute in all six, a message-display
and a not-searchable attribute in five of six. A new cross-cutting attribute joins
that folder rather than being declared next to the type that reads it.

## Patterns

Each pattern gives the shape and the decision rules; the full, ready-to-recreate
code is in the named `references/` file. Open it when a project is missing the
extension or a signature must be reproduced exactly.

| Extension | Reach for it when | Reference |
|---|---|---|
| `RegexExtension` | any named or reused pattern; whitespace or special-character scrubbing | `references/regex-extension.md` |
| `ExpressionExtension` | composing predicates, or arithmetic projections, generically | `references/expression-extension.md` |
| `SerializerExtension` | JSON round-trip with house options, or a parse that may fail | `references/serializer-extension.md` |
| `RandomExtensions` | random string, digits, upper/lower, symbols, alphanumeric | `references/random-extensions.md` |
| `PasswordExtension` | a generated password that must satisfy composition rules | `references/password-extension.md` |
| `ActionContextExtension` | client IP, user agent, platform, route value, query string, raw body | `references/action-context-extension.md` |
| `ValidationExtension` | "does this id / these ids / this code exist?" inside a validator | `references/validation-extension.md` |
| `ValidatorExtension` | a reusable FluentValidation rule method on `IRuleBuilder` | `references/validator-extension.md` |
| `ValidatorService` | validating an object the MVC pipeline never sees | `references/validator-service.md` |

### Regex: one home, one law

**A regex pattern literal never appears at a call site.** `RegexExtension` is the
single home for every pattern in the solution, and the shape is fixed:

```csharp
public static partial class RegexExtension
{
    public static readonly Regex NistPassword = NistPasswordRegex();   // the public handle
    public static readonly Regex VnPhoneNumber = VnPhoneNumberRegex();

    public static bool IsNISTPassword(this string password)            // the ergonomic call
        => NistPassword.IsMatch(password);

    [GeneratedRegex(@"...")]                                           // compile-time generation
    private static partial Regex NistPasswordRegex();
}
```

- **`static partial class` + `[GeneratedRegex]`** — the pattern is compiled at
  build time, so a malformed pattern is a compile error rather than a runtime one,
  and there is no per-call parse cost.
- **Consumers take the field, never the pattern** —
  `ruleBuilder.Matches(RegexExtension.VnPhoneNumber)`, so the pattern is reusable
  *as a value* and a correction reaches every consumer at once.
- **A pattern needing a runtime-substituted fragment** stays here as a private
  template constant with a placeholder token, plus a method that substitutes into
  it. It cannot be `[GeneratedRegex]`, and it still does not move out.
- **New pattern = new field + new partial method here**, even when only one caller
  needs it today.

The canonical set covers whitespace, a password policy, a phone number, a
country-code prefix, whole-string format assertions (digits, number-only,
identifier number, colour code) and two special-character scrub templates. Where a
corpus rule method still states a short pattern inline, the shipped `references/`
file is the corrected form with the pattern hoisted to a field — that inline case
is anti-pattern 1, not an exception to the law. **`references/regex-extension.md`**

### Expressions: generic building blocks only

The canonical file holds composition machinery and nothing else:

- **`Join(base, join, ExpressionOperator.And|Or)`** — null-tolerant: a null second
  expression returns the first unchanged, which is what makes optional filters
  composable without `if` ladders.
- **`AndJoin` / `OrJoin` / `ToPredicate`** — the wrappers over `Join`, plus the
  escape out of the expression world.
- **`Combine(Operation.Add|Subtract|Multiply|Divide, params expressions)`** —
  rebinds every lambda onto one shared parameter through a private
  `ParameterReplacer : ExpressionVisitor`, and coalesces nullable operands.
  *Why the visitor is required:* two lambdas written separately own distinct
  `ParameterExpression` instances; splicing their bodies without rebinding
  produces a tree the query provider cannot translate.
- The two enums those switches read.

Anything naming a module entity, request or business rule is a **feature
expression** — `Modules/<Feature>/Expressions/`, see `module-feature`. This is the
most-confused boundary in the facade; principle 4 is the test.
**`references/expression-extension.md`**

### JSON: a static serializer extension, not an injected service

```csharp
private static readonly JsonSerializerOptions DefaultOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    ReferenceHandler = ReferenceHandler.IgnoreCycles,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    PropertyNameCaseInsensitive = true,
};

private static JsonSerializerOptions Options(Action<JsonSerializerOptions>? configs)
{
    JsonSerializerOptions options = new(DefaultOptions);   // copy, never mutate the shared one
    configs?.Invoke(options);
    return options;
}
```

`Serialize<T>`, `Deserialize<T>` and `TryDeserialize<T>` all route through
`Options(configs)`, so **both directions read one declaration site** and a caller
can still vary one setting per call. `TryDeserialize` returns false instead of
throwing — parsing an untrusted or third-party payload is a control-flow decision,
not an exception case, and without it every call site grows a `try`/`catch`.

*Corpus divergence, stated neutrally:* the same job also exists as an injected
service. Those variants declare their options **inline in each method**, and in four
of the six the deserialize path passes no options at all — three of those omit the
parameter from the interface, so no call site can supply them. Those services write
camelCase and read with the defaults. **`references/serializer-extension.md`**

### Random values and generated passwords

`RandomExtensions` is the primitive layer: `RandomString(length, chars)`,
`RandomDigit`, `RandomUpperCase`, `RandomLowwerCase`, `RandomSymbols`,
`RandomAlphaNumericUpperCase`, and `RandomAlphabetOrSymbols(length, symbols,
params CharacterType[])`, which draws non-repeating type slots so a required mix is
actually produced. The `CharacterType` enum and the symbol sets live here too.

`PasswordExtension.Generate(PasswordOptions? opt = null)` sits on top: it places
one character of each required class (digit / lower / upper / symbol), fills until
**both** the length target and the distinct-character target are met, then
shuffles. Defaults are 8 characters and 4 unique. It calls `RandomExtensions` —
recreate that file first.
**`references/random-extensions.md`**, **`references/password-extension.md`**

### Request context: the caller's IP is a chain, not a property

`ActionContextExtension` extends `IActionContextAccessor` with the request facts
handlers and filters keep reaching for — `GetQueryString`, `RouteValue` for a
`Guid` segment, `RouteValue<TEnum>` for an enum segment (null rather than throwing
on a bad value), `GetFromForm`, `ReadBodyAsStringAsync`, `GetUserAgent`,
`GetPlatform`, `HttpMethod()` — and the IP resolver, which is **mandatory in its
full form**: `X-Forwarded-For`, then `X-Real-IP`, then
`Connection.RemoteIpAddress`, first hit wins.

```csharp
string? ip = actionContextAccessor.GetRemoteIpAddr();   // proxy-aware, in that order
```

**Why the chain:** behind a reverse proxy, load balancer or CDN,
`Connection.RemoteIpAddress` is the proxy. Every downstream use — audit rows, rate
limits, fraud signals, gateway echoes — then records one address for the entire
user base, and it fails invisibly in development where there is no proxy in front.
*Why one file:* every member here is a "reach into the current request" operation,
and scattering them is exactly what produces a second, weaker IP getter.
**`references/action-context-extension.md`**

### Existence checks and reusable validation rules

Two files, one boundary between them.

**`ValidationExtension`** — existence predicates on the repository wrapper, each
taking an optional extra filter folded in through `ExpressionExtension.Join`:

```csharp
RuleFor(request => request.EntityId)
    .Must(id => repositoryWrapper.IsExistById<Entity>(id))
    .WithMessage(MessagesType.NotFound);

RuleFor(request => request.EntityIds)
    .Must(ids => repositoryWrapper.IsExistByIds<Entity>(ids!))   // ALL of them, not any
    .WithMessage(MessagesType.NotFound);
```

`IsExistByIds` compares the distinct match count against the input count, so it
answers *every one of these exists* — a partial match is the bug it exists to
catch. There is a `TId` overload for entities not keyed by `Guid`, and
`IsExistedCode<TEntity>` for entities carrying the code marker interface. Pass a
non-null collection: the count comparison is null-guarded, the predicate it builds
is not.

**`ValidatorExtension`** — `IRuleBuilder` extensions, generic only, returning
`IRuleBuilderOptions` so they stay chainable with `.WithMessage(...)`:

```csharp
public static IRuleBuilderOptions<T, string?> IsValidPhoneNumber<T>(
    this IRuleBuilder<T, string?> ruleBuilder)
    => ruleBuilder.Matches(RegexExtension.VnPhoneNumber);   // the regex law in action
```

The canonical set: `IsValidContentType`, `NotDuplicate` / `NotDuplicateBy`,
`GreaterOrEqualTo` / `LessThanOrEqualTo` on collection counts, `IsValidPhoneNumber`
and `IsValidPassword` (both reading `RegexExtension` fields), `NotSpecialCharacter`,
`NotWhiteSpace`, `IsDigit`, `IsIdentifierNumber`, `IsNumberOnly`,
`IsValidUri(params schemes)`, and `WhenHttpMethod`, which scopes the preceding
rule to one HTTP verb.

**The line:** a rule expressed purely in type parameters and framework types
belongs here; a rule naming a module type is a feature rule and belongs in that
feature's validator — `module-feature`.
**`references/validation-extension.md`**, **`references/validator-extension.md`**

### Validating an object outside the request pipeline

`ValidatorService` is the escape hatch for an object FluentValidation's MVC
integration will never see — a row parsed out of an import file, a payload pulled
from a queue, a nested request assembled in a handler.

```csharp
public class ValidatorService(IServiceScopeFactory serviceScopeFactory) : IValidatorService
{
    public async Task ValidateAsync<T>(T instance, CancellationToken ct = default)
        where T : class
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IValidator<T> validator = scope.ServiceProvider.GetRequiredService<IValidator<T>>();
        ValidationResult result = await validator.ValidateAsync(instance, ct);
        if (!result.IsValid)
        {
            throw new BadRequestException(result.Errors[0].ErrorMessage);
        }
    }
}
```

Throwing `BadRequestException` with the **first** error is deliberate: this path
has no model state to populate, so the caller gets one actionable message and the
normal exception pipeline (`error-handling`) turns it into the standard response —
rather than a bespoke result type only this call site understands.

> **Recreate the disposing form.** Two corpus projects resolve the validator from
> a scope they never dispose — one inline, one through a `Service<T>()` extension
> used at 16 call sites. Two dispose it, as above. `references/validator-service.md`
> ships the disposing shape; see anti-pattern 4.

### Catalogue — the rest of `Common/`

Recurring files with no `references/` entry. Read the file in the project you are
in before writing anything that overlaps one of these.

| File | Purpose |
|---|---|
| `PathExtension` | separator-safe `Combine` for URL/key paths; treats a leading slash as already-rooted. Byte-identical in all six corpus projects. |
| `PropertyInfoExtension` | reflection helpers — `GetPropertyRecursive`, `GetPropertyFromExpression`, `GetPropertyRecursiveWithMaxDeep`, `IsUserDefineType`, `IsGenericCollection`. Full recreatable listing: **list-query-pipeline** `references/property-info-extension.md` |
| `TypeExtension` | `IsClass`, `IsCollection`, `IsNullableType`, `IsAssignableToGenericInterfaceOfType` — the predicates the reflection helpers lean on |
| `EnumExtension` | parse an enum from a description or string value; render a set as descriptions |
| `ConsoleExtentions` | `GetChar(countdownTime, defaultChar)` — read a key with a timeout, for console and seed paths |
| `SemaphoreSlimExtension` | `Synchronize` / `SynchronizeAsync` around a shared gate — process-local only; cross-instance locking is `distributed-lock` |
| `ServiceScopeFactoryExtension` | `Service<T>()` — resolves from a scope it **never disposes** (anti-pattern 4). Prefer `using IServiceScope scope = factory.CreateScope();`. |
| `ConfigurationExtension` | `EntityTypeBuilder<T>` helpers — table naming, key and unique-index conventions → **ef-core-data-access** |
| `RepositoryBaseExtentions` | uniqueness checks on the repository base → **ef-core-data-access** |
| `ValidatorMessageExtention` | `WithMessage(MessagesType)` — resolves the property name and looks the text up → **message-keys** |
| `BatchExtension` | chunked write loop; the corpus signatures are typed to the search client → **elasticsearch-search** |

## Anti-patterns

### 1. A regex pattern written outside `RegexExtension`

```csharp
// BAD — the pattern is now in two places and only one of them is findable
public static IRuleBuilderOptions<T, string?> IsValidPhoneNumber<T>(
    this IRuleBuilder<T, string?> ruleBuilder)
    => ruleBuilder.Matches(@"^((((\+?)84)(0{0,1})|0)(3|5|7|8|9)\d{8})$");

if (Regex.IsMatch(input, @"^[\d]*$")) { }

// GOOD — one home; call sites take the field or the rule method
    => ruleBuilder.Matches(RegexExtension.VnPhoneNumber);
if (input.IsNISTPassword()) { }
```

**Why:** when the rule changes — a new prefix, a longer minimum — a field has one
edit site and a literal has as many as `grep` can find, minus the ones it misses
because someone reformatted the pattern. The corpus carries both forms of the same
phone pattern, and they have already drifted. If the pattern has no field yet, add
the field and the partial method: that is rung 2, and it is two lines.

### 2. Reading the caller's IP straight off the connection

```csharp
// BAD — behind any proxy this is the proxy's address, for every user
public static string GetIpAddress(this IHttpContextAccessor accessor)
    => accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
       ?? throw new BadRequestException(...);

// GOOD — the header chain first, the connection last
string? ip = actionContextAccessor.GetRemoteIpAddr();
```

**Why:** it is correct on a developer machine and wrong in every deployment that
terminates TLS or balances load ahead of the app — and it is wrong *silently*,
writing a plausible-looking address into audit and security data.

### 3. A module type inside a base `Common` extension

```csharp
// BAD — Common/Extensions/ExpressionExtension.cs
using Infrastructure.Modules.FeatureA.Entities;
using Infrastructure.Modules.FeatureB.Requests;

public static class ExpressionExtension
{
    public static Expression<Func<T, TProperty>> Join<T, TProperty>(...) { }        // base

    public static Expression<Func<T, bool>> OverlapWindow<T>(FeatureRequest request) // feature
        where T : IFeatureMarker { ... }

    public static IIncludableQueryable<FeatureEntity, Other?> FeatureInclude(        // feature
        this IQueryable<FeatureEntity> source) => source.Include(...).ThenInclude(...);
}

// GOOD — the base file keeps Join and Combine; OverlapWindow and FeatureInclude
// move to Modules/FeatureA/Expressions/, next to the entities they name.
```

**Why:** the base file is the one every project copies. Once it names a module it
copies four modules with it, or does not copy at all — and the generic `Join`
everyone actually wanted becomes unreachable. Worse, the next developer opens the
file, sees feature logic, concludes this is not where composition helpers go, and
writes their own.

### 4. Resolving a service from a scope that is never disposed

```csharp
// BAD — the scope outlives the call; nothing disposes it, or anything it resolved
public static TService Service<TService>(this IServiceScopeFactory factory)
    where TService : notnull
{
    IServiceScope scope = factory.CreateScope();          // never disposed
    return scope.ServiceProvider.GetRequiredService<TService>();
}

// GOOD — the scope is bounded by the call that needed it
using IServiceScope scope = factory.CreateScope();
IValidator<T> validator = scope.ServiceProvider.GetRequiredService<IValidator<T>>();
```

**Why:** every call leaks a scope and holds everything scoped it resolved — a
`DbContext` among them — until process exit. On a hot path this is unbounded, and
it presents as gradual memory growth with no failing request to trace it back to.
The one-liner is convenient; use it only where the caller owns and disposes the
scope itself.

### 5–13. Nine measured shapes — `references/anti-patterns.md`

Base file grown a feature department (5) · canonical name held by a stub (6) · its
`Guid.Parse` route read (7) · one more IP answer in middleware (8) · `A-z` (9) · a laxer
twin rule (10) · a serializer that writes with options and reads without (11) ·
`WaitAsync()` unawaited (12) · **the security one** — a credential from `Random` (13).

## Decision Guide

| Scenario | Recommendation |
|---|---|
| About to write any helper, formatter, predicate or utility | Search `Common/Extensions/`, then `Services/`, then `Attributes/` — by capability, and for both name spellings. |
| Found it, but it is nearly right | Extend the existing file. Never copy one method out of it. |
| Not there, and a second call site wants it | Promote into the owning `Common/Extensions/` file, generic and module-free, then switch every call site. |
| Not there, and this project is simply missing a canon extension | Recreate it whole from this skill's `references/`, canonical name and namespace, with its dependencies. |
| Genuinely one-off glue, no pattern or format in it | Inline is fine — that is rung 3, not rung 1. |
| Need a regex anywhere | A field + `[GeneratedRegex]` partial in `RegexExtension`; call sites take the field. Never a literal. |
| The pattern needs a runtime-substituted fragment | Private template constant + substituting method, still inside `RegexExtension`. |
| Composing optional filters into one predicate | `ExpressionExtension.Join` — null-tolerant by design. |
| The expression names a module entity or request | Not a base extension. `Modules/<Feature>/Expressions/` — `module-feature`. |
| Serializing or deserializing JSON | Static `SerializerExtension`; `TryDeserialize` when a parse failure is expected. Not an injected serializer service. |
| Generating a code, token or password | `RandomExtensions` for the primitive; `PasswordExtension.Generate(PasswordOptions)` for a policy-bound password. |
| Need the caller's IP | `GetRemoteIpAddr()` — the header chain. Never `Connection.RemoteIpAddress` alone. |
| Checking that referenced ids exist, from a validator | `IsExistById` / `IsExistByIds` / `IsExistedCode` on the repository wrapper. |
| A validation rule expressed only in generics | `ValidatorExtension`, returning `IRuleBuilderOptions` so it stays chainable. |
| One request type serves both create and update, and a rule should apply to only one verb | `WhenHttpMethod(HttpMethod.Post, accessor)` as the last link in that rule's chain — see `references/validator-extension.md`. |
| A validation rule that names a module type | That feature's validator — `module-feature`. |
| Validating an object the MVC pipeline never sees | `ValidatorService` — resolve inside `using IServiceScope`. |
| Resolving a service from a scope by hand | `using IServiceScope scope = factory.CreateScope();` — always. |
| A new cross-cutting attribute | `Common/Attributes/`, one per file, beside the existing set. |
| Filter, sort or pagination helper | `list-query-pipeline`. |
| S3, file keys or media helper | `file-storage`. Excel, import templates, zip: `excel-miniexcel`. |
| Typed `HttpClient` helper | `http-client-factory`. API-key filter: `auth-and-security`. |
| Entity configuration or repository base helper | `ef-core-data-access`. |
| The text of a message a user sees | `message-keys`. |
| Unsure which skill owns the question | `choosing-a-dotnet-skill`. |
