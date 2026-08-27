using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Gallery;

public partial class GalleryItem : NavigateAbleItem
{
    public GalleryItem()
    {
        InitializeComponent();
        GalleryContextMenu.Opened += GalleryContextMenuOnOpened;
        GalleryContextMenu.Closed += GalleryContextMenuOnClosed;
    }

    private IDisposable? _imageSubscription;

    public override void SetViewportVisibility(bool isVisible)
    {
        base.SetViewportVisibility(isVisible);
        if (isVisible)
        {
            LoadImage();
        }
        else
        {
            UnloadImage();
        }
    }

    private void LoadImage()
    {
        if (_imageSubscription is not null || DataContext is not GalleryItemViewModel vm)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        
        _imageSubscription = Observable.EveryValueChanged(vm.Image, img => img.Value, mainWindow.FrameProvider)
            .SubscribeAwait(async (_, ct) =>
            {
                var thumb = await vm.ThumbnailLoaderFunc(ct).ConfigureAwait(false);
                vm.Image.Value = thumb;
            }, DebugHelper.LogError(nameof(GalleryItem), nameof(LoadImage)));
    }

    private void UnloadImage()
    {
        _imageSubscription?.Dispose();
        _imageSubscription = null;
        GalleryImage.Source = null;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_imageSubscription is null)
        {
            return;
        }

        UnloadImage();
        LoadImage();
    }

    private void GalleryContextMenuOnClosed(object? sender, RoutedEventArgs e)
    {
        SetContextMenuOpen(false);
    }

    private void GalleryContextMenuOnOpened(object? sender, RoutedEventArgs e)
    {
        SetContextMenuOpen(true);
    }

    private void Flyout_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control ctl)
        {
            return;
        }

        FlyoutBase.ShowAttachedFlyout(ctl);
    }
    
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var viewer = this.FindLogicalAncestorOfType<NavigateAbleItemsViewer>();
        if (viewer is null)
        {
            return;
        }

        var container = this.FindLogicalAncestorOfType<ContentPresenter>();
        if (container is null)
        {
            return;
        }

        var index = viewer.IndexFromContainer(container);
        if (index == -1)
        {
            return;
        }

        viewer.SelectedItemIndex = index;

        if (viewer.DataContext is TabViewModel tab)
        {
            tab.Gallery.OpenSelectedItemCommand.Execute(index);
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        GalleryContextMenu.Opened -= GalleryContextMenuOnOpened;
        GalleryContextMenu.Closed -= GalleryContextMenuOnClosed;
    }

    private void OpenWith_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation.CurrentValue;
        _ = FileManager.OpenWith(fileName).ConfigureAwait(false);
    }

    private void LocateOnDisk_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation.CurrentValue;
        _ = FileManager.LocateOnDisk(fileName).ConfigureAwait(false);
    }

    private void WallpaperFilled_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation.CurrentValue;
        _ = core.PlatformService.SetAsWallpaper(fileName, 4);
    }

    private void WallpaperFitted_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation.CurrentValue;
        _ = core.PlatformService.SetAsWallpaper(fileName, 3);
    }

    private void WallpaperStretched_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation.CurrentValue;
        _ = core.PlatformService.SetAsWallpaper(fileName, 2);
    }

    private void WallpaperCentered_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation.CurrentValue;
        _ = core.PlatformService.SetAsWallpaper(fileName, 1);
    }

    private void WallpaperTiled_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation.CurrentValue;
        _ = core.PlatformService.SetAsWallpaper(fileName, 0);
    }

    private void CopyFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GalleryItemViewModel item || TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        var fileName = item.FileLocation.CurrentValue;
        _ = ClipboardFileOperations.CopyFileToClipboard(fileName, mainWindow);
    }

    private void CopyImage_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GalleryItemViewModel item)
        {
            return;
        }

        if (item.Image.CurrentValue is Bitmap image)
        {
            _ = ClipboardImageOperations.CopyImageToClipboard(image);
        }
    }

    private void CopyBase64_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GalleryItemViewModel item || TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        _ = ClipboardImageOperations.CopyBase64ToClipboard(item.FileLocation.CurrentValue, mainWindow);
    }

    private void DuplicateFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item
            || TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        var fileName = item.FileLocation.CurrentValue;
        ClipboardFileOperations.Duplicate(fileName, core.MainWindows.ActiveWindow.CurrentValue, mainWindow).ConfigureAwait(false);
    }

    private void DeleteFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation.CurrentValue;
        _ = core.PlatformService.DeleteFile(fileName, recycle: true);
    }
}