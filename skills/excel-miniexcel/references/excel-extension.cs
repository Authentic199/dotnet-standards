// ---------------------------------------------------------------------------
// ExcelExtension — the export side of MiniExcel.
//
// RECREATE VERBATIM. This file is identical in every reference solution and has
// no local variations. If the project you are working in does not have it, add
// it at Infrastructure/Facades/Common/Extensions/ExcelExtension.cs exactly as
// written below and call it. Never inline a MemoryStream + SaveAs at a call
// site, and never copy this file out of another project.
//
// Dependencies:
//   * NuGet  MiniExcel (MiniExcelLibs). Corpus versions: 1.31.2, one at 1.30.2.
//   * PathExtension — a house utility owned by the common-extensions skill.
//     If the project lacks it, recreate it from there. Do not substitute
//     Path.Combine: PathExtension.Combine has its own handling for empty and
//     already-rooted segments, and the two do not agree on those inputs.
//
// Template files: ExportByTemplate reads a designed .xlsx from
//   <base directory>/Files/ExcelTemplates/{templateName}.xlsx
// Ship the template with the application so it is present beside the binaries
// at run time. The template is addressed by bare name — no path, no extension.
// This folder is NOT the same as Files/ImportTemplates/, which holds the blank
// workbooks handed to users for filling in (see import-template-extension.cs).
// ---------------------------------------------------------------------------

using MiniExcelLibs;

namespace Infrastructure.Facades.Common.Extensions;

public static class ExcelExtension
{
    /// <summary>
    /// Writes <paramref name="data"/> to a new .xlsx stream and returns it
    /// rewound to position 0, ready to hand to a file result.
    /// One property of T per column, in declaration order; property names
    /// become the header row.
    /// </summary>
    public static Stream Export<T>(IEnumerable<T> data)
    {
        Stream memoryStream = new MemoryStream();
        memoryStream.SaveAs(data);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }

    /// <summary>
    /// Fills a designed template workbook with <paramref name="data"/> and
    /// returns the result rewound to position 0.
    /// Use this when the output must match a fixed layout — merged cells, a
    /// branded header band, set column widths — rather than a plain grid.
    /// </summary>
    public static Stream ExportByTemplate<T>(IEnumerable<T> data, string templateName)
    {
        string templatePath = PathExtension.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Files/ExcelTemplates/{templateName}.xlsx");
        Stream memoryStream = new MemoryStream();
        memoryStream.SaveAsByTemplate(templatePath, data);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }
}
