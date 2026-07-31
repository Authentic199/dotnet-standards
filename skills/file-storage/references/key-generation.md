# Generating a bucket key

Read this file when you are producing a bucket key, adding an upload overload, or
need to tell the bucket-key format apart from the local temp-file format.

An object key is a persisted contract. It is written into a database column,
returned in responses for as long as the row lives, and is the only handle anyone
has on the stored bytes. It is generated in exactly one place so that it cannot
drift.

The service in `references/implementation.md` takes a key and does not care what
is in it. This extension is what decides. It sits one layer above the service and
is where every normal upload should enter — a call site that talks to the service
directly has taken on both naming and failure handling by hand.

## The key layout

```
{Folder}/{Ticks}_{SanitizedFileName}{Extension}
└─ caller ─┘└ generated ┘└──── from the upload ────┘
```

Each part earns its place:

- **`Folder`** is the caller's — a short constant on the service that owns this
  kind of file, never a computed string. It gives the bucket a browsable shape
  and makes a prefix-scoped cleanup possible.
- **`Ticks`** is `DateTime.UtcNow.Ticks`. Two uploads of the same filename produce
  two distinct keys, so an upload never silently overwrites an earlier one
  without a round trip to check — and as a side effect a folder listing sorts by
  upload time.
- **`SanitizedFileName`** is the original stem with special characters stripped,
  so spaces, diacritics and punctuation never reach the bucket or the URLs built
  from it.
- **`Extension`** is carried through unchanged from the original name, and is
  required — see the guard below.

The format string is declared **once**. Do not re-declare it in a module, a
handler or a service: a second copy is a second owner, and when the canonical one
changes the copy keeps writing the old shape without failing anything.

## What this layer adds over the service

The service logs and returns `false`. These extensions turn that `false` into a
thrown `InternalServerException`, so a caller either gets a key back or does not
continue. That conversion is the entire reason to prefer these overloads over the
raw service.

## Two prerequisites this file does not own

**`ReplaceSpecialCharacters`** is a string extension owned by
**common-extensions**. It is what makes `SanitizedFileName` safe. Look for it in
the solution before writing one, do not re-declare it beside the storage code,
and do not substitute an ad-hoc `Replace(" ", "")`.

**`InternalServerException`** is owned by **error-handling**. It is the type this
layer throws when the service reports a failed upload, and how it becomes a
response is that skill's concern, not this one's.

## `S3AwsExtensions.cs`

Lives in `Infrastructure/Facades/Common/Extensions/`, beside the other shared
extensions rather than inside the storage facade — it depends on the facade, not
the other way round.

```csharp
using Core.Common.Exceptions;
using Infrastructure.Facades.FileStorage;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Facades.Common.Extensions;

public static class S3AwsExtensions
{
    /// <summary>
    /// Object key layout: {Folder}/{Ticks}_{SanitizedFileName}{Extension}.
    /// The tick prefix keeps re-uploads of the same file name from colliding,
    /// and sorts a folder listing by upload time for free.
    /// </summary>
    private const string Format = "{0}/{1}_{2}{3}";

    public static async Task<string?> UploadAsync(this IS3AwsFileStorageService s3AwsFileStorageService, string folder, IFormFile? source)
    {
        if (source == null)
        {
            return null;
        }

        string key = FormatFileName(source.FileName, folder);

        bool success = await s3AwsFileStorageService.UploadAsync(source, key);

        if (!success)
        {
            throw new InternalServerException("File upload failed");
        }

        return key;
    }

    public static async Task<string> UploadAsync(this IS3AwsFileStorageService s3AwsFileStorageService, string folder, string filename, Stream source)
    {
        string key = FormatFileName(filename, folder);

        bool success = await s3AwsFileStorageService.UploadAsync(source, key);

        if (!success)
        {
            throw new InternalServerException("File upload failed");
        }

        return key;
    }

    /// <summary>
    /// Builds a conforming object key without uploading. Use it when the bytes
    /// reach the bucket by some other route and the key still has to match.
    /// </summary>
    public static string FormatFileName(string fileName, string folder)
    {
        if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
        {
            throw new NotSupportedException("Invalid file name");
        }

        return string.Format(
            Format,
            folder,
            DateTime.UtcNow.Ticks,
            Path.GetFileNameWithoutExtension(fileName).ReplaceSpecialCharacters(string.Empty),
            Path.GetExtension(fileName));
    }
}
```

### Three things to get right when recreating this

**The argument orders are not the same, and the mismatch is the trap.** The
`IFormFile` overload is `(folder, source)`. The `Stream` overload is
`(folder, filename, source)` — a stream carries no file name, so the caller must
supply one. But `FormatFileName` is **`(fileName, folder)`, file name first** —
the reverse. Both of its parameters are `string`, so a swap compiles silently and
produces keys shaped `{fileName}/{ticks}_{folder}`, which is only visible once
objects are sitting in the bucket. Check the signature; do not infer it from the
overloads.

**The overloads differ in nullability, deliberately.** The `IFormFile` overload
takes `IFormFile?` and returns `string?` — a null in gives a null out, which is
what lets a caller assign an optional upload straight to a property without a null
check of its own. The `Stream` overload takes a non-null stream and returns a
non-null key, because there is no meaningful "no stream" case.

**The extension guard is a real check, not a formality.** `Path.GetExtension`
returns an empty string, never null, for a name with no extension — so a guard
written as `is null` never fires and lets a shapeless key through. The layout has
a fixed four-part shape and a key missing its final segment cannot be read back
apart. Callers uploading content that has no filename — bytes from an external
source, a generated document — must supply a name with an extension.

## Two tick formats, one facade

Two tick-prefixed name formats live in this facade and they are not
interchangeable — check which side of the network boundary you are on:

| | Bucket object key | Local temp file name |
|---|---|---|
| Format | `{Folder}/{Ticks}_{Name}{Ext}` | `{Directory}/{Ticks}-{Name}{Ext}` |
| Separator | underscore | hyphen |
| Clock | `DateTime.UtcNow.Ticks` | `DateTimeOffset.UtcNow.Ticks` |
| Owner | `S3AwsExtensions` | `MediaInfo` |

The key format is durable — it is what a stored object is addressed by forever.
The temp format names a file that is deleted on close.

They look alike enough to copy by accident, and the direction that matters is
crossing from the temp side to the durable side: when a downloaded file is about
to be re-uploaded, **its temp name is not a key**. Pass the original file name
back through `FormatFileName` and let this file generate a fresh one. The temp
format belongs to the download pipeline — see `references/media-downloads.md`.
Never hand-write either one.

## Normalizations at a glance

| Spot | As found | This file | Reason |
|---|---|---|---|
| Namespace | `Infrastructure.Common.Extensions` | `Infrastructure.Facades.Common.Extensions` | Namespace matches the folder the file actually sits in; one variant already declares it this way. |
| `using` list | `using Infrastructure.Facades.Common.Extensions;` present | dropped | With the namespace corrected, that line became a self-import — a no-op that trips an unused-using analyzer. |
| Extension guard | `Path.GetExtension(fileName) is null` | `string.IsNullOrEmpty(Path.GetExtension(fileName))` | `GetExtension` returns an empty string, not null, so the original guard never fires. |
| Overloads | `IFormFile` only in the baseline | `IFormFile` **and** `(filename, Stream)` | Bytes that did not arrive as a form upload still need a conforming key. |
| Key building | inline in each upload extension | extracted to public `FormatFileName` | A caller that uploads by another route can still produce a matching key, instead of re-declaring the format. |
| Local variable | `fileName` (holding the composed key) | `key` | The variable holds a key, not a file name. |
