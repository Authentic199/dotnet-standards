# SerializerExtension

**When:** serializing to or deserializing from JSON anywhere outside the MVC request pipeline —
cache payloads, outbound HTTP bodies, webhook bodies, queued message envelopes, column values.

**Why:** `JsonSerializerOptions` configured ad hoc at each call site drifts. One site writes
camelCase, the next reads with default PascalCase, and the round trip silently loses properties.
This file makes both directions read one declaration site, so changing the house JSON shape is a
one-line edit rather than a corpus-wide grep.

Transcribed verbatim from one corpus project, where it sits in the `Common/Extensions` slot. No
members added, removed or renamed; no behaviour changed; nothing merged.

```csharp
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Facades.Common.Extensions;

public static class SerializerExtension
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
    };

    public static string Serialize<T>(T obj, Action<JsonSerializerOptions>? configs = null) => JsonSerializer.Serialize(obj, Options(configs));

    public static T? Deserialize<T>(string text, Action<JsonSerializerOptions>? configs = null) => JsonSerializer.Deserialize<T>(text, Options(configs));

    public static bool TryDeserialize<T>(string text, out T? result, Action<JsonSerializerOptions>? configs = null)
    {
        try
        {
            result = Deserialize<T>(text, configs);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    private static JsonSerializerOptions Options(Action<JsonSerializerOptions>? configs)
    {
        JsonSerializerOptions options = new(DefaultOptions);
        configs?.Invoke(options);
        return options;
    }
}
```

## Notes

- **`DefaultOptions` is never handed out.** `Options` copy-constructs from it on every call, so a
  `configs` callback that flips a setting affects that one call and cannot poison the shared
  instance. That is what makes exposing a per-call override safe at all — and it means adding a
  converter is a `configs` lambda, not a new options object built at the call site.
- **The clone happens even when `configs` is `null`** — every call allocates a fresh
  `JsonSerializerOptions`. On a hot path, hoist the serialized form rather than calling in a loop.
- **`TryDeserialize` returns `false` instead of throwing** — that is the whole reason it exists,
  and it is the right call for input you do not control: a webhook body, a cache entry written by
  an older deployment.
- **`TryDeserialize` catches everything.** A bug in a custom converter and a malformed payload are
  indistinguishable at the call site, and nothing is logged. Where the input is supposed to be
  well-formed, call `Deserialize` and let the exception surface.
- **A `true` return does not guarantee a non-null `result`.** The literal text `null` is valid
  JSON and deserializes successfully to `null`. The out parameter is `T?`. Check it.
- **`UnsafeRelaxedJsonEscaping` does not HTML-escape the output.** That is deliberate — it keeps
  non-ASCII text readable in logs and payloads — but the output of `Serialize` must never be
  written straight into an HTML page or a `<script>` block without escaping at that boundary.

## Dependencies and registration

- `System.Text.Json` — in-box, no package reference.
- Static class — **no DI registration**.
