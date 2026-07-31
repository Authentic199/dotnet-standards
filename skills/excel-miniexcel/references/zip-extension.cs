// ---------------------------------------------------------------------------
// ZipExtension — reading a zip that contains one workbook plus a media folder.
//
// RECREATE VERBATIM. If the project you are working in does not have this file,
// add it at Infrastructure/Facades/Common/Extensions/ZipExtension.cs exactly as
// written below. The level-based entry matching and the compiled empty-row
// filter are the parts most easily broken when retyped from memory — copy, do
// not paraphrase. Never inline zip walking at a call site, and never copy this
// file out of another project.
//
// Dependencies:
//   * NuGet  DotNetZip (Ionic.Zip)  — ZipFile / ZipEntry. Corpus version 1.16.0.
//   * NuGet  HeyRed.Mime            — MimeTypesMap
//   * NuGet  MiniExcel              — Query<T>
//   * RegexExtension.ReplaceSpecialCharacters — a house utility owned by the
//     common-extensions skill; recreate it from there if missing.
//
// NAMING, INTENTIONAL: the public field `ExcelExtension` below has the same
// name as the ExcelExtension class in excel-extension.cs. That collision is
// house canon — request validators reference `ZipExtension.ExcelExtension` by
// that name. Do not "fix" it by renaming the field.
//
// The class is public (not internal) because request validators in other files
// read the extension whitelist from it.
//
// DECOUPLING: SaveImage takes the temporary root path as a parameter rather
// than reading it from a storage static. Object storage, upload and serving are
// owned by the file-storage skill; this helper only produces local files.
//
// Two mechanical corrections applied to the source this was distilled from:
//   1. IsImages also requires FileAttributes.Archive, matching IsExcelFile, so
//      a directory entry whose name happens to map to an image MIME type is
//      not treated as a file.
//   2. As a consequence of the decoupling above, the NotSupportedException in
//      SaveImage now names this class instead of the storage class it was
//      copied from.
// ---------------------------------------------------------------------------

using HeyRed.Mime;
using Ionic.Zip;
using MiniExcelLibs;
using System.Linq.Expressions;
using System.Reflection;

namespace Infrastructure.Facades.Common.Extensions;

public static class ZipExtension
{
    /// <summary>
    /// Allowed workbook extensions. Referenced by upload request validators in
    /// other files — this is the one owner of the list.
    /// </summary>
    public static readonly string[] ExcelExtension = [".xlsx", ".xls", ".xlsm"];

    private const StringSplitOptions SplitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
    private const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Static;

    /// <summary>
    /// Stored relative path shape: {Directory}/{Prefix}_{FileName}{Extension}
    /// </summary>
    private const string FileNameFormat = "{0}/{1}_{2}{3}";

    public static bool IsExcelFile(this ZipEntry entry)
        => entry.Attributes == FileAttributes.Archive && ExcelExtension.Contains(Path.GetExtension(entry.FileName));

    public static bool IsImages(this ZipEntry entry)
        => entry.Attributes == FileAttributes.Archive && MimeTypesMap.GetMimeType(entry.FileName).StartsWith("image/");

    public static bool IsValidDirectory(this ZipEntry entry, string directoryName, int level)
        => IsValidEntry(entry, FileAttributes.Directory, directoryName, level, level);

    /// <summary>
    /// Finds every directory entry named <paramref name="directoryName"/> that
    /// sits at <paramref name="level"/> in the archive's directory structure,
    /// counted from the root of the .zip.
    /// Used for structural checks — "there must be exactly one media folder".
    /// </summary>
    public static ZipEntry[] GetDirectories(this IEnumerable<ZipEntry> entries, string directoryName, int level = 1)
    {
        if (level < 1)
        {
            return [];
        }

        return [.. entries.Where(entry => IsValidDirectory(entry, directoryName, level))];
    }

    /// <summary>
    /// Finds every entry whose parent directory is named
    /// <paramref name="parentDirectoryName"/> and that sits at
    /// <paramref name="selectLevel"/> in the archive's directory structure,
    /// counted from the root of the .zip.
    /// Used to collect the media belonging to one row.
    /// </summary>
    public static ZipEntry[] GetEntriesByParent(this IEnumerable<ZipEntry> entries, string parentDirectoryName, FileAttributes fileAttributes, int selectLevel = 1)
    {
        if (selectLevel < 1)
        {
            return [];
        }

        return [.. entries.Where(entry => IsValidEntry(entry, fileAttributes, parentDirectoryName, selectLevel - 1, selectLevel))];
    }

    /// <summary>
    /// Extracts a workbook entry and reads it into row objects, starting at
    /// <paramref name="startCell"/> and dropping rows that carry no values.
    /// Returns an empty list if the entry is not a workbook.
    /// </summary>
    public static List<TResult> EntryExcelCastTo<TResult>(this ZipEntry excelFile, string startCell = "A1")
        where TResult : class, new()
    {
        if (!excelFile.IsExcelFile())
        {
            return [];
        }

        using MemoryStream stream = new();
        excelFile.Extract(stream);

        return [.. stream.Query<TResult>(startCell: startCell).Where(BuildEmptyFilter<TResult>())];
    }

    /// <summary>
    /// Saves an image entry beneath <paramref name="tempDirectoryName"/> inside
    /// <paramref name="tempRootPath"/> and returns the saved path relative to
    /// <paramref name="tempRootPath"/>.
    /// </summary>
    /// <param name="tempRootPath">Absolute root of the local temporary file area. Owned by the caller — file-storage.</param>
    /// <param name="tempDirectoryName">Directory beneath the root for this import run — use a fresh GUID per run.</param>
    /// <param name="markLargeFileSize">Uncompressed size at or above which the saved name gains a `large` marker; null disables marking.</param>
    public static string SaveImage(this ZipEntry entry, string tempRootPath, string tempDirectoryName, long? markLargeFileSize = null)
    {
        if (!entry.IsImages())
        {
            throw new NotSupportedException($"{nameof(ZipExtension)}:{nameof(SaveImage)} not support for file {entry.FileName}");
        }

        string fileName = entry.FileName.Split(Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)[^1];

        // The marker goes on before formatting, so it stays attached to the
        // original name: 650.JPG -> {ticks}_650large.JPG. A later resize pass
        // finds oversized images by that name.
        if (markLargeFileSize.HasValue && entry.UncompressedSize >= markLargeFileSize)
        {
            fileName = $"{Path.GetFileNameWithoutExtension(fileName)!}large{Path.GetExtension(fileName)}";
        }

        string relativePath = FormatFileName(fileName, tempDirectoryName);

        Directory.CreateDirectory(Path.Combine(tempRootPath, tempDirectoryName));

        using FileStream fileStream = new(Path.Combine(tempRootPath, relativePath), FileMode.Create, FileAccess.ReadWrite);
        entry.Extract(fileStream);

        return relativePath;
    }

    /// <summary>
    /// Compiles a predicate that keeps a row if ANY property carries a value:
    /// a string property that is not null or whitespace, or a non-string
    /// property that differs from its type default.
    /// A workbook hands back rows that are formatted but empty; without this
    /// filter they arrive as all-default objects and get inserted.
    /// </summary>
    public static Func<T, bool> BuildEmptyFilter<T>()
        where T : class
    {
        Type sourceType = typeof(T);
        ParameterExpression parameter = Expression.Parameter(sourceType, "x");
        Expression? filterExpression = default;
        foreach (PropertyInfo propertyInfo in sourceType.GetProperties())
        {
            Expression memberAccess = Expression.Property(parameter, propertyInfo);
            if (propertyInfo.PropertyType == typeof(string))
            {
                MethodCallExpression stringINoWsEx = CallIsNullOrWhiteSpace(memberAccess);
                Expression notEx = Expression.Not(stringINoWsEx);
                filterExpression = filterExpression == null ? notEx : Expression.OrElse(filterExpression, notEx);
                continue;
            }

            BinaryExpression notNullExpression = Expression.NotEqual(memberAccess, Expression.Default(propertyInfo.PropertyType));
            filterExpression = filterExpression == null ? notNullExpression : Expression.OrElse(filterExpression, notNullExpression);
        }

        return Expression.Lambda<Func<T, bool>>(filterExpression!, parameter).Compile();
    }

    private static bool IsValidEntry(this ZipEntry entry, FileAttributes fileAttributes, string entryName, int entryLevel = 0, int selectLevel = 1)
    {
        string[] parts = entry.FileName.Split(Path.AltDirectorySeparatorChar, SplitOptions);
        return (entry.Attributes & fileAttributes) != 0 && parts.Length == selectLevel + 1 && string.Equals(parts[entryLevel], entryName, StringComparison.OrdinalIgnoreCase);
    }

    private static MethodCallExpression CallIsNullOrWhiteSpace(Expression memberAccess)
    {
        MethodInfo method = typeof(string).GetMethod(nameof(string.IsNullOrWhiteSpace), StaticFlags, [typeof(string)])!;
        return Expression.Call(method, memberAccess);
    }

    /// <summary>
    /// Builds the stored relative path: {directory}/{ticks}_{name}{extension}.
    /// The ticks prefix is what keeps leaf names distinct once entries from
    /// different folders in the archive land side by side in one flat temp
    /// directory.
    /// If the project's file-storage facade owns an equivalent formatter, the
    /// two must produce the same shape — these local files are later uploaded
    /// directory-wise, unchanged.
    /// ReplaceSpecialCharacters is owned by common-extensions.
    /// </summary>
    private static string FormatFileName(string fileName, string directory)
    {
        if (Path.GetExtension(fileName) is null)
        {
            throw new NotSupportedException("Invalid file name");
        }

        return string.Format(
            FileNameFormat,
            directory,
            DateTime.UtcNow.Ticks,
            Path.GetFileNameWithoutExtension(fileName).ReplaceSpecialCharacters(string.Empty),
            Path.GetExtension(fileName));
    }
}
