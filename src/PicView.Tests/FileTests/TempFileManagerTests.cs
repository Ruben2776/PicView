using PicView.Core.FileHandling;
using System.IO;

namespace PicView.Tests.FileTests;

[Collection("Sequential")]
public class TempFileManagerTests : IDisposable
{
    public TempFileManagerTests()
    {
        TempFileManager.Cleanup();
    }

    public void Dispose()
    {
        TempFileManager.Cleanup();
    }

    [Fact]
    public void GetNewTempFilePath_ShouldReturnValidPathAndCreateDirectory()
    {
        var fileName = "testFile.txt";

        var result = TempFileManager.GetNewTempFilePath(fileName);

        Assert.NotNull(result);
        Assert.EndsWith(fileName, result);

        var directory = Path.GetDirectoryName(result);
        Assert.NotNull(directory);
        Assert.True(Directory.Exists(directory));
    }

    [Fact]
    public void Cleanup_ShouldRemoveCreatedFilesAndDirectories()
    {
        var fileName = "testFile2.txt";
        var result = TempFileManager.GetNewTempFilePath(fileName);
        
        File.WriteAllText(result, "test content");
        Assert.True(File.Exists(result));

        var directory = Path.GetDirectoryName(result);
        Assert.NotNull(directory);
        Assert.True(Directory.Exists(directory));

        TempFileManager.Cleanup();

        Assert.False(File.Exists(result));
        Assert.False(Directory.Exists(directory));
    }
    
    [Fact]
    public void Cleanup_MultipleTimes_ShouldNotThrow()
    {
        var result = TempFileManager.GetNewTempFilePath("test.txt");
        File.WriteAllText(result, "test");
        
        var exception = Record.Exception(() => 
        {
            TempFileManager.Cleanup();
            TempFileManager.Cleanup();
        });
        
        Assert.Null(exception);
    }
}
