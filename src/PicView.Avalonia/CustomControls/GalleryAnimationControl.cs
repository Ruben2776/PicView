using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
using PicView.Avalonia.Gallery;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// Shows the Gallery Appearance of its tab's gallery, and animates from one to the next when the tab's switcher says
/// to. It works out nothing about the look itself: <see cref="GalleryViewModel.Appearance"/> does.
/// </summary>
public class GalleryAnimationControl : UserControl
{
    #region Fields and Properties

    private TabViewModel? TabViewModel => DataContext as TabViewModel;
    private Control? _parentControl;

    private DisposableBag _disposables;
    private NavigateAbleItemsViewer? _viewer;
    private VirtualizingGallery? _itemsPanel;

    /// <summary>
    /// The appearance this control shows. It starts closed, which is all a default record says.
    /// </summary>
    private GalleryAppearance _applied;

    public bool IsInAnimation { get; private set; }

    /// <summary>
    /// The height the Expanded Gallery wraps its items in once it fills the parent, for locking the wrap while the
    /// gallery grows or shrinks.
    /// </summary>
    private double GetExpandedWrapHeight(GalleryAppearance expanded)
    {
        if (_parentControl is null)
        {
            return double.NaN;
        }

        var availableHeight = _parentControl.Bounds.Height
            - expanded.Margin.Top
            - expanded.Margin.Bottom
            - SizeDefaults.HorizontalScrollbarSize
            - expanded.Border.Top
            - expanded.Border.Bottom;

        return Math.Max(0, availableHeight);
    }

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
        if (TabViewModel is not { } tab)
        {
            return;
        }

        // Follow the tab's switcher. A Single Image can hide the gallery from a background thread, and a change during
        // an animation is dropped, since the animation catches up when it ends
        tab.Gallery.Switcher.Changes
            .ObserveOnUIThreadDispatcher()
            .SubscribeAwait(async (change, _) => await OnGalleryModeChanged(change).ConfigureAwait(true),
                DebugHelper.LogError(nameof(GalleryAnimationControl), nameof(OnGalleryModeChanged)),
                AwaitOperation.Drop)
            .AddTo(ref _disposables);

        // The size sliders, the stretch combos, the dock edge and the item spacing, in the mode already shown
        tab.Gallery.Appearance.AsObservable()
            .Skip(1)
            .ObserveOnUIThreadDispatcher()
            .Subscribe(OnAppearanceChanged, DebugHelper.LogError(nameof(GalleryAnimationControl), nameof(OnAppearanceChanged)))
            .AddTo(ref _disposables);

        // Show what the switcher already shows, such as a gallery started docked (ADR-0005), without animation. It is
        // read after subscribing, so a change made from a background thread meanwhile is either read here or followed
        if (tab.Gallery.Appearance.Value is { IsShown: true } shown)
        {
            ShowWithoutAnimation(shown);
        }
    }

    #endregion

    #region Logic & Layout

    /// <summary>
    /// Shows a change the tab's switcher published, animated or not as it says. A change that arrives during an
    /// animation is caught up without animation once the animation ends, as is any other change to the appearance.
    /// </summary>
    private async ValueTask OnGalleryModeChanged(GalleryModeChange change)
    {
        while (true)
        {
            if (IsInAnimation || TabViewModel is not { } tab)
            {
                return;
            }

            var from = _applied;
            var to = tab.Gallery.Appearance.Value;
            try
            {
                IsInAnimation = true;
                var autoFit = tab.ParentWindowContext?.WindowModeState.CurrentValue.AutoFit is true;
                if (change.Animate && GalleryTransition.For(from.Mode, to.Mode, to.Dock, autoFit) is { } transition)
                {
                    await Animate(from, to, transition).ConfigureAwait(true);
                }
                else
                {
                    ShowWithoutAnimation(to);
                }
            }
            catch (Exception ex)
            {
                // Fallback
                _itemsPanel?.WrapHeightOverride = double.NaN;
                Apply(to);
                SetExtent(to);
                DebugHelper.LogDebug(nameof(GalleryAnimationControl), nameof(OnGalleryModeChanged), ex);
            }
            finally
            {
                IsInAnimation = false;
            }

            if (_applied != tab.Gallery.Appearance.Value)
            {
                change = new GalleryModeChange(_applied.Mode, tab.Gallery.Appearance.Value.Mode, false);
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// Shows a new appearance in the mode already shown. One in another mode waits for its mode change, and one during
    /// an animation is caught up once it ends.
    /// </summary>
    private void OnAppearanceChanged(GalleryAppearance appearance)
    {
        if (IsInAnimation || appearance.Mode != _applied.Mode || appearance == _applied)
        {
            return;
        }

        if (!appearance.IsShown)
        {
            Apply(appearance);
            return;
        }

        var dockChanged = appearance.Dock != _applied.Dock;
        Apply(appearance);
        SetExtent(appearance);
        _itemsPanel?.InvalidateMeasure();
        _viewer?.ScrollToCenterOfCurrentItem();

        // The Reserved Space moves with the Docked Gallery
        if (dockChanged && appearance.Mode is GalleryMode.Docked && TopLevel.GetTopLevel(this) is MainWindow window)
        {
            window.WindowFitter.Refit();
        }
    }

    private void ShowWithoutAnimation(GalleryAppearance appearance)
    {
        Apply(appearance);
        SetExtent(appearance);
        if (appearance.Mode is GalleryMode.Docked)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _viewer?.ScrollToCenterOfCurrentItem();
            }, DispatcherPriority.Render);
        }

        if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
        {
            mainWindow.WindowFitter.Refit();
        }
    }

    /// <summary>
    /// Shows <paramref name="appearance"/>, apart from its extent. A closed one only hides the gallery, and leaves the
    /// rest as it was.
    /// </summary>
    private void Apply(GalleryAppearance appearance)
    {
        if (!appearance.IsShown)
        {
            IsVisible = false;
            _applied = appearance;
            return;
        }

        var dockChanged = _applied.Mode is GalleryMode.Docked && appearance.Mode is GalleryMode.Docked &&
                          _applied.Dock != appearance.Dock;

        if (_itemsPanel is { } panel)
        {
            panel.ItemHeight = appearance.ItemHeight;
            panel.ItemWidth = appearance.ItemWidth;
            panel.ItemSpacing = appearance.ItemSpacing;
            panel.Orientation = appearance.Orientation is GalleryOrientation.Horizontal
                ? Orientation.Horizontal
                : Orientation.Vertical;
            panel.IsExpanded = appearance.Wraps;
            panel.Margin = ToThickness(appearance.Margin);
            panel.ThumbnailStretch = GalleryThumbnailStretch.ToStretch(appearance.Stretch);
        }

        BorderThickness = ToThickness(appearance.Border);
        DockPanel.SetDock(this, appearance.Dock switch
        {
            GalleryDockPosition.Top => Dock.Top,
            GalleryDockPosition.Left => Dock.Left,
            GalleryDockPosition.Right => Dock.Right,
            _ => Dock.Bottom
        });

        if (appearance.ScrollsHorizontally)
        {
            _viewer?.SetHorizontalScrolling();
        }
        else
        {
            _viewer?.SetVerticalScrolling();
        }

        IsVisible = true;

        // Only a mode or dock move writes the hover bar; the first show always does, as _applied starts closed and
        // this appearance is shown. Other writers, such as ToggleInterface, keep it through size and stretch changes
        if (appearance.Mode != _applied.Mode || appearance.Dock != _applied.Dock)
        {
            TabViewModel?.Hoverbar.IsHoverbarVisible.Value = appearance.ShowsHoverBar;
        }

        // Moving to another edge wraps at the docked extent for one render pass
        if (dockChanged && _itemsPanel is { } movedPanel)
        {
            movedPanel.WrapHeightOverride = appearance.Extent;
            Dispatcher.UIThread.Post(() =>
            {
                movedPanel.WrapHeightOverride = double.NaN;
            }, DispatcherPriority.Render);
        }

        _applied = appearance;
    }

    /// <summary>
    /// Sizes the control for <paramref name="appearance"/>: its extent along the dock axis when docked, the parent's
    /// bounds on the axis that doesn't stretch when expanded, and nothing when closed.
    /// </summary>
    private void SetExtent(GalleryAppearance appearance)
    {
        var horizontalDock = appearance.Dock is GalleryDockPosition.Top or GalleryDockPosition.Bottom;
        switch (appearance.Mode)
        {
            case GalleryMode.Docked when horizontalDock:
                Width = double.NaN;
                Height = appearance.Extent;
                break;
            case GalleryMode.Docked:
                Width = appearance.Extent;
                Height = double.NaN;
                break;
            case GalleryMode.Expanded when _parentControl is null:
                break;
            case GalleryMode.Expanded when horizontalDock:
                Width = double.NaN;
                Height = _parentControl.Bounds.Height;
                break;
            case GalleryMode.Expanded:
                Width = _parentControl.Bounds.Width;
                Height = double.NaN;
                break;
            default:
                Width = Height = 0;
                break;
        }
    }

    private static Thickness ToThickness(GalleryInsets insets) =>
        new(insets.Left, insets.Top, insets.Right, insets.Bottom);

    private void ParentSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // Keep the layout correct when the view is resized
        if (_applied.Mode is GalleryMode.Expanded)
        {
            SetExtent(_applied);
        }
    }

    #endregion

    #region Animations

    /// <summary>
    /// Animates from <paramref name="from"/> to <paramref name="to"/> as <paramref name="transition"/> says.
    /// </summary>
    private async Task Animate(GalleryAppearance from, GalleryAppearance to, GalleryTransition transition)
    {
        // 1. The new look, unless it comes at the end
        if (to.IsShown && !transition.AppliesLookAtEnd)
        {
            Apply(to);
        }

        // 2. Wrap as the Expanded Gallery does once it fills the parent, so it doesn't rewrap while it grows or shrinks
        if (transition.LocksWrap && _itemsPanel is not null)
        {
            _itemsPanel.WrapHeightOverride = GetExpandedWrapHeight(to.Mode is GalleryMode.Expanded ? to : from);
        }

        // The current item, in the new look
        if (to.IsShown && !transition.AppliesLookAtEnd)
        {
            if (from.Mode is GalleryMode.Closed && to.Mode is GalleryMode.Docked)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _viewer?.ScrollToCenterOfCurrentItem();
                }, DispatcherPriority.Background);
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _viewer?.ScrollToCenterOfCurrentItem();
                }, DispatcherPriority.Render);
            }
        }

        // 3. The tween, from the current extent to the target's
        var startWidth = from.IsShown ? Bounds.Width : 0;
        var startHeight = from.IsShown ? Bounds.Height : 0;
        var endWidth = TweenTarget(to, horizontal: false);
        var endHeight = TweenTarget(to, horizontal: true);
        Task tween;
        if (transition.TweensBothAxes)
        {
            Width = startWidth;
            Height = startHeight;
            tween = Task.WhenAll(
                AnimationsHelper.WidthAnimation(startWidth, endWidth, transition.Speed).RunAsync(this),
                AnimationsHelper.HeightAnimation(startHeight, endHeight, transition.Speed).RunAsync(this));
        }
        else
        {
            var tweensHeight = to.Dock is GalleryDockPosition.Top or GalleryDockPosition.Bottom;
            if (tweensHeight)
            {
                Width = to.IsShown ? double.NaN : Width;
                Height = startHeight;
            }
            else
            {
                Width = startWidth;
                Height = to.IsShown ? double.NaN : Height;
            }

            tween = transition.RefitsEveryFrame
                ? TweenWhileRefitting(startHeight, endHeight, transition.Speed)
                : tweensHeight
                    ? AnimationsHelper.HeightAnimation(startHeight, endHeight, transition.Speed).RunAsync(this)
                    : AnimationsHelper.WidthAnimation(startWidth, endWidth, transition.Speed).RunAsync(this);
        }

        await tween.ConfigureAwait(true);

        // 4. The final extent
        SetExtent(to);
        if (OperatingSystem.IsMacOS() && from.Mode is GalleryMode.Docked && to.Mode is GalleryMode.Expanded &&
            to.Dock is GalleryDockPosition.Left or GalleryDockPosition.Right)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Fixes rendering bug
                Width = _parentControl!.Bounds.Width;
            }, DispatcherPriority.Send);
        }

        // 5. The new look when it comes at the end, or hidden
        if (!to.IsShown || transition.AppliesLookAtEnd)
        {
            Apply(to);
        }

        // 6. Unlock the wrap and remeasure to match the final bounds
        if (transition.LocksWrap && _itemsPanel is not null)
        {
            _itemsPanel.WrapHeightOverride = double.NaN;
            _itemsPanel.InvalidateMeasure();
        }

        // 7. The current item, where the gallery ended up
        if (to.IsShown)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _viewer?.ScrollToCenterOfCurrentItem();
            }, DispatcherPriority.Render);
        }

        // 8. A fit for the gallery that docked or closed
        if (transition.RefitsAtEnd && TopLevel.GetTopLevel(this) is MainWindow mainWindow)
        {
            mainWindow.WindowFitter.Refit();
        }
    }

    /// <summary>
    /// Where the tween ends on one axis: the parent's bounds when expanded, the extent when docked, and 0 when closed.
    /// </summary>
    private double TweenTarget(GalleryAppearance to, bool horizontal)
    {
        return to.Mode switch
        {
            GalleryMode.Expanded when horizontal => _parentControl?.Bounds.Height ?? 0,
            GalleryMode.Expanded => _parentControl?.Bounds.Width ?? 0,
            GalleryMode.Docked => to.Extent,
            _ => 0
        };
    }

    /// <summary>
    /// Tweens the height while refitting the window on every frame, since a window that doesn't AutoFit has to keep
    /// fitting its image to the space the gallery gives back.
    /// </summary>
    private async Task TweenWhileRefitting(double from, double to, double speed)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            await AnimationsHelper.HeightAnimation(from, to, speed).RunAsync(this).ConfigureAwait(true);
            return;
        }

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        Observable.EveryUpdate(mainWindow.FrameProvider, ct).Subscribe(_ =>
        {
            mainWindow.WindowFitter.Refit();
        }, DebugHelper.LogError(nameof(GalleryAnimationControl), nameof(TweenWhileRefitting)));
        await AnimationsHelper.HeightAnimation(from, to, speed).RunAsync(this, ct).ConfigureAwait(true);
        await cts.CancelAsync().ConfigureAwait(true);
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
