using Avalonia.Controls;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Views.Main;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Tests.ImageInfo;

/// <summary>
/// How the Image Info view follows the active tab of its Main Window.
/// </summary>
[Collection("Sequential")]
public class ImageInfoViewTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"picview-image-info-{Guid.NewGuid():N}");

    public ImageInfoViewTests()
    {
        SetDefaults();
        TranslationManager.LoadLanguage("en");
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
        GC.SuppressFinalize(this);
    }

    [AvaloniaFact]
    public void SwitchingToATabWithAnImage_WhenOpenedFromABlankTab_ShowsThatImagesValues()
    {
        var vm = CreateViewModel();
        var imageTab = vm.WindowTabs.ActiveTab.CurrentValue;
        imageTab.Display(Image("a.jpg", 640, 480));
        var blankTab = vm.WindowTabs.CreateTab();
        Assert.Same(blankTab, vm.WindowTabs.ActiveTab.CurrentValue);

        var window = new Window { Content = new ImageInfoView { DataContext = vm } };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0u, vm.Exif!.PixelWidth.Value);

            vm.WindowTabs.SelectTab(imageTab);

            Assert.True(WaitFor(() => vm.Exif.PixelWidth.Value == 640),
                "The Image Info view should show the image of the tab switched to");
        }
        finally
        {
            Close(window, vm);
        }
    }

    [AvaloniaFact]
    public void SwitchingToAnotherTabWithAnImage_ShowsThatImagesValues()
    {
        var vm = CreateViewModel();
        var firstTab = vm.WindowTabs.ActiveTab.CurrentValue;
        firstTab.Display(Image("a.jpg", 640, 480));
        var secondTab = vm.WindowTabs.CreateTab();
        secondTab.Display(Image("b.jpg", 800, 600));
        vm.WindowTabs.SelectTab(firstTab);

        var window = new Window { Content = new ImageInfoView { DataContext = vm } };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => vm.Exif!.PixelWidth.Value == 640),
                "The Image Info view should show the active tab's image when it opens");

            vm.WindowTabs.SelectTab(secondTab);

            Assert.True(WaitFor(() => vm.Exif!.PixelWidth.Value == 800),
                "The Image Info view should show the image of the tab switched to");
        }
        finally
        {
            Close(window, vm);
        }
    }

    [AvaloniaFact]
    public void NavigatingInABackgroundTab_KeepsTheActiveTabsValues()
    {
        var vm = CreateViewModel();
        var firstTab = vm.WindowTabs.ActiveTab.CurrentValue;
        firstTab.Display(Image("a.jpg", 640, 480));
        var secondTab = vm.WindowTabs.CreateTab();
        secondTab.Display(Image("b.jpg", 800, 600));

        var window = new Window { Content = new ImageInfoView { DataContext = vm } };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(WaitFor(() => vm.Exif!.PixelWidth.Value == 800),
                "The Image Info view should show the active tab's image when it opens");

            firstTab.Display(Image("c.jpg", 320, 240));

            Assert.False(WaitFor(() => vm.Exif!.PixelWidth.Value == 320, TimeSpan.FromMilliseconds(300)),
                "The Image Info view should not show an image shown in a tab that is not active");
            Assert.Equal(800u, vm.Exif!.PixelWidth.Value);
        }
        finally
        {
            Close(window, vm);
        }
    }

    // Set up as the window opener does it, with the Exif and Info Window view models the view reads
    private static MainWindowViewModel CreateViewModel()
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        var vm = new MainWindowViewModel(core, core.Translation, null!, core.GlobalSettings!, core.GallerySettings)
        {
            Exif = new ExifViewModel()
        };
        vm.InfoWindow = new ImageInfoWindowViewModel(vm);
        return vm;
    }

    private static void Close(Window window, MainWindowViewModel vm)
    {
        window.Close();
        vm.Exif?.Dispose();
        vm.InfoWindow?.Dispose();
    }

    // A real file, as the view reads its size
    private ImageModel Image(string name, uint width, uint height)
    {
        var path = Path.Combine(_directory, name);
        using (var image = new MagickImage(MagickColors.White, 1, 1))
        {
            image.Format = MagickFormat.Jpeg;
            image.Write(path);
        }
        return new ImageModel { FileInfo = new FileInfo(path), PixelWidth = width, PixelHeight = height };
    }

    // The values are read off the UI thread and set back on it, so the dispatcher runs while waiting
    private static bool WaitFor(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            if (condition())
            {
                return true;
            }
            Thread.Sleep(10);
        }
        return condition();
    }
}
