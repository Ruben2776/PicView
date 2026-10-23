using ImageMagick;
using PicView.Core.BatchResize;
using PicView.Core.Localization;
using PicView.Core.Models;

namespace PicView.Tests.Resizing;

[Collection("Sequential")]
public class BatchResizerTests : IDisposable
{
    private readonly string _source;
    private readonly string _output;

    public BatchResizerTests()
    {
        SetDefaults();
        TranslationManager.Init();
        var root = Path.Combine(Path.GetTempPath(), $"BatchResizerTests_{Guid.NewGuid():N}");
        _source = Path.Combine(root, "source");
        _output = Path.Combine(root, "output");
        Directory.CreateDirectory(_source);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path.GetDirectoryName(_source)!, true);
        }
        catch (IOException)
        {
            // A file still held open is left for the OS to clean up
        }
    }

    [Fact]
    public async Task RunAsync_WithoutResizeOrConversion_WritesEachFileAndReportsOneEntryEach()
    {
        var files = new[] { CreateImage("a.png"), CreateImage("b.jpg") };
        var entries = new List<BatchLogEntry>();

        await BatchResizer.RunAsync(Job(files), Collect(entries), TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(_output, "a.png")));
        Assert.True(File.Exists(Path.Combine(_output, "b.jpg")));
        Assert.Equal(2, entries.Count);
        Assert.All(entries, entry => Assert.False(entry.IsThumbnail));
    }

    [Theory]
    [InlineData(ConversionTarget.Png, ".png", MagickFormat.Png)]
    [InlineData(ConversionTarget.Jpg, ".jpg", MagickFormat.Jpeg)]
    [InlineData(ConversionTarget.Webp, ".webp", MagickFormat.WebP)]
    public async Task RunAsync_WithConversion_WritesTheTargetFormatUnderItsExtension(ConversionTarget target,
        string extension, MagickFormat format)
    {
        var file = CreateImage("photo.bmp");

        await BatchResizer.RunAsync(Job([file]) with { Conversion = target }, _ => { },
            TestContext.Current.CancellationToken);

        using var written = new MagickImage(Path.Combine(_output, "photo" + extension));
        Assert.Equal(format, written.Format);
    }

    [Theory]
    [InlineData(ResizeMode.None, 0u, 0u, 100, true, 100u, 50u)]
    [InlineData(ResizeMode.Percentage, 0u, 0u, 50, true, 50u, 25u)]
    [InlineData(ResizeMode.Width, 40u, 0u, 100, true, 40u, 20u)]
    [InlineData(ResizeMode.Height, 0u, 10u, 100, true, 20u, 10u)]
    [InlineData(ResizeMode.WidthAndHeight, 40u, 40u, 100, true, 40u, 20u)]
    [InlineData(ResizeMode.WidthAndHeight, 40u, 40u, 100, false, 40u, 40u)]
    public async Task RunAsync_WithResize_WritesTheResizedImage(ResizeMode mode, uint width, uint height,
        double percentage, bool keepAspectRatio, uint expectedWidth, uint expectedHeight)
    {
        var file = CreateImage("photo.png", 100, 50);
        var job = Job([file]) with
        {
            Resize = mode,
            Width = width,
            Height = height,
            Percentage = percentage,
            KeepAspectRatio = keepAspectRatio
        };

        await BatchResizer.RunAsync(job, _ => { }, TestContext.Current.CancellationToken);

        using var written = new MagickImage(Path.Combine(_output, "photo.png"));
        Assert.Equal((expectedWidth, expectedHeight), (written.Width, written.Height));
    }

    [Fact]
    public async Task RunAsync_WithQuality_WritesAJpegAtThatQuality()
    {
        var file = CreateImage("photo.png");

        await BatchResizer.RunAsync(Job([file]) with { Conversion = ConversionTarget.Jpg, Quality = 40 }, _ => { },
            TestContext.Current.CancellationToken);

        using var written = new MagickImage(Path.Combine(_output, "photo.jpg"));
        Assert.Equal(40u, written.Quality);
    }

    [Fact]
    public async Task RunAsync_WithLossyCompression_WritesASmallerFileThanWithout()
    {
        var file = CreateNoisyJpeg("photo.jpg");
        var uncompressed = Path.Combine(_output, "none");
        var compressed = Path.Combine(_output, "lossy");

        await BatchResizer.RunAsync(Job([file]) with { OutputFolder = uncompressed }, _ => { },
            TestContext.Current.CancellationToken);
        await BatchResizer.RunAsync(Job([file]) with { OutputFolder = compressed, Compression = CompressionMode.Lossy },
            _ => { }, TestContext.Current.CancellationToken);

        Assert.True(new FileInfo(Path.Combine(compressed, "photo.jpg")).Length <
                    new FileInfo(Path.Combine(uncompressed, "photo.jpg")).Length);
    }

    [Fact]
    public async Task RunAsync_WithThumbnails_ResizesEachFromTheSourceInTheJobsFormat()
    {
        var file = CreateImage("photo.png", 200, 100);
        var entries = new List<BatchLogEntry>();
        var job = Job([file]) with
        {
            Conversion = ConversionTarget.Webp,
            Resize = ResizeMode.Width,
            Width = 40,
            Thumbnails =
            [
                new BatchThumbnail("half", ThumbnailSize.Percentage, 50),
                new BatchThumbnail("quarter", ThumbnailSize.Percentage, 25),
                new BatchThumbnail("tall", ThumbnailSize.Height, 20)
            ]
        };

        await BatchResizer.RunAsync(job, Collect(entries), TestContext.Current.CancellationToken);

        Assert.Equal((100u, 50u), SizeOf(Path.Combine(_output, "half", "photo.webp")));
        Assert.Equal((50u, 25u), SizeOf(Path.Combine(_output, "quarter", "photo.webp")));
        Assert.Equal((40u, 20u), SizeOf(Path.Combine(_output, "tall", "photo.webp")));
        Assert.Equal(3, entries.Count(entry => entry.IsThumbnail));
        Assert.Single(entries, entry => !entry.IsThumbnail);
    }

    [Fact]
    public async Task RunAsync_IntoTheSourceFolder_BacksUpEachFileBeforeOverwritingIt()
    {
        var file = CreateImage("photo.png", 100, 50);
        var untouched = CreateImage("other.png", 100, 50);

        await BatchResizer.RunAsync(Job([file]) with
        {
            OutputFolder = _source,
            Resize = ResizeMode.Width,
            Width = 10
        }, _ => { }, TestContext.Current.CancellationToken);

        var backup = Assert.Single(Directory.GetDirectories(_source));
        Assert.Equal((100u, 50u), SizeOf(Path.Combine(backup, "photo.png"), MagickFormat.Png));
        Assert.False(File.Exists(Path.Combine(backup, untouched.Name)));
        Assert.Equal((10u, 5u), SizeOf(file.FullName, MagickFormat.Png));
    }

    [Fact]
    public async Task RunAsync_IntoAnEmptyFolder_MakesNoBackup()
    {
        var file = CreateImage("photo.png");

        await BatchResizer.RunAsync(Job([file]), _ => { }, TestContext.Current.CancellationToken);

        Assert.Empty(Directory.GetDirectories(_output));
    }

    [Fact]
    public async Task RunAsync_WithAFileThatCannotBeRead_LogsItAndWritesTheOthers()
    {
        var broken = Path.Combine(_source, "broken.png");
        await File.WriteAllTextAsync(broken, "not an image", TestContext.Current.CancellationToken);
        var file = CreateImage("photo.png");
        var entries = new List<BatchLogEntry>();

        await BatchResizer.RunAsync(Job([new FileInfo(broken), file]), Collect(entries),
            TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(_output, "photo.png")));
        Assert.Equal(2, entries.Count);
        var failed = Assert.Single(entries, entry => entry.NewSize == "--");
        Assert.Contains("broken.png", failed.FileName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_Cancelled_ThrowsAndWritesNothing()
    {
        var file = CreateImage("photo.png");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            BatchResizer.RunAsync(Job([file]), _ => { }, cancellation.Token));

        Assert.False(File.Exists(Path.Combine(_output, "photo.png")));
    }

    private static (uint Width, uint Height) SizeOf(string path, MagickFormat format = MagickFormat.WebP)
    {
        using var image = new MagickImage(path);
        Assert.Equal(format, image.Format);
        return (image.Width, image.Height);
    }

    private BatchResizeJob Job(IEnumerable<FileInfo> files) => new()
    {
        Files = [.. files],
        OutputFolder = _output
    };

    private static Action<BatchLogEntry> Collect(List<BatchLogEntry> entries) => entry =>
    {
        lock (entries)
        {
            entries.Add(entry);
        }
    };

    private FileInfo CreateNoisyJpeg(string name)
    {
        var path = Path.Combine(_source, name);
        using var image = new MagickImage(MagickColors.White, 400, 300);
        image.AddNoise(NoiseType.Random);
        image.Quality = 95;
        image.Write(path);
        return new FileInfo(path);
    }

    private FileInfo CreateImage(string name, uint width = 100, uint height = 50)
    {
        var path = Path.Combine(_source, name);
        using var image = new MagickImage(MagickColors.White, width, height);
        image.Write(path);
        return new FileInfo(path);
    }
}
