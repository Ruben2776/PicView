using PicView.Core.MotionPhoto;

namespace PicView.Tests.MotionPhoto;

public class FileSliceStreamTests : IDisposable
{
    private readonly string _tempDirectory = MotionPhotoFixtures.CreateTempDirectory();

    public void Dispose()
    {
        MotionPhotoFixtures.DeleteDirectory(_tempDirectory);
        GC.SuppressFinalize(this);
    }

    private string CreateFile(byte[] bytes)
    {
        var path = Path.Combine(_tempDirectory, Path.GetRandomFileName());
        File.WriteAllBytes(path, bytes);
        return path;
    }

    [Fact]
    public void Read_WithinSlice_ReturnsSlicedBytes()
    {
        byte[] content = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        var path = CreateFile(content);

        using var stream = new FileSliceStream(path, 4, 3);

        Assert.Equal(3, stream.Length);
        var buffer = new byte[3];
        Assert.Equal(3, stream.Read(buffer));
        Assert.Equal([4, 5, 6], buffer);
        // Past the end of the slice
        Assert.Equal(0, stream.Read(buffer));
    }

    [Fact]
    public void Read_RequestedMoreThanRemaining_IsClampedToSlice()
    {
        byte[] content = [0, 1, 2, 3, 4, 5];
        var path = CreateFile(content);

        using var stream = new FileSliceStream(path, 2, 3);

        var buffer = new byte[16];
        Assert.Equal(3, stream.Read(buffer));
        Assert.Equal([2, 3, 4], buffer.AsSpan(0, 3).ToArray());
    }

    [Fact]
    public void Seek_VariousOrigins_StaysWithinSlice()
    {
        var path = CreateFile(new byte[100]);

        using var stream = new FileSliceStream(path, 10, 20);

        Assert.Equal(20, stream.Length);
        Assert.Equal(0, stream.Position);

        Assert.Equal(5, stream.Seek(5, SeekOrigin.Begin));
        Assert.Equal(5, stream.Position);

        Assert.Equal(8, stream.Seek(3, SeekOrigin.Current));

        Assert.Equal(18, stream.Seek(-2, SeekOrigin.End));

        // Out-of-range seeks are clamped, reads follow the clamped position
        Assert.Equal(20, stream.Seek(999, SeekOrigin.Begin));
        Assert.Equal(0, stream.Read(new byte[1]));

        Assert.Equal(0, stream.Seek(-999, SeekOrigin.Begin));
    }

    [Fact]
    public void Length_SliceLongerThanFile_IsClampedToFile()
    {
        var path = CreateFile(new byte[10]);

        using var stream = new FileSliceStream(path, 6, 1000);

        Assert.Equal(4, stream.Length);
    }

    [Fact]
    public async Task ReadAsync_WithinSlice_ReturnsSlicedBytes()
    {
        byte[] content = [10, 11, 12, 13, 14];
        var path = CreateFile(content);

        await using var stream = new FileSliceStream(path, 1, 3);

        var buffer = new byte[3];
        Assert.Equal(3, await stream.ReadAsync(buffer, TestContext.Current.CancellationToken));
        Assert.Equal([11, 12, 13], buffer);
    }
}
