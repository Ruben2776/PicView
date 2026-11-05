using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ImageDecoding;
using PicView.Core.ImageTransformations;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using PicView.Core.Sizing;
using PicView.Tests.WindowBehavior;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class NavigationSubscriptionsTests
{
    public NavigationSubscriptionsTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaFact]
    public async Task Attach_FromABackgroundThread_AttachesTheTab()
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        var window = new TestMainWindow(new NoOpModeAdapter());
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        window.DataContext = vm;
        var hookups = new NavigationSubscriptions(window, vm);
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;

        // Opening the first source sets up a tab off the UI thread, as QuickLoad does
        using var attached = await Task.Run(() => hookups.Attach(tab), TestContext.Current.CancellationToken);

        Assert.NotNull(attached);
    }

    [AvaloniaFact]
    public void Display_AfterAttachWithImageViewer_RunsTheReactionOnce()
    {
        WithWindow((window, vm, hookups, tab) =>
        {
            ViewerOf(tab);
            using var attached = hookups.Attach(tab);
            tab.Orientation.TurnRight();

            tab.Display(BitmapModel("a.jpg"));
            Dispatcher.UIThread.RunJobs();

            // The reaction resets the orientation, as zoom resets on image change by default
            Assert.Equal(Orientation.Upright, tab.Orientation.Current.CurrentValue);
        });
    }

    [AvaloniaFact]
    public void Attach_ToTabThatAlreadyShows_RunsNoReaction()
    {
        WithWindow((window, vm, hookups, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"));
            ViewerOf(tab);
            tab.Orientation.TurnRight();

            using var attached = hookups.Attach(tab);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, tab.Orientation.Current.CurrentValue.QuarterTurns);
        });
    }

    [AvaloniaFact]
    public void ShowStandIn_AfterAttach_RunsNoReaction()
    {
        WithWindow((window, vm, hookups, tab) =>
        {
            ViewerOf(tab);
            using var attached = hookups.Attach(tab);
            tab.Display(BitmapModel("a.jpg"));
            Dispatcher.UIThread.RunJobs();
            tab.Orientation.TurnRight();

            tab.ShowStandIn(new object());
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, tab.Orientation.Current.CurrentValue.QuarterTurns);
        });
    }

    [AvaloniaFact]
    public void Display_WhileViewIsNotAnImageViewer_DoesNothing()
    {
        WithWindow((window, vm, hookups, tab) =>
        {
            // A tab that shows an image derives the viewer unless it crops
            tab.Crop = new CropViewModel();
            using var attached = hookups.Attach(tab);
            tab.Orientation.TurnRight();

            tab.Display(BitmapModel("a.jpg"));
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<CropControl>(tab.CurrentView.Value);
            Assert.Equal(1, tab.Orientation.Current.CurrentValue.QuarterTurns);
        });
    }

    [AvaloniaFact]
    public void Display_Reaction_RunsBeforeWorkPostedAfterIt()
    {
        WithWindow((window, vm, hookups, tab) =>
        {
            ViewerOf(tab);
            using var attached = hookups.Attach(tab);
            tab.Orientation.TurnRight();
            var turnsWhenLaterWorkRan = -1;

            // As QuickLoad posts the Center after the first image, at the highest priority
            tab.Display(BitmapModel("a.jpg"));
            Dispatcher.UIThread.Post(() => turnsWhenLaterWorkRan = tab.Orientation.Current.CurrentValue.QuarterTurns,
                DispatcherPriority.Send);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, turnsWhenLaterWorkRan);
        });
    }

    [AvaloniaFact]
    public void WheelZoom_OnTheTabsViewer_ChangesTheLevelByTheZoomSpeed()
    {
        Settings.Zoom.ZoomSpeed = 0.3;
        WithWindow((window, vm, hookups, tab) =>
        {
            var viewer = ViewerOf(tab);
            using var attached = hookups.Attach(tab);
            tab.Display(BitmapModel("a.jpg"));
            Dispatcher.UIThread.RunJobs();
            tab.Zoom.SetFitScale(0.5);

            ((IZoomScrollTarget)viewer).ZoomAt(zoomIn: true, new Point(1, 1));

            Assert.Equal(1.3, tab.Zoom.Factor);
            Assert.Equal(65, tab.Zoom.Level.CurrentValue);
        });
    }

    [AvaloniaFact]
    public void Display_WhileZoomed_ControlResetReportsAfterTheFitWroteTheNewFitScale()
    {
        // AutoFit fits against the screen, which a test window has; a fixed window would read no content size
        Settings.WindowProperties.AutoFit = true;
        Settings.Zoom.ResetZoomOnChange = true;
        WithWindow((window, vm, hookups, tab) =>
        {
            var viewer = ViewerOf(tab);
            using var attached = hookups.Attach(tab);
            tab.Display(BitmapModel("a.jpg"));
            Dispatcher.UIThread.RunJobs();
            // Zoomed, so the next image makes the reaction reset the control, which reports its scale
            ((IZoomScrollTarget)viewer).ZoomAt(zoomIn: true, new Point(1, 1));
            var control = viewer.FindControl<ZoomPanControl>("ZoomPanControl")!;
            var fitScaleWhenTheControlReset = double.NaN;
            control.ScaleChanged += _ => fitScaleWhenTheControlReset = tab.Zoom.FitScale;

            // Larger than the screen, so its Fit Scale differs from the first image's
            tab.Display(BitmapModel("b.jpg", 4000, 4000));
            Dispatcher.UIThread.RunJobs();

            Assert.InRange(tab.Zoom.FitScale, 0.01, 0.99);
            // The zoom percentage popup reads the level when the control reports, so the fit must already be the new one
            Assert.Equal(tab.Zoom.FitScale, fitScaleWhenTheControlReset);
        });
    }

    private static void WithWindow(Action<TestMainWindow, MainWindowViewModel, NavigationSubscriptions, TabViewModel> test)
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        var window = new TestMainWindow(new NoOpModeAdapter());
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        window.DataContext = vm;
        vm.WindowTabs.SetViewFactory(new TabViews(window));
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        tab.IsSelected = true;
        var application = Application.Current!;
        var previous = application.DataContext;
        application.DataContext = core;
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            test(window, vm, new NavigationSubscriptions(window, vm), tab);
        }
        finally
        {
            window.Close();
            application.DataContext = previous;
        }
    }

    private static ImageViewer ViewerOf(TabViewModel tab)
    {
        tab.SetLoading();
        return (ImageViewer)tab.CurrentView.Value!;
    }

    private static ImageModel BitmapModel(string name, uint pixelWidth = 2, uint pixelHeight = 2) => new()
    {
        FileInfo = new FileInfo(name),
        Image = new WriteableBitmap(new PixelSize(2, 2), new Vector(96, 96)),
        PixelWidth = pixelWidth,
        PixelHeight = pixelHeight,
        ImageType = ImageType.Bitmap
    };

    private sealed class NoOpModeAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state)
        {
        }
    }
}
