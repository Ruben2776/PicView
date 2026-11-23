using ImageMagick;
using PicView.Core.ImageTransformations;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Saving;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using R3;

namespace PicView.Tests.Saving;

[Collection("Sequential")]
public class ShownFileWriterTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewTests_" + Guid.NewGuid());
    private readonly SharedImageCache _cache = new(file => new ImageModel { FileInfo = file });
    private readonly List<IDisposable> _disposables = [];

    public ShownFileWriterTests()
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
    public async Task Copy_TurnedFile_WritesItTurned()
    {
        var source = WriteImage("source.png", MagickColors.Red, 2, 1);
        var tab = CreateTab(source);
        tab.Orientation.TurnRight();
        var destination = Path.Combine(_testDirectory, "other.png");

        var result = await new ShownFileWriter(() => null).WriteAsync(tab, CopyTo(destination));

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.Equal(destination, result.WrittenPath);
        Assert.True(result.TurnApplied);
        AssertSize(destination, 1, 2);
        AssertSize(source, 2, 1);
        Assert.Equal(1, tab.Orientation.Current.CurrentValue.QuarterTurns);
    }

    [Fact]
    public async Task Copy_KeepsTurnAndEffects()
    {
        var source = WriteImage("source.png", MagickColors.Red, 2, 1);
        var tab = CreateTab(source);
        tab.Orientation.TurnRight();
        var effects = await RenderBlueAsync(tab);
        var destination = Path.Combine(_testDirectory, "other.png");

        var result = await new ShownFileWriter(() => effects).WriteAsync(tab, CopyTo(destination));

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.True(result.EffectsApplied);
        Assert.Equal(5, effects.Sharpen.Value);
        Assert.Equal(1, tab.Orientation.Current.CurrentValue.QuarterTurns);
        AssertSize(destination, 1, 2);
        Assert.Equal(Rgb(MagickColors.Blue), PixelAt(destination));
    }

    [Fact]
    public async Task Unedited_IgnoresEffects()
    {
        var source = WriteImage("source.png", MagickColors.Red, 2, 1);
        var tab = CreateTab(source);
        var effects = await RenderBlueAsync(tab);
        var destination = Path.Combine(_testDirectory, "other.png");

        var result = await new ShownFileWriter(() => effects).WriteAsync(tab,
            new ShownFileWrite(WriteSource.Unedited, default, new WriteDestination.Copy(destination)));

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.False(result.EffectsApplied);
        Assert.Equal(Rgb(MagickColors.Red), PixelAt(destination));
    }

    [Fact]
    public async Task SingleImage_ReadsThroughTheSeam()
    {
        var tab = CreateTab(null);
        var writer = new ShownFileWriter(() => null) { SingleImagePixels = new FixedPixels(3, 2) };
        var destination = Path.Combine(_testDirectory, "single.png");

        var result = await writer.WriteAsync(tab, CopyTo(destination));

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        AssertSize(destination, 3, 2);
    }

    [Fact]
    public async Task SingleImage_WithoutSeam_NothingToWrite()
    {
        var tab = CreateTab(null);
        var destination = Path.Combine(_testDirectory, "single.png");

        var result = await new ShownFileWriter(() => null).WriteAsync(tab, CopyTo(destination));

        Assert.Equal(WriteOutcome.NothingToWrite, result.Outcome);
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public async Task Resize_WidthOnly_KeepsAspect()
    {
        var source = WriteImage("source.png", MagickColors.Red, 4, 2);
        var tab = CreateTab(source);
        var destination = Path.Combine(_testDirectory, "other.png");

        var result = await new ShownFileWriter(() => null).WriteAsync(tab,
            new ShownFileWrite(WriteSource.SaveSource, new WriteEdits(Width: 2), new WriteDestination.Copy(destination)));

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        AssertSize(destination, 2, 1);
    }

    [Fact]
    public async Task Cut_UsesCropRender()
    {
        var source = WriteImage("source.png", MagickColors.Red, 4, 4);
        var tab = CreateTab(source);
        var destination = Path.Combine(_testDirectory, "other.png");

        var result = await new ShownFileWriter(() => null).WriteAsync(tab,
            new ShownFileWrite(WriteSource.SaveSource, new WriteEdits(Cut: new PixelArea(0, 0, 2, 1)),
                new WriteDestination.Copy(destination)));

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        AssertSize(destination, 2, 1);
    }

    private static ShownFileWrite CopyTo(string destination) =>
        new(WriteSource.SaveSource, default, new WriteDestination.Copy(destination));

    // A tab showing the file at path, or no file when it is null
    private TabViewModel CreateTab(string? path)
    {
        var services = TestTabServices.For(_cache, platform: new RecordingPlatform());
        var tab = new TabViewModel(new MainWindowViewModel(null!, null!, null!, null!), services);
        _disposables.Add(tab);
        if (path is not null)
        {
            tab.FileInfo.Value = new FileInfo(path);
        }

        return tab;
    }

    // An effects view model whose Effects Render of the tab's file is blue, rendered for a non-default sharpen
    private async Task<EffectsViewModel> RenderBlueAsync(TabViewModel tab)
    {
        var effects = new EffectsViewModel(tab.FileInfo, (file, _, _) =>
        {
            using var shown = new MagickImage(file);
            return Task.FromResult<MagickImage?>(new MagickImage(MagickColors.Blue, shown.Width, shown.Height));
        });
        _disposables.Add(effects);
        var published = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = effects.Rendered.Subscribe(_ => published.TrySetResult());

        effects.Sharpen.Value = 5;

        await published.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        return effects;
    }

    private string WriteImage(string name, MagickColor color, uint width, uint height)
    {
        var path = Path.Combine(_testDirectory, name);
        using var image = new MagickImage(color, width, height);
        image.Write(path);
        return path;
    }

    private static void AssertSize(string path, uint width, uint height)
    {
        using var image = new MagickImage(path);
        Assert.Equal((width, height), (image.Width, image.Height));
    }

    // The red, green and blue of the top-left pixel
    private static (byte R, byte G, byte B) PixelAt(string path)
    {
        using var image = new MagickImage(path);
        using var pixels = image.GetPixels();
        var color = pixels.GetPixel(0, 0).ToColor()!;
        return (color.R, color.G, color.B);
    }

    private static (byte R, byte G, byte B) Rgb(MagickColor color) => (color.R, color.G, color.B);

    private sealed class FixedPixels(uint width, uint height) : ISingleImagePixels
    {
        public MagickImage? Read(TabViewModel tab) => new(MagickColors.Green, width, height);
    }
}
