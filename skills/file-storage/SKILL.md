---
name: file-storage
description: >-
  This skill should be used when a .NET service stores files in S3 or object
  storage: scaffolding the FileStorage facade into a project that has none,
  injecting IS3AwsFileStorageService, uploading an IFormFile, Stream or
  directory, composing bucket keys, exposing them as pre-signed URLs via
  S3FilePath and its JsonConverter, wiring S3AwsSettings and
  AddS3AwsFileStorage, deleting objects, multipart upload, or re-uploading an
  external URL via IMediaManager. Not for: ReplaceSpecialCharacters —
  common-extensions; exception types, middleware — error-handling; response
  DTOs, endpoints — api-surface; CreateMap, MapFrom — automapper-mapping;
  Excel, MiniExcel — excel-miniexcel; typed clients, AddHttpClient —
  http-client-factory; entity configuration — ef-core-data-access; folder
  placement — facade-module-architecture.
---

## Core Principles

### 1. This facade is recreatable canon — recreate it, never reinvent it

The facade, its key-generation extension and the download pipeline are accumulated
wisdom: settings validation, key generation, the response converter, the failure
contract. A project that needs file storage usually has **none of it yet**, and the
failure mode is not "no code" — it is a call site that grows its own S3 client, its
own key string and its own error handling, three of which then disagree.

When the files are absent, recreate them from this skill's `references/`:

- **Recreate whole files**, with the namespaces the reference files carry. A trimmed
  "just the upload method" subset is how the divergence starts.
- **Never inline a bespoke client at a call site.** A service that needs storage needs
  `IS3AwsFileStorageService` injected, not a client of its own.
- **Never cite a project path as the source.** These reference files *are* the source;
  there is nothing else to go and look at.

Where the facade folder itself lives is **facade-module-architecture**.

### 2. Persist the key, serve the URL

The database column holds the **bucket key**. Nothing in the database is ever a URL.

```csharp
// Entity — persisted
public string? Image { get; set; }      // "folder/638…_name.png"

// EntityBaseResponse — serialized
public S3FilePath Image { get; set; }   // becomes a URL on the way out
```

`S3FilePathConverter` mints the URL at **serialization** time, once per response.
Why this way: a URL expires, carries a signature, and changes when the public host
changes. A key does none of those, so it is the only value safe to write down — and a
persisted URL is a persisted expiry.

`S3FilePath` is **response-only**. Its `Read` exists so an inbound value does not fault;
it is not an input contract. Requests take a plain `string` key.

### 3. One key generator, one format

Every object in the bucket is named by one format:

```
{Folder}/{Ticks}_{SanitizedFileName}{Extension}
```

The folder segments the bucket, the tick prefix makes re-uploads of the same filename
collision-free without a lookup, and the stem is sanitized before it reaches the bucket.
Call sites supply a folder constant and the file — **never the string**. The format is
declared once, in the extension layer.

Why one place: the format is a persisted contract. A second declaration drifts silently,
and nothing fails until the two halves of the system disagree about what a key looks like.

The sanitizing helper itself is **common-extensions** — call it, do not restate it.

### 4. Two entry points, two failure contracts

| You call | On failure it |
|---|---|
| `IS3AwsFileStorageService.UploadAsync(...)` — the service | logs and returns `false`, never throws |
| `UploadAsync(folder, file)` — the extension | throws `InternalServerException` |

The service is deliberately quiet: it is a transport, and a transport that throws makes
every caller write the same `try`. The extension is the layer that has an opinion, and
converting `false` into a thrown exception is its whole job.

**Default to the extension overload.** Drop to the raw service only when the payload is
not an `IFormFile` or the caller genuinely has a non-throwing branch — and then it must
read the `bool`.

The exception family, its envelope and the middleware that renders it are **error-handling**.

### 5. A storage write is not part of the database transaction

`RollbackTransactionAsync` does not un-upload an object. The object lands in the bucket
immediately; the row lands at commit; nothing joins them. So every write path needs an
explicit compensation:

- **Failure after upload** — delete the key just uploaded, in the `catch`.
- **Update** — upload the new object, commit, *then* delete the old key.

Without this, a rolled-back request leaves an orphan that nothing references and no
cleanup ever finds.

## Patterns

### Wiring: three registrations that land together

```csharp
// 1 — inside the facade's own Startup: options bound and validated at boot
services.AddOptions<S3AwsSettings>()
    .BindConfiguration(nameof(S3AwsSettings))
    .ValidateDataAnnotationsRecursively()
    .ValidateOnStart();

// 2 — chained from the Infrastructure composition root, beside the other facades
services
    .AddS3AwsFileStorage()
    .AddMediaManager();          // only if external-URL ingestion is needed

// 3 — the converter, in the MVC JSON options
options.JsonSerializerOptions.Converters.Add(
    new S3FilePathConverter(builder.Configuration));
```

Registration 3 is the one that gets forgotten, and it fails *silently in the wrong
direction*: without it `S3FilePath` serializes as a JSON object exposing the raw key
instead of a URL string, so every client sees the bucket key. Note it is constructed
by hand from `IConfiguration` rather than resolved from the container, because
`JsonSerializerOptions` is built before the service provider is.

Because settings validate on start, a missing bucket or key fails the boot rather than
the first upload. **Commit placeholders only** — see the anti-patterns.

Full file text: `references/implementation.md`.

### Uploading a user-supplied file

Prefer the extension overload. It generates the key, sanitizes the filename, and turns
a failed upload into an exception:

```csharp
private const string EntityFolder = "entities";

entity.Image = await fileStorage.UploadAsync(EntityFolder, request.Image);
```

Use a **raw service overload** only when the payload is not an `IFormFile` — a `Stream`,
`byte[]`, or a local file path you produced. Then you own both halves: build the key with
`FormatFileName`, and check the `bool`.

```csharp
string key = S3AwsExtensions.FormatFileName(fileName, EntityFolder);

if (!await fileStorage.UploadAsync(stream, key, cancellationToken: ct))
{
    throw new InternalServerException("File upload failed");
}
```

The upload overloads apply a default canned ACL when the caller passes none. Pass one
explicitly when the object's visibility matters; the default value is named in
`references/implementation.md`.

### Exposing a stored key as a URL

```csharp
public class EntityBaseResponse
{
    public S3FilePath Image { get; set; }
}

// in the colocated profile
CreateMap<Entity, EntityBaseResponse>()
    .ForMember(dest => dest.Image,
        opt => opt.MapFrom(src => new S3FilePath(src.Image!, true)));
```

The second argument is `IsSystem`, and it selects the converter's write branch:

| `IsSystem` | Meaning | Serialized as |
|---|---|---|
| `true` | the value is a key in this service's own bucket | a pre-signed URL |
| `false` | the value is already an absolute, externally-hosted URL | the value, verbatim |

So one response field can carry both a self-hosted object and a third-party URL without
the client knowing the difference. `IsSystem` is get-only, so the decision is made once,
at the mapping site, and cannot be flipped downstream.

`CreateMap`/`ForMember` mechanics are **automapper-mapping**; what belongs in the second
argument is this skill's.

### Choosing among the three URL methods

All three exist because they answer different questions:

- **`GetPreSignedUrl`** — the client-facing one. Time-limited, and the host is rewritten
  from the internal service URL to the public one, so the link works from outside. This
  is what the converter calls. It also takes an optional `responseContentDisposition`,
  passed through as a response-header override: supply it when the client should receive
  the object as a named download rather than render it inline.
- **`GetPublicUrl`** — plain `{PublicUrl}/{bucket}/{key}` concatenation. No expiry, no
  signature. Only for objects meant to be openly readable.
- **`GetServiceUrl`** — signed like the first, but **not** host-rewritten, and it accepts
  an HTTP verb. This is the server-to-server form; the media download pipeline uses it
  with `GET` and `HEAD`. It has no client to instruct, so no content-disposition.

Getting this wrong is quiet: a `GetServiceUrl` link handed to a browser points at an
internal host, and a `GetPublicUrl` link to a private object 403s.

### Create: upload first, compensate if the row fails

```csharp
Entity entity = mapper.Map<Entity>(request);

if (request.Image?.Length > 0)
{
    entity.Image = await fileStorage.UploadAsync(EntityFolder, request.Image);
}

await repositoryWrapper.BeginTransactionAsync(ct);
try
{
    await repositoryWrapper.Repository<Entity>().AddAsync(entity, ct);
    await repositoryWrapper.CommitTransactionAsync(ct);
}
catch (Exception ex)
{
    if (!string.IsNullOrWhiteSpace(entity.Image))
    {
        await fileStorage.DeleteAsync(entity.Image, ct);   // compensate the orphan
    }

    await repositoryWrapper.RollbackTransactionAsync(ct);
    throw new InternalServerException(/* message key */, ex);
}
```

`DeleteAsync` logs its own failures rather than throwing, so it cannot mask the original
exception — which is what makes it safe inside a `catch`.

### Update: never delete the old object before the new one is up

Two orderings look workable and they fail differently:

- **Delete-old-then-upload** — if the upload fails, the old file is already gone and the
  row still points at it. The record is broken by a *successful* request path.
- **Upload-new-then-delete-old-after-commit** — a failed commit leaves one orphaned
  object, which the compensating delete removes.

Take the second. Hold the old key in a local, swap the property, commit, and only then
delete the loser:

```csharp
string? previousKey = entity.Image;

if (request.Image?.Length > 0)
{
    entity.Image = await fileStorage.UploadAsync(EntityFolder, request.Image);
}

await repositoryWrapper.BeginTransactionAsync(ct);
await repositoryWrapper.Repository<Entity>().UpdateAsync(entity, ct);
await repositoryWrapper.CommitTransactionAsync(ct);

if (entity.Image != previousKey && !string.IsNullOrWhiteSpace(previousKey))
{
    await fileStorage.DeleteAsync(previousKey, ct);   // only once the row is safe
}
```

An orphaned object costs storage. A missing object costs a broken record.

For bulk removal — deleting an entity that owns many files — use `DeleteManyAsync`, which
issues one batched request instead of N round trips and no-ops on an empty list.

### Ingesting a file from an external URL

When a file lives at someone else's URL and must become an object in this service's
bucket, the route is **download → temp file → re-upload**, run by `IMediaManager`. It
probes the URL with `HEAD` for content type and length, streams to a temp file, and hands
back a disposable handle.

```csharp
using MediaDownloadInfo info = await mediaManager.DownloadAsync(sourceUrl, cancellationToken: ct);

if (!info.IsSuccess)
{
    // the flow's own failure path
}

await using FileStream stream = info.OpenFileStream();
string key = await fileStorage.UploadAsync(EntityFolder, info.FileName, stream);
```

`MediaDownloadInfo` is `IDisposable` and its disposal removes the temp file, so the
`using` is load-bearing and the stream must be consumed before the block ends.

Registration is a **typed client**, because `MediaManager` takes an `HttpClient`
constructor parameter:

```csharp
services.AddHttpClient<IMediaManager, MediaManager>();
```

This pipeline is optional — bring it in only when the project actually ingests external
media. Full file set and consumer pipeline: `references/media-downloads.md`. `HttpClient`
registration and resilience policy beyond this one line are **http-client-factory**.

## Anti-patterns

### 1. Hand-rolling the key format at the call site

```csharp
// BAD — a second, private declaration of a persisted contract
private const string Format = "{0}/{1}_{2}{3}";

string key = string.Format(Format, folder, DateTime.UtcNow.Ticks, name, ext);
await fileStorage.UploadAsync(stream, key, cancellationToken: ct);
```

```csharp
// GOOD — one generator, and the sanitization comes with it
string key = S3AwsExtensions.FormatFileName(fileName, EntityFolder);
```

A second copy of the format is a second owner. When the canonical one changes, the copy
keeps writing the old shape and nothing fails — the bucket just quietly contains two
conventions. A hand-rolled key also skips the filename sanitization, which is how spaces
and diacritics reach the bucket.

### 2. Binding `S3FilePath` on a request model

```csharp
// BAD — an inbound field typed as a response concern
public class CreateEntityRequest
{
    public S3FilePath Image { get; set; }
}
```

```csharp
// GOOD — inbound keys are strings; the file itself is IFormFile
public class CreateEntityRequest
{
    public IFormFile? Image { get; set; }   // uploading
    public string? ImageKey { get; set; }   // referencing an existing object
}
```

The type exists to turn a key into a signed URL on the way *out*. On the way in there is
nothing to sign, and the client would be echoing back a URL the server minted — expiry
included. Request/response DTO shape in general is **api-surface**.

### 3. Conflating the two tick-based name formats

| Where | Separator | Clock |
|---|---|---|
| Bucket keys | `_` | `DateTime.UtcNow.Ticks` |
| Local temp files | `-` | `DateTimeOffset.UtcNow.Ticks` |

```csharp
// BAD — a temp-file name used as a bucket key
string key = mediaInfo.GetTempFileName(tempDirectory);
await fileStorage.UploadAsync(stream, key, cancellationToken: ct);
```

```csharp
// GOOD — cross the boundary through the key generator
string key = S3AwsExtensions.FormatFileName(mediaInfo.FileName, EntityFolder);
```

They look alike enough to copy by accident and they sit two folders apart. A temp-shaped
key is invisible until something tries to split a key back into a folder and a name.

### 4. Committing real credentials in the storage settings section

```jsonc
// GOOD — placeholders in the repo; the two secrets from the environment
"S3AwsSettings": {
  "ServiceUrl": "https://storage.internal.example",
  "PublicUrl": "https://files.example",
  "BucketName": "<bucket-name>",
  "AccessKey": "<from-environment>",
  "SecretKey": "<from-environment>",
  "PreSignedUrlExpirationInMinutes": 1440
}
```

The trap is that the same section legitimately carries non-secret values — bucket name,
public URL, expiry — that *should* be committed. That is exactly what makes filling in
the two key fields feel natural, and a committed secret is permanently in history.

How secrets are supplied and rotated is **auth-and-security** — follow its doctrine
rather than inventing a second one here. This skill only insists the committed section
holds placeholders.

### 5. Calling a raw overload and discarding the `bool`

```csharp
// BAD — upload fails, returns false, and the key is persisted anyway
await fileStorage.UploadAsync(stream, key, cancellationToken: ct);
entity.Image = key;
```

```csharp
// GOOD — let the extension convert it for you...
entity.Image = await fileStorage.UploadAsync(EntityFolder, request.Image);

// ...or convert it yourself when you own the key
if (!await fileStorage.UploadAsync(stream, key, cancellationToken: ct))
{
    throw new InternalServerException("File upload failed");
}

entity.Image = key;
```

The service never throws — Principle 4 working as intended — so a discarded `bool` is the
one place a failed upload becomes a row pointing at nothing, with nothing reporting it.

## Decision Guide

| Scenario | Do this |
|---|---|
| Project needs file storage and has none | Recreate the facade from `references/implementation.md` + `references/key-generation.md`, then wire all three registrations. Do not start from a call site |
| Uploading an `IFormFile` | `fileStorage.UploadAsync(folder, file)` — the extension overload names the key and throws on failure |
| Uploading a `Stream`, `byte[]` or local path | Raw service overload; key from `FormatFileName(fileName, folder)`; check the `bool` |
| You need the key without uploading yet | `FormatFileName(fileName, folder)` — never build the string inline |
| Uploading a whole local directory | `DirectoryUploadAsync` — batches rather than looping single uploads. `references/implementation.md` |
| Returning a stored object to a client | `S3FilePath` property on the response; `new S3FilePath(src.Key!, true)` |
| Returning a third-party URL in the same field | `new S3FilePath(absoluteUrl, false)` — emitted verbatim, unsigned |
| Accepting a file or an existing key on a request | `IFormFile` or `string` — never `S3FilePath` |
| Client-facing link to a private object | `GetPreSignedUrl` — signed, expiring, host-rewritten. Never `GetServiceUrl` |
| Client should download rather than display | `GetPreSignedUrl` with `responseContentDisposition` |
| Object is openly readable and the link must not expire | `GetPublicUrl` — plain concatenation, no signature |
| Server-to-server fetch, `GET` or `HEAD` | `GetServiceUrl` — not host-rewritten |
| Replacing a file | Upload new → commit → delete old. Never delete first |
| Persist failed after an upload | `DeleteAsync` the new key in the `catch`, then rethrow |
| Removing many objects at once | `DeleteManyAsync` — one batched request, no-ops on an empty set |
| Very large or resumable upload | The multipart members on `IS3AwsFileStorageService` — initiate, upload part, complete, abort. `references/implementation.md` |
| File lives at an external URL | `IMediaManager` download → temp → re-upload. `references/media-downloads.md` |
| Credentials, rotation, secret storage | **auth-and-security** |
| Exception types and the error envelope | **error-handling** |
| `CreateMap`, `ForMember`, profile placement | **automapper-mapping** |
| Where the facade folder and its `Startup` live | **facade-module-architecture** |
| Filename or string sanitization helpers | **common-extensions** — look before writing one |
| The entity column that holds the key | **ef-core-data-access** |

### references/

Read these before writing code, not after.

| File | Read this when |
|---|---|
| `references/implementation.md` | Recreating or repairing the facade — full text of `S3FilePath`, `S3FilePathConverter`, `S3AwsSettings`, `IS3AwsFileStorageService` + implementation, `S3FileUploadException`, `Startup`, the default canned ACL, a placeholder-only config example, and the optional `FileStorage.cs` appendix |
| `references/key-generation.md` | Producing a bucket key, adding an upload overload, or deciding whether a name is well-formed — both `UploadAsync` overloads, `FormatFileName`, and the bucket-key vs local-temp tick-format distinction |
| `references/media-downloads.md` | Ingesting a file from an external URL, or wiring `IMediaManager` — the five `MediaDownloads` files, the typed-client registration, and the download → temp → re-upload pipeline |
| `references/usage-patterns.md` | Wiring the converter or composition root, mapping a key onto a response, writing an upload-then-persist or compensating-delete flow, or serving an attachment download |
