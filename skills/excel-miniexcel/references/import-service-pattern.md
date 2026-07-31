# The import service pattern

No shared import extension exists to copy — unlike `ExcelExtension` and
`ZipExtension`, the import flow is written per module. **This file is the
canonical shape.** Follow it top to bottom and the service is correct without
looking at any existing project. The only parts copied verbatim are the calls
into `ZipExtension`.

Two upload shapes are covered. Pick one per import; do not build a service that
switches on the file type at runtime. They share everything after the parse step.

- **Direct** — a bare `.xlsx`. Rows only.
- **Packaged** — one workbook plus a media folder, one subfolder of media per row.

Ownership, so this file stays in its lane:

| Concern | Owner |
|---|---|
| What a row must satisfy — the rules themselves | `module-feature` |
| Storing extracted media, the temp root, directory upload | `file-storage` |
| Mapping the row type onto the entity | `automapper-mapping` |
| Entity configuration, transactions, query shaping | `ef-core-data-access` |
| Route templates, response envelope, permissions | `api-surface`, `auth-and-security` |
| Exception types and message text | `error-handling`, `message-keys` |
| Background job wiring | `background-worker` |
| The staged-rows listing endpoint | `list-query-pipeline` |

Symbols this pattern assumes already exist. Recreate or wire each from its owner,
not from here:

| Symbol | Owner |
|---|---|
| `EntityMedia`, `Entity.StorageFolderKey`, `fileStorageService` | `file-storage` |
| `IValidatorService` and its FluentValidation wiring | `module-feature` |
| `jobClient` | `background-worker` |
| `repositoryWrapper` | `ef-core-data-access` |
| `currentUser` | `auth-and-security` |

## File layout

```
Infrastructure/Modules/Imports/
  Datas/ImportEntityData.cs          row POCO + validators + Profile (colocated)
  Requests/ImportEntityRequest.cs    upload request + file gate
  Settings/ImportSettings.cs         caps and TTL, bound from configuration
  Services/ImportService.cs          the flow
Web/Controllers/Imports/
  ImportEntitiesController.cs        thin endpoints
```

---

## 1. The row type, its validator and its profile — one file

Plain properties, one per column, in column order. **No MiniExcel column
attributes** — the header text in the template you shipped is the contract, and
`startCell` is the only thing binding the POCO to the sheet.

```csharp
public class ImportEntityData
{
    public string? Code { get; set; }

    public string? Name { get; set; }

    /// <summary>Name of this row's media directory inside the .zip. Packaged imports only.</summary>
    public string? MediaFolder { get; set; }

    /// <summary>Populated by the service from the archive, not read from the sheet.</summary>
    public List<EntityMedia>? Media { get; set; }
}

public class ImportEntityDataValidator : AbstractValidator<ImportEntityData>
{
    public ImportEntityDataValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        // What the rules actually assert is module-feature's call. What belongs
        // here is the shape: one validator per row type, wrapped by a List<T>
        // validator, so the service validates the whole sheet in one call.
    }
}

public class ImportRangeEntityDataValidator : AbstractValidator<List<ImportEntityData>>
{
    public ImportRangeEntityDataValidator()
        => RuleForEach(x => x).SetValidator(new ImportEntityDataValidator());
}

public class ImportEntityDataMapping : Profile
{
    public ImportEntityDataMapping() => CreateMap<ImportEntityData, Entity>();
}
```

Validating the whole batch in one call is what lets a single request report every
bad row; a per-row loop reports only the first failure. The service invokes it
once, after the rows are read and before anything is mapped:

```csharp
await validatorService.ValidateAsync(rows, cancellationToken);
```

`IValidatorService` resolves `IValidator<T>` from a scope and throws on the first
error. **The service must carry no row rules of its own.**

## 2. The request and its file gate

The cheapest gate runs first — before anything is parsed or extracted.

```csharp
public class ImportEntityRequest
{
    public IFormFile? File { get; set; }
}

// Direct upload: the workbook whitelist, read off the zip helper so the list
// has exactly one owner.
public class ImportEntityValidator : AbstractValidator<ImportEntityRequest>
{
    public ImportEntityValidator()
    {
        RuleFor(x => x.File)
            .NotEmpty().WithMessage(/* message-keys */)
            .Must(file => ZipExtension.ExcelExtension.Contains(Path.GetExtension(file!.FileName)))
            .WithMessage(/* message-keys */);
    }
}

// Packaged upload: a .zip name proves nothing — probe the stream.
public class ImportEntityPackageValidator : AbstractValidator<ImportEntityRequest>
{
    public ImportEntityPackageValidator()
    {
        RuleFor(x => x.File)
            .NotEmpty().WithMessage(/* message-keys */)
            .Must(file => ZipFile.IsZipFile(file!.OpenReadStream(), true))
            .WithMessage(/* message-keys */);
    }
}
```

## 3. Settings — caps and TTL from configuration

```csharp
public class ImportSettings : IValidatableObject
{
    /// <summary>Maximum rows accepted in one import.</summary>
    public int MaxObject { get; set; }

    /// <summary>Maximum media files per row.</summary>
    public int MaxImage { get; set; }

    /// <summary>Maximum uncompressed size per media file, in KB.</summary>
    public int MaxSizeImage { get; set; }

    /// <summary>Seconds before unconfirmed staged rows are swept.</summary>
    public int TimeAutoClean { get; set; } = default!;

    // Required() -> common-extensions
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required();
}
```

Never hard-code a cap in the service. Every limit a guard checks comes from here.

## 4. Uniqueness checks — set-based, filters ignored

One round trip for the whole batch, not one per row.

```csharp
// within the batch
if (rows.DistinctBy(x => x.Code, StringComparer.OrdinalIgnoreCase).Count() != rows.Count)
{
    throw new BadRequestException(/* message-keys */);
}

// against what is already stored, staged rows included
if (await repositoryWrapper.Repository<Entity>()
        .Find()
        .IgnoreQueryFilters()
        .AnyAsync(x => rows.Select(r => r.Code).Contains(x.Code), cancellationToken))
{
    throw new BadRequestException(/* message-keys */);
}
```

`IgnoreQueryFilters` is here because of the staging filter in §7 — a value sitting
in someone's unconfirmed batch is still taken. If the project's filter carries
other predicates as well, opting out lifts those too; check the entity's
configuration before adding it.

## 5. Direct flow — one workbook

Lines marked `// staging only` belong to §7. Delete them for a flow that commits
directly, and the rest still stands.

```csharp
public interface IImportEntityService : IScopedService
{
    Task ImportAsync(ImportEntityRequest request, CancellationToken cancellationToken = default);
}

public class ImportEntityService(
    IMapper mapper,
    ICurrentUser currentUser,
    ImportSettings settings,
    IValidatorService validatorService,
    IRepositoryWrapper repositoryWrapper)
    : IImportEntityService
{
    private const string StartCell = "B3";

    public async Task ImportAsync(ImportEntityRequest request, CancellationToken cancellationToken = default)
    {
        List<ImportEntityData> rows =
        [
            .. request.File!
                .OpenReadStream()
                .Query<ImportEntityData>(startCell: StartCell)
                .Where(ZipExtension.BuildEmptyFilter<ImportEntityData>())
        ];

        if (rows.Count > settings.MaxObject)
        {
            throw new BadRequestException(/* message-keys */);
        }

        await validatorService.ValidateAsync(rows, cancellationToken);
        await EnsureUniqueAsync(rows, cancellationToken);

        List<Entity> entities = mapper.Map<List<Entity>>(rows);
        entities.ForEach(entity => entity.ImportSessionId = currentUser.GetUserId());   // staging only

        await repositoryWrapper.BeginTransactionAsync(cancellationToken);
        try
        {
            await repositoryWrapper.Repository<Entity>().AddRangeAsync(entities, cancellationToken);
            await repositoryWrapper.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception)
        {
            await repositoryWrapper.RollbackTransactionAsync(cancellationToken);
            /* log — error-handling */
            throw new InternalServerException(/* message-keys */);
        }

        ScheduleAutoClean();                                                            // staging only
    }
}
```

`StartCell` is a property of the template you shipped — the header or instruction
band above the data decides it. Keep it a `const` beside the flow, never guess it
at the call site.

## 6. Packaged flow — workbook plus media

Structure first, rows second, files third, database last.

```csharp
private const string StartCell = "A2";
private const string MediaFolderName = "Media";
private const long MarkLargeFileSize = 4194304;

// Local temporary file area. In a project with a storage facade this comes from
// there; otherwise a Files/Temp path under the content root. file-storage owns it.
private readonly string tempRootPath;

public async Task ImportPackageAsync(ImportEntityRequest request, CancellationToken cancellationToken = default)
{
    ZipFile zip = ZipFile.Read(request.File!.OpenReadStream());
    ICollection<ZipEntry> entries = zip.Entries;

    // Structural gate — everything the flow below assumes must be true here,
    // and none of it costs a cell read.
    if (entries.GetDirectories(MediaFolderName).Length != 1 ||
        entries.Count(ZipExtension.IsExcelFile) != 1 ||
        entries.Any(entry => entry.UncompressedSize > settings.MaxSizeImage * 1024))
    {
        throw new BadRequestException(/* message-keys */);
    }

    List<ImportEntityData> rows = entries
        .First(ZipExtension.IsExcelFile)
        .EntryExcelCastTo<ImportEntityData>(StartCell);

    if (rows.Count > settings.MaxObject)
    {
        throw new BadRequestException(/* message-keys */);
    }

    await validatorService.ValidateAsync(rows, cancellationToken);
    await EnsureUniqueAsync(rows, cancellationToken);

    string mediaDirectoryEntry = entries.GetDirectories(MediaFolderName)[0].FileName;
    List<ZipEntry> mediaEntries =
    [
        .. entries.Where(x => x.FileName.StartsWith(mediaDirectoryEntry) &&
                              !string.Equals(x.FileName, mediaDirectoryEntry))
    ];

    EnsureRowMediaValid(rows.Select(row => row.MediaFolder!), mediaEntries);

    // One temp directory per request. The GUID keeps two concurrent imports
    // from colliding.
    string tempDirectoryName = Guid.NewGuid().ToString();
    string tempDirectoryPath = Path.Combine(tempRootPath, tempDirectoryName);

    try
    {
        HandleMedia(rows, mediaEntries, tempDirectoryName);

        List<Entity> entities = mapper.Map<List<Entity>>(rows);
        entities.ForEach(entity => entity.ImportSessionId = currentUser.GetUserId());   // staging only

        await repositoryWrapper.BeginTransactionAsync(cancellationToken);
        try
        {
            // Directory upload to object storage -> file-storage.
            await fileStorageService.DirectoryUploadAsync(
                tempDirectoryPath, Entity.StorageFolderKey, cancellationToken);

            await repositoryWrapper.Repository<Entity>().AddRangeAsync(entities, cancellationToken);
            await repositoryWrapper.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception)
        {
            await repositoryWrapper.RollbackTransactionAsync(cancellationToken);
            // Compensate anything already written outside the transaction
            // (uploaded objects) -> file-storage. Logging -> error-handling.
            throw new InternalServerException(/* message-keys */);
        }

        ScheduleAutoClean();                                                            // staging only
    }
    finally
    {
        ClearFolder(tempDirectoryPath);
    }
}

/// <summary>
/// Structural check on the archive, one row folder at a time, every limit from
/// settings. This is not row-value validation — that is module-feature's, and it
/// runs through the FluentValidation call in §1.
/// </summary>
private void EnsureRowMediaValid(IEnumerable<string> mediaFolders, IEnumerable<ZipEntry> mediaEntries)
{
    foreach (string folder in mediaFolders)
    {
        // Depth 2: <media folder>/<row folder>
        ZipEntry[] folderEntries = [.. mediaEntries.Where(entry => entry.IsValidDirectory(folder, 2))];
        if (folderEntries.Length != 1)
        {
            throw new BadRequestException(/* message-keys */);
        }

        string prefix = folderEntries[0].FileName;

        // Nothing but images may sit in a row's folder.
        if (mediaEntries.Any(entry => entry.FileName.StartsWith(prefix) &&
                                      !entry.FileName.EndsWith('/') &&
                                      !entry.IsImages()))
        {
            throw new BadRequestException(/* message-keys */);
        }

        int imageCount = mediaEntries.Count(entry => entry.FileName.StartsWith(prefix) && entry.IsImages());
        if (imageCount <= 0 || imageCount > settings.MaxImage)
        {
            throw new BadRequestException(/* message-keys */);
        }
    }
}

private void HandleMedia(List<ImportEntityData> rows, List<ZipEntry> mediaEntries, string tempDirectoryName)
{
    foreach (ImportEntityData row in rows)
    {
        // Depth 3: <media folder>/<row folder>/<file>
        ZipEntry[] rowEntries = mediaEntries.GetEntriesByParent(row.MediaFolder!, FileAttributes.Archive, 3);

        row.Media =
        [
            .. rowEntries.Select(entry =>
            {
                string localPath = entry.SaveImage(tempRootPath, tempDirectoryName, MarkLargeFileSize);
                string fileName = localPath.Split(Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)[^1];

                return new EntityMedia
                {
                    Name = fileName,
                    ContentType = MimeTypesMap.GetMimeType(fileName),
                    LocalPath = localPath,
                };
            })
        ];
    }
}

private static void ClearFolder(string folderPath)
{
    if (Directory.Exists(folderPath))
    {
        Directory.Delete(folderPath, true);
    }
}
```

**Cleanup is in `finally`, not after the commit.** The transaction can roll back
after the files are already on disk, and the exception that made cleanup
necessary is exactly the one that would skip it.

Files marked `large` by `SaveImage` are the seam for an optional resize pass
before upload; that pass is an image-processing concern, not an Excel one.

## 7. Staging and confirm — an addition, not a requirement

Grounded in one corpus solution, across both of its flows. Reach for it when the
user must review the parsed result before it counts as live data. A flow that
commits directly (§5, §6 without the marked lines) is equally canonical.

The entity carries a nullable marker:

```csharp
public interface IImportable
{
    /// <summary>Id of the user whose session staged this row. Null once confirmed.</summary>
    public Guid? ImportSessionId { get; set; }
}
```

Live queries never see staged rows, because the marker is in the global query
filter — entity configuration itself is `ef-core-data-access`:

```csharp
entityTypeBuilder.HasQueryFilter(x => x.ImportSessionId == null);
```

Every read of staged rows must therefore opt out of the filter — **and scope
itself to the current session, which is what stops one user reaching another
user's rows**:

```csharp
private IQueryable<Entity> FindStaged(Expression<Func<Entity, bool>>? predicate = default)
{
    IQueryable<Entity> query = repositoryWrapper.Repository<Entity>()
        .Find(x => x.ImportSessionId == currentUser.GetUserId())
        .IgnoreQueryFilters();

    return predicate is null ? query : query.Where(predicate);
}
```

Confirm nulls the marker; delete removes the rows; a scheduled sweep collects
whatever was abandoned. Both endpoints read through `FindStaged`, and the count
check is what turns "an id you do not own" into a rejection:

```csharp
public async Task<MultipleIdentiferResponse> ConfirmRangeAsync(ConfirmRangeImportRequest<Entity> request)
{
    ICollection<Entity> staged = await FindStaged(x => request.Ids!.Contains(x.Id)).ToListAsync();
    if (staged.Count != request.Ids!.Count)
    {
        throw new BadRequestException(/* message-keys */);
    }

    await repositoryWrapper.Repository<Entity>().UpdateRangeAsync(
        staged.Select(entity =>
        {
            entity.ImportSessionId = null;
            return entity;
        }));

    return new(request.Ids);
}

// DeleteRangeAsync mirrors this exactly, with DeleteRangeAsync(staged) in place
// of the null-out.

public void ScheduleAutoClean()
    => jobClient.Schedule(
        () => AutoCleanAsync(currentUser.GetUserId()),
        TimeSpan.FromSeconds(settings.TimeAutoClean));

public async Task AutoCleanAsync(Guid importSessionId)
{
    List<Entity> abandoned = await repositoryWrapper.Repository<Entity>()
        .Find(x => x.ImportSessionId == importSessionId)
        .IgnoreQueryFilters()
        .ToListAsync();

    await repositoryWrapper.Repository<Entity>().DeleteRangeAsync(abandoned);
}
```

`ScheduleAutoClean()` is called after the transaction block returns, not inside
it: the sweep deletes rows, so whether it should exist at all is decided by the
commit. `references/anti-patterns.md` carries the reasoning.

Background job wiring is `background-worker`.

## 8. Endpoints — thin

Routes, envelope, permissions and response shaping are `api-surface` and
`auth-and-security`. What belongs here is the **upload gate**.

```csharp
public class ImportEntitiesController(IImportEntityService importService) : BaseController
{
    private const long MaxFileSize = 1 * 1024 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    // permission attribute — auth-and-security
    public async Task<ActionResult<SuccessResultWrapper<object>>> ImportAsync([FromForm] ImportEntityRequest request)
    {
        await importService.ImportAsync(request);
        return OkWrapper<object>(null, /* message-keys */);
    }

    [HttpPost("ConfirmMany")]
    public async Task<ActionResult<SuccessResultWrapper<MultipleIdentiferResponse>>> ConfirmManyAsync(
        [FromBody] ConfirmRangeImportRequest<Entity> request)
        => OkWrapper(await importService.ConfirmRangeAsync(request), /* message-keys */);

    [HttpPost("DeleteMany")]
    public async Task<ActionResult<SuccessResultWrapper<MultipleIdentiferResponse>>> DeleteManyAsync(
        [FromBody] DeleteRangeImportRequest<Entity> request)
        => OkWrapper(await importService.DeleteRangeAsync(request), /* message-keys */);

    // GET listing of staged rows reads through FindStaged — filtering, sorting
    // and pagination are list-query-pipeline.
}
```

**One named constant, both attributes.** An import endpoint has to accept far
more than the default form limit, but the declared limit and the multipart limit
must not drift apart, and a cap is the only thing between a multipart body and
the disk. An unbounded gate also appears in the corpus; the bounded pair is what
this pattern ships. Raise `MaxFileSize` if a real workbook needs it — do not
remove the ceiling.

## Checklist

- [ ] `startCell` is the first data cell and lives as a const on the service.
- [ ] Every sheet read passes through `BuildEmptyFilter<T>()` — `Query<T>` calls
      it explicitly, `EntryExcelCastTo<T>` applies it for you.
- [ ] Structural guards run before any row is read, and every cap comes from
      settings.
- [ ] One validator call over `List<TRow>`; no row rules inside the service.
- [ ] Uniqueness checked set-based, not once per row.
- [ ] Insert is transactional, with rollback on the catch path.
- [ ] Temp directory deleted in `finally`.
- [ ] Staging chosen deliberately, and if chosen: filter, session-scoped
      `FindStaged`, `IgnoreQueryFilters` on staged reads and uniqueness checks,
      confirm, delete, scheduled clean.
- [ ] Endpoint size gate bounded by one shared constant.
