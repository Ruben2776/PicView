using DynamicData;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Services;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.Navigation;

public static class SortingHelper
{
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
            vm.ImageIterator.Pics = files;
            vm.ImageIterator.Index = files.IndexOf(vm.FileInfo.FullName);
            vm.SetTitle();
        }
        else return;

        if (vm.GalleryItems.Count > 0)
        {
            SortGalleryItems(files, vm, platformSpecificService);
        }
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
            vm.ImageIterator.Pics = files;
            vm.ImageIterator.Index = files.IndexOf(vm.FileInfo.FullName);
            vm.SetTitle();
        }
        else return;

        if (vm.GalleryItems.Count > 0)
        {
            SortGalleryItems(files, vm, platformSpecificService);
        }
    }

    public static void SortGalleryItems(List<string> files, MainViewModel vm, IPlatformSpecificService? platformSpecificService)
    {
        var sortedFiles = SortHelper.SortIEnumerable(files, platformSpecificService);
        var tempList = vm.GalleryItems;

        vm.GalleryItems.Clear();

        foreach (var file in sortedFiles)
        {
            vm.GalleryItems.Add(tempList.Where(x => x.FileName == file));
        }
    }
}