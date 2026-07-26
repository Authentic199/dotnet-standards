## What this middleware is

One class, one `try`, one job: turn anything thrown downstream into a JSON
envelope and a status code. It is the only place in the system that writes an
error response body.

```csharp
public class ExceptionHandlerMiddleware
{
    // Wording is a stand-in; the shipped template is house-specific.
    private const string SupportMessageTemplate =
        "Please quote TraceId {traceId} to support for assistance.";

    private readonly RequestDelegate next;

    public ExceptionHandlerMiddleware(RequestDelegate next) => this.next = next;

    public async Task Invoke(
        HttpContext httpContext,
        IJsonSerializerService jsonSerializerService,
        IConfiguration configuration,
        IFileStorageService fileStorageService)   // only for the dedicated catch — see the last section
    {
        try
        {
            await next(httpContext);
        }
        // A second, dedicated catch precedes this one — see the last section.
        catch (Exception exception)
        {
            ErrorResultWrapper wrapper = HandleException(exception);

            if (wrapper.StatusCode >= 500)
            {
                LogErrorResultWrapper(wrapper);
            }

            if (!httpContext.Response.HasStarted)
            {
                ErrorResponseSettings? settings = configuration
                    .GetSection(nameof(ErrorResponseSettings))
                    .Get<ErrorResponseSettings?>();

                HiddenResult(wrapper, settings);
                httpContext.Response.ContentType = "application/json";
                httpContext.Response.StatusCode = wrapper.StatusCode;
                await httpContext.Response.WriteAsync(
                    jsonSerializerService.Serialize(
                        wrapper, x => x.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault));
            }
            else
            {
                Log.Warning("Can't write error response. Response has already started.");
            }
        }
    }
}
```

**Dependencies arrive on `Invoke`, not on the constructor.** A middleware is
constructed once for the lifetime of the application, so a constructor parameter
would capture one instance of a service for every request that follows — for a
transient or scoped service that is a captive-dependency bug, and the serializer
here is registered transient. ASP.NET Core resolves `Invoke`'s extra parameters
from the request scope on every call, which is the correct place for all of them:
the serializer, `IConfiguration`, and — in the shipped code — a storage service
that exists only to serve the dedicated `catch` at the end of this file.

**The general `catch` catches `Exception`.** No filter, no rethrow. Anything that
escapes the rest of the pipeline is shaped here; the middleware never decides that
an exception is "not its problem".

## The three shaping paths

`HandleException` branches once, on type:

```csharp
private static ErrorResultWrapper HandleException(Exception exception) =>
    exception is HttpCustomException customException
        ? HandleHttpCustomException(customException)
        : HandleDefaultException(exception);
```

| Thrown | Status | `Message` | Diagnostics |
|---|---|---|---|
| Any `HttpCustomException` (400, 401, 403, 423, …) | its own pinned `StatusCode` | its own `Message` | **none** |
| `InternalServerException` **carrying an inner exception** | 500 | its own `Message` | built from the **inner** exception |
| `InternalServerException` with **no** inner exception | 500 | its own `Message` | **none** |
| Anything else — a driver error, a null reference, a framework exception | 500 | **the exception's own message** | built from that exception |

The middle two rows are one `if`:

```csharp
if (customException is InternalServerException && customException.InnerException != null)
{
    ModifyErrorResultWrapper(errorResultWrapper, customException.InnerException);
}
```

**The contract between a thrown leaf and this file is two members: `StatusCode`
and `Message`.** Nothing else about a leaf is consulted to shape the response,
which is why adding a leaf is free — a new `sealed` subclass that pins a status is
matched by `exception is HttpCustomException`, its status and message are copied,
and this file needs no edit, no `catch` and no registration. A leaf that wants
more than those two members wants a middleware change, and that is the boundary
the skill body draws. (Relatedly: neither `HttpCustomException.Value` nor the BCL
`Exception.Data` dictionary is a payload channel — the body's *Adding a new
exception* section carries that trap.)

**The third row is the one to notice while debugging.** An
`InternalServerException` thrown without an inner exception is shaped like an
expected failure — 500, message, nothing else. It is still logged, because the log
gate is on status, but the log line's source, method, line and trace id are all
null. A 500 in the wild with no `traceId` is almost always this: a `catch` that
threw `InternalServerException(message)` and dropped the exception it was holding.
Pass the inner exception whenever you have one.

## The diagnostics fields

All six are written together by one method, from whichever exception the path
above selected:

| Field | Source |
|---|---|
| `TraceId` | a freshly generated sequential id, per failure — **not** the ASP.NET Core trace identifier and not a W3C `traceparent`. On the 500 paths the same value goes into the response and into the log line, and that pairing is its only job |
| `SupportMessage` | a fixed template with the trace id substituted in by literal placeholder replacement. The text lives in this file, so it is not a `Messages<T>` key |
| `Exception` | `exception.ToString()` — type, message and full stack, as one string |
| `Source` | `exception.TargetSite?.DeclaringType?.FullName` — the type whose method threw |
| `Method` | `exception.TargetSite?.Name` |
| `Line` | the first stack frame's file line number, `-1` if there is no frame |

**On the wrapped-`InternalServerException` path these describe the inner
exception**, so `Source`, `Method` and `Line` point at the code that actually
failed rather than at the `catch` block that translated it. That is the payoff for
passing the inner exception.

**A missing `line` is usually missing symbols.** The line number comes from a
stack trace requesting file information, which needs a symbol file beside the
assembly. Without one the call yields `0` rather than null — so the `-1` fallback
never fires, and because `0` is a default value the serializer omits the field
entirely. If `line` is absent where you did not redact it, look at what was
deployed before you look at this file.

## Serialization and the wire shape

The envelope goes through the shared serializer service, whose defaults are
camelCase property names and cycle-ignoring reference handling, plus one option
the middleware adds at the call site:

```csharp
x => x.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
```

**Every member still holding its default value is omitted**, so the envelope is
not a fixed-shape object with nulls in it — it is only the fields that were
actually filled. A 400 therefore looks exactly like this:

```json
{ "message": "Order not found.", "statusCode": 400 }
```

and an unredacted 500 like this:

```json
{
  "traceId": "…",
  "exception": "System.InvalidOperationException: …",
  "source": "Application.Orders.OrderService",
  "method": "CreateAsync",
  "line": 137,
  "message": "…",
  "supportMessage": "Please quote TraceId … to support for assistance.",
  "statusCode": 500
}
```

Two consequences worth holding on to:

- **A 4xx envelope has no `traceId` and no `supportMessage`.** Diagnostics are
  produced only on the two 500 paths. A caller cannot quote a reference number for
  a rejected request, because there is none — the support-ticket story exists only
  for failures the system owns.
- **`statusCode` is written into the body *and* copied onto the response**
  (`httpContext.Response.StatusCode = wrapper.StatusCode`), immediately after
  `ContentType` is set to `application/json`. Body and HTTP status can never
  disagree, because they are the same value — which is also why a leaf whose
  constructor forgot to pin `StatusCode` is so damaging: the wrapper carries `0`,
  `0` is a default so `statusCode` disappears from the body, and `0` is copied onto
  the response as its status, a value outside the valid HTTP range. Whether the
  host rejects it outright or emits it, the caller does not get the 400 the
  exception's name promised.

## Redaction — `ErrorResponseSettings`

```csharp
public class ErrorResponseSettings
{
    public List<string> HiddenProperties { get; set; } = new();
}
```

`HiddenResult` reflects over the wrapper's properties, matches each configured
name, and **sets the matched property back to its default** before serialization.
Combined with `WhenWritingDefault`, a redacted field does not appear as `null` or
`""` — it is not in the JSON at all, and a client cannot tell a hidden field from
one that was never populated.

| Environment | `HiddenProperties` | Effect |
|---|---|---|
| Base configuration | empty | nothing hidden; full diagnostics on 500 |
| Production overlay | `Source`, `Method`, `Exception`, `Line` | a 500 reaches the caller as `message`, `statusCode`, `traceId`, `supportMessage` |

The Production set leaves the caller exactly what a support conversation needs — a
sentence and a reference number — and removes everything that describes the inside
of the system.

**`Message` is never in the hidden set, and on the unexpected-exception path
`Message` is the raw exception's own text.** Redaction removes the stack and the
type name; it does not remove the sentence. Redaction cannot save you from a
driver's error string, because that string lands in the one field redaction never
touches.

**As shipped, the settings are read straight from `IConfiguration` inside the
`catch`, on every failed request, and the class is never registered with the
options pattern** the facades otherwise use — no `AddOptions`, no
`BindConfiguration`, no validation on start. Two observable consequences: a
configuration change takes effect without a restart, and a misspelled property name
is silently ignored, because the reflection lookup simply finds no match, rather
than failing at boot. Recorded as observed behaviour, not as a pattern to copy.

## Logging

```csharp
if (wrapper.StatusCode >= 500)
{
    LogErrorResultWrapper(wrapper);
}
```

**Only server-side failures are logged**, as a single error entry carrying source,
method, line, status code, trace id and the full exception text as structured
properties. Below 500 nothing is written: a rejected request is a normal outcome of
a public API, and logging every 400 as an error is how an error log becomes
unreadable. If one particular 400 matters operationally, log it where it is thrown,
with the context that made it interesting.

Note the interaction with the third shaping path: an `InternalServerException`
without an inner exception is logged with four null properties. The log line
exists; it just says nothing.

## Registration and pipeline position

The facade exposes a one-line extension, and the composition root decides where it
sits:

```csharp
public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder app) =>
    app.UseMiddleware<ExceptionHandlerMiddleware>();
```

```
static files → UseRouting → APM → CORS
  → UseExceptionHandlerMiddleware
    → UseAuthentication → current-user → JWT-verify → UseAuthorization
    → … the rest of the application
```

**Everything nested inside it is covered — for throws.** That includes the
authentication middleware, the current-user middleware, the JWT-verify middleware
(whose three `UnAuthorizedException` throws are exactly why a token belonging to a
deleted or blocked principal comes back as a proper 401 envelope), the
authorization middleware, MVC, and every handler and service beneath.

**Everything registered before it is not covered.** Static file serving, routing
itself and CORS run outside the `try`; a failure there is the host's to answer.

`facade-module-architecture` → `references/composition-root.md` owns the ordering
law. What follows is the consequence, which is this skill's.

### What the middleware never sees

Being inside the `try` protects against *throws*. Three common failures are not
throws, and no amount of middleware placement will envelope them:

- **A policy denial.** The authorization middleware does not throw; it writes a
  bare 403 and short-circuits. A response that was never an exception never becomes
  an `ErrorResultWrapper`.
- **An authentication challenge.** A missing, expired or malformed token produces a
  bare 401 the same way.
- **Automatic model-state validation.** Its 400 is produced by the Web layer's
  configured invalid-model-state factory as a plain `{ message }` object, not by
  this middleware. The skill body carries this carve-out; validator rules
  themselves belong to `module-feature`.

For the first two, **how the auth layer answers is `auth-and-security`'s story** —
including whether it should be made to answer in the envelope shape. The fact to
carry away from this file is only that it does not today, and that the reason is
short-circuiting rather than ordering.

## The dedicated catch — a defect of the shared shape

The shipped middleware has a second `catch`, placed **before** the general one:

```csharp
catch (FileUploadException exception)
{
    ErrorResultWrapper wrapper = HandleException(exception);
    LogErrorResultWrapper(wrapper);

    if (exception.AddedKeys?.Count > 0)
    {
        await fileStorageService.DeleteManyAsync(exception.AddedKeys);
    }
    // and then nothing — no response is ever written
}
```

serving an exception shaped like this:

```csharp
public sealed class FileUploadException : CustomException   // not HttpCustomException
{
    public ICollection<string>? AddedKeys { get; set; }      // a payload
}
```

Read this as **a defect of the shared shape, not one team's usage drift**: the
block is identical in both reference codebases, which is precisely how a mistake
that looks like a convention propagates.

| Cost | Detail |
|---|---|
| A dedicated `catch` | The middleware must know a specific exception type from a specific facade — the one thing the `HttpCustomException` contract exists to avoid |
| A dependency in `Invoke` | A storage service is resolved on **every request**, successful or not, so that one failure path can compensate |
| No status pin | It derives from `CustomException`, so `HandleException` sends it down the unknown-exception path and shapes it as a 500 — the name promised nothing and the type delivered nothing |
| **No response** | The block builds the envelope, logs it, deletes the uploaded files, and returns. Because it precedes the general `catch`, the request ends there and the caller gets an empty body with no envelope |
| Unconditional logging | It logs without consulting the `>= 500` gate the other path uses — a small inconsistency inside a large one |

**Compensation belongs to the operation that did the work.** The upload knows which
keys it created; it can clean them up in its own `catch` or its own disposal scope
and then throw a plain leaf — `InternalServerException(message, ex)` — which the
general path shapes, logs and *answers*, with no middleware change at all. Nothing
about undoing a side effect requires the error-shaping layer to know it happened.

## If an error response came out wrong

| Symptom | Cause |
|---|---|
| Invalid or missing HTTP status; `statusCode` absent from the body | A leaf constructor did not pin `StatusCode` |
| A 500 with no `traceId`, and a log line full of nulls | `InternalServerException` thrown without passing the inner exception |
| A 4xx with no `traceId` or `supportMessage` | By design — diagnostics exist only on the 500 paths |
| `source`, `method`, `exception`, `line` all missing in Production | The Production `HiddenProperties` overlay |
| `line` missing where nothing is redacted | No symbol file deployed beside the assembly |
| `message` is a driver or framework sentence | Unknown-exception path — throw a leaf with a written message |
| Empty body, no envelope, no obvious error | The dedicated compensating `catch` above |
| Bare 403 or 401 with no envelope | Authorization or authentication short-circuited without throwing |
| A validation failure returned only `{ "message": … }` | The invalid-model-state factory, not this middleware |
| Log warning "Can't write error response" | The response had already started; nothing could be rewritten |
| A configured hidden property has no effect | The name did not match a wrapper property — the lookup fails silently |
