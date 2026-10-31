using Avalonia.Headless;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Views.Menu;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Tests.WindowBehavior;

[Collection("Sequential")]
public class MainWindowResizeTests
{
    private readonly CoreViewModel _core;

    public MainWindowResizeTests()
    {
        SetDefaults();
        TranslationManager.Init();
        Settings.WindowProperties.AutoFit = false;
        _core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
    }

    [AvaloniaFact]
    public void Resize_AnotherMainWindowNarrow_LeavesThisWindowsButtonsAlone()
    {
        var (window, titleBar, bottomBar) = CreateShownWindow();
        var (other, otherTitleBar, otherBottomBar) = CreateShownWindow();
        // A window skips the first size change it hears; this one is too wide to change any buttons
        other.Width = 700;
        Dispatcher.UIThread.RunJobs();

        other.Width = 300;
        Dispatcher.UIThread.RunJobs();

        Assert.False(otherTitleBar.SharedDropDownMenuButton!.IsVisible, "The narrowed window hides its own buttons");
        Assert.False(otherBottomBar.ResetZoomButton.IsVisible);
        Assert.True(titleBar.SharedDropDownMenuButton!.IsVisible);
        Assert.True(bottomBar.ResetZoomButton.IsVisible);
        other.Close();
        window.Close();
    }

    private (MainWindow Window, MainTitleBar TitleBar, BottomBar BottomBar) CreateShownWindow()
    {
        var window = new TestMainWindow(new NativeStateAdapter());
        window.DataContext = new MainWindowViewModel(_core, _core.Translation, window, _core.GlobalSettings!,
            _core.GallerySettings);
        var titleBar = new CustomTitleBar();
        var bottomBar = new BottomBar();
        window.SharedTitleBar = titleBar;
        window.SharedBottomBar = bottomBar;
        window.Width = 800;
        window.Height = 600;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        // A window starts hearing size changes on its first frame, and headless frames otherwise come on a timer
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        return (window, titleBar, bottomBar);
    }

    private sealed class NativeStateAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state) =>
            WindowModeSwitcher.SetNativeState(window, state.Mode);
    }
}
