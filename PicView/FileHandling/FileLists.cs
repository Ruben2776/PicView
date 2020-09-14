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
        private static List<string> FileList(string path, SortFilesBy sortFilesBy)
        {
            if (!Directory.Exists(path)) { return null; }

            SearchOption searchOption;

            if (!Properties.Settings.Default.IncludeSubDirectories || !string.IsNullOrWhiteSpace(TempZipFile))
            {
                searchOption = SearchOption.TopDirectoryOnly;
            }
            else
            {
                searchOption = SearchOption.AllDirectories;
            }

            var items = Directory.EnumerateFiles(path, "*.*", searchOption)
                .AsParallel()
                .Where(file =>

                        // Standards
                        file.EndsWith("jpg", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("jpe", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("png", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("bmp", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("tif", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("tiff", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("gif", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("ico", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("jfif", StringComparison.OrdinalIgnoreCase)


                    // Photoshop
                    || file.EndsWith("psd", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("psb", StringComparison.OrdinalIgnoreCase)

                    // Web
                    || file.EndsWith("svg", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("webp", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("wbmp", StringComparison.OrdinalIgnoreCase)

                    // Pfim
                    || file.EndsWith("dds", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("tga", StringComparison.OrdinalIgnoreCase)

                    // Raw Camera
                    || file.EndsWith("3fr", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("arw", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("cr2", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("crw", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("dcr", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("dng", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("erf", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("kdc", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("mdc", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("mef", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("mos", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("mrw", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("nef", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("pgm", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("ppm", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("raf", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("raw", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("rw2", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("srf", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("x3f", StringComparison.OrdinalIgnoreCase)

                    // Obscure
                    || file.EndsWith("bpg", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("cut", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("dib", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("emf", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("exr", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("hdr", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("heic", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("pcx", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("wmf", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("wpg", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("xbm", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("xpm", StringComparison.OrdinalIgnoreCase)

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