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
                    return true;

                // Non-standards

                // Photoshop
                case ".psd":
                case ".psb":

                // Web
                case ".svg":
                case ".webp":
                case ".ktx":
                case ".wbmp":

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
        internal static bool? IsSupportedFileWithArchives(string ext)
        {
            ext = Path.GetExtension(ext).ToLower(CultureInfo.CurrentCulture);
            switch (ext)
            {
                // Archives
                case ".zip":
                case ".7zip":
                case ".7z":
                case ".rar":
                case ".cbr":
                case ".cb7":
                case ".cbt":
                case ".cbz":
                case ".xz":
                case ".bzip2":
                case ".gzip":
                case ".tar":
                case ".wim":
                case ".iso":
                case ".cab":

                // Non-standards

                // Photoshop
                case ".psd":
                case ".psb":

                // Web
                case ".svg":
                case ".webp":
                case ".ktx":
                case ".wbmp":

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
                    return true;

                // Non supported
                default:
                    return null;
            }
        }
    }
}