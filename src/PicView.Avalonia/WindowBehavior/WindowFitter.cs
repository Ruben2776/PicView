using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.WindowBehavior;

/// <summary>
/// Fits one window to what its active tab shows, against the screen the window is on: the image's Fit, the window
/// size its Window Mode and AutoFit give it, and where an AutoFit window sits.
/// </summary>
/// <param name="readContentSize">Reads the size of the window's content as it is now; the main view's by default.</param>
/// <param name="readScreen">Reads the screen the window is on now; the one Avalonia reports by default.</param>
public sealed class WindowFitter(MainWindow window, Func<Size>? readContentSize = null,
    Func<ScreenSize>? readScreen = null)
{
    // Used when the window is not on any screen yet
    private static readonly ScreenSize NoScreen = new()
    {
        WorkingAreaWidth = 1920, WorkingAreaHeight = 1050, Width = 1920, Height = 1080, Scaling = 1
    };

    private readonly Func<Size> _readContentSize =
        readContentSize ?? (() => window.UIHelper.GetMainView?.Bounds.Size ?? default);

    private readonly Func<ScreenSize> _readScreen = readScreen ?? (() => ScreenOf(window));

    private ScreenSize? _fittedScreen;

    /// <summary>
    /// The screen the window is on now. Read on the UI thread.
    /// </summary>
    public ScreenSize Screen => _readScreen();

    /// <summary>
    /// Whether the window is on another screen than the one it was last fitted against, as after it was dragged to
    /// another monitor or its scaling changed. False before it is first fitted.
    /// </summary>
    public bool IsOnAnotherScreen() => _fittedScreen is { } fitted && _readScreen() != fitted;

    /// <summary>
    /// Centers the window in the work area of its screen, from its current size. Call on the UI thread.
    /// </summary>
    /// <param name="top">Keeps it at the top of the work area instead of centering it vertically.</param>
    public void Center(bool top = false)
    {
        var position = WindowPlacement.Centered(_readScreen(), window.ClientSize.Width, window.ClientSize.Height, top);
        window.Position = new PixelPoint(position.X, position.Y);
    }

    /// <summary>
    /// Places an AutoFit window after its size changed: centered in its work area with Stay centered, else on its own
    /// center.
    /// </summary>
    /// <returns>Whether the window was placed; not when either size is empty.</returns>
    public bool PlaceAfterResize(Size oldSize, Size newSize)
    {
        if (oldSize.Width is 0 || oldSize.Height is 0 || newSize.Width is 0 || newSize.Height is 0)
        {
            return false;
        }

        var position = WindowPlacement.AfterResize(_readScreen(), new PixelPosition(window.Position.X, window.Position.Y),
            oldSize.Width, oldSize.Height, newSize.Width, newSize.Height, Settings.WindowProperties.KeepCentered);
        window.Position = new PixelPoint(position.X, position.Y);
        return true;
    }

    /// <summary>
    /// Fits the window to what its active tab shows now. It waits instead while a size is not known yet. Called from
    /// another thread, it returns once the image and the window have their size.
    /// </summary>
    public void Refit()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(Refit, DispatcherPriority.Send);
            return;
        }

        if (window.DataContext is not MainWindowViewModel { WindowTabs.ActiveTab.CurrentValue: { } tab } vm ||
            PixelSizes.Of(tab, vm.WindowTabs.SharedCache, Settings.ImageScaling.ShowImageSideBySide) is not { } sizes)
        {
            return;
        }

        Fit(vm, sizes);
    }

    /// <summary>
    /// Fits the window to <paramref name="sizes"/> before it is shown, so it never appears at the wrong size.
    /// Call on the UI thread.
    /// </summary>
    /// <returns>Whether the window could be fitted.</returns>
    public bool FitBeforeShowing(PixelSizes sizes) =>
        window.DataContext is MainWindowViewModel vm && Fit(vm, sizes);

    /// <summary>
    /// Lets the window follow its content when AutoFit is on for it, also when there is no image to fit.
    /// </summary>
    public void FollowContent()
    {
        if (window.DataContext is MainWindowViewModel { WindowModeState.CurrentValue.AutoFit: true })
        {
            window.SizeToContent = SizeToContent.WidthAndHeight;
        }
    }

    /// <summary>
    /// Stops the window from following its content, before it becomes fullscreen or maximized, or turns AutoFit off.
    /// </summary>
    public void KeepSize()
    {
        window.SizeToContent = SizeToContent.Manual;
        if (window.DataContext is MainWindowViewModel vm)
        {
            vm.WindowMaxWidth.Value = vm.WindowMaxHeight.Value = double.NaN;
        }
    }

    private bool Fit(MainWindowViewModel vm, PixelSizes sizes)
    {
        var state = vm.WindowModeState.CurrentValue;
        var contentSize = _readContentSize();
        var screen = _readScreen();
        var fit = ImageFitter.Fit(new FitInput
        {
            ImageWidth = sizes.Width,
            ImageHeight = sizes.Height,
            SecondImageWidth = sizes.SecondWidth,
            SecondImageHeight = sizes.SecondHeight,
            QuarterTurns = sizes.QuarterTurns,
            Mode = state.FitMode,
            ZoomToFit = Settings.ImageScaling.ZoomToFit,
            ScrollEnabled = Settings.Zoom.ScrollEnabled,
            Screen = screen,
            ScreenMargin = Settings.WindowProperties.Margin,
            WindowContentWidth = contentSize.Width,
            WindowContentHeight = contentSize.Height,
            ReservedSpace = GetReservedSpace(vm)
        });
        if (fit.WindowWidth is 0 || fit.WindowHeight is 0)
        {
            return false;
        }

        _fittedScreen = screen;
        vm.WindowTabs.ActiveTab.CurrentValue.InitialZoom.Value = fit.Scale;
        vm.ScrollViewerWidth.Value = fit.ScrollViewerWidth;
        vm.ScrollViewerHeight.Value = fit.ScrollViewerHeight;
        vm.ImageWidth.Value = fit.Width;
        vm.ImageHeight.Value = fit.Height;

        Apply(vm, WindowSizingPlan.For(state.Mode, state.AutoFit, window.IsVisible, fit, screen));
        return true;
    }

    private void Apply(MainWindowViewModel vm, WindowSizingPlan plan)
    {
        window.SizeToContent = plan.SizeToContent;
        vm.WindowMaxWidth.Value = plan.MaxWidth;
        vm.WindowMaxHeight.Value = plan.MaxHeight;
        if (plan.Width is { } width)
        {
            window.Width = width;
        }
        if (plan.Height is { } height)
        {
            window.Height = height;
        }

        if (!plan.SettleToContentAfterRender)
        {
            return;
        }
        Dispatcher.UIThread.Post(() =>
        {
            window.SizeToContent = SizeToContent.WidthAndHeight;
            // Fixes weird window size bug where the window width is at a fixed value
            if (!double.IsNaN(window.Width))
            {
                window.Width = window.Height = double.NaN;
            }
        }, DispatcherPriority.Render);
    }

    private static ScreenSize ScreenOf(Window window)
    {
        if (window.Screens?.ScreenFromWindow(window) is not { } screen)
        {
            return NoScreen;
        }

        var scaling = screen.Scaling;
        return new ScreenSize
        {
            X = screen.Bounds.X / scaling,
            Y = screen.Bounds.Y / scaling,
            Width = screen.Bounds.Width / scaling,
            Height = screen.Bounds.Height / scaling,
            WorkingAreaX = screen.WorkingArea.X / scaling,
            WorkingAreaY = screen.WorkingArea.Y / scaling,
            WorkingAreaWidth = screen.WorkingArea.Width / scaling,
            WorkingAreaHeight = screen.WorkingArea.Height / scaling,
            Scaling = scaling
        };
    }

    private static ReservedSpace GetReservedSpace(MainWindowViewModel vm)
    {
        var (galleryWidth, galleryHeight) = GalleryHelper.GetGallerySize(vm);
        return ReservedSpace.For(vm.WindowTabs.Tabs.CurrentValue.Count, vm.TitlebarHeight.CurrentValue,
            vm.IsTopToolbarShown.Value, Settings.UIProperties.ShowBottomNavBar, galleryWidth, galleryHeight);
    }
}
