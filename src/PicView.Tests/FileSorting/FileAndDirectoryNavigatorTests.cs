using PicView.Core.Config;
using PicView.Core.FileSorting;

namespace PicView.Tests.FileSorting;

[Collection("Sequential")]
public class FileAndDirectoryNavigatorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "PicViewNavigator_" + Guid.NewGuid().ToString("N"));

    public FileAndDirectoryNavigatorTests()
    {
        SetDefaults();
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    private static FileOrdering Ordering(SortFilesBy by = SortFilesBy.Name, bool ascending = true) =>
        new(new FileOrder(by, ascending), string.CompareOrdinal);

    private string CreateFile(string relativePath, int length = 0)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[length]);
        return path;
    }

    [Fact]
    public void FindNextArchive_PastAFoldersLastArchive_ContinuesFromTheNextSibling_NotTheFirst()
    {
        CreateFile(Path.Combine("a", "a1.zip"));
        var lastInA = CreateFile(Path.Combine("a", "a2.zip"));
        var firstInB = CreateFile(Path.Combine("b", "b1.zip"));
        CreateFile(Path.Combine("c", "c1.zip"));

        var next = new FileAndDirectoryNavigator().FindNextArchive(Path.Combine(_root, "a"), true, lastInA, Ordering());

        Assert.Equal(firstInB, next);
    }

    [Fact]
    public void FindNextArchive_SortedBySizeDescending_StepsInThatOrder()
    {
        // By name the next after x would be y; by size descending it is z
        var x = CreateFile("x.zip", 30);
        CreateFile("y.zip", 10);
        var z = CreateFile("z.zip", 20);

        var next = new FileAndDirectoryNavigator().FindNextArchive(_root, true, x, Ordering(SortFilesBy.FileSize, false));

        Assert.Equal(z, next);
    }

    [Fact]
    public void FindNextValidDirectory_Descending_StepsBackwardsByName()
    {
        CreateFile(Path.Combine("d1", "one.jpg"));
        var d2 = Path.Combine(_root, "d2");
        CreateFile(Path.Combine("d2", "two.jpg"));
        CreateFile(Path.Combine("d3", "three.jpg"));

        var next = new FileAndDirectoryNavigator().FindNextValidDirectory(
            Path.Combine(_root, "d3"), Ordering(SortFilesBy.FileSize, false));

        Assert.Equal(d2, next);
    }
}
