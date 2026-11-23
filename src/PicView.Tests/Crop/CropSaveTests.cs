using ImageMagick;
using PicView.Avalonia.Crop;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Saving;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using PicView.Tests.Saving;
using Orientation = PicView.Core.ImageTransformations.Orientation;

namespace PicView.Tests.Crop;

[Collection("Sequential")]
public class CropSaveTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewCropTests_" + Guid.NewGuid());
    private readonly SharedImageCache _cache = new(file => new ImageModel { FileInfo = file });
    private readonly List<IDisposable> _disposables = [];

    public CropSaveTests()
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

        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Best effort: a background preload of the tab's folder can still hold a file for a moment
        }
    }

    [AvaloniaFact]
    public async Task CropSave_OverItselfTurned_ShowsUprightAndForgetsThumbnail()
    {
        var path = Path.Combine(_testDirectory, "source.png");
        using (var image = new MagickImage(MagickColors.Red, 4, 2))
        {
            image.Write(path);
        }

        var services = TestTabServices.For(_cache, platform: new RecordingPlatform());
        var tab = new TabViewModel(new MainWindowViewModel(null!, null!, null!, null!), services);
        _disposables.Add(tab);
        await tab.Load.LoadAsync(path);
        tab.Orientation.TurnRight();
        services.Thumbnails.Keep(tab.Id, path, new object(), 100);
        using var zoom = new TabZoom();
        zoom.SetFitScale(1);
        var crop = new CropViewModel(zoom, 4, 2, tab.Orientation);
        crop.SetSelection(0, 0, 2, 4);
        tab.Crop = crop;
        var service = new CropService(tab, null!);

        var saved = await service.SaveCropToAsync(new ShownFileWriter(() => null), path);

        Assert.True(saved);
        using var written = new MagickImage(path);
        Assert.Equal(2u, written.Width);
        Assert.Equal(4u, written.Height);
        Assert.Equal(Orientation.Upright, tab.Orientation.Current.CurrentValue);
        Assert.False(services.Thumbnails.TryGetKept(new FileInfo(path), out _, out _));
        Assert.Null(tab.Crop);
    }
}
