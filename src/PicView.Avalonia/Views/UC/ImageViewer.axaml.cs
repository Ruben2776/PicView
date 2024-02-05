using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Navigation;
using System.Runtime.InteropServices;
using Avalonia.Platform.Storage;
using PicView.Avalonia.DragAndDrop;

namespace PicView.Avalonia.Views.UC;

public partial class ImageViewer : UserControl
{
    public ImageViewer()
    {
        InitializeComponent();
        PointerWheelChanged += async (_, e) => await Main_OnPointerWheelChanged(e);
        // TODO add visual feedback for drag and drop
        //AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        var data = e.Data.GetFiles();
        if (data == null)
        {
            // TODO Handle URL and folder drops
            return;
        }

        var storageItems = data as IStorageItem[] ?? data.ToArray();
        var firstFile = storageItems.FirstOrDefault();
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? firstFile.Path.AbsolutePath : firstFile.Path.LocalPath;
        _ = vm.LoadPicFromString(path).ConfigureAwait(false);
        foreach (var file in storageItems.Skip(1))
        {
            // TODO Open each file in a new window if the setting to open in the same window is false
        }
    }

    private async Task Main_OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel)
            return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        if (e.Delta.Y < 0)
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                await mainViewModel.LoadNextPic(NavigateTo.Next);
            }
            else
            {
                await mainViewModel.LoadNextPic(NavigateTo.Previous);
            }
        }
        else
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                await mainViewModel.LoadNextPic(NavigateTo.Previous);
            }
            else
            {
                await mainViewModel.LoadNextPic(NavigateTo.Next);
            }
        }
    }
}