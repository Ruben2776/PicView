using PicView.Core.ViewModels;

namespace PicView.Tests;

public class PrintPreviewViewModelTests
{
    public PrintPreviewViewModelTests()
    {
        SetDefaults();
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesWithDefaults()
    {
        // Act
        var viewModel = new PrintPreviewViewModel();

        // Assert
        Assert.NotNull(viewModel.Disposables);
        Assert.Null(viewModel.PrintWindowConfig);
        
        Assert.True(viewModel.IsProcessing.Value);
        Assert.Equal(1.0, viewModel.Zoom.Value);
        Assert.Equal(1.0, viewModel.Opacity.Value);
        Assert.Null(viewModel.GrayCache);
        
        // Initialized lists from TranslationManager
        Assert.NotNull(viewModel.ScaleModes.Value);
        Assert.NotEmpty(viewModel.ScaleModes.Value);
        
        Assert.NotNull(viewModel.Orientations.Value);
        Assert.NotEmpty(viewModel.Orientations.Value);
        
        Assert.NotNull(viewModel.ColorModes.Value);
        Assert.NotEmpty(viewModel.ColorModes.Value);
    }

    [Fact]
    public void Commands_ShouldBeInitialized()
    {
        // Arrange & Act
        var viewModel = new PrintPreviewViewModel();

        // Assert
        Assert.NotNull(viewModel.PrintCommand);
        Assert.NotNull(viewModel.CancelCommand);
    }

    [Fact]
    public void BindableProperties_ShouldBeInitialized()
    {
        // Arrange & Act
        var viewModel = new PrintPreviewViewModel();

        // Assert
        Assert.NotNull(viewModel.Printers);
        Assert.NotNull(viewModel.PaperSizes);
        Assert.NotNull(viewModel.PrintSettings);
        Assert.NotNull(viewModel.PreviewImage);
        Assert.NotNull(viewModel.PageWidth);
        Assert.NotNull(viewModel.PageHeight);
    }

    [Fact]
    public void Dispose_ShouldNotThrowException()
    {
        // Arrange
        var viewModel = new PrintPreviewViewModel();

        // Act
        var exception = Record.Exception(() => viewModel.Dispose());

        // Assert
        Assert.Null(exception);
    }
}
