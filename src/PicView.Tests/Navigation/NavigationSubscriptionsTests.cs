using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ImageDecoding;
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
        var vm = new MainWindowViewModel(core, core.Translation, window, core.GlobalSettings!, core.GallerySettings);
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
            tab.CurrentView.Value = new ImageViewer(tab);
            using var attached = hookups.Attach(tab);
            tab.ZoomLevel.Value = Untouched;

            tab.Display(BitmapModel("a.jpg"));
            Dispatcher.UIThread.RunJobs();

            // The reaction sets the zoom level from the fitted zoom
            Assert.NotEqual(Untouched, tab.ZoomLevel.Value);
            Assert.Equal(Convert.ToInt32(tab.InitialZoom.Value * 100), tab.ZoomLevel.Value);
        });
    }

    [AvaloniaFact]
    public void Attach_ToTabThatAlreadyShows_RunsNoReaction()
    {
        WithWindow((window, vm, hookups, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"));
            tab.CurrentView.Value = new ImageViewer(tab);
            tab.ZoomLevel.Value = Untouched;

            using var attached = hookups.Attach(tab);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Untouched, tab.ZoomLevel.Value);
        });
    }

    [AvaloniaFact]
    public void ShowStandIn_AfterAttach_RunsNoReaction()
    {
        WithWindow((window, vm, hookups, tab) =>
        {
            tab.CurrentView.Value = new ImageViewer(tab);
            using var attached = hookups.Attach(tab);
            tab.Display(BitmapModel("a.jpg"));
            Dispatcher.UIThread.RunJobs();
            tab.ZoomLevel.Value = Untouched;

            tab.ShowStandIn(new object());
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Untouched, tab.ZoomLevel.Value);
        });
    }

    [AvaloniaFact]
    public void Display_WhileViewIsNotAnImageViewer_DoesNothing()
    {
        WithWindow((window, vm, hookups, tab) =>
        {
            using var attached = hookups.Attach(tab);
            tab.ZoomLevel.Value = Untouched;

            tab.Display(BitmapModel("a.jpg"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Untouched, tab.ZoomLevel.Value);
        });
    }

    [AvaloniaFact]
    public void Display_Reaction_RunsBeforeWorkPostedAfterIt()
    {
        WithWindow((window, vm, hookups, tab) =>
        {
            tab.CurrentView.Value = new ImageViewer(tab);
            using var attached = hookups.Attach(tab);
            tab.ZoomLevel.Value = Untouched;
            var zoomWhenLaterWorkRan = Untouched - 1;

            // As QuickLoad posts the Center after the first image, at the highest priority
            tab.Display(BitmapModel("a.jpg"));
            Dispatcher.UIThread.Post(() => zoomWhenLaterWorkRan = tab.ZoomLevel.Value, DispatcherPriority.Send);
            Dispatcher.UIThread.RunJobs();

            Assert.NotEqual(Untouched, zoomWhenLaterWorkRan);
        });
    }

    // A zoom level no reaction produces, so a test can tell whether one ran
    private const int Untouched = -1;

    private static void WithWindow(Action<TestMainWindow, MainWindowViewModel, NavigationSubscriptions, TabViewModel> test)
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        var window = new TestMainWindow(new NoOpModeAdapter());
        var vm = new MainWindowViewModel(core, core.Translation, window, core.GlobalSettings!, core.GallerySettings);
        window.DataContext = vm;
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

    private static ImageModel BitmapModel(string name) => new()
    {
        FileInfo = new FileInfo(name),
        Image = new WriteableBitmap(new PixelSize(2, 2), new Vector(96, 96)),
        PixelWidth = 2,
        PixelHeight = 2,
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
