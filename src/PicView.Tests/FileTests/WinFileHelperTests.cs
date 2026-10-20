#if WINDOWS
using PicView.Core.WindowsNT.FileHandling;

namespace PicView.Tests.FileTests;

public class WinFileHelperTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"PicView.WinFileHelperTests.{Guid.NewGuid():N}");

    public WinFileHelperTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void DeleteFile_Permanently_DeletesFileAndReturnsTrue()
    {
        var path = Path.Combine(_directory, "image.jpg");
        File.WriteAllBytes(path, [1, 2, 3]);

        Assert.True(WinFileHelper.DeleteFile(path, moveToRecycleBin: false));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void HasRecycleBin_FixedDrive_ReturnsTrue()
    {
        Assert.True(WinFileHelper.HasRecycleBin(Path.Combine(_directory, "image.jpg")));
    }

    [Fact]
    public void HasRecycleBin_NetworkShare_ReturnsFalse()
    {
        Assert.False(WinFileHelper.HasRecycleBin(@"\\server\share\image.jpg"));
    }
}
#endif
