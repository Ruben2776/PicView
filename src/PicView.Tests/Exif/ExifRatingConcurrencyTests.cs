using ImageMagick;
using PicView.Core.Exif;

namespace PicView.Tests.Exif;

[Collection("Sequential")]
public class ExifRatingConcurrencyTests : IDisposable
{
    private readonly string _directory =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"picview-rating-race-{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, true);
        }
        catch (IOException)
        {
            // Cleaning up the temp directory is best effort
        }
    }

    [Fact]
    public async Task SetExifRatingAsync_ConcurrentWritesAndReaders_LeavesImageIntact()
    {
        var fileInfo = CreateLargeJpeg();
        var expectedSize = (Width: 1600u, Height: 2400u);
        var ct = TestContext.Current.CancellationToken;

        using var stop = new CancellationTokenSource();
        var readers = Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
        {
            // What the gallery and the file watcher do when the file changes
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    using var image = new MagickImage();
                    image.Ping(fileInfo.FullName);
                }
                catch (Exception)
                {
                    // Locked by the writer
                }
                await Task.Delay(5, CancellationToken.None);
            }
        }, ct)).ToArray();

        var writes = Enumerable.Range(0, 6)
            .Select(i => Task.Run(() => ExifWriter.SetExifRatingAsync(new FileInfo(fileInfo.FullName), (ushort)(i % 6)), ct))
            .ToArray();
        var results = await Task.WhenAll(writes);
        await stop.CancelAsync();
        await Task.WhenAll(readers);

        var warnings = new List<string>();
        using var final = new MagickImage();
        final.Warning += (_, e) => warnings.Add(e.Message);
        final.Read(fileInfo.FullName);

        Assert.All(results, Assert.True);
        Assert.Empty(warnings);
        Assert.Equal(expectedSize.Width, final.Width);
        Assert.Equal(expectedSize.Height, final.Height);
        Assert.NotNull(final.GetExifProfile()?.GetValue(ExifTag.Rating));
    }

    private FileInfo CreateLargeJpeg()
    {
        var path = Path.Combine(_directory, "large.jpg");
        using var image = new MagickImage("plasma:", 1600, 2400);
        image.Format = MagickFormat.Jpeg;
        image.Write(path);
        return new FileInfo(path);
    }
}
