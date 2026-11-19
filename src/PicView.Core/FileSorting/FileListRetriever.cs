using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using ZLinq;

namespace PicView.Core.FileSorting;

public static class FileListRetriever
{
    /// <summary>
    /// The supported files of the folder of <paramref name="fileInfo"/> (or of that folder when it is one), and of its
    /// subfolders when the app includes them, in <paramref name="ordering"/>.
    /// </summary>
    public static List<FileInfo> RetrieveFiles(FileInfo fileInfo, FileOrdering ordering)
    {
        var directoryPath = fileInfo switch
        {
            null => null,
            { Attributes: var attr } when attr.HasFlag(FileAttributes.Directory) => fileInfo.FullName,
            _ => fileInfo.DirectoryName
        };

        if (string.IsNullOrEmpty(directoryPath))
        {
            return [];
        }

        try
        {
            var directory = new DirectoryInfo(directoryPath);
            var files = Settings.Sorting.IncludeSubDirectories
                ? directory.DescendantsAndSelf().OfType<FileInfo>().Where(x => x.IsSupported()).ToList()
                : directory.ChildrenAndSelf().OfType<FileInfo>().Where(x => x.IsSupported()).ToList();
            return ordering.Sort(files);
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(FileListRetriever), nameof(RetrieveFiles), exception);
            return [];
        }
    }
}
