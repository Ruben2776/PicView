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

    [Theory]
    // Windows forms
    [InlineData(@"C:\Users\a\AppData\Local\Temp\x.png", @"C:\Users\a\AppData\Local\Temp\", true, false, true)]
    [InlineData(@"C:\Users\a\AppData\Local\Temp\sub\x.png", @"C:\Users\a\AppData\Local\Temp", true, false, true)]
    [InlineData(@"c:\users\A\appdata\local\TEMP\x.png", @"C:\Users\a\AppData\Local\Temp\", true, false, true)]
    [InlineData(@"C:\Temp2\x.png", @"C:\Temp", true, false, false)]
    [InlineData(@"C:\Temp", @"C:\Temp\", true, false, false)]
    [InlineData(@"C:\Pictures\x.png", @"C:\Temp\", true, false, false)]
    // macOS forms
    [InlineData("/private/var/folders/ab/T/x.png", "/var/folders/ab/T/", true, true, true)]
    [InlineData("/var/folders/ab/T/x.png", "/private/var/folders/ab/T/", true, true, true)]
    [InlineData("/var/folders/ab/T/x.png", "/var/folders/ab/T/", true, true, true)]
    [InlineData("/privateer/var/folders/ab/T/x.png", "/var/folders/ab/T/", true, true, false)]
    [InlineData("/Users/a/Pictures/x.png", "/var/folders/ab/T/", true, true, false)]
    // Exact-case systems
    [InlineData("/TMP/x.png", "/tmp/", false, false, false)]
    [InlineData("/tmp/x.png", "/tmp/", false, false, true)]
    // Nothing to check
    [InlineData("", "/tmp/", false, false, false)]
    [InlineData(null, "/tmp/", false, false, false)]
    public void IsInTempFolder_PathAndRoot_ReturnsExpected(string? path, string tempRoot, bool ignoreCase,
        bool stripPrivatePrefix, bool expected)
    {
        Assert.Equal(expected, TempFileManager.IsInTempFolder(path, tempRoot, ignoreCase, stripPrivatePrefix));
    }

    [Fact]
    public void IsInTempFolder_NewTempFilePath_ReturnsTrue()
    {
        Assert.True(TempFileManager.IsInTempFolder(TempFileManager.GetNewTempFilePath("a.svg")));
    }
}
