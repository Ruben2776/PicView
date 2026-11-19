using PicView.Core.Config;
using PicView.Core.FileSorting;

namespace PicView.Tests.FileSorting;

public class FileOrderingTests : IDisposable
{
    private static readonly DateTime BaseTime = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PicViewFileOrdering_" + Guid.NewGuid().ToString("N"));

    public FileOrderingTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    private static FileOrdering Ordering(SortFilesBy by, bool ascending = true) =>
        new(new FileOrder(by, ascending), string.CompareOrdinal);

    /// <summary>
    /// Creates a file whose <paramref name="field"/> has the given rank (higher rank, higher value) and whose other
    /// fields are the same for every file.
    /// </summary>
    private FileInfo Create(SortFilesBy field, string relativePath, int rank)
    {
        if (field == SortFilesBy.Extension)
        {
            relativePath += ".ext" + rank;
        }
        else
        {
            relativePath += ".txt";
        }

        var path = Path.Combine(_folder, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[field == SortFilesBy.FileSize ? rank * 10 : 10]);
        var created = field == SortFilesBy.CreationTime ? BaseTime.AddDays(rank) : BaseTime;
        var accessed = field == SortFilesBy.LastAccessTime ? BaseTime.AddDays(rank) : BaseTime;
        var written = field == SortFilesBy.LastWriteTime ? BaseTime.AddDays(rank) : BaseTime;
        File.SetCreationTimeUtc(path, created);
        File.SetLastAccessTimeUtc(path, accessed);
        File.SetLastWriteTimeUtc(path, written);
        return new FileInfo(path);
    }

    private static string[] Names(IEnumerable<FileInfo> files) => files.Select(f => f.Name).ToArray();

    [Theory]
    [InlineData(SortFilesBy.FileSize)]
    [InlineData(SortFilesBy.CreationTime)]
    [InlineData(SortFilesBy.Extension)]
    [InlineData(SortFilesBy.LastAccessTime)]
    [InlineData(SortFilesBy.LastWriteTime)]
    [InlineData(SortFilesBy.Name)]
    public void Sort_ByEachField_OrdersByThatField(SortFilesBy field)
    {
        // The field order (b lowest, then a, then c) differs from the name order (a, b, c), except for Name itself.
        var a = Create(field, "a", 2);
        var b = Create(field, "b", 1);
        var c = Create(field, "c", 3);

        var sorted = Ordering(field).Sort([c, a, b]);

        var expected = field == SortFilesBy.Name ? new[] { a, b, c } : [b, a, c];
        Assert.Equal(expected.Select(f => f.FullName), sorted.Select(f => f.FullName));
    }

    [Fact]
    public void Sort_Descending_ReversesTheWholeOrderIncludingTies()
    {
        var a = Create(SortFilesBy.FileSize, "a", 1);
        var subA = Create(SortFilesBy.FileSize, Path.Combine("sub", "a"), 1);
        var b = Create(SortFilesBy.FileSize, "b", 1);
        var c = Create(SortFilesBy.FileSize, "c", 2);

        var ascending = Ordering(SortFilesBy.FileSize).Sort([c, b, subA, a]);
        var descending = Ordering(SortFilesBy.FileSize, false).Sort([a, b, c, subA]);

        Assert.Equal([a.FullName, subA.FullName, b.FullName, c.FullName], ascending.Select(f => f.FullName));
        Assert.Equal([c.FullName, b.FullName, subA.FullName, a.FullName], descending.Select(f => f.FullName));
    }

    [Theory]
    [InlineData(SortFilesBy.FileSize)]
    [InlineData(SortFilesBy.Extension)]
    [InlineData(SortFilesBy.CreationTime)]
    [InlineData(SortFilesBy.LastAccessTime)]
    [InlineData(SortFilesBy.LastWriteTime)]
    public void Sort_FilesEqualInTheField_FallBackToNameThenFullPath(SortFilesBy field)
    {
        var b = Create(field, "b", 1);
        var subA = Create(field, Path.Combine("sub", "a"), 1);
        var a = Create(field, "a", 1);

        var sorted = Ordering(field).Sort([b, subA, a]);

        // Same name a: the shorter ordinal path ("...\a" before "...\sub\a") goes first; then b.
        Assert.Equal([a.FullName, subA.FullName, b.FullName], sorted.Select(f => f.FullName));
    }

    [Fact]
    public void Sort_ExtensionDifferingOnlyInCase_AreEqualAndFallBackToName()
    {
        var folder = Directory.CreateDirectory(Path.Combine(_folder, "case")).FullName;
        var files = new[] { "c.Jpg", "a.JPG", "b.jpg" }.Select(name =>
        {
            var path = Path.Combine(folder, name);
            File.WriteAllBytes(path, [1]);
            return new FileInfo(path);
        }).ToArray();

        var sorted = Ordering(SortFilesBy.Extension).Sort(files);

        Assert.Equal(["a.JPG", "b.jpg", "c.Jpg"], Names(sorted));
    }

    [Theory]
    [InlineData(SortFilesBy.Name, true)]
    [InlineData(SortFilesBy.Name, false)]
    [InlineData(SortFilesBy.FileSize, true)]
    [InlineData(SortFilesBy.FileSize, false)]
    [InlineData(SortFilesBy.CreationTime, true)]
    [InlineData(SortFilesBy.CreationTime, false)]
    [InlineData(SortFilesBy.Extension, true)]
    [InlineData(SortFilesBy.Extension, false)]
    [InlineData(SortFilesBy.LastAccessTime, true)]
    [InlineData(SortFilesBy.LastAccessTime, false)]
    [InlineData(SortFilesBy.LastWriteTime, true)]
    [InlineData(SortFilesBy.LastWriteTime, false)]
    public void InsertPosition_EqualsWhereAFullSortPutsTheFile(SortFilesBy field, bool ascending)
    {
        // Ties: a and sub/a share name and field, b and c share the field, d and e differ.
        var all = new[]
        {
            Create(field, "a", 2),
            Create(field, Path.Combine("sub", "a"), 2),
            Create(field, "b", 1),
            Create(field, "c", 1),
            Create(field, "d", 3),
            Create(field, "e", 2)
        };
        var ordering = Ordering(field, ascending);
        var fullSort = ordering.Sort(all).Select(f => f.FullName).ToArray();

        foreach (var file in all)
        {
            var sorted = ordering.Sort(all.Where(f => f != file));

            sorted.Insert(ordering.InsertPosition(sorted, file), file);

            Assert.Equal(fullSort, sorted.Select(f => f.FullName));
        }
    }

    [Fact]
    public void Sort_Random_KeepsEveryFile_AndInsertPositionAppends()
    {
        var files = Enumerable.Range(0, 20).Select(i => Create(SortFilesBy.Name, "f" + i, 1)).ToArray();
        var ordering = Ordering(SortFilesBy.Random);

        var sorted = ordering.Sort(files);

        Assert.Equal(files.Select(f => f.FullName).Order(StringComparer.Ordinal),
            sorted.Select(f => f.FullName).Order(StringComparer.Ordinal));
        Assert.Equal(sorted.Count, ordering.InsertPosition(sorted, files[0]));
        Assert.Equal(0, ordering.InsertPosition([], files[0]));
    }

    [Theory]
    [InlineData(SortFilesBy.FileSize)]
    [InlineData(SortFilesBy.CreationTime)]
    [InlineData(SortFilesBy.LastAccessTime)]
    [InlineData(SortFilesBy.LastWriteTime)]
    public void Sort_AFileThatNoLongerExists_SortsAsEmptyAndDoesNotThrow(SortFilesBy field)
    {
        var a = Create(field, "a", 1);
        var c = Create(field, "c", 2);
        var ghost = Create(field, "b", 3);
        var ghostInfo = new FileInfo(ghost.FullName);
        File.Delete(ghost.FullName);

        var ascending = Ordering(field).Sort([c, ghostInfo, a]);
        var descending = Ordering(field, false).Sort([a, ghostInfo, c]);

        Assert.Equal(["b.txt", "a.txt", "c.txt"], Names(ascending));
        Assert.Equal(["c.txt", "a.txt", "b.txt"], Names(descending));
    }

    [Fact]
    public void InsertPosition_AFileThatNoLongerExists_DoesNotThrow()
    {
        var a = Create(SortFilesBy.FileSize, "a", 1);
        var c = Create(SortFilesBy.FileSize, "c", 2);
        var ghost = Create(SortFilesBy.FileSize, "b", 3);
        var ghostInfo = new FileInfo(ghost.FullName);
        File.Delete(ghost.FullName);
        var ordering = Ordering(SortFilesBy.FileSize);

        Assert.Equal(0, ordering.InsertPosition([a, c], ghostInfo));
        Assert.Equal(1, ordering.InsertPosition([ghostInfo, c], a));
    }

    [Theory]
    [InlineData(SortFilesBy.FileSize)]
    [InlineData(SortFilesBy.CreationTime)]
    [InlineData(SortFilesBy.Extension)]
    [InlineData(SortFilesBy.LastAccessTime)]
    [InlineData(SortFilesBy.LastWriteTime)]
    [InlineData(SortFilesBy.Name)]
    public void CompareArchiveEntries_ByEachField(SortFilesBy field)
    {
        var a = new ArchiveEntryOrderKey("a.png", "k1", 20, BaseTime.AddDays(2));
        var b = new ArchiveEntryOrderKey("b.bmp", "k2", 10, BaseTime.AddDays(1));
        var c = new ArchiveEntryOrderKey("c.gif", "k3", 30, BaseTime.AddDays(3));
        var list = new List<ArchiveEntryOrderKey> { c, a, b };
        var ordering = Ordering(field);

        list.Sort((x, y) => ordering.Compare(x, y));
        var ascendingKeys = list.Select(e => e.Key).ToArray();
        var descending = Ordering(field, false);
        list.Sort((x, y) => descending.Compare(x, y));

        // Extension order is bmp, gif, png: b, c, a. Name order is a, b, c. Size and dates: b, a, c.
        var expected = field switch
        {
            SortFilesBy.Name => new[] { "k1", "k2", "k3" },
            SortFilesBy.Extension => ["k2", "k3", "k1"],
            _ => ["k2", "k1", "k3"]
        };
        Assert.Equal(expected, ascendingKeys);
        Assert.Equal(expected.Reverse(), list.Select(e => e.Key));
    }

    [Fact]
    public void CompareArchiveEntries_NoModifiedTime_FallsBackToName()
    {
        var withTime = new ArchiveEntryOrderKey("a.png", "k1", 1, BaseTime);
        var noTimeB = new ArchiveEntryOrderKey("b.png", "k2", 1, null);
        var noTimeA = new ArchiveEntryOrderKey("a.png", "k3", 1, null);
        var ordering = Ordering(SortFilesBy.LastWriteTime);
        var list = new List<ArchiveEntryOrderKey> { withTime, noTimeB, noTimeA };

        list.Sort((x, y) => ordering.Compare(x, y));

        // Missing times are DateTime.MinValue, so those two come first, a before b; the dated entry is last.
        Assert.Equal(["k3", "k2", "k1"], list.Select(e => e.Key));
    }

    [Fact]
    public void CompareArchiveEntries_Ties_FallBackToNameThenKey()
    {
        var second = new ArchiveEntryOrderKey("a.png", "z/a.png", 5, BaseTime);
        var first = new ArchiveEntryOrderKey("a.png", "b/a.png", 5, BaseTime);
        var third = new ArchiveEntryOrderKey("b.png", "a/b.png", 5, BaseTime);
        var ordering = Ordering(SortFilesBy.FileSize);
        var list = new List<ArchiveEntryOrderKey> { third, second, first };

        list.Sort((x, y) => ordering.Compare(x, y));

        Assert.Equal(["b/a.png", "z/a.png", "a/b.png"], list.Select(e => e.Key));
        Assert.Equal(0, ordering.Compare(first, first));
    }

    [Fact]
    public void CompareNames_AppliesTheDirection()
    {
        Assert.True(Ordering(SortFilesBy.Name).CompareNames("a", "b") < 0);
        Assert.True(Ordering(SortFilesBy.Name, false).CompareNames("a", "b") > 0);
    }

    [Theory]
    [InlineData(0, SortFilesBy.Name)]
    [InlineData(1, SortFilesBy.FileSize)]
    [InlineData(2, SortFilesBy.CreationTime)]
    [InlineData(3, SortFilesBy.Extension)]
    [InlineData(4, SortFilesBy.LastAccessTime)]
    [InlineData(5, SortFilesBy.LastWriteTime)]
    [InlineData(6, SortFilesBy.Random)]
    public void FromRecord_ReadsFieldAndDirection(int preference, SortFilesBy expected)
    {
        var ascending = FileOrder.FromRecord(new Sorting { SortPreference = preference, Ascending = true });
        var descending = FileOrder.FromRecord(new Sorting { SortPreference = preference, Ascending = false });

        Assert.Equal(new FileOrder(expected, true), ascending);
        Assert.Equal(new FileOrder(expected, false), descending);
    }

    [Fact]
    public void FromRecord_Null_IsNameAscending() =>
        Assert.Equal(new FileOrder(SortFilesBy.Name, true), FileOrder.FromRecord(null));

    [Theory]
    [InlineData(7)]
    [InlineData(-1)]
    [InlineData(99)]
    public void FromRecord_UnknownNumber_IsName(int preference) =>
        Assert.Equal(new FileOrder(SortFilesBy.Name, false),
            FileOrder.FromRecord(new Sorting { SortPreference = preference, Ascending = false }));

    [Fact]
    public void StoreTo_WritesOnlySortPreferenceAndAscending()
    {
        var sorting = new Sorting
        {
            IncludeSubDirectories = true, Name = false, Size = true, CreationTime = true, LastAccessTime = true,
            LastWriteTime = true, Random = true, SortPreference = 0, Ascending = true
        };

        new FileOrder(SortFilesBy.LastWriteTime, false).StoreTo(sorting);

        Assert.Equal(5, sorting.SortPreference);
        Assert.False(sorting.Ascending);
        Assert.True(sorting.IncludeSubDirectories);
        Assert.False(sorting.Name);
        Assert.True(sorting.Size);
        Assert.True(sorting.CreationTime);
        Assert.True(sorting.LastAccessTime);
        Assert.True(sorting.LastWriteTime);
        Assert.True(sorting.Random);
    }

}
