using System.Collections.Concurrent;
using PicView.Core.FileSorting;

namespace PicView.Tests.FileSorting;

/// <summary>
/// The dates read from the file manager, and how the Date field of a File Order takes them (ADR-0051).
/// </summary>
public sealed class FileDatesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PicViewFileDatesTests_" + Guid.NewGuid());

    public FileDatesTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, true);

    // Shows the date a test gives a file by name, none for the rest, and counts what it is asked
    private sealed class FakeSource : IFileDateSource
    {
        public ConcurrentDictionary<string, DateTime> Dates { get; } = new(StringComparer.OrdinalIgnoreCase);
        public ConcurrentQueue<string> Asked { get; } = new();
        public bool CannotTell { get; set; }

        public DateTime? DateOf(string path)
        {
            Asked.Enqueue(Path.GetFileName(path));
            if (CannotTell)
            {
                return null;
            }

            return Dates.TryGetValue(Path.GetFileName(path), out var date) ? date : DateTime.MinValue;
        }
    }

    private FileInfo Write(string name, DateTime written)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllBytes(path, [1]);
        File.SetCreationTimeUtc(path, written);
        File.SetLastWriteTimeUtc(path, written);
        return new FileInfo(path);
    }

    private static readonly DateTime Day = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReadAsync_GivesEachFileTheDateTheSourceShows_AndAsksOnce()
    {
        var source = new FakeSource();
        source.Dates["a.jpg"] = Day.AddYears(-10);
        var dates = new FileDates(source);
        FileInfo[] files = [Write("a.jpg", Day), Write("b.jpg", Day)];
        Assert.Null(dates.Known(files[0]));

        await dates.ReadAsync(files, TestContext.Current.CancellationToken);
        await dates.ReadAsync(files, TestContext.Current.CancellationToken);

        Assert.Equal(Day.AddYears(-10), dates.Known(files[0]));
        // Shown no date, which is known too: it goes before every dated file
        Assert.Equal(DateTime.MinValue, dates.Known(files[1]));
        Assert.Equal(["a.jpg", "b.jpg"], source.Asked.Order());
    }

    [Fact]
    public async Task Known_FileWrittenSinceItsDateWasRead_IsNull_AndItIsReadAgain()
    {
        var source = new FakeSource();
        source.Dates["a.jpg"] = Day.AddYears(-10);
        var dates = new FileDates(source);
        var file = Write("a.jpg", Day);
        await dates.ReadAsync([file], TestContext.Current.CancellationToken);

        var rewritten = Write("a.jpg", Day.AddDays(1));

        Assert.Null(dates.Known(rewritten));
        source.Dates["a.jpg"] = Day.AddYears(-5);
        await dates.ReadAsync([rewritten], TestContext.Current.CancellationToken);
        Assert.Equal(Day.AddYears(-5), dates.Known(rewritten));
    }

    [Fact]
    public async Task ReadAsync_SourceThatCannotTell_LeavesTheFileSystemsDates()
    {
        var source = new FakeSource { CannotTell = true };
        var dates = new FileDates(source);
        var file = Write("a.jpg", Day);

        await dates.ReadAsync([file], TestContext.Current.CancellationToken);

        Assert.Null(dates.Known(file));
    }

    [Fact]
    public async Task ReadAsync_WithoutASource_ReadsNothing()
    {
        var dates = new FileDates();
        var file = Write("a.jpg", Day);

        await dates.ReadAsync([file], TestContext.Current.CancellationToken);

        Assert.False(dates.CanRead);
        Assert.Null(dates.Known(file));
    }

    [Fact]
    public async Task Sort_ByDate_TakesTheDateReadBeforeTheFileSystems_AndIsSortedSaysWhenThatChangedTheOrder()
    {
        var source = new FakeSource();
        // By the file system a, b, c; c has a date taken long before, and b is shown none
        source.Dates["a.jpg"] = Day;
        source.Dates["c.jpg"] = Day.AddYears(-10);
        var dates = new FileDates(source);
        FileInfo[] files = [Write("a.jpg", Day), Write("b.jpg", Day.AddDays(1)), Write("c.jpg", Day.AddDays(2))];
        var ordering = new FileOrdering(new FileOrder(SortFilesBy.Date, true), string.CompareOrdinal, dates.Known);
        var listed = ordering.Sort(files);
        Assert.Equal(["a.jpg", "b.jpg", "c.jpg"], listed.Select(f => f.Name));
        Assert.True(ordering.IsSorted(listed));

        await dates.ReadAsync(files, TestContext.Current.CancellationToken);

        Assert.False(ordering.IsSorted(listed));
        Assert.Equal(["b.jpg", "c.jpg", "a.jpg"], ordering.Sort(listed).Select(f => f.Name));
        Assert.Equal(1, ordering.InsertPosition(ordering.Sort(listed), files[2]) - 1);
    }

    [Fact]
    public void IsSorted_Random_IsAlwaysTrue()
    {
        FileInfo[] files = [Write("b.jpg", Day), Write("a.jpg", Day)];

        Assert.True(new FileOrdering(new FileOrder(SortFilesBy.Random, true), string.CompareOrdinal).IsSorted(files));
        Assert.False(new FileOrdering(new FileOrder(SortFilesBy.Name, true), string.CompareOrdinal).IsSorted(files));
    }
}
