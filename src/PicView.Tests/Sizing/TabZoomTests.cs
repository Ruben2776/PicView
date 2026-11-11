using ImageMagick;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using R3;

namespace PicView.Tests.Sizing;

/// <summary>
/// A tab derives its Zoom Level from the Fit Scale and the Zoom Factor (ADR-0016): the level notifies only when the
/// percent changes, the tab resets the factor when it publishes an image while zoom resets on image change, and a
/// wheel step allocates nothing on its way to the Titles' mark.
/// </summary>
[Collection("Sequential")]
public class TabZoomTests
{
    private readonly TabViewModel _tab;

    public TabZoomTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.LoadLanguage("en");
        _tab = new TabViewModel(null!,
            TestTabServices.For(new SharedImageCache(f => ValueTask.FromResult(Model(f.Name))!), new ThumbnailCache(),
                new MockThumbnailLoader()));
    }

    private static ImageModel Model(string name) => new()
    {
        FileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "PicViewTabZoomTests", name)),
        Image = new object(),
        PixelWidth = 1,
        PixelHeight = 1
    };

    /// <summary>Gives the tab an iterator browsing <paramref name="names"/>, at the first.</summary>
    private void Browse(params string[] names)
    {
        var files = names.Select(n => Model(n).FileInfo!).ToArray();
        _tab.Initialize();
        _tab.ImageIterator!.Initialize(files, 0);
    }

    [Fact]
    public void Level_FitScaleAndFactor_IsTheirProductInPercent()
    {
        Assert.Equal(100, _tab.Zoom.Level.CurrentValue);
        _tab.Zoom.SetFitScale(0.25);
        Assert.Equal(25, _tab.Zoom.Level.CurrentValue);
        _tab.Zoom.SetFactor(2);
        Assert.Equal(50, _tab.Zoom.Level.CurrentValue);
        _tab.Zoom.SetFitScale(0.5);
        Assert.Equal(100, _tab.Zoom.Level.CurrentValue);
    }

    [Fact]
    public void Level_Rounds_AsConvertToInt32()
    {
        _tab.Zoom.SetFitScale(0.3333);
        Assert.Equal(Convert.ToInt32(0.3333 * 100), _tab.Zoom.Level.CurrentValue);
    }

    [Fact]
    public void SetFactor_SamePercent_DoesNotNotify()
    {
        var pushes = 0;
        using var subscription = _tab.Zoom.Level.Skip(1).Subscribe(_ => pushes++);

        _tab.Zoom.SetFactor(1.001);
        Assert.Equal(0, pushes);
        _tab.Zoom.SetFactor(1.5);
        Assert.Equal(1, pushes);
    }

    [Fact]
    public void Display_WithResetOnChange_ResetsTheFactor()
    {
        Settings.Zoom.ResetZoomOnChange = true;
        _tab.Zoom.SetFactor(2);

        _tab.Display(Model("a.jpg"));

        Assert.Equal(1, _tab.Zoom.Factor);
        Assert.Equal(100, _tab.Zoom.Level.CurrentValue);
    }

    [Fact]
    public void Display_WithoutResetOnChange_KeepsTheFactor()
    {
        Settings.Zoom.ResetZoomOnChange = false;
        _tab.Zoom.SetFactor(2);

        _tab.Display(Model("a.jpg"));

        Assert.Equal(2, _tab.Zoom.Factor);
    }

    [Fact]
    public void UpdateShownFile_KeepsTheFactor()
    {
        Settings.Zoom.ResetZoomOnChange = true;
        var model = Model("a.jpg");
        _tab.Display(model);
        _tab.Zoom.SetFactor(2);

        Assert.True(_tab.UpdateShownFile(model.FileInfo!.FullName, Model("b.jpg").FileInfo!));

        Assert.Equal(2, _tab.Zoom.Factor);
    }

#if DEBUG
    [Fact]
    public void Titles_ZoomThenSameFitImage_BuildOncePerImage()
    {
        Settings.Zoom.ResetZoomOnChange = true;
        Browse("a.jpg", "b.jpg");
        _tab.Display(Model("a.jpg"));
        _ = _tab.Titles.Title;
        Assert.Equal(1, _tab.Titles.BuildCount);

        _tab.Zoom.SetFactor(2);
        _ = _tab.Titles.Title;
        Assert.Equal(2, _tab.Titles.BuildCount);
        Assert.Contains("200", _tab.Titles.Title, StringComparison.Ordinal);

        _tab.Display(Model("b.jpg"));
        _ = _tab.Titles.Title;
        Assert.Equal(3, _tab.Titles.BuildCount);
        Assert.DoesNotContain("%", _tab.Titles.Title, StringComparison.Ordinal);
    }
#endif

    [Fact]
    public void SetFactor_HundredWrites_AllocatesNothing()
    {
        _ = _tab.Titles; // Its subscription to Level is live, as in a window
        _tab.Zoom.SetFactor(1.5);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            _tab.Zoom.SetFactor(i % 2 == 0 ? 2 : 1.5); // Every write changes the percent
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);

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
