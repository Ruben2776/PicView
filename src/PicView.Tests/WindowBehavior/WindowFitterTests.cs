using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Tests.WindowBehavior;

[Collection("Sequential")]
public class WindowFitterTests
{
    private static readonly Size ContentSize = new(800, 600);

    private readonly CoreViewModel _core;

    public WindowFitterTests()
    {
        SetDefaults();
        TranslationManager.Init();
        Settings.WindowProperties.AutoFit = false;
        Settings.ImageScaling.ZoomToFit = false;
        _core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
    }

    [AvaloniaFact]
    public void Refit_ImageSmallerThanTheWindow_IsShownAtItsPixelSize()
    {
        var (fitter, vm, window) = CreateWindow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));

        fitter.Refit();

        Assert.Equal(400, vm.ImageWidth.Value);
        Assert.Equal(300, vm.ImageHeight.Value);
        Assert.Equal(SizeToContent.Manual, window.SizeToContent);
    }

    [AvaloniaFact]
    public void Refit_ImageLargerThanTheWindow_FitsInsideIt()
    {
        var (fitter, vm, _) = CreateWindow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 1600, 600));

        fitter.Refit();

        Assert.Equal(800, vm.ImageWidth.Value);
        Assert.Equal(300, vm.ImageHeight.Value);
    }

    [AvaloniaFact]
    public void Refit_PairWhoseSecondSizeIsNotKnownYet_WaitsInsteadOfFittingOneImage()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        var (fitter, vm, _) = CreateWindow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300), Model("b.jpg", 0, 0));

        fitter.Refit();

        Assert.True(double.IsNaN(vm.ImageWidth.Value));
    }

    [AvaloniaFact]
    public void FitBeforeShowing_KnownSizes_FitsAndSaysSo()
    {
        var (fitter, vm, _) = CreateWindow();

        Assert.True(fitter.FitBeforeShowing(new PixelSizes(400, 300)));
        Assert.Equal(400, vm.ImageWidth.Value);
    }

    [AvaloniaFact]
    public void FitBeforeShowing_NoSize_LeavesTheWindowAlone()
    {
        var (fitter, vm, _) = CreateWindow();

        Assert.False(fitter.FitBeforeShowing(new PixelSizes(0, 0)));
        Assert.True(double.IsNaN(vm.ImageWidth.Value));
    }

    [AvaloniaTheory]
    [InlineData(true, SizeToContent.WidthAndHeight)]
    [InlineData(false, SizeToContent.Manual)]
    public void FollowContent_OnlyWithAutoFit(bool autoFit, SizeToContent expected)
    {
        Settings.WindowProperties.AutoFit = autoFit;
        var (fitter, _, window) = CreateWindow();

        fitter.FollowContent();

        Assert.Equal(expected, window.SizeToContent);
    }

    [AvaloniaFact]
    public void KeepSize_StopsFollowingContentAndLiftsTheLimit()
    {
        var (fitter, vm, window) = CreateWindow();
        window.SizeToContent = SizeToContent.WidthAndHeight;
        vm.WindowMaxWidth.Value = 500;

        fitter.KeepSize();

        Assert.Equal(SizeToContent.Manual, window.SizeToContent);
        Assert.True(double.IsNaN(vm.WindowMaxWidth.Value));
    }

    [AvaloniaFact]
    public void Refit_AutoFitWindowsOnDifferentScreens_EachFitsItsOwnWorkArea()
    {
        Settings.WindowProperties.AutoFit = true;
        var (smallFitter, small, _) = CreateWindow(() => Screen(1280, 760));
        var (largeFitter, large, _) = CreateWindow(() => Screen(2560, 1400));
        small.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 4000, 3000));
        large.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 4000, 3000));

        smallFitter.Refit();
        largeFitter.Refit();
        var smallAfterTheOther = small.ImageWidth.Value;
        smallFitter.Refit();

        Assert.True(small.ImageWidth.Value < 1280);
        Assert.True(large.ImageWidth.Value < 2560);
        Assert.True(small.ImageWidth.Value < large.ImageWidth.Value);
        Assert.Equal(smallAfterTheOther, small.ImageWidth.Value);
    }

    [AvaloniaFact]
    public void FollowTabs_NewBlankTabInAnAutoFitWindow_FillsTheWindowLessTheTabStrip()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.FollowTabs();

        var tab = vm.WindowTabs.CreateTab();
        var menu = new StartUpMenu();
        tab.CurrentView.Value = menu;
        Dispatcher.UIThread.RunJobs();

        var reserved = ReservedSpace.For(2, vm.TitlebarHeight.Value, vm.IsTopToolbarShown.Value,
            Settings.UIProperties.ShowBottomNavBar, 0, 0);
        Assert.Equal(800, menu.Width);
        Assert.Equal(600 - reserved.Top - reserved.Bottom, menu.Height);
        Assert.True(reserved.Top >= SizeDefaults.TabHeight, "The tab strip shows with two tabs");
        window.Close();
    }

    [AvaloniaFact]
    public void FollowTabs_NewBlankTabAfterAnImage_FillsTheImagesAreaLessTheTabStrip()
    {
        // Not from the window's size as it is when fitted: it follows its content, so it may already have shrunk
        // to the unsized menu by then
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.FollowTabs();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        fitter.Refit();

        var tab = vm.WindowTabs.CreateTab();
        var menu = new StartUpMenu();
        tab.CurrentView.Value = menu;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(400, menu.Width);
        Assert.Equal(300 - SizeDefaults.TabHeight, menu.Height);
        window.Close();
    }

    [AvaloniaFact]
    public void FollowTabs_BlankTabWithoutAutoFit_StretchesToTheWindow()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.FollowTabs();

        var tab = vm.WindowTabs.CreateTab();
        var menu = new StartUpMenu { Width = 100, Height = 100 };
        tab.CurrentView.Value = menu;
        Dispatcher.UIThread.RunJobs();

        Assert.True(double.IsNaN(menu.Width));
        Assert.True(double.IsNaN(menu.Height));
        window.Close();
    }

    [AvaloniaFact]
    public void FollowTabs_SwitchingToATabWithAnImage_FitsItsImage()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.FollowTabs();
        var first = vm.WindowTabs.ActiveTab.Value;
        var second = vm.WindowTabs.CreateTab();
        second.Display(Model("b.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();

        vm.WindowTabs.SelectTab(first);
        vm.WindowTabs.SelectTab(second);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(400, vm.ImageWidth.Value);
        Assert.Equal(300, vm.ImageHeight.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void FollowTabs_TabStripHides_FitsTheImageAgain()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.FollowTabs();
        var first = vm.WindowTabs.ActiveTab.Value;
        first.Display(Model("a.jpg", 400, 300));
        var second = vm.WindowTabs.CreateTab();
        vm.WindowTabs.SelectTab(first);
        Dispatcher.UIThread.RunJobs();
        vm.ImageWidth.Value = double.NaN;

        second.CloseTab();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(400, vm.ImageWidth.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void IsOnAnotherScreen_AfterTheWindowMovesToAnotherMonitor_IsTrueUntilItIsFittedThere()
    {
        var screen = Screen(1920, 1040);
        var (fitter, vm, _) = CreateWindow(() => screen);
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        var beforeFitting = fitter.IsOnAnotherScreen();
        fitter.Refit();
        var afterFitting = fitter.IsOnAnotherScreen();

        screen = Screen(2560, 1400) with { X = 1920, WorkingAreaX = 1920 };
        var afterMoving = fitter.IsOnAnotherScreen();
        fitter.Refit();

        Assert.False(beforeFitting);
        Assert.False(afterFitting);
        Assert.True(afterMoving);
        Assert.False(fitter.IsOnAnotherScreen());
    }

    private (WindowFitter Fitter, MainWindowViewModel Vm, MainWindow Window) CreateWindow(
        Func<ScreenSize>? readScreen = null)
    {
        var window = new TestMainWindow(new NativeStateAdapter());
        var vm = new MainWindowViewModel(_core, _core.Translation, window, _core.GlobalSettings!,
            _core.GallerySettings);
        window.DataContext = vm;
        return (new WindowFitter(window, () => ContentSize, readScreen), vm, window);
    }

    private (WindowFitter Fitter, MainWindowViewModel Vm, MainWindow Window) CreateShownWindow(double width,
        double height)
    {
        var (fitter, vm, window) = CreateWindow();
        window.Width = width;
        window.Height = height;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (fitter, vm, window);
    }

    private static ScreenSize Screen(double workAreaWidth, double workAreaHeight) => new()
    {
        Width = workAreaWidth, Height = workAreaHeight + 40, WorkingAreaWidth = workAreaWidth,
        WorkingAreaHeight = workAreaHeight, Scaling = 1
    };

    private static ImageModel Model(string name, uint width, uint height) =>
        new() { FileInfo = new FileInfo(name), PixelWidth = width, PixelHeight = height };

    private sealed class NativeStateAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state) =>
            WindowModeSwitcher.SetNativeState(window, state.Mode);
    }
}
