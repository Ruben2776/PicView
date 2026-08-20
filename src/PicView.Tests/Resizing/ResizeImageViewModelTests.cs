using ImageMagick;
using PicView.Core.Extensions;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Resizing;

[Collection("Sequential")]
public class ResizeImageViewModelTests
{
    private ManualFrameProvider _frameProvider;

    public ResizeImageViewModelTests()
    {
        _frameProvider = new ManualFrameProvider();
        ObservableSystem.DefaultFrameProvider = _frameProvider;
    }

    private class ManualFrameProvider : FrameProvider
    {
        private readonly List<IFrameRunnerWorkItem> _items = new();
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => _items.Add(callback);
        public void Tick()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (!_items[i].MoveNext(0))
                    _items.RemoveAt(i);
            }
        }
    }

    private (MainWindowViewModel MainVm, ResizeImageViewModel ResizeVm) CreateViewModels()
    {
        SetDefaults();

        var globalSettings = new GlobalSettingsViewModel();
        var gallerySettings = new GallerySharedSettingsViewModel();
        var translations = new TranslationViewModel();
        var mainVm = new MainWindowViewModel(translations, null!, globalSettings, gallerySettings);

        var resizeVm = new ResizeImageViewModel();
        resizeVm.Initialize(mainVm);

        return (mainVm, resizeVm);
    }

    [Fact]
    public void UpdateFromImageChange_UpdatesOriginalAndDesiredDimensions()
    {
        var (mainVm, resizeVm) = CreateViewModels();

        var model = new ImageModel
        {
            PixelWidth = 1920,
            PixelHeight = 1080
        };

        mainVm.WindowTabs.ActiveTab.Value.Model = model;
        _frameProvider.Tick();

        Assert.Equal(1920u, resizeVm.OriginalPixelWidth.Value);
        Assert.Equal(1080u, resizeVm.OriginalPixelHeight.Value);
        Assert.Equal("1920", resizeVm.DesiredPixelWidth.Value);
        Assert.Equal("1080", resizeVm.DesiredPixelHeight.Value);
    }

    [Fact]
    public void AdjustAspectRatio_WidthChanged_HeightUpdates()
    {
        var (mainVm, resizeVm) = CreateViewModels();
        var model = new ImageModel
        {
            PixelWidth = 1920,
            PixelHeight = 1080
        };
        mainVm.WindowTabs.ActiveTab.Value.Model = model;
        _frameProvider.Tick();

        // Change width to 960 (half)
        resizeVm.DesiredPixelWidth.Value = "960";
        _frameProvider.Tick();

        // Height should be 540
        Assert.Equal("540", resizeVm.DesiredPixelHeight.Value);
    }

    [Fact]
    public void AdjustAspectRatio_HeightChanged_WidthUpdates()
    {
        var (mainVm, resizeVm) = CreateViewModels();
        var model = new ImageModel
        {
            PixelWidth = 1920,
            PixelHeight = 1080
        };
        mainVm.WindowTabs.ActiveTab.Value.Model = model;
        _frameProvider.Tick();

        // Change height to 540 (half)
        resizeVm.DesiredPixelHeight.Value = "540";
        _frameProvider.Tick();

        // Width should be 960
        Assert.Equal("960", resizeVm.DesiredPixelWidth.Value);
    }

    [Fact]
    public void AdjustAspectRatio_WidthChangedPercentage_HeightUpdates()
    {
        var (mainVm, resizeVm) = CreateViewModels();
        var model = new ImageModel
        {
            PixelWidth = 1000,
            PixelHeight = 1000
        };
        mainVm.WindowTabs.ActiveTab.Value.Model = model;
        _frameProvider.Tick();

        // Use percentage (50%)
        resizeVm.DesiredPixelWidth.Value = "50%";
        _frameProvider.Tick();

        // Height and Width should both be 500
        Assert.Equal("500", resizeVm.DesiredPixelWidth.Value);
        Assert.Equal("500", resizeVm.DesiredPixelHeight.Value);
    }

    [Fact]
    public void ToggleAspectRatio_DisablesProportionalScaling()
    {
        var (mainVm, resizeVm) = CreateViewModels();
        var model = new ImageModel
        {
            PixelWidth = 1920,
            PixelHeight = 1080
        };
        mainVm.WindowTabs.ActiveTab.Value.Model = model;
        _frameProvider.Tick();

        // Disable keeping aspect ratio
        resizeVm.ToggleAspectRatio();
        _frameProvider.Tick();
        Assert.False(resizeVm.IsKeepingAspectRatio.Value);

        // Change width
        resizeVm.DesiredPixelWidth.Value = "1000";
        _frameProvider.Tick();

        // Height should NOT change
        Assert.Equal("1080", resizeVm.DesiredPixelHeight.Value);
    }

    [Fact]
    public void ResetSettings_RestoresOriginalDimensions()
    {
        var (mainVm, resizeVm) = CreateViewModels();
        var model = new ImageModel
        {
            PixelWidth = 1920,
            PixelHeight = 1080
        };
        mainVm.WindowTabs.ActiveTab.Value.Model = model;
        _frameProvider.Tick();

        // Change dimensions
        resizeVm.DesiredPixelWidth.Value = "800";
        _frameProvider.Tick();
        
        // Ensure they are changed
        Assert.Equal("800", resizeVm.DesiredPixelWidth.Value);
        Assert.Equal("450", resizeVm.DesiredPixelHeight.Value);

        // Reset
        resizeVm.ResetSettings();
        _frameProvider.Tick();

        // Should be restored
        Assert.Equal("1920", resizeVm.DesiredPixelWidth.Value);
        Assert.Equal("1080", resizeVm.DesiredPixelHeight.Value);
        Assert.True(resizeVm.IsKeepingAspectRatio.Value);
    }

    [Fact]
    public void SelectedConversionIndex_UpdatesQualitySlider_WhenQualityFormatSelected()
    {
        var (mainVm, resizeVm) = CreateViewModels();
        var fileInfo = new FileInfo("test.png");
        var model = new ImageModel
        {
            PixelWidth = 100,
            PixelHeight = 100,
            FileInfo = fileInfo
        };
        mainVm.WindowTabs.ActiveTab.Value.Model = model;
        mainVm.WindowTabs.ActiveTab.Value.FileInfo.Value = fileInfo;
        _frameProvider.Tick();

        // Select PNG conversion (index 1) which is considered a quality format
        resizeVm.SelectedConversionIndex.Value = 1;
        _frameProvider.Tick();

        Assert.True(resizeVm.IsQualityEnabled.Value);
        Assert.Equal(75, resizeVm.Quality.Value);
        Assert.True(resizeVm.ShowReset.Value);
    }

    [Fact]
    public async Task UpdateOutputFileSizeAsync_ValidImage_UpdatesOutputFileSize()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_resize_{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new MagickImage(MagickColors.Red, 200, 200))
            {
                image.Format = MagickFormat.Png;
                image.Write(tempFile);
            }

            var fileInfo = new FileInfo(tempFile);
            var (mainVm, resizeVm) = CreateViewModels();
            var model = new ImageModel
            {
                PixelWidth = 200,
                PixelHeight = 200,
                FileInfo = fileInfo
            };

            mainVm.WindowTabs.ActiveTab.Value.Model = model;
            mainVm.WindowTabs.ActiveTab.Value.FileInfo.Value = fileInfo;
            _frameProvider.Tick();

            await resizeVm.UpdateOutputFileSizeAsync();

            Assert.False(string.IsNullOrEmpty(resizeVm.OutputFileSize.Value));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task UpdateOutputFileSizeAsync_ResizedDimensions_UpdatesOutputFileSize()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_resize_{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new MagickImage(MagickColors.Blue, 500, 500))
            {
                image.Format = MagickFormat.Png;
                image.Write(tempFile);
            }

            var fileInfo = new FileInfo(tempFile);
            var (mainVm, resizeVm) = CreateViewModels();
            var model = new ImageModel
            {
                PixelWidth = 500,
                PixelHeight = 500,
                FileInfo = fileInfo
            };

            mainVm.WindowTabs.ActiveTab.Value.Model = model;
            mainVm.WindowTabs.ActiveTab.Value.FileInfo.Value = fileInfo;
            _frameProvider.Tick();

            resizeVm.DesiredPixelWidth.Value = "50";
            resizeVm.DesiredPixelHeight.Value = "50";
            _frameProvider.Tick();

            await resizeVm.UpdateOutputFileSizeAsync();

            Assert.False(string.IsNullOrEmpty(resizeVm.OutputFileSize.Value));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}