using Avalonia;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Tests.WindowBehavior;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// Leaving a slideshow centers the Main Window it was started on when it comes back to a Normal window with AutoFit:
/// the window's fitter follows it, as App Start attaches it, and centers it as it returns to Normal.
/// </summary>
[Collection("Sequential")]
public class SlideshowTests
{
    // Off the centre of any screen, so centring moves the window
    private static readonly PixelPoint Start = new(5, 5);

    public SlideshowTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaFact]
    public async Task StopSlideshow_AfterFullscreenWithAutoFit_CentersTheWindow()
    {
        var (window, vm) = CreateWindow(autoFit: true);
        try
        {
            Assert.True(vm.WindowTabs.ActiveTab.CurrentValue.CanNavigateForwards.Value);

            await Slideshow.StartSlideshow(window, 60000);
            Assert.True(vm.IsFullscreen.Value);
            Slideshow.StopSlideshow(vm);

            var expected = WindowPlacement.Centered(window.WindowFitter.Screen, window.ClientSize.Width,
                window.ClientSize.Height);
            var centered = new PixelPoint(expected.X, expected.Y);
            Assert.NotEqual(Start, centered);
            Pump(() => window.Position == centered);

            Assert.Equal(centered, window.Position);
            Assert.False(Slideshow.IsRunning);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task StopSlideshow_WithoutAutoFit_LeavesThePositionAlone()
    {
        var (window, vm) = CreateWindow(autoFit: false);
        try
        {
            await Slideshow.StartSlideshow(window, 60000);
            Assert.True(vm.IsFullscreen.Value);
            Slideshow.StopSlideshow(vm);
            Pump(() => false);

            Assert.False(vm.IsFullscreen.Value);
            Assert.Equal(Start, window.Position);
        }
        finally
        {
            window.Close();
        }
    }

    private static (TestMainWindow Window, MainWindowViewModel Vm) CreateWindow(bool autoFit)
    {
        Settings.WindowProperties.AutoFit = autoFit;
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));

        // Two files and at the first, so the slideshow has somewhere to go
        var tab = new TabViewModel(null!,
            TestTabServices.For(new SharedImageCache(f => ValueTask.FromResult(new ImageModel { FileInfo = f })),
                new ThumbnailCache(), new MockThumbnailLoader()));
        tab.Initialize();
        tab.ImageIterator!.Initialize([new FileInfo("a.jpg"), new FileInfo("b.jpg")]);

        var adapter = new RecordingAdapter();
        var window = new TestMainWindow(adapter);
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings,
            tab);
        window.DataContext = vm;
        window.ViewModel = vm;
        // Stops following as the window closes
        window.Disposables.Add(window.WindowFitter.Follow());
        var switcher = new WindowModeSwitcher(window, adapter, () => ValueTask.FromResult(true));
        vm.WindowModeSwitcher = switcher;
        switcher.Start(isAppStart: true);

        window.Width = 400;
        window.Height = 300;
        window.Show();
        window.Position = Start;
        return (window, vm);
    }

    // Runs the UI thread's jobs until done holds, for at most half a second
    private static void Pump(Func<bool> done)
    {
        for (var i = 0; i < 50 && !done(); i++)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
    }

    private sealed class RecordingAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state) =>
            WindowModeSwitcher.SetNativeState(window, state.Mode);
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) =>
            ValueTask.FromResult<object?>(null);

        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null,
            MagickImage? magick = null) => ValueTask.FromResult<object?>(null);

        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
