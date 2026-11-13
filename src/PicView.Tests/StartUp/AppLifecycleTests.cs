using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Printing;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.Views.Menu;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ColorHandling;
using PicView.Core.Config;
using PicView.Core.Favorites;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.ImageDecoding;
using PicView.Core.IPlatform;
using PicView.Core.Keybindings;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Navigation.Tiff;
using PicView.Core.Sizing;
using PicView.Core.Titles;
using PicView.Core.Update;
using PicView.Core.ViewModels;
using PicView.Tests.FileHistory;
using PicView.Tests.Navigation;
using PicView.Tests.WindowBehavior;
using R3;

namespace PicView.Tests.StartUp;

[Collection("Sequential")]
public class AppLifecycleTests : IAsyncDisposable
{
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly ConfigFileBackup _batchResizeConfigBackup = new(new BatchResizeWindowConfig());
    private readonly ConfigFileBackup _effectsConfigBackup = new(new EffectsWindowConfig());
    private readonly ConfigFileBackup _convertConfigBackup = new(new ConvertWindowConfig());
    private readonly List<AppLifecycle> _apps = [];
    private readonly string _folder;
    private readonly CoreViewModel _core;
    private readonly NoPlatform _platform = new();
    private readonly FolderSharedFiles _sharedFiles;
    private readonly LifecycleWindowProvider _provider;
    private readonly List<string> _steps = [];
    private int _exits;

    // Bounds each wait on a held load, so a failing test fails instead of hanging the run
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    // How long a step gets to end before the file shows, which is the bug the held-load test catches
    private static readonly TimeSpan EarlyEndWait = TimeSpan.FromMilliseconds(500);

    // A test holds the load of the file at _heldPath: the image loader waits for _held, and says when it is asked
    private string? _heldPath;
    private readonly TaskCompletionSource<ImageModel> _held = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _heldRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // More held loads, for a test that holds several files, by path
    private readonly ConcurrentDictionary<string, HeldLoad> _holds = new(StringComparer.Ordinal);

    public AppLifecycleTests()
    {
        SetDefaults();
        TranslationManager.Init();
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        _folder = Path.Combine(Path.GetTempPath(), "PicViewAppLifecycleTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _core = new CoreViewModel(_platform, file =>
        {
            if (_holds.TryGetValue(file.FullName, out var hold))
            {
                hold.Requested.TrySetResult();
                return new ValueTask<ImageModel>(hold.Model.Task);
            }

            if (file.FullName == _heldPath)
            {
                _heldRequested.TrySetResult();
                return new ValueTask<ImageModel>(_held.Task);
            }

            return ValueTask.FromResult(
                file.Extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
                    ? ThreePageTiff(file)
                    : new ImageModel { FileInfo = file });
        }, new GlobalSettingsViewModel(static () => ValueTask.FromResult(true)));
        _provider = new LifecycleWindowProvider(_core);
        _sharedFiles = new FolderSharedFiles(_folder);
    }

    public async ValueTask DisposeAsync()
    {
        // Let a load that a test left held end, so shutting down does not wait for it
        foreach (var hold in _holds.Values)
        {
            hold.Model.TrySetResult(new ImageModel());
        }

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
        _convertConfigBackup.Dispose();
    }

    [AvaloniaFact]
    public async Task StartAsync_WithoutAFile_ShowsTheNoImageTitle()
    {
        TranslationManager.LoadLanguage("en");
        var app = CreateApp();

        await app.StartAsync(settingsExists: true, []);

        var tab = ViewModelOf(app.MainWindows.Open[0]).WindowTabs.ActiveTab.CurrentValue;
        Assert.Equal(TitlePhase.None, tab.Titles.Phase);
        Assert.Equal(TranslationManager.Translation.NoImage, tab.Titles.Title);
    }

    [AvaloniaFact]
    public async Task OpenAtAppStart_WithASource_ShowsTheViewerBeforeTheImage()
    {
        var a = await WriteImageAsync("a.jpg");
        var app = CreateApp();

        var (window, setUp) = app.MainWindows.OpenAtAppStart(a, null);
        // Showing the window at startup lays out its bars
        window.SharedTitleBar = new CustomTitleBar();
        window.SharedBottomBar = new BottomBar();

        Assert.IsType<ImageViewer>(ViewOf(window));
        await setUp;
    }

    [AvaloniaFact]
    public async Task OpenAtAppStart_WithASource_FitsTheFirstImageThroughTheFitter()
    {
        // Without AutoFit the window is shown at once, so the first image's Fit comes only from the fitter's NewImage
        // input, which must be attached before the image shows
        Settings.WindowProperties.AutoFit = false;
        Settings.UIProperties.OpenInSameWindow = false;
        _provider.GivesBars = true;
        var a = WritePng("a.png");
        var app = CreateApp();

        await app.StartAsync(settingsExists: true, ["PicView", a]);
        var vm = ViewModelOf(app.MainWindows.Open[0]);
        // The source opens on another thread
        var deadline = DateTime.UtcNow + WaitLimit;
        while (!_provider.Fits.Any(static fitted => fitted.Cause is FitCause.NewImage) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
            Dispatcher.UIThread.RunJobs();
        }

        // The headless window has no laid-out content, so a Fixed Window fits to nothing; the Fit is what is under test
        Assert.False(double.IsNaN(vm.ImageWidth.Value));
        Assert.Contains(_provider.Fits, static fitted => fitted.Cause is FitCause.NewImage);
    }

    [AvaloniaFact]
    public void OpenAtAppStart_WithNothing_HasItsMenuBeforeItIsShown()
    {
        var app = CreateApp();

        var (window, _) = app.MainWindows.OpenAtAppStart(null, null);

        Assert.IsType<StartUpMenu>(ViewOf(window));
    }

    [AvaloniaFact]
    public async Task AppStart_AutoFit_OpensCenteredOnItsScreen()
    {
        Settings.WindowProperties.AutoFit = true;
        Settings.WindowProperties.Left = 123;
        Settings.WindowProperties.Top = 45;
        var app = CreateApp();

        await app.StartAsync(settingsExists: true, []);
        Dispatcher.UIThread.RunJobs();

        var window = app.MainWindows.Open[0];
        Assert.Equal(WindowStartupLocation.CenterScreen, window.WindowStartupLocation);
        // The headless platform finds no screen from the window, so its fitter reads a fallback screen; the screen the
        // window opens on, which CenterScreen centers it in, is the platform's only one
        var opensOn = window.Screens.Primary!;
        var screen = new ScreenSize
        {
            X = opensOn.Bounds.X, Y = opensOn.Bounds.Y, Width = opensOn.Bounds.Width, Height = opensOn.Bounds.Height,
            WorkingAreaX = opensOn.WorkingArea.X, WorkingAreaY = opensOn.WorkingArea.Y,
            WorkingAreaWidth = opensOn.WorkingArea.Width, WorkingAreaHeight = opensOn.WorkingArea.Height,
            Scaling = opensOn.Scaling
        };
        var centered = WindowPlacement.Centered(screen, window.ClientSize.Width, window.ClientSize.Height);
        // Avalonia centers a CenterScreen window itself, so it may round a pixel apart from WindowPlacement
        Assert.InRange(window.Position.X, centered.X - 1, centered.X + 1);
        Assert.InRange(window.Position.Y, centered.Y - 1, centered.Y + 1);
    }

    [AvaloniaFact]
    public async Task AppStart_AutoFitWithAnImage_KeepsTheCenterItOpenedAtThroughItsFirstResize()
    {
        Settings.WindowProperties.AutoFit = true;
        Settings.WindowProperties.KeepCentered = false;
        Settings.UIProperties.OpenInSameWindow = false;
        _provider.GivesBars = true;
        var a = WritePng("a.png", 640, 480);
        PixelPoint? centerWhenOpened = null;
        Size? sizeWhenOpened = null;
        _provider.OnCreated = window =>
        {
            // The headless window has no view to size it, so it is given content that takes the size its image is
            // fitted to, as the main view's image does; the window shows at the Fit's window size, bars included, and
            // shrinks to that content once it follows it again
            var vm = ViewModelOf(window);
            var content = new Border();
            vm.ImageWidth.Subscribe(width => content.Width = width).AddTo(window.Disposables);
            vm.ImageHeight.Subscribe(height => content.Height = height).AddTo(window.Disposables);
            window.Content = content;
            window.Opened += (_, _) =>
            {
                centerWhenOpened = CenterOf(window);
                sizeWhenOpened = window.ClientSize;
            };
        };
        var app = CreateApp();

        await app.StartAsync(settingsExists: true, ["PicView", a]);
        // The source opens on another thread
        var deadline = DateTime.UtcNow + WaitLimit;
        while (!_provider.Fits.Any(static fitted => fitted.Cause is FitCause.NewImage) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
            Dispatcher.UIThread.RunJobs();
        }

        Dispatcher.UIThread.RunJobs();

        var window = app.MainWindows.Open[0];
        Assert.Equal(WindowStartupLocation.CenterScreen, window.WindowStartupLocation);
        Assert.NotNull(sizeWhenOpened);
        Assert.Equal(new Size(640, 480), window.ClientSize);
        Assert.NotEqual(sizeWhenOpened, window.ClientSize);
        // Avalonia centers a CenterScreen window itself, so it may round a pixel apart from the fitter's placement
        var center = CenterOf(window);
        Assert.InRange(center.X, centerWhenOpened!.Value.X - 1, centerWhenOpened.Value.X + 1);
        Assert.InRange(center.Y, centerWhenOpened.Value.Y - 1, centerWhenOpened.Value.Y + 1);
    }

    private static PixelPoint CenterOf(Window window) =>
        new(window.Position.X + (int)window.ClientSize.Width / 2, window.Position.Y + (int)window.ClientSize.Height / 2);

    [AvaloniaFact]
    public async Task AppStart_FixedWindow_OpensAtItsLastPosition()
    {
        Settings.WindowProperties.AutoFit = false;
        Settings.WindowProperties.Left = 123;
        Settings.WindowProperties.Top = 45;
        var app = CreateApp();

        await app.StartAsync(settingsExists: true, []);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new PixelPoint(123, 45), app.MainWindows.Open[0].Position);
    }

    [AvaloniaFact]
    public async Task AppStart_Maximized_OpensOnItsLastScreen()
    {
        Settings.WindowProperties.Maximized = true;
        Settings.WindowProperties.Left = 123;
        Settings.WindowProperties.Top = 45;
        PixelPoint? positionWhenMaximized = null;
        _provider.OnCreated = window => ViewModelOf(window).WindowModeState.Subscribe(state =>
        {
            if (state.Mode is WindowMode.Maximized && positionWhenMaximized is null)
            {
                positionWhenMaximized = window.Position;
            }
        });
        var app = CreateApp();

        var (_, setUp) = app.MainWindows.OpenAtAppStart(null, null);
        await setUp;
        Dispatcher.UIThread.RunJobs();

        // The headless platform does not maximize natively, and reports the window back as Normal once it shows, so
        // the moment under test is the Window Mode turning Maximized: the window is on its last screen by then
        Assert.Equal(new PixelPoint(123, 45), positionWhenMaximized);
    }

    [AvaloniaTheory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task OpenNew_OpensOnTopOfItsOpener(bool keepCentered, bool autoFit)
    {
        Settings.WindowProperties.KeepCentered = keepCentered;
        Settings.WindowProperties.AutoFit = autoFit;
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        Dispatcher.UIThread.RunJobs();
        var opener = app.MainWindows.Open[0];
        // The headless window has no view to size it, so it is given content of a size, which an AutoFit window takes,
        // and that size, which a window that keeps its size takes; then a place off-center
        opener.Content = new Border { Width = 640, Height = 480 };
        opener.Width = 640;
        opener.Height = 480;
        Dispatcher.UIThread.RunJobs();
        opener.Position = new PixelPoint(123, 45);

        var opened = await app.MainWindows.OpenNewAsync(opener);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new PixelPoint(123, 45), opener.Position);
        Assert.Equal(new Size(640, 480), opener.Bounds.Size);
        Assert.Equal(opener.Position, opened.Position);
        Assert.Equal(opener.Bounds.Width, opened.Width);
        Assert.Equal(opener.Bounds.Height, opened.Height);
        Assert.Equal(opener.Bounds.Size, opened.Bounds.Size);
    }

    [AvaloniaFact]
    public async Task MovedTab_OpensAtTheCursor()
    {
        Settings.WindowProperties.AutoFit = true;
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = ViewModelOf(app.MainWindows.Open[0]);
        var tab = first.WindowTabs.CreateTab();
        first.WindowTabs.MoveOut(tab);

        var moved = await app.MainWindows.OpenForMovedTabAsync(tab, new PixelPoint(40, 40), 800, 600);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new PixelPoint(40, 40), moved.Position);
    }

    [AvaloniaFact]
    public async Task MovedTab_StartUpMenuInAnAutoFitWindow_OpensAtTheOriginsSize()
    {
        Settings.WindowProperties.AutoFit = true;
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = ViewModelOf(app.MainWindows.Open[0]);
        var tab = first.WindowTabs.CreateTab();
        first.WindowTabs.MoveOut(tab);

        var moved = await app.MainWindows.OpenForMovedTabAsync(tab, new PixelPoint(40, 40), 800, 600);
        Dispatcher.UIThread.RunJobs();

        Assert.IsType<StartUpMenu>(tab.CurrentView.Value);
        Assert.Equal(new Size(800, 600), moved.Bounds.Size);
    }

    [AvaloniaFact]
    public async Task Open_WithAutoFitAndStayCenteredOff_KeepsTheWindowsPosition()
    {
        Settings.WindowProperties.AutoFit = true;
        Settings.WindowProperties.KeepCentered = false;
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var a = await WriteImageAsync("a.jpg");
        var b = await WriteImageAsync("b.jpg");
        // Both the same pixel size, so the second open fits the window to the size it has
        Hold(a).Model.SetResult(SizedModel(a));
        Hold(b).Model.SetResult(SizedModel(b));
        await SourceOpener.OpenAsync(window, _core, a);
        Dispatcher.UIThread.RunJobs();
        window.Position = new PixelPoint(123, 45);

        await SourceOpener.OpenAsync(window, _core, b);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new PixelPoint(123, 45), window.Position);
    }

    [AvaloniaFact]
    public async Task TopMost_Changed_EveryMainWindowFollows()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, _provider.Created.Count);
        Assert.All(_provider.Created, w => Assert.False(w.Topmost));

        _core.GlobalSettings.IsTopMost.Value = true;
        Dispatcher.UIThread.RunJobs();

        Assert.All(_provider.Created, w => Assert.True(w.Topmost));
    }

    [AvaloniaFact]
    public async Task StartAsync_ComposesTheServicesBeforeTheFirstWindowOpens()
    {
        var app = CreateApp();

        await app.StartAsync(settingsExists: true, []);

        Assert.NotNull(_core.Services);
        Assert.NotNull(_core.SharedNavigationService);
        // The first tab was created with them, before any source was opened
        var tab = ViewModelOf(app.MainWindows.Open[0]).WindowTabs.ActiveTab.CurrentValue;
        Assert.False(tab.IsInitialized);
        Assert.Same(_core.Services, tab.Services);
    }

    [AvaloniaFact]
    public async Task StartAsync_SetsTheImageBackgroundFromTheSettings()
    {
        Settings.UIProperties.BgColorChoice = (int)BackgroundType.White;
        Settings.UIProperties.IsConstrainBackgroundColorEnabled = false;
        var app = CreateApp();
        // PicView's App sets its core as the app's data context before App Start
        Application.Current!.DataContext = _core;
        try
        {
            await app.StartAsync(settingsExists: true, []);

            var settings = _core.GlobalSettings;
            Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(settings.ImageBackground.Value).Color);
            Assert.Equal(Colors.Transparent,
                Assert.IsType<SolidColorBrush>(settings.ConstrainedImageBackground.Value).Color);
            Assert.Equal((int)BackgroundType.White, settings.BackgroundChoice.Value);
        }
        finally
        {
            Application.Current.DataContext = null;
        }
    }

    [AvaloniaFact]
    public async Task StartAsync_WithTheBackgroundConstrained_SetsTheConstrainedImageBackgroundFromTheSettings()
    {
        Settings.UIProperties.BgColorChoice = (int)BackgroundType.White;
        Settings.UIProperties.IsConstrainBackgroundColorEnabled = true;
        var app = CreateApp();
        Application.Current!.DataContext = _core;
        try
        {
            await app.StartAsync(settingsExists: true, []);

            var settings = _core.GlobalSettings;
            Assert.Equal(Colors.Transparent, Assert.IsType<SolidColorBrush>(settings.ImageBackground.Value).Color);
            Assert.Equal(Colors.White,
                Assert.IsType<SolidColorBrush>(settings.ConstrainedImageBackground.Value).Color);
        }
        finally
        {
            Application.Current.DataContext = null;
        }
    }

    [AvaloniaFact]
    public async Task OpenNewAsync_AfterAppStart_KeepsFileHistoryAddedSinceAppStart()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var path = Path.Combine(_folder, "seen.jpg");
        FileHistoryManager.Add(path);

        await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);

        Assert.Contains(FileHistoryManager.AllEntries, entry => entry.Path == path);
    }

    [AvaloniaFact]
    public async Task CloseAsync_MainWindowThatIsNotTheLast_KeepsTempFilesUntilAppShutdown()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var second = await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);
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

        var opened = ViewModelOf(await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]));
        var tab = first.WindowTabs.CreateTab();
        first.WindowTabs.MoveOut(tab);
        var moved = ViewModelOf(await app.MainWindows.OpenForMovedTabAsync(tab, new PixelPoint(40, 40), 800, 600));

        Assert.Equal(SizeDefaults.MainTitlebarHeight, first.TitlebarHeight.Value);
        Assert.Equal(SizeDefaults.BottombarHeight, first.BottombarHeight.Value);
        foreach (var vm in new[] { opened, moved })
        {
            Assert.Equal(first.TitlebarHeight.Value, vm.TitlebarHeight.Value);
            Assert.Equal(first.BottombarHeight.Value, vm.BottombarHeight.Value);
            Assert.True(vm.IsUIShown.Value);
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
        var second = await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);

        first.MainWindowInitializer.ShowAboutWindow();
        second.MainWindowInitializer.ShowAboutWindow();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        Assert.Equal(1, _provider.AboutWindowsCreated);
    }

    [AvaloniaFact]
    public async Task ShowEffectsWindow_FromTwoMainWindows_OpensOneEffectsWindow()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        var second = await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);

        await first.MainWindowInitializer.ShowEffectsWindow();
        await second.MainWindowInitializer.ShowEffectsWindow();

        Assert.Single(_provider.EffectsWindows);
    }

    [AvaloniaFact]
    public async Task CloseAsync_MainWindowEffectsWasOpenedFrom_KeepsEffectsOpenForTheOtherMainWindow()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);
        await first.MainWindowInitializer.ShowEffectsWindow();
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
        await main.MainWindowInitializer.ShowEffectsWindow();
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
        var second = await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);
        await second.MainWindowInitializer.ShowConvertWindow();
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
        var second = await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);

        await first.MainWindowInitializer.ShowBatchResizeWindow();
        await second.MainWindowInitializer.ShowBatchResizeWindow();
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
        main.MainWindowInitializer.ShowAboutWindow();
        await main.MainWindowInitializer.ShowBatchResizeWindow();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        var about = Assert.Single(_provider.AboutWindows);
        var aboutClosed = false;
        about.Closed += (_, _) => aboutClosed = true;

        await app.MainWindows.CloseAsync(main);
        var exitsWhileOpen = _exits;
        Assert.Single(_provider.BatchResizeWindows).Close();
        // App Shutdown saves files with asynchronous writes, so it can end after more than one dispatcher pass
        var waited = Stopwatch.StartNew();
        while (_exits == 0 && waited.Elapsed < WaitLimit)
        {
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            if (_exits == 0)
            {
                await Task.Delay(10);
            }
        }

        Assert.True(aboutClosed);
        Assert.Equal(0, exitsWhileOpen);
        Assert.True(_exits == 1, $"App Shutdown ran {_exits} times within {WaitLimit.TotalSeconds}s; expected once");
        Assert.Null(_core.BatchResize);
    }

    [AvaloniaFact]
    public async Task ShutdownAsync_WithBatchResizeOpen_ClosesIt()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        await app.MainWindows.Open[0].MainWindowInitializer.ShowBatchResizeWindow();
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
        var second = await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);
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
    public async Task ShutdownAsync_StartsNoNewPicView()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);

        await app.ShutdownAsync();

        Assert.Equal(["exit"], _steps);
    }

    [AvaloniaFact]
    public async Task RestartAsync_RunsAppShutdownBeforeStartingANewPicView()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);
        var firstFile = Path.Combine(_folder, "first.jpg");
        ViewModelOf(first).WindowTabs.ActiveTab.CurrentValue.Display(new ImageModel { FileInfo = new FileInfo(firstFile) });
        first.Activate();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        var tempFile = TempFileManager.GetNewTempFilePath("download.jpg");
        await File.WriteAllTextAsync(tempFile, "image");
        var savesBeforeStart = 0;
        var tempFileKept = true;

        await app.RestartAsync(() =>
        {
            savesBeforeStart = _sharedFiles.Saves;
            tempFileKept = File.Exists(tempFile);
            _steps.Add("before start");
            return Task.CompletedTask;
        });

        Assert.Empty(app.MainWindows.Open);
        Assert.Equal(firstFile, Settings.StartUp.LastFile);
        Assert.Equal(1, savesBeforeStart);
        Assert.False(tempFileKept);
        Assert.Equal(["before start", "start new PicView", "exit"], _steps);
    }

    [AvaloniaFact]
    public async Task RestartAsync_AfterAppShutdownBegan_StartsNoNewPicView()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);

        await app.ShutdownAsync();
        await app.RestartAsync();

        Assert.Equal(["exit"], _steps);
    }

    [AvaloniaFact]
    public async Task OpenLaunchFileAsync_AfterAppStart_OpensInTheMostRecentlyActiveMainWindow()
    {
        Settings.UIProperties.OpenInSameWindow = true;
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var first = app.MainWindows.Open[0];
        var second = await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);
        first.Activate();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        var firstView = ViewOf(first);
        var secondView = ViewOf(second);
        // A file that loads, so the window it opens in shows its viewer
        var launched = await WriteImageAsync("launched.jpg");

        await app.OpenLaunchFileAsync(launched);
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
        var second = await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);
        await ActivateAsync(first);
        var firstView = ViewOf(first);
        var secondView = ViewOf(second);
        // A file that loads, so the window it opens in shows its viewer
        var opened = await WriteImageAsync("opened.jpg");

        await SourceOpener.OpenAsync(second, _core, opened);
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
        var second = await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);
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
        var second = await app.MainWindows.OpenNewAsync(app.MainWindows.Open[0]);
        var path = Path.Combine(_folder, "found.jpg");
        await File.WriteAllTextAsync(path, "image");
        // The dialog is only shown while browsing
        OpenActiveTab(first);
        OpenActiveTab(second);
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

    [AvaloniaFact]
    public async Task FileSearchDialog_FilePickedWhileShowingSingleImage_LoadsIt()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var path = Path.Combine(_folder, "found.jpg");
        await File.WriteAllTextAsync(path, "image");
        OpenActiveTab(window);
        var tab = ViewModelOf(window).WindowTabs.ActiveTab.CurrentValue;
        // Showing a single image, such as a clipboard image or a URL, drops the iterator
        tab.DisposeImageIterator();
        Application.Current!.DataContext = _core;
        try
        {
            var dialog = new FileSearchDialog();
            window.Content = dialog;

            await dialog.LoadSelectedFileAsync(path, CancellationToken.None);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

            Assert.NotNull(tab.ImageIterator);
            Assert.Equal(path, tab.Model.FileInfo?.FullName);
        }
        finally
        {
            Application.Current.DataContext = null;
        }
    }

    [AvaloniaFact]
    public async Task FileHistoryPrev_AfterAClipboardImage_ShowsTheFile_AndFileHistoryNextTheSameImageAgain()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var file = Path.Combine(_folder, "viewed.png");
        await File.WriteAllBytesAsync(file, [0x89, 0x50, 0x4E, 0x47], TestContext.Current.CancellationToken);
        Application.Current!.DataContext = _core;
        try
        {
            await SourceOpener.OpenAsync(window, _core, file);
            var bitmap = new WriteableBitmap(new PixelSize(4, 4), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
            UpdateImage.SetSingleImage(vm, bitmap, SingleImageType.Clipboard, "Clipboard image");
            var pasted = tab.Model;

            await vm.ActionRunner!.Run(Actions.FileHistoryPrev);
            var shownAfterBack = tab.Model.FileInfo?.FullName;
            await vm.ActionRunner.Run(Actions.FileHistoryNext);

            Assert.Equal(file, shownAfterBack);
            Assert.Same(pasted, tab.Model);
            Assert.Equal(SingleImageType.Clipboard, tab.SingleImageType);
            Assert.Equal(2, tab.ViewHistory.Count);
        }
        finally
        {
            Application.Current.DataContext = null;
        }
    }

    [AvaloniaFact]
    public async Task FileHistoryPrevAndNext_AtTheEndsOfTheViewHistory_DoNotOpenTheLastFile()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var file = Path.Combine(_folder, "viewed.png");
        var lastFile = Path.Combine(_folder, "last.png");
        await File.WriteAllBytesAsync(file, [0x89, 0x50, 0x4E, 0x47], TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(lastFile, [0x89, 0x50, 0x4E, 0x47], TestContext.Current.CancellationToken);
        Application.Current!.DataContext = _core;
        try
        {
            await SourceOpener.OpenAsync(window, _core, file);
            Settings.StartUp.LastFile = lastFile;

            // The only entry is both the oldest and the newest
            await vm.ActionRunner!.Run(Actions.FileHistoryPrev);
            await vm.ActionRunner.Run(Actions.FileHistoryNext);

            Assert.Equal(file, tab.Model.FileInfo?.FullName);
            Assert.Equal(1, tab.ViewHistory.Count);
        }
        finally
        {
            Application.Current.DataContext = null;
        }
    }

    [AvaloniaFact]
    public async Task FileHistoryPrev_AfterTwoDownloads_ShowsTheFirstAgainWithItsUrl()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        const string firstUrl = "https://example.com/first.png";
        const string secondUrl = "https://example.com/second.png";
        // A download sets its URL before it shows the image
        tab.SourceURL = firstUrl;
        UpdateImage.SetSingleImage(vm, NewBitmap(), SingleImageType.Url, firstUrl);
        var first = tab.Model;
        tab.SourceURL = secondUrl;
        UpdateImage.SetSingleImage(vm, NewBitmap(), SingleImageType.Url, secondUrl);

        await vm.ActionRunner!.Run(Actions.FileHistoryPrev);
        // The Fit that start-up queued runs now, since nothing here lets the dispatcher idle before the window closes
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        Assert.Same(first, tab.Model);
        Assert.Equal(firstUrl, tab.SourceURL);
        Assert.Equal(0, tab.ViewHistory.Cursor);
        Assert.Equal(2, tab.ViewHistory.Count);
    }

    [AvaloniaFact]
    public async Task FileHistoryPrev_WhenTheUserSwitchesTabsMidStep_ShowsNothingInTheOtherTab()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        var first = Path.Combine(_folder, "first.png");
        var second = Path.Combine(_folder, "second.png");
        await File.WriteAllBytesAsync(first, [0x89, 0x50, 0x4E, 0x47], TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(second, [0x89, 0x50, 0x4E, 0x47], TestContext.Current.CancellationToken);
        var tabA = vm.WindowTabs.ActiveTab.CurrentValue;
        tabA.Display(new ImageModel { FileInfo = new FileInfo(first) });
        tabA.Display(new ImageModel { FileInfo = new FileInfo(second) });
        var tabB = vm.WindowTabs.CreateTab();
        vm.WindowTabs.SelectTab(tabA);
        Application.Current!.DataContext = _core;
        try
        {
            ValueTask stepping;
            // BeginStep waits for this lock on its pool thread, after the step has taken tab A and before it can show
            var gate = LockOf(tabA);
            gate.Enter();
            try
            {
                stepping = vm.ActionRunner!.Run(Actions.FileHistoryPrev);
                Assert.False(stepping.IsCompleted);
                vm.WindowTabs.SelectTab(tabB);
            }
            finally
            {
                gate.Exit();
            }

            await stepping;

            Assert.Null(tabB.Model.FileInfo);
            Assert.Equal(0, tabB.ViewHistory.Count);
            Assert.Equal(second, tabA.Model.FileInfo?.FullName);
            Assert.Equal(1, tabA.ViewHistory.Cursor);
            // The dropped step ended, so the next one counts from the cursor again
            Assert.Equal(first, tabA.ViewHistory.BeginStep(forwards: false)?.Target.Path);
        }
        finally
        {
            Application.Current.DataContext = null;
        }
    }

    [AvaloniaFact]
    public async Task FileHistoryPrevAndNext_BetweenPagesOfTheShownTiff_TurnThePageWithoutOpeningTheFile()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var tiff = Path.Combine(_folder, "pages.tif");
        await File.WriteAllBytesAsync(tiff, [0x49, 0x49, 0x2A, 0x00], TestContext.Current.CancellationToken);
        Application.Current!.DataContext = _core;
        try
        {
            await SourceOpener.OpenAsync(window, _core, tiff);
            var model = tab.Model;
            tab.ShowPage(1);
            tab.ShowPage(2);
            // A load that begins after this one makes it stale, which tells whether a step opened the file
            var load = tab.BeginLoad();

            await vm.ActionRunner!.Run(Actions.FileHistoryPrev);
            var pageAfterBack = model.TiffNavigation!.CurrentPage;
            var imageAfterBack = model.Image;
            await vm.ActionRunner.Run(Actions.FileHistoryPrev);
            var pageAfterSecondBack = model.TiffNavigation.CurrentPage;
            await vm.ActionRunner.Run(Actions.FileHistoryNext);

            Assert.Equal(1, pageAfterBack);
            Assert.Same(model.TiffNavigation.Pages![1], imageAfterBack);
            Assert.Equal(0, pageAfterSecondBack);
            Assert.Equal(1, model.TiffNavigation.CurrentPage);
            Assert.Same(model, tab.Model);
            Assert.True(tab.IsLatestLoad(load));
            Assert.Equal(1, tab.ViewHistory.Cursor);
            Assert.Equal([0, 1, 2], tab.ViewHistory.Entries.Select(entry => entry.Page));
        }
        finally
        {
            Application.Current.DataContext = null;
        }
    }

    [AvaloniaFact]
    public async Task FileHistoryPrev_FromAnotherFileToATiffPage_OpensTheFileAtThatPage()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var tiff = Path.Combine(_folder, "pages.tif");
        var other = Path.Combine(_folder, "other.png");
        await File.WriteAllBytesAsync(tiff, [0x49, 0x49, 0x2A, 0x00], TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(other, [0x89, 0x50, 0x4E, 0x47], TestContext.Current.CancellationToken);
        Application.Current!.DataContext = _core;
        try
        {
            await SourceOpener.OpenAsync(window, _core, tiff);
            tab.ShowPage(1);
            await SourceOpener.OpenAsync(window, _core, other);
            // Opening a file from a page of a TIFF goes to that file, it doesn't turn the page
            Assert.Equal(other, tab.Model.FileInfo?.FullName);

            await vm.ActionRunner!.Run(Actions.FileHistoryPrev);

            Assert.Equal(tiff, tab.Model.FileInfo?.FullName);
            Assert.Equal(1, tab.Model.TiffNavigation!.CurrentPage);
            Assert.Same(tab.Model.TiffNavigation.Pages![1], tab.Model.Image);
            Assert.Equal(1, tab.ViewHistory.Cursor);
            // The file opened at page 1 at once. Page 0 would have been recorded as something else showing, and ended the step
            Assert.Equal([tiff, tiff, other], tab.ViewHistory.Entries.Select(entry => entry.Path));
            Assert.Equal([0, 1, -1], tab.ViewHistory.Entries.Select(entry => entry.Page));
        }
        finally
        {
            Application.Current.DataContext = null;
        }
    }

    [AvaloniaFact]
    public async Task FileHistoryPrevAndNext_BetweenFilesInOtherFolders_ShowThemWithoutRecordingThemAsNewViews()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var a = Path.Combine(Directory.CreateDirectory(Path.Combine(_folder, "one")).FullName, "a.png");
        var c = Path.Combine(Directory.CreateDirectory(Path.Combine(_folder, "two")).FullName, "c.png");
        await File.WriteAllBytesAsync(a, [0x89, 0x50, 0x4E, 0x47], TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(c, [0x89, 0x50, 0x4E, 0x47], TestContext.Current.CancellationToken);
        Application.Current!.DataContext = _core;
        try
        {
            await SourceOpener.OpenAsync(window, _core, a);
            await SourceOpener.OpenAsync(window, _core, c);
            // Back loads a again, slowly, as it does once the cache has let go of its image
            _core.SharedCache.Invalidate(a);
            _heldPath = a;

            var back = vm.ActionRunner!.Run(Actions.FileHistoryPrev).AsTask();
            await _heldRequested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
            // A step that ends before the file shows ends within moments of the load being asked for. The file is then
            // recorded as a new view when it shows, which drops the entries after it
            await Task.WhenAny(back, Task.Delay(EarlyEndWait, TestContext.Current.CancellationToken));
            _held.SetResult(new ImageModel { FileInfo = new FileInfo(a) });
            await back.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
            var shownAfterBack = tab.Model.FileInfo?.FullName;
            var countAfterBack = tab.ViewHistory.Count;
            var cursorAfterBack = tab.ViewHistory.Cursor;

            await vm.ActionRunner.Run(Actions.FileHistoryNext);

            Assert.Equal(a, shownAfterBack);
            Assert.Equal(2, countAfterBack);
            Assert.Equal(0, cursorAfterBack);
            Assert.Equal(c, tab.Model.FileInfo?.FullName);
            Assert.Equal(2, tab.ViewHistory.Count);
            Assert.Equal(1, tab.ViewHistory.Cursor);
        }
        finally
        {
            Application.Current.DataContext = null;
        }
    }

    [AvaloniaFact]
    public async Task OpenAsync_WhileTheTitlebarIsHidden_ShowsTheIndicatorOnlyWhileLoading()
    {
        _core.GlobalSettings.ShowInterface.Value = false;
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        var a = await WriteImageAsync("a.jpg");
        var held = Hold(a);
        Assert.False(vm.IsTopToolbarShown.Value);

        var opening = SourceOpener.OpenAsync(window, _core, a).AsTask();
        await held.Requested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var shownWhileHeld = vm.IsLoadingIndicatorShown.Value;
        held.Model.SetResult(new ImageModel { FileInfo = new FileInfo(a) });
        var result = await opening.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.True(shownWhileHeld);
        Assert.True(result.IsLoaded);
        Assert.False(vm.IsLoadingIndicatorShown.Value);
    }

    [AvaloniaFact]
    public async Task OpenAsync_WhileTheTitlebarShows_NeverShowsTheIndicator()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        var a = await WriteImageAsync("a.jpg");
        var held = Hold(a);
        Assert.True(vm.IsTopToolbarShown.Value);
        var seen = new List<bool>();
        using var watching = vm.IsLoadingIndicatorShown.AsObservable().Subscribe(seen.Add);

        var opening = SourceOpener.OpenAsync(window, _core, a).AsTask();
        await held.Requested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var shownWhileHeld = vm.IsLoadingIndicatorShown.Value;
        held.Model.SetResult(new ImageModel { FileInfo = new FileInfo(a) });
        await opening.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.False(shownWhileHeld);
        Assert.DoesNotContain(true, seen);
    }

    [AvaloniaFact]
    public async Task TwoOverlappingLoads_KeepTheIndicatorUntilTheNewerEnds()
    {
        _core.GlobalSettings.ShowInterface.Value = false;
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        var a = await WriteImageAsync("a.jpg");
        var b = await WriteImageAsync("b.jpg");
        // Both held before any load, so a neighbour that is preloaded cannot cache one unheld for the other
        var heldA = Hold(a);
        var heldB = Hold(b);

        var openingA = SourceOpener.OpenAsync(window, _core, a).AsTask();
        await heldA.Requested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var openingB = SourceOpener.OpenAsync(window, _core, b).AsTask();
        await heldB.Requested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        // The older load gives up first, and the newer one is still loading
        heldA.Model.SetResult(new ImageModel { FileInfo = new FileInfo(a) });
        await openingA.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var shownAfterOlderEnded = vm.IsLoadingIndicatorShown.Value;
        heldB.Model.SetResult(new ImageModel { FileInfo = new FileInfo(b) });
        await openingB.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.True(shownAfterOlderEnded);
        Assert.False(vm.IsLoadingIndicatorShown.Value);
    }

    [AvaloniaFact]
    public async Task HidingTheTitlebarMidLoad_ShowsTheIndicator()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        var a = await WriteImageAsync("a.jpg");
        var held = Hold(a);

        var opening = SourceOpener.OpenAsync(window, _core, a).AsTask();
        await held.Requested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var shownWithTitlebar = vm.IsLoadingIndicatorShown.Value;
        vm.GlobalSettings.ShowInterface.Value = false;
        var shownWithoutTitlebar = vm.IsLoadingIndicatorShown.Value;
        held.Model.SetResult(new ImageModel { FileInfo = new FileInfo(a) });
        await opening.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.False(shownWithTitlebar);
        Assert.True(shownWithoutTitlebar);
        Assert.False(vm.IsLoadingIndicatorShown.Value);
    }

    [AvaloniaFact]
    public async Task SwitchingTabsWhileTheTitlebarIsHidden_ShowsTheIndicatorOfTheActiveTab()
    {
        _core.GlobalSettings.ShowInterface.Value = false;
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        var tabs = vm.WindowTabs;
        var tabA = tabs.ActiveTab.CurrentValue;
        var a = await WriteImageAsync("a.jpg");
        var held = Hold(a);

        var opening = SourceOpener.OpenAsync(window, _core, a).AsTask();
        await held.Requested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var shownInA = vm.IsLoadingIndicatorShown.Value;
        // A new tab is selected, and it is not loading
        tabs.CreateTab();
        var shownInB = vm.IsLoadingIndicatorShown.Value;
        tabs.SelectTab(tabA);
        var shownBackInA = vm.IsLoadingIndicatorShown.Value;
        held.Model.SetResult(new ImageModel { FileInfo = new FileInfo(a) });
        await opening.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.True(shownInA);
        Assert.False(shownInB);
        Assert.True(shownBackInA);
        Assert.False(vm.IsLoadingIndicatorShown.Value);
    }

    [AvaloniaFact]
    public async Task OpenAsync_IntoAConsecutiveTab_LoadsThatTabAndLeavesTheActiveOne()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var tabs = ViewModelOf(window).WindowTabs;
        var a = await WriteImageAsync("a.jpg");
        var b = await WriteImageAsync("b.jpg");
        await SourceOpener.OpenAsync(window, _core, a);
        var first = tabs.ActiveTab.CurrentValue;
        var second = tabs.CreateTab();
        // The new tab loads in the background while the first is the active one again
        tabs.SelectTab(first);

        var result = await SourceOpener.OpenAsync(window, _core, b, tab: second);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        Assert.True(result.IsLoaded);
        Assert.Equal(b, second.Model.FileInfo?.FullName);
        Assert.IsType<ImageViewer>(second.CurrentView.Value);
        Assert.True(second.IsInitialized);
        Assert.NotNull(second.Hookups);
        Assert.Same(first, tabs.ActiveTab.CurrentValue);
        Assert.Equal(a, first.Model.FileInfo?.FullName);
    }

    [AvaloniaFact]
    public async Task OpenAsync_OvertakenByANewerOpen_LeavesTheViewToIt()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        var tab = ViewModelOf(window).WindowTabs.ActiveTab.CurrentValue;
        var a = await WriteImageAsync("a.jpg");
        var b = await WriteImageAsync("b.jpg");
        var heldA = Hold(a);
        Assert.IsType<StartUpMenu>(tab.CurrentView.Value);

        // Opened from the start-up menu, then overtaken by a newer open while its image is held
        var openingA = SourceOpener.OpenAsync(window, _core, a).AsTask();
        await heldA.Requested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var resultB = await SourceOpener.OpenAsync(window, _core, b).AsTask()
            .WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        heldA.Model.SetResult(new ImageModel { FileInfo = new FileInfo(a) });
        var resultA = await openingA.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        Assert.Equal(LoadOutcome.Superseded, resultA.Outcome);
        Assert.True(resultB.IsLoaded);
        Assert.IsType<ImageViewer>(tab.CurrentView.Value);
        Assert.Equal(b, tab.Model.FileInfo?.FullName);
    }

    [AvaloniaFact]
    public async Task StartUp_WithAFile_ListsItsFilesThroughTheTabsLoad()
    {
        var watcher = new RecordingWatcher();
        // Composed before App Start, which keeps them, so the test sees what the startup open watches
        _core.InitializeNavigation(
            TestTabServices.For(_core.SharedCache, _core.SharedThumbnailCache, new TestTabServices.NoThumbnailLoader(),
                watcher, platform: new NoPlatform()),
            new NavigationService(string.CompareOrdinal));
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        // Showing the window at startup lays out its bars
        window.SharedTitleBar = new CustomTitleBar();
        window.SharedBottomBar = new BottomBar();
        var tab = ViewModelOf(window).WindowTabs.ActiveTab.CurrentValue;
        var a = WritePng("a.png");
        var b = WritePng("b.png");

        var result = await SourceOpener.OpenAsync(window, _core, a, new OpenOptions(IsStartup: true)).AsTask()
            .WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.True(result.IsLoaded);
        Assert.Equal(a, tab.Model.FileInfo?.FullName);
        Assert.Equal([a, b], tab.ImageIterator!.Files.Select(f => f.FullName));
        Assert.Same(_core.Services, tab.Services);
        Assert.Same(tab, watcher.WatchedTab);
    }

    [AvaloniaFact]
    public async Task StartUp_WhileTheOpenIsHeld_SaysLoading()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        // Showing the window at startup lays out its bars
        window.SharedTitleBar = new CustomTitleBar();
        window.SharedBottomBar = new BottomBar();
        var tab = ViewModelOf(window).WindowTabs.ActiveTab.CurrentValue;
        WritePng("a.png");
        WritePng("b.png");
        // Opening a folder at startup sorts its names before any image shows, which waits while the gate is closed
        _platform.CompareGate.Reset();

        var opening = Task.Run(() =>
            SourceOpener.OpenAsync(window, _core, _folder, new OpenOptions(IsStartup: true)).AsTask());
        await _platform.Comparing.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var phaseWhileHeld = tab.Titles.Phase;
        _platform.CompareGate.Set();
        var result = await opening.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(TitlePhase.Loading, phaseWhileHeld);
        Assert.True(result.IsLoaded);
        Assert.Equal(TitlePhase.None, tab.Titles.Phase);
    }

    [AvaloniaFact]
    public async Task StartUp_WhileTheInterfaceIsHiddenAndTheOpenIsHeld_ShowsTheIndicator()
    {
        _core.GlobalSettings.ShowInterface.Value = false;
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var vm = ViewModelOf(window);
        window.SharedTitleBar = new CustomTitleBar();
        window.SharedBottomBar = new BottomBar();
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        WritePng("a.png");
        WritePng("b.png");
        // Held before the startup path decodes and before its Load begins: opening a folder sorts its names first,
        // which waits while the gate is closed, so only the startup path's own Loading covers this time
        _platform.CompareGate.Reset();

        var opening = Task.Run(() =>
            SourceOpener.OpenAsync(window, _core, _folder, new OpenOptions(IsStartup: true)).AsTask());
        await _platform.Comparing.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var shownWhileHeld = vm.IsLoadingIndicatorShown.Value;
        var loadingWhileHeld = tab.Load.IsLoading.Value;
        _platform.CompareGate.Set();
        var result = await opening.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.False(vm.IsTopToolbarShown.Value);
        Assert.False(loadingWhileHeld);
        Assert.True(shownWhileHeld);
        Assert.True(result.IsLoaded);
        Assert.False(vm.IsLoadingIndicatorShown.Value);
    }

    [AvaloniaFact]
    public async Task StartUp_OfAMissingFile_LeavesNoLoadingPhase()
    {
        var app = CreateApp();
        await app.StartAsync(settingsExists: true, []);
        var window = app.MainWindows.Open[0];
        var tab = ViewModelOf(window).WindowTabs.ActiveTab.CurrentValue;

        var result = await SourceOpener.OpenAsync(window, _core, Path.Combine(_folder, "missing.jpg"),
            new OpenOptions(IsStartup: true));

        Assert.False(result.IsLoaded);
        Assert.Equal(TitlePhase.None, tab.Titles.Phase);
    }

    private async Task<string> WriteImageAsync(string name)
    {
        var path = Path.Combine(_folder, name);
        await File.WriteAllBytesAsync(path, [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
        return path;
    }

    // A real image, for the startup path, which decodes it itself
    private string WritePng(string name, uint width = 2, uint height = 2)
    {
        var path = Path.Combine(_folder, name);
        using var image = new MagickImage(MagickColors.Red, width, height);
        image.Write(path, MagickFormat.Png);
        return path;
    }

    // Records the tab it was last asked to watch
    private sealed class RecordingWatcher : IFileWatcherService
    {
        public TabViewModel? WatchedTab { get; private set; }

        public void Watch(TabViewModel tab, string? directory = null) => WatchedTab = tab;
        public void Unwatch(TabViewModel tab) { }
        public void RestartWatches() { }
    }

    // Holds the load of the file at path until the test sets its model
    private HeldLoad Hold(string path) => _holds.GetOrAdd(path, static _ => new HeldLoad());

    private sealed class HeldLoad
    {
        public TaskCompletionSource<ImageModel> Model { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Requested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static WriteableBitmap NewBitmap() =>
        new(new PixelSize(4, 4), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

    // A 4Ã—4 bitmap as the image loader returns it, so every file given one is the same pixel size
    private static ImageModel SizedModel(string path) => new()
    {
        FileInfo = new FileInfo(path),
        Image = NewBitmap(),
        PixelWidth = 4,
        PixelHeight = 4,
        ImageType = ImageType.Bitmap
    };

    /// <summary>
    /// A multi-page TIFF as the image loader returns it: one bitmap per page, shown at the first.
    /// </summary>
    private static ImageModel ThreePageTiff(FileInfo file)
    {
        object[] pages = [NewBitmap(), NewBitmap(), NewBitmap()];
        return new ImageModel
        {
            FileInfo = file,
            Image = pages[0],
            PixelWidth = 4,
            PixelHeight = 4,
            ImageType = ImageType.Bitmap,
            TiffNavigation = new TiffNavigationInfo { PageCount = pages.Length, Pages = pages }
        };
    }

    /// <summary>
    /// The lock a tab's View History takes for each of its operations. A test holds it to stop <c>BeginStep</c> on its
    /// pool thread at a known point, which nothing else in the View History allows.
    /// </summary>
    private static Lock LockOf(TabViewModel tab) =>
        (Lock)tab.ViewHistory.GetType().GetField("_lock", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(tab.ViewHistory)!;

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
        ViewModelOf(window).WindowTabs.ActiveTab.CurrentValue.CurrentView.Value;

    private static MainWindowViewModel ViewModelOf(MainWindow window) => (MainWindowViewModel)window.DataContext!;

    // Opens the active tab of the window for the first time, as its first source does
    private void OpenActiveTab(MainWindow window)
    {
        var vm = ViewModelOf(window);
        TabNavigationInitializer.Initialize(_core, vm, vm.WindowTabs.ActiveTab.CurrentValue);
    }

    private AppLifecycle CreateApp()
    {
        var app = new AppLifecycle(_core, _provider, _sharedFiles, () =>
        {
            _exits++;
            _steps.Add("exit");
        }, () => _steps.Add("start new PicView"));
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
        public bool GivesBars { get; set; }
        public List<Fitted> Fits { get; } = [];

        // Called with each main window as it is created, before the lifecycle sizes, places or shows it
        public Action<MainWindow>? OnCreated { get; set; }
        public int AboutWindowsCreated => AboutWindows.Count;
        public List<Window> AboutWindows { get; } = [];
        public List<Window> BatchResizeWindows { get; } = [];
        public List<Window> ConvertWindows { get; } = [];
        public List<Window> EffectsWindows { get; } = [];

        public MainWindow CreateMainWindow(TabViewModel? firstTab = null)
        {
            var window = new TestMainWindow(new NoOpModeAdapter(), this);
            window.DataContext = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!,
                core.GallerySettings, firstTab);
            Created.Add(window);
            if (GivesBars)
            {
                // Showing the window at startup lays out its bars, which the open thread does before a test can set them;
                // the Fits are recorded from the start for the same reason
                window.SharedTitleBar = new CustomTitleBar();
                window.SharedBottomBar = new BottomBar();
                window.WindowFitter.Fits.Subscribe(Fits.Add);
            }

            OnCreated?.Invoke(window);
            return window;
        }

        public IWindowModeAdapter CreateWindowModeAdapter() => new NoOpModeAdapter();
        public Window CreateAboutWindow()
        {
            var window = Track(new Window());
            AboutWindows.Add(window);
            return window;
        }

        public Window CreateBatchResizeWindow()
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

        public Window CreateEffectsWindow()
        {
            var window = Track(new Window());
            EffectsWindows.Add(window);
            return window;
        }
        public Window CreateImageInfoWindow(MainWindowViewModel vm) => Track(new Window());
        public Window CreateKeybindingsWindow() => Track(new Window());
        public Window CreateSettingsWindow(SettingsWindowConfig config) => Track(new Window());
        public Window CreateSingleImageResizeWindow(MainWindowViewModel vm) => Track(new Window());
        public Window CreatePrintPreviewWindow() => Track(new Window());

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
        // While closed, every name comparison waits for it, so a test can hold a listing while it sorts; Comparing says
        // when one waits
        public ManualResetEventSlim CompareGate { get; } = new(initialState: true);
        public TaskCompletionSource Comparing { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CompareStrings(string str1, string str2)
        {
            if (!CompareGate.IsSet)
            {
                Comparing.TrySetResult();
                CompareGate.Wait(WaitLimit);
            }

            return string.CompareOrdinal(str1, str2);
        }
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
