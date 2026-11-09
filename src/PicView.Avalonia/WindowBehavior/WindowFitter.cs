using System.Collections.Specialized;
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
/// size its Window Mode and AutoFit give it, and where an AutoFit window sits. The window follows its Fit inputs
/// (<see cref="Follow"/>) and publishes its Fits; App Start fits before showing (<see cref="FitBeforeShowing"/>,
/// <see cref="FitStartUpMenu"/>).
/// </summary>
/// <param name="readContentSize">Reads the size of the window's content as it is now; the main view's by default.</param>
/// <param name="readScreen">Reads the screen the window is on now; the one Avalonia reports by default.</param>
/// <param name="contentBounds">Publishes the size of the window's content each time it is laid out at another size;
/// the main view's bounds by default.</param>
/// <param name="screenChanges">Publishes each time the window's scaling or position changes, which may mean it is on
/// another screen; the window's own events by default.</param>
public sealed class WindowFitter(MainWindow window, Func<Size>? readContentSize = null,
    Func<ScreenSize>? readScreen = null, Observable<Size>? contentBounds = null,
    Observable<Unit>? screenChanges = null) : IDisposable
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
    private readonly Subject<Size> _placed = new();

    private ScreenSize? _fittedScreen;
    private int _isFitBeforeFramePending;
    private int _isFitAfterLayoutPending;
    private int _isFitForNewImagePending;

    // Set on Dispose; read by Fits posted earlier, which may run on a tab that is disposed by then
    private volatile bool _isDisposed;

    // Whether the window has opened: shown, and past the size it takes as it shows. Read and written on the UI thread
    private bool _hasOpened;

    // The window's size before the first change of its size that the pending placement is for, or null when none is
    // pending. Read and written on the UI thread
    private Size? _sizeBeforePlacement;

    // The active tab whose new image the pending Fit is for; written on the thread that showed it, taken by the Fit
    private TabViewModel? _tabWithNewImage;

    // The tabs that show a new image no applied Fit has followed yet: the active tab until its Fit applies, and a tab
    // that showed it in the background until it is shown and fitted. Each tab keeps its own viewer, which resets on the
    // NewImage Fit, so the debt is per tab. Read and written on the UI thread
    private readonly HashSet<TabViewModel> _tabsOwingNewImageFit = [];

    // The window size the last image's Fit gave, or the start-up menu's before any, which a tab without an image keeps
    private Size? _fittedWindowSize;

    /// <summary>
    /// Each Fit the window applied, with its cause, published on the UI thread after the tab's Fit Scale, the view
    /// model's sizes and the window's sizing were set. A Fit that could not be made publishes nothing.
    /// </summary>
    public Observable<Fitted> Fits => _fits;

    /// <summary>
    /// The window's new client size each time it was moved after a change of its size, published on the UI thread
    /// once it sits where its Placement puts it.
    /// </summary>
    public Observable<Size> Placed => _placed;

    /// <summary>
    /// Whether <paramref name="tab"/> showed a new image that no applied Fit has followed yet. Read on the UI thread.
    /// </summary>
    internal bool IsNewImageFitOwed(TabViewModel tab) => _tabsOwingNewImageFit.Contains(tab);

    /// <summary>
    /// Ends <see cref="Fits"/> and <see cref="Placed"/>, and stops the Fits already posted, which may run after the
    /// window tore down its tabs.
    /// </summary>
    public void Dispose()
    {
        _isDisposed = true;
        _fits.Dispose();
        _placed.Dispose();
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
    /// Moves the window to the origin of its screen, where a fullscreen window sits. Call on the UI thread.
    /// </summary>
    public void MoveToScreenOrigin()
    {
        var position = WindowPlacement.ScreenOrigin(_readScreen());
        window.Position = new PixelPoint(position.X, position.Y);
    }

    // An AutoFit window is placed after each change of its size, a Fit included. A window that has not opened is not:
    // while hidden, or while it shows and takes the size it was given, its opener has placed it, or App Start centers
    // it as it shows. Nor is one that is not Normal, keeps its size or is between Window Modes. Normal also natively:
    // the operating system maximizes a window, as Win+Up does, before its Window Mode follows. Decided one dispatcher
    // turn after the change, since Windows sets the new size before the native state for one WM_SIZE, and a drag of
    // the border turns AutoFit off only once its size is set. At Send, so no frame shows the window unplaced. Changes
    // until then give one placement, from the size before the first to the size after the last
    private void PlaceAfterResizeSoon(MainWindowViewModel vm, Size oldSize)
    {
        // A change before the window opened is its opener's, even when the decision runs once it has
        if (!_hasOpened || _sizeBeforePlacement is not null)
        {
            return;
        }

        _sizeBeforePlacement = oldSize;
        Dispatcher.UIThread.Post(() =>
        {
            var sizeBefore = _sizeBeforePlacement.GetValueOrDefault();
            _sizeBeforePlacement = null;
            var state = vm.WindowModeState.CurrentValue;
            if (_isDisposed || !_hasOpened || state.Mode is not WindowMode.Normal ||
                window.WindowState is not WindowState.Normal || !state.AutoFit || window.IsChangingWindowState ||
                _placed.IsDisposed)
            {
                return;
            }

            var newSize = window.ClientSize;
            if (PlaceAfterResize(sizeBefore, newSize))
            {
                _placed.OnNext(newSize);
            }
        }, DispatcherPriority.Send);
    }

    // Centered in its work area with Stay Centered, else on its own center. Whether the window moved; not when either
    // size is empty
    private bool PlaceAfterResize(Size oldSize, Size newSize)
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
    /// Fits the window again whenever one of the Fit inputs it watches changes: the active tab shows a new image,
    /// another tab becomes active, the active tab is turned or flipped, the tab strip or a bar shows or hides, the
    /// Window Mode changes, zoom to fit, scrolling or side by side turns on or off, the active tab's gallery changes
    /// its mode, dock, size or stretch, or whether its Expanded Gallery returns to docked, the active tab changes its
    /// Tab View, as a crop opens or closes, or, in a Fixed Window, its content is laid out at another size, or the
    /// window's scaling or position puts it on another screen than the one it was last fitted against. A new image is
    /// fitted at the highest priority and published with the cause <see cref="FitCause.NewImage"/>; a tab that shows
    /// one in the background owes that cause to the first Fit for it once it is active. Other changes that come together give one Fit, before the next
    /// frame is drawn, so the window never shows them at the old size. Without AutoFit the Fit reads the laid-out
    /// view, so it is fitted once more after layout. An opened AutoFit window in Normal is also placed after each change
    /// of its size, a Fit included, and published as <see cref="Placed"/> when that moved it, which places it when
    /// AutoFit turns on too. An AutoFit window that returns to Normal from fullscreen or maximized is centered once in
    /// the work area of its screen.
    /// </summary>
    /// <returns>What stops following the inputs.</returns>
    public IDisposable Follow()
    {
        if (window.DataContext is not MainWindowViewModel vm)
        {
            return Disposable.Empty;
        }

        // A window followed once it is shown has opened already
        _hasOpened = window.IsVisible;
        return Disposable.Combine(
            Observable.FromEventHandler(h => window.Opened += h, h => window.Opened -= h)
                .Subscribe(_ => _hasOpened = true),
            FollowEveryTabsImages(vm),
            vm.WindowTabs.ActiveTab.Skip(1).Subscribe(_ => FitSoon()),
            // Only the shown tab's Orientation, so turning a tab in the background fits nothing
            vm.WindowTabs.ActiveTab.Select(static tab => tab.Orientation.Current.Skip(1)).Switch()
                .Subscribe(_ => FitSoon()),
            vm.WindowTabs.IsTabStripShown.Skip(1).Subscribe(_ => FitSoon()),
            vm.Interface.AsObservable().Skip(1).Subscribe(_ => FitSoon()),
            vm.WindowModeState.Skip(1).Subscribe(_ => FitSoon()),
            vm.WindowModeState.Pairwise().Subscribe(pair => CenterIfBackToNormalWithAutoFit(vm, pair.Previous,
                pair.Current)),
            vm.GlobalSettings.IsZoomedToFit.Skip(1).Subscribe(_ => FitSoon()),
            vm.GlobalSettings.IsScrollingEnabled.Skip(1).Subscribe(_ => FitSoon()),
            vm.GlobalSettings.IsSideBySide.Skip(1).Subscribe(_ => FitSoon()),
            vm.GlobalSettings.WindowMargin.Skip(1).Subscribe(_ => FitSoon()),
            // Only the shown tab's gallery, so a gallery docking in a background tab fits nothing. It may change off the
            // UI thread, as FitSoon allows
            vm.WindowTabs.ActiveTab.Select(static tab => tab.Gallery.Appearance.AsObservable().Skip(1)).Switch()
                .Subscribe(_ => FitSoon()),
            // An Expanded Gallery reserves the docked thickness it returns to, so choosing whether it returns to docked
            // changes the Reserved Space without changing its appearance
            vm.WindowTabs.ActiveTab.Select(static tab => tab.Gallery.Switcher.IsDocked.Skip(1)).Switch()
                .Subscribe(_ => FitSoon()),
            // Only the shown tab's Tab View. A crop hides the docked gallery, so it changes the Reserved Space. The kind
            // may change off the UI thread, as a load does, which FitSoon allows. The Fit posts at Normal, before the
            // crop control lays out at Render, so the control meets its size
            vm.WindowTabs.ActiveTab.Select(static tab => tab.ViewKind.AsObservable().Skip(1)).Switch()
                .Subscribe(_ => FitSoon()),
            FollowContentBounds(vm),
            // Only this window's size, as it changes on the UI thread
            window.GetPropertyChangedObservable(Window.ClientSizeProperty).ToObservable()
                .Subscribe(change =>
                    PlaceAfterResizeSoon(vm, ((AvaloniaPropertyChangedEventArgs<Size>)change).OldValue
                        .GetValueOrDefault())),
            // A move within the same screen fits nothing; a drag to another monitor or a scaling change does
            (screenChanges ?? ScreenChangesOf(window)).Subscribe(_ =>
            {
                if (IsOnAnotherScreen())
                {
                    FitSoon();
                }
            }));
    }

    // An AutoFit window that returns to Normal from fullscreen or maximized is centered once in the work area of its
    // screen; the operating system's restore moves it while it is between Window Modes, which the after-resize rule
    // leaves alone. Posted by the change itself, not by the Fit that follows it, since a window showing the start-up
    // menu fits no image. At Loaded, below Render, so it reads the window's size after the layout that follows that
    // Fit: a shown AutoFit window takes the Fit's size as it is laid out, not as the Fit runs
    private void CenterIfBackToNormalWithAutoFit(MainWindowViewModel vm, WindowModeState previous,
        WindowModeState next)
    {
        if (previous.Mode is not (WindowMode.Fullscreen or WindowMode.Maximized) ||
            next is not { Mode: WindowMode.Normal, AutoFit: true })
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (!_isDisposed && window.IsVisible && window.WindowState is WindowState.Normal &&
                vm.WindowModeState.CurrentValue is { Mode: WindowMode.Normal, AutoFit: true })
            {
                Center();
            }
        }, DispatcherPriority.Loaded);
    }

    private static Observable<Unit> ScreenChangesOf(MainWindow window) =>
        Observable.Merge(
            Observable.FromEvent<EventHandler, Unit>(onNext => (_, _) => onNext(Unit.Default),
                h => window.ScalingChanged += h, h => window.ScalingChanged -= h),
            Observable.FromEventHandler<PixelPointEventArgs>(h => window.PositionChanged += h,
                h => window.PositionChanged -= h).Select(static _ => Unit.Default));

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

    // Every tab of the window, not only the active one: a tab that shows a new image in the background, as when the
    // folder watcher replaces its shown file, reset its Zoom Factor, so its viewer must reset when it is shown again.
    // Tabs come and go on the UI thread, as the tab strip shows them
    private IDisposable FollowEveryTabsImages(MainWindowViewModel vm)
    {
        var tabs = vm.WindowTabs.Tabs.Value;
        var watched = new Dictionary<TabViewModel, IDisposable>();

        void Watch(TabViewModel tab)
        {
            if (watched.ContainsKey(tab))
            {
                return;
            }

            watched[tab] = tab.Shown.Subscribe(change =>
            {
                if (change is ShownChange.Image)
                {
                    OnNewImage(vm, tab);
                }
            });
        }

        // Later, not as the tab is removed: a reorder removes a tab and inserts it again, and keeps what it owes
        void ForgetTabsThatLeft()
        {
            List<TabViewModel>? left = null;
            foreach (var tab in watched.Keys)
            {
                if (!tabs.Contains(tab))
                {
                    (left ??= []).Add(tab);
                }
            }

            if (left is null)
            {
                return;
            }

            foreach (var tab in left)
            {
                watched.Remove(tab, out var subscription);
                subscription?.Dispose();
                _tabsOwingNewImageFit.Remove(tab);
            }
        }

        void OnTabsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action is NotifyCollectionChangedAction.Reset)
            {
                foreach (var tab in tabs)
                {
                    Watch(tab);
                }
            }
            else if (e.NewItems is { } added)
            {
                foreach (TabViewModel tab in added)
                {
                    Watch(tab);
                }
            }

            if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace
                or NotifyCollectionChangedAction.Reset)
            {
                Dispatcher.UIThread.Post(ForgetTabsThatLeft);
            }
        }

        foreach (var tab in tabs)
        {
            Watch(tab);
        }

        tabs.CollectionChanged += OnTabsChanged;
        return Disposable.Create(() =>
        {
            tabs.CollectionChanged -= OnTabsChanged;
            foreach (var subscription in watched.Values)
            {
                subscription.Dispose();
            }

            watched.Clear();
            _tabsOwingNewImageFit.Clear();
        });
    }

    // The tab may show its image on a thread-pool thread, as a Load does. Posted, never invoked, so that thread does
    // not wait for the window, and at the highest priority, so the Fit runs before work posted after the image showed
    private void OnNewImage(MainWindowViewModel vm, TabViewModel tab)
    {
        if (!ReferenceEquals(tab, vm.WindowTabs.ActiveTab.CurrentValue))
        {
            // Rare, and fits nothing: the tab is only marked as owing its Fit
            Dispatcher.UIThread.Post(() => FitNewImage(tab), DispatcherPriority.Send);
            return;
        }

        // Many images of the active tab, as a held key shows them, give one Fit
        var previous = Interlocked.Exchange(ref _tabWithNewImage, tab);
        if (previous is not null && !ReferenceEquals(previous, tab))
        {
            // Another tab's image whose Fit has not run yet, as when the user switched tabs meanwhile: it still owes it
            Dispatcher.UIThread.Post(() => FitNewImage(previous), DispatcherPriority.Send);
        }

        if (Interlocked.Exchange(ref _isFitForNewImagePending, 1) is not 0)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            Interlocked.Exchange(ref _isFitForNewImagePending, 0);
            FitNewImage(Interlocked.Exchange(ref _tabWithNewImage, null));
        }, DispatcherPriority.Send);
    }

    private void FitNewImage(TabViewModel? tab)
    {
        if (_isDisposed || window.DataContext is not MainWindowViewModel vm || tab is null)
        {
            return;
        }

        // Owed until a Fit for the tab applies: the tab already reset its Zoom Factor, so its viewer must still reset
        // its control to meet it, also when this Fit cannot be made, or the tab is not shown, and a later Fit is the
        // first that applies for it. A tab closed or moved out meanwhile owes nothing
        if (!vm.WindowTabs.Tabs.Value.Contains(tab))
        {
            return;
        }

        _tabsOwingNewImageFit.Add(tab);
        if (!ReferenceEquals(tab, vm.WindowTabs.ActiveTab.CurrentValue) ||
            vm.WindowModeState.CurrentValue.Mode is WindowMode.Minimized ||
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

    // Published with the cause NewImage when it is the first Fit that applies for the active tab since that tab showed a
    // new image, whatever asked for it, as the switch to a tab that showed it in the background; else with Input
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
            ScreenMargin = vm.GlobalSettings.WindowMargin.CurrentValue,
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
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        tab.Zoom.SetFitScale(fit.Scale);
        vm.ScrollViewerWidth.Value = fit.ScrollViewerWidth;
        vm.ScrollViewerHeight.Value = fit.ScrollViewerHeight;
        vm.ImageWidth.Value = fit.Width;
        vm.ImageHeight.Value = fit.Height;

        Apply(vm, WindowSizingPlan.For(state.Mode, state.AutoFit, window.IsVisible, fit, screen));
        var cause = _tabsOwingNewImageFit.Remove(tab) ? FitCause.NewImage : FitCause.Input;
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
        var gallery = vm.WindowTabs.ActiveTab.CurrentValue.Gallery;
        var (galleryWidth, galleryHeight) =
            GalleryDocking.ReservedBy(gallery.Appearance.Value, gallery.Switcher.IsDocked.CurrentValue);
        // What the window shows, not the settings: hiding the interface hides the bottom bar but keeps its setting
        return ReservedSpace.For(vm.WindowTabs.Tabs.CurrentValue.Count, vm.Interface.Value, galleryWidth,
            galleryHeight);
    }
}
