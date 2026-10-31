using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Views.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Tests.WindowBehavior;
using R3;

namespace PicView.Tests.Gallery;

/// <summary>
/// How the gallery view shows the items of its tab's gallery as they change.
/// </summary>
[Collection("Sequential")]
public class GalleryViewTests
{
    public GalleryViewTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaFact]
    public void Sorted_ShownGallery_ShowsItsItemsInTheNewOrder()
    {
        WithGalleryView((tab, viewer) =>
        {
            var fileA = new FileInfo("a.jpg");
            var fileB = new FileInfo("b.jpg");
            var fileC = new FileInfo("c.jpg");
            var a = new GalleryItemViewModel { FileInfo = fileA };
            var b = new GalleryItemViewModel { FileInfo = fileB };
            var c = new GalleryItemViewModel { FileInfo = fileC };
            tab.Gallery.GalleryItems.AddRange([a, b, c]);
            Dispatcher.UIThread.RunJobs();

            var order = new[] { fileC, fileA, fileB };
            tab.Gallery.GalleryItems.Sort(Comparer<GalleryItemViewModel>.Create((x, y) =>
                Array.IndexOf(order, x.FileInfo).CompareTo(Array.IndexOf(order, y.FileInfo))));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([c, a, b], viewer.Items.Cast<GalleryItemViewModel>());
        });
    }

    [AvaloniaFact]
    public void Cleared_ShownGallery_ShowsNoItems()
    {
        WithGalleryView((tab, viewer) =>
        {
            tab.Gallery.GalleryItems.AddRange([
                new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") },
                new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") }
            ]);
            Dispatcher.UIThread.RunJobs();

            tab.Gallery.GalleryItems.Clear();
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(viewer.Items);
        });
    }

    [AvaloniaFact]
    public void Replaced_ShownGallery_ShowsTheNewItem()
    {
        WithGalleryView((tab, viewer) =>
        {
            var a = new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") };
            var b = new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") };
            tab.Gallery.GalleryItems.AddRange([a, b]);
            Dispatcher.UIThread.RunJobs();
            var rebuilt = new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") };

            tab.Gallery.GalleryItems[1] = rebuilt;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([a, rebuilt], viewer.Items.Cast<GalleryItemViewModel>());
        });
    }

    [AvaloniaFact]
    public void Initialize_ForATabThatIsNotTheActiveOne_ShowsThatTabsItems()
    {
        WithGalleryView(vm =>
        {
            var first = vm.WindowTabs.ActiveTab.CurrentValue;
            var second = vm.WindowTabs.CreateTab();
            vm.WindowTabs.SelectTab(first);
            return second;
        }, (tab, vm, viewer) =>
        {
            var active = vm.WindowTabs.ActiveTab.CurrentValue;
            Assert.NotSame(tab, active);
            var own = new GalleryItemViewModel { FileInfo = new FileInfo("own.jpg") };

            tab.Gallery.GalleryItems.Add(own);
            active.Gallery.GalleryItems.Add(new GalleryItemViewModel { FileInfo = new FileInfo("other.jpg") });
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([own], viewer.Items.Cast<GalleryItemViewModel>());
        });
    }

    private static void WithGalleryView(Action<TabViewModel, NavigateAbleItemsViewer> test) =>
        WithGalleryView(static vm => vm.WindowTabs.ActiveTab.CurrentValue, (tab, _, viewer) => test(tab, viewer));

    // The gallery view loads its XAML only when the application's DataContext is a CoreViewModel
    private static void WithGalleryView(Func<MainWindowViewModel, TabViewModel> tabOf,
        Action<TabViewModel, MainWindowViewModel, NavigateAbleItemsViewer> test)
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        var window = new TestMainWindow(new NoOpModeAdapter());
        var vm = new MainWindowViewModel(core, core.Translation, window, core.GlobalSettings!, core.GallerySettings);
        window.DataContext = vm;
        core.MainWindows.ActiveWindow.Value = vm;
        var tab = tabOf(vm);
        var thumbCache = new MockThumbnailCache();
        tab.Initialize(new SharedImageCache(static f => ValueTask.FromResult(new ImageModel { FileInfo = f })),
            thumbCache, new MockThumbnailLoader());

        var application = Application.Current!;
        var previous = application.DataContext;
        application.DataContext = core;
        try
        {
            var view = new GalleryView();
            view.Initialize(tab);
            test(tab, vm, view.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!);
        }
        finally
        {
            application.DataContext = previous;
        }
    }

    private sealed class NoOpModeAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state)
        {
        }
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) => ValueTask.FromResult<object?>(null);
        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class MockThumbnailCache : IThumbnailCache
    {
        public void Add(uint ownerId, string path, object thumbnail) { }
        public bool TryGet(string path, out object? thumbnail) { thumbnail = null; return false; }
        public void Remove(string path) { }
        public void RemoveOwner(uint ownerId) { }
        public bool IsEmpty => true;
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
