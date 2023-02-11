using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PicView.FileHandling
{
    internal static class SupportedFiles
    {
        internal static readonly List<string> FileExtensions = new List<string>
        {
            ".jpg", ".jpeg", ".jpe", ".png", ".bmp", ".gif", ".jfif", ".ico", ".webp", ".wbmp",
            ".psd", ".psb",
            ".tif", ".tiff", ".dds", ".tga", ".heic", ".heif", ".hdr", ".xcf", ".jxl", ".jp2",
            ".b64",
            ".svg", ".svgz",
            ".3fr", ".arw", ".cr2", ".cr3", ".crw", ".dcr", ".dng", ".erf", ".kdc", ".mdc", ".mef", ".mos", ".mrw",
            ".nef", ".nrw", ".orf", ".pef", ".raf", ".raw", ".rw2", ".srf", ".x3f",
            ".pgm", ".ppm", ".cut", ".exr", ".dib", ".emf", ".wmf", ".wpg", ".pcx", ".xbm", ".xpm",
        };

        internal static readonly List<string> FileExtensionsArchives = new List<string>
        {
            ".zip", ".7zip", ".7z", ".rar", ".cbr", ".cb7", ".cbt", ".cbz", ".xz", ".bzip2",
            ".gzip", ".tar", ".wim", ".iso", ".cab"
        };

        public static bool IsSupported(this string file)
        {
            return FileExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsSupported(this FileInfo fileInfo)
        {
            return FileExtensions.Any(ext => fileInfo.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsArchive(this string file)
        {
            return FileExtensionsArchives.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsArchive(this FileInfo fileInfo)
        {
            return FileExtensionsArchives.Any(ext => fileInfo.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase));
        }
    }
}