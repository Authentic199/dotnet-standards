# Wiring and call sites

Read this file when you are hooking the storage facade into an application,
mapping a stored key onto a response, or writing a flow that uploads a file and
saves a row.

The facade files themselves are in `references/implementation.md`. This file is
what happens around them.

## 1. Compose the facade

The facade's own `Startup` is `internal`, so it is chained from the Infrastructure
composition root inside the same assembly:

```csharp
services
    .AddS3AwsFileStorage()
    // … other facades …
    .AddMediaManager();     // only if the project ingests external media
```

Expect these two to sit far apart in a real chain — composition roots grow long
and are ordered for readability, not semantics. **Search the chain by method name;
do not skim it.** A duplicate `AddS3AwsFileStorage()` does not fail: both
descriptors stay in the container and a normal resolve silently takes the last
one, leaving the first as dead configuration.

`AddMediaManager()` belongs here only when `references/media-downloads.md`
applies.

## 2. Register the converter in the JSON options

**This is the step that gets missed.** The facade works without it, and every
response silently ships raw bucket keys instead of URLs.

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new S3FilePathConverter(builder.Configuration));
        // … other converters …
    });
```

**How to tell it is missing:** an `S3FilePath` field serializes as a JSON object
with an `originPath` property instead of as a URL string.

Register it once, and on the serializer options **only** — never also in the
service collection. If the same converter type is added twice, both entries stay
in the list and **the first one registered wins**, so a later registration
intended as a correction does nothing. `references/implementation.md` explains why
the converter is hand-constructed from `IConfiguration` rather than resolved.

## 3. Expose a stored key on a response

The entity holds the key; the response holds an `S3FilePath`:

```csharp
// Entity
public string? Image { get; set; }

// EntityBaseResponse
public S3FilePath Image { get; set; }
```

The mapping site is where the key becomes signable:

```csharp
CreateMap<Entity, EntityBaseResponse>()
    .ForMember(dest => dest.Image,
        opt => opt.MapFrom(src => new S3FilePath(src.Image!, true)));
```

When the stored value is an absolute URL owned by somebody else, pass `false` and
the converter emits it untouched:

```csharp
.ForMember(dest => dest.ExternalImage,
    opt => opt.MapFrom(src => new S3FilePath(src.ExternalImageUrl, false)));
```

One response field can therefore carry both kinds without the client knowing the
difference. `IsSystem` is get-only, so this is decided once, here, by the author
who knows which kind of value the column holds. Getting it wrong is quiet in both
directions: `true` on an external URL signs a URL that was already complete;
`false` on our own key ships the raw key to the client.

Profile placement and `CreateMap` mechanics are **automapper-mapping**'s; which
value goes in the second argument is this skill's. The response base-class chain
is **api-surface**'s.

## 4. Create: upload, then persist

```csharp
Entity entity = mapper.Map<Entity>(request);

if (request.Image is { Length: > 0 })
{
    entity.Image = await fileStorageService.UploadAsync(EntityFolder, request.Image);
}

await repositoryWrapper.BeginTransactionAsync(cancellationToken);
try
{
    await repositoryWrapper.Repository<Entity>().AddAsync(entity, cancellationToken);
    await repositoryWrapper.CommitTransactionAsync(cancellationToken);
}
catch (Exception ex)
{
    if (!string.IsNullOrWhiteSpace(entity.Image))
    {
        await fileStorageService.DeleteAsync(entity.Image, cancellationToken);
    }

    await repositoryWrapper.RollbackTransactionAsync(cancellationToken);
    throw new InternalServerException(/* message key */, ex);
}
```

Three things carry weight:

- **Rolling back the transaction does not un-upload anything.** The compensating
  `DeleteAsync` in the `catch` is the only thing that removes the object.
- **`DeleteAsync` logs rather than throws**, which is what makes it safe inside a
  `catch` — it cannot replace the exception being handled. The cost is that a
  failure during compensation leaves an orphan rather than an obscured error.
- **The extension overload throws on upload failure**, so reaching `AddAsync`
  means the object is in the bucket. There is no `bool` to check here.

Whether the upload runs before or inside the transaction is a local choice; both
appear in practice. What is not a choice is the compensating delete.

Message keys are **message-keys**'; the exception and its envelope are
**error-handling**'s.

## 5. Update: upload, commit, then delete the old key

```csharp
string? previousKey = entity.Image;

await repositoryWrapper.BeginTransactionAsync(cancellationToken);
try
{
    if (request.Image is { Length: > 0 })
    {
        entity.Image = await fileStorageService.UploadAsync(EntityFolder, request.Image);
    }

    mapper.Map(request, entity);

    await repositoryWrapper.Repository<Entity>().UpdateAsync(entity, cancellationToken);
    await repositoryWrapper.CommitTransactionAsync(cancellationToken);
}
catch (Exception ex)
{
    if (entity.Image != previousKey && !string.IsNullOrWhiteSpace(entity.Image))
    {
        await fileStorageService.DeleteAsync(entity.Image, cancellationToken);
    }

    await repositoryWrapper.RollbackTransactionAsync(cancellationToken);
    throw new InternalServerException(/* message key */, ex);
}

// Only once the row is committed does the old object become garbage.
if (entity.Image != previousKey && !string.IsNullOrWhiteSpace(previousKey))
{
    await fileStorageService.DeleteAsync(previousKey, cancellationToken);
}
```

**Never delete the old object before the new one is up.** If the upload then
fails, the old file is gone and the row still points at it — a request that took
the *success* path through its own logic has destroyed data. Uploading first and
deleting last inverts the failure: the worst outcome becomes one orphaned object,
which costs storage and nothing else.

**The `entity.Image != previousKey` guard matters in both places.** Without it, a
request that did not replace the image deletes the image it kept.

`previousKey` is captured **above** the `try` so both the `catch` and the
post-commit block can see it. This is the detail that gets lost when the pattern
is retyped from memory.

For an entity that owns many files, collect the keys and call `DeleteManyAsync`
once rather than looping `DeleteAsync`.

## 6. Serve a file as a named download

By default a pre-signed URL lets the browser decide — images render inline, PDFs
open in a viewer. To force a download under a chosen name, pass a content
disposition:

```csharp
string? url = fileStorageService.GetPreSignedUrl(
    fileStorageService.GetBucketName(),
    key,
    responseContentDisposition: $"attachment; filename=\"{safeFileName}\"");
```

- `GetPreSignedUrl` is the only member that requires a bucket name explicitly;
  `GetBucketName()` supplies the service's own.
- **`safeFileName` is the name the user sees, not the key.** Keys carry a tick
  prefix and a stripped stem — do not hand one to a user as a filename. Build the
  display name from a sanitized value; sanitizing helpers are
  **common-extensions**'.
- This is a call-site decision, which is why the parameter lives on the service
  and not on the converter: a converter serializing a response field has no
  request context to decide from.

Use `GetPublicUrl` instead when the object is meant to be openly readable and the
link must not expire, and `GetServiceUrl` for server-to-server fetches — that one
is **not** host-rewritten, so a link from it points at an internal host and is
useless to a browser.

## 7. Ingest a file from an external URL

Download to a temp file, decide whether anything changed, re-upload only if it
did, then persist the new key.

```csharp
if (string.IsNullOrWhiteSpace(request.SourceUrl))
{
    return;
}

using MediaDownloadInfo info = await mediaManager.DownloadAsync(
    request.SourceUrl, cancellationToken: cancellationToken);

if (!info.IsSuccess)
{
    Log.Warning("Ingest failed for {source}: {error}", info.Source, info.Error);
    return;
}

await using FileStream stream = info.OpenFileStream();

string checksum = await ComputeChecksumAsync(stream, cancellationToken);
stream.Seek(0, SeekOrigin.Begin);

if (string.Equals(request.PreviousChecksum, checksum, StringComparison.Ordinal))
{
    return;     // upstream unchanged
}

string key = await fileStorageService.UploadAsync(EntityFolder, info.FileName, stream);

await repositoryWrapper.Repository<Entity>()
    .Find(x => x.Id == request.EntityId)
    .ExecuteUpdateAsync(
        x => x.SetProperty(p => p.Image, key)
              .SetProperty(p => p.ImageChecksum, checksum),
        cancellationToken);
```

What each step is doing, and what breaks if you change it:

- **`using` on the info, `await using` on the stream, in that order.** Declared
  this way they dispose in reverse — stream first, info second — which is what the
  temp-file lifecycle expects. See `references/media-downloads.md`.
- **Check `IsSuccess`.** A failed download returns normally; nothing throws.
- **Seek back to 0 after hashing.** Reading the stream to hash it leaves the
  position at the end, and the upload would send zero bytes.
- **The checksum comparison is the point of the whole flow.** Re-uploading an
  unchanged file mints a new key every run, orphaning the previous object each
  time and churning every cached response that embedded it.
- **`info.FileName`, not the temp name.** It is the name pulled from the source
  URL, so the key carries the original name and extension. A temp file name is not
  a key — `references/key-generation.md`.
- **Key and checksum are written together.** If only one lands, the next run
  either re-uploads a file that did not change or skips one that did.

`ComputeChecksumAsync` is a placeholder: no file this skill ships provides it. Use
any stable hash **over the raw bytes** — do not decode the bytes to text first,
which loses information on any content that is not valid text and can make two
different files hash alike.

Query and update mechanics — `Find`, `ExecuteUpdateAsync` — are
**ef-core-data-access**'s.

## Which upload call to make

| You have | Call |
|---|---|
| An `IFormFile` from a request | `UploadAsync(folder, file)` — returns the key, throws on failure, returns null for a null file |
| A `Stream` and a file name | `UploadAsync(folder, fileName, stream)` — returns the key, throws on failure |
| A `byte[]`, or a key you built yourself | The raw service overload — **check the `bool`** |
| A whole local directory | `DirectoryUploadAsync(fromFolder, toFolder)` — returns a `bool` |
| Bytes reaching the bucket by another route | `S3AwsExtensions.FormatFileName(fileName, folder)` for a conforming key |

The first two are extensions and already convert a failed upload into an
exception. The raw overloads do not — they log and return `false`. Discarding that
`bool` is how a row ends up pointing at an object that was never written.

Note the argument order: `FormatFileName` takes the **file name first**, the
folder second, while the `UploadAsync` extensions take the **folder first**. Both
parameters are strings, so a swap compiles.

## At a glance

Every row here fails silently — no exception, no log, no failing test.

| Call site | The thing that goes wrong |
|---|---|
| Converter registration | Omitted — responses serialize an object, not a URL |
| Converter registration | Added twice — the first wins, so a corrected second one does nothing |
| Composition-root chain | Registered twice — the last wins, the first is dead configuration |
| Response mapping | `IsSystem: true` on a value that is already an absolute URL |
| Create | The compensating delete omitted — rollback leaves an orphan object |
| Update | Old key deleted before the commit — rollback loses the bytes |
| Update | The `!= previousKey` guard omitted — an unchanged image is deleted |
| Attachment download | The bucket key handed to the user as the filename |
| External ingest | Temp file name used as the bucket key |
| External ingest | No seek after hashing — a zero-byte upload |
| Any raw upload | The `bool` discarded |
