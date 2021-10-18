using PicView.ChangeImage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        internal static List<string>? FileList()
        {
            if (Properties.Settings.Default.IncludeSubDirectories)
            {
                var args = Environment.GetCommandLineArgs();

                if (args.Length > 1)
                {
                    var originFolder = Path.GetDirectoryName(Path.GetDirectoryName(args[1]));
                    if (string.IsNullOrWhiteSpace(originFolder) == false || Directory.Exists(originFolder) == false)
                    {
                        return null;
                    }
                    var currentFolder = Path.GetDirectoryName(Path.GetDirectoryName(Navigation.Pics?[Navigation.FolderIndex]));
                    if (string.IsNullOrWhiteSpace(currentFolder) == false || Directory.Exists(currentFolder) == false)
                    {
                        return null;
                    }
                    if (originFolder != currentFolder)
                    {
                        return FileList(new FileInfo(currentFolder));
                    }
                    return FileList(new FileInfo(originFolder));
                }
                return FileList(new FileInfo(Path.GetDirectoryName(Navigation.Pics?[Navigation.FolderIndex])));
            }
            return FileList(new FileInfo(Path.GetDirectoryName(Navigation.Pics?[Navigation.FolderIndex])));
        }

        /// <summary>
        /// Sort and return list of supported files
        /// </summary>
        internal static List<string>? FileList(FileInfo fileInfo) => Properties.Settings.Default.SortPreference switch
        {
            0 => FileList(fileInfo, SortFilesBy.Name),
            1 => FileList(fileInfo, SortFilesBy.FileSize),
            2 => FileList(fileInfo, SortFilesBy.Creationtime),
            3 => FileList(fileInfo, SortFilesBy.Extension),
            4 => FileList(fileInfo, SortFilesBy.Lastaccesstime),
            5 => FileList(fileInfo, SortFilesBy.Lastwritetime),
            6 => FileList(fileInfo, SortFilesBy.Random),
            _ => FileList(fileInfo, SortFilesBy.Name),
        };

        /// <summary>
        /// Sort and return list of supported files
        /// </summary>
        private static List<string>? FileList(FileInfo fileInfo, SortFilesBy sortFilesBy)
        {
            if ((fileInfo.Directory.Exists) == false) { return null; }

            SearchOption searchOption;

            if (!Properties.Settings.Default.IncludeSubDirectories || !string.IsNullOrWhiteSpace(TempZipFile))
            {
                searchOption = SearchOption.TopDirectoryOnly;
            }
            else
            {
                searchOption = SearchOption.AllDirectories;
            }

            var items = Directory.EnumerateFiles(fileInfo.DirectoryName, "*.*", searchOption)
                .AsParallel()
                .Where(file => SupportedFiles.IsSupportedExt(file)

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
            }
        }

        /// <summary>
        /// Gets values and extracts archives
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static Task GetValues(FileInfo fileInfo) => Task.Run(async () =>
        {
            if (fileInfo is null)
            {
                await Error_Handling.ReloadAsync(true).ConfigureAwait(false);
                return;
            }
            // Check if to load from archive
            if (SupportedFiles.IsSupportedArchives(fileInfo.FullName))
            {
                if (!Extract(fileInfo.FullName))
                {
                    if (Error_Handling.CheckOutOfRange() == false)
                    {
                        Navigation.BackupPath = Navigation.Pics[Navigation.FolderIndex];
                    }
                    
                    await Error_Handling.ReloadAsync(true).ConfigureAwait(false);
                }
                return;
            }

            // Set files to Pics and get index
            Navigation.Pics = FileList(fileInfo);
            if (Navigation.Pics == null)
            {
                await Error_Handling.ReloadAsync(true).ConfigureAwait(false);
                return;
            }
#if DEBUG
            Trace.WriteLine("Getvalues completed ");
#endif
        });
    }
}