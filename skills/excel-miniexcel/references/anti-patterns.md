## More anti-patterns

Five defects the six entries in `SKILL.md` do not cover. They go wrong at the
endpoint's edge, at the commit boundary, in cleanup, and in the names files are
saved under — later than the mistakes SKILL.md catches, which is why they are
easy to ship. Read this file when writing an upload endpoint or an import
service. Each entry is a shape, not an incident.

### Leaving an import upload without a size ceiling

```csharp
// BAD — both limits removed, so nobody on the team chose the ceiling
[HttpPost]
[DisableRequestSizeLimit]
[RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue)]
[HasPermission(/* the module's Import permission */)]
public async Task<ActionResult<SuccessResultWrapper<object>>> ImportAsync(
    [FromForm] ImportEntityRequest request)
```

The bounded form is the endpoint under **Import a bare `.xlsx` upload** in
`SKILL.md`: one `MaxFileSize` constant used in both `[RequestSizeLimit]` and
`[RequestFormLimits]`.

**Why:** this pair is usually reached for after a legitimate workbook was
rejected by the default form limit, and it does make the rejection stop. What it
also does is leave the endpoint with no ceiling anyone chose. The endpoint is
authenticated and permission-gated, so this is not an open door — but the largest
body a holder of the Import permission can push into a parse-and-extract path is
now whatever the host in front happens to allow, and nothing in the file records
a size the team considered acceptable. Raise the constant to what the largest
real workbook needs; do not delete the constant. Deriving both attributes from
one constant is also what keeps them in step: with two different literals they
drift by hand. Permission attributes themselves are **auth-and-security**.

### Arming the cleanup sweep before the transaction commits

```csharp
// BAD — the schedule is placed where its correctness depends on an outcome
// that has not happened yet
await repositoryWrapper.BeginTransactionAsync(cancellationToken);
try
{
    await repositoryWrapper.Repository<Entity>().AddRangeAsync(entities, cancellationToken);
    ScheduleAutoClean();
    await repositoryWrapper.CommitTransactionAsync(cancellationToken);
}
catch (Exception)
{
    await repositoryWrapper.RollbackTransactionAsync(cancellationToken);
    throw new InternalServerException(/* message-keys */);
}
```

```csharp
// GOOD — schedule once the commit has returned, so the job exists if and only
// if there are staged rows for it to sweep
await repositoryWrapper.BeginTransactionAsync(cancellationToken);
try
{
    await repositoryWrapper.Repository<Entity>().AddRangeAsync(entities, cancellationToken);
    await repositoryWrapper.CommitTransactionAsync(cancellationToken);
}
catch (Exception)
{
    await repositoryWrapper.RollbackTransactionAsync(cancellationToken);
    throw new InternalServerException(/* message-keys */);
}

ScheduleAutoClean();
```

**Why:** the scheduled job deletes rows, so whether it should exist at all is
decided by the commit — and in the BAD form that decision is taken before the
commit is attempted. The answer then turns on how the job store behaves with
respect to an open transaction, which is a property of a component the code never
names: neither a reader of this file nor the next person to change either half
can settle it by reading. Moving the call below the transaction block removes the
question instead of answering it. Nothing about the job store has to be known to
see that the ordering is right.

The stakes are set by the sweep's scope. Its predicate is the import-session
marker — the current user id, not an identifier for this run
(`references/import-service-pattern.md` §7) — so on the flow this skill
prescribes, whenever that job fires it acts on every row that user has staged,
including rows staged by a later import that has not yet been confirmed. A side
effect that wide is the last thing to arm on an outcome that has not been
established. The marker is the staging design and stays as it is; what changes
here is the placement, not the predicate.

The asymmetry is what makes the moved form the safe one. If scheduling is skipped
because the commit failed, rows stay staged and the confirm and delete endpoints
still reach them — a visible leftover rather than a silent deletion. Transaction
boundaries themselves are **ef-core-data-access**.

### Passing an `async` lambda to `List<T>.ForEach`

```csharp
// BAD — async void: nothing awaits the disposals, and a failure in one is
// unobservable
finally
{
    mediaRequests.ForEach(async x => await x.Content.DisposeAsync());
}
```

```csharp
// GOOD — a loop the method actually awaits
finally
{
    foreach (MediaRequest request in mediaRequests)
    {
        await request.Content.DisposeAsync();
    }
}
```

**Why:** `List<T>.ForEach` takes an `Action<T>`, and an `async` lambda supplied
where an `Action<T>` is expected binds as `async void`. Two consequences follow
from the language, not from any library: each lambda returns to `ForEach` at its
first incomplete `await`, so `ForEach` can return — and the `finally` complete —
with disposals still outstanding; and an exception raised inside one of them has
no caller to return to, so the surrounding `try`/`catch` cannot catch it however
carefully it is written. The failure is silent in both directions.

In an import this lands badly twice over. The handles being disposed are open
over files in the run's temp directory and the enclosing flow deletes that
directory in a `finally` of its own, so the deletion is ordered against nothing.
And this `finally` is itself the cleanup path, so the failure the block exists to
tidy up after is exactly the one whose own cleanup errors vanish. The rule
outlives this case: a lambda containing `await` needs a target that returns a
`Task`, and `Action<T>` is not one.

### Leaving a `Console.WriteLine` timing probe in the import path

```csharp
// BAD — a stopwatch reading on stdout, in a file that already has a logger
Stopwatch sw = Stopwatch.StartNew();
await ResizeMarkedImagesAsync(tempDirectoryPath);
Console.WriteLine("[resize] " + sw.ElapsedMilliseconds);
```

```csharp
// GOOD — the probe goes out with the question that prompted it
await ResizeMarkedImagesAsync(tempDirectoryPath);
```

**Why:** the probe answers a real question — how long the post-processing pass
takes on an archive of realistic size — and then ships with the branch that asked
it. The tell is that the same file routes its genuine failures through the
project's logging facade in two other places: this is not a project without a
logger, it is a number that went around the one it has. `Console.WriteLine`
carries no level, nothing tying it to the request that produced it, and no
structure for whatever collects the output; it cannot be quietened by
configuration, and on a bulk path it fires once per import. Delete the probe when
the question is answered, or promote it into a real measurement through the
facade. Where a diagnostic genuinely earns a permanent place in the source,
**list-query-pipeline** settles on `Debug.WriteLine` for the same reason; logger
wiring and levels are **error-handling**.

### Letting a clock read be the only distinguishing part of a saved file name

```csharp
// BAD — every row's media lands in one directory, so two identical leaf names
// are told apart only by the ticks value inside the name
foreach (ImportEntityData row in rows)
{
    ZipEntry[] rowEntries = mediaEntries.GetEntriesByParent(
        row.MediaFolder!, FileAttributes.Archive, 3);

    row.Media = [.. rowEntries.Select(entry => new EntityMedia
    {
        LocalPath = entry.SaveImage(tempRootPath, tempDirectoryName, MarkLargeFileSize),
    })];
}
```

```csharp
// GOOD — each row gets its own directory segment, so two rows cannot produce
// the same path at all
for (int i = 0; i < rows.Count; i++)
{
    string rowDirectoryName = string.Join(
        Path.AltDirectorySeparatorChar, tempDirectoryName, i.ToString());

    ZipEntry[] rowEntries = mediaEntries.GetEntriesByParent(
        rows[i].MediaFolder!, FileAttributes.Archive, 3);

    rows[i].Media = [.. rowEntries.Select(entry => new EntityMedia
    {
        LocalPath = entry.SaveImage(tempRootPath, rowDirectoryName, MarkLargeFileSize),
    })];
}
```

**Why:** the run's temp directory already carries a `Guid`, so two concurrent
imports cannot reach the same path — that half is solved and this entry does not
reopen it. The window left is inside a single run. An archive holds one media
folder per row and every file from every row is flattened into the one run
directory, so two rows each holding a file with the same leaf name differ only in
the prefix — and that prefix is a wall-clock read taken inside a loop. It is the
only distinguishing component the code offers.

**This is not a reason to change the formatter.** The ticks prefix in
`references/zip-extension.cs` does exactly what that file says it does: it keeps
leaf names distinct once entries from different archive folders land side by side
in one flat directory. Change the *side by side* instead. `SaveImage` already
takes the directory as a parameter, so give each row its own segment and the
collision has nowhere to occur — `zip-extension.cs` needs no edit for this. Use a
positional segment rather than the row's own media-folder name, which comes off
the sheet and is caller-supplied text. Note the consequence before adopting it:
the saved paths gain a level, so a directory upload produces keys one segment
deeper. That shape is **file-storage**'s, and it is the thing to settle there
first.

> **Documentation-derived** — not corpus-verified. `SaveImage` opens the target
> with `FileMode.Create`, which replaces an existing file of that name rather
> than failing. A collision here therefore raises nothing: one row's media
> quietly takes another's place and the import reports success.
