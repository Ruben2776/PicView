using System.Diagnostics;
using Cysharp.Text;
using PicView.Core.Extensions;
using PicView.Core.Localization;

namespace PicView.Core.Navigation;

/// <summary>
/// Provides methods for generating window titles for image display,
/// including the ability to format titles with image properties such as
/// file name, resolution, zoom level, and aspect ratio.
/// </summary>
public static class ImageTitleFormatter
{
    /// <summary>
    /// Struct that holds different representations of a window title.
    /// </summary>
    public readonly struct WindowTitles
    {
        /// <summary>
        /// The base title containing the image name, index, file count, resolution, and other details.
        /// </summary>
        public string BaseTitle { get; init; }

        /// <summary>
        /// The base title with the application name appended at the end.
        /// </summary>
        public string TitleWithAppName { get; init; }

        /// <summary>
        /// The title with the full file path instead of just the file name.
        /// </summary>
        public string FilePathTitle { get; init; }
    }
    
    /// <summary>
    /// The name of the application.
    /// </summary>
    internal const string AppName = "PicView";

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
    public static WindowTitles GenerateTitleStrings(int width, int height, int index, FileInfo? fileInfo, double zoomValue,
        List<string> filesList)
    {
        if (index < 0 || index >= filesList.Count)
        {
            return GenerateErrorTitle($"{nameof(ImageTitleFormatter)}:{nameof(GenerateTitleStrings)} - index invalid");
        }

        if (fileInfo == null)
        {
            try
            {
                fileInfo = new FileInfo(filesList[index]);
            }
            catch (Exception e)
            {
                return GenerateErrorTitle(
                    $"{nameof(ImageTitleFormatter)}:{nameof(GenerateTitleStrings)} - FileInfo exception \n{e.Message}");
            }
        }

        if (!fileInfo.Exists)
        {
            return GenerateErrorTitle(
                $"{nameof(ImageTitleFormatter)}:{nameof(GenerateTitleStrings)} - FileInfo does not exist");
        }

        using var sb = ZString.CreateStringBuilder(true);
        
        // Build the base title (common parts)
        sb.Append(fileInfo.Name);
        sb.Append(' ');
        sb.Append(index + 1);
        sb.Append('/');
        sb.Append(filesList.Count);
        sb.Append(' ');
        sb.Append(filesList.Count == 1 ? TranslationHelper.Translation.File : TranslationHelper.Translation.Files);
        sb.Append(" (");
        sb.Append(width);
        sb.Append(" x ");
        sb.Append(height);
        sb.Append(FormatAspectRatio(width, height));
        sb.Append(fileInfo.Length.GetReadableFileSize());

        // Add zoom information if applicable
        var zoomString = FormatZoomPercentage(zoomValue);
        if (zoomString is not null)
        {
            sb.Append(", ");
            sb.Append(zoomString);
        }

        var baseTitle = sb.ToString();

        // Full title with AppName
        var fullTitle = $"{baseTitle} - {AppName}";

        // Title with file path instead of file name
        var filePathTitle = baseTitle.Replace(fileInfo.Name, fileInfo.FullName);

        return new WindowTitles
        {
            BaseTitle = baseTitle,
            TitleWithAppName = fullTitle,
            FilePathTitle = filePathTitle
        };
    }


    /// <inheritdoc cref="GenerateTitleStrings(int, int, int, FileInfo, double, List{string})" />
    public static WindowTitles GenerateTitleStrings(double width, double height, int index, FileInfo? fileInfo,
        double zoomValue, List<string> filesList)
    {
        var newWidth = Convert.ToInt32(width);
        var newHeight = Convert.ToInt32(height);
        return GenerateTitleStrings(newWidth, newHeight, index, fileInfo, zoomValue, filesList);
    }

    /// <summary>
    /// Generates a set of error titles in case of invalid parameters or exceptions during title generation.
    /// </summary>
    /// <param name="exception">A string representing the error message or exception details.</param>
    /// <returns>A <see cref="WindowTitles"/> struct containing error titles.</returns>
    private static WindowTitles GenerateErrorTitle(string exception)
    {
#if DEBUG
        Trace.WriteLine(exception);
        Debug.Assert(TranslationHelper.Translation.UnexpectedError != null);
#endif

        return new WindowTitles
        {
            BaseTitle = TranslationHelper.Translation.UnexpectedError,
            TitleWithAppName = TranslationHelper.Translation.UnexpectedError,
            FilePathTitle = TranslationHelper.Translation.UnexpectedError
        };
    }

    /// <summary>
    /// Formats the zoom percentage for display, omitting the zoom information if it's 0 or 100%.
    /// </summary>
    /// <param name="zoomValue">The current zoom level of the image as a double value.</param>
    /// <returns>A formatted string representing the zoom percentage, or null if the zoom is 0 or 100%.</returns>
    private static string? FormatZoomPercentage(double zoomValue)
    {
        if (zoomValue is 0 or 1)
        {
            return null;
        }

        var zoom = Math.Round(zoomValue * 100);

        return zoom + "%";
    }

    /// <summary>
    /// Generates a window title for a single image, including its name, resolution, aspect ratio, and zoom level.
    /// </summary>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="name">Display name of the image.</param>
    /// <param name="zoomValue">The current zoom level of the image.</param>
    /// <returns>A <see cref="WindowTitles"/> struct containing the generated titles for the single image.</returns>
    public static WindowTitles GenerateTitleForSingleImage(int width, int height, string name, double zoomValue)
    {
        using var sb = ZString.CreateStringBuilder(true);

        // Build the base title (common parts)
        sb.Append(name);
        sb.Append(" (");
        sb.Append(width);
        sb.Append(" x ");
        sb.Append(height);
        sb.Append(FormatAspectRatio(width, height));

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

    /// <inheritdoc cref="GenerateTitleForSingleImage(int, int, string, double)" />
    public static WindowTitles GenerateTitleForSingleImage(double width, double height, string name, double zoomValue)
    {
        var newWidth = Convert.ToInt32(width);
        var newHeight = Convert.ToInt32(height);
        return GenerateTitleForSingleImage(newWidth, newHeight, name, zoomValue);
    }

    /// <summary>
    /// Generates a string representing the aspect ratio of an image based on its width and height.
    /// If the aspect ratio exceeds a certain limit, no aspect ratio is returned.
    /// </summary>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <returns>A string representing the aspect ratio in the format "x:y", or an empty string if the ratio is too large.</returns>
    public static string FormatAspectRatio(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return ") ";
        }

        // Calculate the greatest common divisor
        var gcd = GCD(width, height);
        var x = width / gcd;
        var y = height / gcd;

        // Check if aspect ratio is within specified limits
        if (x > 48 || y > 18)
        {
            return ") ";
        }

        return $", {x}:{y}) ";
    }

    /// <summary>
    /// Calculates the Greatest Common Divisor (GCD) of two integers.
    /// </summary>
    /// <param name="x">The first integer.</param>
    /// <param name="y">The second integer.</param>
    /// <returns>The GCD of the two integers.</returns>
    // ReSharper disable once InconsistentNaming
    public static int GCD(int x, int y)
    {
        while (true)
        {
            if (y == 0)
            {
                return x;
            }

            var x1 = x;
            x = y;
            y = x1 % y;
        }
    }
}