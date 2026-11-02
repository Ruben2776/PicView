using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Printing;
using PicView.Avalonia.Services;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Sizing;
using PicView.Core.Update;
using PicView.Core.ViewModels;

namespace PicView.Tests.WindowBehavior;

[Collection("Sequential")]
public class SecondaryWindowOpenerTests
{
    private readonly FakeProvider _provider = new();
    private readonly MainWindow _mainWindow;
    private int _creates;
    private int _fileAssociationCreates;

    public SecondaryWindowOpenerTests()
    {
        _mainWindow = new TestMainWindow(new NoOpModeAdapter());
        // Avalonia refuses to show a window owned by a window that is not visible yet
        _mainWindow.Show();
    }

    [AvaloniaFact]
    public async Task OpenAsync_OpeningTwice_ShowsTheSameWindowAndCreatesOnce()
    {
        var opener = CreateOpener();

        var first = await opener.OpenAsync(SecondaryWindowKind.About, Spec());
        var second = await opener.OpenAsync(SecondaryWindowKind.About, Spec());

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Equal(1, _creates);
    }

    [AvaloniaFact]
    public async Task OpenAsync_AfterTheWindowCloses_CreatesANewOne()
    {
        var opener = CreateOpener();
        var first = await opener.OpenAsync(SecondaryWindowKind.About, Spec());

        first!.Close();
        var second = await opener.OpenAsync(SecondaryWindowKind.About, Spec());

        Assert.NotNull(second);
        Assert.NotSame(first, second);
        Assert.Equal(2, _creates);
    }

    [AvaloniaFact]
    public async Task OpenAsync_FileAssociationsReopenedAfterClosing_CreatesTheWindowAgain()
    {
        var opener = CreateOpener();
        var first = await opener.OpenAsync(SecondaryWindowKind.FileAssociations, FileAssociationsSpec());

        first!.Close();
        var second = await opener.OpenAsync(SecondaryWindowKind.FileAssociations, FileAssociationsSpec());

        Assert.NotNull(second);
        Assert.NotSame(first, second);
        Assert.Equal(2, _fileAssociationCreates);
    }

    [AvaloniaFact]
    public async Task OpenAsync_ProviderReturnsNull_ReturnsNullAndOpensAgainWithoutFailing()
    {
        _provider.NoFileAssociations = true;
        var opener = CreateOpener();

        var first = await opener.OpenAsync(SecondaryWindowKind.FileAssociations, FileAssociationsSpec());
        var second = await opener.OpenAsync(SecondaryWindowKind.FileAssociations, FileAssociationsSpec());

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(2, _fileAssociationCreates);
    }

    [AvaloniaFact]
    public async Task OpenAsync_OwnedWindow_IsOwnedByTheMainWindow()
    {
        var opener = CreateOpener();

        var window = await opener.OpenAsync(SecondaryWindowKind.About, Spec(WindowOwner.MainWindow));

        Assert.Same(_mainWindow, window!.Owner);
    }

    [AvaloniaFact]
    public async Task OpenAsync_OwnedWindowWithNoMainWindow_IsShownWithoutAnOwner()
    {
        var opener = CreateOpener(ownerWindow: () => null);

        var window = await opener.OpenAsync(SecondaryWindowKind.About, Spec(WindowOwner.MainWindow));

        Assert.Null(window!.Owner);
    }

    [AvaloniaFact]
    public async Task OpenAsync_UnownedWindow_HasNoOwner()
    {
        var opener = CreateOpener();

        var window = await opener.OpenAsync(SecondaryWindowKind.ImageInfo, Spec(WindowOwner.None));

        Assert.Null(window!.Owner);
    }

    [AvaloniaFact]
    public async Task OpenAsync_MainWindowExceptMacOS_OwnsTheWindowOnlyWhenNotOnMacOS()
    {
        var opener = CreateOpener();

        var window = await opener.OpenAsync(SecondaryWindowKind.Keybindings, Spec(WindowOwner.MainWindowExceptMacOS));

        if (OperatingSystem.IsMacOS())
        {
            Assert.Null(window!.Owner);
        }
        else
        {
            Assert.Same(_mainWindow, window!.Owner);
        }
    }

    [AvaloniaFact]
    public async Task OpenAsync_Configure_RunsBeforeTheWindowIsShown()
    {
        var opener = CreateOpener();
        var visibleWhenConfigured = true;
        var spec = Spec() with
        {
            Configure = window => visibleWhenConfigured = window.IsVisible
        };

        var window = await opener.OpenAsync(SecondaryWindowKind.About, spec);

        Assert.False(visibleWhenConfigured);
        Assert.True(window!.IsVisible);
    }

    [AvaloniaFact]
    public async Task OpenAsync_OnOpened_RunsOnlyWhenTheOpenCreatedTheWindow()
    {
        var opener = CreateOpener();
        var opened = 0;
        var spec = Spec() with
        {
            OnOpened = _ =>
            {
                opened++;
                return Task.CompletedTask;
            }
        };

        await opener.OpenAsync(SecondaryWindowKind.PrintPreview, spec);
        await opener.OpenAsync(SecondaryWindowKind.PrintPreview, spec);

        Assert.Equal(1, opened);
    }

    [AvaloniaFact]
    public async Task OpenAsync_Closing_RunsTheCloseHookWithTheWindow()
    {
        var opener = CreateOpener();
        Window? closed = null;
        var spec = Spec() with
        {
            OnClosing = window =>
            {
                closed = window;
                return Task.CompletedTask;
            }
        };
        var window = await opener.OpenAsync(SecondaryWindowKind.About, spec);

        window!.Close();

        Assert.Same(window, closed);
    }

    [AvaloniaFact]
    public async Task IsOpen_TracksTheWindowFromOpenToClose()
    {
        var opener = CreateOpener();
        Assert.False(opener.IsOpen(SecondaryWindowKind.About));

        var window = await opener.OpenAsync(SecondaryWindowKind.About, Spec());
        Assert.True(opener.IsOpen(SecondaryWindowKind.About));

        window!.Close();
        Assert.False(opener.IsOpen(SecondaryWindowKind.About));
    }

    [AvaloniaFact]
    public async Task NewestOpenWindow_IsTheLastOpenedWindowStillOpen()
    {
        var opener = CreateOpener();
        var about = await opener.OpenAsync(SecondaryWindowKind.About, Spec());
        var keybindings = await opener.OpenAsync(SecondaryWindowKind.Keybindings, Spec());
        Assert.Same(keybindings, opener.NewestOpenWindow);

        keybindings!.Close();

        Assert.Same(about, opener.NewestOpenWindow);
    }

    [AvaloniaFact]
    public async Task OpenAsync_SetsTheTitle()
    {
        var opener = CreateOpener();

        var window = await opener.OpenAsync(SecondaryWindowKind.About, Spec());

        Assert.Equal("About - PicView", window!.Title);
    }

    [AvaloniaFact]
    public async Task OpenAsync_CenterScreen_SetsTheStartupLocationBeforeShowing()
    {
        var opener = CreateOpener();
        WindowStartupLocation? recorded = null;
        var spec = Spec() with
        {
            OnOpened = window =>
            {
                recorded = window.WindowStartupLocation;
                return Task.CompletedTask;
            }
        };

        await opener.OpenAsync(SecondaryWindowKind.About, spec);

        Assert.Equal(WindowStartupLocation.CenterScreen, recorded);
    }

    [AvaloniaFact]
    public async Task OpenAsync_CenterOwner_SetsTheStartupLocation()
    {
        var opener = CreateOpener();
        var spec = Spec(WindowOwner.MainWindow) with { Placement = new SecondaryWindowPlacement.CenterOwner() };

        var window = await opener.OpenAsync(SecondaryWindowKind.About, spec);

        Assert.Equal(WindowStartupLocation.CenterOwner, window!.WindowStartupLocation);
    }

    [AvaloniaFact]
    public async Task OpenAsync_Remembered_AppliesThePositionBeforeShowing()
    {
        var opener = CreateOpener();
        PixelPoint? recorded = null;
        var spec = RememberedSpec(new Properties { Left = 120, Top = 80 }) with
        {
            OnOpened = window =>
            {
                recorded = window.Position;
                return Task.CompletedTask;
            }
        };

        var window = await opener.OpenAsync(SecondaryWindowKind.Settings, spec);

        Assert.Equal(new PixelPoint(120, 80), recorded);
        Assert.Equal(WindowStartupLocation.Manual, window!.WindowStartupLocation);
    }

    [AvaloniaFact]
    public async Task OpenAsync_RememberedMaximized_AppliesTheSizeAndPositionAndMaximizes()
    {
        var opener = CreateOpener();
        var properties = new Properties { Maximized = true, Width = 500, Height = 400, Left = 120, Top = 80 };

        var window = await opener.OpenAsync(SecondaryWindowKind.Settings, RememberedSpec(properties));

        Assert.Equal(WindowState.Maximized, window!.WindowState);
        Assert.Equal(500d, window.Width);
        Assert.Equal(400d, window.Height);
        Assert.Equal(WindowStartupLocation.Manual, window.WindowStartupLocation);
    }

    [AvaloniaFact]
    public async Task OpenAsync_RememberedOnNoScreen_CentersOnScreen()
    {
        var opener = CreateOpener();
        // The headless platform reports one screen at (0, 0)
        var properties = new Properties { Left = -5000, Top = 80 };

        var window = await opener.OpenAsync(SecondaryWindowKind.Settings, RememberedSpec(properties));

        Assert.Equal(WindowStartupLocation.CenterScreen, window!.WindowStartupLocation);
    }

    [AvaloniaFact]
    public async Task OpenAsync_RememberedWithNothingRemembered_CentersOnScreen()
    {
        var opener = CreateOpener();

        var window = await opener.OpenAsync(SecondaryWindowKind.Settings, RememberedSpec(new Properties()));

        Assert.Equal(WindowStartupLocation.CenterScreen, window!.WindowStartupLocation);
    }

    [AvaloniaFact]
    public async Task Closing_Remembered_WritesTheBoundsThenRunsOnClosingThenSaves()
    {
        var opener = CreateOpener();
        var properties = new Properties();
        List<string> order = [];
        (int? Left, int? Top) heldInOnClosing = default;
        var spec = RememberedSpec(properties, order) with
        {
            OnClosing = _ =>
            {
                heldInOnClosing = (properties.Left, properties.Top);
                order.Add("onClosing");
                return Task.CompletedTask;
            }
        };
        var window = await opener.OpenAsync(SecondaryWindowKind.Settings, spec);

        window!.Position = new PixelPoint(33, 44);
        window.Close();

        Assert.Equal(33, properties.Left);
        Assert.Equal(44, properties.Top);
        Assert.Equal((33, 44), heldInOnClosing);
        Assert.Equal(["onClosing", "save"], order);
    }

    [AvaloniaFact]
    public async Task Closing_DisposesTheWindowOnce()
    {
        _provider.DisposableAbout = true;
        var opener = CreateOpener();
        var window = await opener.OpenAsync(SecondaryWindowKind.About, RememberedSpec(new Properties()));

        window!.Close();

        Assert.Equal(1, Assert.IsType<DisposableWindow>(window).Disposals);
    }

    [AvaloniaFact]
    public async Task Escape_ClosesTheWindowAndMarksIt()
    {
        var opener = CreateOpener();
        var window = await opener.OpenAsync(SecondaryWindowKind.About, Spec());

        PressEscape(window!);

        Assert.False(opener.IsOpen(SecondaryWindowKind.About));
        Assert.True(_mainWindow.KeybindRouter.IsIgnoredUntilReleased(Key.Escape));
    }

    [AvaloniaFact]
    public async Task Close_NotByEscape_DoesNotMark()
    {
        var opener = CreateOpener();
        var window = await opener.OpenAsync(SecondaryWindowKind.About, Spec());

        window!.Close();

        Assert.False(opener.IsOpen(SecondaryWindowKind.About));
        Assert.False(_mainWindow.KeybindRouter.IsIgnoredUntilReleased(Key.Escape));
    }

    [AvaloniaFact]
    public async Task Escape_WhenTurnedOff_DoesNotClose()
    {
        var opener = CreateOpener();
        var window = await opener.OpenAsync(SecondaryWindowKind.Keybindings, Spec() with { CloseOnEscape = false });

        PressEscape(window!);

        Assert.True(opener.IsOpen(SecondaryWindowKind.Keybindings));
    }

    [AvaloniaFact]
    public async Task Escape_WhileAKeybindBoxRecords_LeavesTheWindowOpen()
    {
        var opener = CreateOpener();
        var box = new KeybindBox();
        await opener.OpenAsync(SecondaryWindowKind.Settings, Spec() with { Configure = window => window.Content = box });
        Dispatcher.UIThread.RunJobs();
        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.IsRecording);

        PressEscape(box.TagBox);

        Assert.True(opener.IsOpen(SecondaryWindowKind.Settings));
    }

    [AvaloniaFact]
    public async Task Escape_FromAFocusedNumTextBox_ClosesTheWindow()
    {
        var opener = CreateOpener();
        var box = new NumTextBox();
        await opener.OpenAsync(SecondaryWindowKind.ImageInfo, Spec() with { Configure = window => window.Content = box });
        Dispatcher.UIThread.RunJobs();
        box.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.IsFocused);

        PressEscape(box);

        Assert.False(opener.IsOpen(SecondaryWindowKind.ImageInfo));
    }

    [AvaloniaFact]
    public async Task Escape_WithNoMainWindow_StillCloses()
    {
        var opener = CreateOpener(mainWindow: () => null);
        var window = await opener.OpenAsync(SecondaryWindowKind.About, Spec());

        PressEscape(window!);

        Assert.False(opener.IsOpen(SecondaryWindowKind.About));
    }

    private SecondaryWindowOpener CreateOpener(Func<Window?>? ownerWindow = null, Func<MainWindow?>? mainWindow = null) =>
        new(mainWindow ?? (() => _mainWindow), _provider, ownerWindow ?? (() => _mainWindow));

    private SecondaryWindowSpec Spec(WindowOwner owner = WindowOwner.None) =>
        new(provider =>
        {
            _creates++;
            return provider.CreateAboutWindow();
        }, owner)
        {
            Title = "About - PicView",
            Placement = new SecondaryWindowPlacement.CenterScreen()
        };

    private SecondaryWindowSpec RememberedSpec(Properties properties, List<string>? order = null) =>
        Spec() with
        {
            Placement = new SecondaryWindowPlacement.Remembered(properties, () =>
            {
                order?.Add("save");
                return Task.CompletedTask;
            }),
            OnClosing = _ =>
            {
                order?.Add("onClosing");
                return Task.CompletedTask;
            }
        };

    private SecondaryWindowSpec FileAssociationsSpec() =>
        new(provider =>
        {
            _fileAssociationCreates++;
            return provider.CreateFileAssociationsWindow();
        }, WindowOwner.MainWindow)
        {
            Title = "About - PicView",
            Placement = new SecondaryWindowPlacement.CenterScreen()
        };

    private static void PressEscape(Interactive source) =>
        source.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Escape,
            Source = source
        });

    private sealed class Properties : IWindowProperties
    {
        public int? Top { get; set; }
        public int? Left { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public bool Maximized { get; set; }
    }

    private sealed class DisposableWindow : Window, IDisposable
    {
        public int Disposals { get; private set; }

        public void Dispose() => Disposals++;
    }

    private sealed class NoOpModeAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state)
        {
        }
    }

    private sealed class FakeProvider : IWindowProvider
    {
        public bool NoFileAssociations { get; set; }

        public bool DisposableAbout { get; set; }

        public MainWindow CreateMainWindow(TabViewModel? firstTab = null) => throw new NotSupportedException();
        public IWindowModeAdapter CreateWindowModeAdapter() => throw new NotSupportedException();
        public Window CreateAboutWindow() => DisposableAbout ? new DisposableWindow() : new Window();
        public Window CreateBatchResizeWindow() => new();
        public Window CreateFileAssociationsWindow() => NoFileAssociations ? null! : new Window();
        public Window CreateConvertWindow() => new();
        public Window CreateEffectsWindow() => new();
        public Window CreateImageInfoWindow(MainWindowViewModel vm) => new();
        public Window CreateKeybindingsWindow() => new();
        public Window CreateSettingsWindow(SettingsWindowConfig config) => new();
        public Window CreateSingleImageResizeWindow(MainWindowViewModel vm) => new();
        public Window CreatePrintPreviewWindow() => new();

        public ISystemPrinters CreateSystemPrinters() => throw new NotSupportedException();

        public Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath) => Task.CompletedTask;
    }
}
