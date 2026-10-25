using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
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
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.FileHistory;
using PicView.Core.FileSorting;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

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

    /// <summary>
    /// The Main Windows that are open, in the order they opened.
    /// </summary>
    public IReadOnlyList<MainWindow> Open => _open;

    /// <summary>
    /// The Main Window the user activated last, which a later launch opens in.
    /// </summary>
    public MainWindow? MostRecentlyActive { get; private set; }

    /// <summary>
    /// Opens a new Main Window that shows the start-up menu.
    /// </summary>
    /// <returns>Completes once the window is set up, after it is shown.</returns>
    public async Task<MainWindow> OpenNewAsync()
    {
        var window = Create();
        var vm = ViewModelOf(window);
        var settingUp = ApplyWindowSettingsAsync(vm, isAppStart: false);
        WindowFunctions.HandleWindowScalingMode(window, isAppStart: false);
        vm.WindowTabs.ActiveTab.Value.CurrentView.Value = new StartUpMenu
        {
            Buttons =
            {
                DataContext = vm.WindowTabs.ActiveTab.CurrentValue
            }
        };
        window.Show();
        await Task.WhenAll(settingUp, SetUpShownWindow(window, isAppStart: false)).ConfigureAwait(true);
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
        var vm = ViewModelOf(window);
        var settingUp = ApplyWindowSettingsAsync(vm, isAppStart: false);
        window.Position = position;
        window.Width = width;
        window.Height = height;
        WindowFunctions.HandleWindowScalingMode(window, isAppStart: false, adjustPos: false);
        window.Show();
        await Task.WhenAll(settingUp, SetUpShownWindow(window, isAppStart: false)).ConfigureAwait(true);
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
        var window = Create(null, false);
        var vm = ViewModelOf(window);
        var settingUp = ApplyWindowSettingsAsync(vm, isAppStart: true);
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

        return (window, Task.WhenAll(settingUp, SetUpShownWindow(window, isAppStart: true)));
    }

    /// <summary>
    /// Opens the source PicView was launched with in the first Main Window, sizing it first with AutoFit.
    /// </summary>
    internal void OpenStartSource(MainWindow window, string source, bool continueFromLeftOff)
    {
        _ = Task.Run(() => SourceOpener.OpenAsync(window, _core, source,
            new OpenOptions(IsStartup: true, ContinueFromLeftOff: continueFromLeftOff)).AsTask());
        if (window.WindowModeState.AutoFit)
        {
            // If this is not called, the window will have jerky behavior on next resize.
            Dispatcher.UIThread.Post(() => window.WindowFitter.Center(), DispatcherPriority.Render);
        }
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
            var vm = ViewModelOf(window);
            var tab = vm.WindowTabs.ActiveTab.CurrentValue;
            tab.CurrentView.Value = new StartUpMenu
            {
                Buttons =
                {
                    DataContext = tab
                }
            };
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
            // After hiding, since deactivating a window remembers the file history's current entry instead
            RememberFile(window);
        }
        // On macOS most Tool Windows have no owner, so they would outlive their Main Window
        window.MainWindowInitializer?.CloseToolWindows();
        vm.WindowTabs.TearDown();

        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is IDisposable disposable)
        {
            disposable.Dispose();
        }

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

        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        string? lastFile;
        if (!string.IsNullOrEmpty(tab.ArchiveExtractionService?.LastOpenedArchive))
        {
            lastFile = tab.ArchiveExtractionService.LastOpenedArchive;
        }
        else if (tab.SingleImageType is SingleImageType.Url && tab.SourceURL is not null)
        {
            lastFile = tab.SourceURL;
        }
        else
        {
            lastFile = tab.Model?.FileInfo?.FullName ?? FileHistoryManager.GetLastEntry();
        }

        if (lastFile is not null)
        {
            Settings.StartUp.LastFile = lastFile;
        }
    }

    private MainWindow Create(TabViewModel? firstTab = null, bool setMainWindow = true)
    {
        var window = _provider.CreateMainWindow(firstTab);
        var vm = ViewModelOf(window);
        vm.WindowTabs.SetHookups(new NavigationSubscriptions(window, vm, _core));
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

    private static MainWindowViewModel ViewModelOf(MainWindow window) =>
        window.DataContext as MainWindowViewModel
        ?? throw new InvalidOperationException("A main window needs a MainWindowViewModel as its DataContext.");

    /// <summary>
    /// Gives a window the bars the remembered settings ask for. Only the first window at App Start may start fullscreen,
    /// which hides them.
    /// </summary>
    private static Task ApplyWindowSettingsAsync(MainWindowViewModel vm, bool isAppStart)
    {
        var startsFullscreen = WindowModeSwitcher.Starting(isAppStart).Mode is WindowMode.Fullscreen;
        return Task.Run(() =>
        {
            vm.TitlebarHeight.Value = startsFullscreen || !Settings.UIProperties.ShowInterface
                ? 0
                : SizeDefaults.MainTitlebarHeight;
            vm.BottombarHeight.Value = startsFullscreen || !Settings.UIProperties.ShowInterface
                ? 0
                : SizeDefaults.BottombarHeight;
            vm.IsSideBySide.Value = Settings.ImageScaling.ShowImageSideBySide;
            vm.IsUIShown.Value = Settings.UIProperties.ShowInterface;
            vm.IsTopToolbarShown.Value = Settings.UIProperties.ShowInterface;
            vm.IsBottomToolbarShown.Value = Settings.UIProperties.ShowBottomNavBar &&
                                            Settings.UIProperties.ShowInterface;
            if (isAppStart)
            {
                vm.WindowTabs.ActiveTab.CurrentValue.SetLoading();
            }
        });
    }

    /// <summary>
    /// Starts a shown window in its Window Mode and gives it its Actions, input handlers and menus.
    /// </summary>
    private static Task SetUpShownWindow(MainWindow window, bool isAppStart)
    {
        var vm = ViewModelOf(window);
        window.WindowModeSwitcher.Start(isAppStart);
        window.Disposables.Add(window.WindowFitter.FollowFitInputs());
        if (vm.IsFullscreen.Value)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (vm.IsFullscreen.Value)
                {
                    ToggleUIVisibility.FullscreenHideInterface(vm);
                }
                window.WindowFitter.Refit();
                WindowFunctions.CenterWindowOnScreen(window, top: true);
            }, DispatcherPriority.ApplicationIdle);
        }

        var settingUp = Task.Run(() =>
        {
            vm.ActionRunner = new FunctionsMapper(vm, window);
            vm.IsScrollingEnabled.Value = Settings.Zoom.ScrollEnabled;
            if (Settings.WindowProperties.TopMost)
            {
                Dispatcher.UIThread.Invoke(() => { window.Topmost = true; });
            }
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
            TooltipHelper.StartTooltipSubscription(vm.ToolTip, window);
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

    private static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is MainWindow window)
        {
            _ = window.KeybindRouter.KeyDownAsync(new Keybind(e.Key, e.KeyModifiers),
                window.FocusManager?.GetFocusedElement());
        }
    }

    private static void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (sender is MainWindow window)
        {
            _ = window.KeybindRouter.KeyUpAsync(e.Key);
        }
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is MainWindow window)
        {
            _ = ImageViewerClickBehavior.PointerPressedAsync(e, window);
        }
    }
}
