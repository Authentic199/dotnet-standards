# Four more anti-patterns

Read this file when you are writing an update flow, an external-URL ingest, or a bulk
delete. It continues the numbering of the Anti-patterns section in `SKILL.md`: entries
1–5 are there, 6–9 are here.

What they share is that the damage is not what fails. Three of the four report success;
the fourth reports a failure that names the upload, not the object it destroyed.

## 6. Deleting the old object before the new upload succeeds

```csharp
// BAD — the old object is destroyed before anything replaces it
string? newKey = null;

await repositoryWrapper.BeginTransactionAsync(cancellationToken);
try
{
    if (request.Image?.Length > 0)
    {
        if (!string.IsNullOrWhiteSpace(entity.Image))
        {
            await fileStorage.DeleteAsync(entity.Image, cancellationToken);   // gone here
        }

        newKey = await fileStorage.UploadAsync(EntityFolder, request.Image);
        entity.Image = newKey;
    }

    await repositoryWrapper.Repository<Entity>().UpdateAsync(entity, cancellationToken);
    await repositoryWrapper.CommitTransactionAsync(cancellationToken);
}
catch (Exception ex)
{
    if (!string.IsNullOrWhiteSpace(newKey))
    {
        await fileStorage.DeleteAsync(newKey, cancellationToken);             // the new key only
    }

    await repositoryWrapper.RollbackTransactionAsync(cancellationToken);
    throw new InternalServerException(/* message key */, ex);
}
```

GOOD is the ordering in the Pattern **Update: never delete the old object before the new
one is up** — hold the old key in a local above the `try`, upload, commit, and delete last.
Do not retype it from memory; the listing is in `SKILL.md` and, with both guards, in
`references/usage-patterns.md` §5.

The `catch` compensates the *new* key, which is the only key it still can compensate. The
old object no longer exists and nothing in this method can bring it back. Two ordinary
failures both land there: the extension overload throws when the upload fails, and a failed
commit rolls the row back to a key whose object was deleted a few lines earlier.

So the request reports a failure — of the upload, or of the commit — and the caller retries
or gives up, while the record nobody meant to touch is already broken. Inverted, the worst
outcome is one orphaned object: that costs storage, and a missing object costs a record.

## 7. Taking a checksum over a text decoding of the bytes

```csharp
// BAD — the digest is of a text projection, not of the file
string checksum = CreateHash(Encoding.UTF8.GetString(memory.ToArray()));

// …and the helper re-encodes, under a different encoding, before hashing
byte[] inputBytes = Encoding.ASCII.GetBytes(input);
byte[] hashBytes = hashAlgorithm.ComputeHash(inputBytes);
```

```csharp
// GOOD — hash the stream; the bytes never become text
string checksum = await ComputeChecksumAsync(stream, cancellationToken);
stream.Seek(0, SeekOrigin.Begin);
```

`references/usage-patterns.md` §7 already states the rule in one line — *use any stable
hash over the raw bytes*. What this entry adds is the shape that hides the violation: the
decode and the re-encode sit in **two different files**, under **two different encodings**,
and neither half looks wrong on its own. The call site reads as "hash a string"; the helper
reads as "hash some bytes".

The consequence is not a crash. This checksum's whole job is to be compared against a
stored one to decide whether to re-upload at all, so two files that collapse onto the same
text projection produce the same digest, the comparison reads "unchanged", a genuine update
is skipped, and the flow logs a successful no-op. Nobody notices until a stored object has
quietly stopped tracking its source.

`ComputeChecksumAsync` is a placeholder: **no file this skill ships provides it.** Do not
attribute one to it. What this skill does require is the `Seek` back to 0 afterwards —
hashing leaves the position at the end, and the upload would send zero bytes.

## 8. Leaving the streams around an ingest undisposed

```csharp
// BAD — the using covers the handle, not the stream the call site opened
using MediaDownloadInfo info = await mediaManager.DownloadAsync(sourceUrl, cancellationToken: cancellationToken);
FileStream file = info.OpenFileStream();

MemoryStream memory = new();
await file.CopyToAsync(memory, cancellationToken);
```

```csharp
// GOOD — dispose what you opened, and do not open the buffer at all
using MediaDownloadInfo info = await mediaManager.DownloadAsync(sourceUrl, cancellationToken: cancellationToken);
await using FileStream stream = info.OpenFileStream();

string key = await fileStorage.UploadAsync(EntityFolder, info.FileName, stream);
```

The `using` in the BAD block is present, is correct, and covers neither of the two streams
below it: `OpenFileStream()` returns a *new* `FileStream` and leaves the handle's own stream
property null, so the handle's `Dispose` closes nothing the call site opened. The three
facts behind that are already written out — read `references/media-downloads.md`, **"The
dispose contract"**, rather than reasoning it out from the listing.

The two halves are not equally bad. The `FileStream` is the load-bearing half: an OS file
handle that is not released at the end of the block that opened it, and that nothing later
in the flow releases either. It is opened with `FileOptions.DeleteOnClose`, so the flag
meant to guarantee the temp file's cleanup contributes nothing on this path — the info's own
`Dispose` is left doing that work alone. The `MemoryStream` is the lesser half: no handle,
but the entire payload sits in memory with nothing releasing it early.

`await using` on both is the minimum. Better, do not open the second one: the upload
extension takes a `Stream` and a file name, so the file stream can go straight to it and the
payload never needs a second copy. Buffer only when something between the download and the
upload genuinely has to re-read the bytes — and then `await using` that too.

The Pattern **Ingesting a file from an external URL** in `SKILL.md` says the `using` is
load-bearing. This is what it looks like when the `using` is there and still holds nothing.

## 9. Discarding the `Task` returned by a delete

```csharp
// BAD — nothing awaits it, so nothing joins the cleanup to the request
finally
{
    if (deleteKeys.Count > 0)
    {
        _ = fileStorage.DeleteManyAsync(deleteKeys, cancellationToken);
    }
}
```

```csharp
// GOOD — one keyword, and nothing else changes
finally
{
    if (deleteKeys.Count > 0)
    {
        await fileStorage.DeleteManyAsync(deleteKeys, cancellationToken);
    }
}
```

The discard buys nothing, and that is the whole argument. `DeleteManyAsync` issues one
batched request rather than N and no-ops on an empty list, so there is no latency worth
dodging — and it catches and logs its own failures rather than throwing, so awaiting it
cannot break the request either. There is no risk being avoided here, only an `await` being
skipped.

What is actually lost is the join. The method returns before the deletion has finished, and
the failure the service logs is never tied to the request that caused it: the caller sees
the same success either way, and no test can distinguish a cleanup that ran from one that
did not. The awaited form is the house form — sibling cleanup flows call this same method
and await it; the discard is the outlier.

**Leave the `Count > 0` guard alone.** It is not part of the defect and it is not redundant:
a failure path that empties the list before the `finally` runs uses that guard to skip the
call entirely, where calling in with an empty list would log a spurious "nothing to delete"
on every rollback. Change the one keyword and nothing else.
