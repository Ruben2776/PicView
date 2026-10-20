using PicView.Core.FileHistory;
using ZLinq;

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
}
