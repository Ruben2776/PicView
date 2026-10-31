using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Views.Gallery;
using PicView.Avalonia.Views.Menu;
using PicView.Avalonia.Views.UC;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Tests.WindowBehavior;
using R3;

namespace PicView.Tests.Gallery;

/// <summary>
/// How an AutoFit window fits a Docked Gallery that moves to another edge.
/// </summary>
[Collection("Sequential")]
public class DockedGalleryWindowFitTests
{
    private const uint ImageWidth = 800;
    private const uint ImageHeight = 600;

    public DockedGalleryWindowFitTests()
    {
        // The tab's dock flags follow the setting on a frame of their own, which never comes here: as when the
        // gallery moves before they catch up
        ObservableSystem.DefaultFrameProvider = new OneFrameProvider();
        SetDefaults();
        TranslationManager.Init();
        Settings.WindowProperties.AutoFit = true;
        Settings.ImageScaling.ZoomToFit = false;
        Settings.Zoom.ScrollEnabled = false;
    }

    [AvaloniaTheory]
    [InlineData(GalleryDockPosition.Top, GalleryDockPosition.Left)]
    [InlineData(GalleryDockPosition.Bottom, GalleryDockPosition.Right)]
    [InlineData(GalleryDockPosition.Left, GalleryDockPosition.Top)]
    public void DockPositionChanged_AutoFitWindow_FitsTheGalleryAtItsNewEdge(GalleryDockPosition from,
        GalleryDockPosition to)
    {
        Settings.Gallery.DockPosition = from;
        WithDockedGallery((window, vm) =>
        {
            var before = WindowMaxSize(vm);

            // What docking at another edge does to a tab that is docked, without saving the settings file
            Settings.Gallery.DockPosition = to;
            RenderFrame(window);

            Assert.Equal(ExpectedWindowSize(vm, from), before);
            Assert.Equal(ExpectedWindowSize(vm, to), WindowMaxSize(vm));
        });
    }

    // The gallery view loads its XAML only when the application's DataContext is a CoreViewModel
    private static void WithDockedGallery(Action<MainWindow, MainWindowViewModel> test)
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        var window = new TestMainWindow(new NoOpModeAdapter());
        var vm = new MainWindowViewModel(core, core.Translation, window, core.GlobalSettings!, core.GallerySettings);
        window.DataContext = vm;
        // Resizing the window lays out its bars
        window.SharedTitleBar = new CustomTitleBar();
        window.SharedBottomBar = new BottomBar();
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Display(new ImageModel
        {
            FileInfo = new FileInfo("a.jpg"), PixelWidth = ImageWidth, PixelHeight = ImageHeight
        });
        tab.Gallery.Initialize();
        tab.Gallery.Switcher.Start(docked: true);

        var application = Application.Current!;
        var previous = application.DataContext;
        application.DataContext = core;
        try
        {
            var view = new GalleryView { DataContext = tab };
            view.Initialize(tab);
            window.Content = new DockPanel { Children = { view } };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            RenderFrame(window);

            test(window, vm);
        }
        finally
        {
            window.Close();
            application.DataContext = previous;
        }
    }

    // The gallery follows the dock setting on the window's frames. Headless frames come on a timer, and a tick renders
    // one only when something on screen changed
    private static void RenderFrame(Window window)
    {
        window.InvalidateVisual();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static Size WindowMaxSize(MainWindowViewModel vm) => new(vm.WindowMaxWidth.Value, vm.WindowMaxHeight.Value);

    private static Size ExpectedWindowSize(MainWindowViewModel vm, GalleryDockPosition dock)
    {
        var isSide = dock is GalleryDockPosition.Left or GalleryDockPosition.Right;
        var reserved = ReservedSpace.For(1, vm.TitlebarHeight.Value, vm.IsTopToolbarShown.Value,
            vm.IsBottomToolbarShown.Value, isSide ? GalleryDefaults.GetDockedGalleryWidth : 0,
            isSide ? 0 : GalleryDefaults.GetDockedGalleryHeight);
        return new Size(ImageWidth + reserved.GalleryWidth,
            ImageHeight + reserved.Top + reserved.Bottom + reserved.GalleryHeight);
    }

    private sealed class NoOpModeAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state)
        {
        }
    }

    private sealed class OneFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
