using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.Views.UC;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Input;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Services;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.FileSorting;
using PicView.Core.Navigation;
using PicView.Core.Titles;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.StartUp;

/// <summary>
/// Opens each Main Window, whichever way it opens (at App Start, as a new window or as a detached tab), and closes it.
/// Every step acts on the window being opened or closed, never on whichever window is active.
/// </summary>
public sealed class MainWindowLifecycle
{
    private readonly CoreViewModel _core;
    private readonly IWindowProvider _provider;
    private readonly AppLifecycle _app;
    private readonly List<MainWindow> _open = [];
    private readonly HashSet<MainWindow> _closing = [];

    internal MainWindowLifecycle(CoreViewModel core, IWindowProvider provider, AppLifecycle app)
    {
        _core = core;
        _provider = provider;
        _app = app;
    }

    /// <summary>
    /// Opens the App Windows, shared by every Main Window.
    /// </summary>
    internal SecondaryWindowOpener AppWindows => _app.AppWindows;

    /// <inheritdoc cref="AppLifecycle.AppToolWindows"/>
    internal SecondaryWindowOpener AppToolWindows => _app.AppToolWindows;

    /// <inheritdoc cref="AppLifecycle.ShutdownIfNothingIsOpenAsync"/>
    internal Task ShutdownIfNothingIsOpenAsync() => _app.ShutdownIfNothingIsOpenAsync();

    /// <inheritdoc cref="AppLifecycle.RestartAsync"/>
    internal Task RestartAsync(Func<Task>? beforeStart = null) => _app.RestartAsync(beforeStart);

    /// <summary>
    /// The Main Windows that are open, in the order they opened.
    /// </summary>
    public IReadOnlyList<MainWindow> Open => _open;

    /// <summary>
    /// The Main Window the user activated last, which a later launch opens in.
    /// </summary>
    public MainWindow? MostRecentlyActive { get; private set; }

    /// <summary>
    /// Opens a new Main Window whose tab derives the start-up menu.
    /// </summary>
    /// <returns>Completes once the window is set up, after it is shown.</returns>
    public async Task<MainWindow> OpenNewAsync(MainWindow senderWindow)
    {
        var window = await Dispatcher.UIThread.InvokeAsync(() => Create());
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            WindowFunctions.SaveSize(senderWindow);
            WindowFunctions.HandleWindowScalingMode(window, isAppStart: false);
            window.Show();
        });

        await SetUpShownWindow(window, isAppStart: false).ConfigureAwait(true);
        return window;
    }

    /// <summary>
    /// Opens a Main Window with a tab moved out of another one, placed where the tab was dropped.
    /// </summary>
    /// <param name="tab">The tab, already taken out of its window, that the new window opens with.</param>
    /// <returns>Completes once the window is set up, after it is shown.</returns>
    public async Task<MainWindow> OpenForMovedTabAsync(TabViewModel tab, PixelPoint position, double width, double height)
    {
        var window = Create(tab);
        window.Position = position;
        window.Width = width;
        window.Height = height;
        WindowFunctions.HandleWindowScalingMode(window, isAppStart: false, adjustPos: false);
        window.Show();
        await SetUpShownWindow(window, isAppStart: false).ConfigureAwait(true);
        return window;
    }

    /// <summary>
    /// Opens the first Main Window at App Start, following the order in <see cref="QuickLoad"/>.
    /// </summary>
    /// <param name="source">What to open, or null for the start-up menu or the last file.</param>
    /// <param name="launchFileArrived">When given, a launch file may still arrive; the start-up menu or last file is
    /// shown only if none has by the time the window is idle.</param>
    /// <returns>The window, and a task that completes once it is set up.</returns>
    internal (MainWindow Window, Task SetUp) OpenAtAppStart(string? source, Func<bool>? launchFileArrived)
    {
        // A window that opens a source says Loading from the start, so no menu is made for it; QuickLoad replaces the
        // ticket with its own
        var opensSource = source is not null || (launchFileArrived is null && Settings.StartUp.OpenLastFile &&
                                                 !string.IsNullOrWhiteSpace(Settings.StartUp.LastFile));
        var window = Create(null, false, opensSource);
        WindowFunctions.HandleWindowScalingMode(window, isAppStart: true, adjustPos: !Settings.WindowProperties.AutoFit);
        if (source is not null)
        {
            OpenStartSource(window, source, continueFromLeftOff: false);
        }
        else if (launchFileArrived is null)
        {
            ShowStartUpMenuOrLastFile(window);
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!launchFileArrived())
                {
                    ShowStartUpMenuOrLastFile(window);
                }
            }, DispatcherPriority.Background);
        }

        return (window, SetUpShownWindow(window, isAppStart: true));
    }

    /// <summary>
    /// Opens the source PicView was launched with in the first Main Window, sizing it first with AutoFit. QuickLoad says
    /// Loading from the start until the source shows, and the tab's Load from when it begins; a start without a source
    /// never enters the phase, so its titles say No image, while a start with a source enters it at creation
    /// (ADR-0022).
    /// </summary>
    internal void OpenStartSource(MainWindow window, string source, bool continueFromLeftOff)
    {
        _ = Task.Run(() => SourceOpener.OpenAsync(window, _core, source,
            new OpenOptions(IsStartup: true, ContinueFromLeftOff: continueFromLeftOff)).AsTask());
    }

    private void ShowStartUpMenuOrLastFile(MainWindow window)
    {
        if (Settings.StartUp.OpenLastFile && !string.IsNullOrWhiteSpace(Settings.StartUp.LastFile))
        {
            OpenStartSource(window, Settings.StartUp.LastFile, continueFromLeftOff: true);
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            // Only the first menu at App Start is fitted to its opening size, before the window is shown, so an AutoFit
            // window opens at the menu's size; a window opened later takes the size of the window that opened it
            if (ViewModelOf(window).WindowTabs.ActiveTab.CurrentValue.CurrentView.Value is StartUpMenu menu)
            {
                window.WindowFitter.FitStartUpMenu(menu);
            }
            window.Show();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = window;
            }
        }, DispatcherPriority.Send);

        if (!Settings.StartUp.OpenLastFile && window.WindowModeState.AutoFit && OperatingSystem.IsMacOS())
        {
            WindowFunctions.CenterWindowOnScreen(window);
        }
    }

    /// <summary>
    /// Closes a Main Window and its Tool Windows: remembers its file and size, cleans up its tabs and saves what every
    /// Main Window shares. Closing the last one runs App Shutdown.
    /// </summary>
    public Task CloseAsync(MainWindow window) => CloseAsync(window, closeNatively: true);

    /// <param name="closeNatively">False when the window is already closing natively, as when the user closes it.</param>
    internal async Task CloseAsync(MainWindow window, bool closeNatively)
    {
        if (!_closing.Add(window) || window.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var remembers = !_app.IsShuttingDown;
        if (remembers)
        {
            WindowFunctions.SaveSize(window);
        }

        await Dispatcher.UIThread.InvokeAsync(window.Hide);
        if (remembers)
        {
            RememberFile(window);
        }
        // On macOS most Tool Windows have no owner, so they would outlive their Main Window
        window.MainWindowInitializer.CloseToolWindows();
        vm.WindowTabs.TearDown();

        _core.MainWindows.MainWindows.Remove(vm);
        _open.Remove(window);
        if (MostRecentlyActive == window)
        {
            MostRecentlyActive = _open.Count > 0 ? _open[^1] : null;
        }

        if (closeNatively)
        {
            window.Close();
        }

        if (_open.Count == 0 && !_app.IsShuttingDown)
        {
            await _app.ShutdownIfNothingIsOpenAsync().ConfigureAwait(true);
        }
        else if (!_app.IsShuttingDown)
        {
            // Only App Shutdown deletes the temp files, since a tab in another window may still show one
            await _app.SaveSharedFilesAsync().ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Remembers what the window shows, for the next App Start to open when it opens the last file.
    /// </summary>
    internal static void RememberFile(MainWindow window)
    {
        if (window.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        LastFile.Remember(vm.WindowTabs.ActiveTab.CurrentValue);
    }

    private MainWindow Create(TabViewModel? firstTab = null, bool setMainWindow = true, bool opensSource = false)
    {
        var window = _provider.CreateMainWindow(firstTab);
        var vm = ViewModelOf(window);
        // A window keeps the view model it is created with, so hand it over once here
        window.ViewModel = vm;
        vm.WindowModeSwitcher = window.WindowModeSwitcher;
        vm.WindowTabs.SetHookups(new NavigationSubscriptions(window, vm));
        if (opensSource)
        {
            // Before the factory attaches, so no menu is made for a window that opens a source
            vm.WindowTabs.ActiveTab.Value.SetLoading();
        }

        vm.WindowTabs.SetViewFactory(new TabViews(window));
        window.Disposables.Add(ShowLoadingIndicatorWhileLoading(vm));
        _core.MainWindows.MainWindows.Add(vm);
        _core.MainWindows.ActiveWindow.Value = vm;
        _open.Add(window);
        MostRecentlyActive = window;
        window.Lifecycle = this;
        window.Activated += (_, _) => MostRecentlyActive = window;
        window.Closed += (_, _) =>
        {
            _open.Remove(window);
            _closing.Remove(window);
        };
        if (setMainWindow && Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // If setting the main window with no window shown, it will force the window to be shown.
            desktop.MainWindow = window;
        }

        return window;
    }

    /// <summary>
    /// Shows the window's loading indicator while the titlebar is hidden and its active tab's Title Phase is Loading or
    /// Downloading. The titlebar already says Loading or Downloading, so the indicator shows that text's state when the
    /// titlebar is hidden. It follows the phase, not the tab's Load, so a startup open, which says Loading while it
    /// decodes before its Load begins, shows it too.
    /// </summary>
    private static IDisposable ShowLoadingIndicatorWhileLoading(MainWindowViewModel vm)
    {
        var saysLoading = vm.WindowTabs.ActiveTab
            .Select(static tab => SaysLoading(tab.Titles))
            .Switch();
        return saysLoading.CombineLatest(vm.IsTopToolbarShown.AsObservable(),
                static (loading, titlebarShown) => loading && !titlebarShown)
            .Subscribe(show => vm.IsLoadingIndicatorShown.Value = show);
    }

    /// <summary>
    /// Whether <paramref name="titles"/> say Loading or Downloading, now and on each change. A tab that never loads
    /// stays in no phase, so it says false.
    /// </summary>
    private static Observable<bool> SaysLoading(TabTitles titles) =>
        Observable.Create<bool, TabTitles>(titles, static (observer, titles) =>
        {
            PropertyChangedEventHandler onChanged = (_, _) => PublishPhase(observer, titles);
            titles.PropertyChanged += onChanged;
            // After subscribing, so a phase entered meanwhile is not missed
            PublishPhase(observer, titles);
            return Disposable.Create(() => titles.PropertyChanged -= onChanged);
        }).DistinctUntilChanged();

    // The Titles change from any thread, so a change published late could leave an older phase last. Reading the phase
    // again after publishing, and publishing again until it holds, leaves the phase as it is now last
    private static void PublishPhase(Observer<bool> observer, TabTitles titles)
    {
        var says = IsLoadingPhase(titles.Phase);
        while (true)
        {
            observer.OnNext(says);
            var now = IsLoadingPhase(titles.Phase);
            if (now == says)
            {
                return;
            }

            says = now;
        }
    }

    private static bool IsLoadingPhase(TitlePhase phase) => phase is TitlePhase.Loading or TitlePhase.Downloading;

    private static MainWindowViewModel ViewModelOf(MainWindow window) =>
        window.DataContext as MainWindowViewModel
        ?? throw new InvalidOperationException("A main window needs a MainWindowViewModel as its DataContext.");

    /// <summary>
    /// Every Main Window follows the two window settings, top most and the taskbar progress (ADR-0020).
    /// </summary>
    internal static IDisposable FollowSettings(MainWindow window)
    {
        var vm = ViewModelOf(window);
        var settings = vm.GlobalSettings;
        return Disposable.Combine(
            // Not Skip(1), so a new window takes the setting as it is
            settings.IsTopMost.Subscribe(topMost => Dispatcher.UIThread.Post(() => window.Topmost = topMost)),
            settings.IsShowingTaskbarProgress.Skip(1).Subscribe(shown => Dispatcher.UIThread.Post(() =>
            {
                if (shown)
                {
                    UpdateImage.SetTaskbarProgress(vm, vm.WindowTabs.ActiveTab.CurrentValue);
                }
                else
                {
                    vm.Core.PlatformService?.StopTaskbarProgress();
                }
            })));
    }

    /// <summary>
    /// Starts a shown window in its Window Mode and gives it its Actions, input handlers and menus.
    /// </summary>
    private static Task SetUpShownWindow(MainWindow window, bool isAppStart)
    {
        var vm = ViewModelOf(window);
        window.WindowModeSwitcher.Start(isAppStart);
        window.Disposables.Add(window.WindowFitter.FollowFitInputs());
        window.Disposables.Add(FollowSettings(window));
        if (vm.IsFullscreen.Value)
        {
            Dispatcher.UIThread.Post(() =>
            {
                window.WindowFitter.Refit();
                WindowFunctions.CenterWindowOnScreen(window, top: true);
            }, DispatcherPriority.ApplicationIdle);
        }

        var settingUp = Task.Run(() =>
        {
            vm.ActionRunner = new FunctionsMapper(vm, window);
            vm.WindowTabs.SetSortOrder((SortFilesBy)Settings.Sorting.SortPreference);
        });

        AddInputHandlers(window);
        Dispatcher.UIThread.Post(() =>
        {
            window.SetClipboardCopyService();
            // The menus go into the main view, which a headless test window has none of
            if (window.UIHelper.GetMainView is not null)
            {
                window.UIHelper.AddDropDownMenu(window);
                window.UIHelper.AddFileMenu(vm);
                window.UIHelper.AddSettingsMenu(vm);
            }

            vm.ToolTip ??= new ToolTipViewModel();
        }, DispatcherPriority.Background);

        return settingUp;
    }

    private static void AddInputHandlers(MainWindow window)
    {
        // Using AddHandler fixes the first keydown event not firing properly
        // Typed handlers, since Avalonia calls an untyped Delegate through reflection on every event
        window.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        window.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
        window.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        window.Deactivated += delegate { window.KeybindRouter.Deactivated(); };
    }

    private static async ValueTask OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is MainWindow window)
        {
            await window.KeybindRouter.KeyDownAsync(new Keybind(e.Key, e.KeyModifiers),
                window.FocusManager?.GetFocusedElement()).ConfigureAwait(false);
        }
    }

    private static async ValueTask OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (sender is MainWindow window)
        {
            await window.KeybindRouter.KeyUpAsync(e.Key).ConfigureAwait(false);
        }
    }

    private static async ValueTask OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is MainWindow window)
        {
            await ImageViewerClickBehavior.PointerPressedAsync(e, window).ConfigureAwait(false);
        }
    }
}
