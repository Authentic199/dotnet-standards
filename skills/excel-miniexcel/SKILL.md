---
name: excel-miniexcel
description: >-
  This skill should be used when reading or writing Excel with MiniExcel in a
  .NET service: exporting rows via ExcelExtension.Export or ExportByTemplate,
  importing an uploaded .xlsx through Query<T> with a startCell, filtering
  blank rows, unpacking a zip of workbook plus images, serving or replacing an
  import template file, or staging imported rows for confirm. Not for: row
  validation rules — module-feature; S3 upload, serving — file-storage;
  search, pagination endpoints — list-query-pipeline; PathExtension —
  common-extensions; upload routes, DTOs — api-surface; permission attributes,
  ApiKey — auth-and-security; profiles — automapper-mapping; persistence,
  transactions — ef-core-data-access; exceptions — error-handling; message
  text — message-keys.
---

## Overview

Two directions, one library. **Export** hands the HTTP layer a rewound `Stream`.
**Import** parses an uploaded workbook — bare, or zipped alongside a media
folder — into row POCOs, validates them, and commits once.

Four artifacts carry all of it, and they are **accumulated project wisdom, not
framework code**. A new solution frequently has none of them.

| Concern | Artifact | Lives in | Full implementation |
|---|---|---|---|
| Export rows to `.xlsx` | `ExcelExtension` | `Infrastructure/Facades/Common/Extensions/` | `references/excel-extension.cs` |
| Unpack a zip of workbook plus media | `ZipExtension` | `Infrastructure/Facades/Common/Extensions/` | `references/zip-extension.cs` |
| Serve and replace a blank template | `ImportTemplateExtension` | `Infrastructure/Facades/Common/Extensions/` | `references/import-template-extension.cs` |
| Read an upload into entities | an import service — no extension exists | `Infrastructure/Modules/Imports/Services/` | `references/import-service-pattern.md` |

`ExcelExtension` is byte-identical in all six reference solutions and has **zero
call sites in any of them** — it ships ahead of its first use. Treat a helper's
absence as a gap to fill, not as a sign the project does things differently.

**When a helper is missing, recreate it verbatim from this skill's
`references/`.** Never inline a bespoke copy at a call site, and never copy from
another project's path — the reference file is the source. Open a reference
*before* writing, not after: these files are meant to be reproduced whole, and
reconstructing them from the summaries below loses the parts that matter (the
empty-filter expression build, the level-based zip entry matching, the corrected
template-name call). Answer day-to-day questions from this file; open a
reference when recreating or writing the thing it covers.

Package set in the corpus: MiniExcel 1.31.2 (one solution at 1.30.2); zip work
uses DotNetZip (`Ionic.Zip`) 1.16.0 and `HeyRed.Mime` for MIME lookup.

**Two different template folders — do not merge them.** `Files/ExcelTemplates/`
holds *export* layouts consumed by `ExportByTemplate`. `Files/ImportTemplates/`
holds the *import* workbooks handed to users for filling in. Only the second
exists anywhere in the corpus; the first is created with your first export
template.

## Core Principles

### 1. The Excel helpers are house code you recreate, not call-site code

`ExcelExtension` is 23 lines and identical in six independent solutions — the
shape of a helper that gets carried forward, not reinvented. When a project
lacks it, add the file from `references/excel-extension.cs` and call it; do not
open a `MemoryStream` in a service.

**Why:** the helper's whole value is the one line a call site forgets, and a
bespoke seventh variant is a regression with no upside.

### 2. `Export` returns a stream already rewound to position 0

Every corpus copy does `SaveAs`, then `Seek(0, SeekOrigin.Begin)`, then returns.
The return type is `Stream`, not `byte[]` and not `MemoryStream`.

**Why:** the position after a write is at the end of the data. Rewinding inside
the helper means the rewind is not something a call site can forget, and
returning `Stream` keeps the endpoint free to hand it straight to a file result
without knowing the backing type.

This rule is about what an export helper hands back to a caller, not a blanket
seek-before-you-read rule: the read path inside `EntryExcelCastTo` queries its
`MemoryStream` straight after `Extract` with no seek, and
`references/zip-extension.cs` reproduces that unchanged.

### 3. Every read is anchored to a start cell and filtered for blank rows

Both corpus import flows do the same two things: pass an explicit start cell —
`Query<T>(startCell: "B3")` on a direct upload, `EntryExcelCastTo<T>("A2")` from
a zip entry — and then `.Where(BuildEmptyFilter<T>())`.

**Why:** house import workbooks carry a header or instruction band above the
data, so row 1 is never the first record; and a workbook hands back rows that
are formatted but carry no values. Without the filter those arrive as
all-default entities and get inserted. `BuildEmptyFilter<T>` compiles one
expression per type — non-empty on *any* property keeps the row.

`BuildEmptyFilter<T>` lives on the zip helper because that is where it was first
needed. A direct import calls it from there rather than duplicating it; the
cross-reference is deliberate, not an oversight to tidy up.

### 4. The upload is gated three times, cheapest gate first

1. **Request validator** on the `IFormFile` — extension whitelist for a bare
   workbook (`ZipExtension.ExcelExtension.Contains(...)`), `ZipFile.IsZipFile`
   for a zip.
2. **Structural checks** before parsing — exactly one workbook entry, exactly
   one media directory, entry counts and per-entry size against a settings
   object bound from configuration.
3. **Row validation** — after the rows exist, never before.

`ZipExtension.ExcelExtension` is a `string[]` of allowed workbook extensions
living on the zip helper — not the export helper class of the same name; the
collision is house canon, so read it as written rather than repointing it at the
export class.

**Why:** parsing is the expensive step and extraction is the dangerous one. A
junk file should be rejected by an extension check, and a malformed archive by a
count check, before a single cell is read.

### 5. An import commits once, cleans up in `finally`, and can be staged for confirm

The insert is wrapped in `BeginTransactionAsync` / `CommitTransactionAsync` with
`RollbackTransactionAsync` in the catch, and any temp media directory is deleted
in `finally` so a rollback does not leave files behind. Where the user needs a
review step, rows are inserted carrying an **import-session marker** (the current
user id); a confirm endpoint nulls it, a delete endpoint removes the rows, and a
scheduled job sweeps whatever is still unconfirmed after a configured TTL.

**Why:** an import is bulk and unreviewable at upload time — a partial commit
leaves the user with no way to tell what landed. The marker turns "undo an
import" into "delete rows still carrying my marker".

Staging is grounded in one corpus solution, across both of its flows; the other
commits directly. It is an addition, not a requirement — reach for it when the
user must eyeball the parsed result before it counts.

## Patterns

### Export a list to a downloadable stream

```csharp
// call site — the endpoint owns the file name and content type (api-surface)
Stream stream = ExcelExtension.Export(rows);
return File(stream, ExcelContentType, $"{fileName}.xlsx");
```

The rows passed in are a flat response-shaped type: one property per column, in
column order. Header text comes from the property names unless a template is
used, so shape the export by projecting to a dedicated row type first — do not
reshape the sheet inside the extension.

**Read `references/excel-extension.cs`** before adding this file to a project
that does not have it — it carries both methods verbatim.

### Export through a stored template

`ExportByTemplate(data, templateName)` calls MiniExcel's `SaveAsByTemplate`
against `PathExtension.Combine(AppDomain.CurrentDomain.BaseDirectory,
$"Files/ExcelTemplates/{templateName}.xlsx")`, then rewinds like `Export`.

Use this when the output must match a designed workbook — merged cells, a
branded header band, fixed column widths — rather than a plain grid. The
template `.xlsx` ships with the application under `Files/ExcelTemplates/`, must
be copied to output, and is addressed by bare name: no extension, no path.

`PathExtension.Combine` is owned by **common-extensions** — recreate it from
there if the project lacks it.

### Import a bare `.xlsx` upload

The row POCO is plain properties, one per column — no MiniExcel column
attributes appear anywhere in the corpus — and the per-row validator, the batch
validator and the mapping profile sit in the **same file** as the POCO:

```csharp
public class ImportRangeEntityDataValidator : AbstractValidator<List<ImportEntityData>>
{
    public ImportRangeEntityDataValidator()
        => RuleForEach(x => x).SetValidator(new ImportEntityDataValidator());
}
```

Validating the **whole batch** through `AbstractValidator<List<T>>` +
`RuleForEach` is what lets one call reject the upload; a per-row loop reports
only the first failure. What the rules assert is **module-feature**; the profile
is **automapper-mapping**.

The request is one `IFormFile? File` property; its validator gates the extension
with `ZipExtension.ExcelExtension.Contains(Path.GetExtension(file!.FileName))`.
That cross-file reuse of the whitelist is why the zip helper is `public`.

The read, which is the whole of what this skill owns inside the service:

```csharp
private const string StartCell = "B3";

List<ImportEntityData> rows =
[
    .. request.File!
        .OpenReadStream()
        .Query<ImportEntityData>(startCell: StartCell)
        .Where(ZipExtension.BuildEmptyFilter<ImportEntityData>())
];
```

Validate, map, then insert inside `BeginTransactionAsync` /
`CommitTransactionAsync` with `RollbackTransactionAsync` in the catch —
`references/import-service-pattern.md` §5 carries that skeleton whole.

`startCell` is the first **data** cell, below the header band. It is the only
thing binding the POCO to the sheet layout, so it belongs next to the flow as a
`private const string`, not guessed per call.

Endpoint — cap the upload with one named constant used in both attributes:

```csharp
private const long MaxFileSize = 1 * 1024 * 1024 * 1024;

[HttpPost]
[RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
[RequestSizeLimit(MaxFileSize)]
// permission or API-key attribute — auth-and-security
public async Task<ActionResult<SuccessResultWrapper<object>>> ImportAsync([FromForm] ImportEntityRequest request)
```

An unbounded form also appears in the corpus (`[DisableRequestSizeLimit]` with
`MultipartBodyLengthLimit = int.MaxValue`). Prefer the bounded pair: one
constant caps the request in both places, and the ceiling becomes a decision
someone made rather than one nobody made.

**Read `references/import-service-pattern.md`** before writing the flow — it
carries the skeleton, the staging lifecycle and the endpoint quartet.

### Import a zip of workbook plus media

```csharp
private const string StartCell = "A2";
private const string MediaFolderName = "Media";

ZipFile zips = ZipFile.Read(request.File!.OpenReadStream());
ICollection<ZipEntry> entries = zips.Entries;

// Structural gate — before a single cell is read.
if (entries.GetDirectories(MediaFolderName).Length != 1 ||
    entries.Count(ZipExtension.IsExcelFile) != 1 ||
    entries.Any(entry => entry.UncompressedSize > settings.MaxSizeImage * 1024))
{
    throw new BadRequestException(/* message-keys */);
}

List<ImportEntityData> rows = entries
    .First(ZipExtension.IsExcelFile)
    .EntryExcelCastTo<ImportEntityData>(StartCell);
```

Per-row media then goes to a per-request temp directory via `GetEntriesByParent`
+ `SaveImage`, the insert is transactional, and the temp directory is cleared in
`finally` — `references/import-service-pattern.md` §6 carries the whole flow,
including the per-row structural guard this summary omits.

`EntryExcelCastTo<T>` extracts the entry into a `MemoryStream`, runs
`Query<T>(startCell:)` and applies `BuildEmptyFilter<T>` — the same two rules as
principle 3, packaged. `SaveImage` writes into a per-request temp directory and
appends a `large` marker to the file name when the entry exceeds a configured
size, so an oversized image can be post-processed by name.

A second helper variant exists in the corpus with a slightly different surface
(`GetRootDirectory`, `GetDirectory`, `GetImagesDirectoryName`, and an additional
`FileAttributes.Archive` check inside `IsImages`). The version in
`references/zip-extension.cs` is the one to recreate: it keeps the modern
syntax and `SaveImage`, is `public` so request validators can reach the
extension whitelist, and adopts the `Archive` check on `IsImages`.

**Read `references/zip-extension.cs`** before any zip import work — the
level-based entry matching is what goes subtly wrong when retyped by hand.

### Serve and replace the import template

The template handed to users is a real file on disk, downloadable by anyone with
the module's Import permission and replaceable out-of-band.

The module service holds the name as a `private const string ImportFileName` and
delegates both calls straight to the extension; the endpoints stay thin:

```csharp
[HttpGet("Import/Template")]
[HasPermission(/* the module's Import permission */)]
public ActionResult<SuccessResultWrapper<string>> DownloadImportTemplateAsync()
    => OkWrapper(entityService.DownTemplateImport());

[HttpPut("Import/Template")]
[ApiKey]
public async Task<ActionResult<SuccessResultWrapper<string>>> UpdateImportTemplateAsync(
    [FromForm] UpdateEntityImportTemplateRequest request)
    => OkWrapper(await entityService.UpdateTemplateImport(request.File!));
```

Download resolves by prefix — `Array.Find(Directory.GetFiles(folderPath), f =>
Path.GetFileName(f).StartsWith(settings.GetTemplateName(fileName)))` — so the
file on disk must begin with the module's constant, and the prefix match is what
lets a replacement upload carry its own extension. Upload therefore has to write
using the **same** name builder:
`Path.Combine(folderPath, settings.GetTemplateName(fileName, fileExtension))`.

> **Corrected against the corpus.** The corpus copy of `UpdateTemplateImport`
> passes only `fileExtension` to `GetTemplateName`, whose signature is
> `GetTemplateName(string fileName, string fileExtension = "")`. The replacement
> is then saved under a name consisting of the extension alone, which the prefix
> lookup in `DownTemplateImport` can never match. That form — both
> arguments, in order — is what this skill ships, and it is what
> `references/import-template-extension.cs` contains. Recreate from the
> reference, not from an existing project copy.

> **Known divergence, surfaced not resolved.** `ImportTemplateExtension` resolves
> its folder from `Directory.GetCurrentDirectory()`, `ExcelExtension.ExportByTemplate`
> from `AppDomain.CurrentDomain.BaseDirectory`. Both are reproduced as-is in
> `references/`. Pick one anchor per project and use it in both places.

> **Documentation-derived** — not corpus-verified. These two anchors are not
> guaranteed to resolve to the same directory: the first follows the process
> working directory, the second the directory holding the binaries, and they
> diverge when a host starts the process from somewhere else.

**Read `references/import-template-extension.cs`** when adding template serving —
it carries the extension, the settings type and the corrected call.

## Anti-patterns

### Opening a `MemoryStream` at the call site

```csharp
// BAD — a bespoke seventh variant of a 23-line helper
MemoryStream stream = new();
stream.SaveAs(rows);
return File(stream, ExcelContentType, fileName);
```

```csharp
// GOOD — recreate the helper from references/excel-extension.cs, then call it
Stream stream = ExcelExtension.Export(rows);
return File(stream, ExcelContentType, fileName);
```

**Why:** every corpus copy rewinds before returning, and putting that inside the
helper is the point — a call site that rolls its own has to remember it, and two
call sites later there are two subtly different copies. "The project does not
have `ExcelExtension`" is a reason to add the file, not to inline it.

### Querying with no start cell and no empty filter

```csharp
// BAD — the header band becomes row data, and trailing formatted rows
// arrive as all-default entities
List<ImportEntityData> rows = [.. stream.Query<ImportEntityData>()];
```

**Why:** both corpus flows pass a start cell and both filter — the good form is
the read shown under "Import a bare `.xlsx` upload" above. The start cell is a
property of the template you shipped, so it belongs next to the flow as a
`const`, not guessed per call.

### Trusting the file extension of a zip upload

```csharp
// BAD — ".zip" in a file name is not evidence of a zip
RuleFor(x => x.File).NotEmpty()
    .Must(file => Path.GetExtension(file!.FileName) == ".zip");
```

```csharp
// GOOD — the check both corpus zip flows use
RuleFor(x => x.File).NotEmpty()
    .Must(file => ZipFile.IsZipFile(file!.OpenReadStream(), true));
```

**Why:** the archive is about to be extracted to disk, so the gate before it
should read the file, not its name. The extension whitelist is the right gate
for a plain spreadsheet upload, where nothing is unpacked.

### Checking the database once per row

```csharp
// BAD — N round trips, and rows staged by another import session are invisible
foreach (ImportEntityData row in rows)
{
    if (await repository.Find(x => x.Code == row.Code).AnyAsync(ct))
    {
        throw new BadRequestException(/* ... */);
    }
}
```

```csharp
// GOOD — one set-based check, filters ignored
if (await repositoryWrapper.Repository<Entity>()
        .Find()
        .IgnoreQueryFilters()
        .AnyAsync(x => rows.Select(r => r.Code).Contains(x.Code), ct))
{
    throw new BadRequestException(/* message-keys */);
}
```

**Why:** an import is bulk by definition, so the per-row shape scales with the
workbook. `IgnoreQueryFilters` is not decoration: where staging is used, the
entity's global query filter is `ImportSessionId == null`, so staged rows are
invisible to an ordinary query. Without it, a uniqueness check cannot see rows
another session has already staged and both sessions insert the same value —
which is why all three corpus uniqueness checks carry it. Query shaping beyond
this is **ef-core-data-access**.

### Extracting media without a temp directory, or cleaning only on success

```csharp
// BAD — files survive a rollback and accumulate
foreach (ZipEntry entry in mediaEntries) { entry.Extract(uploadRoot); }
await CommitAsync();
ClearFolder(uploadRoot);
```

```csharp
// GOOD — per-request directory, cleanup in finally
string tempDirectoryName = string.Join(
    Path.AltDirectorySeparatorChar, MediaFolderName, Guid.NewGuid().ToString());
try
{
    // extract, upload, insert, commit
}
finally
{
    ClearFolder(tempDirectoryName);
}
```

**Why:** the transaction can roll back after the files are on disk, and two
concurrent imports must not collide — hence the `Guid` segment. Cleanup after
the commit is skipped by the exception that made cleanup necessary. Where the
files then go is **file-storage**.

### Composing the template path by hand

```csharp
// BAD — a second, independent construction of the same path
string path = Path.Combine("Files", "ImportTemplates", $"{ImportFileName}.xlsx");
```

```csharp
// GOOD
string url = staticFileSettings.DownTemplateImport<Entity>(ImportFileName);
```

**Why:** the download resolves by `StartsWith` over a directory listing while
the upload writes with a name builder. Any third construction of that name
drifts from the other two, and a drifted name does not throw — the lookup simply
finds nothing and the user gets a not-found for a file that is sitting right
there. Keep exactly one name builder and one folder constant, both inside the
extension.

## Decision Guide

| Scenario | Do this |
|---|---|
| Send a list of rows to the client as `.xlsx` | `ExcelExtension.Export(rows)`; the endpoint owns file name and content type — **api-surface**. Missing? `references/excel-extension.cs` |
| The export must match a designed workbook layout | `ExportByTemplate(rows, templateName)`; ship the `.xlsx` under `Files/ExcelTemplates/` |
| Accept a bare `.xlsx` upload | Extension whitelist on the request, then `Query<T>(startCell:)` + `BuildEmptyFilter<T>` — `references/import-service-pattern.md` |
| Accept a zip of workbook plus media | `ZipFile.IsZipFile` on the request, structural checks, then `EntryExcelCastTo<T>` — `references/zip-extension.cs` |
| Cap the size of an upload endpoint | One `MaxFileSize` constant in both `[RequestSizeLimit]` and `[RequestFormLimits]` |
| Decide what a row must satisfy | `AbstractValidator<List<T>>` + `RuleForEach`, invoked once after the rows are read; the rules themselves are **module-feature** |
| Store or serve the extracted media | Temp directory, then a directory upload — **file-storage** |
| Users must review before anything is committed | Stage with an import-session marker plus confirm, delete and scheduled-cleanup — `references/import-service-pattern.md` |
| List the staged rows for review | A search endpoint over the staged set — **list-query-pipeline** |
| Serve or replace the template users fill in | `DownTemplateImport` / `UpdateTemplateImport` — `references/import-template-extension.cs` |
| Map a row POCO onto an entity | A `Profile` colocated with the row POCO — **automapper-mapping** |
| Path joining and other small utilities | **common-extensions** |
| None of these helpers exist in this project | Recreate the file verbatim from `references/`, then call it. Never inline at the call site, never copy from another project's path |
