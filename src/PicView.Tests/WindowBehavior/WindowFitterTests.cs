using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
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
    public void Refit_AutoFitOff_LevelIsTheFitScaleInPercent()
    {
        var (fitter, vm, _) = CreateWindow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 1600, 600));

        fitter.Refit();

        Assert.Equal(0.5, tab.Zoom.FitScale);
        Assert.Equal(50, tab.Zoom.Level.CurrentValue);
    }

    [AvaloniaFact]
    public void Refit_AutoFitOn_LevelFollowsTheFitAgainstTheScreen()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, _) = CreateWindow(() => Screen(1280, 760));
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 1600, 2000));

        fitter.Refit();

        Assert.InRange(tab.Zoom.Level.CurrentValue, 1, 99);
        Assert.Equal(Convert.ToInt32(tab.Zoom.FitScale * 100), tab.Zoom.Level.CurrentValue);
        Assert.Equal(1600 * tab.Zoom.FitScale, vm.ImageWidth.Value, precision: 6);
    }

    [AvaloniaFact]
    public void Refit_AfterZoomToFitTurnsOn_LevelFollowsTheNewFit()
    {
        var (fitter, vm, _) = CreateWindow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 400, 300));
        fitter.Refit();
        Assert.Equal(100, tab.Zoom.Level.CurrentValue);

        Settings.ImageScaling.ZoomToFit = true;
        fitter.Refit();

        // Upscaled to fill the 800×600 content less the reserved space, so the exact percent follows the fit
        Assert.True(tab.Zoom.Level.CurrentValue > 100);
        Assert.Equal(Convert.ToInt32(tab.Zoom.FitScale * 100), tab.Zoom.Level.CurrentValue);
        Assert.Equal(400 * tab.Zoom.FitScale, vm.ImageWidth.Value, precision: 6);
    }

    [AvaloniaFact]
    public void Follow_ZoomToFitTurnsOn_FitsWithoutARefit()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 400, 300));
        fitter.Refit();
        Assert.Equal(100, tab.Zoom.Level.CurrentValue);

        vm.GlobalSettings.IsZoomedToFit.Value = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(tab.Zoom.Level.CurrentValue > 100);
        window.Close();
    }

    [AvaloniaFact]
    public void Follow_ScrollingTurnsOn_FitsToTheWidthWithoutARefit()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 1600, 2400));
        fitter.Refit();
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
    public void Follow_SideBySideTurnsOff_FitsTheOneImageWithoutARefit()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        vm.GlobalSettings.IsSideBySide.Value = true;
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg", 400, 300), Model("b.jpg", 400, 300));
        fitter.Refit();
        var pairWidth = vm.ImageWidth.Value;

        vm.GlobalSettings.IsSideBySide.Value = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(pairWidth / 2, vm.ImageWidth.Value, precision: 6);
        window.Close();
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
        fitter.Refit();

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
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));
        fitter.Refit();
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
    public void Refit_BottomBarHiddenWithTheInterface_ReservesNoRoomForIt()
    {
        // Hiding the interface hides the bottom bar but keeps its setting
        Settings.WindowProperties.AutoFit = true;
        Settings.UIProperties.ShowBottomNavBar = true;
        var (fitter, vm, _) = CreateWindow(() => Screen(1920, 1040));
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 3000, 4000));
        vm.GlobalSettings.ShowBottomBar.Value = true;
        fitter.Refit();
        var withBar = vm.ImageHeight.Value;

        vm.GlobalSettings.ShowBottomBar.Value = false;
        fitter.Refit();

        Assert.Equal(withBar + SizeDefaults.BottombarHeight, vm.ImageHeight.Value, 6);
    }

    [AvaloniaFact]
    public void Follow_BottomBarHides_FitsTheImageToTheRoomItLeaves()
    {
        Settings.WindowProperties.AutoFit = true;
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 3000, 4000));
        vm.GlobalSettings.ShowBottomBar.Value = true;
        Dispatcher.UIThread.RunJobs();
        fitter.Refit();
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
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 3000, 4000));
        Dispatcher.UIThread.RunJobs();
        fitter.Refit();
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
        fitter.Refit();
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
        fitter.Refit();
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
    public void Follow_FitterDisposedBeforeTheNewImagesFit_DoesNotThrow()
    {
        var (fitter, vm, window) = CreateShownWindow(800, 600);
        using var following = fitter.Follow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));

        fitter.Dispose();
        var thrown = Record.Exception(() => Dispatcher.UIThread.RunJobs());

        Assert.Null(thrown);
        // The Fit still applies; only its publishing is skipped
        Assert.Equal(400, vm.ImageWidth.Value);
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
        Func<ScreenSize>? readScreen = null, Func<Size>? readContentSize = null,
        Observable<Size>? contentBounds = null)
    {
        var window = new TestMainWindow(new NativeStateAdapter());
        var vm = new MainWindowViewModel(_core, _core.Translation, _core.GlobalSettings!,
            _core.GallerySettings);
        window.DataContext = vm;
        vm.WindowTabs.SetViewFactory(new TabViews(window));
        return (new WindowFitter(window, readContentSize ?? (() => ContentSize), readScreen, contentBounds), vm, window);
    }

    private (WindowFitter Fitter, MainWindowViewModel Vm, MainWindow Window) CreateShownWindow(double width,
        double height, Func<Size>? readContentSize = null, Observable<Size>? contentBounds = null)
    {
        var (fitter, vm, window) = CreateWindow(null, readContentSize, contentBounds);
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
