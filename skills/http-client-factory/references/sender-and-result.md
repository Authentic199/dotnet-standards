# The sender and its result

Full source for the outbound HTTP facade's core: `IHttpClientSender`,
`HttpClientSender` (with its private `RequestBuilder` and key/value parsing helpers),
`HttpResult` and `HttpResultProfile`.

Everything below is one file in the reference solutions:
`Infrastructure/Facades/Common/HttpClients/HttpClientSender.cs`. Recreate it as one file.

## Contents

| Section | What it holds |
|---|---|
| [Behavioural contract](#behavioural-contract) | What a caller must know before using or debugging the sender |
| [Dependencies](#dependencies) | Packages and types this file needs |
| [Packages for the whole facade](#packages-for-the-whole-facade) | The four package references |
| [HttpClientSender.cs](#httpclientsendercs) | The complete file — interface, sender, builder, result, profile |
| [Notes for a project that already has a sender](#notes-for-a-project-that-already-has-a-sender) | The older lineage, and style notes |

## Behavioural contract

These are properties of the code below, not of `HttpClient`. A reader recreating the facade
must keep them; a reader debugging a call needs them spelled out.

1. **The sender never throws from `SendAsync`.** Every exception is caught and returned as an
   `HttpResult` with `StatusCode = InternalServerError` and the exception logged. A `500` from
   this facade therefore means "the call did not succeed" — it does not mean the remote server
   answered 500.
2. **The four readers never throw either.** `ReadAsStringAsync`, `ReadFromJsonAsync<T>`,
   `ReadAsStreamAsync` and `ReadAsByteArrayAsync` catch, log, set `StatusCode` to 500 and
   return `default` (`Array.Empty<byte>()` for the byte reader). Check the status before you
   read the body.
3. **Builder state persists across sends.** `RequestBuilder` is a `readonly` field created once
   per sender instance and never reset. Consequences:
   - `Headers` is a `Dictionary<string, string>`: a repeated name overwrites, but nothing ever
     removes an entry, so headers set for one call are still sent on the next.
   - `Method`, `Uri` and `Content` are overwritten by their `With…`/`Use…` call and otherwise
     carry over — a second chain that sets no content still sends the first chain's content.
   - `UseClient` sets **both** `CustomClient` and `UseLogging = false`, and both stick for
     every later send from that instance.
   Set everything each call needs; never rely on a previous chain having left something behind.
4. **`WithUri` is the only mandatory link.** `Build()` throws
   `InvalidOperationException("URI must be specified.")` when `Uri` is null. There is no query
   string affordance — build the query into the URI before passing it.
5. **Headers are applied with `TryAddWithoutValidation`**, and names that are null or
   whitespace and values that are null are skipped. Non-standard header names go out unaltered.
6. **The sender is registered transient** because of (3) — a shared instance would interleave
   concurrent chains.
7. **`HttpResult` is an `HttpResponseMessage`.** It is disposable; callers `using` it.
8. **`OnError`** replaces the default `Log.Error` when a subscriber is attached, and fires only
   for an actual exception. Subscribe before reading — the reader methods route their own
   failures through it too.
9. **`Duration` measures the send only.** It is wall-clock around `client.SendAsync`, so it
   excludes the time spent reading the body.
10. **On the failure path `Duration` is `TimeSpan.Zero`.** The catch block returns before any
    timing is recorded, so a zero duration on a 500 means the request never completed.

## Dependencies

- `AutoMapper` — `HttpResult` is produced by mapping the `HttpResponseMessage`
  (`HttpResultProfile`). Profile discovery and `AddAutoMapper` wiring belong to
  `automapper-mapping`.
- `Serilog` — the static `Log` used for request/response and error logging.
- `Microsoft.AspNetCore.Http` — carried in the using list but not used by anything in this
  file. Kept because the recreation is verbatim; harmless to drop if your analyzers flag
  unused usings.
- BCL only otherwise.

### Packages for the whole facade

All three reference files together need four packages, at the versions the reference
solution pins:

| Package | Version | Needed by |
|---|---|---|
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | 12.0.1 | this file (`HttpResultProfile`) |
| `Serilog.AspNetCore` | 7.0.0 | this file (the `Log` statics) |
| `Humanizer` | 2.14.1 | `content-extensions.md` (the snake-case overload) |
| `ReHackt.Extensions.Options.Validation` | 7.0.1 | `registration-and-settings.md` |

## HttpClientSender.cs

```csharp
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Facades.Common.HttpClients;

public interface IHttpClientSender
{
    /// <summary>
    /// Use to set client instance if not using default client
    /// </summary>
    IHttpClientSender UseClient(HttpClient httpClient);

    /// <summary>
    /// set http method (default is get)
    /// </summary>
    IHttpClientSender UseMethod(HttpMethod method);

    /// <summary>
    /// set request uri (default is empty)
    /// </summary>
    IHttpClientSender WithUri(string uri);

    /// <summary>
    /// set request uri (default is empty)
    /// </summary>
    IHttpClientSender WithUri(Uri uri);

    /// <summary>
    /// Header is object or keyvaluepair
    /// </summary>
    /// <param name="headers">Names/values of HTTP headers to set. Typically an anonymous object or IDictionary.</param>
    /// <param name="replaceUnderscoreWithHyphen">If true, underscores in property names will be replaced by hyphens. Default is true.</param>
    /// <exception cref="ArgumentNullException"><paramref name="headers"/> is <c>null</c>.</exception>
    IHttpClientSender WithHeaders(object headers, bool replaceUnderscoreWithHyphen = true);

    /// <summary>
    /// set request content (default is empty)
    /// </summary>
    IHttpClientSender WithContent(HttpContent content);

    Task<HttpResult> SendAsync(CancellationToken cancellationToken = default);
}

public class HttpClientSender : IHttpClientSender
{
    private static readonly HttpClient DefaultHttpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    });

    private readonly IMapper mapper;
    private readonly RequestBuilder builder = new();

    public HttpClientSender(IMapper mapper)
    {
        this.mapper = mapper;
    }

    public IHttpClientSender UseClient(HttpClient httpClient)
    {
        builder.CustomClient = httpClient;
        builder.UseLogging = false;
        return this;
    }

    public IHttpClientSender UseMethod(HttpMethod method)
    {
        builder.Method = method;
        return this;
    }

    public IHttpClientSender WithUri(string uri) => WithUri(new Uri(uri));

    public IHttpClientSender WithUri(Uri uri)
    {
        builder.Uri = uri;
        return this;
    }

    public IHttpClientSender WithContent(HttpContent content)
    {
        builder.Content = content;
        return this;
    }

    public IHttpClientSender WithHeaders(object headers, bool replaceUnderscoreWithHyphen = true)
    {
        foreach (var (key, value) in ParseKeyValuePairs(headers))
        {
            string headerKey = replaceUnderscoreWithHyphen ? key.Replace("_", "-", StringComparison.Ordinal) : key;
            if (!string.IsNullOrWhiteSpace(headerKey) && value != null)
            {
                builder.Headers[headerKey] = value.ToString()!;
            }
        }

        return this;
    }

    public async Task<HttpResult> SendAsync(CancellationToken cancellationToken = default)
    {
        var request = builder.Build();
        var client = builder.CustomClient ?? DefaultHttpClient;

        TimeSpan duration = TimeSpan.Zero;

        try
        {
            if (builder.UseLogging)
            {
                Log.Information("---> Request Info: \n{request}\n---> End", request.ToString());
            }

            DateTime start = DateTime.UtcNow;
            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            DateTime end = DateTime.UtcNow;

            var result = mapper.Map<HttpResult>(response);
            result.Duration = end - start;

            if (builder.UseLogging)
            {
                Log.Information("---> Response Info: \n{response}\n---> End", result.ToString());
            }

            return result;
        }
        catch (Exception ex)
        {
            return new(duration, ex, request);
        }
    }

    /// <summary>
    /// Returns a key-value-pairs representation of the object.
    /// For strings, URL query string format assumed and pairs are parsed from that.
    /// For objects that already implement IEnumerable&lt;KeyValuePair&gt;, the object itself is simply returned.
    /// For all other objects, all publicly readable properties are extracted and returned as pairs.
    /// </summary>
    /// <param name="obj">The object to parse into key-value pairs</param>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null" />.</exception>
    private static IEnumerable<(string Key, object? Value)> ParseKeyValuePairs(object obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        if (obj is IEnumerable e)
        {
            return
            obj is string s ? StringToKeyValue(s) :
            (IEnumerable<(string, object? Value)>)CollectionToKeyPair(e);
        }
        else
        {
            return
            obj is string s ? StringToKeyValue(s) :
            ObjectToKeyValue(obj);
        }
    }

    private static IEnumerable<(string Key, object? Value)> StringToKeyValue(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return Enumerable.Empty<(string, object?)>();
        }

        return
            from p in s.Split('&')
            let pair = SplitOnFirstOccurence(p, "=")
            let name = pair[0]
            let value = pair.Length == 1 ? null : pair[1]
            select (name, (object)value);
    }

    /// <summary>
    /// Splits at the first occurrence of the given separator.
    /// </summary>
    /// <param name="s">The string to split.</param>
    /// <param name="separator">The separator to split on.</param>
    /// <returns>Array of at most 2 strings. (1 if separator is not found.)</returns>
    private static string[] SplitOnFirstOccurence(string s, string separator)
    {
        if (string.IsNullOrEmpty(s))
        {
            return new[] { s };
        }

        var i = s.IndexOf(separator);
        return i == -1 ?
            new[] { s } :
            new[] { s[..i], s[(i + separator.Length)..] };
    }

    private static IEnumerable<(string Name, object? Value)> ObjectToKeyValue(object obj) =>
        from prop in obj.GetType().GetProperties()
        let getter = prop.GetGetMethod(false)
        where getter != null
        let val = getter.Invoke(obj, null)
        select (prop.Name, GetDeclaredTypeValue(val, prop.PropertyType));

    private static object? GetDeclaredTypeValue(object value, Type declaredType)
    {
        if (value == null || value.GetType() == declaredType)
        {
            return value;
        }

        declaredType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (value is IEnumerable col
            && declaredType.IsGenericType
            && declaredType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            && !col.GetType().GetInterfaces().Contains(declaredType)
            && declaredType.IsInstanceOfType(col))
        {
            var elementType = declaredType.GetGenericArguments()[0];
            return col.Cast<object>().Select(element => Convert.ChangeType(element, elementType));
        }

        return value;
    }

    private static IEnumerable<(string Key, object? Value)> CollectionToKeyPair(IEnumerable col)
    {
        bool TryGetProp(object obj, string name, out object? value)
        {
            var prop = obj.GetType().GetProperty(name);
            var field = obj.GetType().GetField(name);

            if (prop != null)
            {
                value = prop.GetValue(obj, null);
                return true;
            }

            if (field != null)
            {
                value = field.GetValue(obj);
                return true;
            }

            value = null;
            return false;
        }

        bool IsTuple2(object item, out object? name, out object? val)
        {
            name = null;
            val = null;
            return
                OrdinalContains(item.GetType().Name, "Tuple") &&
                TryGetProp(item, "Item1", out name) &&
                TryGetProp(item, "Item2", out val) &&
                !TryGetProp(item, "Item3", out _);
        }

        bool LooksLikeKV(object item, out object? name, out object? val)
        {
            name = null;
            val = null;
            return
                (TryGetProp(item, "Key", out name) || TryGetProp(item, "key", out name) || TryGetProp(item, "Name", out name) || TryGetProp(item, "name", out name)) &&
                (TryGetProp(item, "Value", out val) || TryGetProp(item, "value", out val));
        }

        foreach (var item in col)
        {
            if (item == null)
            {
                continue;
            }

            if (!IsTuple2(item, out var name, out var val) && !LooksLikeKV(item, out name, out val))
            {
                yield return (ToInvariantString(name) ?? throw new ArgumentNullException(nameof(col)), null);
            }
            else if (name != null)
            {
                yield return (ToInvariantString(name) ?? throw new ArgumentNullException(nameof(col)), val);
            }
        }
    }

    private static bool OrdinalContains(string s, string value, bool ignoreCase = false) =>
            s?.IndexOf(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0;

    /// <summary>
    /// Returns a string that represents the current object, using CultureInfo.InvariantCulture where possible.
    /// Dates are represented in ISO 8601.
    /// </summary>
    private static string? ToInvariantString(object? obj)
    {
        if (obj == null)
        {
            return null;
        }
        else
        {
            if (obj is DateTime dt)
            {
                return dt.ToString("o", CultureInfo.InvariantCulture);
            }
            else if (obj is DateTimeOffset dto)
            {
                return dto.ToString("o", CultureInfo.InvariantCulture);
            }
            else if (obj is IConvertible c)
            {
                return c.ToString(CultureInfo.InvariantCulture);
            }
            else if (obj is IFormattable f)
            {
                return f.ToString(null, CultureInfo.InvariantCulture);
            }
            else
            {
                return obj.ToString();
            }
        }
    }

    private sealed class RequestBuilder
    {
        public HttpMethod Method { get; set; } = HttpMethod.Get;

        public Uri? Uri { get; set; }

        public Dictionary<string, string> Headers { get; } = new();

        public HttpContent? Content { get; set; }

        public bool UseLogging { get; set; } = true;

        public HttpClient? CustomClient { get; set; }

        public HttpRequestMessage Build()
        {
            if (Uri == null)
            {
                throw new InvalidOperationException("URI must be specified.");
            }

            var request = new HttpRequestMessage(Method, Uri)
            {
                Content = Content,
            };

            foreach (var header in Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return request;
        }
    }
}

public class HttpResult : HttpResponseMessage
{
    public event Action<Exception>? OnError;

    private readonly Action<Exception> defaultLogError = (ex) =>
    {
        Log.Error("---> An error occurred: {error}", ex);
    };

    public HttpResult()
    {
    }

    public HttpResult(TimeSpan duration, Exception requestException, HttpRequestMessage request)
    {
        Duration = duration;
        RequestMessage = request;
        StatusCode = HttpStatusCode.InternalServerError;
        LogException(requestException);
    }

    [AllowNull]
    public new HttpResponseHeaders Headers { get; set; }

    [AllowNull]
    public new HttpResponseHeaders TrailingHeaders { get; set; }

    public TimeSpan Duration { get; internal set; }

    public async Task<string?> ReadAsStringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogException(ex);
            StatusCode = HttpStatusCode.InternalServerError;
            return default;
        }
    }

    public async Task<TResponse?> ReadFromJsonAsync<TResponse>(JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Content.ReadFromJsonAsync<TResponse>(options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogException(ex);
            StatusCode = HttpStatusCode.InternalServerError;
            return default;
        }
    }

    public async Task<Stream?> ReadAsStreamAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogException(ex);
            StatusCode = HttpStatusCode.InternalServerError;
            return default;
        }
    }

    public async Task<byte[]> ReadAsByteArrayAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogException(ex);
            StatusCode = HttpStatusCode.InternalServerError;
            return Array.Empty<byte>();
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new(base.ToString());
        sb.AppendLine("', Duration: ");
        sb.Append(Duration);
        return sb.ToString();
    }

    private void LogException(Exception ex)
    {
        if (OnError != null)
        {
            OnError?.Invoke(ex);
        }
        else
        {
            defaultLogError.Invoke(ex);
        }
    }
}

public class HttpResultProfile : Profile
{
    public HttpResultProfile()
    {
        CreateMap<HttpResponseMessage, HttpResult>();
    }
}
```

## Notes for a project that already has a sender

Some existing projects carry an older shape of this file: no `RequestBuilder`, a
`HttpRequestMessage` field re-created inside `UseClient` and in a `finally` after each send.
It has the same public interface, so callers do not change — but its state behaviour differs
from the contract above. Bring it to this form when you touch it deliberately; do not rewrite
it as a drive-by while doing something else.

Local style note: this transcription keeps the canonical file's use of `var`. Projects whose
analyzers require explicit types can spell the locals out — no behaviour depends on it.
One transcribed signature will warn under stricter nullable settings than the source's:
`GetDeclaredTypeValue(object value, …)` takes a non-nullable parameter and immediately
null-checks it. Left as-is; annotate the parameter if your build treats that as an error.
