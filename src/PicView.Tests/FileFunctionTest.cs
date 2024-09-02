using PicView.Core.FileHandling;

namespace PicView.Tests;

public class FileFunctionTest
{
    [Fact]
    public void TestTemporaryFiles()
    {
        var result = TempFileHelper.CreateTempDirectory();
        var path = TempFileHelper.TempFilePath;
        Assert.True(result);
        Assert.True(Directory.Exists(TempFileHelper.TempFilePath));
        FileDeletionHelper.DeleteTempFiles();
        Assert.False(Directory.Exists(path));
        Assert.False(Directory.Exists(TempFileHelper.TempFilePath));
    }
}