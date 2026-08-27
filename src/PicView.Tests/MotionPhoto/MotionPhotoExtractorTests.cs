using PicView.Core.FileHandling;
using PicView.Core.MotionPhoto;

namespace PicView.Tests.MotionPhoto;

public class MotionPhotoExtractorTests : IDisposable
{
    private readonly string _tempDirectory = MotionPhotoFixtures.CreateTempDirectory();

    public MotionPhotoExtractorTests()
    {
        TempFileManager.Cleanup();
    }

    public void Dispose()
    {
        MotionPhotoFixtures.DeleteDirectory(_tempDirectory);
        TempFileManager.Cleanup();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void FindFtypStart_ExactExpectedPosition_ReturnsExpected()
    {
        var video = MotionPhotoFixtures.BuildMp4Head(32);
        var window = new byte[512];
        video.CopyTo(window.AsSpan(100));

        var result = MotionPhotoExtractor.FindFtypStart(window, 1000, 1100, 10_000);

        Assert.Equal(1100, result);
    }

    [Fact]
    public void FindFtypStart_TrailerShiftedStart_FindsClosestValidFtyp()
    {
        // Simulates a vendor trailer: the real ftyp box is 32 bytes before the
        // expected start position (the naive "last N bytes" slice lands mid-video).
        var video = MotionPhotoFixtures.BuildMp4Head(32);
        var window = new byte[512];
        video.CopyTo(window.AsSpan(100));

        var result = MotionPhotoExtractor.FindFtypStart(window, 1000, 1132, 10_000);

        Assert.Equal(1100, result);
    }

    [Fact]
    public void FindFtypStart_InvalidBoxSize_IsRejected()
    {
        var window = new byte[512];
        // "ftyp" signature but a box size larger than the remaining file -> invalid.
        window[100] = 0xFF;
        window[101] = 0xFF;
        window[102] = 0xFF;
        window[103] = 0xFF;
        "ftyp"u8.CopyTo(window.AsSpan(104, 4));

        var result = MotionPhotoExtractor.FindFtypStart(window, 1000, 1100, 10_000);

        Assert.Null(result);
    }

    [Fact]
    public void FindFtypStart_NoValidBox_ReturnsNull()
    {
        var window = new byte[512];
        new Random(42).NextBytes(window);

        var result = MotionPhotoExtractor.FindFtypStart(window, 0, 256, 10_000);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExtractEmbedded_StandardLayout_ReturnsVideoStream()
    {
        var video = MotionPhotoFixtures.BuildMp4Head(48);
        var path = Path.Combine(_tempDirectory, "embedded.jpg");
        await File.WriteAllBytesAsync(path, new byte[1024], TestContext.Current.CancellationToken);
        await using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write))
        {
            await stream.WriteAsync(video, TestContext.Current.CancellationToken);
        }

        var fileInfo = new FileInfo(path);
        var info = new MotionPhotoInfo
        {
            Source = MotionPhotoSource.EmbeddedXmp,
            VideoOffset = fileInfo.Length - video.Length,
            VideoLength = video.Length,
        };

        var result = await MotionPhotoExtractor.ExtractAsync(fileInfo, info, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await using (result.ConfigureAwait(false))
        {
            Assert.Equal(video.Length, result.Length);
            var buffer = new byte[result.Length];
            Assert.Equal(buffer.Length, await result.ReadAsync(buffer, TestContext.Current.CancellationToken));
            Assert.Equal(video, buffer);
        }
    }

    [Fact]
    public async Task ExtractEmbedded_TrailerShiftedOffset_CorrectsStartAndExtracts()
    {
        // File layout: [jpeg prefix][ftyp video][32 byte vendor trailer].
        // The XMP length only covers the video, so the expected start is shifted
        // forward by the trailer size and must be corrected backwards to the ftyp box.
        const int trailerSize = 32;
        var video = MotionPhotoFixtures.BuildMp4Head(48);
        var path = Path.Combine(_tempDirectory, "dji.jpg");
        await File.WriteAllBytesAsync(path, new byte[1024], TestContext.Current.CancellationToken);
        await using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write))
        {
            await stream.WriteAsync(video, TestContext.Current.CancellationToken);
            await stream.WriteAsync(new byte[trailerSize], TestContext.Current.CancellationToken);
        }

        var fileInfo = new FileInfo(path);
        var ftypPosition = fileInfo.Length - video.Length - trailerSize;
        var info = new MotionPhotoInfo
        {
            Source = MotionPhotoSource.EmbeddedXmp,
            VideoOffset = fileInfo.Length - video.Length,
            VideoLength = video.Length,
        };

        var result = await MotionPhotoExtractor.ExtractAsync(fileInfo, info, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await using (result.ConfigureAwait(false))
        {
            Assert.Equal(fileInfo.Length - ftypPosition, result.Length);
            var buffer = new byte[8];
            Assert.Equal(8, await result.ReadAsync(buffer, TestContext.Current.CancellationToken));
            Assert.Equal("ftyp"u8.ToArray(), buffer.AsSpan(4, 4).ToArray());
        }
    }

    [Fact]
    public async Task ExtractEmbedded_NoFtypBox_ReturnsNull()
    {
        var path = Path.Combine(_tempDirectory, "corrupt.jpg");
        var bytes = new byte[4096];
        new Random(42).NextBytes(bytes);
        await File.WriteAllBytesAsync(path, bytes, TestContext.Current.CancellationToken);

        var fileInfo = new FileInfo(path);
        var info = new MotionPhotoInfo
        {
            Source = MotionPhotoSource.EmbeddedXmp,
            VideoOffset = 2048,
            VideoLength = 2048,
        };

        var result = await MotionPhotoExtractor.ExtractAsync(fileInfo, info, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExtractEmbedded_OffsetOutOfRange_ReturnsNull()
    {
        var path = Path.Combine(_tempDirectory, "small.jpg");
        await File.WriteAllBytesAsync(path, new byte[128], TestContext.Current.CancellationToken);

        var info = new MotionPhotoInfo
        {
            Source = MotionPhotoSource.EmbeddedXmp,
            VideoOffset = 999_999,
            VideoLength = 100,
        };

        var result = await MotionPhotoExtractor.ExtractAsync(new FileInfo(path), info, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExtractAsync_Sidecar_ReturnsSidecarContent()
    {
        var videoBytes = MotionPhotoFixtures.BuildMp4Head(40);
        var sidecarPath = Path.Combine(_tempDirectory, "IMG_300.mov");
        await File.WriteAllBytesAsync(sidecarPath, videoBytes, TestContext.Current.CancellationToken);

        var info = new MotionPhotoInfo
        {
            Source = MotionPhotoSource.Sidecar,
            SidecarFile = new FileInfo(sidecarPath),
        };

        var result = await MotionPhotoExtractor.ExtractAsync(new FileInfo(Path.Combine(_tempDirectory, "IMG_300.heic")), info, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await using (result.ConfigureAwait(false))
        {
            Assert.Equal(videoBytes.Length, result.Length);
        }
    }

    [Fact]
    public async Task ExtractAsync_MissingSidecar_ReturnsNull()
    {
        var info = new MotionPhotoInfo
        {
            Source = MotionPhotoSource.Sidecar,
            SidecarFile = new FileInfo(Path.Combine(_tempDirectory, "missing.mov")),
        };

        var result = await MotionPhotoExtractor.ExtractAsync(new FileInfo(Path.Combine(_tempDirectory, "IMG_400.jpg")), info, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExtractAsync_LivpContainer_ReturnsVideoEntry()
    {
        var videoBytes = MotionPhotoFixtures.BuildMp4Head(56);
        var livp = MotionPhotoFixtures.CreateLivp(_tempDirectory, "IMG_500.livp", [1, 2, 3], videoBytes);

        var info = new MotionPhotoInfo { Source = MotionPhotoSource.LivpContainer };

        var result = await MotionPhotoExtractor.ExtractAsync(livp, info, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await using (result.ConfigureAwait(false))
        {
            Assert.Equal(videoBytes.Length, result.Length);
            var buffer = new byte[result.Length];
            Assert.Equal(buffer.Length, await result.ReadAsync(buffer, TestContext.Current.CancellationToken));
            Assert.Equal(videoBytes, buffer);
        }
    }

    [Fact]
    public async Task ExtractLivpCoverToTempFileAsync_ExtractsImageEntry()
    {
        byte[] imageBytes = [10, 20, 30, 40];
        var livp = MotionPhotoFixtures.CreateLivp(_tempDirectory, "IMG_600.livp", imageBytes, MotionPhotoFixtures.BuildMp4Head());

        var tempPath = await MotionPhotoExtractor.ExtractLivpCoverToTempFileAsync(livp, TestContext.Current.CancellationToken);

        Assert.NotNull(tempPath);
        Assert.True(File.Exists(tempPath));
        Assert.Equal(imageBytes, await File.ReadAllBytesAsync(tempPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExtractLivpCoverToTempFileAsync_NoImageEntry_ReturnsNull()
    {
        var path = Path.Combine(_tempDirectory, "video-only.livp");
        await using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create);
            var entry = zip.CreateEntry("video.mov");
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(MotionPhotoFixtures.BuildMp4Head(), TestContext.Current.CancellationToken);
        }

        var result = await MotionPhotoExtractor.ExtractLivpCoverToTempFileAsync(new FileInfo(path), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }
}
