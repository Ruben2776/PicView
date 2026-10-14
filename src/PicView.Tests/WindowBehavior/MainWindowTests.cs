using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Update;
using PicView.Core.ViewModels;

namespace PicView.Tests.WindowBehavior;

[Collection("Sequential")]
public class MainWindowTests
{
    public MainWindowTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaTheory]
    [InlineData(WindowState.Maximized, nameof(TestMainWindow.Maximize))]
    [InlineData(WindowState.FullScreen, nameof(TestMainWindow.Fullscreen))]
    public async Task WindowStateChanged_WhenSettingsDoNotMatch_AppliesState(WindowState state, string expectedCall)
    {
        var window = new TestMainWindow();

        await window.ChangeWindowStateAsync(state);

        Assert.Equal([expectedCall], window.Calls);
    }

    [AvaloniaFact]
    public async Task WindowStateChanged_ToMaximizedWhenAlreadyMaximized_DoesNothing()
    {
        Settings.WindowProperties.Maximized = true;
        var window = new TestMainWindow();

        await window.ChangeWindowStateAsync(WindowState.Maximized);

        Assert.Empty(window.Calls);
    }

    [AvaloniaFact]
    public async Task WindowStateChanged_ToFullScreenWhenAlreadyFullscreen_DoesNothing()
    {
        Settings.WindowProperties.Fullscreen = true;
        var window = new TestMainWindow();

        await window.ChangeWindowStateAsync(WindowState.FullScreen);

        Assert.Empty(window.Calls);
    }

    [AvaloniaTheory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task WindowStateChanged_ToNormalWhenMaximizedOrFullscreen_Restores(bool maximized, bool fullscreen)
    {
        Settings.WindowProperties.Maximized = maximized;
        Settings.WindowProperties.Fullscreen = fullscreen;
        var window = new TestMainWindow();

        await window.ChangeWindowStateAsync(WindowState.Normal);

        Assert.Equal([nameof(TestMainWindow.Restore)], window.Calls);
    }

    [AvaloniaFact]
    public async Task WindowStateChanged_ToNormalWhenAlreadyNormal_DoesNothing()
    {
        var window = new TestMainWindow();

        await window.ChangeWindowStateAsync(WindowState.Normal);

        Assert.Empty(window.Calls);
    }

    [AvaloniaFact]
    public void MoveTab_ToAnotherWindow_RemovesItFromSourceAndSelectsItInTarget()
    {
        var core = CreateCore();
        var source = CreateWindowViewModel(core);
        var target = CreateWindowViewModel(core);
        var remaining = source.WindowTabs.ActiveTab.Value;
        var tab = new TabViewModel(_ => { }, source, null);
        source.WindowTabs.Tabs.Value.Add(tab);
        source.WindowTabs.SelectTab(tab);

        MainWindow.MoveTab(tab, source, target);

        Assert.DoesNotContain(tab, source.WindowTabs.Tabs.Value);
        Assert.Same(remaining, source.WindowTabs.ActiveTab.Value);
        Assert.Contains(tab, target.WindowTabs.Tabs.Value);
        Assert.Same(tab, target.WindowTabs.ActiveTab.Value);
        Assert.Same(target, tab.ParentWindowContext);
    }

    [AvaloniaFact]
    public void MoveTab_WhenDragPreviewAlreadyAddedIt_DoesNotDuplicateIt()
    {
        var core = CreateCore();
        var source = CreateWindowViewModel(core);
        var target = CreateWindowViewModel(core);
        var tab = new TabViewModel(_ => { }, source, null);
        source.WindowTabs.Tabs.Value.Add(tab);
        target.WindowTabs.Tabs.Value.Add(tab);

        MainWindow.MoveTab(tab, source, target);

        Assert.Single(target.WindowTabs.Tabs.Value, t => ReferenceEquals(t, tab));
        Assert.Same(tab, target.WindowTabs.ActiveTab.Value);
    }

    private static CoreViewModel CreateCore() =>
        new(null, _ => ValueTask.FromResult(new ImageModel()));

    private static MainWindowViewModel CreateWindowViewModel(CoreViewModel core) =>
        new(core, core.Translation, null!, core.GlobalSettings!, core.GallerySettings);

    private sealed class TestMainWindow : MainWindow
    {
        public List<string> Calls { get; } = [];

        public Task ChangeWindowStateAsync(WindowState state) => OnWindowStateChangedAsync(state);

        // The window is loaded when the headless dispatcher drains its queue, which creates the initializer
        protected override IWindowProvider CreateWindowProvider() => new UnsupportedWindowProvider();

        public override Task Maximize(bool saveSetting = true) => Record(nameof(Maximize));

        public override Task MaximizeRestore(bool saveSetting = true) => Record(nameof(MaximizeRestore));

        public override Task Fullscreen(bool saveSetting = true) => Record(nameof(Fullscreen));

        public override Task ToggleFullscreen(bool saveSetting = true) => Record(nameof(ToggleFullscreen));

        public override Task Restore() => Record(nameof(Restore));

        public override void Minimize() => Calls.Add(nameof(Minimize));

        private Task Record(string call)
        {
            Calls.Add(call);
            return Task.CompletedTask;
        }
    }

    private sealed class UnsupportedWindowProvider : IWindowProvider
    {
        public MainWindow CreateMainWindow() => throw new NotSupportedException();
        public Window CreateAboutWindow() => throw new NotSupportedException();
        public Window CreateBatchResizeWindow(BatchResizeWindowConfig config) => throw new NotSupportedException();
        public Window CreateFileAssociationsWindow() => throw new NotSupportedException();
        public Window CreateConvertWindow() => throw new NotSupportedException();
        public Window CreateEffectsWindow(EffectsWindowConfig config) => throw new NotSupportedException();
        public Window CreateImageInfoWindow(MainWindowViewModel vm) => throw new NotSupportedException();
        public Window CreateKeybindingsWindow(KeybindingWindowConfig config) => throw new NotSupportedException();
        public Window CreateSettingsWindow(SettingsWindowConfig config) => throw new NotSupportedException();
        public Window CreateSingleImageResizeWindow(MainWindowViewModel vm) => throw new NotSupportedException();
        public Window CreatePrintPreviewWindow(PrintWindowConfig config) => throw new NotSupportedException();

        public ValueTask InitializePrintAsync(MainWindowViewModel vm, string path, Window printPreviewWindow) =>
            throw new NotSupportedException();

        public Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath) => throw new NotSupportedException();
    }
}
