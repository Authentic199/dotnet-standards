---
name: http-client-factory
description: >-
  This skill should be used when .NET code calls out over HTTP: reaching a
  third-party or integration API, any new HttpClient(), injecting
  IHttpClientSender, chaining
  UseClient/UseMethod/WithUri/WithHeaders/WithContent into SendAsync, reading
  an HttpResult, JSON, form-urlencoded or multipart via
  ToStringContent/ToFormUrlEncodedContent/ToFormDataContent, an outbound file
  upload, [FormName] flattening, an HttpClientSettings partial or
  httpclient.json, registering AddHttpClientSender or typed AddHttpClient, or
  recreating the sender facade where none exists. Not for: object storage,
  media download workflows — file-storage; inbound endpoints, DTOs —
  api-surface; exception types, ExceptionHandlerMiddleware — error-handling;
  JWT, secret storage — auth-and-security; client file placement —
  facade-module-architecture; utility extensions — common-extensions; faking
  the sender — dotnet-testing.
---

## Core Principles

### 1. The sender is the only way out of the process

Inject `IHttpClientSender`, chain the request, `await SendAsync(...)`. A call site never
constructs `HttpClient`, `HttpClientHandler` or `HttpRequestMessage` itself. Across the
reference solutions there is **not one** `new HttpClient(...)` outside the facade — the
facade's own instance is the only one in the process:

```csharp
private static readonly HttpClient DefaultHttpClient = new(new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
});
```

Why: one instance is one connection pool with a bounded DNS lifetime. Per-call construction
brings back socket exhaustion and stale DNS, and it drops the request/response logging and the
`Duration` timing the facade already does for every call.

### 2. The chain is the whole API — there are no verb shortcuts

`UseClient` -> `UseMethod` -> `WithUri` -> `WithHeaders` -> `WithContent` -> `SendAsync`. Every
builder method returns `IHttpClientSender`, so order is free; only `SendAsync` is terminal.

There is no `GetAsync`, `PostAsync` or `PostJsonAsync` here. The verb is data:
`UseMethod(HttpMethod.Post)`. The builder's own default is `Get`, so a plain GET could omit
`UseMethod` — state it anyway, because the default is invisible at the call site.

`WithUri` is the one mandatory link: `Build()` throws
`InvalidOperationException("URI must be specified.")` without it. `WithUri(string)` wraps its
argument in `new Uri(...)`, so it needs an **absolute** URL — including when a typed client
already carries a `BaseAddress`. The relative-path-plus-`BaseAddress` idiom that works with a
plain `HttpClient` throws `UriFormatException` here.

### 3. The sender returns; it does not throw

`SendAsync` catches every transport exception and returns an `HttpResult` whose `StatusCode`
is `InternalServerError`, with the exception logged and the request attached.
`ReadAsStringAsync`, `ReadFromJsonAsync<T>`, `ReadAsStreamAsync` and `ReadAsByteArrayAsync`
behave the same way — catch, log, set `500`, return `default`.

The rule that follows: **check the status before reading the body.** A `null` out of
`ReadFromJsonAsync<T>` means "no usable response", never "the API sent null" — a transport
failure, a deserialization failure and a legitimately empty body are indistinguishable
afterwards. Why the facade is built this way: an outbound failure is data, not control flow, so
a socket error cannot escape into the inbound request pipeline; the price is that the caller,
not the compiler, has to notice. Dispose the result — it derives from `HttpResponseMessage`.

### 4. The builder is instance state that outlives the call

The sender holds one `RequestBuilder` as a field and nothing on the send path resets it. Three
consequences, all of which bite a client that injects the sender once and calls it from several
methods:

- Headers are written into a dictionary by name. Re-setting the same name overwrites it, but a
  name only the *first* call set is still there on the second.
- `Uri`, `Method` and `Content` carry over. A chain that forgets `WithUri` does not throw — it
  silently reuses the previous call's URI.
- `UseClient` sets both the custom client **and** `UseLogging = false`, and both stick. Once any
  chain on an instance passes a typed client, later chains on that instance keep using it and
  stay silent.

Registration is `AddTransient`, so each *resolution* starts clean. Set method, URI, headers and
content explicitly on every call rather than relying on what the last one left behind.

### 5. Content is built by an extension, never by hand

`ToStringContent()` for JSON, `ToFormUrlEncodedContent()` for
`application/x-www-form-urlencoded`, `ToFormDataContent()` for `multipart/form-data`. All three
take the request object and flatten it: nested objects become dotted paths,
`[FormName("wire_name")]` overrides the property name, and an `IFormFile` becomes
`StreamContent` carrying the file's own `ContentType` and `FileName`.

Why: hand-assembled `MultipartFormDataContent` and string-concatenated form bodies are where
field names drift away from the third party's contract. The flattener makes the wire name a
declared, reviewable part of the request type instead of a string literal at the call site.

### 6. Every integration contributes its own settings partial

`HttpClientSettings` is a `partial class` that itself declares no properties. Each integration
adds one file holding a partial with its own section property plus that section's settings
class, bound and validated from one root at startup. Nothing — scheme, host, route, client id,
key — is a literal at the call site.

Why: adding an integration is one new file and one property, with no edit to a shared class and
no merge conflict between integrations; and `ValidateOnStart()` turns a missing key into a
startup failure instead of a 500 the first time the endpoint is hit in production.

### 7. Where the facade is missing, recreate it — never improvise one

A project with no `Facades/Common/HttpClients` folder gets the facade rebuilt from this skill's
`references/` files, unchanged, before the first outbound call is written. Never inline a
private sender, a private content builder or a private flattener next to the consumer that
needs it.

Why: the facade is a house-wide contract, and a bespoke copy diverges from it silently. The
divergences are exactly the behaviours callers rely on — return-don't-throw, `Duration`, the
`500`-on-failure mapping, header hyphenation — so every consumer written against the copy has
to be re-read when the real facade arrives.

## Patterns

### The default-client call

Inject `IOptions<HttpClientSettings>` and `IHttpClientSender`, build an absolute URI from the
settings section, chain, check, read.

```csharp
public sealed class ThirdPartyApiClient : IThirdPartyApiClient
{
    private static readonly JsonSerializerOptions SerializeOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ThirdPartyApiSettings settings;
    private readonly IHttpClientSender sender;

    public ThirdPartyApiClient(IOptions<HttpClientSettings> options, IHttpClientSender sender)
    {
        settings = options.Value.ThirdPartyApi;
        this.sender = sender;
    }

    public async Task<EntityResponse?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using HttpResult result = await sender
            .UseMethod(HttpMethod.Get)
            .WithHeaders(new Dictionary<string, string>
            {
                [HeaderNames.Authorization] = $"Bearer {settings.AccessKey}",
            })
            .WithUri(BuildUri($"{settings.EntityRoute}/{id}"))
            .SendAsync(cancellationToken);

        if (!result.IsSuccessStatusCode)
        {
            Log.Error("{client} failed with {status}", nameof(ThirdPartyApiClient), result.StatusCode);
            return default;
        }

        return await result.ReadFromJsonAsync<EntityResponse>(SerializeOptions, cancellationToken);
    }

    private string BuildUri(string route)
        => UriHelper.BuildAbsolute(settings.Scheme, new HostString(settings.Host), route);
}
```

A query string is built into that URI before `WithUri` — `UriHelper.BuildAbsolute(..., query:
QueryString.Create(...))` or a `UriBuilder`. The sender has no query affordance of its own.

### The typed client, when the integration has a fixed base address

Register a typed client so the base address comes from settings once, then hand that
`HttpClient` to the sender with `UseClient`.

```csharp
internal static IServiceCollection AddThirdPartyApiClient(this IServiceCollection services)
{
    services.AddScoped<IThirdPartyApiClient, ThirdPartyApiClient>();
    services.AddHttpClient<IThirdPartyApiClient, ThirdPartyApiClient>((provider, cfg) =>
    {
        ThirdPartyApiSettings setting = provider
            .GetRequiredService<IOptions<HttpClientSettings>>().Value.ThirdPartyApi;

        cfg.BaseAddress = new Uri(UriHelper.BuildAbsolute(setting.Scheme, new HostString(setting.Host)));
    });

    return services;
}
```

Both registrations are present: the scoped service is what the rest of the code injects, and
`AddHttpClient` configures the `HttpClient` handed to its constructor. The consumer injects
**both** that typed `HttpClient` and `IHttpClientSender`, and opens the chain with
`.UseClient(httpClient)` ahead of the usual links.

The URI is still absolute and still built from the same settings section, even though the typed
client has a `BaseAddress`. Note also that `UseClient` silences the sender's own
request/response logging (Principle 4) — a typed client that needs a log writes its own.

### Headers

`WithHeaders(object headers, bool replaceUnderscoreWithHyphen = true)` accepts an anonymous
object, an `IDictionary`, any key/value collection, or a query-string-shaped `string`. By
default every parsed name has its underscores replaced with hyphens — whatever the source — so
an anonymous object can express header names C# identifiers cannot:

```csharp
.WithHeaders(new { x_api_key = settings.AccessKey })          // sent as x-api-key
.WithHeaders(dictionary, replaceUnderscoreWithHyphen: false)  // names kept verbatim
```

Names that are null or whitespace and values that are null are skipped, and headers go on with
`TryAddWithoutValidation`, so a third party's non-standard header name still goes out unaltered.

### JSON, form-urlencoded, multipart

```csharp
.WithContent(request.ToStringContent())                            // JSON, UTF-8
.WithContent(request.ToFormUrlEncodedContent())                    // names as declared
.WithContent(request.ToFormUrlEncodedContent(useSnakeCase: true))  // client_id, grant_type, ...
.WithContent(request.ToFormDataContent())                          // multipart; the only one that takes files
```

The request type is an ordinary class or record:

```csharp
public sealed class UploadEntityRequest
{
    [FormName("entity_code")]
    public string Code { get; set; } = default!;

    public IFormFile? Attachment { get; set; }

    public ICollection<string>? Tags { get; set; }
}
```

`ToFormDataContent()` walks it: `Attachment` becomes `StreamContent` with its own `ContentType`
and `FileName` (files inside a collection too), each `Tags` element goes under one field name,
and `Code` is sent as `entity_code`. `useSnakeCase` runs each flattened path through
`Underscore()` (Humanizer); `[FormName]` still wins where it is declared.

Both form builders reject a top-level `IEnumerable` with `NotSupportedException` — pass a
request object with a named collection property, not a bare collection.

**Read `references/content-extensions.md` when** a payload does not land on the wire in the
shape the third party expects, when you need the flattening rules for nested objects and
collections, or when recreating the facade.

### Credentials that travel in the payload

When the third party wants its client id and secret **in the body** rather than in a header, put
them on the request type and fill them from the settings section immediately before sending —
never from a literal, and never mapped in from an inbound DTO:

```csharp
public void AttachCredentials(ThirdPartyApiSettings settings)
{
    ClientId = settings.ClientId;
    ClientSecret = settings.ClientSecret;
    GrantType = settings.GrantType;
}
```

Attaching an outbound credential is this skill's concern. How the secret is stored, and how
inbound tokens are validated, is not — `auth-and-security`.

### The settings partial an integration contributes

```csharp
public partial class HttpClientSettings
{
    public ThirdPartyApiSettings ThirdPartyApi { get; set; } = new();
}

public class ThirdPartyApiSettings : IValidatableObject
{
    public string Scheme { get; set; } = default!;

    public string Host { get; set; } = default!;

    public string EntityRoute { get; set; } = default!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required();
}
```

The configuration file nests every section under one `HttpClientSettings` root:

```json
{
  "HttpClientSettings": {
    "ThirdPartyApi": { "Scheme": "https", "Host": "api.example.test", "EntityRoute": "/v1/entities" }
  }
}
```

`validationContext.Required()` is a house validation helper, not framework API;
`references/registration-and-settings.md` carries it, the full settings shape, and where the
file sits in the configuration load order.

### Registration

```csharp
internal static class Startup
{
    internal static IServiceCollection AddHttpClientSender(this IServiceCollection services)
    {
        services.AddTransient<IHttpClientSender, HttpClientSender>();
        return services;
    }

    internal static IServiceCollection AddClientSetting(this IServiceCollection services)
    {
        services
            .AddOptions<HttpClientSettings>()
            .BindConfiguration(nameof(HttpClientSettings))
            .ValidateDataAnnotationsRecursively()
            .ValidateOnStart();

        return services;
    }
}
```

Three things this code does not show:

- **The composition root calls both.** `AddHttpClientSender()` registers the sender and nothing
  else. A project that wires it alone gets a working sender over an unbound settings root — every
  section silently empty, and `ValidateOnStart` never runs.
- **`ValidateDataAnnotationsRecursively()` is not in the BCL.** It comes from the
  `ReHackt.Extensions.Options.Validation` package (7.0.1 in the reference solution). Without it
  the registration does not compile, and the BCL's `ValidateDataAnnotations()` is not a drop-in
  substitute, because the per-integration sections are nested objects.
- **`AddTransient`, not singleton.** The builder is instance state (Principle 4) — a shared instance would interleave concurrent chains.

The `HttpResponseMessage` -> `HttpResult` map is an AutoMapper profile shipped inside the
facade; profile placement and `CreateMap` mechanics belong to `automapper-mapping`.

### Branching on the result

```csharp
if (result.StatusCode == HttpStatusCode.BadRequest)
{
    // the third party's own error body, read as its documented shape
    return await result.ReadFromJsonAsync<ErrorPayload>(SerializeOptions, cancellationToken);
}
```

Everything else falls through to the general failure branch shown in *The default-client call*.
A transport failure also arrives as `500`, so a `500` branch means "the call did not succeed",
not "the remote server errored". Where that distinction matters, subscribe to
`HttpResult.OnError` — it fires only for an actual exception, and replaces the facade's default
error logging for that result. `result.Duration` carries the measured round trip.

Turning any of this into a response for *your own* caller — which exception, what the client
sees — is `error-handling`.

### A binary response

```csharp
// after the usual status check
await using Stream? stream = await result.ReadAsStreamAsync(cancellationToken);
```

Fetching the bytes is this skill's. What happens next — the bucket, the key, the retention, the
download workflow — is `file-storage`.

## Anti-patterns

### 1. Constructing a client at the call site

```csharp
// BAD - a new pool, no logging, no Duration, no shared handler
using HttpClient client = new();
HttpResponseMessage response = await client.PostAsync(url, content, cancellationToken);
```

```csharp
// GOOD
using HttpResult result = await sender
    .UseMethod(HttpMethod.Post)
    .WithUri(url)
    .WithContent(payload.ToStringContent())
    .SendAsync(cancellationToken);
```

Beyond style: the two do not fail the same way. The first throws `HttpRequestException` into
whatever pipeline is above it; the second returns a `500` result the caller must handle.

| Rationalization | Reality |
|---|---|
| "It's one call, the facade is overkill" | The facade is one constructor parameter and four chained calls. There is no smaller version. |
| "I made it `static readonly`, so no socket exhaustion" | Pooling was never the only reason. Logging, `Duration`, the uniform result and the settings root all disappear with it. |
| "The sender doesn't support what I need" | Then the gap is in the facade, and it gets fixed there — for every caller, once. |
| "I need a delegating handler, a timeout or a proxy" | Register a typed client and pass it with `UseClient`. That is what typed clients are for. |

**Red flags — stop and re-read this skill:** the token `new HttpClient` outside the facade;
`HttpResponseMessage` as a local at a call site; `client.PostAsync` / `GetStringAsync`;
`IHttpClientFactory.CreateClient()` in a service.

### 2. A bespoke sender or content builder next to the consumer

```csharp
// BAD - a private copy of the facade, one integration deep
MultipartFormDataContent content = new();
content.Add(new StringContent(request.Code), "entity_code");
content.Add(new StreamContent(request.Attachment.OpenReadStream()), "attachment", request.Attachment.FileName);
```

```csharp
// GOOD - recreate the facade from references/, then use it
.WithContent(request.ToFormDataContent())
```

Hand-built content drifts from the flattener: nested objects flatten one way here and another
way there, `[FormName]` stops being the single source of the wire name, and the file's content
type is silently dropped. A missing facade is a reason to add the facade, not to write around it.

### 3. Reading the body before checking the status

```csharp
// BAD - null here means "failed", "empty" or "not JSON", and all three look identical
EntityResponse? entity = await result.ReadFromJsonAsync<EntityResponse>(cancellationToken: cancellationToken);
return entity!.Id;
```

The readers swallow their exceptions by design (Principle 3). The corrected shape is the one in
*The default-client call*: check `IsSuccessStatusCode`, log, return, and only then read.

### 4. Hard-coded hosts, routes and keys

```csharp
// BAD
.WithUri("https://api.example.test/v1/entities")
.WithHeaders(new { x_api_key = "..." })
```

```csharp
// GOOD - a settings partial, bound and validated at startup
.WithUri(BuildUri(settings.EntityRoute))
.WithHeaders(new { x_api_key = settings.AccessKey })
```

A literal host also defeats `ValidateOnStart()`: the misconfiguration that should have stopped
the process at boot instead surfaces as a failed call in production.

### 5–8. Four more, in `references/anti-patterns.md`

A blocking body read in a result constructor (5); a multipart collection loop that tests the
collection instead of the item (6); a long-lived consumer holding one sender with a `With…` link
inside an `if` (7); a reader's return value used with no branch for what it returns on failure
(8). Read them when auditing an existing call or a hand-maintained copy of the facade.

## Retry, timeout and the facade boundary

The facade has **no** retry, timeout or circuit-breaker policy, and none gets added to it. Where
those settings exist in the reference solutions they are properties of one integration's own
settings section — a retry count, a delay in milliseconds, a timeout in seconds — read by that
integration's own client code, never by the sender.

So: if a call needs retry semantics, they are the integration's, they are configured in that
integration's settings section alongside its host and routes, and they are implemented in that
client. Do not assume a call already has them.

## Decision Guide

| Situation | Do this |
|---|---|
| Which content builder? | JSON → `ToStringContent()`. Form fields only → `ToFormUrlEncodedContent()` (`useSnakeCase: true` for OAuth-shaped). Any file → `ToFormDataContent()` with `IFormFile` properties |
| A wire name the third party spells differently | `[FormName("wire_name")]` on the property |
| A GET, with or without query parameters | `UseMethod(HttpMethod.Get)`, no `WithContent`; build the query into the URI before `WithUri` |
| One stable host, long-lived integration | Typed `AddHttpClient<TInterface,TImpl>` + `.UseClient(httpClient)` |
| One-off or dynamically addressed call | Default client — chain without `UseClient` |
| Host, routes, credentials for a new integration | An `HttpClientSettings` partial in the integration's own folder + a section in `httpclient.json` |
| `InvalidOperationException: URI must be specified.` | `WithUri` was never called on this chain |
| A second call on one injected sender behaving oddly | Builder state carried over (Principle 4) — set method, URI, headers and content every call |
| Boot failed on registration | `ValidateOnStart` rejected a section, or the `ReHackt.Extensions.Options.Validation` package is missing |
| Retry or timeout needed | The integration's own settings section and its own client code — not the facade |
| The project has no `HttpClients` facade | Recreate it from `references/`, then write the call. Never a bespoke copy |
| Storing what you downloaded, object storage, a media workflow | `file-storage` |
| Inbound endpoint, controller, response envelope | `api-surface` |
| Which exception to throw when the call fails | `error-handling` |
| Storing a secret, validating an inbound token | `auth-and-security` |
| Where the client file and its facade folder go | `facade-module-architecture` |
| A general-purpose extension with no HTTP in it | `common-extensions` |
| Faking the sender in a test | `dotnet-testing` |

## References

Open these when writing the facade itself, not when calling it.

| File | Open it when |
|---|---|
| `references/sender-and-result.md` | Recreating `IHttpClientSender`, `HttpClientSender`, `HttpResult` or `HttpResultProfile`, or checking exactly what the sender does with an exception, a header or the builder |
| `references/content-extensions.md` | Recreating `HttpClientExtensions`, `HttpPropertyFlattener` or its support set (`PropertyFlatten`, `PropertyFlattener`, `PropertyFlattenOptions`, `FormNameAttribute`, the collection-type reflection helpers), or debugging how a nested property became a wire field name |
| `references/registration-and-settings.md` | Wiring `AddHttpClientSender` / `AddClientSetting`, adding an integration's settings partial, registering a typed client, or laying out the settings file and its validation helper |
