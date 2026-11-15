using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Core.DebugTools;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.Gallery;

public partial class GalleryItem : NavigateAbleItem
{
    /// <summary>Whether the item is a Masonry or Rows tile. Only the tile style in GalleryView.axaml writes it.</summary>
    public static readonly StyledProperty<bool> IsTileProperty =
        AvaloniaProperty.Register<GalleryItem, bool>(nameof(IsTile));

    private static readonly DoubleTransition ThumbnailOpacityTransition = new()
        { Property = OpacityProperty, Duration = TimeSpan.FromSeconds(0.32), Easing = new CubicEaseOut() };
    private static readonly TransformOperationsTransition ThumbnailScaleTransition = new()
        { Property = RenderTransformProperty, Duration = TimeSpan.FromSeconds(0.36), Easing = new CubicEaseOut() };
    private static readonly TransformOperations ThumbnailStartScale = TransformOperations.Parse("scale(1.04)");
    private static readonly TransformOperations ThumbnailEndScale = TransformOperations.Parse("scale(1)");

    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _sharpenCts;
    // What TileHeightAsked was before the request in flight, for when that request is cancelled
    private uint _tileHeightAskedBefore;
    private ContextMenu? _contextMenu;
    private Border? _ring;
    private string? _ringBrushKey;

    static GalleryItem()
    {
        // Nothing runs for an item that never becomes a tile
        IsTileProperty.Changed.AddClassHandler<GalleryItem>((item, _) => item.OnIsTileChanged());
    }

    public GalleryItem()
    {
        InitializeComponent();
    }

    public bool IsTile
    {
        get => GetValue(IsTileProperty);
        set => SetValue(IsTileProperty, value);
    }

    protected override void OnMarkChanged() => UpdateRing();

    private void OnIsTileChanged()
    {
        if (IsTile)
        {
            GalleryImage.PropertyChanged += OnThumbnailChanged;
        }
        else
        {
            GalleryImage.PropertyChanged -= OnThumbnailChanged;
        }

        UpdateRing();
    }

    // The ring exists only while a tile is marked: Docked and Columns items never get one
    private void UpdateRing()
    {
        var brushKey = !IsTile ? null
            : IsCurrent ? "AccentColor"
            : IsSelected || IsContextMenuOpen ? "SecondaryAccentColor"
            : null;
        var panel = (Panel)GalleryImage.Parent!;
        if (brushKey is null)
        {
            if (_ring is not null)
            {
                panel.Children.Remove(_ring);
                _ring = null;
                _ringBrushKey = null;
            }
            return;
        }

        if (_ring is null)
        {
            _ring = new Border
            {
                Name = "TileRing",
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                IsHitTestVisible = false
            };
            panel.Children.Add(_ring);
        }

        if (_ringBrushKey != brushKey)
        {
            _ringBrushKey = brushKey;
            _ring.Bind(Border.BorderBrushProperty, new DynamicResourceExtension(brushKey));
        }
    }

    private void OnThumbnailChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // A thumbnail that is there before the item is first arranged shows at once
        if (e.Property != Image.SourceProperty || e.OldValue is not null || e.NewValue is null || Bounds.Width <= 0)
        {
            return;
        }

        var image = GalleryImage;
        image.Transitions = null;
        image.Opacity = 0;
        image.RenderTransform = ThumbnailStartScale;
        image.Transitions = [ThumbnailOpacityTransition, ThumbnailScaleTransition];
        image.Opacity = 1;
        image.RenderTransform = ThumbnailEndScale;
    }

    private void InitContextMenu()
    {
        // Snapshot analysis from timeline profiling shows a lot of allocations from ContextMenu bindings,
        // so we lazily initialize the context menu.
        if (_contextMenu is not null ||
            !Resources.TryGetResource("GalleryContextMenu", ThemeVariant.Default, out var contextMenu)
            || contextMenu is not ContextMenu cm)
        {
            return;
        }
        
        _contextMenu = cm;
        _contextMenu.Opened += GalleryContextMenuOnOpened;
        _contextMenu.Closed += GalleryContextMenuOnClosed;
        ContextMenu = _contextMenu;
    }

    /// <summary>
    /// The height of the Tile Thumbnail last asked for since the item was loaded, so a rest does not ask for it again;
    /// 0 when none was.
    /// </summary>
    public uint TileHeightAsked { get; private set; }

    /// <summary>Whether a Tile Thumbnail asked for by <see cref="SharpenAsync"/> has not come back yet.</summary>
    internal bool IsSharpening => _sharpenCts is not null;

    /// <summary>
    /// Shows <paramref name="vm"/>'s thumbnail. A Masonry or Rows tile shows a kept one at once, a Tile Thumbnail before
    /// a small one, and asks for nothing; failing that, and in the Docked Gallery and Columns, it loads the small one.
    /// </summary>
    public Task LoadImage(GalleryItemViewModel vm) =>
        LoadImage(vm, this.FindAncestorOfType<VirtualizingGallery>() is { } panel &&
                      NavigateAbleItemsViewer.ShowsTiles(panel));

    /// <inheritdoc cref="LoadImage(GalleryItemViewModel)" />
    /// <param name="vm">The item to show.</param>
    /// <param name="isTile">
    /// Whether the item is in an expanded Masonry or Rows panel, which the caller reads from the panel: a recycled item
    /// is not in it yet.
    /// </param>
    internal async Task LoadImage(GalleryItemViewModel vm, bool isTile)
    {
        // Fast-path: If the image is already loaded, skip
        if (vm.Image.Value is IImage)
        {
            return;
        }

        if (isTile && vm.TryGetKeptThumbnail(out var kept, out var tileHeight) && kept is not null)
        {
            vm.Image.Value = kept;
            vm.TileHeightShown = tileHeight;
            return;
        }

        // ReSharper disable once MethodHasAsyncOverload
#pragma warning disable MA0042
        _loadCts?.Cancel();
#pragma warning restore MA0042
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        try
        {
            var thumb = await vm.LoadThumbnailAsync(token).ConfigureAwait(false);
        
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

    /// <summary>
    /// Shows a Tile Thumbnail of <paramref name="vm"/> made for <paramref name="height"/> in place of what the item
    /// shows, once it comes, unless <paramref name="ct"/> or unloading the item cancelled it first. A file that gives none
    /// leaves the shown thumbnail alone. Either way the height counts as asked for until the item is unloaded.
    /// </summary>
    public async Task SharpenAsync(GalleryItemViewModel vm, uint height, CancellationToken ct)
    {
        if (_sharpenCts is null)
        {
            _tileHeightAskedBefore = TileHeightAsked;
        }
        else
        {
            // ReSharper disable once MethodHasAsyncOverload
#pragma warning disable MA0042
            _sharpenCts.Cancel();
#pragma warning restore MA0042
        }

        TileHeightAsked = height;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _sharpenCts = cts;
        object? thumbnail = null;
        try
        {
            thumbnail = await vm.LoadTileThumbnailAsync(height, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Scrolled again, or the item was unloaded, while it waited for its turn
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(GalleryItem), nameof(SharpenAsync), ex);
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            EndSharpening(vm, height, thumbnail, cts);
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() => EndSharpening(vm, height, thumbnail, cts));
        }
    }

    // On the UI thread. A newer request, or unloading, took the item over when its source is no longer this one's
    private void EndSharpening(GalleryItemViewModel vm, uint height, object? thumbnail, CancellationTokenSource cts)
    {
        var isCurrent = ReferenceEquals(_sharpenCts, cts);
        var cancelled = cts.IsCancellationRequested;
        cts.Dispose();
        if (!isCurrent)
        {
            return;
        }

        _sharpenCts = null;
        if (cancelled)
        {
            // Not shown, so the next rest asks again, from the kept Tile Thumbnail if it was made meanwhile
            TileHeightAsked = _tileHeightAskedBefore;
            return;
        }

        if (thumbnail is null || !ReferenceEquals(DataContext, vm))
        {
            return;
        }

        // A small thumbnail still on its way must not land over the sharp one. The small one is not disposed: the
        // tab's store still keeps it
        _loadCts?.Cancel();
        vm.Image.Value = thumbnail;
        vm.TileHeightShown = height;
    }

    public void UnloadImage()
    {
        _loadCts?.Cancel();
        _sharpenCts?.Cancel();
        _sharpenCts = null;
        TileHeightAsked = 0;
        // A small thumbnail stays on its view model, as it did before Tile Thumbnails, when this ran after the viewer
        // had cleared the data context: the tab's store holds it anyway
        if (DataContext is not GalleryItemViewModel { TileHeightShown: > 0 } vm)
        {
            return;
        }

        // A Tile Thumbnail off screen is held only by the bounded tier, which may then let go of it
        vm.Image.Value = null;
        vm.TileHeightShown = 0;
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
        _contextMenu.Close();
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
            if (_contextMenu is null)
            {
                InitContextMenu();
            }
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
            _ = GalleryHelper.GalleryClick(tab.ParentWindowContext).ConfigureAwait(false);
        }
    }

    #region Menu click events

    private void OpenWith_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation;
        _ = FileManager.OpenWith(fileName).ConfigureAwait(false);
    }

    private void LocateOnDisk_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation;
        _ = FileManager.LocateOnDisk(fileName).ConfigureAwait(false);
    }

    private void WallpaperFilled_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation;
        _ = core.PlatformService.SetAsWallpaper(fileName, 4);
    }

    private void WallpaperFitted_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation;
        _ = core.PlatformService.SetAsWallpaper(fileName, 3);
    }

    private void WallpaperStretched_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation;
        _ = core.PlatformService.SetAsWallpaper(fileName, 2);
    }

    private void WallpaperCentered_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation;
        _ = core.PlatformService.SetAsWallpaper(fileName, 1);
    }

    private void WallpaperTiled_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item)
        {
            return;
        }
        var fileName = item.FileLocation;
        _ = core.PlatformService.SetAsWallpaper(fileName, 0);
    }

    private void CopyFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GalleryItemViewModel item ||
            TopLevel.GetTopLevel(this) is not MainWindow { DataContext: MainWindowViewModel } mainWindow)
        {
            return;
        }
        var fileName = item.FileLocation;
        _ = mainWindow.ClipboardCopyService.CopyFileAsync(fileName);
    }

    private void CopyImage_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item ||
            TopLevel.GetTopLevel(this) is not MainWindow { DataContext: MainWindowViewModel vm } mainWindow)
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
        if (DataContext is not GalleryItemViewModel item || TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        _ = mainWindow.ClipboardCopyService.CopyBase64Async(item.FileLocation).ConfigureAwait(false);
    }

    private void DuplicateFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GalleryItemViewModel item ||
            TopLevel.GetTopLevel(this) is not MainWindow { DataContext: MainWindowViewModel vm } mainWindow)
        {
            return;
        }
        var sourcePath = item.FileLocation;
        var currentActiveFilePath = vm.WindowTabs.ActiveTab.CurrentValue?.FileInfo?.CurrentValue.FullName;
        _ = FileManager.DuplicateFileAsync(sourcePath, currentActiveFilePath, vm, mainWindow)
            .ConfigureAwait(false);
    }

    private void DeleteFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not GalleryItemViewModel item ||
            TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        var fileName = item.FileLocation;
        _ = FileManager.DeleteFileWithOptionalDialog(recycle: true, fileName, core.PlatformService, mainWindow);
    }
    
    #endregion
    
    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        _contextMenu?.Opened -= GalleryContextMenuOnOpened;
        _contextMenu?.Closed -= GalleryContextMenuOnClosed;
    }
}