using System;
using System.IO;

namespace PicView.Data.IO;

public static class SupportedFiles
    {
        public static bool IsSupportedFile(string file)
        {
            var supported = CheckIsSupportedFile(file);
            return supported is not null;
        }
        
        /// <summary>
        /// Check if supported:
        /// Returns true if common files,
        /// False if uncommon,
        /// Null if unsupported
        /// </summary>
        /// <param name="file">File extension</param>
        /// <returns></returns>
        public static bool? CheckIsSupportedFile(string file)
        {
            var ext = Path.GetExtension(file);
            switch (ext)
            {
                // Standards
                case { } when ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".jpe", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".png", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".gif", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".jfif", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".ico", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".webp", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".wbmp", StringComparison.OrdinalIgnoreCase):
                    return true;

                // Photoshop
                case { } when ext.Equals(".psd", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".psb", StringComparison.OrdinalIgnoreCase):

                // additional
                case { } when ext.Equals(".tif", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".tiff", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".dds", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".tga", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".heic", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".heif", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".hdr", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".xcf", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".jxl", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".jp2", StringComparison.OrdinalIgnoreCase):

                // Base64
                case { } when ext.Equals(".b64", StringComparison.OrdinalIgnoreCase):

                // Vector
                case { } when ext.Equals(".svg", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".svgz", StringComparison.OrdinalIgnoreCase):


                // Camera
                case { } when ext.Equals(".3fr", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".arw", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".cr2", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".cr3", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".crw", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".dcr", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".dng", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".erf", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".kdc", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".mdc", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".mef", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".mos", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".mrw", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".nef", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".nrw", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".orf", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".pef", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".raf", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".raw", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".rw2", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".srf", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".x3f", StringComparison.OrdinalIgnoreCase):

                // Others
                case { } when ext.Equals(".pgm", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".ppm", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".cut", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".exr", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".dib", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".emf", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".wmf", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".wpg", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".pcx", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".xbm", StringComparison.OrdinalIgnoreCase):
                case { } when ext.Equals(".xpm", StringComparison.OrdinalIgnoreCase):

                    return false;

                default: return null;
            }
        }

        /// <summary>
        /// Returns true if supported archive
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool IsSupportedArchives(string file)
        {
            return IsSupportedArchives(new FileInfo(file));
        }

        /// <summary>
        /// Returns true if supported archive
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static bool IsSupportedArchives(FileInfo fileInfo)
        {
            switch (fileInfo.Extension)
            {
                // Standards
                case { } when fileInfo.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".7zip", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".7z", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".rar", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".cbr", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".cb7", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".cbt", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".xz", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".bzip2", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".gzip", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".tar", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".wim", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".iso", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".cab", StringComparison.OrdinalIgnoreCase):
                    return true;

                default: return false;
            }
        }
    }