# Anti-patterns 5–8

Entries 1–4 are in `SKILL.md`; these four continue that numbering. They live here because each
one needs its code shown to be recognizable. Read them when auditing an existing outbound call
or a hand-maintained copy of the facade — 5 and 6 are defects inside such a copy, 7 and 8 are
defects at a call site that uses the facade correctly right up to the line that breaks.

## 5. Reading the body synchronously in the result's constructor

```csharp
// BAD - a constructor cannot await, so every response pays for the whole body, unconditionally
public HttpResult(HttpResponseMessage response)
{
    try
    {
        Content = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        StatusCode = HttpStatusCode.InternalServerError;
        LogException(ex);
        Content = ex.GetBaseException().ToString();
    }
}
```

```csharp
// GOOD - the body is read on demand, awaited, cancellable, and only after the status check
string? body = await result.ReadAsStringAsync(cancellationToken);
```

Because it is in a constructor there is no way out of it: it cannot be awaited, it cannot be
skipped for a caller that only wanted the status, and it takes no `CancellationToken`. Every
response is buffered whole into a string before the caller sees the result — including a large
binary body the caller intended to stream.

`GetAwaiter().GetResult()` blocks the calling thread until the whole body has arrived over the
network, so under concurrency that is one blocked thread-pool thread per in-flight response, for
as long as the remote takes to finish sending. Note what this is *not*: `ConfigureAwait(false)`
is present, so this is not the classic synchronization-context deadlock. It is starvation, not a
hang — which is why it survives testing and shows up as latency under load.

The `catch` adds a second defect on top. A read failure becomes a `500` *and* writes the
exception's own text into the property callers read as the body, so a caller that logs, forwards
or deserializes that string is handling a stack trace as if it were the remote's answer.

The canonical result reads nothing in its constructor. Its four readers are async, take a
`CancellationToken`, and are called by the caller after the status check — Principle 3, and
contract 2 in `references/sender-and-result.md`.

## 6. Testing the collection where the item is meant to be tested

```csharp
// BAD - `value` is the collection, never an IFormFile, so this branch cannot run
else if (elementType != null)
{
    foreach (object? item in (dynamic)value)
    {
        if (value is IFormFile fileInCollection)
        {
            content.Add(new StreamContent(fileInCollection.OpenReadStream()), property.Path, fileInCollection.FileName);
        }
        else
        {
            content.Add(new StringContent(item?.ToString(), Encoding.UTF8), property.Path);
        }
    }
}
```

```csharp
// GOOD - narrow the collection once, test each item, and keep the file's content type
else if (elementType != null && value is IEnumerable items)
{
    foreach (var item in items)
    {
        if (item is IFormFile fileInCollection)
        {
            StreamContent stream = new(fileInCollection.OpenReadStream());
            stream.Headers.ContentType = new MediaTypeHeaderValue(fileInCollection.ContentType);

            content.Add(stream, property.Path, fileInCollection.FileName);
        }
        else
        {
            content.Add(new StringContent(item?.ToString() ?? string.Empty, Encoding.UTF8), property.Path);
        }
    }
}
```

The loop variable is `item`; the test is on `value`. `value` is the collection property's value,
so the test is false on every iteration and every file in a collection takes the else branch —
sent as `item.ToString()`, which for a file object is its type name. The request succeeds, the
field is present on the wire under the right name, and the third party stores a short string
where the bytes should be.

It survives testing because a *single* file property is handled correctly by the earlier
`value is IFormFile file` branch. Only collection-valued file properties break, so an
integration exercised with one attachment passes and the multi-file path fails in production.

The else branch also drops the file's content type, which the canonical form sets on the
`StreamContent` on both paths. That is the second reason to take the form in
`references/content-extensions.md` whole rather than patch this one in place.

## 7. A long-lived consumer holding one sender, with a conditional link in the chain

```csharp
// BAD - the field outlives every request, and one link only sometimes runs
private readonly IHttpClientSender httpClientSender;   // injected once, into a singleton

IHttpClientSender client = httpClientSender
    .UseClient(httpClient)
    .UseMethod(request.Method!)
    .WithUri(request.Url!)
    .WithContent(request.Data!.ToStringContent(options: options));

if (request.Header != null)
{
    client.WithHeaders(request.Header);
}

HttpResult result = await client.SendAsync();
```

```csharp
// GOOD - a sender resolved per unit of work, every link set on every chain
using HttpResult result = await sender
    .UseClient(httpClient)
    .UseMethod(method)
    .WithUri(url)
    .WithHeaders(BuildHeaders(settings))   // always called, never inside an if
    .WithContent(payload.ToStringContent())
    .SendAsync(cancellationToken);
```

Two defects, and both come from Principle 4 — the builder is a field, and nothing on the send
path resets it. Measured against the facade this skill ships, here is what each one costs.

**The conditional link.** Headers accumulate in a dictionary that no send ever clears. A call
that supplies an `Authorization` value leaves it there, and the next call through the same
instance — the one that supplies no headers at all, so the `if` does not run — still sends it, to
whatever URI that next call set. Note what does *not* fix this: `WithHeaders` writes the pairs it
is given and removes nothing, so calling it with an empty collection changes nothing. Only a
sender that has not been used yet starts with an empty dictionary.

**The captive capture.** A singleton consumer resolves its transient sender once, at startup, and
keeps it. From then on every concurrent call through that consumer shares one builder, which is
the interleaving `AddTransient` exists to prevent (see *Registration*). Two calls in flight can
overwrite each other's method, URI and content. And because the chain opens with `UseClient`,
which sets `UseLogging = false` permanently on that instance, none of it appears in a request log.

| Rationalization | Reality |
|---|---|
| "The header only matters for the call that sets it" | Nothing removes a header. The next chain on that instance sends it too — including a credential. |
| "The sender is transient, so every call starts clean" | Every *resolution* starts clean. A singleton consumer resolves once, for the life of the process. |
| "Then call `WithHeaders` with an empty dictionary" | A repeated name overwrites, but nothing ever removes an entry. An empty call writes nothing and clears nothing — only a fresh instance is clean. |

Resolve the sender per unit of work: a consumer whose lifetime is the request, or an explicit
scope opened inside the long-lived one.

## 8. Using what a reader returned without checking that it read anything

```csharp
// BAD - the status branch is correct; the line after it is not
if (result.StatusCode == HttpStatusCode.BadRequest)
{
    Wrapper<TResponse>? badRequestResult =
        await result.ReadFromJsonAsync<Wrapper<TResponse>>(SerializeOptions, cancellationToken);

    string messages = badRequestResult!.Message!;
}
```

```csharp
// GOOD - the read is a branch, not an assumption
if (await result.ReadFromJsonAsync<Wrapper<TResponse>>(SerializeOptions, cancellationToken)
    is Wrapper<TResponse> badRequestResult)
{
    return badRequestResult.Message;
}

Log.Error("{client} returned {status} with an unreadable body", nameof(ThirdPartyApiClient), result.StatusCode);
return default;
```

This is not anti-pattern 3. The status *was* checked, and the branch taken is the right one — the
defect is one line later, in what the code assumes the read produced. The readers catch, log, set
the status to `500` and return `default` (Principle 3), so a `null` here means the remote's error
body was not the shape this integration documented: a plain-text 400, a gateway's HTML page, an
empty body.

The dereference then throws `NullReferenceException` from a line that mentions no HTTP at all.
Nothing in the facade stops it there, so it escapes into the inbound request pipeline — a remote
that answered `400` is reported to this service's own caller as a `500`, logged against the wrong
subsystem.

The reader's own status mutation is no substitute for the check: it writes `500` *after* the
caller's status branch was already chosen, so the code is already inside the `BadRequest` arm when
it happens, and nothing downstream re-reads it. The returned value is the only signal of that
failure which survives.

The same applies to the other three readers, with one twist: `ReadAsByteArrayAsync` returns
`Array.Empty<byte>()`, so its swallowed failure arrives as a zero-length payload rather than a
null — a saved empty file instead of an exception.

The point is the silence, not the operator. The same defect exists with `.Value`, with a plain
member access, or with the value passed straight on to a mapper. What is missing is a branch for
"the reader read nothing".

**Red flags — stop and re-read this skill:** `.GetAwaiter().GetResult()`, `.Result` or `.Wait()`
anywhere near a response body; a body assigned in a constructor; a `catch` that writes an
exception's text into a value callers read as data; a `With…` or `Use…` call inside an `if`; an
`IHttpClientSender` field on a type registered singleton; a pattern test naming the loop's source
instead of its item; a reader's return value used on the next line with no branch for what that
reader returns on failure.
