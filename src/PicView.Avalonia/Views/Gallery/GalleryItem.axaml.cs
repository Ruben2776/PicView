using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Gallery;

public partial class GalleryItem : NavigateAbleItem
{
    private CancellationTokenSource? _loadCts;
    private DisposableBag _disposables;

    public GalleryItem()
    {
        InitializeComponent();
        GalleryContextMenu.Opened += GalleryContextMenuOnOpened;
        GalleryContextMenu.Closed += GalleryContextMenuOnClosed;
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        core.GallerySettings.DockedGalleryStretchMode.Subscribe(x =>
        {
            if (!core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.Gallery.IsGalleryDocked.CurrentValue)
            {
                return;
            }

            GalleryImage.Stretch = x switch
            {
                GalleryStretchMode.Uniform or GalleryStretchMode.Square => Stretch.Uniform,
                GalleryStretchMode.UniformToFill or GalleryStretchMode.FillSquare => Stretch.UniformToFill,
                _ => GalleryImage.Stretch
            };
        }, DebugHelper.LogError(nameof(GalleryItem), nameof(core.GallerySettings.DockedGalleryStretchMode)))
        .AddTo(ref _disposables);
        
        core.GallerySettings.ExpandedGalleryStretchMode.Subscribe(x =>
        {
            if (!core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.Gallery.IsGalleryExpanded.CurrentValue)
            {
                return;
            }

            GalleryImage.Stretch = x switch
            {
                GalleryStretchMode.Uniform or GalleryStretchMode.Square => Stretch.Uniform,
                GalleryStretchMode.UniformToFill or GalleryStretchMode.FillSquare => Stretch.UniformToFill,
                _ => GalleryImage.Stretch
            };
        }, DebugHelper.LogError(nameof(GalleryItem), nameof(core.GallerySettings.DockedGalleryStretchMode)))
        .AddTo(ref _disposables);
    }

    public async ValueTask LoadImage()
    {
        if (DataContext is not GalleryItemViewModel vm)
        {
            return;
        }

        // Fast-path: If the image is already loaded, skip
        if (vm.Image.Value is IImage)
        {
            return;
        }

        if (vm.ThumbnailLoaderFunc == null)
        {
            return;
        }

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        try
        {
            // Execute the lazy load directly
            var thumb = await vm.ThumbnailLoaderFunc(token).ConfigureAwait(false);
        
            // Ensure the user hasn't scrolled past this item while we were loading
            if (!token.IsCancellationRequested && thumb is not null)
            {
                vm.Image.Value = thumb;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected behavior when the user scrolls fast
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(GalleryItem), nameof(LoadImage), ex);
        }
    }

    public void UnloadImage()
    {
        _loadCts?.Cancel();
        if (DataContext is not GalleryItemViewModel vm)
        {
            return;
        }
        vm.Image.Value = null;
    }

    private void GalleryContextMenuOnClosed(object? sender, RoutedEventArgs e)
    {
        SetContextMenuOpen(false);
    }

    private void GalleryContextMenuOnOpened(object? sender, RoutedEventArgs e)
    {
        SetContextMenuOpen(true);
    }

    private void ShowGalleryItemSizeSlider(object? sender, PointerPressedEventArgs e)
    {
        GalleryContextMenu.Close();
        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        mainWindow.AddGalleryItemSizeSlider();
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

    #region Menu click events

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
    
    #endregion
    
    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        GalleryContextMenu.Opened -= GalleryContextMenuOnOpened;
        GalleryContextMenu.Closed -= GalleryContextMenuOnClosed;
        _disposables.Dispose();
    }
}