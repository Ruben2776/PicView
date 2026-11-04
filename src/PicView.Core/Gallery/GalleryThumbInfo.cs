using System.Globalization;
using PicView.Core.Extensions;
using PicView.Core.Localization;

namespace PicView.Core.Gallery;

/// <summary>
/// The labeled texts a gallery item's tooltip shows about its file. Its image size comes with the item's details
/// instead.
/// </summary>
public static class GalleryThumbInfo
{
    /// <summary>The file's size, labeled in the current language: "File size: 100 B".</summary>
    public static string FileSizeText(FileInfo file) =>
        Labeled(TranslationManager.Translation.FileSize, file.Length.GetReadableFileSize());

    /// <summary>The file's modified time, labeled in the current language and culture: "Modified: 10/31/2026 12:30:45".</summary>
    public static string FileDateText(FileInfo file) =>
        Labeled(TranslationManager.Translation.Modified, file.LastWriteTimeUtc.ToString(CultureInfo.CurrentCulture));

    private static string Labeled(string label, string value) => string.Concat(label, ": ", value);
}
