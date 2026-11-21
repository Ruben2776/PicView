using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Core.DebugTools;
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
    // Cancelled when the item is unloaded: the item is shown from LoadImage until then
    private CancellationTokenSource? _shownCts;
    // What TileHeightAsked was before the request in flight, for when that request is cancelled
    private uint _tileHeightAskedBefore;
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

    /// <summary>
    /// True from when the item's data context starts to change until the viewer has started loading the thumbnail of
    /// the gallery item it shows now. A thumbnail shown in between shows at once, in a recycled item as in a new one.
    /// </summary>
    internal bool IsTakingItem { get; set; }

    protected override void OnDataContextBeginUpdate()
    {
        base.OnDataContextBeginUpdate();
        IsTakingItem = true;
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
        if (e.Property != Image.SourceProperty || e.OldValue is not null || e.NewValue is null)
        {
            return;
        }

        var image = GalleryImage;
        if (IsTakingItem)
        {
            // A thumbnail the new item has already shows at once, whatever the last item's fade left behind
            image.Transitions = null;
            image.Opacity = 1;
            image.RenderTransform = ThumbnailEndScale;
            return;
        }

        // A thumbnail that is there before the item is first arranged shows at once
        if (Bounds.Width <= 0)
        {
            return;
        }

        image.Transitions = null;
        image.Opacity = 0;
        image.RenderTransform = ThumbnailStartScale;
        image.Transitions = [ThumbnailOpacityTransition, ThumbnailScaleTransition];
        image.Opacity = 1;
        image.RenderTransform = ThumbnailEndScale;
    }

    /// <summary>
    /// The height of the Tile Thumbnail last asked for since the item was loaded, so a rest does not ask for it again;
    /// 0 when none was.
    /// </summary>
    public uint TileHeightAsked { get; private set; }

    /// <summary>Whether a Tile Thumbnail asked for by <see cref="SharpenAsync"/> has not come back yet.</summary>
    internal bool IsSharpening => _sharpenCts is not null;

    /// <summary>
    /// Shows <paramref name="vm"/>'s thumbnail. A Masonry or Rows tile shows a kept Tile Thumbnail at once, even in place
    /// of a small one it holds, and has its tab keep the small one too; else it shows a kept small one at once. Failing
    /// that, and in the Docked Gallery and Columns, it loads the small one.
    /// </summary>
    /// <param name="vm">The item to show.</param>
    /// <param name="isTile">
    /// Whether the item is in an expanded Masonry or Rows panel, which the caller reads from the panel: a recycled item
    /// is not in it yet.
    /// </param>
    internal async Task LoadImage(GalleryItemViewModel vm, bool isTile)
    {
        // ReSharper disable once MethodHasAsyncOverload
#pragma warning disable MA0042
        _shownCts?.Cancel();
#pragma warning restore MA0042
        _shownCts = new CancellationTokenSource();

        if (isTile && vm.TileHeightShown is 0 && vm.TryGetKeptThumbnail(out var kept, out var tileHeight) &&
            kept is not null)
        {
            if (tileHeight > 0 && !HasFewerPixelRows(kept, vm.Image.Value))
            {
                // Sharp at once, as after the Docked Gallery, where the item took its small one. The tab still keeps
                // the small one, made the ordinary way, so the motion photo badge is detected and the tile does not go
                // empty once the tier lets go of this one. One the tab keeps already is served at once, decoding nothing
                vm.Image.Value = kept;
                vm.TileHeightShown = tileHeight;
                await LoadSmallThumbnailAsync(vm, isTile, show: false).ConfigureAwait(false);
                return;
            }

            if (vm.Image.Value is not IImage)
            {
                vm.Image.Value = kept;
            }

            // Shown without loading the small thumbnail, which detects the badge along with it
            DetectMotionPhoto();
            return;
        }

        // Fast-path: If the image is already loaded, skip
        if (vm.Image.Value is IImage)
        {
            DetectMotionPhoto();
            return;
        }

        await LoadSmallThumbnailAsync(vm, isTile, show: true).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads the small thumbnail of <paramref name="vm"/>, which the tab then keeps, and shows it if
    /// <paramref name="show"/> unless the item shows a Tile Thumbnail by then. Unloading the item cancels it.
    /// </summary>
    private async Task LoadSmallThumbnailAsync(GalleryItemViewModel vm, bool isTile, bool show)
    {
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
            if (!show || token.IsCancellationRequested || thumb is null)
            {
                return;
            }

            if (!isTile)
            {
                // The Docked Gallery and Columns, as before Tile Thumbnails. A Tile Thumbnail shown after a switch to
                // Masonry or Rows stays
                if (vm.TileHeightShown is 0)
                {
                    vm.Image.Value = thumb;
                }

                return;
            }

            // A tile decides on the UI thread, where a Tile Thumbnail lands, so the small one never lands over it
            if (Dispatcher.UIThread.CheckAccess())
            {
                ShowSmallThumbnail(vm, thumb, token);
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() => ShowSmallThumbnail(vm, thumb, token));
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(GalleryItem), nameof(LoadImage), ex);
        }
    }

    /// <summary>
    /// Whether <paramref name="thumbnail"/> is a bitmap of fewer pixels in height than the bitmap
    /// <paramref name="shown"/>, so showing it in its place would make the item softer.
    /// </summary>
    private static bool HasFewerPixelRows(object? thumbnail, object? shown) =>
        thumbnail is Bitmap bitmap && shown is Bitmap shownBitmap &&
        bitmap.PixelSize.Height < shownBitmap.PixelSize.Height;

    // On the UI thread. A Tile Thumbnail that came first stays: the small one is then only kept for the tab
    private static void ShowSmallThumbnail(GalleryItemViewModel vm, object thumbnail, CancellationToken token)
    {
        if (!token.IsCancellationRequested && vm.TileHeightShown is 0)
        {
            vm.Image.Value = thumbnail;
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
            // Scrolled again, or the item was unloaded, while it waited for its turn: null, and EndSharpening sees
            // the cancel
            thumbnail = await vm.LoadTileThumbnailAsync(height, cts.Token).ConfigureAwait(false);
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
        if (thumbnail is null && !cancelled)
        {
            // The file gives none at that height, so it is not asked for again
            return;
        }

        // Only a tile shows a Tile Thumbnail: the gallery may have left Masonry and Rows, for Columns or docked
        var panel = this.FindAncestorOfType<VirtualizingGallery>();
        if (thumbnail is null || panel is null || !NavigateAbleItemsViewer.ShowsTiles(panel) ||
            !ReferenceEquals(DataContext, vm))
        {
            // Cancelled while it waited for its turn, or not shown: a later rest asks again, and one is due if the
            // item is still a tile on screen
            TileHeightAsked = _tileHeightAskedBefore;
            this.FindAncestorOfType<NavigateAbleItemsViewer>()?.RequestTileSharpen();
            return;
        }

        if (HasFewerPixelRows(thumbnail, vm.Image.Value))
        {
            // The source gave less than the item shows, as a small image's small thumbnail, enlarged, can be. The item
            // keeps what it shows, and the height counts as asked for, so it is not asked for again
            return;
        }

        // A make that finished after a new rest began is still this item's, made for a height it asked for, so it is
        // shown. If the tile needs more by now, the shown height says so, and the rest after it asks for the taller one
        if (cancelled)
        {
            this.FindAncestorOfType<NavigateAbleItemsViewer>()?.RequestTileSharpen();
        }

        // A small thumbnail still on its way is not cancelled, so the tab keeps it, and it does not land over this one,
        // as TileHeightShown says the item shows a Tile Thumbnail. The small one shown is not disposed: the tab keeps it
        vm.Image.Value = thumbnail;
        vm.TileHeightShown = height;
    }

    /// <summary>
    /// Detects the motion photo badge of the item shown, off the UI thread, if the badges are shown and it was not
    /// checked before. Nothing for an item that is not shown.
    /// </summary>
    internal void DetectMotionPhoto()
    {
        if (_shownCts is { } shown && DataContext is GalleryItemViewModel vm)
        {
            vm.DetectMotionPhoto(shown.Token);
        }
    }

    public void UnloadImage()
    {
        // ReSharper disable once MethodHasAsyncOverload
#pragma warning disable MA0042
        _shownCts?.Cancel();
#pragma warning restore MA0042
        _shownCts = null;
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

    /// <summary>
    /// Gives the gallery view's menu back, closing it, if this item has it: the menu would act on whatever the item
    /// shows next, or keep an item that left the gallery alive.
    /// </summary>
    internal void ReleaseContextMenu() => (ContextMenu as GalleryItemContextMenu)?.DetachFrom(this);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // The gallery view's one menu, so an item builds none. It opens for this item as the button is released
            this.FindLogicalAncestorOfType<GalleryView>()?.ItemContextMenu.AttachTo(this);
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

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        ReleaseContextMenu();
    }
}