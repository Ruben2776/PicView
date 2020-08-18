using PicView.ChangeImage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static PicView.FileHandling.ArchiveExtraction;

namespace PicView.FileHandling
{
    internal static class FileLists
    {
        internal enum SortFilesBy
        {
            Name,
            FileSize,
            Creationtime,
            Extension,
            Lastaccesstime,
            Lastwritetime,
            Random
        }

        /// <summary>
        /// Sort and return list of supported files
        /// </summary>
        internal static List<string> FileList(string path) => Properties.Settings.Default.SortPreference switch
        {
            0 => FileList(path, SortFilesBy.Name),
            1 => FileList(path, SortFilesBy.FileSize),
            2 => FileList(path, SortFilesBy.Creationtime),
            3 => FileList(path, SortFilesBy.Extension),
            4 => FileList(path, SortFilesBy.Lastaccesstime),
            5 => FileList(path, SortFilesBy.Lastwritetime),
            6 => FileList(path, SortFilesBy.Random),
            _ => FileList(path, SortFilesBy.Name),
        };

        /// <summary>
        /// Sort and return list of supported files
        /// </summary>
        internal static List<string> FileList(string path, SortFilesBy sortFilesBy)
        {
            if (!Directory.Exists(path))
            {
                return null;
            }

            var items = Directory.EnumerateFiles(path, "*.*",
                Properties.Settings.Default.IncludeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .AsParallel()
                .Where(file =>

                           // Standards
                           file.ToLower(CultureInfo.CurrentCulture).EndsWith("jpg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("jpeg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("jpe", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("png", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("bmp", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("tif", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("tiff", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("gif", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("ico", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("wdp", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("jfif", StringComparison.OrdinalIgnoreCase)

                        // Photoshop
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("psd", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("psb", StringComparison.OrdinalIgnoreCase)

                        // Web
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("svg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("webp", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("ktx", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("wbmp", StringComparison.OrdinalIgnoreCase)

                        // Raw Camera
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("3fr", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("arw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("cr2", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("crw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dcr", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dng", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("erf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("kdc", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("mdc", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("mef", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("mos", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("mrw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("nef", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pgm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("ppm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("raf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("raw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("rw2", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("srf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("x3f", StringComparison.OrdinalIgnoreCase)

                        // Obscure
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("tga", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("bpg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("cur", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("cut", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dib", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("emf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("exif", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("exr", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("hdr", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("heic", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pcx", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("wmf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("wpg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("xbm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("xpm", StringComparison.OrdinalIgnoreCase)

                    );

            switch (sortFilesBy)
            {
                default:  // Alphanumeric sort
                case SortFilesBy.Name:
                    var list = items.ToList();
                    list.Sort((x, y) => { return SystemIntegration.NativeMethods.StrCmpLogicalW(x, y); });
                    return list;

                case SortFilesBy.FileSize:
                    return items.OrderBy(f => new FileInfo(f).Length).ToList();

                case SortFilesBy.Extension:
                    return items.OrderBy(f => new FileInfo(f).Extension).ToList();

                case SortFilesBy.Creationtime:
                    return items.OrderBy(f => new FileInfo(f).CreationTime).ToList();

                case SortFilesBy.Lastaccesstime:
                    return items.OrderBy(f => new FileInfo(f).LastAccessTime).ToList();

                case SortFilesBy.Lastwritetime:
                    return items.OrderBy(f => new FileInfo(f).LastWriteTime).ToList();

                case SortFilesBy.Random:
                    return items.OrderBy(f => Guid.NewGuid()).ToList();
            }
        }

        /// <summary>
        /// Gets values and extracts archives
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static Task GetValues(string path) => Task.Run(() =>
        {
            var extension = Path.GetExtension(path);
            extension = extension.ToLower(CultureInfo.CurrentCulture);

            // Check if to load from archive
            if (SupportedFiles.IsSupportedArchives(extension))
            {
                if (!Extract(path))
                {
                    Error_Handling.Reload(true);
                    return;
                }
            }

            // Set files to Pics and get index
            Navigation.Pics = FileList(Path.GetDirectoryName(path));
            if (Navigation.Pics == null)
            {
                return;
            }
#if DEBUG
            Trace.WriteLine("Getvalues completed ");
#endif
        });
    }
}