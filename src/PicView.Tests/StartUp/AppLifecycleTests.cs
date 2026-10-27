using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Printing;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Core.Config;
using PicView.Core.Favorites;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.Update;
using PicView.Core.ViewModels;
using PicView.Tests.FileHistory;
using PicView.Tests.WindowBehavior;
using R3;

namespace PicView.Tests.StartUp;

[Collection("Sequential")]
public class AppLifecycleTests : IAsyncDisposable
{
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly ConfigFileBackup _batchResizeConfigBackup = new(new BatchResizeWindowConfig());
    private readonly ConfigFileBackup _effectsConfigBackup = new(new EffectsWindowConfig());
    private readonly List<AppLifecycle> _apps = [];
    private readonly string _folder;
    private readonly CoreViewModel _core;
    private readonly FolderSharedFiles _sharedFiles;
    private readonly LifecycleWindowProvider _provider;
    private int _exits;

    public AppLifecycleTests()
    {
        SetDefaults();
        TranslationManager.Init();
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        _folder = Path.Combine(Path.GetTempPath(), "PicViewAppLifecycleTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _core = new CoreViewModel(new NoPlatform(), file => ValueTask.FromResult(new ImageModel { FileInfo = file }));
        _provider = new LifecycleWindowProvider(_core);
        _sharedFiles = new FolderSharedFiles(_folder);
    }

    public async ValueTask DisposeAsync()
    {
        // Shut down before deleting the folder, since a late save to a missing folder goes to the user's own
        foreach (var app in _apps)
        {
            await app.ShutdownAsync();
        }

        foreach (var window in _provider.Created)
        {
            window.Close();
        }

        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }

        _fileHistoryBackup.Dispose();
        _batchResizeConfigBackup.Dispose();
        _effectsConfigBackup.Dispose();
    }

    [AvaloniaFact]
    public async Task OpenNewAsync_AfterAppStart_KeepsFileHistoryAddedSinceAppStart()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var path = Path.Combine(_folder, "seen.jpg");
        FileHistoryManager.Add(path);

        await app.MainWindows.OpenNewAsync();

        Assert.Contains(FileHistoryManager.AllEntries, entry => entry.Path == path);
    }

    [AvaloniaFact]
    public async Task CloseAsync_MainWindowThatIsNotTheLast_KeepsTempFilesUntilAppShutdown()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var second = await app.MainWindows.OpenNewAsync();
        var tempFile = TempFileManager.GetNewTempFilePath("download.jpg");
        await File.WriteAllTextAsync(tempFile, "image");

        await app.MainWindows.CloseAsync(second);
        var keptAfterClose = File.Exists(tempFile);
        await app.ShutdownAsync();

        Assert.True(keptAfterClose);
        Assert.False(File.Exists(tempFile));
    }

    [AvaloniaFact]
    public async Task OpenNewAsyncAndOpenForMovedTabAsync_GiveTheWindowTheBarsOfTheFirstMainWindow()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = ViewModelOf(app.MainWindows.Open[0]);

        var opened = ViewModelOf(await app.MainWindows.OpenNewAsync());
        var tab = first.WindowTabs.CreateTab();
        first.WindowTabs.MoveOut(tab);
        var moved = ViewModelOf(await app.MainWindows.OpenForMovedTabAsync(tab, new PixelPoint(40, 40), 800, 600));

        Assert.Equal(SizeDefaults.MainTitlebarHeight, first.TitlebarHeight.CurrentValue);
        Assert.Equal(SizeDefaults.BottombarHeight, first.BottombarHeight.CurrentValue);
        foreach (var vm in new[] { opened, moved })
        {
            Assert.Equal(first.TitlebarHeight.CurrentValue, vm.TitlebarHeight.CurrentValue);
            Assert.Equal(first.BottombarHeight.CurrentValue, vm.BottombarHeight.CurrentValue);
            Assert.True(vm.IsUIShown.CurrentValue);
        }
    }

    [AvaloniaFact]
    public async Task OpenForMovedTabAsync_TabMovedOutOfAMainWindow_OpensWithOnlyThatTab()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = ViewModelOf(app.MainWindows.Open[0]);
        var tab = first.WindowTabs.CreateTab();

        first.WindowTabs.MoveOut(tab);
        var moved = ViewModelOf(await app.MainWindows.OpenForMovedTabAsync(tab, new PixelPoint(40, 40), 800, 600));

        Assert.Equal([tab], moved.WindowTabs.Tabs.CurrentValue);
        Assert.Same(tab, moved.WindowTabs.ActiveTab.CurrentValue);
        Assert.Same(moved, tab.ParentWindowContext);
        Assert.DoesNotContain(tab, first.WindowTabs.Tabs.CurrentValue);
    }

    [AvaloniaFact]
    public async Task ShowAboutWindow_FromTwoMainWindows_OpensOneAboutWindow()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        var second = await app.MainWindows.OpenNewAsync();

        first.ShowAboutWindow();
        second.ShowAboutWindow();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        Assert.Equal(1, _provider.AboutWindowsCreated);
    }

    [AvaloniaFact]
    public async Task ShowEffectsWindow_FromTwoMainWindows_OpensOneEffectsWindow()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        var second = await app.MainWindows.OpenNewAsync();

        await first.MainWindowInitializer!.ShowEffectsWindow();
        await second.MainWindowInitializer!.ShowEffectsWindow();

        Assert.Single(_provider.EffectsWindows);
    }

    [AvaloniaFact]
    public async Task CloseAsync_MainWindowEffectsWasOpenedFrom_KeepsEffectsOpenForTheOtherMainWindow()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        await app.MainWindows.OpenNewAsync();
        await first.MainWindowInitializer!.ShowEffectsWindow();
        var effects = Assert.Single(_provider.EffectsWindows);

        await app.MainWindows.CloseAsync(first);

        Assert.True(effects.IsVisible);
    }

    [AvaloniaFact]
    public async Task CloseAsync_LastMainWindowWithEffectsOpen_ClosesEffectsAndRunsAppShutdown()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var main = app.MainWindows.Open[0];
        await main.MainWindowInitializer!.ShowEffectsWindow();
        var effects = Assert.Single(_provider.EffectsWindows);

        await app.MainWindows.CloseAsync(main);

        Assert.False(effects.IsVisible);
        Assert.Equal(1, _exits);
    }

    [AvaloniaFact]
    public async Task CloseAsync_MainWindow_ClosesItsToolWindows()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var second = await app.MainWindows.OpenNewAsync();
        second.ShowConvertWindow();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        var convert = Assert.Single(_provider.ConvertWindows);
        var closed = false;
        convert.Closed += (_, _) => closed = true;

        await app.MainWindows.CloseAsync(second);

        Assert.True(closed);
    }

    [AvaloniaFact]
    public async Task ShowBatchResizeWindow_FromTwoMainWindows_OpensOneThatOutlivesTheFirst()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        var second = await app.MainWindows.OpenNewAsync();

        await first.ShowBatchResizeWindow();
        await second.ShowBatchResizeWindow();
        await app.MainWindows.CloseAsync(first);

        var batchResize = Assert.Single(_provider.BatchResizeWindows);
        Assert.True(batchResize.IsVisible);
        Assert.NotNull(_core.BatchResize);
    }

    [AvaloniaFact]
    public async Task CloseAsync_LastMainWindowWithBatchResizeOpen_KeepsRunningUntilBatchResizeCloses()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var main = app.MainWindows.Open[0];
        main.ShowAboutWindow();
        await main.ShowBatchResizeWindow();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        var about = Assert.Single(_provider.AboutWindows);
        var aboutClosed = false;
        about.Closed += (_, _) => aboutClosed = true;

        await app.MainWindows.CloseAsync(main);
        var exitsWhileOpen = _exits;
        Assert.Single(_provider.BatchResizeWindows).Close();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        Assert.True(aboutClosed);
        Assert.Equal(0, exitsWhileOpen);
        Assert.Equal(1, _exits);
        Assert.Null(_core.BatchResize);
    }

    [AvaloniaFact]
    public async Task ShutdownAsync_WithBatchResizeOpen_ClosesIt()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        await app.MainWindows.Open[0].ShowBatchResizeWindow();
        var closed = false;
        Assert.Single(_provider.BatchResizeWindows).Closed += (_, _) => closed = true;

        await app.ShutdownAsync();

        Assert.True(closed);
        Assert.Equal(1, _exits);
    }

    [AvaloniaFact]
    public async Task ShutdownAsync_ClosesEveryMainWindowAndRemembersTheMostRecentlyActiveOne()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        var second = await app.MainWindows.OpenNewAsync();
        var firstFile = Path.Combine(_folder, "first.jpg");
        ViewModelOf(first).WindowTabs.ActiveTab.CurrentValue.Display(new ImageModel { FileInfo = new FileInfo(firstFile) });
        ViewModelOf(second).WindowTabs.ActiveTab.CurrentValue
            .Display(new ImageModel { FileInfo = new FileInfo(Path.Combine(_folder, "second.jpg")) });
        first.Activate();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        await app.ShutdownAsync();

        Assert.Empty(app.MainWindows.Open);
        Assert.Equal(firstFile, Settings.StartUp.LastFile);
        Assert.Equal(1, _sharedFiles.Saves);
        Assert.Equal(1, _exits);
    }

    [AvaloniaFact]
    public async Task OpenLaunchFileAsync_AfterAppStart_OpensInTheMostRecentlyActiveMainWindow()
    {
        Settings.UIProperties.OpenInSameWindow = true;
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        var second = await app.MainWindows.OpenNewAsync();
        first.Activate();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        var firstView = ViewOf(first);
        var secondView = ViewOf(second);

        await app.OpenLaunchFileAsync(Path.Combine(_folder, "launched.jpg"));
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        Assert.NotSame(firstView, ViewOf(first));
        Assert.Same(secondView, ViewOf(second));
    }

    [AvaloniaFact]
    public async Task SourceOpenerOpenAsync_IntoMainWindowThatIsNotActive_OpensInThatMainWindow()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        var second = await app.MainWindows.OpenNewAsync();
        await ActivateAsync(first);
        var firstView = ViewOf(first);
        var secondView = ViewOf(second);

        await SourceOpener.OpenAsync(second, _core, Path.Combine(_folder, "opened.jpg"));
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        Assert.Same(firstView, ViewOf(first));
        Assert.NotSame(secondView, ViewOf(second));
    }

    [AvaloniaFact]
    public async Task SourceOpenerOpenAsync_AtStartupIntoMainWindowThatIsNotActive_LeavesTheActiveOneAlone()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        var second = await app.MainWindows.OpenNewAsync();
        await ActivateAsync(first);
        var firstView = ViewOf(first);

        await SourceOpener.OpenAsync(second, _core, Path.Combine(_folder, "missing.jpg"), new OpenOptions(IsStartup: true));
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        Assert.Same(firstView, ViewOf(first));
    }

    [AvaloniaFact]
    public async Task FileSearchDialog_FilePickedInMainWindowThatIsNotActive_LoadsInThatMainWindow()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        var second = await app.MainWindows.OpenNewAsync();
        var path = Path.Combine(_folder, "found.jpg");
        await File.WriteAllTextAsync(path, "image");
        // The dialog is only shown while browsing
        TabNavigationInitializer.Initialize(_core, [], ViewModelOf(first));
        TabNavigationInitializer.Initialize(_core, [], ViewModelOf(second));
        // The dialog reads the core from the app, as it does in PicView
        Application.Current!.DataContext = _core;
        try
        {
            var dialog = new FileSearchDialog();
            second.Content = dialog;
            await ActivateAsync(first);

            await dialog.LoadSelectedFileAsync(path, CancellationToken.None);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

            Assert.Null(ViewModelOf(first).WindowTabs.ActiveTab.CurrentValue.Model.FileInfo);
            Assert.Equal(path, ViewModelOf(second).WindowTabs.ActiveTab.CurrentValue.Model.FileInfo?.FullName);
        }
        finally
        {
            Application.Current.DataContext = null;
        }
    }

    /// <summary>
    /// Makes <paramref name="window"/> the Active Main Window. The headless app has no core to set it from when a
    /// window is activated.
    /// </summary>
    private async Task ActivateAsync(MainWindow window)
    {
        window.Activate();
        _core.MainWindows.ActiveWindow.Value = ViewModelOf(window);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private static object? ViewOf(MainWindow window) =>
        ViewModelOf(window).WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue;

    private static MainWindowViewModel ViewModelOf(MainWindow window) => (MainWindowViewModel)window.DataContext!;

    private AppLifecycle CreateApp()
    {
        var app = new AppLifecycle(_core, _provider, _sharedFiles, () => _exits++);
        _apps.Add(app);
        return app;
    }

    /// <summary>
    /// Keeps file history and favorites in a temp folder, and leaves the user's settings and Keymap alone.
    /// </summary>
    private sealed class FolderSharedFiles(string folder) : ISharedFiles
    {
        public int Saves { get; private set; }

        public Task LoadAsync()
        {
            FileHistoryManager.Initialize(Path.Combine(folder, "history.json"));
            FavoritesManager.Initialize(Path.Combine(folder, "favorites.json"));
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            Saves++;
            await FileHistoryManager.SaveToFileAsync();
        }
    }

    /// <summary>
    /// Creates headless main windows, each with its own view model, and plain windows for everything else.
    /// </summary>
    private sealed class LifecycleWindowProvider(CoreViewModel core) : IWindowProvider
    {
        public List<Window> Created { get; } = [];
        public int AboutWindowsCreated => AboutWindows.Count;
        public List<Window> AboutWindows { get; } = [];
        public List<Window> BatchResizeWindows { get; } = [];
        public List<Window> ConvertWindows { get; } = [];
        public List<Window> EffectsWindows { get; } = [];

        public MainWindow CreateMainWindow(TabViewModel? firstTab = null)
        {
            var window = new TestMainWindow(new NoOpModeAdapter(), this);
            window.DataContext = new MainWindowViewModel(core, core.Translation, window, core.GlobalSettings!,
                core.GallerySettings, firstTab);
            Created.Add(window);
            return window;
        }

        public IWindowModeAdapter CreateWindowModeAdapter() => new NoOpModeAdapter();
        public Window CreateAboutWindow()
        {
            var window = Track(new Window());
            AboutWindows.Add(window);
            return window;
        }

        public Window CreateBatchResizeWindow(BatchResizeWindowConfig config)
        {
            var window = Track(new Window());
            BatchResizeWindows.Add(window);
            return window;
        }
        public Window CreateFileAssociationsWindow() => Track(new Window());
        public Window CreateConvertWindow()
        {
            var window = Track(new Window());
            ConvertWindows.Add(window);
            return window;
        }

        public Window CreateEffectsWindow(EffectsWindowConfig config)
        {
            var window = Track(new Window());
            EffectsWindows.Add(window);
            return window;
        }
        public Window CreateImageInfoWindow(MainWindowViewModel vm) => Track(new Window());
        public Window CreateKeybindingsWindow(KeybindingWindowConfig config) => Track(new Window());
        public Window CreateSettingsWindow(SettingsWindowConfig config) => Track(new Window());
        public Window CreateSingleImageResizeWindow(MainWindowViewModel vm) => Track(new Window());
        public Window CreatePrintPreviewWindow(PrintWindowConfig config) => Track(new Window());

        public ISystemPrinters CreateSystemPrinters() => throw new NotSupportedException();

        public Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath) => Task.CompletedTask;

        private Window Track(Window window)
        {
            Created.Add(window);
            return window;
        }
    }

    /// <summary>
    /// A platform that does nothing natively, and finds no files around the one it is asked about.
    /// </summary>
    private sealed class NoPlatform : IPlatformSpecificService
    {
        public void SetTaskbarProgress(ulong progress, ulong maximum) { }
        public void StopTaskbarProgress() { }
        public void SetCursorPos(int x, int y) { }
        public void DisableScreensaver() { }
        public void EnableScreensaver() { }
        public List<FileInfo> GetFiles(FileInfo fileInfo) => [];
        public int CompareStrings(string str1, string str2) => string.CompareOrdinal(str1, str2);
        public void OpenWith(string path) { }
        public void LocateOnDisk(string path) { }
        public void ShowFileProperties(string path) { }
        public Task SetAsWallpaper(string path, int wallpaperStyle) => Task.CompletedTask;
        public bool SetAsLockScreen(string path) => false;
        public Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory) => Task.FromResult(false);
        public string DefaultJsonKeyMap() => "{}";
        public void InitiateFileAssociationService() { }
        public Task<bool> DeleteFile(string path, bool recycle) => Task.FromResult(false);
        public bool CanMoveToRecycleBin(string path) => true;
        public Task<bool> CutFile(string path) => Task.FromResult(false);

        public byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight)
        {
            pixelWidth = pixelHeight = 0;
            return null;
        }

        public byte[]? GetPixelsFromNativeImagingComponent(string path, out int pixelWidth, out int pixelHeight)
        {
            pixelWidth = pixelHeight = 0;
            return null;
        }
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }

    private sealed class NoOpModeAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state)
        {
        }
    }
}
