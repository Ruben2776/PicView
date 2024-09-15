﻿using System.Diagnostics;
using Cysharp.Text;
using PicView.Core.FileHandling;
using PicView.Core.Localization;

namespace PicView.Core.Navigation;

public static class TitleHelper
{
    /// <summary>
    /// The name of the application.
    /// </summary>
    internal const string AppName = "PicView";

    /// <summary>
    /// Gets the title string based on the specified parameters.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <param name="index">The index of the image's location.</param>
    /// <param name="fileInfo">The file information of the image.</param>
    /// <param name="zoomValue">The zoom value of the image.</param>
    /// <param name="filesList">The list of files.</param>
    /// <returns>An array of three strings representing different aspects of the title.</returns>
    public static string[] GetTitle(int width, int height, int index, FileInfo? fileInfo, double zoomValue, List<string> filesList)
    {
        // Check index validity
        if (index < 0 || index >= filesList.Count)
        {
            return ReturnError("index invalid");
        }

        // Check if file info is present or not
        if (fileInfo == null)
        {
            try
            {
                fileInfo = new FileInfo(filesList[index]);
            }
            catch (Exception e)
            {
                return ReturnError("FileInfo exception " + e.Message);
            }
        }

        // Check if file exists or not
        if (!fileInfo.Exists)
        {
            fileInfo = new FileInfo(Path.GetInvalidFileNameChars().Aggregate(fileInfo.FullName,
                (current, c) => current.Replace(c.ToString(), string.Empty)));
            if (!fileInfo.Exists)
                return ReturnError("FileInfo does not exist?");
        }

        var files = filesList.Count == 1
            ? TranslationHelper.Translation.File
            : TranslationHelper.Translation.Files;
        
        using var sb = ZString.CreateStringBuilder(true);
        sb.Append(fileInfo.Name);
        sb.Append(' ');
        sb.Append(index + 1);
        sb.Append('/');
        sb.Append(filesList.Count);
        sb.Append(' ');
        sb.Append(files);
        sb.Append(" (");
        sb.Append(width);
        sb.Append(" x ");
        sb.Append(height);
        sb.Append(StringAspect(width, height));
        sb.Append(fileInfo.Length.GetReadableFileSize());

        // Check if ZoomPercentage is not empty
        if (!string.IsNullOrEmpty(ZoomPercentage(zoomValue)))
        {
            sb.Append(", ");
            sb.Append(ZoomPercentage(zoomValue));
        }

        sb.Append(" - ");
        sb.Append(AppName);

        var array = new string[3];
        array[0] = sb.ToString();
        sb.Remove(sb.Length - (AppName.Length + 3),
            AppName.Length + 3); // Remove AppName + " - "
        array[1] = sb.ToString();
        sb.Replace(fileInfo.Name, filesList[index]);
        array[2] = sb.ToString();
        return array;
    }

    private static string[] ReturnError(string exception)
    {
#if DEBUG
        Trace.WriteLine(exception);
#endif
        return
        [
            TranslationHelper.Translation.UnexpectedError ?? "UnexpectedError",
            TranslationHelper.Translation.UnexpectedError ?? "UnexpectedError",
            TranslationHelper.Translation.UnexpectedError ?? "UnexpectedError",
        ];
    }

    private static string ZoomPercentage(double zoomValue)
    {
        if (zoomValue is 1)
        {
            return string.Empty;
        }

        var zoom = Math.Round(zoomValue * 100);

        return zoom + "%";
    }

    /// <summary>
    /// Returns string with file name,
    /// zoom, aspect ratio and resolution
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="path"></param>
    /// <param name="zoomValue"></param>
    /// <returns></returns>
    public static string[] TitleString(int width, int height, string path, double zoomValue)
    {
        using var sb = ZString.CreateStringBuilder(true);
        sb.Append(path);
        sb.Append(" (");
        sb.Append(width);
        sb.Append(" x ");
        sb.Append(height);
        sb.Append(StringAspect(width, height));

        if (!string.IsNullOrEmpty(ZoomPercentage(zoomValue)))
        {
            sb.Append(", ");
            sb.Append((string?)ZoomPercentage(zoomValue));
        }

        sb.Append(" - ");
        sb.Append(AppName);

        var array = new string[2];
        array[0] = sb.ToString();
        sb.Remove(sb.Length - (AppName.Length + 3), AppName.Length + 3); // Remove AppName + " - "
        array[1] = sb.ToString();
        return array;
    }

    /// <summary>
    /// Generates a string representation of the aspect ratio based on the provided width and height.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <returns>
    /// A string representation of the aspect ratio if within specified limits; otherwise, an empty string.
    /// </returns>
    internal static string StringAspect(int width, int height)
    {
        if (width is 0 || height is 0)
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

        return $", {x} : {y}) ";
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
            if (y == 0) return x;
            var x1 = x;
            x = y;
            y = x1 % y;
        }
    }
}