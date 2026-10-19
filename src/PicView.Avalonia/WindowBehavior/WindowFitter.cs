using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.WindowBehavior;

/// <summary>
/// Fits one window to what its active tab shows: the image's Fit, and the window size its Window Mode and AutoFit
/// give it.
/// </summary>
/// <param name="readContentSize">Reads the size of the window's content as it is now; the main view's by default.</param>
public sealed class WindowFitter(MainWindow window, Func<Size>? readContentSize = null)
{
    private readonly Func<Size> _readContentSize =
        readContentSize ?? (() => window.UIHelper.GetMainView?.Bounds.Size ?? default);

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
            Screen = ScreenHelper.ScreenSize,
            ScreenMargin = Settings.WindowProperties.Margin,
            WindowContentWidth = contentSize.Width,
            WindowContentHeight = contentSize.Height,
            ReservedSpace = GetReservedSpace(vm)
        });
        if (fit.WindowWidth is 0 || fit.WindowHeight is 0)
        {
            return false;
        }

        vm.WindowTabs.ActiveTab.CurrentValue.InitialZoom.Value = fit.Scale;
        vm.ScrollViewerWidth.Value = fit.ScrollViewerWidth;
        vm.ScrollViewerHeight.Value = fit.ScrollViewerHeight;
        vm.ImageWidth.Value = fit.Width;
        vm.ImageHeight.Value = fit.Height;

        Apply(vm, WindowSizingPlan.For(state.Mode, state.AutoFit, window.IsVisible, fit, ScreenHelper.ScreenSize));
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

    private static ReservedSpace GetReservedSpace(MainWindowViewModel vm)
    {
        var (galleryWidth, galleryHeight) = GalleryHelper.GetGallerySize(vm);
        return ReservedSpace.For(vm.WindowTabs.Tabs.CurrentValue.Count, vm.TitlebarHeight.CurrentValue,
            vm.IsTopToolbarShown.Value, Settings.UIProperties.ShowBottomNavBar, galleryWidth, galleryHeight);
    }
}
