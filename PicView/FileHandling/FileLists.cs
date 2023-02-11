using PicView.ChangeImage;
using PicView.Properties;
using PicView.SystemIntegration;
using System;
using System.Collections.Generic;
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
        internal static List<string>? FileList(FileInfo fileInfo) => Settings.Default.SortPreference switch
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
            if (fileInfo == null) { return null; }

            var isDirectory = FileFunctions.CheckIfDirectoryOrFile(fileInfo.FullName);
            if (!isDirectory.HasValue) { return null; }

            var directory = isDirectory.Value ? fileInfo.FullName : fileInfo.DirectoryName;

            var searchOption = Settings.Default.IncludeSubDirectories && string.IsNullOrWhiteSpace(TempZipFile)
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var items = Directory.EnumerateFiles(directory, "*.*", searchOption)
                .AsParallel()
                .Where(f => SupportedFiles.FileExtensions.Any(ext => Path.GetExtension(f).Equals(ext, StringComparison.OrdinalIgnoreCase)));

            switch (sortFilesBy)
            {
                default:
                case SortFilesBy.Name: // Alphanumeric sort
                    var list = items.ToList();
                    if (Settings.Default.Ascending)
                    {
                        list.Sort((x, y) => NativeMethods.StrCmpLogicalW(x, y));
                    }
                    else
                    {
                        list.Sort((x, y) => NativeMethods.StrCmpLogicalW(y, x));
                    }
                    return list;

                case SortFilesBy.FileSize:
                    return Settings.Default.Ascending ?
                        items.OrderBy(f => new FileInfo(f).Length).ToList()
                        : items.OrderByDescending(f => new FileInfo(f).Length).ToList();

                case SortFilesBy.Extension:
                    return Settings.Default.Ascending ?
                        items.OrderBy(f => new FileInfo(f).Extension).ToList()
                        : items.OrderByDescending(f => new FileInfo(f).Extension).ToList();

                case SortFilesBy.Creationtime:
                    return Settings.Default.Ascending ?
                        items.OrderBy(f => new FileInfo(f).CreationTime).ToList()
                        : items.OrderByDescending(f => new FileInfo(f).CreationTime).ToList();

                case SortFilesBy.Lastaccesstime:
                    return Settings.Default.Ascending ?
                        items.OrderBy(f => new FileInfo(f).LastAccessTime).ToList()
                        : items.OrderByDescending(f => new FileInfo(f).LastAccessTime).ToList();

                case SortFilesBy.Lastwritetime:
                    return Settings.Default.Ascending
                        ? items.OrderBy(f => new FileInfo(f).LastWriteTime).ToList()
                        : items.OrderByDescending(f => new FileInfo(f).LastWriteTime).ToList();

                case SortFilesBy.Random:
                    return items.OrderBy(f => Guid.NewGuid()).ToList();
            }
        }

        /// <summary>
        /// Gets values and extracts archives
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        internal static Task RetrieveFilelistAsync(FileInfo? fileInfo) => Task.Run(async () =>
        {
            if (fileInfo is null)
            {
                await ErrorHandling.ReloadAsync(true).ConfigureAwait(false);
                return;
            }
            // Check if to load from archive
            if (SupportedFiles.IsArchive(fileInfo.Extension))
            {
                if (Extract(fileInfo.FullName)) { return; }
                if (ErrorHandling.CheckOutOfRange() == false)
                {
                    Navigation.BackupPath = Navigation.Pics[Navigation.FolderIndex];
                }

                await ErrorHandling.ReloadAsync(true).ConfigureAwait(false);
                return;
            }

            await Task.Run(() => {
                // Set files to Pics and get index
                Navigation.Pics = FileList(fileInfo);
                if (Navigation.Pics == null)
                {
                    _= ErrorHandling.ReloadAsync(true).ConfigureAwait(false);
                }
            });


        });
    }
}