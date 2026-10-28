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
    public void SortLoadedGallery_ShownGallery_ShowsItsItemsInTheNewOrder()
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

            tab.GalleryCoordinator!.SortLoadedGallery([fileC, fileA, fileB]);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([c, a, b], viewer.Items.Cast<GalleryItemViewModel>());
        });
    }

    [AvaloniaFact]
    public void Clear_ShownGallery_ShowsNoItems()
    {
        WithGalleryView((tab, viewer) =>
        {
            tab.Gallery.GalleryItems.AddRange([
                new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") },
                new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") }
            ]);
            Dispatcher.UIThread.RunJobs();

            tab.GalleryCoordinator!.Clear();
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(viewer.Items);
        });
    }

    // The gallery view finds its tab through the application's DataContext, as the active tab of the active window
    private static void WithGalleryView(Action<TabViewModel, NavigateAbleItemsViewer> test)
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        var window = new TestMainWindow(new NoOpModeAdapter());
        var vm = new MainWindowViewModel(core, core.Translation, window, core.GlobalSettings!, core.GallerySettings);
        window.DataContext = vm;
        core.MainWindows.ActiveWindow.Value = vm;
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var thumbCache = new MockThumbnailCache();
        tab.Initialize(new SharedImageCache(static f => ValueTask.FromResult(new ImageModel { FileInfo = f })),
            thumbCache, new MockThumbnailLoader());

        var application = Application.Current!;
        var previous = application.DataContext;
        application.DataContext = core;
        try
        {
            var view = new GalleryView { DataContext = tab };
            test(tab, view.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!);
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
