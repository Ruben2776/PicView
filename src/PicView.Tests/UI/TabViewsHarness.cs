using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using PicView.Tests.WindowBehavior;

namespace PicView.Tests.UI;

/// <summary>
/// A shown test Main Window whose tabs get their views from a real <see cref="TabViews"/>, for the suites that test
/// the controls a tab shows.
/// </summary>
internal static class TabViewsHarness
{
    public static object? ViewOf(TabViewModel tab) => tab.Views!.View.Value;

    // Loaded is raised by a layout pass, which the headless window runs when asked
    public static void Settle(TestMainWindow window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    public static void WithWindow(Action<TestMainWindow, MainWindowViewModel, TabViewModel> test)
        => WithWindowAsync((window, vm, tab) =>
        {
            test(window, vm, tab);
            return Task.CompletedTask;
        }).GetAwaiter().GetResult();

    // The gallery view loads its XAML only when the application's DataContext is a CoreViewModel with its navigation
    public static async Task WithWindowAsync(Func<TestMainWindow, MainWindowViewModel, TabViewModel, Task> test)
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

    public static ImageModel BitmapModel(string name) => new()
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
