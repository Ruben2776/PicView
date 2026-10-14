using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using PicView.Avalonia;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Tests.WindowBehavior;

[Collection("Sequential")]
public class MainWindowTests
{
    static MainWindowTests()
    {
        if (Application.Current == null)
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
        }
    }

    public MainWindowTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [Theory]
    [InlineData(WindowState.Maximized, nameof(TestMainWindow.Maximize))]
    [InlineData(WindowState.FullScreen, nameof(TestMainWindow.Fullscreen))]
    public async Task WindowStateChanged_WhenSettingsDoNotMatch_AppliesState(WindowState state, string expectedCall)
    {
        var window = new TestMainWindow();

        await window.ChangeWindowStateAsync(state);

        Assert.Equal([expectedCall], window.Calls);
    }

    [Fact]
    public async Task WindowStateChanged_ToMaximizedWhenAlreadyMaximized_DoesNothing()
    {
        Settings.WindowProperties.Maximized = true;
        var window = new TestMainWindow();

        await window.ChangeWindowStateAsync(WindowState.Maximized);

        Assert.Empty(window.Calls);
    }

    [Fact]
    public async Task WindowStateChanged_ToFullScreenWhenAlreadyFullscreen_DoesNothing()
    {
        Settings.WindowProperties.Fullscreen = true;
        var window = new TestMainWindow();

        await window.ChangeWindowStateAsync(WindowState.FullScreen);

        Assert.Empty(window.Calls);
    }

    [Theory]
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

    [Fact]
    public async Task WindowStateChanged_ToNormalWhenAlreadyNormal_DoesNothing()
    {
        var window = new TestMainWindow();

        await window.ChangeWindowStateAsync(WindowState.Normal);

        Assert.Empty(window.Calls);
    }

    [Fact]
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

    [Fact]
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

        protected override IWindowProvider CreateWindowProvider() => throw new NotSupportedException();

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
}
