using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.CustomControls;

public class GalleryAnimationControl : UserControl
{
    #region Fields and Properties
    
    private const int ZeroSize = 0;
    private const int BorderTopAndBottomThickness = 2;
    private const int BorderSideThickness = 1;

    private TabViewModel? TabViewModel => DataContext as TabViewModel;
    private Control? _parentControl;

    private DisposableBag _disposables;
    private NavigateAbleItemsViewer? _viewer;
    private VirtualizingGallery? _itemsPanel;

    /// Tracks the previous mode to determine the animation transition
    private GalleryMode _previousMode = GalleryMode.Closed;

    public static readonly StyledProperty<GalleryMode> ActiveGalleryModeProperty =
        AvaloniaProperty.Register<GalleryAnimationControl, GalleryMode>(nameof(ActiveGalleryMode));

    public GalleryMode ActiveGalleryMode
    {
        get => GetValue(ActiveGalleryModeProperty);
        set => SetValue(ActiveGalleryModeProperty, value);
    }

    public bool IsInAnimation { get; private set; }

    private static Thickness GetDockedMargin => new(0);
    private static Thickness GetExpandedMargin => new(15, 40, 15, 5);

    private static double GetDockedSize(GalleryDockPosition dock)
    {
        switch (dock)
        {
            default:
            case GalleryDockPosition.Closed:
                return 0;
            case GalleryDockPosition.Bottom:
            case GalleryDockPosition.Top:
                return Settings.Gallery.DockedGalleryItemSize + BorderSideThickness + SizeDefaults.VerticalScrollbarSize;
            case GalleryDockPosition.Left:
            case GalleryDockPosition.Right:
                return Settings.Gallery.DockedGalleryItemSize + BorderTopAndBottomThickness + SizeDefaults.HorizontalScrollbarSize;
        }
    } 

    private double GetExpandedWrapHeight()
    {
        if (_parentControl is null)
        {
            return double.NaN;
        }

        var availableHeight = _parentControl.Bounds.Height
            - GetExpandedMargin.Top
            - GetExpandedMargin.Bottom
            - SizeDefaults.HorizontalScrollbarSize
            - BorderThickness.Top
            - BorderThickness.Bottom;

        return Math.Max(0, availableHeight);
    }

    private static bool IsHorizontalDock(GalleryDockPosition dock) => dock is GalleryDockPosition.Top or GalleryDockPosition.Bottom;

    #endregion

    #region Constructors, Subscriptions & Setup

    protected GalleryAnimationControl()
    {
        Loaded += OnControlLoaded;
    }

    private void OnControlLoaded(object? sender, RoutedEventArgs e)
    {
        _viewer = this.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl");

        if (_viewer?.ItemsPanelRoot is VirtualizingGallery panel)
        {
            _itemsPanel = panel;
        }
        else
        {
            DebugHelper.LogDebug(nameof(GalleryAnimationControl), nameof(OnControlLoaded), "Could not find ItemsControl.ItemsPanelRoot");
        }

        IsVisible = false; // Don't take up space initially
        Width = Height = 0; // Make sure it has no size before animation
        SetupSubscriptions();

        _parentControl = Parent as Control;
        _parentControl.SizeChanged += ParentSizeChanged;
    }

    private void SetupSubscriptions()
    {
        Debug.Assert(Settings.Gallery is not null);

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }

        // Change layout corresponding to DockPositions
        Observable.EveryValueChanged(Settings.Gallery, gallery => gallery.DockPosition, mainWindow.FrameProvider)
            .Skip(1)
            .Subscribe(SetDockedLayout, DebugHelper.LogError(nameof(GalleryAnimationControl), nameof(SetDockedLayout)))
            .AddTo(ref _disposables);
        
        // Update expanded item sizes
        Observable.EveryValueChanged(core.GallerySettings, gallery => gallery.ExpandedGalleryItemSize.CurrentValue, mainWindow.FrameProvider)
            .Skip(1)
            .Subscribe(UpdateExpandedItemHeight, DebugHelper.LogError(nameof(GalleryAnimationControl), nameof(UpdateExpandedItemHeight)))
            .AddTo(ref _disposables);

        // Update docked item sizes
        Observable.EveryValueChanged(core.GallerySettings, gallery => gallery.DockedGalleryItemSize.CurrentValue, mainWindow.FrameProvider)
            .Skip(1)
            .Subscribe(UpdateDockedItemHeight, DebugHelper.LogError(nameof(GalleryAnimationControl), nameof(UpdateDockedItemHeight)))
            .AddTo(ref _disposables);
        
        core.GallerySettings.ExpandedGalleryStretchMode.Skip(1).Subscribe(mode =>
            {
                SetExpandedThumbs(mode);
                _itemsPanel.InvalidateMeasure();
                Dispatcher.UIThread.Invoke(() =>
                {
                    _viewer.ScrollToCenterOfCurrentItem();
                }, DispatcherPriority.Render);
            }, DebugHelper.LogError(nameof(GalleryAnimationControl), nameof(UpdateExpandedItemHeight)))
        .AddTo(ref _disposables);
        
        core.GallerySettings.DockedGalleryStretchMode.Skip(1).Subscribe(mode =>
            {
                SetDockedThumbs(mode);
                _itemsPanel.InvalidateMeasure();
                _viewer.ScrollToCenterOfCurrentItem();
            }, DebugHelper.LogError(nameof(GalleryAnimationControl), nameof(core.GallerySettings.DockedGalleryStretchMode)))
        .AddTo(ref _disposables);
    }

    #endregion

    #region Logic & Layout

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ActiveGalleryModeProperty && change.NewValue is GalleryMode mode)
        {
            _ = Dispatcher.UIThread.InvokeAsync(async () => await OnGalleryModeChanged(mode).ConfigureAwait(false));
        }
        else if (change.Property == DockPanel.DockProperty)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _viewer.ScrollToCenterOfCurrentItem();
            }, DispatcherPriority.Render);
        }
    }

    private async ValueTask OnGalleryModeChanged(GalleryMode newMode)
    {
        if (IsInAnimation)
        {
            return;
        }
        try
        {
            IsInAnimation = true;
            var oldMode = _previousMode;
            _previousMode = newMode;

            switch (oldMode, newMode)
            {
                case (GalleryMode.Closed, GalleryMode.Docked): await ClosedToDocked().ConfigureAwait(false); break;
                case (GalleryMode.Closed, GalleryMode.Expanded): await ClosedToExpanded().ConfigureAwait(false); break;
                case (GalleryMode.Docked, GalleryMode.Expanded): await DockedToExpanded().ConfigureAwait(false); break;
                case (GalleryMode.Docked, GalleryMode.Closed): await DockedToClosed().ConfigureAwait(false); break;
                case (GalleryMode.Expanded, GalleryMode.Docked): await ExpandedToDocked().ConfigureAwait(false); break;
                case (GalleryMode.Expanded, GalleryMode.Closed): await ExpandedToClosed().ConfigureAwait(false); break;
                default: UpdateLayoutForCurrentState(); break;
            }
        }
        catch (Exception ex)
        {
            UpdateLayoutForCurrentState(); // Fallback
            DebugHelper.LogDebug(nameof(GalleryAnimationControl), nameof(OnGalleryModeChanged), ex);
        }
        finally
        {
            IsInAnimation = false;
        }
    }

    private void UpdateLayoutForCurrentState()
    {
        var dock = Settings.Gallery.DockPosition;
        IsVisible = ActiveGalleryMode != GalleryMode.Closed;

        switch (ActiveGalleryMode)
        {
            case GalleryMode.Closed:
                Width = Height = ZeroSize;
                break;
            case GalleryMode.Expanded:
                SetExpandedLayout(dock);
                break;
            case GalleryMode.Docked:
            default:
                SetDockedLayout(dock);
                break;
        }
    }

    #endregion

    #region Expanded Configuration

    private void SetExpandedLayout(GalleryDockPosition dock)
    {
        SetExpandedLayoutCore(dock);
        SetExpandedThumbs();
    }

    private void SetExpandedLayoutCore(GalleryDockPosition dock)
    {
        _itemsPanel.IsExpanded = true;
        _itemsPanel.WrapHeightOverride = double.NaN;
        
        if (_parentControl != null)
        {
            if (IsHorizontalDock(dock))
            {
                Width = double.NaN;
                Height = _parentControl.Bounds.Height;
            }
            else
            {
                Width = _parentControl.Bounds.Width;
                Height = double.NaN;
            }
        }

        _itemsPanel?.Orientation = Orientation.Vertical;
        TabViewModel?.Gallery.ItemSpacing.Value = Settings.Gallery.ItemSpacing;
        _viewer?.SetHorizontalScrolling();
        TabViewModel.Hoverbar.IsHoverbarVisible.Value = false;
    }

    private void SetExpandedThumbs()
    {
        ApplyThumbSettings(
            Settings.Gallery.ExpandedGalleryItemSize,
            Settings.Gallery.ExpandedGalleryStretchMode,
            GetExpandedMargin);
    }
    
    private void SetExpandedThumbs(GalleryStretchMode mode)
    {
        ApplyThumbSettings(
            Settings.Gallery.ExpandedGalleryItemSize,
            mode,
            GetExpandedMargin);
        _viewer.ScrollToCenterOfCurrentItem();
    }

    private void UpdateExpandedItemHeight(double itemHeight)
    {
        if (!TabViewModel.Gallery.IsGalleryExpanded.CurrentValue)
        {
            return;
        }
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        core.GallerySettings.ItemHeight.Value = itemHeight;
        _itemsPanel.InvalidateMeasure();
        _viewer.ScrollToCenterOfCurrentItem();
    }
    
    private void ParentSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // Keep the layout correct when the view is resized
        if (ActiveGalleryMode == GalleryMode.Expanded)
        {
            UpdateLayoutForCurrentState();
        }
    }

    #endregion

    #region Docked Configuration

    private void SetDockedLayout(GalleryDockPosition dock)
    {
        SetDockedLayoutCore(dock);
        SetDockedThumbPosition(dock);
    }

    private void SetDockedLayoutCore(GalleryDockPosition dock)
    {
        _itemsPanel.IsExpanded = false;
        
        var size = GetDockedSize(dock);
        TabViewModel.Gallery.ItemSpacing.Value = 0;
        
        if (IsHorizontalDock(dock))
        {
            Width = double.NaN;
            Height = size;

            _itemsPanel?.Orientation = Orientation.Horizontal;
            BorderThickness = dock == GalleryDockPosition.Top ? new Thickness(0, 0, 0, 1) : new Thickness(0, 1, 0, 0);

            _viewer?.SetHorizontalScrolling();
        }
        else // Left or Right
        {
            Width = size;
            Height = double.NaN;

            _itemsPanel?.Orientation = Orientation.Vertical;
            BorderThickness = dock == GalleryDockPosition.Right ? new Thickness(1, 0, 0, 0) : new Thickness(0, 0, 1, 0);

            _viewer?.SetVerticalScrolling();
        }
        TabViewModel.Hoverbar.IsHoverbarVisible.Value = !Settings.UIProperties.ShowBottomNavBar && Settings.UIProperties.ShowHoverNavigationBar;
    }

    private void SetDockedThumbPosition(GalleryDockPosition dock)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        var gallery = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.Value.Gallery;

        // Reset all dock flags
        gallery.IsTopDocked.Value = gallery.IsBottomDocked.Value =
        gallery.IsLeftDocked.Value = gallery.IsRightDocked.Value = false;

        switch (dock)
        {
            case GalleryDockPosition.Top:
                DockPanel.SetDock(this, Dock.Top);
                gallery.IsTopDocked.Value = true;
                break;
            case GalleryDockPosition.Left:
                DockPanel.SetDock(this, Dock.Left);
                gallery.IsLeftDocked.Value = true;
                break;
            case GalleryDockPosition.Right:
                DockPanel.SetDock(this, Dock.Right);
                gallery.IsRightDocked.Value = true;
                break;
            case GalleryDockPosition.Bottom:
                DockPanel.SetDock(this, Dock.Bottom);
                gallery.IsBottomDocked.Value = true;
                break;
            case GalleryDockPosition.Closed:
            default:
                if (Settings.Gallery.IsGalleryDocked) goto case GalleryDockPosition.Bottom;
                IsVisible = false;
                return;
        }

        IsVisible = true;
        ApplyThumbSettings(
            Settings.Gallery.DockedGalleryItemSize,
            Settings.Gallery.DockedGalleryStretchMode,
            GetDockedMargin);
    }

    private void UpdateDockedItemHeight(double itemHeight)
    {
        if (TabViewModel.Gallery.IsGalleryExpanded.CurrentValue || Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        core.GallerySettings.ItemHeight.Value = itemHeight;

        // Resize control bounds
        var size = itemHeight + BorderTopAndBottomThickness + SizeDefaults.HorizontalScrollbarSize;
        if (IsHorizontalDock(Settings.Gallery.DockPosition))
        {
            Width = double.NaN;
            Height = size;
        }
        else
        {
            Width = size;
            Height = double.NaN;
        }
        _viewer.ScrollToCenterOfCurrentItem();
    }
    
    private void SetDockedThumbs(GalleryStretchMode mode)
    {
        ApplyThumbSettings(
            Settings.Gallery.DockedGalleryItemSize,
            mode,
            GetDockedMargin);
    }
    
    private void ApplyThumbSettings(double size, GalleryStretchMode mode, Thickness margin, double spacing = 0)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        var settings = core.GallerySettings;
        settings.ItemHeight.Value = size;
        switch (mode)
        {
            case GalleryStretchMode.Uniform:
                settings.GalleryStretch.Value = Stretch.Uniform;
                settings.ItemWidth.Value = double.NaN;
                break;
            case GalleryStretchMode.UniformToFill:
                settings.GalleryStretch.Value = Stretch.UniformToFill;
                settings.ItemWidth.Value = double.NaN;
                break;
            case GalleryStretchMode.Square:
                settings.GalleryStretch.Value = Stretch.Uniform;
                settings.ItemWidth.Value = size;
                break;
            case GalleryStretchMode.FillSquare:
                settings.GalleryStretch.Value = Stretch.Fill;
                settings.ItemWidth.Value = size;
                break;
        }

        if (spacing > 0)
        {
            TabViewModel.Gallery.ItemSpacing.Value = spacing;
        }
        
        _itemsPanel.Margin = margin;
    }

    #endregion

    #region Animations

    private async Task ClosedToDocked()
    {
        if (!Settings.Gallery.IsGalleryDocked)
        {
            return;
        }

        var dock = Settings.Gallery.DockPosition;
        IsVisible = true;
        SetDockedLayoutCore(dock);
        SetDockedThumbPosition(dock);
        
        Dispatcher.UIThread.Post(() =>
        {
            _viewer.ScrollToCenterOfCurrentItem();
        }, DispatcherPriority.Render);
        
        var targetSize = GetDockedSize(dock);

        if (IsHorizontalDock(dock))
        {
            Height = ZeroSize;
            var heightAnim = AnimationsHelper.HeightAnimation(ZeroSize, targetSize, GalleryDefaults.FastAnimationSpeed);
            await heightAnim.RunAsync(this).ConfigureAwait(true);
            Height = targetSize;
        }
        else
        {
            Width = ZeroSize;
            var widthAnim = AnimationsHelper.WidthAnimation(ZeroSize, targetSize, GalleryDefaults.FastAnimationSpeed);
            await widthAnim.RunAsync(this).ConfigureAwait(true);
            Width = targetSize;
        }
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _viewer.ScrollToCenterOfCurrentItem();
        }, DispatcherPriority.Render);

        if (Settings.WindowProperties.AutoFit)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
                {
                    WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
                }
            });
        }
    }

    private async Task DockedToClosed()
    {
        var isHorizontal = IsHorizontalDock(Settings.Gallery.DockPosition);
        var currentSize = GetDockedSize(Settings.Gallery.DockPosition);

        if (isHorizontal)
        {
            await AnimationsHelper.HeightAnimation(currentSize, ZeroSize, GalleryDefaults.MediumAnimationSpeed).RunAsync(this).ConfigureAwait(true);
            Height = ZeroSize;
        }
        else
        {
            await AnimationsHelper.WidthAnimation(currentSize, ZeroSize, GalleryDefaults.MediumAnimationSpeed).RunAsync(this).ConfigureAwait(true);
            Width = ZeroSize;
        }

        IsVisible = false;
    }

    private async Task DockedToExpanded()
    {
        var dock = Settings.Gallery.DockPosition;
        var targetHeight = _parentControl!.Bounds.Height;

        SetExpandedLayoutCore(dock);
        SetExpandedThumbs();

        // LOCK the layout wrap constraint to the final calculated available size before animating
        _itemsPanel.WrapHeightOverride = GetExpandedWrapHeight();

        var startSize = GetDockedSize(dock);
        Dispatcher.UIThread.Post(() =>
        {
            _viewer.ScrollToCenterOfCurrentItem();
        }, DispatcherPriority.Render);

        if (IsHorizontalDock(dock))
        {
            var heightAnim =
                AnimationsHelper.HeightAnimation(startSize, targetHeight, GalleryDefaults.MediumAnimationSpeed);
            await heightAnim.RunAsync(this).ConfigureAwait(true);
            Height = targetHeight;
        }
        else
        {
            var targetWidth = _parentControl.Bounds.Width;
            var widthAnim = AnimationsHelper.WidthAnimation(startSize, targetWidth, GalleryDefaults.MediumAnimationSpeed);
            await widthAnim.RunAsync(this).ConfigureAwait(true);
            Width = targetWidth;
            if (OperatingSystem.IsMacOS())
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Fixes rendering bug
                    Width = _parentControl.Bounds.Width;
                }, DispatcherPriority.Send);
            }
        }
        
        // Unlock the layout and trigger remeasure to match exact final bounds
        _itemsPanel.WrapHeightOverride = double.NaN;
        _itemsPanel.InvalidateMeasure();

        if (OperatingSystem.IsMacOS())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Mac doesn't want to scroll to center for some reason
                _viewer.ScrollToCenterOfCurrentItem();
            }, DispatcherPriority.Render);
        }
    }

    private async Task ExpandedToDocked()
    {
        var dock = Settings.Gallery.DockPosition;
        var startHeight = _parentControl.Bounds.Height;

        // LOCK the layout wrap constraint so it doesn't wrap tighter as the window shrinks
        _itemsPanel.WrapHeightOverride = GetExpandedWrapHeight();
        
        if (IsHorizontalDock(dock))
        {
            var targetHeight = GetDockedSize(dock);
            if (Settings.WindowProperties.AutoFit)
            {
                Height = startHeight;
                var heightAnim = AnimationsHelper.HeightAnimation(startHeight, targetHeight, GalleryDefaults.SlowAnimationSpeed);
                await heightAnim.RunAsync(this).ConfigureAwait(true);
            }
            else
            {
                // Need to continuously update the image size while animating if auto-fit is off
                if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
                {
                    return;
                }
                var cts = new CancellationTokenSource();
                var ct = cts.Token;
                Observable.EveryUpdate(mainWindow.FrameProvider, ct).Subscribe(_ =>
                {
                    WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
                }, DebugHelper.LogError(nameof(GalleryAnimationControl), nameof(ExpandedToDocked)));
                var heightAnim =
                    AnimationsHelper.HeightAnimation(startHeight, targetHeight, GalleryDefaults.SlowAnimationSpeed);
                await heightAnim.RunAsync(this, ct).ConfigureAwait(true);
                await cts.CancelAsync().ConfigureAwait(true);
            }
            Height = targetHeight;
        }
        else
        {
            var startWidth = _parentControl.Bounds.Width;
            var targetWidth = GetDockedSize(dock);
            Width = startWidth;
            var widthAnim =
                AnimationsHelper.WidthAnimation(startWidth, targetWidth, GalleryDefaults.SlowAnimationSpeed);
            await widthAnim.RunAsync(this).ConfigureAwait(true);
            Width = targetWidth;
        }

        SetDockedLayout(dock);
        
        // Unlock the layout before scrolling so measurements match docked mode
        _itemsPanel.WrapHeightOverride = double.NaN;
        _itemsPanel.InvalidateMeasure();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _viewer?.ScrollToCenterOfCurrentItem();
        }, DispatcherPriority.Render);
    }

    private async Task ClosedToExpanded()
    {
        IsVisible = true;
        Width = Height = ZeroSize;

        var targetHeight = _parentControl!.Bounds.Height;
        var targetWidth = _parentControl.Bounds.Width;

        SetExpandedLayoutCore(Settings.Gallery.DockPosition);
        SetExpandedThumbs();

        // Lock constraint and set expanded layout BEFORE animation for a smooth reveal
        _itemsPanel.WrapHeightOverride = GetExpandedWrapHeight();
        
        Dispatcher.UIThread.Post(() =>
        {
            _viewer.ScrollToCenterOfCurrentItem();
        }, DispatcherPriority.Render);

        await Task.WhenAll(
            AnimationsHelper.WidthAnimation(ZeroSize, targetWidth, GalleryDefaults.MediumAnimationSpeed).RunAsync(this),
            AnimationsHelper.HeightAnimation(ZeroSize, targetHeight, GalleryDefaults.MediumAnimationSpeed).RunAsync(this)
        ).ConfigureAwait(true);
        
        if (IsHorizontalDock(Settings.Gallery.DockPosition))
        {
            Width = double.NaN;
            Height = targetHeight;
        }
        else
        {
            Width = targetWidth;
            Height = double.NaN;
        }

        _itemsPanel.WrapHeightOverride = double.NaN;
        _itemsPanel.InvalidateMeasure();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _viewer?.ScrollToCenterOfCurrentItem();
        }, DispatcherPriority.Render);
    }

    private async Task ExpandedToClosed()
    {
        _itemsPanel.WrapHeightOverride = GetExpandedWrapHeight();

        await Task.WhenAll(
            AnimationsHelper.WidthAnimation(Bounds.Width, ZeroSize, GalleryDefaults.FastAnimationSpeed).RunAsync(this),
            AnimationsHelper.HeightAnimation(Bounds.Height, ZeroSize, GalleryDefaults.FastAnimationSpeed).RunAsync(this)
        ).ConfigureAwait(true);

        IsVisible = false;
        _itemsPanel.WrapHeightOverride = double.NaN;
        _itemsPanel.InvalidateMeasure();

        if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
        {
            WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
        }
    }

    #endregion

    #region Cleanup

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (_parentControl != null)
        {
            _parentControl.SizeChanged -= ParentSizeChanged;
        }

        Loaded -= OnControlLoaded;
    }

    #endregion
}