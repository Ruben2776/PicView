using System.Diagnostics;
using System.Runtime;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Navigation.Services;
using PicView.Avalonia.Services;
using PicView.Avalonia.WindowBehavior;
using PicView.Avalonia.SettingsManagement;
using PicView.Core.DebugTools;
using PicView.Core.FileAssociations;
using PicView.Core.FileHandling;
using PicView.Core.ProcessHandling;
using PicView.Core.ViewModels;

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
    private readonly Action _startNewProcess;
    private MainWindow? _firstWindow;
    private bool _launchFileArrived;
    private string? _launchFileBeforeStart;

    /// <param name="core">The view model every Main Window shares.</param>
    /// <param name="provider">Creates this platform's windows.</param>
    public AppLifecycle(CoreViewModel core, IWindowProvider provider)
        : this(core, provider, new SharedFiles(core.PlatformService), () => Environment.Exit(0),
            ProcessHelper.StartNewProcess)
    {
        Current = this;
    }

    /// <param name="sharedFiles">Loads and saves what every Main Window shares.</param>
    /// <param name="exit">Ends the process, last of all at App Shutdown.</param>
    /// <param name="startNewProcess">Starts a new PicView with no arguments, when restarting.</param>
    /// <param name="core">The view model every Main Window shares.</param>
    /// <param name="provider">Creates this platform's windows.</param>
    internal AppLifecycle(CoreViewModel core, IWindowProvider provider, ISharedFiles sharedFiles, Action exit,
        Action startNewProcess)
    {
        _core = core;
        _sharedFiles = sharedFiles;
        _exit = exit;
        _startNewProcess = startNewProcess;
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
    /// Runs App Shutdown: closes every Main Window, remembering the file and size of the most recently active one, saves
    /// what they share, deletes the temp files and ends the process.
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
        // Closing Batch Resize cancels a run that is still going
        AppToolWindows.CloseAll();
        // Closing Effects records where it is, and saves it
        AppWindows.Close(SecondaryWindowKind.Effects);
        var mostRecent = MainWindows.MostRecentlyActive;
        if (mostRecent is not null)
        {
            WindowFunctions.SaveSize(mostRecent);
        }

        foreach (var window in MainWindows.Open.ToArray())
        {
            await MainWindows.CloseAsync(window).ConfigureAwait(true);
        }

        // After closing, since deactivating a window remembers the file history's current entry instead
        if (mostRecent is not null)
        {
            MainWindowLifecycle.RememberFile(mostRecent);
        }

        await SaveSharedFilesAsync().ConfigureAwait(true);
        TempFileManager.Cleanup();
        if (restart is not null)
        {
            await restart().ConfigureAwait(true);
            _startNewProcess();
        }

        _exit();
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
    /// Runs App Start: handles a file-association argument or passes the launch on to an already running PicView, and
    /// otherwise opens the first Main Window and loads what every Main Window shares.
    /// </summary>
    /// <param name="settingsExists">Whether a settings file was found, as opposed to defaults being used.</param>
    /// <param name="args">The command-line arguments, starting with the program.</param>
    /// <param name="launchFileMayFollow">Whether the operating system may deliver the file PicView was launched with
    /// after App Start begins, through <see cref="OpenLaunchFileAsync"/>.</param>
    /// <returns>Completes once App Start has finished, after the first Main Window is shown.</returns>
    public async Task StartAsync(bool settingsExists, IReadOnlyList<string> args, bool launchFileMayFollow = false)
    {
        var source = args.Count > 1 ? args[1] : null;
        if (source?.StartsWith("associate:", StringComparison.OrdinalIgnoreCase) == true)
        {
            await ProcessFileAssociationAsync(source).ConfigureAwait(false);
            return;
        }

        if (source is not null && !OperatingSystem.IsMacOS() && Settings.UIProperties.OpenInSameWindow &&
            ProcessHelper.CheckIfAnotherInstanceIsRunning())
        {
            IPC.SendWithArgs(args.ToArray());
            return;
        }

        // Without a launch file to wait for, every one that arrives is a later launch
        _launchFileArrived = !launchFileMayFollow;
        ApplySharedSettings(settingsExists);
        var (window, settingUp) = MainWindows.OpenAtAppStart(source, launchFileMayFollow ? () => _launchFileArrived : null);
        _firstWindow = window;
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
        Dispatcher.UIThread.Post(() =>
        {
            if (!OperatingSystem.IsMacOS())
            {
                // macOS already opens files in the same window, the other platforms need IPC for it
                if (Settings.UIProperties.OpenInSameWindow && !ProcessHelper.CheckIfAnotherInstanceIsRunning())
                {
                    _ = IPC.StartListeningForArguments();
                }
            }

            Application.Current.Name = "PicView";
        }, DispatcherPriority.Background);

        await Task.WhenAll(settingUp, loading).ConfigureAwait(true);
    }

    /// <summary>
    /// Opens a file the operating system launched PicView with. The first one, while App Start shows the first Main
    /// Window, opens there and continues from where the user left off; a later one opens in the most recently active
    /// Main Window, or in a new PicView when files should not open in the same window.
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

        if (!Settings.UIProperties.OpenInSameWindow || MainWindows.MostRecentlyActive is not { } window)
        {
            ProcessHelper.StartNewProcess(path);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            window.Activate();
            // Opening reads the active window's view model, and activating may report back later
            _core.MainWindows.ActiveWindow.Value = window.DataContext as MainWindowViewModel;
        }, DispatcherPriority.Send);
        await SourceOpener.OpenAsync(window, _core, path).ConfigureAwait(false);
    }

    private void ApplySharedSettings(bool settingsExists)
    {
        ServiceHelper.SetAvaloniaImageLoader(_core);
        ServiceHelper.SetGalleryLoader();
        _ = Task.Run(() =>
        {
            _core.GlobalSettings?.BackgroundChoice.Value = Settings.UIProperties.BgColorChoice;
#pragma warning disable MA0042
            LanguageUpdater.UpdateLanguage(_core.Translation, settingsExists);
#pragma warning restore MA0042
        });
        ThemeManager.DetermineTheme(Application.Current, settingsExists);
        ColorManager.UpdateAccentColors(Settings.Theme.ColorTheme);
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
