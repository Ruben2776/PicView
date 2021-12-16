using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PicView.Data.IO
{
    public static class FileListHelper
    {
        public static List<string>? GetFileList(FileInfo fileInfo, SortFilesBy sortFilesBy)
        {
            if (fileInfo is null) { return null; }
            
            var getAttributes = File.GetAttributes(fileInfo.FullName);
            var directory = getAttributes.HasFlag(FileAttributes.Directory) ? fileInfo.FullName : fileInfo.DirectoryName;

            var items = Directory.EnumerateFiles(directory, "*.*",
                    SearchOption.AllDirectories).AsParallel().Where(SupportedFiles.IsSupportedExt);

            switch (sortFilesBy)
            {
                default:
                case SortFilesBy.Name: // Alphanumeric sort
                    var list = items.ToList();
                    return list;

                case SortFilesBy.FileSize:
                    throw new NotImplementedException();
                case SortFilesBy.Extension:
                    throw new NotImplementedException();
                case SortFilesBy.Creationtime:
                    throw new NotImplementedException();
                case SortFilesBy.Lastaccesstime:
                    throw new NotImplementedException();
                case SortFilesBy.Lastwritetime:
                    throw new NotImplementedException();
                case SortFilesBy.Random:
                    return items.OrderBy(f => Guid.NewGuid()).ToList();
            }
        }
    }
}