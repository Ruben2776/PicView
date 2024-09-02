namespace PicView.Core.FileHandling;

/// <summary>
/// Class that contains information about supported file extensions.
/// </summary>
public static class SupportedFiles
{
    /// <summary>
    /// List of supported file extensions.
    /// </summary>
    public static readonly string[] FileExtensions =
    [
        ".jpg",
        ".jpeg",
        ".jpe",
        ".png",
        ".bmp",
        ".gif",
        ".jfif",
        ".ico",
        ".webp",
        ".wbmp",
        ".avif",
        ".psd",
        ".psb",
        ".tif",
        ".tiff",
        ".dds",
        ".tga",
        ".heic",
        ".heif",
        ".hdr",
        ".xcf",
        ".jxl",
        ".jp2",
        ".qoi",
        ".thm",
        ".b64",
        ".svg",
        ".svgz",
        ".3fr",
        ".arw",
        ".cr2",
        ".cr3",
        ".crw",
        ".dcr",
        ".dng",
        ".erf",
        ".kdc",
        ".mdc",
        ".mef",
        ".mos",
        ".mrw",
        ".nef",
        ".nrw",
        ".orf",
        ".pef",
        ".raf",
        ".raw",
        ".rw2",
        ".srf",
        ".x3f",
        ".fpx",
        ".pcd",
        ".flif",
        ".pgm",
        ".ppm",
        ".cut",
        ".exr",
        ".dib",
        ".emf",
        ".wmf",
        ".wpg",
        ".pcx",
        ".xbm",
        ".xpm"
    ];

    /// <summary>
    /// List of supported archive file extensions.
    /// </summary>
    public static readonly string[] FileExtensionsArchives =
    [
        ".zip",
        ".7zip",
        ".7z",
        ".rar",
        ".cbr",
        ".cb7",
        ".cbt",
        ".cbz",
        ".xz",
        ".bzip2",
        ".gzip",
        ".tar",
        ".wim",
        ".iso",
        ".cab"
    ];

    /// <summary>
    /// Extension method to check if a file is supported.
    /// </summary>
    /// <param name="file">File to check</param>
    /// <returns>True if file is supported, False otherwise</returns>
    public static bool IsSupported(this string file)
    {
        return FileExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extension method to check if a `FileInfo` is supported.
    /// </summary>
    /// <param name="fileInfo">FileInfo to check</param>
    /// <returns>True if `FileInfo` is supported, False otherwise</returns>
    public static bool IsSupported(this FileInfo fileInfo)
    {
        return FileExtensions.Any(ext => fileInfo.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extension method to check if a file is a supported archive.
    /// </summary>
    /// <param name="file">File to check</param>
    /// <returns>True if file is a supported archive, False otherwise</returns>
    public static bool IsArchive(this string file)
    {
        return FileExtensionsArchives.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extension method to check if a FileInfo is a supported archive.
    /// </summary>
    /// <param name="FileInfo">FileInfo to check</param>
    /// <returns>True if file is a supported archive, False otherwise</returns>
    public static bool IsArchive(this FileInfo fileInfo)
    {
        return FileExtensionsArchives.Any(ext =>
            fileInfo.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }
}