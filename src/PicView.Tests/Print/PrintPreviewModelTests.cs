using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Tests.Print;

public class PrintPreviewModelTests
{
    public PrintPreviewModelTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [Fact]
    public void Constructor_ShouldInitialize_ScaleModes()
    {
        var sut = new PrintPreviewViewModel();
        
        Assert.NotNull(sut.ScaleModes.Value);
        Assert.Equal(4, sut.ScaleModes.Value.Count());
        Assert.Contains(TranslationManager.Translation!.Fit, sut.ScaleModes.Value);
        Assert.Contains(TranslationManager.Translation!.Fill, sut.ScaleModes.Value);
        Assert.Contains(TranslationManager.Translation!.Stretch, sut.ScaleModes.Value);
        Assert.Contains(TranslationManager.Translation!.Center, sut.ScaleModes.Value);
    }

    [Fact]
    public void Constructor_ShouldInitialize_Orientations()
    {
        var sut = new PrintPreviewViewModel();
        
        Assert.NotNull(sut.Orientations.Value);
        Assert.Equal(2, sut.Orientations.Value.Count());
        Assert.Contains(TranslationManager.Translation!.Portrait, sut.Orientations.Value);
        Assert.Contains(TranslationManager.Translation!.Landscape, sut.Orientations.Value);
    }

    [Fact]
    public void Constructor_ShouldInitialize_ColorModes()
    {
        var sut = new PrintPreviewViewModel();
        
        Assert.NotNull(sut.ColorModes.Value);
        Assert.Equal(3, sut.ColorModes.Value.Count());
        Assert.Contains(TranslationManager.Translation!.Auto, sut.ColorModes.Value);
        Assert.Contains(TranslationManager.Translation!.Color, sut.ColorModes.Value);
        Assert.Contains(TranslationManager.Translation!.BlackAndWhite, sut.ColorModes.Value);
    }

    [Fact]
    public void Constructor_ShouldInitialize_DefaultValues()
    {
        var sut = new PrintPreviewViewModel();
        
        Assert.Equal(1.0, sut.Zoom.Value);
        Assert.True(sut.IsProcessing.Value);
        Assert.Equal(1.0, sut.Opacity.Value);
        Assert.NotNull(sut.Disposables);
        Assert.Empty(sut.Printers.Value ?? []);
        Assert.Empty(sut.PaperSizes.Value ?? []);
    }

    [Fact]
    public void Dispose_ShouldDispose_Disposables()
    {
        var sut = new PrintPreviewViewModel();
        
        var exception = Record.Exception(() => sut.Dispose());
        
        Assert.Null(exception);
        Assert.True(sut.Disposables.IsDisposed);
    }
}