using ImageMagick;
using PicView.Core.FileHandling;
using PicView.Core.ImageTransformations;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Saving;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using R3;
using Orientation = PicView.Core.ImageTransformations.Orientation;

namespace PicView.Tests.Saving;

[Collection("Sequential")]
public class ShownFileWriterTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewTests_" + Guid.NewGuid());
    private readonly SharedImageCache _cache;
    private readonly List<IDisposable> _disposables = [];
    private readonly RecordingPlatform _platform = new();

    // How many times the cache decoded a model
    private int _decodes;

    public ShownFileWriterTests()
    {
        _cache = new SharedImageCache(file =>
        {
            Interlocked.Increment(ref _decodes);
            return new ImageModel { FileInfo = file };
        });
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

    [Fact]
    public async Task OverItself_RecyclesACopyBeforeWriting()
    {
        var source = WriteImage("source.png", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        tab.Orientation.TurnRight();

        var result = await new ShownFileWriter(() => null).WriteAsync(tab, OverItself());

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.Equal(source, result.WrittenPath);
        var recycled = Assert.Single(_platform.Deleted);
        Assert.NotEqual(source, recycled.Path);
        using (var old = new MagickImage(recycled.Bytes))
        {
            Assert.Equal((2u, 1u), (old.Width, old.Height));
        }

        AssertSize(source, 1, 2);
        Assert.Equal(Orientation.Upright, tab.Orientation.Current.CurrentValue);
    }

    [Fact]
    public async Task OverItself_NoRecycleBin_StillWrites()
    {
        _platform.HasRecycleBin = false;
        var source = WriteImage("source.png", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        tab.Orientation.TurnRight();

        var result = await new ShownFileWriter(() => null).WriteAsync(tab, OverItself());

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.Empty(_platform.Deleted);
        AssertSize(source, 1, 2);
    }

    [Fact]
    public async Task OverItself_EffectsApplied_ResetsEffects()
    {
        var source = WriteImage("source.png", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        var effects = await RenderBlueAsync(tab);

        var result = await new ShownFileWriter(() => effects).WriteAsync(tab, OverItself());

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.True(result.EffectsApplied);
        Assert.Equal(0, effects.Sharpen.Value);
        Assert.Equal(Rgb(MagickColors.Blue), PixelAt(source));
    }

    [Fact]
    public async Task OverItself_Unchanged_KeepsTheShownModel()
    {
        var source = WriteImage("source.png", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        var before = tab.Model;
        var decodes = _decodes;

        var result = await new ShownFileWriter(() => null).WriteAsync(tab, OverItself());

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.Same(before, tab.Model);
        Assert.Equal(decodes, _decodes);
    }

    [Fact]
    public async Task OverItself_ArchivePage_ShowsItAgain()
    {
        var page = TempFileManager.GetNewTempFilePath("page.png");
        try
        {
            using (var image = new MagickImage(MagickColors.Red, 4, 2))
            {
                image.Write(page);
            }

            var tab = await LoadTabAsync(page);
            var before = tab.Model;

            var result = await new ShownFileWriter(() => null).WriteAsync(tab,
                new ShownFileWrite(WriteSource.SaveSource, new WriteEdits(Width: 2),
                    new WriteDestination.OverItself()));

            Assert.Equal(WriteOutcome.Written, result.Outcome);
            Assert.NotSame(before, tab.Model);
            Assert.Equal(page, tab.Model.FileInfo?.FullName);
            AssertSize(page, 2, 1);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(page)!, true);
        }
    }

    [Fact]
    public async Task OverItself_LosslessOptimize_KeepsThePixelsAndRecyclesACopy()
    {
        var source = WriteImage("source.png", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        tab.Orientation.TurnRight();

        var result = await new ShownFileWriter(() => null).WriteAsync(tab,
            new ShownFileWrite(WriteSource.Unedited, new WriteEdits(LosslessOptimize: true),
                new WriteDestination.OverItself()));

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.False(result.TurnApplied);
        Assert.Single(_platform.Deleted);
        AssertSize(source, 2, 1);
        Assert.Equal(Rgb(MagickColors.Red), PixelAt(source));
    }

    [Fact]
    public async Task Optimize_KeepsTheTurnAndForgetsTheThumbnail()
    {
        var wasReset = Settings.Zoom.ResetZoomOnChange;
        Settings.Zoom.ResetZoomOnChange = false;
        try
        {
            var source = WriteImage("source.png", MagickColors.Red, 2, 1);
            var services = TestTabServices.For(_cache, platform: _platform);
            var tab = new TabViewModel(new MainWindowViewModel(null!, null!, null!, null!), services);
            _disposables.Add(tab);
            await tab.Load.LoadAsync(source);
            tab.Orientation.TurnRight();
            services.Thumbnails.Keep(tab.Id, source, new object(), 100);

            var result = await new ShownFileWriter(() => null).WriteAsync(tab,
                new ShownFileWrite(WriteSource.Unedited, new WriteEdits(LosslessOptimize: true),
                    new WriteDestination.OverItself()));

            Assert.Equal(WriteOutcome.Written, result.Outcome);
            // The file was not turned, so the tab still shows it turned
            Assert.Equal(1, tab.Orientation.Current.CurrentValue.QuarterTurns);
            Assert.False(services.Thumbnails.TryGetKept(new FileInfo(source), out _, out _));
            AssertSize(source, 2, 1);
        }
        finally
        {
            Settings.Zoom.ResetZoomOnChange = wasReset;
        }
    }

    [Fact]
    public async Task OverItself_EffectsApplied_ResetsThemOnTheUiThread()
    {
        var source = WriteImage("source.png", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        var effects = await RenderBlueAsync(tab);
        var sharpenOnUiThread = -1.0;
        var writer = new ShownFileWriter(() => effects)
        {
            UiThread = action =>
            {
                action();
                sharpenOnUiThread = effects.Sharpen.Value;
                return Task.CompletedTask;
            }
        };

        await writer.WriteAsync(tab, OverItself());

        Assert.Equal(0, sharpenOnUiThread);
    }

    [Fact]
    public async Task OverItself_LosslessOptimizeUnsupported_NothingToWrite()
    {
        var source = WriteImage("source.bmp", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        var before = tab.Model;

        var result = await new ShownFileWriter(() => null).WriteAsync(tab,
            new ShownFileWrite(WriteSource.Unedited, new WriteEdits(LosslessOptimize: true),
                new WriteDestination.OverItself()));

        Assert.Equal(WriteOutcome.NothingToWrite, result.Outcome);
        Assert.Empty(_platform.Deleted);
        Assert.Same(before, tab.Model);
    }

    [Fact]
    public async Task Copy_OntoTheShownFile_ShowsItAgain()
    {
        var source = WriteImage("source.png", MagickColors.Red, 4, 2);
        var tab = await LoadTabAsync(source);
        var before = tab.Model;

        var result = await new ShownFileWriter(() => null).WriteAsync(tab,
            new ShownFileWrite(WriteSource.SaveSource, new WriteEdits(Width: 2),
                new WriteDestination.Copy(source.ToUpperInvariant())));

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.NotSame(before, tab.Model);
        Assert.Single(_platform.Deleted);
        AssertSize(source, 2, 1);
    }

    [Fact]
    public async Task OverItself_NoFile_NothingToWrite()
    {
        var tab = CreateTab(null);

        var result = await new ShownFileWriter(() => null).WriteAsync(tab, OverItself());

        Assert.Equal(WriteOutcome.NothingToWrite, result.Outcome);
    }

    [Fact]
    public async Task Copy_OntoExistingFile_RecyclesItFirst()
    {
        var source = WriteImage("source.png", MagickColors.Red, 2, 1);
        var destination = WriteImage("other.png", MagickColors.Green, 3, 3);
        var old = await File.ReadAllBytesAsync(destination, TestContext.Current.CancellationToken);
        var tab = CreateTab(source);

        var result = await new ShownFileWriter(() => null).WriteAsync(tab, CopyTo(destination));

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        var recycled = Assert.Single(_platform.Deleted);
        Assert.Equal(old, recycled.Bytes);
        AssertSize(destination, 2, 1);
    }

    [Fact]
    public async Task Copy_ToNewPath_RecyclesNothing()
    {
        var source = WriteImage("source.png", MagickColors.Red, 2, 1);
        var tab = CreateTab(source);

        await new ShownFileWriter(() => null).WriteAsync(tab, CopyTo(Path.Combine(_testDirectory, "other.png")));

        Assert.Empty(_platform.Deleted);
    }

    [Fact]
    public async Task Replace_NameTaken_WritesAndDeletesNothing()
    {
        var source = WriteImage("a.png", MagickColors.Red, 2, 1);
        var taken = WriteImage("a.jpg", MagickColors.Green, 3, 3);
        var old = await File.ReadAllBytesAsync(taken, TestContext.Current.CancellationToken);
        var tab = await LoadTabAsync(source);

        var result = await new ShownFileWriter(() => null).WriteAsync(tab, ReplaceWith(taken));

        Assert.Equal(WriteOutcome.NameTaken, result.Outcome);
        Assert.Equal(old, await File.ReadAllBytesAsync(taken, TestContext.Current.CancellationToken));
        Assert.True(File.Exists(source));
        Assert.Empty(_platform.Deleted);
    }

    [Fact]
    public async Task Replace_ShowsTheNewFileAndRecyclesTheOriginal()
    {
        var wasReset = Settings.Zoom.ResetZoomOnChange;
        Settings.Zoom.ResetZoomOnChange = false;
        try
        {
            var source = WriteImage("a.png", MagickColors.Red, 2, 1);
            var tab = await LoadTabAsync(source);
            tab.Orientation.TurnRight();
            var newPath = Path.Combine(_testDirectory, "a.jpg");

            var result = await new ShownFileWriter(() => null).WriteAsync(tab, ReplaceWith(newPath));

            Assert.Equal(WriteOutcome.Written, result.Outcome);
            Assert.Equal(newPath, tab.Model.FileInfo?.FullName);
            Assert.Equal(Orientation.Upright, tab.Orientation.Current.CurrentValue);
            Assert.False(File.Exists(source));
            Assert.Contains(_platform.Deleted, d => d.Path.EndsWith("a.png", StringComparison.Ordinal));
            AssertSize(newPath, 1, 2);
        }
        finally
        {
            Settings.Zoom.ResetZoomOnChange = wasReset;
        }
    }

    [Fact]
    public async Task Replace_SamePathOtherCase_DoesNotDeleteTheNewFile()
    {
        var source = WriteImage("a.png", MagickColors.Red, 2, 1);
        var tab = await LoadTabAsync(source);
        var newPath = Path.Combine(_testDirectory, "a.PNG");

        var result = await new ShownFileWriter(() => null).WriteAsync(tab, ReplaceWith(newPath));

        Assert.Equal(WriteOutcome.Written, result.Outcome);
        Assert.True(File.Exists(newPath));
        Assert.Single(_platform.Deleted);
    }

    private static ShownFileWrite ReplaceWith(string newPath) =>
        new(WriteSource.SaveSource, default, new WriteDestination.Replace(newPath));

    private static ShownFileWrite OverItself() =>
        new(WriteSource.SaveSource, default, new WriteDestination.OverItself());

    // A tab that loaded the file at path, so it shows it with an iterator over its folder
    private async Task<TabViewModel> LoadTabAsync(string path)
    {
        var tab = CreateTab(null);
        await tab.Load.LoadAsync(path);
        return tab;
    }

    private static ShownFileWrite CopyTo(string destination) =>
        new(WriteSource.SaveSource, default, new WriteDestination.Copy(destination));

    // A tab showing the file at path, or no file when it is null
    private TabViewModel CreateTab(string? path)
    {
        var services = TestTabServices.For(_cache, platform: _platform);
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
