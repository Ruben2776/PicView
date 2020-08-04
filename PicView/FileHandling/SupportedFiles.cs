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
            switch (ext)
            {
                // Standards
                case ".jpg":
                case ".jpeg":
                case ".jpe":
                case ".png":
                case ".bmp":
                case ".tif":
                case ".tiff":
                case ".gif":
                case ".ico":
                case ".wdp":
                case ".jfif":
                case ".ktx":
                case ".webp":
                case ".wbmp":
                    return true;

                // Non-standards

                // Photoshop
                case ".psd":
                case ".psb":

                // Web
                case ".svg":

                // Raw Camera
                case ".3fr":
                case ".arw":
                case ".cr2":
                case ".crw":
                case ".dcr":
                case ".dng":
                case ".erf":
                case ".kdc":
                case ".mdc":
                case ".mef":
                case ".mos":
                case ".mrw":
                case ".nef":
                case ".nrw":
                case ".orf":
                case ".pef":
                case ".pgm":
                case ".ppm":
                case ".raf":
                case ".raw":
                case ".rw2":
                case ".srf":
                case ".x3f":

                // Obscure

                case ".bpg": // untested
                case ".cur":
                case ".cut": // untested
                case ".dib": // untested
                case ".emf": // untested
                case ".exif": // untested
                case ".exr":
                case ".hdr":
                case ".heic":
                case ".pcx":
                case ".tga":
                case ".wmf": // untested
                case ".wpg":
                case ".xbm":
                case ".xpm":

                    return false;

                // Non supported
                default:
                    return null;
            }
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