using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static PicView.ArchiveExtraction;
using static PicView.Fields;

namespace PicView
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
        internal static List<string> FileList(string path)
        {
            switch (Properties.Settings.Default.SortPreference)
            {
                case 0:
                    return FileList(path, SortFilesBy.Name);
                case 1:
                    return FileList(path, SortFilesBy.FileSize);
                case 2:
                    return FileList(path, SortFilesBy.Creationtime);
                case 3:
                    return FileList(path, SortFilesBy.Extension);
                case 4:
                    return FileList(path, SortFilesBy.Lastaccesstime);
                case 5:
                    return FileList(path, SortFilesBy.Lastwritetime);
                case 6:
                    return FileList(path, SortFilesBy.Random);
                default:
                    return FileList(path, SortFilesBy.Name);
            }
        }

        /// <summary>
        /// Sort and return list of supported files
        /// </summary>
        internal static List<string> FileList(string path, SortFilesBy sortFilesBy)
        {
            /// TODO need to get a recursive folder user configurable option for this added
            /// Need to look through file list to check if they all work,
            /// or some alternative way, mayhaps..?

            if (!Directory.Exists(path))
            {
                return null;
            }

            var items = Directory.EnumerateFiles(path)
                .AsParallel()
                .Where(file =>
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
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("svg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("psd", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("psb", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("orf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("cr2", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("crw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dng", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("raf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("raw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("mrw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("nef", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("x3f", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("arw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("webp", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("aai", StringComparison.OrdinalIgnoreCase)
                        //|| file.ToLower(CultureInfo.CurrentCulture).EndsWith("ai", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("art", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("bgra", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("bgro", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("canvas", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("cin", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("cmyk", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("cmyka", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("cur", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("cut", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dcm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dcr", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dcx", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dds", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dfont", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dlib", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dpx", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dxt1", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("dxt5", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("emf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("epi", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("eps", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("ept", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("ept2", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("ept3", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("exr", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("fax", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("fits", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("flif", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("g3", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("g4", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("gif87", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("gradient", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("gray", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("group4", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("hald", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("hdr", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("hrz", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("icb", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("icon", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("ipl", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("jc2", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("j2k", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("jng", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("jnx", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("jpm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("jps", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("jpt", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("kdc", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("label", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("map", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("nrw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("otb", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("otf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pbm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pcd", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pcds", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pcl", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pct", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pcx", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pfa", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pfb", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pfm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("picon", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pict", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pix", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pjpeg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("png00", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("png24", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("png32", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("png48", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("png64", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("png8", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("pnm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("ppm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("ps", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("radialgradient", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("ras", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("rgb", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("rgba", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("rgbo", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("rla", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("rle", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("scr", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("screenshot", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("sgi", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("srf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("sun", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("svgz", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("tiff64", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("ttf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("vda", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("vicar", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("vid", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("viff", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("vst", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("vmf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("wpg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("xbm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("xcf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower(CultureInfo.CurrentCulture).EndsWith("yuv", StringComparison.OrdinalIgnoreCase)
                    );

            switch (sortFilesBy)
            {
                // Alphanumeric sort
                case SortFilesBy.Name:
                    var list = items.ToList();
                    list.Sort((x, y) => { return NativeMethods.StrCmpLogicalW(x, y); });
                    return list;
                case SortFilesBy.FileSize:
                    items = items.OrderBy(f => new FileInfo(f).Length);
                    break;
                case SortFilesBy.Extension:
                    items = items.OrderBy(f => new FileInfo(f).Extension);
                    break;
                case SortFilesBy.Creationtime:
                    items = items.OrderBy(f => new FileInfo(f).CreationTime);
                    break;
                case SortFilesBy.Lastaccesstime:
                    items = items.OrderBy(f => new FileInfo(f).LastAccessTime);
                    break;
                case SortFilesBy.Lastwritetime:
                    items = items.OrderBy(f => new FileInfo(f).LastWriteTime);
                    break;
                case SortFilesBy.Random:
                    items = items.OrderBy(f => Guid.NewGuid());
                    break;
            }
            return items.ToList();
        }

        /// <summary>
        /// Gets values and extracts archives
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static Task GetValues(string path)
        {
            return Task.Run(() =>
            {
                // Determine if archive to be extracted or not
                bool zipped = false;
                var extension = Path.GetExtension(path);
                extension = extension.ToLower(CultureInfo.CurrentCulture);
                switch (extension)
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
                        zipped = Extract(path);
                        if (!zipped)
                        {
                            Pics = new List<string>();
                            FolderIndex = -1;
                        }
                        return;
                }

                // Set files to Pics and get index
                Pics = FileList(Path.GetDirectoryName(path));
                if (Pics == null)
                {
                    return;
                }

                FolderIndex = Pics.IndexOf(path);
            });
        }
    }
}
