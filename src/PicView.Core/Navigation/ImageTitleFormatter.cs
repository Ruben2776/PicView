using System.Diagnostics;
using Cysharp.Text;
using PicView.Core.Extensions;
using PicView.Core.Localization;

namespace PicView.Core.Navigation;

public static class ImageTitleFormatter
{
    /// <summary>
    /// The name of the application.
    /// </summary>
    internal const string AppName = "PicView";

    /// <summary>
    /// Generates the title strings based on the specified parameters.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <param name="index">The index of the image's location.</param>
    /// <param name="fileInfo">The file information of the image.</param>
    /// <param name="zoomValue">The zoom value of the image.</param>
    /// <param name="filesList">The list of files.</param>
    /// <returns>An array of three strings representing different aspects of the title.</returns>
    public static string[] GenerateTitleStrings(int width, int height, int index, FileInfo? fileInfo, double zoomValue,
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

        return [fullTitle, baseTitle, filePathTitle];
    }


    /// <inheritdoc cref="GenerateTitleStrings(int, int, int, FileInfo, double, List{string})" />
    public static string[] GenerateTitleStrings(double width, double height, int index, FileInfo? fileInfo,
        double zoomValue, List<string> filesList)
    {
        var newWidth = Convert.ToInt32(width);
        var newHeight = Convert.ToInt32(height);
        return GenerateTitleStrings(newWidth, newHeight, index, fileInfo, zoomValue, filesList);
    }

    private static string[] GenerateErrorTitle(string exception)
    {
#if DEBUG
        Trace.WriteLine(exception);
        Debug.Assert(TranslationHelper.Translation.UnexpectedError != null, "TranslationHelper.Translation.UnexpectedError != null");
#endif
        
        return
        [
            TranslationHelper.Translation.UnexpectedError,
            TranslationHelper.Translation.UnexpectedError,
            TranslationHelper.Translation.UnexpectedError
        ];
    }

    private static string? FormatZoomPercentage(double zoomValue)
    {
        if (zoomValue is 1)
        {
            return null;
        }

        var zoom = Math.Round(zoomValue * 100);

        return zoom + "%";
    }

    /// <summary>
    /// Returns string with file name, zoom, aspect ratio and resolution
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <param name="name">The name to display.</param>
    /// <param name="zoomValue">The zoom value of the image.</param>
    public static string[] GenerateTitleForSingleImage(int width, int height, string name, double zoomValue)
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

        return [fullTitle, baseTitle];
    }

    /// <inheritdoc cref="GenerateTitleForSingleImage(int, int, string, double)" />
    public static string[] GenerateTitleForSingleImage(double width, double height, string name, double zoomValue)
    {
        var newWidth = Convert.ToInt32(width);
        var newHeight = Convert.ToInt32(height);
        return GenerateTitleForSingleImage(newWidth, newHeight, name, zoomValue);
    }

    /// <summary>
    /// Generates a string representation of the aspect ratio based on the provided width and height.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    private static string FormatAspectRatio(int width, int height)
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