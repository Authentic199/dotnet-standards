# Anti-patterns 5–13

SKILL.md's `## Anti-patterns` carries entries 1–4: the four shapes you are most likely to
write today. These nine were measured across the corpus afterwards — each is a shape that
survived review, because each looked reasonable in its own commit. Numbering continues from
SKILL.md, so "anti-pattern 7" means one thing everywhere in this skill. Several deepen an
entry over there rather than restating it, and those cross-references are load-bearing.

**Entry 13 is a security constraint. Read it even if you skip the rest.**

## 5. A base `Common` file that has grown a feature department

```csharp
// BAD — Common/Extensions/ExpressionExtension.cs, 169 lines. The using block is the
//       whole diagnosis, before one member is declared.
using Infrastructure.Modules.FeatureA.Entities;     // five module directives,
using Infrastructure.Modules.FeatureB.Entities;     // four modules
using Infrastructure.Modules.FeatureB.Requests;
using Infrastructure.Modules.FeatureC.Entities;
using Infrastructure.Modules.FeatureD.Entities;
using Microsoft.EntityFrameworkCore;                // and the ORM, for the Include chains
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;                      // the only one the generic core needs

public static class ExpressionExtension
{
    public static Expression<Func<T, TProperty>> Join<T, TProperty>(...) { }      // ~27 lines
    public static Expression<Func<T, TProperty>> AndJoin<T, TProperty>(...) { }   // of generic
    public static Expression<Func<T, TProperty>> OrJoin<T, TProperty>(...) { }    // core
    public static Predicate<T> ToPredicate<T>(...) { }

    public static Expression<Func<T, bool>> PrioritizeOverlap<T>(BaseFeatureRequest r)
        where T : IFeatureMarker { ... }
    public static Expression<Func<T, bool>> Overlap<T>(BaseFeatureRequest r)
        where T : IFeatureMarker { ... }                     // near-copy of the above
    public static List<T> ExactlyOverlap<T>(this IEnumerable<T> sources, ...)
        { foreach (T item in sources.Where(...)) { ... } }   // a scan, not an expression
    public static IIncludableQueryable<FeatureEntityA, Other?> FeatureAInclude(
        this IQueryable<FeatureEntityA> source) => source.Include(...).ThenInclude(...);
    public static IIncludableQueryable<FeatureEntityB, Other?> FeatureBInclude(
        this IQueryable<FeatureEntityB> source) => source.Include(...).ThenInclude(...);
}

// GOOD — the same file in another corpus project: 114 lines, one using directive.
using System.Linq.Expressions;

public static class ExpressionExtension { /* the generic core, and nothing else */ }
```

Four defects rode in on one filename, and each has a different owner:

| In the file | What it actually is | Where it belongs |
|---|---|---|
| 5 module `using` directives, 4 modules | a module dependency inside the folder every module depends on | delete with the members below |
| 2 predicate builders naming a module request | feature expressions | `Modules/<Feature>/Expressions/` — `module-feature` |
| a `this IEnumerable<T>` `foreach` scan | in-memory filtering wearing an expression file's name | the feature service that needs it |
| 2 eager-load chains, 9 and 10 `Include` roots with 12 `ThenInclude` rungs each | query shape for two entities | that feature's query code — `ef-core-data-access` |

**Why:** the dependency arrow is reversed, and the file can no longer be lifted into a new
solution — which was the only reason to put it in `Common/` at all. So the next solution
writes its own `Join`, and the corpus now carries the same composition helper in four
shapes. The two eager-load chains make it worse than dead weight: an `Include` chain answers
*what does this query need loaded*, and a shared base file cannot know that, so every caller
pays for twenty-odd navigations it did not ask for. The clean variant proves nothing here
was ever needed: 114 lines, one using directive, and the **contamination was a trade, not an
addition** — the 169-line file gained three wrapper members and lost `Combine` along with
the whole `ParameterReplacer` rebinding machinery, the one genuinely hard thing in the file.
The mechanical test costs nothing and catches all four rows at once: **if the file needs a
`using` for a module namespace, it is not a base file.**
`references/expression-extension.md` ships the corrected form.

## 6. The canonical name and slot, holding a three-member stub

```csharp
// BAD — Common/Extensions/ActionContextExtension.cs: 22 lines, three members,
//       two accessor abstractions
public static class ActionContextExtension
{
    public static Guid? RouteValue(this IActionContextAccessor accessor, string idTemplate)
    { ... }                                                            // anti-pattern 7

    public static string HttpMethod(this IActionContextAccessor accessor)
        => accessor.ActionContext?.HttpContext.Request.Method ?? string.Empty;

    public static string GetIpAddress(this IHttpContextAccessor accessor)  // anti-pattern 2
        => accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
           ?? throw new BadRequestException(...);
}

// GOOD — recreate the file whole from references/action-context-extension.md:
// one accessor abstraction, the proxy-aware IP chain, both RouteValue overloads,
// the query string, the form dump, the body reader, the user agent, the platform.
```

**Why:** neither member defect is the lesson — anti-patterns 2 and 7 own those, and
re-teaching them here would double-label the same lines. The lesson is **file-level**, and
the measurement is what makes it one. This file carries two of the canon's nine members plus
a tenth the canon explicitly rejects, in twenty-two lines, where the fullest corpus variant
of the same class is a hundred and fourteen. And the same solution *already holds* a
five-member proxy-aware variant of this exact class name, seventy-five lines, in a
subsystem folder — the base slot simply never absorbed it. So the trimmed file is worse than
an absent one, because it **answers the grep**: the next developer searches
`Common/Extensions/`, finds the canonical name, sees no query-string or user-agent member,
and concludes the house has no answer. The mixed abstraction is the tell — two members reach
through the MVC action context and one through the raw HTTP context, because each arrived
from whatever the writing call site already had injected. A file assembled that way has no
contract to violate, so it never looks wrong; it just never grows the member you came for,
and the members it lacks get written again somewhere else (anti-pattern 8). SKILL.md
principle 3 is the rule it breaks: recreate the file whole, or do not create it.

## 7. Parsing a route segment with `Guid.Parse`

```csharp
// BAD — a malformed segment leaves the validator as an unhandled exception
public static Guid? RouteValue(this IActionContextAccessor accessor, string idTemplate)
{
    string? routValue = accessor.ActionContext?.RouteData?.Values[idTemplate]?.ToString();
    return routValue is null ? default(Guid?) : Guid.Parse(routValue);
}

// GOOD — absent and malformed both answer null, which is what the Guid? already promises
public static Guid? RouteValue(this IActionContextAccessor accessor, string idTemplate)
{
    string? routValue = accessor.ActionContext?.RouteData?.Values[idTemplate]?.ToString();
    return Guid.TryParse(routValue, out Guid id) ? id : null;
}
```

**Why:** the return type is `Guid?`, and the corpus reads it as one from **twelve request
files in a single solution** — every call in a validator's constructor, so it runs during
model validation, before any handler. Follow a malformed segment through the house exception
middleware: its `switch` maps the house `CustomException` base to that exception's own
status code and sends everything else to the `default:` arm, which is
`HttpStatusCode.InternalServerError` — plus a `Log.Error` carrying the base exception and an
error id the response tells the caller to quote at support. A caller who mistypes one URL
character gets a 500, and the operators get a paged error for a client mistake. The house
answer already exists twice over: the `TEnum` overload returns `null` on a value that does
not parse, and another corpus project reads the identical route key through `Guid.TryParse`.
This member is the one place that does neither. The two-line fix also removes the `is null`
branch — `TryParse` already answers `false` for a null input.

> The shipped `references/action-context-extension.md` reproduces this member in its corpus
> form and documents the hazard in its Notes. Treat that file as the transcription and this
> entry as the ruling: **write the `TryParse` form.**

## 8. A question the solution has already answered, answered again in place

```csharp
// BAD — inline in middleware: two rungs, and the middle one is missing
string? ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
    ?? httpContext.Connection.RemoteIpAddress?.ToString();

// GOOD — one call; the chain and its order live in one file
string? ipAddress = actionContextAccessor.GetRemoteIpAddr();
```

**Why:** in one corpus solution, "what is the caller's IP" has **five implementations in
four different behaviours** across eleven call sites — connection-only-and-throw in the base
`Common` extension, the full `X-Forwarded-For` → `X-Real-IP` → connection chain in a
subsystem folder, this two-rung version inlined in middleware, and a private
connection-only-and-null method duplicated byte-for-byte in two service files. The rung this
one drops is `X-Real-IP`; anti-pattern 2 carries why the chain matters.

The sharpest evidence is not the count. **One service file calls two of those answers**: the
throwing base extension at two call sites and its own private null-returning method at two
others, in the same file. Nobody chose that; it is what accretion looks like from the
inside. So the same request is recorded under different addresses depending on which path
handled it — and the middleware result above is written straight to a persisted row. No
amount of care at the call site fixes it, because the defect is that the call site is
deciding at all, and no grep for a method name finds all five. This is principle 1 in the
negative: one `grep -ril "ipaddr"` over `Common/` returns the existing chain in under a
second, and the inline version costs more lines than the call it replaces. **If a call site
genuinely cannot reach the accessor, the fix is still one implementation** — add the
overload it needs to the canonical file and route both call sites through it.

## 9. The `A-z` character class

```csharp
// BAD — reads as "any letter"; it is a contiguous code-point range that is wider
return ruleBuilder.Matches($"^[A-z0-9{acceptCharacter}]*$");

// GOOD — the corrected range, and the pattern back in its one home
return ruleBuilder.Matches(RegexExtension.AcceptedCharactersPattern(acceptCharacter));

// in RegexExtension:
private static readonly string AcceptedCharacters = $"^[A-Za-z0-9{TextToReplace}]*$";
```

**Why:** derive it from the code points rather than trusting the shape. `A` is 65, `Z` is
90, `a` is 97, `z` is 122, so `A-z` is the range 65–122 — which also spans 91 through 96:
`[`, `\`, `]`, `^`, `_` and the backtick. A rule whose entire job is *reject special
characters* therefore admits six of them, including a backslash and two bracket characters,
on exactly the inputs where a special-character rule was thought to be the control. The
defect is invisible at a glance because `A-z` reads as an abbreviation of `A-Za-z` rather
than a different set, which is why it is present at **six members across five corpus
projects**; the sixth has already corrected it in place and left a source comment saying to
use `A-Za-z`, so treat that as the house verdict rather than a local preference. Hoisting
also fixes the smaller defect underneath — a pattern literal inside a rule method is
anti-pattern 1 — and routes the caller-supplied accepted set through the escaping the canon
does at that one place, which five of the six skip entirely; the sixth takes no set at all
and hard-codes its own. See `references/regex-extension.md`, whose Notes state the limits of
that escaping.

## 10. A laxer twin of a rule, distinguished only by a vaguer name

```csharp
// BAD — same file as the strict rule; the name is the only thing that chooses
public static IRuleBuilderOptions<T, string?> IsValidPhoneNumber<T>(
    this IRuleBuilder<T, string?> ruleBuilder)
    => ruleBuilder.Matches(RegexExtension.VnPhoneNumber);   // documented prefixes, strict

public static IRuleBuilderOptions<T, string?> IsValidAllPhoneNumber<T>(
    this IRuleBuilder<T, string?> ruleBuilder)
    => ruleBuilder.Matches("^[+]?[0-9]{5,15}$");            // added later, inline, no comment

// GOOD — one rule per format, named for the format it enforces
RuleFor(request => request.PhoneNumber).IsValidPhoneNumber();

// A genuinely wider case earns a named RegexExtension field and a name that says what it
// accepts — never a comparative, and never a word like "All".
```

State this one accurately, because the accurate version is damning enough. The second rule
is **not** a rule that validates nothing: it anchors both ends, allows one optional leading
`+`, and constrains the rest to 5–15 digits, so it rejects letters, spaces and punctuation.
What it does not carry is the country-prefix and mobile-prefix structure the strict rule
documents in a comment directly above its pattern.

**Why:** the defect is the **pair**, and what the pair does to the call sites. Two rules for
one concept sit in one file, told apart only by a vague quantifier, so a call site picks by
which name sounds more general rather than by which constraint it means. In the corpus
project carrying both, the permissive one has **five callers** — including the shared
request interface in the `Common` slot that every phone-carrying request inherits from — and
the strict one, the one carrying the documented rules, **has no caller at all**. In the
other five corpus projects that same shared interface calls the strict rule. So one solution
silently dropped its phone-format policy, and the diff that did it added a method and
changed a call, with nothing anywhere saying a policy had changed. The laxer twin did not
supplement the rule; it replaced it, one validator at a time. The inline literal is
anti-pattern 1 on top. `references/validator-extension.md` carries the strict rule only, and
deliberately does not reproduce this one.

## 11. A serializer that writes with options and reads without them

```csharp
// BAD — the asymmetry is in the interface, so no call site can correct it
public interface IJsonSerializerService : ITransientService
{
    string Serialize<T>(T obj, Action<JsonSerializerOptions>? configs = null);

    T? Deserialize<T>(string text);        // nowhere to put options
}

public string Serialize<T>(T obj, Action<JsonSerializerOptions>? configs = null)
{
    JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };
    configs?.Invoke(options);
    return JsonSerializer.Serialize(obj, options);
}

public T? Deserialize<T>(string text) => JsonSerializer.Deserialize<T>(text);

// GOOD — one declaration site; both directions route through it
private static readonly JsonSerializerOptions DefaultOptions = new() { /* ... */ };

public static string Serialize<T>(T obj, Action<JsonSerializerOptions>? configs = null)
    => JsonSerializer.Serialize(obj, Options(configs));

public static T? Deserialize<T>(string text, Action<JsonSerializerOptions>? configs = null)
    => JsonSerializer.Deserialize<T>(text, Options(configs));
```

**Why:** **four of the six corpus projects** configure the write path and leave the read path
unconfigured, so this is a pattern, not one author's slip. In three of those four the
asymmetry is written into the **contract**, not one body — the interface omits the
parameter, so a call site that notices the problem has nothing to pass, and no reviewer ever
sees a missing argument. The write side names two settings, the read side names none, and
nothing in either signature says so. Whatever the unconfigured path resolves to is a second
set of rules nobody declared and nobody can grep for, and a change to one half of the round
trip silently does not reach the other. The corpus contains its own fix: one project hoists
a single `DefaultOptions` — including `PropertyNameCaseInsensitive = true`, precisely so the
read path tolerates whatever the write path chose — and both directions clone it. Do not
repair this by pasting the options literal into `Deserialize`: that is a third declaration
site, not a fix. `references/serializer-extension.md` has the round-trip consequence and the
full corrected file; SKILL.md's JSON pattern is the shape to copy.

## 12. `WaitAsync()` called without `await`

```csharp
// BAD — one keyword short of its own siblings; the method is not async, so nothing
//       at the call site or in the signature marks it
public static T Synchronize<T>(Func<T> action)
{
    SemaphoreSlim.WaitAsync();

    try
    {
        return action();
    }
    finally
    {
        SemaphoreSlim.Release();
    }
}

// GOOD — the async members in the same file take the gate correctly; make the call
//        site async and use them
public static async Task<T> SynchronizeAsync<T>(Func<Task<T>> action)
{
    await SemaphoreSlim.WaitAsync();

    try
    {
        return await action();
    }
    finally
    {
        SemaphoreSlim.Release();
    }
}
```

**Why:** the file itself is the evidence. Two async overloads immediately above write `await
SemaphoreSlim.WaitAsync();` against the same static gate, declared `new SemaphoreSlim(1,
1)`; the synchronous sibling writes the same call with the `await` dropped. Two corpus
projects carry the file and both carry the defect unchanged, which is what you expect from a
shape whose only distinguishing feature is a missing keyword — it survives every review that
reads the method in isolation and only shows up when the three overloads are read side by
side. Read them side by side. The gate is process-local either way; cross-instance locking
is `distributed-lock`.

> **Documentation-derived** — not corpus-verified. `WaitAsync` returns a task that completes
> when the permit is acquired; discarding it means execution continues without waiting, so
> the guarded action can run concurrently with another caller and the method's name promises
> an exclusion it does not provide. The `finally` then calls `Release` on a permit this
> caller may never have taken, which unbalances the count rather than restoring it. That
> failure is the worst kind to diagnose: it corrupts only under concurrency and passes every
> single-threaded test. `SemaphoreSlim` also exposes a synchronous `Wait()`; **no corpus
> file calls it**, so reach for it only where a call site genuinely cannot be made async,
> and prefer the async members above, which the corpus does carry.

## 13. A credential drawn from the shared non-cryptographic `Random`

```csharp
// BAD — a long-lived shared secret whose entropy is a process-start timestamp
public static string GenerateSecretKey(IEnumerable<string> existing)
{
    string secretKey;
    do
    {
        secretKey = PasswordExtension.Generate(new()
        {
            RequiredLength = 33,
            RequireDigit = false,
            RequireLowercase = false,
            RequireSymbols = false,
        });
    }
    while (existing.Contains(secretKey));

    return secretKey;
}

// GOOD — the helper is unchanged; what changes is what you route through it.
// A signing secret, an API key or a client secret is generated from a cryptographic
// random source, at the call site that owns the credential.
```

**This entry does not change the canon.** `RandomExtensions` and `PasswordExtension` ship
exactly as `references/random-extensions.md` and `references/password-extension.md` have
them, and both files' Notes already state the security property. What is labelled here is
the **routing decision** — which call sites may reach for them.

| Reach for `PasswordExtension.Generate` | Do not |
|---|---|
| a temporary password delivered out of band and changed on first login | a long-lived shared secret, signing key or API key |
| a short code a person reads aloud or types back | a session, reset or invitation token |
| a non-security identifier that just needs to look random | any value whose only protection is that it is hard to guess |

**Why this needs a named entry on top of that prose:** because the caveat has been read as
advisory, and it is not. Every corpus project — six of six, at line 9 of the same file —
declares one `public static readonly Random Random = new(Environment.TickCount);` shared
process-wide, and `Generate` draws every character of its output from that one field. **Nine
call sites across three projects** route through it: an account password, a request-signing
client-id and secret-key pair, two short authentication codes issued to non-human clients, a
message-broker account password, and a generated reference code.

Sort those by what the row above them permits. The account password is the **compliant**
case, and it is worth reading closely rather than flagging: the generated value is emailed
to its owner and hashed before storage, so it is a temporary password delivered out of band
— exactly what the Notes sanction. Hashing is what makes it acceptable. The signing secret
has no such step, and that is the whole difference: a shared secret must stay recoverable at
both ends for its entire life, so there is no hashing to fall back on. It is also the
weakest draw in the set — asked for 33 characters with three of its four character classes
switched off, so the whole value comes from a 26-letter alphabet. **A generated value you
can hash before storing may come from here. A generated value you must keep may not.**
