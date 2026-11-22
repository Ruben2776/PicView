using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Input;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Services;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.FileHistory;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Navigation;
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

    // The dispatcher of the thread the app was made on, which every Main Window is made on. A pool thread can have
    // a dispatcher of its own, so what Dispatcher.UIThread is there is not asked
    private readonly Dispatcher _dispatcher = Dispatcher.UIThread;

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
    /// Opens a new Main Window, on top of the window it is opened from and at its size, with a copy of that window's
    /// Window Settings, and brings it to the front. Its tab derives the start-up menu, or shows
    /// <paramref name="source"/> when one is given.
    /// </summary>
    /// <param name="senderWindow">The window it is opened from, or null when no Main Window is open, as while only
    /// Batch Resize is; it then starts from the settings record, where the last Main Window was.</param>
    /// <param name="source">What the new window shows; it says Loading from the start, so no menu is made for it. The
    /// window is already visible and sized, so the Source loads through Core, not in the startup order (ADR-0005).</param>
    /// <returns>Completes once the window is set up, after it is shown, and its Source has loaded or failed.</returns>
    public async Task<MainWindow> OpenNewAsync(MainWindow? senderWindow, string? source = null)
    {
        // Callable from any thread: everything here but the load itself touches windows
        if (!_dispatcher.CheckAccess())
        {
            return await _dispatcher.InvokeAsync(() => OpenNewAsync(senderWindow, source)).ConfigureAwait(false);
        }

        // The new window starts with a copy of its opener's Window Settings, in the mode the opener is in
        WindowSettings? windowSettings = null;
        if (senderWindow?.DataContext is MainWindowViewModel opener)
        {
            // A first Main Window that App Start has not shown yet has no size or place to pass on
            if (senderWindow.IsVisible)
            {
                WindowFunctions.RememberBounds(senderWindow);
            }

            windowSettings = opener.WindowSettings.CopyFor(opener.WindowModeState.CurrentValue.Remembered);
        }

        var window = Create(opensSource: source is not null, windowSettings: windowSettings);

        WindowFunctions.HandleWindowScalingMode(window, isAppStart: false);
        window.Show();
        window.Activate();

        await SetUpShownWindow(window, isAppStart: false).ConfigureAwait(true);
        if (source is null)
        {
            return window;
        }

        // The ticket Create's Loading had is replaced, so a Source that opens nothing leaves no Loading behind
        var tab = ViewModelOf(window).WindowTabs.ActiveTab.CurrentValue;
        var ticket = tab.SetLoading();
        var loaded = false;
        try
        {
            // Off the UI thread, as every Source opens
            var result = await Task.Run(() => SourceOpener.OpenAsync(window, _core, source, tab: tab).AsTask())
                .ConfigureAwait(true);
            loaded = result.Outcome is LoadOutcome.Loaded;
        }
        finally
        {
            if (!loaded)
            {
                tab.EndLoading(ticket);
            }
        }

        return window;
    }

    /// <summary>
    /// Opens a Main Window with a tab moved out of another one, placed where the tab was dropped, with a copy of that
    /// window's Window Settings.
    /// </summary>
    /// <param name="tab">The tab, already taken out of its window, that the new window opens with.</param>
    /// <returns>Completes once the window is set up, after it is shown.</returns>
    public async Task<MainWindow> OpenForMovedTabAsync(TabViewModel tab, PixelPoint position, double width, double height)
    {
        // The tab still names the window it was moved out of, whose Window Settings the new window starts with
        var windowSettings = tab.ParentWindowContext is { } source
            ? source.WindowSettings.CopyFor(source.WindowModeState.CurrentValue.Remembered)
            : null;
        var window = Create(tab, windowSettings: windowSettings);
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
    /// <param name="source">What to open, or null for what the start-up behavior opens: the start-up menu, the Last
    /// File or the Last Session.</param>
    /// <param name="launchFileArrived">When given, a launch file may still arrive; what the start-up behavior opens is
    /// shown only if none has by the time the window is idle.</param>
    /// <returns>The window, and a task that completes once it is set up.</returns>
    internal (MainWindow Window, Task SetUp) OpenAtAppStart(string? source, Func<bool>? launchFileArrived)
    {
        // Once, after App Start has loaded the file history it reads
        var plan = source is null ? DecideStartUp() : default;
        // A window that opens a source says Loading from the start, so no menu is made for it; QuickLoad replaces the
        // ticket with its own
        var opensSource = source is not null || (launchFileArrived is null && !plan.ShowsMenu);
        var window = Create(null, false, opensSource);
        WindowFunctions.HandleWindowScalingMode(window, isAppStart: true,
            adjustPos: !ViewModelOf(window).WindowSettings.StartingMode.AutoFit);
        if (source is not null)
        {
            OpenStartSource(window, source, continueFromLeftOff: false);
        }
        else if (launchFileArrived is null)
        {
            ShowStartUpMenuOrLastFile(window, plan);
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!launchFileArrived())
                {
                    ShowStartUpMenuOrLastFile(window, plan);
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

    private static StartUpPlan DecideStartUp() =>
        StartUpPlan.Decide(Settings.StartUp.Behavior, Settings.Navigation.IsFileHistoryEnabled,
            FileHistoryManager.LastFile, FileHistoryManager.LastSession, File.Exists);

    /// <summary>
    /// Shows the start-up menu, or opens what <paramref name="plan"/> opens: its active Source in the startup order,
    /// continuing from where the user left off, then the other tabs of the Last Session.
    /// </summary>
    private void ShowStartUpMenuOrLastFile(MainWindow window, StartUpPlan plan)
    {
        if (!plan.ShowsMenu)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await SourceOpener.OpenAsync(window, _core, plan.ActiveSource!,
                        new OpenOptions(IsStartup: true, ContinueFromLeftOff: true)).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // The other tabs still restore
                    DebugHelper.LogDebug(nameof(MainWindowLifecycle), nameof(ShowStartUpMenuOrLastFile), e);
                }

                if (plan.HasOtherTabs)
                {
                    await RestoreOtherTabsAsync(window, plan).ConfigureAwait(false);
                }
            });
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
    }

    /// <summary>
    /// Opens the Last Session's other tabs around the active one, in order, once it shows; the window is shown and
    /// sized by then, so each loads through Core (ADR-0005). Only the active tab lists from the start-up directory.
    /// </summary>
    private async Task RestoreOtherTabsAsync(MainWindow window, StartUpPlan plan)
    {
        for (var i = 0; i < plan.Sources.Count; i++)
        {
            if (i == plan.ActiveIndex)
            {
                continue;
            }

            var index = i;
            try
            {
                // The window's view model is read on the UI thread too, as its DataContext lives there. A window
                // closed meanwhile gets no more tabs
                var tab = await _dispatcher.InvokeAsync(() =>
                    MayBecomeActive(window) ? ViewModelOf(window).WindowTabs.InsertTab(index) : null);
                if (tab is null)
                {
                    return;
                }

                // Off the UI thread, as every Source opens; the insert may have resumed on it
                await Task.Run(() => SourceOpener.OpenAsync(window, _core, plan.Sources[index], tab: tab).AsTask())
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(MainWindowLifecycle), nameof(RestoreOtherTabsAsync), e);
            }
        }
    }

    /// <summary>
    /// Closes a Main Window and its Tool Windows: remembers its file, stores its Window Settings in the settings record,
    /// cleans up its tabs and saves what every Main Window shares. Closing the last one runs App Shutdown.
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
            StoreWindowSettings(window);
        }

        await Dispatcher.UIThread.InvokeAsync(window.Hide);
        if (remembers)
        {
            RememberFile(window);
        }
        // On macOS most Tool Windows have no owner, so they would outlive their Main Window
        window.MainWindowInitializer.CloseToolWindows();
        vm.WindowTabs.TearDown();
        vm.WindowSettings.Dispose();

        _core.MainWindows.MainWindows.Remove(vm);
        _open.Remove(window);
        if (MostRecentlyActive == window)
        {
            MostRecentlyActive = _open.Count > 0 ? _open[^1] : null;
        }

        if (_core.MainWindows.ActiveWindow.Value == vm)
        {
            _core.MainWindows.ActiveWindow.Value = MostRecentlyActive?.DataContext as MainWindowViewModel;
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
    /// Remembers the window's bounds and stores its Window Settings in the record.
    /// </summary>
    internal static void StoreWindowSettings(MainWindow window)
    {
        if (window.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        WindowFunctions.RememberBounds(window);
        vm.StoreWindowSettings(Settings);
    }

    /// <summary>
    /// Remembers what the window shows, for the next App Start to open when it opens the last file, and what its tabs
    /// show, for it to open when it continues the last session.
    /// </summary>
    internal static void RememberFile(MainWindow window)
    {
        if (window.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        LastFile.Remember(vm.WindowTabs.ActiveTab.CurrentValue);
        FileHistoryManager.RecordSession(LastSession.Of(vm.WindowTabs.Tabs.Value, vm.WindowTabs.ActiveTab.CurrentValue));
    }

    private MainWindow Create(TabViewModel? firstTab = null, bool setMainWindow = true, bool opensSource = false, WindowSettings? windowSettings = null)
    {
        var window = _provider.CreateMainWindow(firstTab, windowSettings);
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
        // Before the window is shown and its mode applied, so the first image, which a startup source shows before the
        // window is set up, and the starting mode are Fit inputs like any later one. After the factory, so the view
        // adapter reacts to a new image before the fitter does
        window.Disposables.Add(window.WindowFitter.Follow());
        window.Disposables.Add(window.WindowFitter);
        // Paints the window's two background brushes from its own seeded choice now, so it shows with them; a window
        // copied from another derives its own, as the copy carries no brushes. The rest of its followers start once
        // it is shown, in FollowSettings
        window.Disposables.Add(BackgroundManager.Follow(vm.WindowSettings));
        _core.MainWindows.MainWindows.Add(vm);
        _core.MainWindows.ActiveWindow.Value = vm;
        _open.Add(window);
        MostRecentlyActive = window;
        window.Lifecycle = this;
        window.Activated += (_, _) =>
        {
            // An activation that arrives once the window is closing would make App Shutdown store a closed window
            if (MayBecomeActive(window))
            {
                MostRecentlyActive = window;
            }
        };
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
    /// Whether an activation of <paramref name="window"/> may make it the active one: only while it is open and not
    /// closing, as a closing window's settings are about to be disposed.
    /// </summary>
    internal bool MayBecomeActive(MainWindow window) => _open.Contains(window) && !_closing.Contains(window);

    private static MainWindowViewModel ViewModelOf(MainWindow window) =>
        window.DataContext as MainWindowViewModel
        ?? throw new InvalidOperationException("A main window needs a MainWindowViewModel as its DataContext.");

    /// <summary>
    /// Every Main Window follows its own top most and the shared taskbar progress setting (ADR-0020). Its own
    /// background is followed from when it is made, before it shows (<see cref="BackgroundManager.Follow"/>).
    /// </summary>
    internal static IDisposable FollowSettings(MainWindow window)
    {
        var vm = ViewModelOf(window);
        var settings = vm.GlobalSettings;
        return Disposable.Combine(
            // Not Skip(1), so a new window takes its top most as it is seeded
            vm.WindowSettings.IsTopMost.Subscribe(topMost => Dispatcher.UIThread.Post(() => window.Topmost = topMost)),
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
        // Its own gallery commands, for each window however it opened, as the shared gallery settings start once the
        // first window is shown (ADR-0034); a copy from another window needs its own start
        vm.WindowSettings.Gallery.Start();
        window.Disposables.Add(FollowSettings(window));

        var settingUp = Task.Run(() => vm.ActionRunner = new FunctionsMapper(vm, window));

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
