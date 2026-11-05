using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Core.Gallery;
using PicView.Avalonia.Views.UC;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.WindowBehavior;

/// <summary>
/// Fits one window to what its active tab shows, against the screen the window is on: the image's Fit, the window
/// size its Window Mode and AutoFit give it, and where an AutoFit window sits.
/// </summary>
/// <param name="readContentSize">Reads the size of the window's content as it is now; the main view's by default.</param>
/// <param name="readScreen">Reads the screen the window is on now; the one Avalonia reports by default.</param>
/// <param name="contentBounds">Publishes the size of the window's content each time it is laid out at another size;
/// the main view's bounds by default.</param>
public sealed class WindowFitter(MainWindow window, Func<Size>? readContentSize = null,
    Func<ScreenSize>? readScreen = null, Observable<Size>? contentBounds = null) : IDisposable
{
    // Used when the window is not on any screen yet
    private static readonly ScreenSize NoScreen = new()
    {
        WorkingAreaWidth = 1920, WorkingAreaHeight = 1050, Width = 1920, Height = 1080, Scaling = 1
    };

    private readonly Func<Size> _readContentSize =
        readContentSize ?? (() => window.UIHelper.GetMainView?.Bounds.Size ?? default);

    private readonly Func<ScreenSize> _readScreen = readScreen ?? (() =>
        ScreenOf(window));

    private readonly Subject<Fitted> _fits = new();

    private ScreenSize? _fittedScreen;
    private int _isFitBeforeFramePending;
    private int _isFitAfterLayoutPending;
    private int _isFitForNewImagePending;

    // Set on Dispose; read by Fits posted earlier, which may run on a tab that is disposed by then
    private volatile bool _isDisposed;

    // The tab whose new image the pending Fit is for; written on the thread that showed it
    private TabViewModel? _tabWithNewImage;

    // Whether a new image is shown that no applied Fit has followed yet. Read and written on the UI thread
    private bool _isNewImageFitOwed;

    // The window size the last image's Fit gave, or the start-up menu's before any, which a tab without an image keeps
    private Size? _fittedWindowSize;

    /// <summary>
    /// Each Fit the window applied, with its cause, published on the UI thread after the tab's Fit Scale, the view
    /// model's sizes and the window's sizing were set. A Fit that could not be made publishes nothing.
    /// </summary>
    public Observable<Fitted> Fits => _fits;

    /// <summary>
    /// Ends <see cref="Fits"/> and stops the Fits already posted, which may run after the window tore down its tabs.
    /// </summary>
    public void Dispose()
    {
        _isDisposed = true;
        _fits.Dispose();
    }

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
        var prevPos = window.Position;
        window.Position = new PixelPoint(position.X, position.Y);
        return window.Position != prevPos;
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
    /// Fits the window again whenever one of the Fit inputs it watches changes: the active tab shows a new image,
    /// another tab becomes active, the active tab is turned or flipped, the tab strip or a bar shows or hides, the
    /// Window Mode changes, zoom to fit, scrolling or side by side turns on or off, the active tab's gallery changes
    /// its mode, dock, size or stretch, the active tab changes its Tab View, as a crop opens or closes, or, in a Fixed
    /// Window, its content is laid out at another size. A new image is fitted at the highest priority and published
    /// with the cause <see cref="FitCause.NewImage"/>. Other changes that come together give one Fit, before the next
    /// frame is drawn, so the window never shows them at the old size. Without AutoFit the Fit reads the laid-out
    /// view, so it is fitted once more after layout.
    /// </summary>
    /// <remarks>Screen changes and resizing by hand still ask for a Fit themselves.</remarks>
    /// <returns>What stops following the inputs.</returns>
    public IDisposable Follow()
    {
        if (window.DataContext is not MainWindowViewModel vm)
        {
            return Disposable.Empty;
        }

        return Disposable.Combine(
            // Only the shown tab's images, so a tab loading in the background fits nothing
            vm.WindowTabs.ActiveTab.Select(static tab =>
                    tab.Shown.Where(static change => change is ShownChange.Image).Select(_ => tab)).Switch()
                .Subscribe(FitForNewImageSoon),
            vm.WindowTabs.ActiveTab.Skip(1).Subscribe(_ => FitSoon()),
            // Only the shown tab's Orientation, so turning a tab in the background fits nothing
            vm.WindowTabs.ActiveTab.Select(static tab => tab.Orientation.Current.Skip(1)).Switch()
                .Subscribe(_ => FitSoon()),
            vm.WindowTabs.IsTabStripShown.Skip(1).Subscribe(_ => FitSoon()),
            vm.Interface.AsObservable().Skip(1).Subscribe(_ => FitSoon()),
            vm.WindowModeState.Skip(1).Subscribe(_ => FitSoon()),
            vm.GlobalSettings.IsZoomedToFit.Skip(1).Subscribe(_ => FitSoon()),
            vm.GlobalSettings.IsScrollingEnabled.Skip(1).Subscribe(_ => FitSoon()),
            vm.GlobalSettings.IsSideBySide.Skip(1).Subscribe(_ => FitSoon()),
            // Only the shown tab's gallery, so a gallery docking in a background tab fits nothing. It may change off the
            // UI thread, as FitSoon allows
            vm.WindowTabs.ActiveTab.Select(static tab => tab.Gallery.Appearance.AsObservable().Skip(1)).Switch()
                .Subscribe(_ => FitSoon()),
            // Only the shown tab's Tab View. A crop hides the docked gallery, so it changes the Reserved Space. The kind
            // may change off the UI thread, as a load does, which FitSoon allows. The Fit posts at Normal, before the
            // crop control lays out at Render, so the control meets its size
            vm.WindowTabs.ActiveTab.Select(static tab => tab.ViewKind.AsObservable().Skip(1)).Switch()
                .Subscribe(_ => FitSoon()),
            FollowContentBounds(vm));
    }

    // A Fixed Window fits its image to the room its content has, so it fits again when the content is laid out at
    // another size. An AutoFit window's content follows the window it sized itself, so it never listens
    private IDisposable FollowContentBounds(MainWindowViewModel vm)
    {
        void FitIfFixedWindow()
        {
            if (vm.WindowModeState.CurrentValue.FitMode is FitMode.FixedWindow)
            {
                FitSoon();
            }
        }

        if (contentBounds is not null)
        {
            return contentBounds.Subscribe(_ => FitIfFixedWindow());
        }

        var subscription = new SerialDisposable();
        void ObserveMainView()
        {
            if (window.UIHelper.GetMainView is { } view)
            {
                subscription.Disposable = view.GetObservable(Visual.BoundsProperty).ToObservable()
                    .Subscribe(_ => FitIfFixedWindow());
            }
        }

        if (window.UIHelper.GetMainView is not null)
        {
            ObserveMainView();
            return subscription;
        }

        // The main view is created when the window opens
        void OnOpened(object? sender, EventArgs e)
        {
            window.Opened -= OnOpened;
            ObserveMainView();
        }

        window.Opened += OnOpened;
        return Disposable.Combine(subscription, Disposable.Create(() => window.Opened -= OnOpened));
    }

    // The tab may show its image on a thread-pool thread, as a Load does. Posted, never invoked, so that thread does
    // not wait for the window, and at the highest priority, so the Fit runs before work posted after the image showed
    private void FitForNewImageSoon(TabViewModel tab)
    {
        Volatile.Write(ref _tabWithNewImage, tab);
        if (Interlocked.Exchange(ref _isFitForNewImagePending, 1) is not 0)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            Interlocked.Exchange(ref _isFitForNewImagePending, 0);
            FitNewImage(Volatile.Read(ref _tabWithNewImage));
        }, DispatcherPriority.Send);
    }

    private void FitNewImage(TabViewModel? tab)
    {
        // A tab closed meanwhile is no longer the active one; switching to another tab fits that one as an input
        if (_isDisposed || window.DataContext is not MainWindowViewModel vm || tab is null ||
            !ReferenceEquals(tab, vm.WindowTabs.ActiveTab.CurrentValue))
        {
            return;
        }

        // Owed until a Fit applies: the tab already reset its Zoom Factor, so the viewer must still reset its control
        // to meet it, also when this Fit cannot be made and a later input's Fit is the first that applies
        _isNewImageFitOwed = true;
        if (vm.WindowModeState.CurrentValue.Mode is WindowMode.Minimized ||
            PixelSizes.Of(tab, vm.WindowTabs.SharedCache, Settings.ImageScaling.ShowImageSideBySide) is not { } sizes)
        {
            return;
        }

        Fit(vm, sizes);
    }

    private void FitSoon()
    {
        if (Interlocked.Exchange(ref _isFitBeforeFramePending, 1) is 0)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Interlocked.Exchange(ref _isFitBeforeFramePending, 0);
                FitWhatIsShown(afterLayout: false);
            }, DispatcherPriority.Normal);
        }

        if (Interlocked.Exchange(ref _isFitAfterLayoutPending, 1) is 0)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Interlocked.Exchange(ref _isFitAfterLayoutPending, 0);
                FitWhatIsShown(afterLayout: true);
            }, DispatcherPriority.Background);
        }
    }

    private void FitWhatIsShown(bool afterLayout)
    {
        if (_isDisposed || window.DataContext is not MainWindowViewModel { WindowTabs.ActiveTab.CurrentValue: { } tab } vm)
        {
            return;
        }

        var state = vm.WindowModeState.CurrentValue;
        // Nothing is resized while the window is minimized, and only a Fit that reads the laid-out view needs layout
        if (state.Mode is WindowMode.Minimized || afterLayout && state.FitMode is not FitMode.FixedWindow)
        {
            return;
        }

        if (PixelSizes.Of(tab, vm.WindowTabs.SharedCache, Settings.ImageScaling.ShowImageSideBySide) is { } sizes)
        {
            Fit(vm, sizes);
        }
        else if (tab.CurrentView.Value is StartUpMenu menu)
        {
            FitStartUpMenu(menu);
        }
    }

    /// <summary>
    /// Sizes a tab's start-up menu so the window keeps its size: in an AutoFit window it fills the size the last
    /// image gave the window, less the Reserved Space as it is now, so the tab strip it shows takes room from the
    /// menu. An AutoFit window that is not shown yet and has fitted no image opens at the start-up menu's own size.
    /// Otherwise it stretches to fill the window. Call on the UI thread, before the menu is laid out, as when it
    /// is created.
    /// </summary>
    public void FitStartUpMenu(StartUpMenu menu)
    {
        if (window.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (vm.WindowModeState.CurrentValue.FitMode is not FitMode.AutoFit)
        {
            menu.Width = menu.Height = double.NaN;
            return;
        }

        if (_fittedWindowSize is null && !window.IsVisible)
        {
            var (startUpWidth, startUpHeight) = ImageFitter.StartUpMenuWindowSize(_readScreen());
            _fittedWindowSize = new Size(startUpWidth, startUpHeight);
        }

        // Not the window's size as it is: it follows its content, so it may already have shrunk to an unsized menu
        var windowSize = _fittedWindowSize ?? window.ClientSize;
        if (windowSize.Width <= 0 || windowSize.Height <= 0)
        {
            return;
        }

        var (width, height) = ImageFitter.WindowAvailableArea(windowSize.Width, windowSize.Height,
            GetReservedSpace(vm));
        menu.Width = width;
        menu.Height = height;
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

    // Published with the cause NewImage when it is the first Fit that applies since a new image showed, whatever asked
    // for it, else with the cause Input
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
        _fittedWindowSize = new Size(fit.WindowWidth, fit.WindowHeight);
        vm.WindowTabs.ActiveTab.CurrentValue.Zoom.SetFitScale(fit.Scale);
        vm.ScrollViewerWidth.Value = fit.ScrollViewerWidth;
        vm.ScrollViewerHeight.Value = fit.ScrollViewerHeight;
        vm.ImageWidth.Value = fit.Width;
        vm.ImageHeight.Value = fit.Height;

        Apply(vm, WindowSizingPlan.For(state.Mode, state.AutoFit, window.IsVisible, fit, screen));
        var cause = _isNewImageFitOwed ? FitCause.NewImage : FitCause.Input;
        _isNewImageFitOwed = false;
        // A Fit posted before the window disposed its fitter still runs, and must not throw
        if (_fits.IsDisposed)
        {
            return true;
        }

        _fits.OnNext(new Fitted(fit, cause));
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
        var (galleryWidth, galleryHeight) =
            GalleryDocking.ReservedBy(vm.WindowTabs.ActiveTab.CurrentValue.Gallery.Appearance.Value);
        // What the window shows, not the settings: hiding the interface hides the bottom bar but keeps its setting
        return ReservedSpace.For(vm.WindowTabs.Tabs.CurrentValue.Count, vm.Interface.Value, galleryWidth,
            galleryHeight);
    }
}
