using Avalonia.Controls;
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

    private SecondaryWindowOpener CreateOpener(Func<Window?>? ownerWindow = null) =>
        new(() => _mainWindow, _provider, ownerWindow ?? (() => _mainWindow), () => null);

    private SecondaryWindowSpec Spec(WindowOwner owner = WindowOwner.None) =>
        new(provider =>
        {
            _creates++;
            return provider.CreateAboutWindow();
        }, owner);

    private SecondaryWindowSpec FileAssociationsSpec() =>
        new(provider =>
        {
            _fileAssociationCreates++;
            return provider.CreateFileAssociationsWindow();
        }, WindowOwner.MainWindow);

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

        public MainWindow CreateMainWindow(TabViewModel? firstTab = null) => throw new NotSupportedException();
        public IWindowModeAdapter CreateWindowModeAdapter() => throw new NotSupportedException();
        public Window CreateAboutWindow() => new();
        public Window CreateBatchResizeWindow(BatchResizeWindowConfig config) => new();
        public Window CreateFileAssociationsWindow() => NoFileAssociations ? null! : new Window();
        public Window CreateConvertWindow() => new();
        public Window CreateEffectsWindow(EffectsWindowConfig config) => new();
        public Window CreateImageInfoWindow(MainWindowViewModel vm) => new();
        public Window CreateKeybindingsWindow(KeybindingWindowConfig config) => new();
        public Window CreateSettingsWindow(SettingsWindowConfig config) => new();
        public Window CreateSingleImageResizeWindow(MainWindowViewModel vm) => new();
        public Window CreatePrintPreviewWindow(PrintWindowConfig config) => new();

        public ISystemPrinters CreateSystemPrinters() => throw new NotSupportedException();

        public Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath) => Task.CompletedTask;
    }
}
