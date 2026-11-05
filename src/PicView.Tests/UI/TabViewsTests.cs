using System.Collections;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Localization;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using PicView.Tests.WindowBehavior;

namespace PicView.Tests.UI;

[Collection("Sequential")]
public class TabViewsTests
{
    public TabViewsTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaFact]
    public void Attach_NewTab_ShowsAMenuWithTheTabAsDataContext()
        => WithWindow((_, _, tab) =>
        {
            var menu = Assert.IsType<StartUpMenu>(ViewOf(tab));
            Assert.Same(tab, menu.DataContext);
        });

    [AvaloniaFact]
    public void SetLoading_ShowsAViewerWithTheTabAsDataContext()
        => WithWindow((_, _, tab) =>
        {
            tab.SetLoading();
            var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
            Assert.Same(tab, viewer.DataContext);
        });

    [AvaloniaFact]
    public void Crop_SetAndCleared_ShowsACropControlThenTheSameViewer()
        => WithWindow((_, _, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"));
            var viewer = ViewOf(tab);
            tab.Crop = new CropViewModel();
            Assert.IsType<CropControl>(ViewOf(tab));
            tab.Crop = null;
            Assert.Same(viewer, ViewOf(tab));
        });

    [AvaloniaFact]
    public void EndLoading_OnAFreshTab_ShowsTheKeptMenuAgain()
        => WithWindow((_, _, tab) =>
        {
            var menu = ViewOf(tab);
            var ticket = tab.SetLoading();
            tab.EndLoading(ticket);
            Assert.Same(menu, ViewOf(tab));
        });

    [AvaloniaFact]
    public void Reorder_KeepsTheSameViewer()
        => WithWindow((_, vm, tab) =>
        {
            tab.SetLoading();
            var viewer = ViewOf(tab);
            vm.WindowTabs.CreateTab();
            vm.WindowTabs.Reorder(tab, 1);
            Dispatcher.UIThread.RunJobs();
            Assert.Same(viewer, ViewOf(tab));
        });

    [AvaloniaFact]
    public void CloseTab_DisposesTheViewerAndClearsTheView()
        => WithWindow((_, vm, _) =>
        {
            var tab = vm.WindowTabs.CreateTab();
            tab.SetLoading();
            var views = tab.Views!;
            var viewer = (ImageViewer)views.View.Value!;
            var a = new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") };
            tab.Gallery.GalleryItems.Add(a);
            Dispatcher.UIThread.RunJobs();
            vm.WindowTabs.CloseTab(tab);
            tab.Gallery.GalleryItems.Add(new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") });
            Dispatcher.UIThread.RunJobs();
            Assert.Null(views.View.Value);
            Assert.Null(tab.Views);
            Assert.Equal([a],
                viewer.GalleryView.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!.Items
                    .Cast<GalleryItemViewModel>());
        });

    [AvaloniaFact]
    public async Task Attach_OffTheUIThread_MakesTheMenuOnTheUIThread()
    {
        // Consecutive pasted files create their tabs off the UI thread
        await WithWindowAsync(async (_, vm, _) =>
        {
            // Made on a thread of its own and joined, so the UI thread cannot run the posted reaction before the
            // assertion, and a waited-for pool task cannot be run inline on the UI thread
            TabViewModel? tab = null;
            var thread = new Thread(() => tab = vm.WindowTabs.CreateTab());
            thread.Start();
            thread.Join();
            Assert.Null(ViewOf(tab!));
            Dispatcher.UIThread.RunJobs();
            Assert.IsType<StartUpMenu>(ViewOf(tab!));
        });
    }

    [AvaloniaFact]
    public void StartUpMenu_ReattachedAfterAViewer_HandlesAClickOnce()
        => WithWindow((window, _, tab) =>
        {
            // The test window has no main view, so a host presents whatever the tab shows, as the main view does
            var host = new ContentControl();
            window.Content = host;
            var menu = Assert.IsType<StartUpMenu>(ViewOf(tab));
            host.Content = menu;
            Settle(window);
            Assert.True(menu.IsLoaded);

            // The viewer replaces the menu, which detaches it; ending the load on a fresh tab shows the same menu again
            var ticket = tab.SetLoading();
            Assert.IsType<ImageViewer>(ViewOf(tab));
            // The viewer's own Loaded needs the full window, so the host is left empty while it shows
            host.Content = null;
            Settle(window);
            Assert.False(menu.IsLoaded);
            tab.EndLoading(ticket);
            host.Content = ViewOf(tab);
            Settle(window);
            Assert.Same(menu, ViewOf(tab));
            Assert.True(menu.IsLoaded);

            // The click's effect opens the last file, so count what the button holds for Click instead
            Assert.Equal(1, ClickHandlerCount(menu.OpenLastFileButton));
        });

    // Loaded is raised by a layout pass, which the headless window runs when asked
    private static void Settle(TestMainWindow window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    // Interactive keeps its routed event handlers in a dictionary keyed by event
    private static int ClickHandlerCount(Button button)
    {
        for (var type = typeof(Button); type is not null; type = type.BaseType)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (field.GetValue(button) is IDictionary handlers && handlers.Contains(Button.ClickEvent))
                {
                    return ((ICollection)handlers[Button.ClickEvent]!).Count;
                }
            }
        }

        return 0;
    }

    [AvaloniaFact]
    public async Task SetLoading_OnAThreadPoolThread_ShowsTheViewerOnTheUIThread()
        => await WithWindowAsync(async (_, _, tab) =>
        {
            await Task.Run(tab.SetLoading, TestContext.Current.CancellationToken);
            Dispatcher.UIThread.RunJobs();
            Assert.IsType<ImageViewer>(ViewOf(tab));
        });

    private static object? ViewOf(TabViewModel tab) => tab.Views!.View.Value;

    private static void WithWindow(Action<TestMainWindow, MainWindowViewModel, TabViewModel> test)
        => WithWindowAsync((window, vm, tab) =>
        {
            test(window, vm, tab);
            return Task.CompletedTask;
        }).GetAwaiter().GetResult();

    // The gallery view loads its XAML only when the application's DataContext is a CoreViewModel with its navigation
    private static async Task WithWindowAsync(Func<TestMainWindow, MainWindowViewModel, TabViewModel, Task> test)
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        core.InitializeNavigation(
            TestTabServices.For(new SharedImageCache(static f => ValueTask.FromResult(new ImageModel { FileInfo = f })),
                new MockThumbnailCache(), new MockThumbnailLoader()),
            new NavigationService(string.CompareOrdinal));
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
            await test(window, vm, tab);
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

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null,
            MagickImage? magick = null) => ValueTask.FromResult<object?>(null);

        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class MockThumbnailCache : IThumbnailCache
    {
        public void Add(uint ownerId, string path, object thumbnail) { }

        public bool TryGet(string path, out object? thumbnail)
        {
            thumbnail = null;
            return false;
        }

        public void Remove(string path) { }
        public void RemoveOwner(uint ownerId) { }
        public bool IsEmpty => true;
    }

    private sealed class NoOpModeAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state)
        {
        }
    }
}
