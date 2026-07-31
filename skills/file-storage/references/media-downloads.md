# Ingesting a file from an external URL

Read this file when the project must fetch a file that lives on someone else's
server and store it in its own bucket — a profile image from an identity
provider, a document from a partner API, a media asset from a CDN.

The shape is always **external URL → temp file on local disk → our bucket**. The
middle step exists because a large download should not be held in memory while it
is being re-uploaded.

**This pipeline is optional.** Most codebases that use the storage facade never
need it. If files only ever arrive as uploads on a request, stop here — the
facade in `references/implementation.md` and the key generator in
`references/key-generation.md` are all you need. Bring these files in only when
there is a real external source to ingest from.

## Pre-scaffold guard

1. **Does the project already download remote files anywhere** — a raw
   `HttpClient.GetStreamAsync` inside a service, a bespoke downloader? If so,
   that is the capability. Consolidate rather than adding a second one.
2. **Does the composition-root chain already contain `AddMediaManager()`?**
   Expect the storage and media registrations to sit far apart in a long chain;
   search it by method name rather than skimming it.

## Prerequisites

**1. The storage facade.** This pipeline ends in an upload, so
`IS3AwsFileStorageService` must exist — see `references/implementation.md`.

**2. The key generator.** The re-upload needs a conforming bucket key. See
`references/key-generation.md`, and read the tick-format table there before
writing any code here: **a temp file name is not a bucket key.**

**3. Two packages**, both on the `Infrastructure` project:

| Package | Provides |
|---|---|
| `Downloader` | `DownloadBuilder`, `IDownload`, `DownloadConfiguration`, `DownloadStatus` |
| `MimeTypesMap` | the `HeyRed.Mime` namespace and `MimeTypesMap.GetMimeType(...)`, the fallback when a server sends no `Content-Type` |

`HeyRed.Mime` comes from `MimeTypesMap`, not from a separately named package.

## Checklist

1. Create `Infrastructure/Facades/Common/MediaDownloads/`.
2. Add both packages.
3. Write `MediaInfo.cs` — the base type; nothing else compiles without it.
4. Write `MediaDownloadInfo.cs`.
5. Write `MediaManager.cs` — interface and implementation, one file.
6. Write `MediaDownloadExtension.cs` — only if you need to read objects back out
   of your **own** bucket through this pipeline.
7. Write `Startup.cs`.
8. Append `.AddMediaManager()` to the composition-root chain.

## `MediaInfo.cs`

Describes a remote file — name, content type, length — and owns the **local temp**
name format. Note the separator: a hyphen, and a `DateTimeOffset` clock. That is
deliberately different from the bucket key format, which uses an underscore and
`DateTime`. See `references/key-generation.md`.

> **One name is corrected here.** The source codebases spell this member
> `GetTempFileNameWithoutDicrectory`, and its backing constant the same way. Both
> ship below corrected. If you are reading an existing codebase, expect the
> misspelling.

```csharp
using System.Web;
using static System.IO.Path;

namespace Infrastructure.Facades.Common.MediaDownloads;

public class MediaInfo
{
    /// <summary>
    /// Local temp file name format: {Directory}/{Ticks}-{FileName}{Extension}.
    /// The hyphen and the DateTimeOffset clock keep this visibly distinct from a
    /// bucket object key, which uses an underscore and DateTime.
    /// </summary>
    private const string FileNameFormat = "{0}/{1}-{2}{3}";

    /// <summary>
    /// Local temp file name without a directory: {Ticks}-{FileName}{Extension}.
    /// </summary>
    private const string FileNameWithoutDirectoryFormat = "{0}-{1}{2}";

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public long Length { get; set; }

    /// <summary>
    /// A remote URL is not a file name. This pulls the last path segment out of a
    /// well-formed absolute URL, and falls back to treating the whole string as a
    /// name when it is not one.
    /// </summary>
    public static string GetFileNameFromUrl(string url)
    {
        if (IsValidUri(url, out Uri? uri))
        {
            return GetFileName(uri!.LocalPath);
        }

        return GetFileName(url);
    }

    public string Combine(params string[] directory)
        => string.Join(AltDirectorySeparatorChar, directory.Append(FileName));

    public string GetTempFileName(string directory)
        => string.Format(FileNameFormat, directory, DateTimeOffset.UtcNow.Ticks, GetFileNameWithoutExtension(FileName), GetExtension(FileName));

    public string GetTempFileNameWithoutDirectory()
        => string.Format(FileNameWithoutDirectoryFormat, DateTimeOffset.UtcNow.Ticks, GetFileNameWithoutExtension(FileName), GetExtension(FileName));

    private static bool IsValidUri(string uriString, out Uri? uri)
    {
        uri = null;
        return HttpUtility.UrlDecode(uriString) is { } uriDecode &&
               Uri.IsWellFormedUriString(uriDecode, UriKind.Absolute) &&
               Uri.TryCreate(uriDecode, UriKind.Absolute, out uri);
    }

    public MediaInfo(string fileName, string contentType, long length)
    {
        FileName = GetFileNameFromUrl(fileName);
        ContentType = contentType;
        Length = length;
    }
}
```

The constructor runs its `fileName` argument through `GetFileNameFromUrl`, so it
is safe to hand it a full URL — which is exactly what `MediaManager` does.

## `MediaDownloadInfo.cs`

The handle to a completed download: everything `MediaInfo` carries, plus where
the bytes landed, whether it worked, and how to read them. It is `IDisposable`,
and **disposing it deletes the temp file**.

**Read the dispose contract below before using it.**

```csharp
using Downloader;
using SystemFile = System.IO.File;

namespace Infrastructure.Facades.Common.MediaDownloads;

/// <summary>
/// Information about a completed media download, including its source, local
/// file path and status.
/// </summary>
/// <remarks>
/// Use this class with <see cref="IDisposable"/> correctly so the temp file is
/// released after use.
/// </remarks>
public class MediaDownloadInfo : MediaInfo, IDisposable
{
    private bool disposed;

    public string Source { get; set; }

    public string FilePath { get; set; }

    public long Duration { get; set; }

    public bool IsSuccess { get; set; }

    public string? Error { get; set; }

    public FileStream? Stream { get; private set; }

    public bool AutomaticCloseOnDispose { get; private set; }

    /// <summary>
    /// Opens the downloaded file as a stream. The caller owns the returned
    /// stream and must dispose it.
    /// </summary>
    /// <param name="automaticCloseOnDispose">Retained for source compatibility;
    /// see the dispose contract below this listing.</param>
    public FileStream OpenFileStream(bool automaticCloseOnDispose = true)
    {
        if (Stream is not null)
        {
            return Stream;
        }

        AutomaticCloseOnDispose = automaticCloseOnDispose;
        return new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, (int)Math.Min(Length, int.MaxValue), FileOptions.Asynchronous | FileOptions.DeleteOnClose);
    }

    /// <summary>
    /// Releases all resources used by the <see cref="MediaDownloadInfo"/> class,
    /// including deleting the downloaded temp file.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by the <see cref="MediaDownloadInfo"/> class.
    /// </summary>
    /// <param name="disposing">true to release managed resources as well.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            disposed = true;

            if (Stream is not null && AutomaticCloseOnDispose)
            {
                Stream.Dispose();
                Stream = null;
            }

            if (SystemFile.Exists(FilePath))
            {
                SystemFile.Delete(FilePath);
            }
        }
    }

    public MediaDownloadInfo(MediaInfo mediaInfo, IDownload download, long duration, string? error)
        : base(download.Filename, mediaInfo.ContentType, download.TotalFileSize)
    {
        Duration = duration;
        FilePath = download.Package.FileName;
        Source = download.Url;
        IsSuccess = download.Status == DownloadStatus.Completed;
        Error = error;
    }
}
```

### The dispose contract

This class ships exactly as the codebases it came from run it, and its dispose
logic reads as more than it does. Three facts, in order:

1. **`OpenFileStream` never assigns the `Stream` property.** It constructs and
   returns a new `FileStream` and leaves `Stream` null.
2. **Therefore the `Stream is not null && AutomaticCloseOnDispose` branch in
   `Dispose(bool)` is unreachable**, and the `automaticCloseOnDispose` parameter
   has no effect on anything.
3. **Therefore the caller owns the stream and must dispose it.** Disposing the
   `MediaDownloadInfo` does not close a stream you opened from it.

Disposing the *info* is still required — that is what deletes the temp file. Use
both, declaring the info first so the stream disposes first:

```csharp
using MediaDownloadInfo info = await mediaManager.DownloadAsync(url, cancellationToken: ct);
await using FileStream stream = info.OpenFileStream();
```

Do not rely on the parameter; do rely on `await using` at every call site that
opens a stream.

### Why the file survives long enough to read

`Dispose` deletes `FilePath`, and `OpenFileStream` opens with:

```csharp
FileShare.ReadWrite | FileShare.Delete
FileOptions.Asynchronous | FileOptions.DeleteOnClose
```

That flag set is what permits the path to be deleted while the handle is open and
the stream to keep returning content — which is why `MediaDownloadExtension`
below can dispose the `MediaDownloadInfo` and still hand back a usable stream.
`DeleteOnClose` is what removes the file once the caller finally disposes the
stream, so nothing is left behind even after the info's own delete has run.
`Dispose` guards its delete with `SystemFile.Exists`, so a file already removed is
not an error.

Two consequences for anyone editing this:

- **Do not remove `FileShare.Delete` from `OpenFileStream`.** It is not defensive
  boilerplate — the pipeline stops working without it.
- **Do not "fix" `DownloadStreamAsync` by dropping its `using`.** The `using` is
  what guarantees cleanup on the failure path, where no stream is ever returned.

## `MediaManager.cs`

Interface and implementation, one file. `InfoAsync` probes the URL with `HEAD` —
falling back to `GET` when the remote server rejects `HEAD` with `403` — and
`DownloadAsync` streams to a temp file under a `Files/Temps` directory beneath the
process's current directory.

The download configuration lives on the interface as a `static readonly` default,
so a caller can pass its own without the manager owning a settings class.

```csharp
using Downloader;
using HeyRed.Mime;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace Infrastructure.Facades.Common.MediaDownloads;

public interface IMediaManager
{
    Task<MediaInfo> InfoAsync(string mediaUrl, CancellationToken cancellationToken = default);

    Task<MediaDownloadInfo> DownloadAsync(string mediaUrl, DownloadConfiguration? configuration = default, CancellationToken cancellationToken = default);

    Task<MediaDownloadInfo> DownloadAsync(string getMediaUrl, string? headMediaUrl, DownloadConfiguration? configuration = default, CancellationToken cancellationToken = default);

    static readonly DownloadConfiguration DefaultDownloadConfiguration = new()
    {
        // File parts to download
        ChunkCount = 8,

        // Number of parallel downloads
        ParallelCount = 4,

        // Download parts of the file as parallel or not. The default value is false
        ParallelDownload = true,

        // Release memory buffer after each 50 MB
        MaximumMemoryBufferBytes = 1024 * 1024 * 50,

        // Clear package chunks data when download completed with failure
        ClearPackageOnCompletionWithFailure = true,

        // Before starting the download, reserve the storage space of the file as file size
        ReserveStorageSpaceBeforeStartingDownload = true,
    };
}

public class MediaManager : IMediaManager
{
    private static readonly string TempDirectory = Path.Combine(Environment.CurrentDirectory, "Files", "Temps");
    private readonly HttpClient httpClient;

    public MediaManager(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<MediaInfo> InfoAsync(string mediaUrl, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage httpRequest = new(HttpMethod.Head, mediaUrl);
        HttpResponseMessage httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken);

        // Some servers reject HEAD outright. Retry the probe as GET rather than
        // failing the whole download over a metadata request.
        if (httpResponse.StatusCode == HttpStatusCode.Forbidden)
        {
            HttpRequestMessage httpGetRequest = new(HttpMethod.Get, mediaUrl);
            httpResponse = await httpClient.SendAsync(httpGetRequest, cancellationToken);
        }

        if (!httpResponse.IsSuccessStatusCode || httpResponse.StatusCode is not HttpStatusCode.OK)
        {
            throw new ArgumentException(ExceptionMessage(nameof(InfoAsync), $"Can not get media information with url {mediaUrl}"));
        }

        HttpContentHeaders responseContentHeaders = httpResponse.Content.Headers;
        responseContentHeaders.TryGetValues(HeaderNames.ContentType, out IEnumerable<string>? contentTypes);
        responseContentHeaders.TryGetValues(HeaderNames.ContentLength, out IEnumerable<string>? contentLengths);

        // Fall back to inferring the type from the URL when the server sends none.
        return new(
                mediaUrl,
                contentTypes?.FirstOrDefault() ?? MimeTypesMap.GetMimeType(mediaUrl),
                long.TryParse(contentLengths?.FirstOrDefault(), out long longValue) ? longValue : default
            );
    }

    public async Task<MediaDownloadInfo> DownloadAsync(string mediaUrl, DownloadConfiguration? configuration = default, CancellationToken cancellationToken = default)
        => await DownloadAsync(mediaUrl, mediaUrl, configuration, cancellationToken);

    // Two URLs, because a pre-signed URL is signed for one verb. The HEAD probe and
    // the GET download need separately signed URLs when the source is our own bucket.
    public async Task<MediaDownloadInfo> DownloadAsync(string getMediaUrl, string? headMediaUrl, DownloadConfiguration? configuration = null, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(TempDirectory);
        MediaInfo mediaInfo = await InfoAsync(headMediaUrl ?? getMediaUrl, cancellationToken);

        configuration ??= IMediaManager.DefaultDownloadConfiguration;
        IDownload download = DownloadBuilder.New()
            .WithUrl(getMediaUrl)
            .WithFileLocation(mediaInfo.GetTempFileName(TempDirectory))
            .WithConfiguration(configuration)
            .Build();

        Stopwatch sw = Stopwatch.StartNew();

        string? errorMessage = null;

        // The completion event fires whether the download succeeded or failed, so this
        // is where the error is captured; StartAsync itself does not throw on failure.
        download.DownloadFileCompleted += (_, e) => errorMessage = e.Error?.Message;

        await download.StartAsync(cancellationToken);
        long duration = sw.ElapsedMilliseconds;

        return new(mediaInfo, download, duration, errorMessage);
    }

    private static string ExceptionMessage(string functionName, string message)
        => $"\n----------------\n-----{nameof(MediaManager)} : {functionName}\n-----Exception: {message}\n----------------";
}
```

Three things to know before calling it:

**A failed download does not throw.** `StartAsync` returns normally and the
failure lands in `IsSuccess` and `Error` — the same shape as the storage
service's `bool`. Check `IsSuccess`, always.

**`InfoAsync` does throw**, an `ArgumentException`, when the probe fails. The two
halves of this class do not share a failure style.

**The temp directory sits under the process's current directory.** `Files/Temps`
is created on every download call, so the process needs write access there.

## `MediaDownloadExtension.cs`

The bridge from our own bucket back to a stream — needed only when this process
must *read* an object it stored, rather than hand a client a URL. It uses
`GetServiceUrl`, not `GetPreSignedUrl`, because this is a server-to-server fetch
that must not be host-rewritten.

```csharp
using Amazon.S3;
using Infrastructure.Facades.FileStorage;

namespace Infrastructure.Facades.Common.MediaDownloads;

public static class MediaDownloadExtension
{
    /// <summary>
    /// Downloads an object from this service's own bucket and returns it as a
    /// stream. The caller owns the returned stream and must dispose it.
    /// </summary>
    public static async Task<Stream> DownloadStreamAsync(this IMediaManager mediaManager, IS3AwsFileStorageService fileStorageService, string s3ObjectKey, bool automaticCloseOnDispose = false, CancellationToken cancellationToken = default)
    {
        string getUrl = fileStorageService.GetServiceUrl(s3ObjectKey, verb: HttpVerb.GET)!;
        string headUrl = fileStorageService.GetServiceUrl(s3ObjectKey, verb: HttpVerb.HEAD)!;
        using MediaDownloadInfo mediaDownloadInfo = await mediaManager.DownloadAsync(getUrl, headUrl, cancellationToken: cancellationToken);

        if (!mediaDownloadInfo.IsSuccess || !File.Exists(mediaDownloadInfo.FilePath))
        {
            throw new InvalidOperationException($"{nameof(MediaDownloadExtension)}:{nameof(DownloadStreamAsync)} Failed to download media url `{getUrl}` with error {mediaDownloadInfo.Error}");
        }

        return mediaDownloadInfo.OpenFileStream(automaticCloseOnDispose);
    }
}
```

This is the one place `GetServiceUrl` is used, and it shows why the verb
parameter exists: the probe is signed for `HEAD`, the download for `GET`, and a
single URL signed for `GET` will not serve the probe.

The `using` declaration disposes the `MediaDownloadInfo` — deleting the temp path
— as this method returns, and the stream handed back stays readable for the
reason given under the dispose contract. **The `automaticCloseOnDispose` argument
it passes through has no effect**, on either side; it is kept so the signature
matches the codebases this came from. The contract that matters is unchanged:
**the caller disposes the returned stream.**

## `Startup.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Facades.Common.MediaDownloads;

public static class Startup
{
    public static IServiceCollection AddMediaManager(this IServiceCollection services)
    {
        // MediaManager's constructor takes an HttpClient. Registering it as a typed
        // client guarantees that dependency is satisfied by this call alone, rather
        // than by whatever else in the application happens to have registered one.
        services.AddHttpClient<IMediaManager, MediaManager>();

        return services;
    }
}
```

> This registration is transient-lifetime, where the codebases it came from
> registered the manager as scoped. `MediaManager` holds no per-request state, so
> nothing depends on the scope.

Typed-client configuration beyond this one line — handlers, timeouts, resilience
policies — belongs to **http-client-factory**.

## The wiring line

```csharp
// Infrastructure composition root — the same fluent chain as the storage facade
services
    // … existing facades, including .AddS3AwsFileStorage() …
    .AddMediaManager();
```

## Temp names are not bucket keys

The temp names produced here and the bucket keys produced by
`references/key-generation.md` look alike and are not the same format:

| | Local temp file name | Bucket object key |
|---|---|---|
| Format | `{Directory}/{Ticks}-{Name}{Ext}` | `{Folder}/{Ticks}_{Name}{Ext}` |
| Separator | hyphen | underscore |
| Clock | `DateTimeOffset.UtcNow.Ticks` | `DateTime.UtcNow.Ticks` |
| Owner | `MediaInfo` | `S3AwsExtensions` |

A temp name is a scratch path deleted on close. A bucket key is how a stored
object is addressed forever. When downloaded bytes go into the bucket, the key
comes from `references/key-generation.md` — pass `info.FileName`, the name pulled
from the source URL, and never `GetTempFileName`.

The end-to-end consumer flow is in `references/usage-patterns.md`, step 7.

## Normalizations at a glance

| Spot | As found | This file | Reason |
|---|---|---|---|
| `Startup` registration | `services.AddScoped<IMediaManager, MediaManager>()` | `services.AddHttpClient<IMediaManager, MediaManager>()` | `MediaManager`'s constructor takes an `HttpClient`; the scoped registration only resolves if something unrelated registered one. |
| `GetTempFileNameWithoutDicrectory` and `FileNameWithoutDicrectoryFormat` | misspelled | `...Directory...` on both | Spelling. Corrected together so the file does not carry two spellings of one word three lines apart. |
| `MediaInfo` format doc comment | says `{Prefix}_{FileName}` | says `{Ticks}-{FileName}` | The comment described an underscore; the code uses a hyphen. Corrected to match the code, which is the bucket-key distinction. |
| XML doc comments | Vietnamese | English | Artifact language. `OpenFileStream`'s doc now states the caller-dispose contract instead of the parameter's advertised effect. |
| Exception message typo | `"Can't not get"` | `"Can not get"` | Cosmetic, in a message string, no behaviour change. |
| `ExceptionMessage` body | dashed multi-line format | unchanged | Kept verbatim. |
| Console progress writes | `Console.Out.WriteLine` before and after download | removed | Unstructured console writes in a library; diagnostics belong on the logger. |
| `Stream` property, `AutomaticCloseOnDispose` | present, inert | unchanged | Shipped verbatim. See the dispose contract — do not "fix" this without changing every caller. |
