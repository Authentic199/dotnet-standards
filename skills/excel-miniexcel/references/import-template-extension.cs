// ---------------------------------------------------------------------------
// ImportTemplateExtension + StaticFileSettings — serving and replacing the
// blank workbook that users download, fill in, and upload back.
//
// RECREATE VERBATIM. Two types, shipped together because neither works without
// the other. This reference holds both for reading; split them when you create
// them:
//   Infrastructure/Facades/Common/Extensions/ImportTemplateExtension.cs
//   Infrastructure/Facades/Common/Settings/StaticFileSettings.cs
//
// Template files live at <content root>/Files/ImportTemplates/ and are served
// statically. This is NOT Files/ExcelTemplates/, which holds the designed
// layouts consumed by ExcelExtension.ExportByTemplate. Never build the
// "Files/ImportTemplates" path at a call site — the owning service supplies
// only the template name.
//
// CORRECTED — read this before comparing against any existing copy:
//   UpdateTemplateImport below calls GetTemplateName(fileName, fileExtension).
//   Copies exist that pass only the extension — GetTemplateName(fileExtension).
//   Because the parameter list is (string fileName, string fileExtension = "")
//   the extension lands in the fileName slot, so the replacement is saved under
//   a name consisting of the extension alone (".xlsx"). DownTemplateImport then
//   looks the file up by StartsWith(<module constant>) over the folder listing
//   and can never match it: the upload reports success, the download reports
//   not-found, and the file is sitting right there. Both arguments, in order.
//
// Path anchor, reproduced as-is: this file resolves its folder from
// Directory.GetCurrentDirectory(), while ExcelExtension.ExportByTemplate
// resolves from AppDomain.CurrentDomain.BaseDirectory. Pick one anchor per
// project and use it in both places rather than inheriting the split.
//
// House types kept as-is:
//   BadRequestException          -> error-handling owns the exception family
//   Messages<TEntity>            -> message-keys owns the text
//   validationContext.Required() -> a house settings-validation extension;
//                                   recreate it from common-extensions if the
//                                   project lacks it, do not re-implement here
// ---------------------------------------------------------------------------

using Core.Common.Exceptions;
using Infrastructure.Facades.Common.Settings;
using Infrastructure.Facades.Definitions;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Facades.Common.Extensions;

public static class ImportTemplateExtension
{
    /// <summary>
    /// Resolves the module's import template on disk and returns its public
    /// URL. <paramref name="fileName"/> is the module's own constant — the
    /// stored file must begin with it.
    /// </summary>
    public static string DownTemplateImport<TEntity>(this StaticFileSettings staticFileSettings, string fileName)
    {
        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Files", "ImportTemplates");
        string? filePath = Array.Find(Directory.GetFiles(folderPath), f => Path.GetFileName(f).StartsWith(staticFileSettings.GetTemplateName(fileName)));

        if (!System.IO.File.Exists(filePath))
        {
            throw new BadRequestException(Messages<TEntity>.NotFound("Template"));
        }

        string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath).Replace("\\", "/");

        return staticFileSettings.GetTemplateUrl(relativePath);
    }

    /// <summary>
    /// Replaces the module's import template with an uploaded file and returns
    /// the URL of the stored result. The new file is written under the same
    /// name builder the download lookup uses, so the two cannot drift.
    /// </summary>
    public static async Task<string> UpdateTemplateImport<TEntity>(this StaticFileSettings staticFileSettings, string fileName, IFormFile file)
    {
        string fileExtension = Path.GetExtension(file.FileName);
        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Files", "ImportTemplates");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // CORRECTED: both arguments, in order. See the header note.
        string filePath = Path.Combine(folderPath, staticFileSettings.GetTemplateName(fileName, fileExtension));

        using (FileStream stream = new(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return staticFileSettings.DownTemplateImport<TEntity>(fileName);
    }
}

// ---------------------------------------------------------------------------
// Infrastructure/Facades/Common/Settings/StaticFileSettings.cs
// ---------------------------------------------------------------------------

using Infrastructure.Facades.Common.Extensions;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Facades.Common.Settings;

public class StaticFileSettings : IValidatableObject
{
    public string BaseUrl { get; set; } = default!;

    public string GetTemplateUrl(string filePath)
    {
        if (!BaseUrl.EndsWith('/'))
        {
            BaseUrl += "/";
        }

        return $"{BaseUrl}{filePath}";
    }

    /// <summary>
    /// The single name builder. The download lookup calls it with the module
    /// constant alone and matches by prefix; the upload calls it with the
    /// constant AND the uploaded file's extension. Never build this name a
    /// third time by hand.
    /// </summary>
    public string GetTemplateName(string fileName, string fileExtension = "") => $"{fileName}{fileExtension}";

    // Required() is a house settings-validation extension — common-extensions.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required();
}

// ---------------------------------------------------------------------------
// Call-site shape — the module constant ties the two calls together, and the
// endpoints stay thin.
// ---------------------------------------------------------------------------

public class EntityService : IEntityService
{
    private const string ImportFileName = "Import_Template_Entity";

    public string DownTemplateImport()
        => staticFileSettings.DownTemplateImport<Entity>(ImportFileName);

    public Task<string> UpdateTemplateImport(IFormFile file)
        => staticFileSettings.UpdateTemplateImport<Entity>(ImportFileName, file);
}

[HttpGet("Import/Template")]
// permission attribute — auth-and-security
public ActionResult<SuccessResultWrapper<string>> DownloadImportTemplateAsync()
    => OkWrapper(entityService.DownTemplateImport(), /* message-keys */);

[HttpPut("Import/Template")]
// API-key or permission attribute — auth-and-security
public async Task<ActionResult<SuccessResultWrapper<string>>> UpdateImportTemplateAsync(
    [FromForm] UpdateEntityImportTemplateRequest request)
    => OkWrapper(await entityService.UpdateTemplateImport(request.File!), /* message-keys */);

public class UpdateEntityImportTemplateRequest
{
    public IFormFile? File { get; set; }
}

public class UpdateEntityImportTemplateValidator : AbstractValidator<UpdateEntityImportTemplateRequest>
{
    // The template flow keeps its own whitelist — it does not depend on the zip
    // helper, which a project doing template serving may well not have.
    private static readonly string[] AllowedExtensions = [".xlsx", ".xls"];

    public UpdateEntityImportTemplateValidator()
    {
        RuleFor(x => x.File)
            .NotEmpty().WithMessage(/* message-keys */)
            .Must(HaveValidExtension).WithMessage(/* message-keys */);
    }

    private static bool HaveValidExtension(IFormFile? file)
    {
        if (file is null)
        {
            return false;
        }

        return AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());
    }
}
