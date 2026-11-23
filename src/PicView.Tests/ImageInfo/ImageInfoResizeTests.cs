using System.Globalization;
using ImageMagick;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using PicView.Tests.Config;
using PicView.Tests.Navigation;
using Orientation = PicView.Core.ImageTransformations.Orientation;

namespace PicView.Tests.ImageInfo;

/// <summary>
/// The Image Info window's resize by width or height, which writes through the save module.
/// </summary>
[Collection("Sequential")]
public class ImageInfoResizeTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewInfoResize_" + Guid.NewGuid());
    private readonly List<IDisposable> _disposables = [];

    public ImageInfoResizeTests()
    {
        SetDefaults();
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task InfoResize_Turned_WritesTurnAndShowsUpright()
    {
        var wasReset = Settings.Zoom.ResetZoomOnChange;
        Settings.Zoom.ResetZoomOnChange = false;
        try
        {
            var path = Path.Combine(_testDirectory, "a.png");
            using (var image = new MagickImage(MagickColors.Red, 4, 2))
            {
                image.Write(path);
            }

            var (mainVm, tab) = await CreateAsync(path);
            tab.Orientation.TurnRight();

            await mainVm.InfoWindow!.ResizeImageAsync(true, "2", "0");

            // Turned to 2x4, then fitted to a width of 2; resizing the unturned file would give 2x1
            using var written = new MagickImage(path);
            Assert.Equal((2u, 4u), (written.Width, written.Height));
            Assert.Equal(Orientation.Upright, tab.Orientation.Current.CurrentValue);
            Assert.Equal((2u, 4u), (tab.Model.PixelWidth, tab.Model.PixelHeight));
        }
        finally
        {
            Settings.Zoom.ResetZoomOnChange = wasReset;
        }
    }

    [Fact]
    public async Task InfoResize_SingleImage_DoesNothingAndDoesNotThrow()
    {
        var (mainVm, tab) = await CreateAsync(null);

        await mainVm.InfoWindow!.ResizeImageAsync(true, "100", "0");
        await mainVm.InfoWindow.ResizeImageAsync(false, "0", "50");

        Assert.Null(tab.FileInfo.CurrentValue);
        Assert.False(mainVm.InfoWindow.IsLoading.Value);
    }

    // Text double.TryParse reads as NaN, infinity or a side too large, each once
    public static TheoryData<string> NotASize() => new(new[]
    {
        "NaN", "Infinity", CultureInfo.CurrentCulture.NumberFormat.NaNSymbol,
        CultureInfo.CurrentCulture.NumberFormat.PositiveInfinitySymbol, "65536"
    }.Distinct());

    [Theory]
    [MemberData(nameof(NotASize))]
    public async Task InfoResize_NotASize_WritesNothing(string text)
    {
        var path = Path.Combine(_testDirectory, "a.png");
        using (var image = new MagickImage(MagickColors.Red, 4, 2))
        {
            image.Write(path);
        }

        var before = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        var written = File.GetLastWriteTimeUtc(path);
        var (mainVm, _) = await CreateAsync(path);

        await mainVm.InfoWindow!.ResizeImageAsync(true, text, "0");

        Assert.Equal(written, File.GetLastWriteTimeUtc(path));
        Assert.Equal(before, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
    }

    // A Main Window whose active tab loaded the file at path, or shows no file when it is null
    private async Task<(MainWindowViewModel MainVm, TabViewModel Tab)> CreateAsync(string? path)
    {
        var core = new CoreViewModel(null, file =>
        {
            var info = new MagickImageInfo(file);
            return new ImageModel { FileInfo = file, PixelWidth = info.Width, PixelHeight = info.Height };
        });
        core.InitializeNavigation(TestTabServices.For(core.SharedCache), new NavigationService());
        var mainVm = new MainWindowViewModel(core, new TranslationViewModel(), StartedSettings.New(),
            new GallerySharedSettingsViewModel());
        mainVm.InfoWindow = new ImageInfoWindowViewModel(mainVm);
        _disposables.Add(mainVm.InfoWindow);
        var tab = mainVm.WindowTabs.ActiveTab.CurrentValue;
        _disposables.Add(tab);
        if (path is not null)
        {
            await tab.Load.LoadAsync(path);
        }

        return (mainVm, tab);
    }
}
