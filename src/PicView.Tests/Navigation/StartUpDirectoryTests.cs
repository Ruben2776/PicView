using PicView.Avalonia.Navigation;

namespace PicView.Tests.Navigation;

/// <summary>
/// Which directory files are listed from when continuing from where the user left off.
/// </summary>
[Collection("Sequential")]
public class StartUpDirectoryTests : IDisposable
{
    private readonly string _root;
    private readonly string _file;

    public StartUpDirectoryTests()
    {
        SetDefaults();
        _root = Path.Combine(Path.GetTempPath(), "PicViewStartUpDirectoryTests_" + Guid.NewGuid());
        var sub = Directory.CreateDirectory(Path.Combine(_root, "sub")).FullName;
        _file = Path.Combine(sub, "a.jpg");
        File.WriteAllText(_file, "x");
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void GetStartUpDirectory_NotBrowsingSubdirectories_ReturnsNull()
    {
        Settings.Sorting.IncludeSubDirectories = false;
        Settings.StartUp.StartUpDirectory = _root;

        Assert.Null(SourceOpener.GetStartUpDirectory(_file));
    }

    [Fact]
    public void GetStartUpDirectory_FileInsideRemembered_ReturnsRemembered()
    {
        Settings.Sorting.IncludeSubDirectories = true;
        Settings.StartUp.StartUpDirectory = _root;

        Assert.Equal(_root, SourceOpener.GetStartUpDirectory(_file));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/somewhere/else")]
    public void GetStartUpDirectory_NothingRememberedOrFileOutside_ReturnsFilesDirectory(string remembered)
    {
        Settings.Sorting.IncludeSubDirectories = true;
        Settings.StartUp.StartUpDirectory = remembered;

        Assert.Equal(Path.GetDirectoryName(_file), SourceOpener.GetStartUpDirectory(_file));
    }

    [Fact]
    public void GetStartUpDirectory_SourceIsNotAFile_ReturnsNull()
    {
        Settings.Sorting.IncludeSubDirectories = true;

        Assert.Null(SourceOpener.GetStartUpDirectory("https://example.com/a.jpg"));
    }
}
