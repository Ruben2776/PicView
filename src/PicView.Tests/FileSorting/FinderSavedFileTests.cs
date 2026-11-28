#if MACOS
using PicView.Core.MacOS.FileSorting;
using static PicView.Tests.FileSorting.DsStoreBytes;

namespace PicView.Tests.FileSorting;

/// <summary>
/// Where a folder's saved Finder view is read from, and that a file which is missing, unreadable, too long or no
/// <c>.DS_Store</c> reads as no answer (ADR-0050, On macOS).
/// </summary>
public class FinderSavedFileTests : IDisposable
{
    private const string Folder = "Characters";

    private readonly string _parent = Path.Combine(Path.GetTempPath(), "picview-saved-" + Guid.NewGuid().ToString("N"));

    public FinderSavedFileTests()
    {
        Directory.CreateDirectory(Path.Combine(_parent, Folder));
    }

    public void Dispose() => Directory.Delete(_parent, true);

    private string FolderPath => Path.Combine(_parent, Folder);

    private string StorePath => Path.Combine(_parent, ".DS_Store");

    private static byte[] FileWithKind() =>
        Build([(Folder, "GRP0", "ustr", Ustr("Kind"))]);

    [Fact]
    public void SavedViewLocation_IsTheParentsDsStore_AndTheFoldersName()
    {
        var expected = ("/Users/someone/Pictures/.DS_Store", "Characters");

        Assert.Equal(expected, FinderFolderOrder.SavedViewLocation("/Users/someone/Pictures/Characters"));
        Assert.Equal(expected, FinderFolderOrder.SavedViewLocation("/Users/someone/Pictures/Characters/"));
    }

    [Fact]
    public void SavedViewLocation_FolderInTheRoot_IsTheRootsDsStore() =>
        Assert.Equal(("/.DS_Store", "Pictures"), FinderFolderOrder.SavedViewLocation("/Pictures"));

    [Fact]
    public void SavedViewLocation_SeveralTrailingSeparators_IsNullAndDoesNotThrow() =>
        Assert.Null(FinderFolderOrder.SavedViewLocation("/Users/someone/Pictures/Characters//"));

    [Theory]
    [InlineData("/")]
    [InlineData("")]
    public void SavedViewLocation_FolderWithNoParent_IsNull(string directory) =>
        Assert.Null(FinderFolderOrder.SavedViewLocation(directory));

    [Fact]
    public void SavedView_ReadsWhatTheParentsFileHolds()
    {
        File.WriteAllBytes(StorePath, FileWithKind());

        Assert.Equal("Kind", FinderFolderOrder.SavedView(FolderPath)?.GroupBy);
    }

    [Fact]
    public void SavedView_NoFile_IsNull() => Assert.Null(FinderFolderOrder.SavedView(FolderPath));

    [Fact]
    public void SavedView_FileThatIsNoDsStore_IsNull()
    {
        File.WriteAllText(StorePath, "not one");

        Assert.Null(FinderFolderOrder.SavedView(FolderPath));
    }

    [Fact]
    public void SavedView_FileLargerThanTheLimit_IsNull()
    {
        File.WriteAllBytes(StorePath, FileWithKind());
        using (var stream = new FileStream(StorePath, FileMode.Open, FileAccess.Write))
        {
            stream.SetLength(4 * 1024 * 1024 + 1);
        }

        Assert.Null(FinderFolderOrder.SavedView(FolderPath));
    }
}
#endif
