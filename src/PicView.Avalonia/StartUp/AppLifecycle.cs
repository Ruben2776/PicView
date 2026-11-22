using System.Diagnostics;
using System.Runtime;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Navigation.Services;
using PicView.Avalonia.Services;
using PicView.Avalonia.SettingsManagement;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.FileAssociations;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.Navigation;
using PicView.Core.ProcessHandling;
using PicView.Core.Thumbnailing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.StartUp;

/// <summary>
/// Runs App Start and App Shutdown, each once per process, around the Main Windows that
/// <see cref="MainWindows"/> opens and closes.
/// </summary>
public sealed class AppLifecycle
{
    private readonly CoreViewModel _core;
    private readonly ISharedFiles _sharedFiles;
    private readonly Action _exit;
    private readonly Action<string?, bool> _startNewProcess;
    private readonly HandOverListener? _handOvers;

    // The hand-overs taken and not opened yet, which App Shutdown owes a PicView to open in. Guarded by the lock,
    // with whether any more are taken, so one is either opened here or handed to a new process, never neither
    private readonly Lock _handOverLock = new();

    // The dispatcher of the thread the app was made on, for what arrives on a pool thread
    private readonly Dispatcher _dispatcher = Dispatcher.UIThread;
    private readonly List<TakenHandOver> _takenHandOvers = [];
    private bool _takesNoHandOvers;
    private MainWindow? _firstWindow;
    private bool _launchFileArrived;
    private string? _launchFileBeforeStart;

    /// <param name="core">The view model every Main Window shares.</param>
    /// <param name="provider">Creates this platform's windows.</param>
    /// <param name="handOvers">Takes what PicViews started later hand over, or null on a platform that brings a later
    /// start to the running PicView itself.</param>
    public AppLifecycle(CoreViewModel core, IWindowProvider provider, HandOverListener? handOvers = null)
        : this(core, provider, new SharedFiles(core.PlatformService), () => Environment.Exit(0),
            ProcessHelper.StartNewProcess, handOvers)
    {
        Current = this;
    }

    /// <param name="sharedFiles">Loads and saves what every Main Window shares.</param>
    /// <param name="exit">Ends the process, last of all at App Shutdown.</param>
    /// <param name="startNewProcess">Starts a new PicView process with a Source or none. True starts one that runs
    /// its own App Start whatever is running, when restarting; false one that hands over as any start does.</param>
    /// <param name="core">The view model every Main Window shares.</param>
    /// <param name="provider">Creates this platform's windows.</param>
    /// <param name="handOvers">Takes what PicViews started later hand over.</param>
    internal AppLifecycle(CoreViewModel core, IWindowProvider provider, ISharedFiles sharedFiles, Action exit,
        Action<string?, bool> startNewProcess, HandOverListener? handOvers = null)
    {
        _core = core;
        _sharedFiles = sharedFiles;
        _exit = exit;
        _startNewProcess = startNewProcess;
        _handOvers = handOvers;
        MainWindows = new MainWindowLifecycle(core, provider, this);
        AppWindows = new SecondaryWindowOpener(() => MainWindows.MostRecentlyActive, provider, () => null);
        AppToolWindows = new SecondaryWindowOpener(() => MainWindows.MostRecentlyActive, provider, () => null);
    }

    /// <summary>
    /// The app this process runs, once App Start has begun.
    /// </summary>
    public static AppLifecycle? Current { get; private set; }

    /// <summary>
    /// Opens and closes the Main Windows.
    /// </summary>
    public MainWindowLifecycle MainWindows { get; }

    /// <summary>
    /// Opens the App Windows and Effects, at most one of each for the whole app, owned by no Main Window. They close
    /// with the last Main Window.
    /// </summary>
    internal SecondaryWindowOpener AppWindows { get; }

    /// <summary>
    /// Opens the Tool Windows that belong to the whole app rather than to a Main Window: Batch Resize. They stay open
    /// when the last Main Window closes, and PicView keeps running until they close too.
    /// </summary>
    // TODO: allow opening Batch Resize at App Start without a Main Window (e.g. from the file manager's context
    // menu); its file list would then start empty.
    internal SecondaryWindowOpener AppToolWindows { get; }

    /// <summary>
    /// Whether App Shutdown has begun.
    /// </summary>
    internal bool IsShuttingDown { get; private set; }

    /// <summary>
    /// Runs App Shutdown once the last Main Window and the Tool Windows that outlive it are all closed.
    /// </summary>
    internal async Task ShutdownIfNothingIsOpenAsync()
    {
        if (IsShuttingDown || MainWindows.Open.Count > 0)
        {
            return;
        }

        if (AppToolWindows.NewestOpenWindow is not null)
        {
            // Only the App Windows go with the last Main Window; PicView runs on for the Tool Windows left
            AppWindows.CloseAll();
            await SaveSharedFilesAsync().ConfigureAwait(true);
            return;
        }

        await ShutdownAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Runs App Shutdown: closes every Main Window, remembering the file and storing the Window Settings of the most recently
    /// active one, saves what they share, deletes the temp files and ends the process.
    /// </summary>
    public Task ShutdownAsync() => ShutdownAsync(restart: null);

    /// <summary>
    /// Restarts PicView: runs App Shutdown, then starts a new PicView with no file, whose App Start follows the
    /// settings as usual. Does nothing once App Shutdown has begun.
    /// </summary>
    /// <param name="beforeStart">Runs once App Shutdown has saved what every Main Window shares, before the new
    /// PicView starts.</param>
    public Task RestartAsync(Func<Task>? beforeStart = null) =>
        ShutdownAsync(beforeStart ?? (() => Task.CompletedTask));

    /// <param name="restart">Runs before a new PicView starts, or <see langword="null"/> to start none.</param>
    private async Task ShutdownAsync(Func<Task>? restart)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShutdownAsync(restart)).ConfigureAwait(false);
            return;
        }

        if (IsShuttingDown)
        {
            return;
        }

        IsShuttingDown = true;
        // First, so a PicView started from here on, as the one a restart starts, runs its own App Start
        var unopened = StopTakingHandOvers();
        // Closing Batch Resize cancels a run that is still going
        AppToolWindows.CloseAll();
        // Closing Effects records where it is, and saves it
        AppWindows.Close(SecondaryWindowKind.Effects);
        var mostRecent = MainWindows.MostRecentlyActive;
        if (mostRecent is not null)
        {
            MainWindowLifecycle.StoreWindowSettings(mostRecent);
        }

        foreach (var window in MainWindows.Open.ToArray())
        {
            await MainWindows.CloseAsync(window).ConfigureAwait(true);
        }

        if (mostRecent is not null)
        {
            MainWindowLifecycle.RememberFile(mostRecent);
        }

        await SaveSharedFilesAsync().ConfigureAwait(true);
        TempFileManager.Cleanup();
        if (restart is not null)
        {
            await restart().ConfigureAwait(true);
            // Its own process, or a PicView that is still listening would take it and keep its own settings
            _startNewProcess(null, true);
        }

        // After the save, so the PicView that opens them reads what this one left
        foreach (var source in unopened)
        {
            _startNewProcess(source, false);
        }

        _exit();
    }

    /// <summary>
    /// Stops the listener and takes no more hand-overs.
    /// </summary>
    /// <returns>The Sources that were taken and never opened: those the listener held for an App Start that did not
    /// get to them, and those still waiting for the UI thread.</returns>
    private List<string?> StopTakingHandOvers()
    {
        List<string?> unopened = [];
        if (_handOvers is not null)
        {
            unopened.AddRange(_handOvers.Stop());
        }

        lock (_handOverLock)
        {
            _takesNoHandOvers = true;
            foreach (var taken in _takenHandOvers)
            {
                unopened.Add(taken.Source);
            }

            _takenHandOvers.Clear();
        }

        return unopened;
    }

    /// <summary>
    /// Saves what every Main Window shares, and the settings window's own placement.
    /// </summary>
    internal async Task SaveSharedFilesAsync()
    {
        try
        {
            await _sharedFiles.SaveAsync().ConfigureAwait(true);
            if (_core.SettingsViewModel?.SettingsWindowConfig is { } settingsWindowConfig)
            {
                await settingsWindowConfig.SaveAsync().ConfigureAwait(true);
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(AppLifecycle), nameof(SaveSharedFilesAsync), e);
        }
    }

    /// <summary>
    /// Runs App Start: handles a file-association argument, and otherwise opens the first Main Window, loads what every
    /// Main Window shares and begins taking what PicViews started later hand over. A PicView that started while
    /// another was running has handed its Source over and exited before this (ADR-0037).
    /// </summary>
    /// <param name="settingsExists">Whether a settings file was found, as opposed to defaults being used.</param>
    /// <param name="args">The command-line arguments, starting with the program.</param>
    /// <param name="launchFileMayFollow">Whether the operating system may deliver the file PicView was launched with
    /// after App Start begins, through <see cref="OpenLaunchFileAsync"/>.</param>
    /// <returns>Completes once App Start has finished, after the first Main Window is shown.</returns>
    public async Task StartAsync(bool settingsExists, IReadOnlyList<string> args, bool launchFileMayFollow = false)
    {
        var source = SingleProcess.SourceOf(args, first: 1);
        if (source?.StartsWith("associate:", StringComparison.OrdinalIgnoreCase) == true)
        {
            await ProcessFileAssociationAsync(source).ConfigureAwait(false);
            return;
        }

        // Without a launch file to wait for, every one that arrives is a later launch
        _launchFileArrived = !launchFileMayFollow;
        ApplySharedSettings(settingsExists);
        if (source is null && Settings.StartUp.Behavior != StartUpBehavior.None && Settings.Navigation.IsFileHistoryEnabled)
        {
            // What the start-up behavior opens is read from the file history, before the first window is made; a start
            // with a Source opens that instead, so it reads nothing before its window
            FileHistoryManager.EnsureInitialized();
        }

        var (window, settingUp) = MainWindows.OpenAtAppStart(source, launchFileMayFollow ? () => _launchFileArrived : null);
        _firstWindow = window;
        FollowOnceOpened(window);
        // Those held while the first Main Window was not there yet open now, each after this window's own start
        _handOvers?.Attach(ReceiveHandOver);
        if (_launchFileBeforeStart is { } early)
        {
            _launchFileBeforeStart = null;
            await OpenLaunchFileAsync(early).ConfigureAwait(true);
        }

        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Quitting, as from the macOS app menu, goes through App Shutdown, which ends the process itself
            desktop.ShutdownRequested += async (_, e) =>
            {
                e.Cancel = true;
                await ShutdownAsync().ConfigureAwait(true);
            };
        }

        ResourceLimits.LimitMemory(new Percentage(80));
        GCSettings.LatencyMode = GCLatencyMode.LowLatency;
        var loading = _sharedFiles.LoadAsync();
        Dispatcher.UIThread.Post(() => Application.Current.Name = "PicView", DispatcherPriority.Background);

        await Task.WhenAll(settingUp, loading).ConfigureAwait(true);
    }

    /// <summary>
    /// Opens a file the operating system launched PicView with. The first one, while App Start shows the first Main
    /// Window, opens there and continues from where the user left off; a later one opens as a hand-over does, where
    /// the Opening Mode says: in a new tab of the Active Main Window, in place of what its active tab shows, or in a
    /// new Main Window.
    /// </summary>
    public async Task OpenLaunchFileAsync(string path)
    {
        if (_firstWindow is null)
        {
            // Delivered before App Start reached the first Main Window, which opens it
            _launchFileBeforeStart = path;
            return;
        }

        if (!_launchFileArrived)
        {
            _launchFileArrived = true;
            MainWindows.OpenStartSource(_firstWindow, path, continueFromLeftOff: true);
            Dispatcher.UIThread.Post(_firstWindow.Activate, DispatcherPriority.Send);
            return;
        }

        await OpenHandOverAsync(path).ConfigureAwait(false);
    }

    /// <summary>
    /// Takes the Source another PicView process was started with, null when it was started with none, and opens it on
    /// the UI thread. Callable from any thread.
    /// </summary>
    /// <returns>False once App Shutdown has begun. One taken before that and not opened by the time App Shutdown
    /// ends is given to a new PicView process.</returns>
    internal bool ReceiveHandOver(string? source)
    {
        var taken = new TakenHandOver(source);
        lock (_handOverLock)
        {
            if (_takesNoHandOvers)
            {
                return false;
            }

            _takenHandOvers.Add(taken);
        }

        _dispatcher.Post(() => _ = OpenAndLogAsync(taken), DispatcherPriority.Background);
        return true;
    }

    private sealed record TakenHandOver(string? Source);

    private async Task OpenAndLogAsync(TakenHandOver taken)
    {
        lock (_handOverLock)
        {
            // App Shutdown got to it first, and starts a PicView for it
            if (!_takenHandOvers.Remove(taken))
            {
                return;
            }
        }

        try
        {
            await OpenHandOverAsync(taken.Source).ConfigureAwait(true);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(AppLifecycle), nameof(ReceiveHandOver), e);
        }
    }

    /// <summary>
    /// Opens a Source that reached the running PicView from outside it: one another PicView process handed over, or a
    /// file the operating system launched PicView with again. Under Same Window it opens in the active tab of the
    /// Active Main Window, and under New Tab in a new tab of it, or in its active tab when that is a Fresh Tab; under
    /// either, a start without a Source only brings that window to the front. Under New Window, or with no Main Window
    /// open, it opens in a new Main Window, opened from the Active Main Window, which shows a Fresh Tab when there is
    /// no Source. One taken just as App Shutdown began goes to a new PicView process instead, so it is not lost.
    /// </summary>
    internal async Task OpenHandOverAsync(string? source)
    {
        if (!_dispatcher.CheckAccess())
        {
            await _dispatcher.InvokeAsync(() => OpenHandOverAsync(source)).ConfigureAwait(false);
            return;
        }

        var active = MainWindows.MostRecentlyActive;
        var mode = Settings.UIProperties.OpeningMode;
        switch (HandOverReceipt.Respond(IsShuttingDown, mode, source is not null, active is not null))
        {
            case HandOverResponse.Refuse:
                _startNewProcess(source, false);
                break;
            case HandOverResponse.OpenInActiveMainWindow:
                BringToFront(active);
                // Need to stop taskbar progress if it's running, otherwise the new one will not be updated
                _core.PlatformService?.StopTaskbarProgress();
                // Off the UI thread, as every Source opens
                await Task.Run(() => SourceOpener.OpenAsync(active!, _core, source!).AsTask()).ConfigureAwait(false);
                break;
            case HandOverResponse.OpenInNewTab:
                BringToFront(active!);
                _core.PlatformService?.StopTaskbarProgress();
                var tabs = active!.ViewModel!.WindowTabs;
                var tab = OpeningTarget.PrepareNewTab(tabs, mode) ?? tabs.ActiveTab.CurrentValue;
                await Task.Run(() => SourceOpener.OpenAsync(active, _core, source!, tab: tab).AsTask())
                    .ConfigureAwait(false);
                break;
            case HandOverResponse.ActivateActiveMainWindow:
                BringToFront(active);
                break;
            default:
                await MainWindows.OpenNewAsync(active, source).ConfigureAwait(false);
                break;
        }
    }

    // A minimized window is shown again first, in the Window Mode it had, or the user would see nothing happen
    private void BringToFront(MainWindow window)
    {
        window.WindowModeSwitcher.LeaveMinimized();
        window.Activate();
        // Opening reads the active window's view model, and activating may report back later
        _core.MainWindows.ActiveWindow.Value = window.DataContext as MainWindowViewModel;
    }

    private void ApplySharedSettings(bool settingsExists)
    {
        ComposeServices();
        _core.GlobalSettings.FollowLanguage(_core.Translation.LanguageChanged);
        _ = Task.Run(() =>
        {
#pragma warning disable MA0042
            LanguageUpdater.UpdateLanguage(_core.Translation, settingsExists);
#pragma warning restore MA0042
        });
        // Before the first window is made, so the brushes it paints its background with resolve in this theme
        ThemeManager.DetermineTheme(Application.Current, settingsExists);
        ColorManager.UpdateAccentColors(Settings.Theme.ColorTheme);
    }

    // No setting is followed before the first Main Window is open, so it shows without the cost
    // A first window closed before it opens never follows, which is fine as nothing can then be toggled
    private void FollowOnceOpened(MainWindow window)
    {
        if (window.IsVisible)
        {
            FollowSharedSettings();
            return;
        }

        void OnOpened(object? sender, EventArgs e)
        {
            window.Opened -= OnOpened;
            FollowSharedSettings();
        }

        window.Opened += OnOpened;
    }

    private void FollowSharedSettings()
    {
        var settings = _core.GlobalSettings;
        // The write-throughs first, so each follower below is notified after the stored value is written
        settings.InitializeActionSettings();
        _core.GallerySettings.Start();
        // Lives for the process. The Opening Mode has no follower: each hand-over reads it as it arrives
        if (_core.SharedFileWatcher is { } fileWatcher)
        {
            // Every tab's watch follows the setting
            _ = settings.IsIncludingSubdirectories.Skip(1).Subscribe(_ => fileWatcher.RestartWatches());
        }
    }

    /// <summary>
    /// Composes the services every tab loads with, once, for the <see cref="CoreViewModel"/> to hand to each tab it
    /// creates. Only constructs them: it lists no files and starts no watching, so the first image shows as soon as
    /// it would without them.
    /// </summary>
    private void ComposeServices()
    {
        // Every platform's app has a platform service; without one no tab could load, so a missing one is a bug
        Debug.Assert(_core.PlatformService != null);
        if (_core.PlatformService is not { } platform)
        {
            return;
        }

        var imageLoader = new AvaloniaImageModelLoader(_core);
        var thumbnails = new Thumbnails(new AvaloniaThumbnailDecoder(platform), _core.SharedCache);
        // A rename the watcher reports reaches the history of every tab of every window before any tab follows it
        var fileWatcher = new FileWatcherService(_core.SharedCache, thumbnails,
            fileRenamed: (oldPath, newPath) => FileRenames.Follow(_core, oldPath, newPath),
            folderRenamed: (oldFolder, newFolder) => FileRenames.FollowFolder(_core, oldFolder, newFolder));
        var navigation = new NavigationService();
        _core.InitializeNavigation(
            new TabServices(_core.SharedCache, thumbnails, fileWatcher, imageLoader,
                platform, _core.GlobalSettings),
            navigation);
    }

    private Task ProcessFileAssociationAsync(string argument) => Task.Run(async () =>
    {
        try
        {
            _core.PlatformService?.InitiateFileAssociationService();
            Debug.WriteLine($"Processing file association argument: {argument}");
            await FileAssociationProcessor.ProcessFileAssociationArguments(argument).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in file association processing: {ex.Message}");
        }
        finally
        {
            // Always exit the elevated process after processing associations
            _exit();
        }
    });
}
