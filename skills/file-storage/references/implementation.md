# Recreating the file-storage facade

Read this file when a codebase needs S3-backed file storage and has none, or when
you need the exact text of a facade file that already exists elsewhere in the
solution.

Every file below is complete. Recreate them whole, with these namespaces, into
`Infrastructure/Facades/FileStorage/`. Do not recreate a trimmed subset, do not
rename the types, and do not inline any of this at a call site — a service that
needs storage injects `IS3AwsFileStorageService`.

If the codebase already has a storage capability, extend it in place. The guard
below is how you tell the difference.

## Pre-scaffold guard

Before creating anything, check all four:

1. **Does any folder already own `IAmazonS3`?** A storage capability may exist
   under a different name than the one this file scaffolds. If so, you would be
   adding a *second* service type over the same bucket — two clients, two
   configurations, two things to keep in step. Use what is there.
2. **Does the composition-root chain already contain a storage registration** —
   an `AddS3AwsFileStorage()`, `AddFileStorage()`, or similarly named line? A
   duplicate registration of the same service type does not fail: both
   descriptors stay in the container and a normal resolve silently gets the last
   one, leaving the first as dead configuration nobody notices.
3. **Is there already an `S3AwsSettings` section** in configuration? A second one
   under a different name splits the credentials across two places.
4. **Is `S3FilePathConverter` already registered** in the JSON options? A project
   can have the facade and be missing only this step — see step 11.

A hit on 1–3 means the capability exists: stop scaffolding and use it in place. A
hit on 4 alone means the scaffold was abandoned half-done; finish it rather than
starting over.

## Prerequisites this scaffold never creates

Each has an owner elsewhere. Verify all three before writing any file.

**1. Packages.** The facade will not compile without these:

| Package | Provides |
|---|---|
| `AWSSDK.S3` | `IAmazonS3`, `TransferUtility`, every request/response type below |
| `ReHackt.Extensions.Options.Validation` | `ValidateDataAnnotationsRecursively()` in `Startup.cs` |

`ValidateDataAnnotationsRecursively()` is not framework code and is not declared
anywhere in a solution that uses it — it is package-supplied. If that package is
unavailable, replace that one call with the framework's
`ValidateDataAnnotations()`. Everything else in `Startup.cs` stays as written.

**2. A string-sanitizing helper.** The key generator calls
`ReplaceSpecialCharacters`. It is not part of this facade — see
`references/key-generation.md` and **common-extensions**. Look for it in the
solution before writing one, and do not re-declare it beside the storage code.

**3. An exception base type.** `S3FileUploadException` derives from the
solution's `CustomException`, and the key-generation extension throws
`InternalServerException`. Both belong to **error-handling**. If the solution has
no exception hierarchy yet, stop and settle that first — this facade should not
invent one, and deriving from `Exception` directly to route around a missing base
type is not a shortcut, it is a divergence.

**If any prerequisite is missing: stop, report, and let the caller choose.**
Scaffolding the missing piece first, as its own task, is the normal answer.

## Checklist

Work in this order. Each step is done only when the named artifact exists.

1. Create `Infrastructure/Facades/FileStorage/`.
2. Add `AWSSDK.S3` to the `Infrastructure` project.
3. Write `S3FilePath.cs`.
4. Write `FileResponseConverter.cs`.
5. Write `S3AwsSettings.cs`.
6. Write `S3AwsFileStorageService.cs` — interface and implementation, one file.
7. Write `S3FileUploadException.cs`.
8. Write `Startup.cs`.
9. Add the storage configuration topic file and its loader line.
10. Append `.AddS3AwsFileStorage()` to the composition-root chain.
11. Register `S3FilePathConverter` in the JSON serializer options.
12. Add the key generator from `references/key-generation.md`.

Steps 9–11 are where scaffolds get abandoned half-done. **Step 11 in
particular**: without it every `S3FilePath` serializes as a JSON object exposing
`originPath` and `isSystem` instead of a URL string, and nothing throws — clients
receive raw bucket keys and the first sign of trouble is a broken image.

## `S3FilePath.cs`

The unit of currency. A response carries this type; the database carries the
plain string inside it. A struct with two members and no behaviour — all the
behaviour lives in the converter that serializes it.

```csharp
namespace Infrastructure.Facades.FileStorage;

/// <summary>
/// A stored object key. Serializes to a URL, never to the raw key.
/// </summary>
public struct S3FilePath
{
    /// <param name="relativePath">The object key inside the bucket, or an absolute
    /// external URL when <paramref name="isSystem"/> is false.</param>
    /// <param name="isSystem">True when this codebase owns the object and the value
    /// is a bucket key to be pre-signed; false when the value is already a complete
    /// URL hosted elsewhere and must be emitted verbatim.</param>
    public S3FilePath(string? relativePath, bool isSystem = true)
    {
        OriginPath = relativePath;
        IsSystem = isSystem;
    }

    public string? OriginPath { get; set; }

    public bool IsSystem { get; }
}
```

`IsSystem` is get-only on purpose: the decision is made once, at the mapping site
that constructs the value, and nothing downstream can flip it.

## `FileResponseConverter.cs`

**The file name and the class name differ** — the file is
`FileResponseConverter.cs`, the class is `S3FilePathConverter`. Searching a
solution for `S3FilePathConverter.cs` finds nothing; search for the type.

This converter is what makes the whole design work: it turns a stored key into a
URL at serialization time, so no URL is ever persisted or cached.

It builds its **own** `IAmazonS3` from `IConfiguration` rather than taking one
from the container. That is a consequence of where it is registered: JSON
serializer options are configured while the service provider is still being
built, so the converter is constructed by hand at that point and has no container
to resolve from.

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Facades.FileStorage;

public class S3FilePathConverter : JsonConverter<S3FilePath>
{
    private readonly IAmazonS3 s3Client;
    private readonly S3AwsSettings s3AwsSettings;
    private readonly Protocol protocol;

    public S3FilePathConverter(IConfiguration configuration)
    {
        s3AwsSettings = configuration.GetRequiredSection(nameof(S3AwsSettings)).Get<S3AwsSettings>()!;

        AmazonS3Config amazonS3Config = new()
        {
            ServiceURL = s3AwsSettings.ServiceUrl,
            ForcePathStyle = true,
        };
        s3Client = new AmazonS3Client(s3AwsSettings.AccessKey, s3AwsSettings.SecretKey, amazonS3Config);

        // The scheme must come from PublicUrl, not ServiceUrl: the returned URL has
        // ServiceUrl replaced by PublicUrl below, so signing under ServiceUrl's scheme
        // can stamp a scheme the client never sees.
        protocol = new Uri(s3AwsSettings.PublicUrl).Scheme.Contains(nameof(Protocol.HTTPS), StringComparison.OrdinalIgnoreCase)
            ? Protocol.HTTPS
            : Protocol.HTTP;
    }

    public override S3FilePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // The token already holds the path as a plain string. Re-parsing that string
        // as JSON — Deserialize<S3FilePath>(reader.GetString()) — throws on every real
        // payload, because a bucket key is not a JSON document. Construct it directly.
        return new S3FilePath(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, S3FilePath value, JsonSerializerOptions options)
    {
        if (value.OriginPath is null)
        {
            writer.WriteNullValue();
            return;
        }

        // A value this codebase owns is a bucket key and must be signed before a
        // client can fetch it. A value it does not own is already a complete URL —
        // signing it would corrupt it.
        writer.WriteStringValue(value.IsSystem
            ? GetPreSignedUrl(s3AwsSettings.BucketName, value.OriginPath)
            : value.OriginPath);
    }

    public string? GetPreSignedUrl(string bucketName, string key, DateTime? expires = default)
    {
        expires ??= DateTime.UtcNow.AddMinutes(s3AwsSettings.PreSignedUrlExpirationInMinutes);

        GetPreSignedUrlRequest request = new()
        {
            BucketName = bucketName,
            Key = key,
            Protocol = protocol,
            Expires = (DateTime)expires,
        };

        return s3Client.GetPreSignedURL(request)
            .Replace(s3AwsSettings.ServiceUrl, s3AwsSettings.PublicUrl, StringComparison.OrdinalIgnoreCase);
    }

    public string GetPublicUrl(string key) => $"{s3AwsSettings.PublicUrl}/{s3AwsSettings.BucketName}/{key}";
}
```

The converter's `GetPreSignedUrl` deliberately stops at three parameters where
the service's takes a fourth. It is serializing a field on a response object and
has no access to the request that asked for it, so it could never have a
content-disposition to pass — the parameter would be unreachable forever. Serving
a file as a named download is a call-site decision that goes through the service;
see `references/usage-patterns.md`.

> **`S3FilePath` is a response type in practice.** Every construction site in the
> codebases this facade came from is a response mapping — the struct goes out, it
> does not come in. `Read` is implemented anyway so that binding an `S3FilePath`
> on a request model does not fail the request outright. Be aware the round trip
> is not symmetric: `Write` emits a pre-signed URL, so a client echoing a response
> back sends a URL, and `Read` will store that URL in `OriginPath` where a bucket
> key was expected. Accept a plain `string` key on request models instead.

## `S3AwsSettings.cs`

Six properties, one section. `Validate` runs at startup, so a missing endpoint or
credential fails the boot instead of the first upload.

`ServiceUrl` and `PublicUrl` are two different hosts for the same storage on
purpose: the one this process talks to, and the one a browser can reach.
`GetPreSignedUrl` rewrites the first into the second. Where they are the same
value the rewrite is a no-op and nothing changes — but keep both properties,
because the pair is what makes the facade portable between a private network and
a public one.

```csharp
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Facades.FileStorage;

public class S3AwsSettings : IValidatableObject
{
    public string ServiceUrl { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string PublicUrl { get; set; } = string.Empty;

    public double PreSignedUrlExpirationInMinutes { get; set; } = 1440;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(ServiceUrl))
        {
            yield return new ValidationResult(
                $"{nameof(S3AwsSettings)}.{nameof(ServiceUrl)} is not configured.",
                new[] { nameof(ServiceUrl) });
        }

        if (string.IsNullOrEmpty(AccessKey))
        {
            yield return new ValidationResult(
                $"{nameof(S3AwsSettings)}.{nameof(AccessKey)} is not configured.",
                new[] { nameof(AccessKey) });
        }

        if (string.IsNullOrEmpty(SecretKey))
        {
            yield return new ValidationResult(
                $"{nameof(S3AwsSettings)}.{nameof(SecretKey)} is not configured.",
                new[] { nameof(SecretKey) });
        }

        if (string.IsNullOrEmpty(BucketName))
        {
            yield return new ValidationResult(
                $"{nameof(S3AwsSettings)}.{nameof(BucketName)} is not configured.",
                new[] { nameof(BucketName) });
        }

        if (string.IsNullOrEmpty(PublicUrl))
        {
            yield return new ValidationResult(
                $"{nameof(S3AwsSettings)}.{nameof(PublicUrl)} is not configured.",
                new[] { nameof(PublicUrl) });
        }
    }
}
```

`PreSignedUrlExpirationInMinutes` defaults to `1440` — twenty-four hours — and is
the only property not validated, because a zero here is a configuration choice
rather than a missing value.

## `S3AwsFileStorageService.cs`

Interface and implementation in one file, interface first. Three facts about this
class carry the whole failure contract:

- **No method throws on a storage failure.** Every upload path funnels into the
  `TransferUtilityUploadRequest` overload, which catches, logs, and returns
  `false`. (Argument preparation before the `try` — copying an `IFormFile` into a
  `MemoryStream`, allocating one over a `byte[]` — can still throw; it is the
  storage call that is swallowed.) Converting `false` into an exception is the
  key-generation extension's job — see `references/key-generation.md`.
- **Deletes swallow their own failures** and return `void`. That is what makes
  `DeleteAsync` safe to call from a `catch` block as a compensating action: it
  cannot mask the original exception.
- **`defaultS3CannedAcl` is `S3CannedACL.PublicRead`.** Every upload overload
  applies it when the caller passes no `s3CannedAcl`, so objects are readable by
  anyone holding the URL unless a caller says otherwise. Change the field, or pass
  an explicit ACL, when an object must not be.

```csharp
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;

namespace Infrastructure.Facades.FileStorage;

public interface IS3AwsFileStorageService
{
    Task<bool> UploadAsync(TransferUtilityUploadRequest request, CancellationToken cancellationToken = default);

    Task<bool> UploadAsync(IFormFile source, string key, S3CannedACL? s3CannedAcl = default, CancellationToken cancellationToken = default);

    Task<bool> UploadAsync(byte[] source, string key, S3CannedACL? s3CannedAcl = default, CancellationToken cancellationToken = default);

    Task<bool> UploadAsync(Stream source, string key, S3CannedACL? s3CannedAcl = default, CancellationToken cancellationToken = default);

    Task<bool> UploadAsync(string source, string key, S3CannedACL? s3CannedAcl = default, CancellationToken cancellationToken = default);

    Task<bool> DirectoryUploadAsync(string fromFolder, string toFolder, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    string? GetPreSignedUrl(string bucketName, string key, DateTime? expires = default, string? responseContentDisposition = default);

    string GetPublicUrl(string key);

    string GetBucketName();

    Task<UploadPartResponse> UploadPartAsync(UploadPartRequest request);

    Task<CompleteMultipartUploadResponse> CompleteMultipartUploadAsync(CompleteMultipartUploadRequest request);

    Task<AbortMultipartUploadResponse> AbortMultipartUploadAsync(AbortMultipartUploadRequest request);

    Task<InitiateMultipartUploadResponse> InitiateMultipartUploadAsync(InitiateMultipartUploadRequest request);

    Task<PutObjectResponse> PutObjectAsync(string path, string key, CancellationToken cancellationToken = default);

    string? GetServiceUrl(string key, DateTime? expires = default, HttpVerb verb = HttpVerb.GET);

    string? GetServiceUrl(string bucketName, string key, DateTime? expires = default, HttpVerb verb = HttpVerb.GET);
}

public class S3AwsFileStorageService : IS3AwsFileStorageService
{
    private readonly IAmazonS3 s3Client;
    private readonly S3AwsSettings s3AwsSettings;
    private readonly S3CannedACL defaultS3CannedAcl = S3CannedACL.PublicRead;
    private readonly Protocol protocol;

    public S3AwsFileStorageService(IOptions<S3AwsSettings> options)
    {
        s3AwsSettings = options.Value;

        AmazonS3Config amazonS3Config = new()
        {
            ServiceURL = s3AwsSettings.ServiceUrl,
            ForcePathStyle = true,
        };
        s3Client = new AmazonS3Client(s3AwsSettings.AccessKey, s3AwsSettings.SecretKey, amazonS3Config);

        // The scheme must come from PublicUrl, not ServiceUrl: the returned URL has
        // ServiceUrl replaced by PublicUrl below, so signing under ServiceUrl's scheme
        // can stamp a scheme the client never sees.
        protocol = new Uri(s3AwsSettings.PublicUrl).Scheme.Contains(nameof(Protocol.HTTPS), StringComparison.OrdinalIgnoreCase)
            ? Protocol.HTTPS
            : Protocol.HTTP;
    }

    // Every other upload overload funnels here, so the try/catch and the
    // log-and-return-false contract are written exactly once.
    public async Task<bool> UploadAsync(TransferUtilityUploadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            TransferUtility transferUtility = new(s3Client);
            await transferUtility.UploadAsync(request, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ExceptionAWSS3Upload");
            return false;
        }
    }

    public async Task<bool> UploadAsync(IFormFile source, string key, S3CannedACL? s3CannedAcl = default, CancellationToken cancellationToken = default)
    {
        using MemoryStream memoryStream = new();
        source.CopyTo(memoryStream);

        TransferUtilityUploadRequest request = new()
        {
            InputStream = memoryStream,
            BucketName = s3AwsSettings.BucketName,
            Key = key,
            CannedACL = s3CannedAcl ?? defaultS3CannedAcl,
        };

        return await UploadAsync(request, cancellationToken);
    }

    public async Task<bool> UploadAsync(byte[] source, string key, S3CannedACL? s3CannedAcl = default, CancellationToken cancellationToken = default)
    {
        using MemoryStream memoryStream = new(source);

        TransferUtilityUploadRequest request = new()
        {
            InputStream = memoryStream,
            BucketName = s3AwsSettings.BucketName,
            Key = key,
            CannedACL = s3CannedAcl ?? defaultS3CannedAcl,
        };

        return await UploadAsync(request, cancellationToken);
    }

    // The caller owns this stream and its lifetime; this overload does not dispose it.
    public async Task<bool> UploadAsync(Stream source, string key, S3CannedACL? s3CannedAcl = default, CancellationToken cancellationToken = default)
    {
        TransferUtilityUploadRequest request = new()
        {
            InputStream = source,
            BucketName = s3AwsSettings.BucketName,
            Key = key,
            CannedACL = s3CannedAcl ?? defaultS3CannedAcl,
        };

        return await UploadAsync(request, cancellationToken);
    }

    // `source` here is a local file path, not the file's contents.
    public async Task<bool> UploadAsync(string source, string key, S3CannedACL? s3CannedAcl = default, CancellationToken cancellationToken = default)
    {
        TransferUtilityUploadRequest request = new()
        {
            FilePath = source,
            BucketName = s3AwsSettings.BucketName,
            Key = key,
            CannedACL = s3CannedAcl ?? defaultS3CannedAcl,
        };

        return await UploadAsync(request, cancellationToken);
    }

    public async Task<bool> DirectoryUploadAsync(string fromFolder, string toFolder, CancellationToken cancellationToken = default)
    {
        try
        {
            TransferUtilityUploadDirectoryRequest request = new()
            {
                BucketName = s3AwsSettings.BucketName,
                Directory = fromFolder,
                KeyPrefix = toFolder,
                UploadFilesConcurrently = true,
            };

            TransferUtility transferUtility = new(s3Client);
            await transferUtility.UploadDirectoryAsync(request, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ExceptionAWSS3Upload");
            return false;
        }
    }

    // Returns void and swallows its own failure, which is what makes it safe to
    // call from a catch block without masking the exception being handled.
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            DeleteObjectRequest request = new()
            {
                BucketName = s3AwsSettings.BucketName,
                Key = key,
            };
            await s3Client.DeleteObjectAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ExceptionAWSS3Delete");
        }
    }

    // One batched request instead of N round trips. An empty list is a no-op,
    // not an error — the caller is usually deleting whatever an entity owned.
    public async Task DeleteManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        if (!keys.Any())
        {
            Log.Information("--S3-- {action}: keys is empty to delete", nameof(DeleteManyAsync));
            return;
        }

        try
        {
            List<KeyVersion> keyVersions = keys.Select(x => new KeyVersion
            {
                Key = x,
            }).ToList();

            DeleteObjectsRequest multiObjectDeleteRequest = new()
            {
                BucketName = s3AwsSettings.BucketName,
                Objects = keyVersions,
            };
            await s3Client.DeleteObjectsAsync(multiObjectDeleteRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ExceptionAWSS3Delete");
        }
    }

    // The client-facing URL: expiring, and host-rewritten to PublicUrl.
    public string? GetPreSignedUrl(string bucketName, string key, DateTime? expires = default, string? responseContentDisposition = default)
    {
        expires ??= DateTime.UtcNow.AddMinutes(s3AwsSettings.PreSignedUrlExpirationInMinutes);

        GetPreSignedUrlRequest request = new()
        {
            BucketName = bucketName,
            Key = key,
            Protocol = protocol,
            Expires = (DateTime)expires,

            // Set this to make the browser download the object under a chosen file
            // name instead of rendering it inline. Leaving it null keeps the
            // object's own headers, which is what most read paths want.
            ResponseHeaderOverrides = string.IsNullOrWhiteSpace(responseContentDisposition)
                ? null
                : new ResponseHeaderOverrides { ContentDisposition = responseContentDisposition },
        };

        return s3Client.GetPreSignedURL(request)
            .Replace(s3AwsSettings.ServiceUrl, s3AwsSettings.PublicUrl, StringComparison.OrdinalIgnoreCase);
    }

    // Plain concatenation: no signature, no expiry. Only for objects that are
    // meant to be openly readable.
    public string GetPublicUrl(string key) => $"{s3AwsSettings.PublicUrl}/{s3AwsSettings.BucketName}/{key}";

    public string GetBucketName() => s3AwsSettings.BucketName;

    public async Task<UploadPartResponse> UploadPartAsync(UploadPartRequest request)
    {
        return await s3Client.UploadPartAsync(request);
    }

    public async Task<CompleteMultipartUploadResponse> CompleteMultipartUploadAsync(CompleteMultipartUploadRequest request)
    {
        return await s3Client.CompleteMultipartUploadAsync(request);
    }

    public async Task<AbortMultipartUploadResponse> AbortMultipartUploadAsync(AbortMultipartUploadRequest request)
    {
        return await s3Client.AbortMultipartUploadAsync(request);
    }

    public async Task<InitiateMultipartUploadResponse> InitiateMultipartUploadAsync(InitiateMultipartUploadRequest request)
    {
        return await s3Client.InitiateMultipartUploadAsync(request);
    }

    // `path` is a local file path. Unlike the upload overloads this one returns
    // the raw response and does not catch — callers get the exception.
    public async Task<PutObjectResponse> PutObjectAsync(string path, string key, CancellationToken cancellationToken = default)
    {
        PutObjectRequest putRequest = new()
        {
            BucketName = s3AwsSettings.BucketName,
            Key = key,
            FilePath = path,
        };

        return await s3Client.PutObjectAsync(putRequest, cancellationToken);
    }

    public string? GetServiceUrl(string key, DateTime? expires = default, HttpVerb verb = HttpVerb.GET)
        => GetServiceUrl(s3AwsSettings.BucketName, key, expires, verb);

    // Same signing as GetPreSignedUrl, minus the ServiceUrl -> PublicUrl rewrite,
    // and with the verb open. This is the server-to-server URL: this process
    // fetching an object over the network it is already on. Never hand one of
    // these to a client — it points at an internal host.
    public string? GetServiceUrl(string bucketName, string key, DateTime? expires = default, HttpVerb verb = HttpVerb.GET)
    {
        expires ??= DateTime.UtcNow.AddMinutes(s3AwsSettings.PreSignedUrlExpirationInMinutes);

        GetPreSignedUrlRequest request = new()
        {
            BucketName = bucketName,
            Key = key,
            Protocol = protocol,
            Verb = verb,
            Expires = (DateTime)expires,
        };

        return s3Client.GetPreSignedURL(request);
    }
}
```

The four multipart members and `PutObjectAsync` are thin pass-throughs to the SDK
client, with no try/catch. They exist so a caller with a genuinely large or
resumable upload does not need its own `IAmazonS3`. They deliberately do **not**
share the `bool`-returning contract: a caller driving a multi-step protocol needs
the failure, not a `false`.

## `S3FileUploadException.cs`

```csharp
using Core.Common.Exceptions;
using System.Runtime.Serialization;

namespace Infrastructure.Facades.FileStorage;

[Serializable]
public sealed class S3FileUploadException : CustomException
{
    public S3FileUploadException(string? message)
       : base(message)
    {
    }

    public S3FileUploadException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public S3FileUploadException(string? message, ICollection<string> addedKeys)
        : base(message)
    {
        AddedKeys = addedKeys;
    }

    public S3FileUploadException(string? message, ICollection<string> addedKeys, Exception? innerException)
        : base(message, innerException)
    {
        AddedKeys = addedKeys;
    }

    public ICollection<string>? AddedKeys { get; set; }

    private S3FileUploadException()
    {
    }

    private S3FileUploadException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
```

> **When to reach for this.** `S3FileUploadException` carries an `AddedKeys`
> collection, which exists so a caller that failed partway through a multi-file
> upload can delete the objects it already wrote. The single-file upload
> extension does not need that and throws `InternalServerException` instead. Keep
> this exception for batch upload paths, where the rollback list is what makes
> the failure recoverable.

The four public constructors are two pairs — with and without `AddedKeys`, each
with and without an inner exception. The two private ones exist for serialization
and are not called by hand. `CustomException`, its middleware and the response
envelope belong to **error-handling**; if the solution's base type sits under a
different namespace, that `using` is the line to change.

## `Startup.cs`

This file is identical in every codebase the facade appears in — treat it as
fixed.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Facades.FileStorage;

internal static class Startup
{
    internal static IServiceCollection AddS3AwsFileStorage(this IServiceCollection services)
    {
        services.AddOptions<S3AwsSettings>()
            .BindConfiguration(nameof(S3AwsSettings))
            .ValidateDataAnnotationsRecursively()
            .ValidateOnStart();

        services.AddScoped(typeof(IS3AwsFileStorageService), typeof(S3AwsFileStorageService));

        return services;
    }
}
```

Three things to keep as written. `BindConfiguration(nameof(S3AwsSettings))` ties
the configuration section name to the class name, so renaming one without the
other breaks binding silently at startup rather than loudly at compile time.
`ValidateOnStart()` is what turns a missing setting into a boot failure — without
it, `Validate` runs on first resolve, which is a request. And both the class and
the method are `internal`: the facade is registered from the composition root
inside the same assembly, and nothing outside should call it.

## The configuration section

Storage settings live in their own configuration topic file rather than in the
main application settings — one file per facade, each loaded explicitly.
Placeholder values only:

```jsonc
{
  "S3AwsSettings": {
    "ServiceUrl": "https://storage.internal.invalid",
    "PublicUrl": "https://files.example.invalid",
    "BucketName": "<replace-with-bucket-name>",
    "AccessKey": "<supplied at deploy time — never committed>",
    "SecretKey": "<supplied at deploy time — never committed>",
    "PreSignedUrlExpirationInMinutes": 1440
  }
}
```

The root key must match `nameof(S3AwsSettings)` exactly.

The trap here is that this one section legitimately mixes both kinds of value.
`BucketName`, `PublicUrl`, `ServiceUrl` and the expiry are ordinary configuration
and **belong** in the committed file — reviewing an expiry change in a diff is a
feature. `AccessKey` and `SecretKey` are credentials and do not. That mixture is
exactly what makes filling in all six fields feel natural, and a committed secret
stays in history.

**How this solution supplies secrets is auth-and-security's** — follow the
mechanism already in place rather than introducing a second one here. In the
wiring below, environment variables are added after every JSON topic, so they
override whatever the files contain; that ordering is a property of the chain you
write, not a rule, so check it before relying on it.

The base topic file must exist even when every value is overlaid; the
environment-specific overlay beside it is optional.

## The three wiring lines

```csharp
// Configuration composition — the storage topic, then environment variables last
builder.Configuration
        // … existing topics, in load order …
        .AddJsonFiles(environmentName, "filestorage")
        .AddEnvironmentVariables();
```

```csharp
// Infrastructure composition root — appended to the same single fluent chain
services
    // … existing facades …
    .AddS3AwsFileStorage();
```

```csharp
// Application entry point — the step that is easiest to miss
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new S3FilePathConverter(builder.Configuration)));
```

The converter is constructed by hand rather than resolved, for the reason given
in its own section. It is registered exactly once, on the serializer options —
**not** in the service collection. This is the canonical statement of the three
lines; `references/usage-patterns.md` shows them at a call site and points back
here for the reasoning.

## Appendix — optional: `FileStorage.cs`

This file ships with the facade in every codebase that uses it, but nothing
consumes it — it is a holder for file metadata whose `[NotMapped] FullPath`
property wraps `Path` back into an `S3FilePath` for serialization. Include it if
you want the facade file set complete; skip it if you are adding S3 storage to a
new codebase, and reach for it only when you have an entity that needs to carry
uploaded-file metadata alongside its key.

```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Facades.FileStorage;

public class FileStorage
{
    public string? ContentType { get; }

    public string? ContentDisposition { get; }

    public long Length { get; }

    public string? Name { get; }

    public string? FileName { get; }

    public string? Path { get; set; }

    [NotMapped]
    public S3FilePath FullPath => new(Path);
}
```

`FullPath => new(Path)` always takes the default `IsSystem: true`, so this class
cannot express the external-URL branch — a `Path` holding a third-party URL would
be pre-signed as if it were a key in this bucket.

## Normalizations at a glance

Every place this file departs from the codebases it was drawn from, and why.

| Spot | As found | This file | Reason |
|---|---|---|---|
| Converter `Read` | `JsonSerializer.Deserialize<S3FilePath>(reader.GetString()!)` | `new S3FilePath(reader.GetString())` | The original re-parses a bucket key as a JSON document and throws on every real payload. |
| Converter `Write` | always pre-signs `OriginPath` | branches on `IsSystem`; null writes null | Without the branch an externally-hosted absolute URL is signed as if it were a key in this bucket. |
| `S3FilePath` XML docs | `<param>` tags on the type | `<param>` tags on the constructor | On a non-record struct, type-level `<param>` tags raise `CS1572` and break a warnings-as-errors build. |
| `GetPreSignedUrl` (service) | 3 parameters | 4th: `responseContentDisposition` | Attachment downloads need a `Content-Disposition` override; the read path does not. |
| `GetPreSignedUrl` (converter) | 3 parameters | unchanged, 3 parameters | A serializer has no request to take a disposition from; the parameter would be unreachable. |
| `DirectoryUploadAsync` | absent from the baseline | present, on interface and class | Whole-folder upload, in the same `bool`-returning contract as the rest. |
| `expires` defaulting | `if (expires == null) { expires = … }` | `expires ??= …` | One form across all three URL methods, so the file is not half-modernised. |
| File names | `S3AWSFileStorageService.cs`, `S3AWSSettings.cs` | `S3AwsFileStorageService.cs`, `S3AwsSettings.cs` | Lossless casing fix so file names match type names. |
| File name | `FileResponseConverter.cs` | unchanged | Deliberately **not** normalized: the file/class mismatch is a documented trap worth more than the consistency. |
| `Startup` using list | `using Microsoft.Extensions.Options;` retained | unchanged | Corpus text, kept verbatim. One variant omits it, but that variant differs in two ways at once, so its removal is untested here. |
| `Startup` registration | `AddScoped(typeof(…), typeof(…))` | unchanged | Majority form. Presented without comment. |
| Config hosts | real endpoints | `.invalid` hosts, bracketed secrets | `.invalid` cannot resolve, so a copied example cannot point at anything real. |
