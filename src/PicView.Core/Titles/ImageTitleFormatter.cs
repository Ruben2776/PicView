using System.Runtime.CompilerServices;
using Cysharp.Text;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;

namespace PicView.Core.Titles;

/// <summary>
/// Provides methods for generating window titles for image display,
/// including the ability to format titles with image properties such as
/// file name, resolution, zoom level, and aspect ratio.
/// </summary>
public static class ImageTitleFormatter
{
    /// <summary>
    /// The name of the application.
    /// </summary>
    public const string AppName = "PicView";

    private const double NormalZoomLevel = 100;
    private const double NoZoomLevel = 0.0;


    /// <summary>
    /// Generates the title strings based on the specified parameters, including image properties
    /// such as width, height, file name, zoom level, and current index in the file list.
    /// </summary>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="index">The index of the image in the list.</param>
    /// <param name="fileInfo">The <see cref="FileInfo"/> object representing the image file.</param>
    /// <param name="zoomValue">The current zoom level of the image.</param>
    /// <param name="filesList">The list of image file paths.</param>
    /// <returns>A <see cref="WindowTitles"/> struct containing the generated titles.</returns>
    public static WindowTitles GenerateTitleStrings(int width, int height, int index, FileInfo? fileInfo, double zoomValue, bool hasChanges, IReadOnlyList<FileInfo> filesList)
    {
        if (!TryValidateAndGetFileInfo(index, filesList, fileInfo, out var validatedFileInfo, out var errorTitle))
        {
            return errorTitle;
        }

        var namePart = validatedFileInfo.Name;
        return GenerateTitleStringsCore(width, height, validatedFileInfo, zoomValue, hasChanges, filesList, index, namePart);
    }

    /// <summary>
    /// Generates the title strings for TIFF images, including page navigation information.
    /// </summary>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="index">The index of the image in the list.</param>
    /// <param name="fileInfo">The <see cref="FileInfo"/> object representing the image file.</param>
    /// <param name="tiffNavigationInfo">The TIFF navigation information containing page details.</param>
    /// <param name="zoomValue">The current zoom level of the image.</param>
    /// <param name="filesList">The list of image file paths.</param>
    /// <returns>A <see cref="WindowTitles"/> struct containing the generated titles.</returns>
    public static WindowTitles GenerateTiffTitleStrings(int width, int height, int index, FileInfo fileInfo, TiffManager.TiffNavigationInfo tiffNavigationInfo, double zoomValue, bool hasChanges, List<FileInfo> filesList)
    {
        if (tiffNavigationInfo == null)
        {
            return GenerateErrorTitle();
        }

        if (!TryValidateAndGetFileInfo(index, filesList, fileInfo, out var validatedFileInfo, out var errorTitle))
        {
            return errorTitle;
        }

        var namePart = $"{validatedFileInfo.Name} [{tiffNavigationInfo.CurrentPage + 1}/{tiffNavigationInfo.PageCount}]";
        return GenerateTitleStringsCore(width, height, validatedFileInfo, zoomValue, hasChanges, filesList, index, namePart);
    }

    private static WindowTitles GenerateTitleStringsCore(int width, int height, FileInfo fileInfo, double zoomValue, bool hasChanges, IReadOnlyList<FileInfo> filesList, int index, string namePart)
    {
        using var sb = ZString.CreateStringBuilder(true);

        sb.Append(namePart);
        if (hasChanges)
            sb.Append("*");
        sb.Append(' ');
        sb.Append(index + 1);
        sb.Append('/');
        sb.Append(filesList.Count);
        sb.Append(' ');
        sb.Append(filesList.Count == 1 ? TranslationManager.Translation.File : TranslationManager.Translation.Files);
        sb.Append(" (");
        sb.Append(width);
        sb.Append(" x ");
        sb.Append(height);
        sb.Append(AspectRatioFormatter.FormatAspectRatio(width, height));
        sb.Append(") "); 
        sb.Append(fileInfo.Length.GetReadableFileSize());

        var zoomString = FormatZoomPercentage(zoomValue);
        if (zoomString is not null)
        {
            sb.Append(", ");
            sb.Append(zoomString);
        }

        var baseTitle = sb.ToString();

        var fullTitle = $"{baseTitle} - {AppName}";
        var filePathTitle = baseTitle.Replace(fileInfo.Name, fileInfo.FullName);

        return new WindowTitles
        {
            BaseTitle = baseTitle,
            TitleWithAppName = fullTitle,
            FilePathTitle = filePathTitle
        };
    }

    private static bool TryValidateAndGetFileInfo(int index, IReadOnlyList<FileInfo> filesList, FileInfo? fileInfo, out FileInfo? validatedFileInfo, out WindowTitles errorTitle, [CallerMemberName] string callerName = "")
    {
        validatedFileInfo = null;
        errorTitle = default;

        if (index < 0 || index >= filesList.Count)
        {
            DebugHelper.LogDebug(nameof(ImageTitleFormatter), callerName, "index invalid");
            return false;
        }

        if (fileInfo is null)
        {
            try
            {
                validatedFileInfo = filesList[index];
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(ImageTitleFormatter), callerName, e);
                return false;
            }
        }
        else
        {
            validatedFileInfo = fileInfo;
        }
        
        if (!Settings.Navigation.IsFileWatcherEnabled)
        {
            // Don't check if the file exists if file watcher disabled
            return true;
        }

        if (validatedFileInfo.Exists)
        {
            return true;
        }

        errorTitle = GenerateErrorTitle();
        return false;
    }


    /// <summary>
    /// Generates a set of error titles in case of invalid parameters or exceptions during title generation.
    /// </summary>
    /// <returns>A <see cref="WindowTitles"/> struct containing error titles.</returns>
    private static WindowTitles GenerateErrorTitle()
    {
        return new WindowTitles
        {
            BaseTitle = TranslationManager.Translation.UnexpectedError ?? "",
            TitleWithAppName = TranslationManager.Translation.UnexpectedError ?? "",
            FilePathTitle = TranslationManager.Translation.UnexpectedError ?? ""
        };
    }

    /// <summary>
    /// Formats the zoom percentage for display, omitting the zoom information if it's 0 or 100%.
    /// </summary>
    /// <param name="zoomValue">The current zoom level of the image as a double value.</param>
    /// <returns>A formatted string representing the zoom percentage, or null if the zoom is 0 or 100%.</returns>
    private static string? FormatZoomPercentage(double zoomValue) =>
        zoomValue is NoZoomLevel or NormalZoomLevel ? null : $"{Math.Floor(zoomValue)}%";


    /// <summary>
    /// Generates a window title for a single image, including its name, resolution, aspect ratio, and zoom level.
    /// </summary>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="name">Display name of the image.</param>
    /// <param name="zoomValue">The current zoom level of the image.</param>
    /// <returns>A <see cref="WindowTitles"/> struct containing the generated titles for the single image.</returns>
    public static WindowTitles GenerateTitleForSingleImage(int width, int height, string name, double zoomValue, bool hasChanges)
    {
        using var sb = ZString.CreateStringBuilder(true);

        // Build the base title (common parts)
        sb.Append(name);
        if (hasChanges)
            sb.Append("*");
        sb.Append(" (");
        sb.Append(width);
        sb.Append(" x ");
        sb.Append(height);
        sb.Append(AspectRatioFormatter.FormatAspectRatio(width, height));
        sb.Append(") "); 

        // Add zoom information if applicable
        var zoomString = FormatZoomPercentage(zoomValue);
        if (zoomString is not null)
        {
            sb.Append(", ");
            sb.Append(zoomString);
        }

        var baseTitle = sb.ToString(); // Save the base title (without AppName)

        // Full title with AppName
        var fullTitle = $"{baseTitle} - {AppName}";

        return new WindowTitles
        {
            BaseTitle = baseTitle,
            TitleWithAppName = fullTitle,
            FilePathTitle = baseTitle
        };
    }

}