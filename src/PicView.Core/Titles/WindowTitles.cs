using System.Runtime.InteropServices;

namespace PicView.Core.Titles;

/// <summary>
/// Struct that holds different representations of a window title.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct WindowTitles
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