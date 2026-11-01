using PicView.Core.FileHistory;

namespace PicView.Tests.FileHistory;

[Collection("Sequential")]
public class FileHistoryManagerTests : IDisposable
{
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly string _tempRoot;
    private readonly string _configPath;

    public FileHistoryManagerTests()
    {
        SetDefaults();
        Settings.Navigation.IsFileHistoryEnabled = true;
        _tempRoot = Path.Combine(Path.GetTempPath(), "PicView_FileHistoryTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _configPath = Path.Combine(_tempRoot, "FileHistory.json");
        FileHistoryManager.Initialize(_configPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try
            {
                Directory.Delete(_tempRoot, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        _fileHistoryBackup.Dispose();
    }

    [Fact]
    public void Add_AndNavigatePreviousAndNext_MovesCursorAndDeduplicates()
    {
        FileHistoryManager.Add("/path/one.jpg");
        FileHistoryManager.Add("/path/two.jpg");
        FileHistoryManager.Add("https://example.com/three.png");

        Assert.Equal(3, FileHistoryManager.Count);
        Assert.Equal("https://example.com/three.png", FileHistoryManager.CurrentEntry);
        Assert.True(FileHistoryManager.HasPrevious);
        Assert.False(FileHistoryManager.HasNext);

        Assert.Equal("/path/two.jpg", FileHistoryManager.GetPreviousEntry());
        Assert.Equal("/path/one.jpg", FileHistoryManager.GetPreviousEntry());
        Assert.Null(FileHistoryManager.GetPreviousEntry());

        Assert.Equal("/path/two.jpg", FileHistoryManager.GetNextEntry());

        // Re-adding an existing entry moves the cursor without duplicating
        FileHistoryManager.Add("/path/one.jpg");
        Assert.Equal(3, FileHistoryManager.Count);
        Assert.Equal("/path/one.jpg", FileHistoryManager.CurrentEntry);
    }

    [Fact]
    public void Pin_ExceedingMaxPinnedEntries_UnpinsOldestPinnedEntry()
    {
        for (var i = 1; i <= 6; i++)
        {
            var path = $"/path/file{i}.jpg";
            FileHistoryManager.Add(path);
            FileHistoryManager.Pin(path);
        }

        var pinned = FileHistoryManager.PinnedEntries.ToList();
        Assert.Equal(5, pinned.Count);
        Assert.DoesNotContain(pinned, e => e.Path == "/path/file1.jpg");
        Assert.Contains(pinned, e => e.Path == "/path/file6.jpg");
    }

    [Fact]
    public async Task SaveToFileAsync_AndInitialize_RoundTripsEntriesAndSortOrder()
    {
        FileHistoryManager.IsSortingDescending = true;
        FileHistoryManager.Add("/path/alpha.jpg");
        FileHistoryManager.Add("/path/beta.jpg");
        FileHistoryManager.Pin("/path/alpha.jpg");

        await FileHistoryManager.SaveToFileAsync();

        FileHistoryManager.Clear();
        Assert.Equal(0, FileHistoryManager.Count);

        FileHistoryManager.Initialize(_configPath);
        Assert.Equal(2, FileHistoryManager.Count);
        Assert.True(FileHistoryManager.IsSortingDescending);
        Assert.Single(FileHistoryManager.PinnedEntries.ToList());
        Assert.Equal("/path/alpha.jpg", FileHistoryManager.PinnedEntries.First().Path);
    }

    [Fact]
    public void Rename_AndRemove_UpdatesEntriesAndClampsCursor()
    {
        FileHistoryManager.Add("/path/old.jpg");
        FileHistoryManager.Add("/path/second.jpg");

        FileHistoryManager.Rename("/path/old.jpg", "/path/new.jpg");
        Assert.Equal("/path/new.jpg", FileHistoryManager.GetFirstEntry());

        var removed = FileHistoryManager.Remove("/path/second.jpg");
        Assert.True(removed);
        Assert.Equal(1, FileHistoryManager.Count);
        Assert.Equal("/path/new.jpg", FileHistoryManager.CurrentEntry);
    }

    [Fact]
    public void Add_PathAlreadyListed_MovesItToTheNewestEnd()
    {
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Add("/path/b.jpg");
        FileHistoryManager.Add("/path/c.jpg");

        FileHistoryManager.Add("/path/a.jpg");

        Assert.Equal(["/path/b.jpg", "/path/c.jpg", "/path/a.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public void Add_PinnedPathAgain_KeepsItsPlace()
    {
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Pin("/path/a.jpg");
        FileHistoryManager.Add("/path/b.jpg");

        FileHistoryManager.Add("/path/a.jpg");

        Assert.Equal(["/path/a.jpg", "/path/b.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public void Add_SamePathInAnotherCase_DoesNotAddItAgain()
    {
        FileHistoryManager.Add(@"C:\Pics\A.jpg");

        FileHistoryManager.Add(@"c:\pics\a.jpg");

        Assert.Equal(1, FileHistoryManager.Count);
    }

    [Fact]
    public void Add_NewPathAtTheCap_EvictsTheOldestUnpinnedEntry()
    {
        FileHistoryManager.Add("/path/pinned.jpg");
        FileHistoryManager.Pin("/path/pinned.jpg");
        for (var i = 1; i <= 50; i++)
        {
            FileHistoryManager.Add($"/path/{i}.jpg");
        }

        FileHistoryManager.Add("/path/new.jpg");

        var paths = FileHistoryManager.AllEntries.Select(e => e.Path).ToArray();
        Assert.Equal(51, paths.Length);
        Assert.Equal("/path/pinned.jpg", paths[0]);
        Assert.Equal("/path/2.jpg", paths[1]);
        Assert.Equal("/path/new.jpg", paths[^1]);
    }

    [Fact]
    public void AllEntries_IsACopy_ThatLaterAddsLeaveAlone()
    {
        FileHistoryManager.Add("/path/a.jpg");
        var copy = FileHistoryManager.AllEntries;

        FileHistoryManager.Add("/path/b.jpg");

        Assert.Single(copy);
    }

    [Fact]
    public async Task AllEntries_WhileOtherThreadsAdd_NeverThrows()
    {
        var adding = Enumerable.Range(0, 4).Select(thread => Task.Run(() =>
        {
            for (var i = 0; i < 2000; i++)
            {
                FileHistoryManager.Add($"/thread{thread}/{i % 80}.jpg");
            }
        })).ToArray();

        while (!adding.All(task => task.IsCompleted))
        {
            foreach (var entry in FileHistoryManager.AllEntries)
            {
                Assert.NotNull(entry.Path);
            }
            Assert.True(FileHistoryManager.PinnedEntries.Length <= 5);
        }

        await Task.WhenAll(adding);
        Assert.Equal(50, FileHistoryManager.Count);
    }
}
