using System.Globalization;
using System.IO;

namespace PicView.FileHandling
{
    internal static class SupportedFiles
    {
        /// <summary>
        /// Check if supported:
        /// Returns true if common files,
        /// False if uncommon,
        /// Null if unsupported
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        internal static bool? IsSupportedFile(string ext)
        {
            ext = Path.GetExtension(ext).ToLower(CultureInfo.CurrentCulture);
            return ext switch
            {
                // Standards
                ".jpg" or ".jpeg" or ".jpe" or ".png" or ".bmp" or ".tif" or ".tiff" or ".gif" or ".ico" or ".jfif" or ".ppm" or ".webp" or ".wbmp" or ".ppm" => true,

                // Non-standards

                // Photoshop
                ".psd" or ".psb" or 

                // Vector
                ".svg" or
                
                // Camera
                ".3fr" or ".arw" or ".cr2" or ".crw" or ".dcr" or ".dng" or ".erf" or ".kdc" or ".mdc" or ".mef" or ".mos" or ".mrw" or ".nef" or ".nrw" or ".orf" or ".pef"
                 or ".raf" or ".raw" or ".rw2" or ".srf" or ".x3f"

                // Others
                or ".pgm" or ".hdr" or ".cut" or ".exr" or ".dib" or ".heic" or ".emf" or ".wmf" or ".wpg" or ".pcx" or ".xbm" or ".xpm"

                => false,
                // Non supported
                _ => null,
            };
        }

        /// <summary>
        /// Check if supported:
        /// Returns true if common files,
        /// False if uncommon,
        /// Null if unsupported
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        internal static bool IsSupportedArchives(string ext)
        {
            ext = Path.GetExtension(ext).ToLower(CultureInfo.CurrentCulture);
            return ext switch
            {
                // Archives
                ".zip" or ".7zip" or ".7z" or ".rar" or ".cbr" or ".cb7" or ".cbt" or ".cbz" or ".xz" or ".bzip2" or ".gzip" or ".tar" or ".wim" or ".iso" or ".cab" => true,
                // Non supported
                _ => false,
            };
        }
    }
}