using PicView.Core.FileHandling;

namespace PicView.Tests;

public class FileFunctionTest
{
    [Fact]
    public void TestTemporaryFiles()
    {
        var path = Path.GetTempPath() + Path.GetRandomFileName();
        var result = ArchiveHelper.CreateTempDirectory(path);
        Assert.True(result);
        Assert.True(Directory.Exists(ArchiveHelper.TempFilePath));
        FileDeletionHelper.DeleteTempFiles();
        Assert.False(Directory.Exists(path));
        Assert.False(Directory.Exists(ArchiveHelper.TempFilePath));
    }
}