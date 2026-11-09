using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.WindowBehavior;

[Collection("Sequential")]
public class WindowFitterTests
{
    private static readonly Size ContentSize = new(800, 600);

    private readonly CoreViewModel _core;

    public WindowFitterTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.Init();
        Settings.WindowProperties.AutoFit = false;
        Settings.ImageScaling.ZoomToFit = false;
        _core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()),
            new GlobalSettingsViewModel(static () => ValueTask.FromResult(true)));
    }

    [AvaloniaFact]
    public void Follow_ImageSmallerThanTheWindow_IsShownAtItsPixelSize()
    {
        var (fitter, vm, window) = CreateWindow();
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));

        Dispatcher.UIThread.RunJobs();

        Assert.Equal(400, vm.ImageWidth.Value);
        Assert.Equal(300, vm.ImageHeight.Value);
        Assert.Equal(SizeToContent.Manual, window.SizeToContent);
    }

    [AvaloniaFact]
    public void Follow_ImageLargerThanTheWindow_FitsInsideIt()
    {
        var (fitter, vm, _) = CreateWindow();
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 1600, 600));

        Dispatcher.UIThread.RunJobs();

        Assert.Equal(800, vm.ImageWidth.Value);
        Assert.Equal(300, vm.ImageHeight.Value);
    }

    [AvaloniaFact]
    public void Follow_AutoFitOff_LevelIsTheFitScaleInPercent()
    {
        var (fitter, vm, _) = CreateWindow();
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 1600, 600));

        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0.5, tab.Zoom.FitScale);
        Assert.Equal(50, tab.Zoom.Level.CurrentValue);
    }

    [AvaloniaFact]
    public void Follow_AutoFitOn_LevelFollowsTheFitAgainstTheScreen()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, _) = CreateWindow(() => Screen(1280, 760));
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 1600, 2000));

        Dispatcher.UIThread.RunJobs();

        Assert.InRange(tab.Zoom.Level.CurrentValue, 1, 99);
        Assert.Equal(Convert.ToInt32(tab.Zoom.FitScale * 100), tab.Zoom.Level.CurrentValue);
        Assert.Equal(1600 * tab.Zoom.FitScale, vm.ImageWidth.Value, precision: 6);
    }

    [AvaloniaFact]
    public void Follow_ZoomToFitTurnsOn_LevelFollowsTheNewFit()
    {
        var (fitter, vm, _) = CreateWindow();
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(100, tab.Zoom.Level.CurrentValue);

        vm.GlobalSettings.IsZoomedToFit.Value = true;
        Dispatcher.UIThread.RunJobs();

        // Upscaled to fill the 800×600 content less the reserved space, so the exact percent follows the fit
        Assert.True(tab.Zoom.Level.CurrentValue > 100);
        Assert.Equal(Convert.ToInt32(tab.Zoom.FitScale * 100), tab.Zoom.Level.CurrentValue);
        Assert.Equal(400 * tab.Zoom.FitScale, vm.ImageWidth.Value, precision: 6);
    }

    [AvaloniaFact]
    public void Follow_ZoomToFitTurnsOn_FitsTheShownWindow()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(100, tab.Zoom.Level.CurrentValue);

        vm.GlobalSettings.IsZoomedToFit.Value = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(tab.Zoom.Level.CurrentValue > 100);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_ScrollingTurnsOn_FitsToTheWidth()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 1600, 2400));
        Dispatcher.UIThread.RunJobs();
        var widthBefore = vm.ImageWidth.Value;
        Assert.True(double.IsNaN(vm.ScrollViewerHeight.Value));

        vm.GlobalSettings.IsScrollingEnabled.Value = true;
        Dispatcher.UIThread.RunJobs();

        // Scrolling fits the width and scrolls the rest of the height
        Assert.True(vm.ImageWidth.Value > widthBefore);
        Assert.False(double.IsNaN(vm.ScrollViewerHeight.Value));
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_SideBySideTurnsOff_FitsTheOneImage()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        vm.GlobalSettings.IsSideBySide.Value = true;
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 400, 300), Model("b.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        var pairWidth = vm.ImageWidth.Value;

        vm.GlobalSettings.IsSideBySide.Value = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(pairWidth / 2, vm.ImageWidth.Value, precision: 6);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_PairWhoseSecondSizeIsNotKnownYet_WaitsInsteadOfFittingOneImage()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        var (fitter, vm, _) = CreateWindow();
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300), Model("b.jpg", 0, 0));

        Dispatcher.UIThread.RunJobs();

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
    public void Follow_AutoFitWindowsOnDifferentScreens_EachFitsItsOwnWorkArea()
    {
        Settings.WindowProperties.AutoFit = true;
        var (smallFitter, small, _) = CreateWindow(() => Screen(1280, 760));
        var (largeFitter, large, _) = CreateWindow(() => Screen(2560, 1400));
        using var followingSmall = smallFitter.Follow();
        using var followingLarge = largeFitter.Follow();
        var smallTab = small.WindowTabs.ActiveTab.Value;
        smallTab.Display(Model("a.jpg", 4000, 3000));
        large.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 4000, 3000));

        Dispatcher.UIThread.RunJobs();
        var smallAfterTheOther = small.ImageWidth.Value;
        // Turned half way round it has the same shape, so it only fits again
        smallTab.Orientation.TurnTo(2);
        Dispatcher.UIThread.RunJobs();

        Assert.True(small.ImageWidth.Value < 1280);
        Assert.True(large.ImageWidth.Value < 2560);
        Assert.True(small.ImageWidth.Value < large.ImageWidth.Value);
        Assert.Equal(smallAfterTheOther, small.ImageWidth.Value);
    }

    [AvaloniaFact]
    public void Follow_NewBlankTabInAnAutoFitWindow_FillsTheWindowLessTheTabStrip()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();

        var tab = vm.WindowTabs.CreateTab();
        var menu = (StartUpMenu)tab.CurrentView.Value!;
        // A sentinel size, so only the re-fit as the tab becomes active can give the sizes below
        menu.Width = 100;
        menu.Height = 100;
        Dispatcher.UIThread.RunJobs();

        var reserved = ReservedSpace.For(2, vm.Interface.Value, 0, 0);
        Assert.Equal(800, menu.Width);
        Assert.Equal(600 - reserved.Top - reserved.Bottom, menu.Height);
        Assert.True(reserved.Top >= SizeDefaults.TabHeight, "The tab strip shows with two tabs");
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_NewBlankTabAfterAnImage_FillsTheImagesAreaLessTheTabStrip()
    {
        // Not from the window's size as it is when fitted: it follows its content, so it may already have shrunk
        // to the unsized menu by then
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();

        var tab = vm.WindowTabs.CreateTab();
        var menu = (StartUpMenu)tab.CurrentView.Value!;
        // A sentinel size, so only the re-fit as the tab becomes active can give the sizes below
        menu.Width = 100;
        menu.Height = 100;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(400, menu.Width);
        Assert.Equal(300 - SizeDefaults.TabHeight, menu.Height);
        window.Close();
    }

    [AvaloniaFact]
    public void FitStartUpMenu_AutoFitWindowNotShownYet_OpensAtTheStartUpMenusSize()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, _) = CreateWindow(() => Screen(1920, 1040));
        var menu = new StartUpMenu();

        fitter.FitStartUpMenu(menu);

        var reserved = ReservedSpace.For(1, vm.Interface.Value, 0, 0);
        Assert.Equal(1000, menu.Width);
        Assert.Equal(470 - reserved.Top - reserved.Bottom, menu.Height);
    }

    [AvaloniaFact]
    public void FitStartUpMenu_AutoFitWindowAfterAnImage_KeepsTheImagesSize()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, _) = CreateWindow(() => Screen(1920, 1040));
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        var menu = new StartUpMenu();

        fitter.FitStartUpMenu(menu);

        Assert.Equal(400, menu.Width);
    }

    [AvaloniaFact]
    public void Follow_BlankTabWithoutAutoFit_StretchesToTheWindow()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();

        var tab = vm.WindowTabs.CreateTab();
        var menu = (StartUpMenu)tab.CurrentView.Value!;
        // A sentinel size, so only the re-fit as the tab becomes active can give the sizes below
        menu.Width = 100;
        menu.Height = 100;
        Dispatcher.UIThread.RunJobs();

        Assert.True(double.IsNaN(menu.Width));
        Assert.True(double.IsNaN(menu.Height));
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_SwitchingToATabWithAnImage_FitsItsImage()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
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
    public void Follow_TabStripHides_FitsTheImageAgain()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
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
    public void Follow_ScreenMarginChanges_FitsWithTheNewMargin()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, _) = CreateWindow(() => Screen(1280, 3000));
        using var following = fitter.Follow();
        vm.GlobalSettings.WindowMargin.Value = 0;
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 4000, 3000));
        Dispatcher.UIThread.RunJobs();
        var widthBefore = vm.ImageWidth.Value;
        vm.ImageWidth.Value = double.NaN;

        vm.GlobalSettings.WindowMargin.Value = 200;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(widthBefore - 200, vm.ImageWidth.Value, precision: 6);
    }

    [AvaloniaFact]
    public void Follow_BottomBarHides_FitsTheImageToTheRoomItLeaves()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600, readScreen: () => Screen(1920, 1040));
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 3000, 4000));
        vm.GlobalSettings.ShowBottomBar.Value = true;
        Dispatcher.UIThread.RunJobs();
        var withBar = vm.ImageHeight.Value;

        vm.GlobalSettings.ShowBottomBar.Value = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(withBar + SizeDefaults.BottombarHeight, vm.ImageHeight.Value, 6);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_InterfaceHides_FitsTheImageToTheRoomItLeaves()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600, readScreen: () => Screen(1920, 1040));
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 3000, 4000));
        Dispatcher.UIThread.RunJobs();
        var withBars = vm.ImageHeight.Value;

        vm.GlobalSettings.ShowInterface.Value = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(withBars + SizeDefaults.MainTitlebarHeight + 2 + SizeDefaults.BottombarHeight, vm.ImageHeight.Value, 6);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_AutoFitTurnedOff_FitsTheImageInsideTheWindow()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 1600, 600));
        Dispatcher.UIThread.RunJobs();
        var withAutoFit = vm.ImageWidth.Value;

        vm.WindowModeState.Value = vm.WindowModeState.Value.SetAutoFit(false);
        Dispatcher.UIThread.RunJobs();

        Assert.True(withAutoFit > ContentSize.Width, "AutoFit fits the image to the work area");
        Assert.Equal(ContentSize.Width, vm.ImageWidth.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_WindowMinimized_FitsNothing()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        // Shown before following, so only the minimizing could fit it
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        using var following = fitter.Follow();

        vm.WindowModeState.Value = vm.WindowModeState.Value.OnNativeState(WindowMode.Minimized);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(WindowMode.Minimized, vm.WindowModeState.Value.Mode);
        Assert.True(double.IsNaN(vm.ImageWidth.Value));
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_ActiveTabTurned_FitsItsNewShape()
    {
        // 1600 by 400 fits the 800 by 600 window at half size; turned, its 1600 high side fits 600
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 1600, 400));
        Dispatcher.UIThread.RunJobs();
        var upright = vm.ImageWidth.Value;

        tab.Orientation.TurnRight();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(800, upright);
        Assert.Equal(600, vm.ImageWidth.Value, 6);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_TabInTheBackgroundTurned_FitsNothing()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var background = vm.WindowTabs.ActiveTab.Value;
        background.Display(Model("a.jpg", 1600, 400));
        var shown = vm.WindowTabs.CreateTab();
        Dispatcher.UIThread.RunJobs();
        vm.ImageWidth.Value = double.NaN;

        background.Orientation.TurnRight();
        Dispatcher.UIThread.RunJobs();

        Assert.Same(shown, vm.WindowTabs.ActiveTab.Value);
        Assert.True(double.IsNaN(vm.ImageWidth.Value));
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_InputChanges_PublishesTheFitWithCauseInput()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        fits.Clear();

        vm.GlobalSettings.IsZoomedToFit.Value = true;
        Dispatcher.UIThread.RunJobs();

        // A window that is not AutoFit is fitted once more after layout, so one change can publish twice
        Assert.NotEmpty(fits);
        var fit = fits[^1];
        Assert.All(fits, published => Assert.Equal(FitCause.Input, published.Cause));
        Assert.Equal(vm.ImageWidth.Value, fit.Fit.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_NewImageShown_FitsOnceAtSendWithCauseNewImage()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Send);

        var fit = Assert.Single(fits);
        Assert.Equal(FitCause.NewImage, fit.Cause);
        Assert.Equal(400, vm.ImageWidth.Value);
        fitter.Dispose();
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_ShownFileRenamed_FitsNothing()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        var model = Model("a.jpg", 400, 300);
        tab.Display(model);
        Dispatcher.UIThread.RunJobs();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        tab.UpdateShownFile(model.FileInfo!.FullName, new FileInfo("b.jpg"));
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(fits);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_NewImageAndTurnTogether_FitsOncePerPriority()
    {
        // AutoFit, so the turn is not fitted once more after layout
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);
        var tab = vm.WindowTabs.ActiveTab.Value;

        tab.Display(Model("a.jpg", 1600, 400));
        tab.Orientation.TurnRight();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([FitCause.NewImage, FitCause.Input], fits.Select(static fitted => fitted.Cause));
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_SwitchToATabStillLoading_FitsWhenItsImageShows()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);
        var loading = vm.WindowTabs.CreateTab();
        loading.SetLoading();
        Dispatcher.UIThread.RunJobs();
        var fitsWhileLoading = fits.Count;

        loading.Display(Model("b.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, fitsWhileLoading);
        var fit = Assert.Single(fits);
        Assert.Equal(FitCause.NewImage, fit.Cause);
        Assert.Equal(400, vm.ImageWidth.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_TabClosesBeforeItsFit_PublishesNoNewImage()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var first = vm.WindowTabs.ActiveTab.Value;
        first.Display(Model("a.jpg", 400, 300));
        // Fitted while it is active, so its image is not new when it is shown again
        Dispatcher.UIThread.RunJobs();
        var second = vm.WindowTabs.CreateTab();
        Dispatcher.UIThread.RunJobs();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        // Closing it disposes it and shows the first tab, whose image is not new, so only that switch is fitted
        second.Display(Model("b.jpg", 200, 100));
        vm.WindowTabs.CloseTab(second);
        Dispatcher.UIThread.RunJobs();

        Assert.NotEmpty(fits);
        Assert.All(fits, fitted => Assert.Equal(FitCause.Input, fitted.Cause));
        Assert.Equal(400, vm.ImageWidth.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_NewImageWhileMinimized_PublishesItAsNewImageOnRestore()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);
        vm.WindowModeState.Value = vm.WindowModeState.Value.OnNativeState(WindowMode.Minimized);
        Dispatcher.UIThread.RunJobs();

        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        var fitsWhileMinimized = fits.Count;
        vm.WindowModeState.Value = vm.WindowModeState.Value.OnNativeState(WindowMode.Normal);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, fitsWhileMinimized);
        // The restore's Fit is the first that applies, so it carries the new image; the one after layout does not
        Assert.Equal(FitCause.NewImage, fits[0].Cause);
        Assert.Single(fits, static fitted => fitted.Cause is FitCause.NewImage);
        Assert.Equal(400, vm.ImageWidth.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_NewImageInABackgroundTab_IsPublishedAsNewImageOnTheSwitchToIt()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var background = vm.WindowTabs.ActiveTab.Value;
        background.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        var shown = vm.WindowTabs.CreateTab();
        shown.Display(Model("b.jpg", 200, 100));
        Dispatcher.UIThread.RunJobs();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        // As the folder watcher replaces its shown file while another tab is shown
        background.Display(Model("c.jpg", 300, 200));
        Dispatcher.UIThread.RunJobs();
        var fitsInTheBackground = fits.Count;
        vm.WindowTabs.SelectTab(background);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, fitsInTheBackground);
        Assert.Equal(FitCause.NewImage, fits[0].Cause);
        Assert.Single(fits, static fitted => fitted.Cause is FitCause.NewImage);
        Assert.Equal(300, vm.ImageWidth.Value);
        Assert.False(fitter.IsNewImageFitOwed(background));
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_TabSwitchedAwayBeforeItsNewImageIsFitted_IsPublishedAsNewImageOnTheSwitchBack()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var first = vm.WindowTabs.ActiveTab.Value;
        first.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        var second = vm.WindowTabs.CreateTab();
        second.Display(Model("b.jpg", 200, 100));
        Dispatcher.UIThread.RunJobs();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        // Next pressed, then another tab chosen before the new image's Fit ran
        second.Display(Model("c.jpg", 300, 200));
        vm.WindowTabs.SelectTab(first);
        Dispatcher.UIThread.RunJobs();
        var causesOnTheFirstTab = fits.Select(static fitted => fitted.Cause).ToList();
        fits.Clear();
        vm.WindowTabs.SelectTab(second);
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotContain(FitCause.NewImage, causesOnTheFirstTab);
        Assert.Equal(FitCause.NewImage, fits[0].Cause);
        Assert.Single(fits, static fitted => fitted.Cause is FitCause.NewImage);
        Assert.Equal(300, vm.ImageWidth.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_BackgroundTabWithANewImageCloses_FitsNothingAndForgetsIt()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var background = vm.WindowTabs.ActiveTab.Value;
        background.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        var shown = vm.WindowTabs.CreateTab();
        shown.Display(Model("b.jpg", 200, 100));
        Dispatcher.UIThread.RunJobs();
        background.Display(Model("c.jpg", 300, 200));
        Dispatcher.UIThread.RunJobs();
        Assert.True(fitter.IsNewImageFitOwed(background));
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        vm.WindowTabs.CloseTab(background);
        var thrown = Record.Exception(() => Dispatcher.UIThread.RunJobs());

        Assert.Null(thrown);
        Assert.False(fitter.IsNewImageFitOwed(background));
        Assert.DoesNotContain(fits, static fitted => fitted.Cause is FitCause.NewImage);
        Assert.Equal(200, vm.ImageWidth.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_BackgroundTabReordered_KeepsItsNewImageFitOwed()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var background = vm.WindowTabs.ActiveTab.Value;
        background.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        var shown = vm.WindowTabs.CreateTab();
        shown.Display(Model("b.jpg", 200, 100));
        Dispatcher.UIThread.RunJobs();
        background.Display(Model("c.jpg", 300, 200));
        Dispatcher.UIThread.RunJobs();

        // Reordering takes the tab out of the collection and puts it back
        vm.WindowTabs.Reorder(background, 1);
        Dispatcher.UIThread.RunJobs();

        Assert.True(fitter.IsNewImageFitOwed(background));
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_FitterDisposedBeforeTheNewImagesFit_DoesNotThrow()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));

        fitter.Dispose();
        var thrown = Record.Exception(() => Dispatcher.UIThread.RunJobs());

        Assert.Null(thrown);
        // A disposed fitter applies no Fit that was posted before
        Assert.True(double.IsNaN(vm.ImageWidth.Value));
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_GalleryDocks_ReservesItsExtent()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Gallery.Initialize(_core.GallerySettings);
        tab.Display(Model("a.jpg", 1600, 1200));
        Dispatcher.UIThread.RunJobs();
        var heightBefore = vm.ImageHeight.Value;
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        tab.Gallery.Switcher.Start(docked: true);
        Dispatcher.UIThread.RunJobs();

        var extent = tab.Gallery.Appearance.Value.Extent;
        Assert.True(extent > 0);
        Assert.Contains(fits, static fitted => fitted.Cause is FitCause.Input);
        Assert.Equal(heightBefore - extent, vm.ImageHeight.Value, precision: 6);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_GalleryHides_ReservesNothing()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Gallery.Initialize(_core.GallerySettings);
        tab.Display(Model("a.jpg", 1600, 1200));
        Dispatcher.UIThread.RunJobs();
        var heightBefore = vm.ImageHeight.Value;
        tab.Gallery.Switcher.Start(docked: true);
        Dispatcher.UIThread.RunJobs();
        Assert.True(vm.ImageHeight.Value < heightBefore);

        tab.Gallery.Switcher.Start(docked: false);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(heightBefore, vm.ImageHeight.Value, precision: 6);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_DockedGalleryItemSizeChanges_FitsAgain()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Gallery.Initialize(_core.GallerySettings);
        tab.Display(Model("a.jpg", 1600, 1200));
        tab.Gallery.Switcher.Start(docked: true);
        Dispatcher.UIThread.RunJobs();
        var heightBefore = vm.ImageHeight.Value;
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        _core.GallerySettings.DockedGalleryItemSize.Value += 40;
        Dispatcher.UIThread.RunJobs();

        Assert.NotEmpty(fits);
        Assert.True(vm.ImageHeight.Value < heightBefore);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_DockedGalleryExpandsAndCollapses_KeepsTheDockedReserve()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600, readScreen: () => Screen(1280, 760));
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Gallery.Initialize(_core.GallerySettings);
        tab.Display(Model("a.jpg", 1600, 1200));
        tab.Gallery.Switcher.Start(docked: true);
        Dispatcher.UIThread.RunJobs();
        var docked = (vm.ImageWidth.Value, vm.ImageHeight.Value, vm.ScrollViewerHeight.Value);

        tab.Gallery.Switcher.ToggleExpanded();
        Dispatcher.UIThread.RunJobs();
        var expanded = (vm.ImageWidth.Value, vm.ImageHeight.Value, vm.ScrollViewerHeight.Value);
        tab.Gallery.Switcher.ToggleExpanded();
        Dispatcher.UIThread.RunJobs();
        var collapsed = (vm.ImageWidth.Value, vm.ImageHeight.Value, vm.ScrollViewerHeight.Value);

        Assert.Equal(GalleryMode.Docked, tab.Gallery.ShownMode.Value);
        Assert.Equal(docked, expanded);
        Assert.Equal(docked, collapsed);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_ExpandedGalleryStopsReturningToDocked_GivesBackTheReserve()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Gallery.Initialize(_core.GallerySettings);
        tab.Display(Model("a.jpg", 1600, 1200));
        Dispatcher.UIThread.RunJobs();
        var heightWithoutGallery = vm.ImageHeight.Value;
        tab.Gallery.Switcher.Start(docked: true);
        tab.Gallery.Switcher.ToggleExpanded();
        Dispatcher.UIThread.RunJobs();
        Assert.True(vm.ImageHeight.Value < heightWithoutGallery);

        // Expanded still, but it now gives back Closed
        _ = tab.Gallery.Switcher.ToggleDocked();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(GalleryMode.Expanded, tab.Gallery.ShownMode.Value);
        Assert.Equal(heightWithoutGallery, vm.ImageHeight.Value, precision: 6);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_GalleryInABackgroundTab_FitsNothing()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var background = vm.WindowTabs.ActiveTab.Value;
        background.Gallery.Initialize(_core.GallerySettings);
        background.Display(Model("a.jpg", 1600, 1200));
        var shown = vm.WindowTabs.CreateTab();
        Dispatcher.UIThread.RunJobs();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        background.Gallery.Switcher.Start(docked: true);
        Dispatcher.UIThread.RunJobs();

        Assert.Same(shown, vm.WindowTabs.ActiveTab.Value);
        Assert.Empty(fits);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_CropOpens_FitsWithoutTheGallery()
    {
        // Guards the gallery hiding; Follow_CropOpensBeforeAnyFit_FitsBeforeTheCropLaysOut proves the Tab View input
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Gallery.Initialize(_core.GallerySettings);
        tab.Display(Model("a.jpg", 1600, 1200));
        Dispatcher.UIThread.RunJobs();
        var heightWithoutGallery = vm.ImageHeight.Value;
        tab.Gallery.Switcher.Start(docked: true);
        Dispatcher.UIThread.RunJobs();
        Assert.True(vm.ImageHeight.Value < heightWithoutGallery);
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        tab.Crop = new CropViewModel(tab.Zoom);
        Dispatcher.UIThread.RunJobs();

        Assert.NotEmpty(fits);
        Assert.Equal(heightWithoutGallery, vm.ImageHeight.Value, precision: 6);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_CropCloses_FitsWithTheGalleryBack()
    {
        // Guards the gallery hiding; Follow_CropOpensBeforeAnyFit_FitsBeforeTheCropLaysOut proves the Tab View input
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Gallery.Initialize(_core.GallerySettings);
        tab.Display(Model("a.jpg", 1600, 1200));
        tab.Gallery.Switcher.Start(docked: true);
        Dispatcher.UIThread.RunJobs();
        var heightWithGallery = vm.ImageHeight.Value;
        tab.Crop = new CropViewModel(tab.Zoom);
        Dispatcher.UIThread.RunJobs();
        Assert.True(vm.ImageHeight.Value > heightWithGallery);
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        tab.Crop = null;
        Dispatcher.UIThread.RunJobs();

        Assert.NotEmpty(fits);
        Assert.Equal(heightWithGallery, vm.ImageHeight.Value, precision: 6);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_CropOpensBeforeAnyFit_FitsBeforeTheCropLaysOut()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Model = Model("a.jpg", 1600, 1200);
        Assert.True(double.IsNaN(vm.ImageWidth.Value));

        tab.Crop = new CropViewModel(tab.Zoom);
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Normal);

        Assert.False(double.IsNaN(vm.ImageWidth.Value));
        fitter.Dispose();
        window.Close();
    }

    [AvaloniaFact]
    public void Dispose_WithAFitPending_LetsItRunAfterTheTabsAreTornDownWithoutThrowing()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        // An input posts a Fit at Normal and, in a Fixed Window, one at Background after layout
        vm.GlobalSettings.IsZoomedToFit.Value = !vm.GlobalSettings.IsZoomedToFit.Value;

        // As closing the window does: the fitter ends, the tabs are released, the posted Fits still run
        fitter.Dispose();
        vm.WindowTabs.TearDown();
        var exception = Record.Exception(static () => Dispatcher.UIThread.RunJobs());

        Assert.Null(exception);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_FixedWindowContentResizes_FitsTheImageToIt()
    {
        var content = new Subject<Size>();
        var contentSize = new Size(800, 600);
        var (fitter, vm, window) = CreateShownWindow(800, 600, () => contentSize, content);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 1600, 1200));
        Dispatcher.UIThread.RunJobs();
        var heightBefore = vm.ImageHeight.Value;

        contentSize = new Size(800, 400);
        content.OnNext(contentSize);
        Dispatcher.UIThread.RunJobs();

        Assert.True(vm.ImageHeight.Value < heightBefore);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_AutoFitWindowContentResizes_FitsNothing()
    {
        var content = new Subject<Size>();
        var contentSize = new Size(800, 600);
        var (fitter, vm, window) = CreateShownWindow(800, 600, () => contentSize, content);
        vm.WindowModeState.Value = vm.WindowModeState.Value.SetAutoFit(true);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 1600, 1200));
        Dispatcher.UIThread.RunJobs();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        contentSize = new Size(800, 400);
        content.OnNext(contentSize);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(fits);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_AttachedBeforeTheMode_FullscreenStartFits()
    {
        var (fitter, vm, window) = CreateWindow(() => Screen(1280, 720));
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 4000, 300));
        Dispatcher.UIThread.RunJobs();
        // Fitted to the 800 wide content before the mode starts
        Assert.Equal(ContentSize.Width, vm.ImageWidth.Value);
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);
        Settings.WindowProperties.Fullscreen = true;
        var switcher = new WindowModeSwitcher(window, new NativeStateAdapter(), () => ValueTask.FromResult(true));

        switcher.Start(isAppStart: true);
        Dispatcher.UIThread.RunJobs();

        // The starting mode is a Fit input like any later one: the image fills the whole screen, not the work area
        Assert.True(vm.IsFullscreen.Value);
        Assert.NotEmpty(fits);
        Assert.Equal(1280, vm.ImageWidth.Value, precision: 6);
    }

    [AvaloniaFact]
    public void MoveToScreenOrigin_SecondScalingScreen_PutsTheWindowAtItsOriginInPixels()
    {
        var screen = Screen(2560, 1400) with { X = 1920, Y = 0, WorkingAreaX = 1920, Scaling = 1.5 };
        var (fitter, _, window) = CreateShownWindow(800, 600, readScreen: () => screen);
        window.Position = new PixelPoint(100, 100);

        fitter.MoveToScreenOrigin();

        Assert.Equal(2880, window.Position.X);
        Assert.Equal(0, window.Position.Y);
    }

    [AvaloniaFact]
    public void IsOnAnotherScreen_AfterTheWindowMovesToAnotherMonitor_IsTrueUntilItIsFittedThere()
    {
        var screen = Screen(1920, 1040);
        var changes = new Subject<Unit>();
        var (fitter, vm, _) = CreateWindow(() => screen, screenChanges: changes);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        var beforeFitting = fitter.IsOnAnotherScreen();
        Dispatcher.UIThread.RunJobs();
        var afterFitting = fitter.IsOnAnotherScreen();

        screen = Screen(2560, 1400) with { X = 1920, WorkingAreaX = 1920 };
        var afterMoving = fitter.IsOnAnotherScreen();
        changes.OnNext(Unit.Default);
        Dispatcher.UIThread.RunJobs();

        Assert.False(beforeFitting);
        Assert.False(afterFitting);
        Assert.True(afterMoving);
        Assert.False(fitter.IsOnAnotherScreen());
    }

    [AvaloniaFact]
    public void Follow_ScalingChanges_FitsAgainstTheNewScreen()
    {
        var screen = Screen(1920, 1040);
        var changes = new Subject<Unit>();
        var (fitter, vm, window) = CreateWindow(() => screen, screenChanges: changes);
        // AutoFit sizes the image against the screen
        vm.WindowModeState.Value = vm.WindowModeState.Value.SetAutoFit(true);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 4000, 3000));
        Dispatcher.UIThread.RunJobs();
        var widthBefore = vm.ImageWidth.Value;

        screen = Screen(1280, 700);
        changes.OnNext(Unit.Default);
        Dispatcher.UIThread.RunJobs();

        Assert.True(vm.ImageWidth.Value < widthBefore);
        Assert.False(fitter.IsOnAnotherScreen());
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_WindowMovesToAnotherScreen_FitsThere()
    {
        var screen = Screen(1920, 1040);
        var changes = new Subject<Unit>();
        var (fitter, vm, window) = CreateWindow(() => screen, screenChanges: changes);
        // AutoFit, so the one Fit before the frame is the only one
        vm.WindowModeState.Value = vm.WindowModeState.Value.SetAutoFit(true);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        screen = Screen(2560, 1400) with { X = 1920, WorkingAreaX = 1920 };
        changes.OnNext(Unit.Default);
        Dispatcher.UIThread.RunJobs();

        Assert.Single(fits);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_WindowMovesOnTheSameScreen_FitsNothing()
    {
        var changes = new Subject<Unit>();
        var (fitter, vm, window) = CreateWindow(() => Screen(1920, 1040), screenChanges: changes);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        var fits = new List<Fitted>();
        using var listening = fitter.Fits.Subscribe(fits.Add);

        changes.OnNext(Unit.Default);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(fits);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_FitGrowsTheWindowWithStayCentered_StaysCentered()
    {
        Settings.WindowProperties.KeepCentered = true;
        var screen = Screen(1920, 1040);
        var (fitter, vm, window) = CreateShownAutoFitWindow(() => screen);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        window.Position = new PixelPoint(100, 50);

        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 800, 600));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Size(800, 600), window.ClientSize);
        var centered = WindowPlacement.Centered(screen, 800, 600);
        Assert.Equal(new PixelPoint(centered.X, centered.Y), window.Position);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_FitGrowsTheWindowWithoutStayCentered_KeepsItsCenter()
    {
        var (fitter, vm, window) = CreateShownAutoFitWindow(() => Screen(1920, 1040));
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        window.Position = new PixelPoint(500, 300);
        var centerBefore = CenterOf(window);

        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 800, 600));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Size(800, 600), window.ClientSize);
        Assert.Equal(centerBefore, CenterOf(window));
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_FixedWindowResizes_IsNotMoved()
    {
        Settings.WindowProperties.KeepCentered = true;
        var (fitter, _, window) = CreateShownWindow(400, 300, readScreen: () => Screen(1920, 1040));
        using var following = fitter.Follow();
        window.Position = new PixelPoint(100, 50);

        window.Width = 800;
        window.Height = 600;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Size(800, 600), window.ClientSize);
        Assert.Equal(new PixelPoint(100, 50), window.Position);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_FullscreenFit_IsNotMoved()
    {
        Settings.WindowProperties.KeepCentered = true;
        var (fitter, vm, window) = CreateShownAutoFitWindow(() => Screen(1920, 1040));
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        vm.WindowModeState.Value = vm.WindowModeState.Value.ToggleFullscreen();
        Dispatcher.UIThread.RunJobs();
        window.Position = new PixelPoint(100, 50);

        // A fullscreen window takes the size of its screen
        window.Width = 1920;
        window.Height = 1080;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Size(1920, 1080), window.ClientSize);
        Assert.Equal(new PixelPoint(100, 50), window.Position);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_NativelyMaximizedWindowResizes_IsNotMoved()
    {
        var (fitter, vm, window) = CreateShownAutoFitWindow(() => Screen(1920, 1040));
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        window.Position = new PixelPoint(100, 50);
        var modesWhenResized = new List<WindowMode>();
        using var listening = window.GetPropertyChangedObservable(Window.ClientSizeProperty).ToObservable()
            .Subscribe(_ => modesWhenResized.Add(vm.WindowModeState.Value.Mode));

        // The operating system maximizes it, as Win+Up does, and its Window Mode follows only later
        window.WindowState = WindowState.Maximized;
        window.SizeToContent = SizeToContent.Manual;
        window.Width = 1920;
        window.Height = 1040;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([WindowMode.Normal], modesWhenResized);
        Assert.Equal(new Size(1920, 1040), window.ClientSize);
        Assert.Equal(new PixelPoint(100, 50), window.Position);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_Placed_PublishesTheNewSize()
    {
        var (fitter, vm, window) = CreateShownAutoFitWindow(() => Screen(1920, 1040));
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        window.Position = new PixelPoint(500, 300);
        var placed = new List<Size>();
        using var listening = fitter.Placed.Subscribe(placed.Add);

        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 800, 600));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([new Size(800, 600)], placed);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Follow_ReturnsToNormalWithAutoFit_IsCenteredOnItsScreen()
    {
        var screen = Screen(1920, 1040);
        var (fitter, vm, window) = CreateShownAutoFitWindow(() => screen);
        var (otherFitter, otherVm, otherWindow) = CreateShownAutoFitWindow(() => screen);
        using var following = fitter.Follow();
        using var otherFollowing = otherFitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        otherVm.WindowTabs.ActiveTab.Value.Display(Model("b.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        window.Position = new PixelPoint(100, 50);
        otherWindow.Position = new PixelPoint(300, 200);
        var switcher = new WindowModeSwitcher(window, new NativeStateAdapter(), () => ValueTask.FromResult(true));

        await switcher.ToggleFullscreen();
        Dispatcher.UIThread.RunJobs();
        await switcher.ToggleFullscreen();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(WindowMode.Normal, vm.WindowModeState.Value.Mode);
        var centered = WindowPlacement.Centered(fitter.Screen, window.ClientSize.Width, window.ClientSize.Height);
        Assert.NotEqual(new PixelPoint(100, 50), new PixelPoint(centered.X, centered.Y));
        Assert.Equal(new PixelPoint(centered.X, centered.Y), window.Position);
        Assert.Equal(new PixelPoint(300, 200), otherWindow.Position);
        window.Close();
        otherWindow.Close();
    }

    [AvaloniaFact]
    public async Task Follow_ReturnsToNormalWithoutAutoFit_KeepsItsPreviousSizeAndPosition()
    {
        Settings.WindowProperties.KeepCentered = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600, readScreen: () => Screen(1920, 1040));
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        window.Position = new PixelPoint(100, 50);
        var switcher = new WindowModeSwitcher(window, new NativeStateAdapter(), () => ValueTask.FromResult(true));

        await switcher.ToggleFullscreen();
        Dispatcher.UIThread.RunJobs();
        await switcher.ToggleFullscreen();
        Dispatcher.UIThread.RunJobs();

        // The operating system's native restore gives the window its previous size and position back; a headless
        // window does no native restore, so this asserts the fitter moved and resized nothing on the way
        Assert.Equal(WindowMode.Normal, vm.WindowModeState.Value.Mode);
        Assert.Equal(new Size(800, 600), window.ClientSize);
        Assert.Equal(new PixelPoint(100, 50), window.Position);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Follow_LeavesFullscreenToMaximized_IsNotMoved()
    {
        Settings.WindowProperties.KeepCentered = true;
        var (fitter, vm, window) = CreateShownAutoFitWindow(() => Screen(1920, 1040));
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        var switcher = new WindowModeSwitcher(window, new NativeStateAdapter(), () => ValueTask.FromResult(true));
        await switcher.ToggleMaximized();
        Dispatcher.UIThread.RunJobs();
        window.Position = new PixelPoint(100, 50);

        await switcher.ToggleFullscreen();
        Dispatcher.UIThread.RunJobs();
        await switcher.ToggleFullscreen();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(WindowMode.Maximized, vm.WindowModeState.Value.Mode);
        Assert.Equal(new PixelPoint(100, 50), window.Position);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Follow_AutoFitTurnsOnWithStayCentered_IsCenteredInTheWorkArea()
    {
        Settings.WindowProperties.KeepCentered = true;
        var screen = Screen(1920, 1040);
        var (following, vm, window, switcher) = await CreateResizedByHandWindow(() => screen);
        using var stopsFollowing = following;
        window.Position = new PixelPoint(100, 50);

        await switcher.SetAutoFit(true);
        Dispatcher.UIThread.RunJobs();

        // Through the after-resize rule: the window takes its Fit's size again
        Assert.Equal(new Size(400, 300), window.ClientSize);
        var centered = WindowPlacement.Centered(screen, 400, 300);
        Assert.Equal(new PixelPoint(centered.X, centered.Y), window.Position);
        Assert.True(vm.WindowModeState.Value.AutoFit);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Follow_AutoFitTurnsOnWithoutStayCentered_KeepsItsCenter()
    {
        var (following, vm, window, switcher) = await CreateResizedByHandWindow(() => Screen(1920, 1040));
        using var stopsFollowing = following;
        window.Position = new PixelPoint(500, 300);
        var centerBefore = CenterOf(window);

        await switcher.SetAutoFit(true);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Size(400, 300), window.ClientSize);
        Assert.Equal(centerBefore, CenterOf(window));
        Assert.True(vm.WindowModeState.Value.AutoFit);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Follow_AutoFitTurnsOff_IsNotMoved()
    {
        Settings.WindowProperties.KeepCentered = true;
        var (fitter, vm, window) = CreateShownAutoFitWindow(() => Screen(1920, 1040));
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        window.Position = new PixelPoint(100, 50);
        var switcher = new WindowModeSwitcher(window, new NativeStateAdapter(), () => ValueTask.FromResult(true));

        await switcher.SetAutoFit(false);
        Dispatcher.UIThread.RunJobs();

        Assert.False(vm.WindowModeState.Value.AutoFit);
        Assert.Equal(new Size(400, 300), window.ClientSize);
        Assert.Equal(new PixelPoint(100, 50), window.Position);
        window.Close();
    }

    // A shown window fitted to a 400 by 300 image with AutoFit, which then turned AutoFit off and was resized by hand
    // to 600 by 500. Returns what stops its fitter following, and its own switcher
    private async Task<(IDisposable Following, MainWindowViewModel Vm, MainWindow Window, WindowModeSwitcher Switcher)>
        CreateResizedByHandWindow(Func<ScreenSize> readScreen)
    {
        var (fitter, vm, window) = CreateShownAutoFitWindow(readScreen);
        var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        Dispatcher.UIThread.RunJobs();
        var switcher = new WindowModeSwitcher(window, new NativeStateAdapter(), () => ValueTask.FromResult(true));
        await switcher.SetAutoFit(false);
        Dispatcher.UIThread.RunJobs();
        window.Width = 600;
        window.Height = 500;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Size(600, 500), window.ClientSize);
        return (following, vm, window, switcher);
    }

    private static PixelPoint CenterOf(Window window) =>
        new(window.Position.X + (int)window.ClientSize.Width / 2, window.Position.Y + (int)window.ClientSize.Height / 2);

    // An AutoFit window, shown, whose content takes the size its image is fitted to, as the main view's image does
    private (WindowFitter Fitter, MainWindowViewModel Vm, MainWindow Window) CreateShownAutoFitWindow(
        Func<ScreenSize> readScreen)
    {
        var (fitter, vm, window) = CreateWindow(readScreen);
        vm.WindowModeState.Value = vm.WindowModeState.Value.SetAutoFit(true);
        var content = new Border { Width = 400, Height = 300 };
        vm.ImageWidth.Subscribe(width => content.Width = double.IsNaN(width) ? 400 : width).AddTo(window.Disposables);
        vm.ImageHeight.Subscribe(height => content.Height = double.IsNaN(height) ? 300 : height)
            .AddTo(window.Disposables);
        window.Content = content;
        window.SizeToContent = SizeToContent.WidthAndHeight;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (fitter, vm, window);
    }

    private (WindowFitter Fitter, MainWindowViewModel Vm, MainWindow Window) CreateWindow(
        Func<ScreenSize>? readScreen = null, Func<Size>? readContentSize = null,
        Observable<Size>? contentBounds = null, Observable<Unit>? screenChanges = null)
    {
        var window = new TestMainWindow(new NativeStateAdapter());
        var vm = new MainWindowViewModel(_core, _core.Translation, _core.GlobalSettings!,
            _core.GallerySettings);
        window.DataContext = vm;
        vm.WindowTabs.SetViewFactory(new TabViews(window));
        return (new WindowFitter(window, readContentSize ?? (() => ContentSize), readScreen, contentBounds,
            screenChanges), vm, window);
    }

    private (WindowFitter Fitter, MainWindowViewModel Vm, MainWindow Window) CreateShownWindow(double width,
        double height, Func<Size>? readContentSize = null, Observable<Size>? contentBounds = null,
        Func<ScreenSize>? readScreen = null)
    {
        var (fitter, vm, window) = CreateWindow(readScreen, readContentSize, contentBounds);
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

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }

    private sealed class NativeStateAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state) =>
            WindowModeSwitcher.SetNativeState(window, state.Mode);
    }
}
