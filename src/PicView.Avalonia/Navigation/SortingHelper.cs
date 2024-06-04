using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Services;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Avalonia.Views.UC;
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

        await SortGalleryItems(files, vm);
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

        await SortGalleryItems(files, vm);
    }

    public static async Task SortGalleryItems(List<string> files, MainViewModel vm)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var mainView =
            await Dispatcher.UIThread.InvokeAsync(() => desktop.MainWindow.GetControl<MainView>("MainView"));

        var galleryListBox = mainView.GalleryView.GalleryListBox;
        if (galleryListBox == null) return;
        var initialDirectory = Path.GetDirectoryName(vm.FileInfo.FullName);
        try
        {
            while (GalleryLoad.IsLoading)
            {
                await Task.Delay(200, cancellationTokenSource.Token);
                if (initialDirectory == Path.GetDirectoryName(files[0]))
                {
                    continue;
                }

                // Directory changed, cancel the operation
                await cancellationTokenSource.CancelAsync();
                return;
            }

            // Create a dictionary to quickly lookup GalleryItems by their file location
            var galleryItemLookup = galleryListBox.Items.OfType<GalleryItem>()
                .ToDictionary(galleryItem => galleryItem.FileLocation.Text);

            // Clear the ListBox items
            await Dispatcher.UIThread.InvokeAsync(() => { galleryListBox.Items.Clear(); });

            // Create a sorted list of GalleryItems based on the order of file paths
            var sortedGalleryItems = files
                .Where(file => galleryItemLookup.ContainsKey(file))
                .Select(file => galleryItemLookup[file])
                .ToList();

            // Add the sorted GalleryItems back to the ListBox
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                for (var i = 0; i < sortedGalleryItems.Count; i++)
                {
                    var galleryItem = sortedGalleryItems[i];
                    galleryListBox.Items.Add(galleryItem);
                    if (i != vm.ImageIterator.Index)
                    {
                        continue;
                    }
                    
                    if (initialDirectory != Path.GetDirectoryName(files[0]))
                    {
                        cancellationTokenSource.Cancel();
                        return;
                    }

                    // Set and scroll to the selected item
                    vm.SelectedGalleryItemIndex = i;
                    galleryListBox.SelectedItem = galleryItem;
                }
            });
        }
        catch (TaskCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                galleryListBox.Items.Clear();
            });
        }
        catch (Exception e)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                galleryListBox.Items.Clear();
            });
            vm.ToolTipUIText = e.Message;
#if DEBUG
            Console.WriteLine(e);
#endif
        }
    }
}