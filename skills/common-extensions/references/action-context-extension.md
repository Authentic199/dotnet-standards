# ActionContextExtension

**When:** any code below the controller needs a fact about the request currently in flight — the
caller's IP, the HTTP method, a route value, the query string, the raw body, the user agent, the
client platform.

**Why:** every member here is a **reach-into-the-current-request operation**. Threading
`HttpContext` down through service signatures to obtain one of these facts couples the whole call
chain to ASP.NET Core; injecting `IActionContextAccessor` and calling one extension keeps the
coupling on the single line that needs it. Injecting the accessor also makes that reach visible in
the constructor, which is the honest signal that the code cannot run outside a request.
`GetRemoteIpAddr` is the reason the file exists: behind a reverse proxy,
`Connection.RemoteIpAddress` is the proxy, not the client.

> **Corrected canon.** This file is the union of two corpus variants.
>
> - `GetRemoteIpAddr`, `GetQueryString`, `RouteValue<TEnum>`, `GetFromForm`,
>   `ReadBodyAsStringAsync`, `GetUserAgent` and `GetPlatform` come from the canonical project's
>   fullest variant. **In the corpus that variant lives under a module's `Extensions/` folder.**
>   Nothing in it is module-specific, so this skill prescribes
>   `Infrastructure/Facades/Common/Extensions/` as its home going forward and normalizes the
>   namespace accordingly.
> - `HttpMethod()` and the `Guid?` `RouteValue` overload are merged in from a second project in
>   the corpus, whose file also carries a bare `Connection.RemoteIpAddress` accessor that is
>   **not** carried over — the proxy-aware chain below is this file's mandatory form.
>   `HttpMethod()` is not optional: `references/validator-extension.md` calls it.
> - Two `using` directives from the source file are dropped: one imported a code-analysis
>   namespace that nothing in the file uses, and one imported the file's own namespace.

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

// CORRECTED CANON: namespace normalized; this file belongs in the Common slot.
namespace Infrastructure.Facades.Common.Extensions;

public static class ActionContextExtension
{
    // Chain order is load-bearing: X-Forwarded-For, then X-Real-IP, then the socket peer.
    // First hit wins.
    public static string? GetRemoteIpAddr(this IActionContextAccessor actionContextAccessor)
    {
        HttpContext? httpContext = actionContextAccessor.ActionContext?.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        string? result = string.Empty;

        if (httpContext.Request.Headers != null)
        {
            StringValues forwardedHeader = httpContext.Request.Headers["X-Forwarded-For"];
            if (
                !string.IsNullOrEmpty(forwardedHeader)
                && forwardedHeader.FirstOrDefault() is string forwardIp
                )
            {
                return forwardIp;
            }

            if (
                string.IsNullOrEmpty(result)
                && httpContext.Request.Headers.TryGetValue("X-Real-IP", out StringValues xRealStringValue)
                && xRealStringValue.FirstOrDefault() is string xRealIp)
            {
                return xRealIp;
            }
        }

        if (string.IsNullOrEmpty(result) && httpContext.Connection.RemoteIpAddress != null)
        {
            return httpContext.Connection.RemoteIpAddress.ToString();
        }

        return result;
    }

    public static string? GetQueryString(this IActionContextAccessor actionContextAccessor)
        => actionContextAccessor.ActionContext?.HttpContext.Request.QueryString.Value;

    public static TEnum? RouteValue<TEnum>(this IActionContextAccessor actionContextAccessor, string enumTemplate)
        where TEnum : struct, Enum
    {
        string? routValue = actionContextAccessor.ActionContext?.RouteData?.Values[enumTemplate]?.ToString();

        if (
            routValue is null
            ||
            !EnumExtension.TryGetFromString(routValue, out TEnum? result)
            )
        {
            return null;
        }

        return result;
    }

    // MERGED from a second project in the corpus: this overload and HttpMethod below.
    public static Guid? RouteValue(this IActionContextAccessor actionContextAccessor, string idTemplate)
    {
        string? routValue = actionContextAccessor.ActionContext?.RouteData?.Values[idTemplate]?.ToString();
        return routValue is null ? default(Guid?) : Guid.Parse(routValue);
    }

    public static string HttpMethod(this IActionContextAccessor actionContextAccessor)
        => actionContextAccessor.ActionContext?.HttpContext.Request.Method ?? string.Empty;

    // END of the merged members.

    public static string? GetFromForm(this IActionContextAccessor actionContextAccessor)
    {
        if (actionContextAccessor.ActionContext?.HttpContext.Request.HasFormContentType != true
            ||
            actionContextAccessor.ActionContext?.HttpContext.Request.Form.Count == 0)
        {
            return null;
        }

        return string.Join('&', actionContextAccessor.ActionContext?.HttpContext.Request.Form.Select(x => x.Key + '=' + x.Value)!);
    }

    public static async Task<string?> ReadBodyAsStringAsync(this IActionContextAccessor actionContextAccessor)
    {
        Stream? bodyStream = actionContextAccessor.ActionContext?.HttpContext.Request.Body;
        if (bodyStream == null)
        {
            return null;
        }

        using StreamReader reader = new(bodyStream);
        return await reader.ReadToEndAsync();
    }

    public static string? GetUserAgent(this IActionContextAccessor actionContextAccessor)
    {
        HttpContext? httpContext = actionContextAccessor.ActionContext?.HttpContext;
        if (httpContext?.Request.Headers is null)
        {
            return null;
        }

        return httpContext.Request.Headers["User-Agent"];
    }

    public static string? GetPlatform(this IActionContextAccessor actionContextAccessor)
    {
        HttpContext? httpContext = actionContextAccessor.ActionContext?.HttpContext;
        if (httpContext?.Request.Headers is null)
        {
            return null;
        }

        return httpContext.Request.Headers["sec-ch-ua-platform"];
    }
}
```

## Notes

- **`GetRemoteIpAddr` returns `string.Empty`, not `null`, when no source yields an address.**
  `result` is initialized to `string.Empty` and never reassigned, so every path that is not a hit
  falls through to `return result`. A caller testing `is null` misses that case — test with
  `string.IsNullOrEmpty`.
- **It returns the whole `X-Forwarded-For` header value, not the first hop.** `FirstOrDefault()`
  picks the first *header value*, and a proxy chain conventionally appends addresses
  comma-separated inside one value. Behind two or more proxies, expect a comma-separated list;
  split at the call site if you need a single address.
- **`X-Forwarded-For` is client-supplied.** Trusting it is only safe when every route into the
  application passes through a proxy you control. Treat the result as diagnostic — do not gate
  authorization, rate limiting or allow-listing on it.
- **`ReadBodyAsStringAsync` consumes the request body once and does not rewind it.** The
  `StreamReader` is created without `leaveOpen` and nothing enables buffering, so after it runs,
  model binding and downstream middleware read an empty body. Call it from a place that owns the
  body — a logging filter that runs after binding, or an endpoint that binds nothing.
- **The two `RouteValue` overloads disagree on bad input, and resolve by call shape.** The `TEnum`
  overload returns `null` when the value does not parse; the `Guid` overload calls `Guid.Parse`
  and throws. `accessor.RouteValue("id")` resolves to the `Guid?` one because `TEnum` cannot be
  inferred — always write `RouteValue<TSomeEnum>("...")` explicitly.
- **Everything here answers `null` (or `string.Empty`) rather than throwing when there is no
  ambient request.** Calling any of it from a background job, a hosted service or a message
  consumer fails silently with a wrong answer, not loudly. For work that outlives the request,
  read the value at the edge and pass it explicitly.
- **`GetFromForm` is a diagnostic dump, not a parser.** It joins every field and value raw, with
  no URL-encoding. Do not log its result on an endpoint that accepts credentials or personal data.

## Dependencies and registration

**Registration is mandatory** — `IActionContextAccessor` is not registered by `AddControllers()`.
Every corpus project that uses this file registers it in the composition root:

```csharp
services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
```

**`RouteValue<TEnum>` needs a companion helper.** In the corpus it calls an `EnumExtension` that
lives in a *module* namespace — which a base file may not import (see the mechanical test in
`references/expression-extension.md`). The Common-slot `EnumExtension` in the corpus does not
carry this member. If your project's `EnumExtension` does not have it, add exactly this, in the
base slot beside this file:

```csharp
// Infrastructure/Facades/Common/Extensions/EnumExtension.cs
namespace Infrastructure.Facades.Common.Extensions;

public static class EnumExtension
{
    public static bool TryGetFromString<TEnum>(string value, out TEnum? result)
        where TEnum : struct, Enum
    {
        if (int.TryParse(value, out int intValue) && Enum.IsDefined(typeof(TEnum), intValue))
        {
            result = (TEnum)Enum.ToObject(typeof(TEnum), intValue);
            return true;
        }

        if (Enum.IsDefined(typeof(TEnum), value)
            && Enum.TryParse(typeof(TEnum), value, out object? resultObj)
            && resultObj is TEnum resultEnum)
        {
            result = resultEnum;
            return true;
        }

        result = null;
        return false;
    }
}
```

**Framework access.** `IActionContextAccessor` lives in `Microsoft.AspNetCore.Mvc.Infrastructure`.
The corpus project hosting this file is a plain `Microsoft.NET.Sdk` class library with no explicit
`FrameworkReference` — the ASP.NET Core types arrive transitively through an ASP.NET Core package
it already references. A library referencing none would need
`<FrameworkReference Include="Microsoft.AspNetCore.App" />` instead; that situation does not occur
in the corpus.

**Dependents.** `references/validator-extension.md` calls `HttpMethod()`. Recreate this file first.
