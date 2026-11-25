using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.ImageHandling;
using PicView.Core.IPlatform;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.Gallery;

/// <summary>
/// The context menu of a Gallery Item. A gallery view has one, made on the first right-click, which the item that was
/// right-clicked last borrows: building a menu for every item was most of the cost of creating one.
/// </summary>
public partial class GalleryItemContextMenu : ContextMenu
{
    // The item the menu opens for, and the only one it is set on, so it keeps no item that scrolled away alive
    private GalleryItem? _item;

    public GalleryItemContextMenu()
    {
        InitializeComponent();
        Opened += (_, _) => _item?.SetContextMenuOpen(true);
        Closed += (_, _) => _item?.SetContextMenuOpen(false);
        if (!OperatingSystem.IsWindows())
        {
            LockscreenSeparator.IsVisible = false;
            LockscreenItem.IsVisible = false;
        }
        else
        {
            LockscreenItem.AddHandler(PointerPressedEvent, SetLockscreenAsync);
        }
        WallpaperFilled.AddHandler(PointerPressedEvent, SetAsWallpaperFilledAsync);
        WallpaperFitted.AddHandler(PointerPressedEvent, WallpaperFitted_OnClick);
        WallpaperStretched.AddHandler(PointerPressedEvent, WallpaperStretched_OnClick);
        WallpaperCentered.AddHandler(PointerPressedEvent, WallpaperCentered_OnClick);
        WallpaperTiled.AddHandler(PointerPressedEvent, WallpaperTiled_OnClick);
    }

    protected override Type StyleKeyOverride => typeof(ContextMenu);

    /// <summary>Makes the menu <paramref name="item"/>'s, taking it from the item that had it.</summary>
    internal void AttachTo(GalleryItem item)
    {
        if (ReferenceEquals(_item, item))
        {
            return;
        }

        Release();
        _item = item;
        item.ContextMenu = this;
    }

    /// <summary>Takes the menu from <paramref name="item"/>, closing it, if it is that item's.</summary>
    internal void DetachFrom(GalleryItem item)
    {
        if (ReferenceEquals(_item, item))
        {
            Release();
        }
    }

    private void Release()
    {
        if (_item is not { } item)
        {
            return;
        }

        Close();
        _item = null;
        item.SetContextMenuOpen(false);
        item.ContextMenu = null;
    }

    private GalleryItemViewModel? ItemViewModel => _item?.DataContext as GalleryItemViewModel;

    private MainWindow? MainWindow => _item is null ? null : TopLevel.GetTopLevel(_item) as MainWindow;

    private void ShowGalleryItemSizeSlider(object? sender, PointerPressedEventArgs e)
    {
        var mainWindow = MainWindow;
        Close();
        mainWindow?.AddGalleryItemSizeSlider();
    }

    private void OpenWith_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ItemViewModel is not { } item)
        {
            return;
        }

        _ = FileManager.OpenWith(item.FileLocation).ConfigureAwait(false);
    }

    private void LocateOnDisk_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ItemViewModel is not { } item)
        {
            return;
        }

        _ = FileManager.LocateOnDisk(item.FileLocation).ConfigureAwait(false);
    }

    // Through Platform Actions, which converts a file the OS cannot show, claims Busy and reports a failure
    private async ValueTask SetAsWallpaperAsync(WallpaperStyle style)
    {
        if (ItemViewModel is not { } item || MainWindow is not { DataContext: MainWindowViewModel vm })
        {
            return;
        }

        await PlatformActions.SetAsWallpaper(vm, item.FileLocation, style).ConfigureAwait(false);
    }

    private async ValueTask SetLockscreenAsync(object? sender, RoutedEventArgs e)
    {
        if (ItemViewModel is not { } item || MainWindow is not { DataContext: MainWindowViewModel vm })
        {
            return;
        }

        await PlatformActions.SetAsLockScreen(vm, item.FileLocation).ConfigureAwait(false);
    }

    private async ValueTask SetAsWallpaperFilledAsync(object? sender, RoutedEventArgs e) =>
        await SetAsWallpaperAsync(WallpaperStyle.Fill).ConfigureAwait(false);

    private async ValueTask WallpaperFitted_OnClick(object? sender, RoutedEventArgs e) =>
        await SetAsWallpaperAsync(WallpaperStyle.Fit).ConfigureAwait(false);

    private async ValueTask WallpaperStretched_OnClick(object? sender, RoutedEventArgs e) =>
        await SetAsWallpaperAsync(WallpaperStyle.Stretch).ConfigureAwait(false);

    private async ValueTask WallpaperCentered_OnClick(object? sender, RoutedEventArgs e) =>
        await SetAsWallpaperAsync(WallpaperStyle.Center).ConfigureAwait(false);

    private async ValueTask WallpaperTiled_OnClick(object? sender, RoutedEventArgs e) =>
        await SetAsWallpaperAsync(WallpaperStyle.Tile).ConfigureAwait(false);

    private void CopyFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ItemViewModel is not { } item || MainWindow is not { DataContext: MainWindowViewModel } mainWindow)
        {
            return;
        }

        _ = mainWindow.ClipboardCopyService.CopyFileAsync(item.FileLocation);
    }

    private void CopyImage_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || ItemViewModel is not { } item ||
            MainWindow is not { DataContext: MainWindowViewModel vm } mainWindow)
        {
            return;
        }

        // The image open in the viewer is copied as it is shown there, turned and flipped
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        if (item.FileLocation.Equals(tab.FileInfo.CurrentValue.FullName, StringComparison.Ordinal))
        {
            _ = mainWindow.ClipboardCopyService.CopyImageAsync(tab).ConfigureAwait(false);
            return;
        }

        _ = CopyOriginalImageAsync(new FileInfo(item.FileLocation), core.SharedCache,
            mainWindow.ClipboardCopyService);
    }

    private static async Task CopyOriginalImageAsync(FileInfo fileInfo, SharedImageCache cache,
        ClipboardCopyService? clipboardCopyService)
    {
        if (clipboardCopyService is null)
        {
            return;
        }

        // A decoded bitmap is never disposed here, since pasting in PicView can show that same bitmap in a tab
        var bitmap = await GetOriginalImage.GetBitmapAsync(fileInfo, cache).ConfigureAwait(false);
        await clipboardCopyService.CopyImageAsync(bitmap).ConfigureAwait(false);
    }

    private void CopyBase64_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ItemViewModel is not { } item || MainWindow is not { } mainWindow)
        {
            return;
        }

        _ = mainWindow.ClipboardCopyService.CopyBase64Async(item.FileLocation).ConfigureAwait(false);
    }

    private void DuplicateFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ItemViewModel is not { } item || MainWindow is not { DataContext: MainWindowViewModel vm } mainWindow)
        {
            return;
        }

        var currentActiveFilePath = vm.WindowTabs.ActiveTab.CurrentValue?.FileInfo?.CurrentValue.FullName;
        _ = FileManager.DuplicateFileAsync(item.FileLocation, currentActiveFilePath, vm, mainWindow)
            .ConfigureAwait(false);
    }

    private void DeleteFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || ItemViewModel is not { } item ||
            MainWindow is not { } mainWindow)
        {
            return;
        }

        _ = FileManager.DeleteFileWithOptionalDialog(recycle: true, item.FileLocation, core.PlatformService, mainWindow);
    }
}
