using PicView.Core.FileSorting;

namespace PicView.Tests.FileSorting;

// Including subdirectories is read from the settings record, which other tests change too
[Collection("Sequential")]
public class FileListRetrieverTests : IDisposable
{
    private static readonly DateTime BaseTime = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PicViewFileListRetriever_" + Guid.NewGuid().ToString("N"));

    public FileListRetrieverTests()
    {
        SetDefaults();
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        SetDefaults();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    /// <summary>
    /// Creates a file whose size and last write time grow with <paramref name="rank"/>.
    /// </summary>
    private FileInfo Create(string relativePath, int rank)
    {
        var path = Path.Combine(_folder, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[rank * 10]);
        File.SetLastWriteTimeUtc(path, BaseTime.AddDays(rank));
        return new FileInfo(path);
    }

    private static string[] Names(IEnumerable<FileInfo> files) => files.Select(f => f.Name).ToArray();

    [Theory]
    // Every order differs from the name order a, b, c the record (Name ascending) would give
    [InlineData(SortFilesBy.FileSize, true, new[] { "b.jpg", "a.jpg", "c.jpg" })]
    [InlineData(SortFilesBy.LastWriteTime, true, new[] { "b.jpg", "a.jpg", "c.jpg" })]
    [InlineData(SortFilesBy.Extension, false, new[] { "b.png", "c.jpg", "a.gif" })]
    public void RetrieveFiles_ListsInTheGivenOrder(SortFilesBy by, bool ascending, string[] expected)
    {
        var extensions = by == SortFilesBy.Extension ? new[] { ".gif", ".png", ".jpg" } : [".jpg", ".jpg", ".jpg"];
        var a = Create("a" + extensions[0], 2);
        Create("b" + extensions[1], 1);
        Create("c" + extensions[2], 3);

        var files = FileListRetriever.RetrieveFiles(a, new FileOrdering(new FileOrder(by, ascending), string.CompareOrdinal));

        Assert.Equal(expected, Names(files));
    }

    [Fact]
    public void RetrieveFiles_IncludingSubdirectories_OrdersAcrossFolders()
    {
        Settings.Sorting.IncludeSubDirectories = true;
        var a = Create("a.jpg", 2);
        var sub = Create(Path.Combine("sub", "b.jpg"), 1);
        var c = Create("c.jpg", 3);
        var deeper = Create(Path.Combine("sub", "deeper", "d.jpg"), 4);

        var files = FileListRetriever.RetrieveFiles(a,
            new FileOrdering(new FileOrder(SortFilesBy.FileSize, false), string.CompareOrdinal));

        // By size, largest first, whichever folder each is in
        Assert.Equal([deeper.FullName, c.FullName, a.FullName, sub.FullName], files.Select(f => f.FullName));
    }
}
