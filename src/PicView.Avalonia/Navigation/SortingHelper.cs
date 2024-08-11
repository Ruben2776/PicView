using PicView.Avalonia.Gallery;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.Navigation;

public static class SortingHelper
{
    public static List<string> SortIEnumerable(IEnumerable<string> files, IPlatformSpecificService? platformService)
    {
        var sortFilesBy = FileListHelper.GetSortOrder();

        switch (sortFilesBy)
        {
            default:
            case FileListHelper.SortFilesBy.Name: // Alphanumeric sort
                var list = files.ToList();
                if (SettingsHelper.Settings.Sorting.Ascending)
                {
                    list.Sort(platformService.CompareStrings);
                }
                else
                {
                    list.Sort((x, y) => platformService.CompareStrings(y, x));
                }

                return list;

            case FileListHelper.SortFilesBy.FileSize: // Sort by file size
                var fileInfoList = files.Select(f => new FileInfo(f)).ToList();
                var sortedBySize = SettingsHelper.Settings.Sorting.Ascending
                    ? fileInfoList.OrderBy(f => f.Length)
                    : fileInfoList.OrderByDescending(f => f.Length);
                return sortedBySize.Select(f => f.FullName).ToList();

            case FileListHelper.SortFilesBy.Extension: // Sort by file extension
                var sortedByExtension = SettingsHelper.Settings.Sorting.Ascending
                    ? files.OrderBy(Path.GetExtension)
                    : files.OrderByDescending(Path.GetExtension);
                return sortedByExtension.ToList();

            case FileListHelper.SortFilesBy.CreationTime: // Sort by file creation time
                var sortedByCreationTime = SettingsHelper.Settings.Sorting.Ascending
                    ? files.OrderBy(f => new FileInfo(f).CreationTime)
                    : files.OrderByDescending(f => new FileInfo(f).CreationTime);
                return sortedByCreationTime.ToList();

            case FileListHelper.SortFilesBy.LastAccessTime: // Sort by file last access time
                var sortedByLastAccessTime = SettingsHelper.Settings.Sorting.Ascending
                    ? files.OrderBy(f => new FileInfo(f).LastAccessTime)
                    : files.OrderByDescending(f => new FileInfo(f).LastAccessTime);
                return sortedByLastAccessTime.ToList();

            case FileListHelper.SortFilesBy.LastWriteTime: // Sort by file last write time
                var sortedByLastWriteTime = SettingsHelper.Settings.Sorting.Ascending
                    ? files.OrderBy(f => new FileInfo(f).LastWriteTime)
                    : files.OrderByDescending(f => new FileInfo(f).LastWriteTime);
                return sortedByLastWriteTime.ToList();

            case FileListHelper.SortFilesBy.Random: // Sort files randomly
                return files.OrderBy(f => Guid.NewGuid()).ToList();
        }
    }
    
    public static async Task UpdateFileList(IPlatformSpecificService? platformSpecificService, MainViewModel vm, FileListHelper.SortFilesBy sortFilesBy)
    {
        SettingsHelper.Settings.Sorting.SortPreference = (int)sortFilesBy;
        if (!NavigationHelper.CanNavigate(vm))
        {
            return;
        }
        var files = await Task.FromResult(platformSpecificService.GetFiles(vm.FileInfo)).ConfigureAwait(false);
        if (files is { Count: > 0 })
        {
            vm.ImageIterator.UpdateFileListAndIndex(files, files.IndexOf(vm.FileInfo.FullName));
            SetTitleHelper.SetTitle(vm);
        }
        else return;

        await GalleryFunctions.SortGalleryItems(files, vm);
    }

    public static async Task UpdateFileList(IPlatformSpecificService? platformSpecificService, MainViewModel vm, bool ascending)
    {
        SettingsHelper.Settings.Sorting.Ascending = ascending;
        if (!NavigationHelper.CanNavigate(vm))
        {
            return;
        }
        var files = await Task.FromResult(platformSpecificService.GetFiles(vm.FileInfo)).ConfigureAwait(false);
        if (files is { Count: > 0 })
        {
            vm.ImageIterator.UpdateFileListAndIndex(files, files.IndexOf(vm.FileInfo.FullName));
            SetTitleHelper.SetTitle(vm);
        }
        else return;

        await GalleryFunctions.SortGalleryItems(files, vm);
    }
}