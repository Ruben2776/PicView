using System;
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
        internal static bool? IsSupportedFile(string file)
        {
            string ext = Path.GetExtension(file);
            switch (ext)
            {
                // Standards
                case string when ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".jpe", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".png", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".gif", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".jfif", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".ico", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".webp", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".wbmp", StringComparison.OrdinalIgnoreCase):
                    return true;

                // Photoshop
                case string when ext.Equals(".psd", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".psb", StringComparison.OrdinalIgnoreCase):

                // additional
                case string when ext.Equals(".tif", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".tiff", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".dds", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".tga", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".heic", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".hdr", StringComparison.OrdinalIgnoreCase):

                // Vector
                case string when ext.Equals(".svg", StringComparison.OrdinalIgnoreCase): // Maybe add svgz at some point
                case string when ext.Equals(".xcf", StringComparison.OrdinalIgnoreCase):

                // Camera
                case string when ext.Equals(".3fr", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".arw", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".cr2", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".crw", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".dcr", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".dng", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".erf", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".kdc", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".mdc", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".mef", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".mos", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".mrw", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".nef", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".nrw", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".orf", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".pef", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".raf", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".raw", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".rw2", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".srf", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".x3f", StringComparison.OrdinalIgnoreCase):

                // Others
                case string when ext.Equals(".pgm", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".ppm", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".cut", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".exr", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".dib", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".emf", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".wmf", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".wpg", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".pcx", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".xbm", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".xpm", StringComparison.OrdinalIgnoreCase):
                    return false;

                default: return null;
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
        internal static bool IsSupportedArchives(string file)
        {
            string ext = Path.GetExtension(file);
            switch (ext)
            {
                // Standards
                case string when ext.Equals(".zip", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".7zip", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".7z", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".rar", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".cbr", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".cb7", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".cbt", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".cbz", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".xz", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".bzip2", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".gzip", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".tar", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".wim", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".iso", StringComparison.OrdinalIgnoreCase):
                case string when ext.Equals(".cab", StringComparison.OrdinalIgnoreCase):
                    return true;

                default: return false;
            }
        }
    }
}