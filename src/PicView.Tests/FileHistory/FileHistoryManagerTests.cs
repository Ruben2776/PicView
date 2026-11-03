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
    public void Rename_AndRemove_UpdateTheEntries()
    {
        FileHistoryManager.Add("/path/old.jpg");
        FileHistoryManager.Add("/path/second.jpg");

        FileHistoryManager.Rename("/path/old.jpg", "/path/new.jpg");
        var removed = FileHistoryManager.Remove("/path/second.jpg");

        Assert.True(removed);
        Assert.Equal(["/path/new.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public void Rename_KeepsTheEntrysPlaceAndPin()
    {
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Pin("/path/a.jpg");
        FileHistoryManager.Add("/path/b.jpg");

        FileHistoryManager.Rename("/path/a.jpg", "/path/z.jpg");

        Assert.Equal(["/path/z.jpg", "/path/b.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
        Assert.Equal("/path/z.jpg", Assert.Single(FileHistoryManager.PinnedEntries).Path);
    }

    [Fact]
    public void Rename_OntoAListedPath_MergesThemInTheNewerPlace_PinnedIfEitherWas()
    {
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Pin("/path/a.jpg");
        FileHistoryManager.Add("/path/b.jpg");
        FileHistoryManager.Add("/path/c.jpg");

        FileHistoryManager.Rename("/path/a.jpg", "/path/c.jpg");

        Assert.Equal(["/path/b.jpg", "/path/c.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
        Assert.True(FileHistoryManager.AllEntries[1].IsPinned);
    }

    [Fact]
    public void Rename_IgnoresCase_AndNeverMatchesAPathThatOnlyContainsTheOld()
    {
        FileHistoryManager.Add("/Path/A.jpg");
        FileHistoryManager.Add("/path/a.jpg.bak");

        FileHistoryManager.Rename("/path/a.jpg", "/path/z.jpg");

        Assert.Equal(["/path/z.jpg", "/path/a.jpg.bak"], FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public void Rename_SameRenameTwice_ChangesNothingTheSecondTime()
    {
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Add("/path/b.jpg");
        FileHistoryManager.Rename("/path/a.jpg", "/path/z.jpg");

        FileHistoryManager.Rename("/path/a.jpg", "/path/z.jpg");

        Assert.Equal(["/path/z.jpg", "/path/b.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public void RenameFolder_MovesEveryEntryUnderTheFolder()
    {
        var x = Path.Combine(_tempRoot, "x");
        FileHistoryManager.Add(Path.Combine(x, "a.jpg"));
        FileHistoryManager.Add(Path.Combine(x, "sub", "b.jpg"));
        FileHistoryManager.Add(Path.Combine(_tempRoot, "xy", "c.jpg"));

        FileHistoryManager.RenameFolder(x, Path.Combine(_tempRoot, "y"));

        Assert.Equal(
            [Path.Combine(_tempRoot, "y", "a.jpg"), Path.Combine(_tempRoot, "y", "sub", "b.jpg"), Path.Combine(_tempRoot, "xy", "c.jpg")],
            FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public void RenameFolder_OntoListedPaths_MergesThemInTheNewerPlace_PinnedIfEitherWas()
    {
        var x = Path.Combine(_tempRoot, "x");
        var y = Path.Combine(_tempRoot, "y");
        FileHistoryManager.Add(Path.Combine(x, "a.jpg"));
        FileHistoryManager.Pin(Path.Combine(x, "a.jpg"));
        FileHistoryManager.Add(Path.Combine(y, "b.jpg"));
        FileHistoryManager.Add(Path.Combine(y, "a.jpg"));

        FileHistoryManager.RenameFolder(x, y);

        Assert.Equal([Path.Combine(y, "b.jpg"), Path.Combine(y, "a.jpg")], FileHistoryManager.AllEntries.Select(e => e.Path));
        Assert.True(FileHistoryManager.AllEntries[1].IsPinned);
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
    public async Task Add_NewPathAfterUnpinningAtTheCap_KeepsTheNewestViewsAtTheCapOnceSavedAndLoaded()
    {
        FileHistoryManager.Add("/path/pinned.jpg");
        FileHistoryManager.Pin("/path/pinned.jpg");
        for (var i = 1; i <= 50; i++)
        {
            FileHistoryManager.Add($"/path/{i}.jpg");
        }
        // Unpinning one leaves one more unpinned entry than the cap
        FileHistoryManager.UnPin("/path/pinned.jpg");

        FileHistoryManager.Add("/path/newest.jpg");
        await FileHistoryManager.SaveToFileAsync();
        FileHistoryManager.Initialize(_configPath);

        var paths = FileHistoryManager.AllEntries.Select(e => e.Path).ToArray();
        Assert.Equal(50, paths.Length);
        Assert.Equal("/path/2.jpg", paths[0]);
        Assert.Equal("/path/newest.jpg", paths[^1]);
    }

    [Fact]
    public async Task SaveToFileAsync_MoreUnpinnedEntriesThanTheCap_KeepsTheNewestInTheirOrder()
    {
        FileHistoryManager.Add("/path/pinned.jpg");
        FileHistoryManager.Pin("/path/pinned.jpg");
        for (var i = 1; i <= 50; i++)
        {
            FileHistoryManager.Add($"/path/{i}.jpg");
        }
        FileHistoryManager.UnPin("/path/pinned.jpg");

        await FileHistoryManager.SaveToFileAsync();
        FileHistoryManager.Initialize(_configPath);

        Assert.Equal(Enumerable.Range(1, 50).Select(i => $"/path/{i}.jpg"), FileHistoryManager.AllEntries.Select(e => e.Path));
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

    // App Start shows the launch file before the history file has loaded, so the next tests add before Initialize

    [Fact]
    public async Task Initialize_AfterPathsWereAddedBeforeTheLoad_ListsThemLast()
    {
        await SaveFileListingAsync("/path/one.jpg", "/path/two.jpg");
        FileHistoryManager.Add("/path/early1.jpg");
        FileHistoryManager.Add("/path/early2.jpg");

        FileHistoryManager.Initialize(_configPath);

        Assert.Equal(["/path/one.jpg", "/path/two.jpg", "/path/early1.jpg", "/path/early2.jpg"],
            FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public async Task Initialize_EarlyPathTheFileAlreadyLists_MovesItToTheNewestEnd()
    {
        await SaveFileListingAsync("/path/a.jpg", "/path/b.jpg", "/path/c.jpg");
        FileHistoryManager.Add("/PATH/A.JPG");

        FileHistoryManager.Initialize(_configPath);

        Assert.Equal(["/path/b.jpg", "/path/c.jpg", "/path/a.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public async Task Initialize_EarlyPathThatIsPinned_KeepsItsPlace()
    {
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Add("/path/b.jpg");
        FileHistoryManager.Pin("/path/a.jpg");
        await FileHistoryManager.SaveToFileAsync();
        FileHistoryManager.Unload();
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Add("/path/early.jpg");

        FileHistoryManager.Initialize(_configPath);

        Assert.Equal(["/path/a.jpg", "/path/b.jpg", "/path/early.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
        Assert.Equal("/path/a.jpg", Assert.Single(FileHistoryManager.PinnedEntries).Path);
    }

    [Fact]
    public async Task Initialize_EarlyPathWhenTheFileListsTheMost_EvictsTheOldestEntry()
    {
        await SaveFileListingAsync(Enumerable.Range(1, 50).Select(i => $"/path/{i}.jpg").ToArray());
        FileHistoryManager.Add("/path/early.jpg");

        FileHistoryManager.Initialize(_configPath);

        var paths = FileHistoryManager.AllEntries.Select(e => e.Path).ToArray();
        Assert.Equal(50, paths.Length);
        Assert.Equal("/path/2.jpg", paths[0]);
        Assert.Equal("/path/early.jpg", paths[^1]);
    }

    [Fact]
    public void Initialize_EarlyPathAddedAgain_ListsItOnceAtItsNewestPlace()
    {
        FileHistoryManager.Unload();
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Add("/path/b.jpg");
        FileHistoryManager.Add("/PATH/A.JPG");

        FileHistoryManager.Initialize(_configPath);

        Assert.Equal(["/path/b.jpg", "/path/a.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public void Initialize_MoreEarlyPathsThanTheCap_ListsTheNewest()
    {
        FileHistoryManager.Unload();
        for (var i = 1; i <= 120; i++)
        {
            FileHistoryManager.Add($"/path/{i}.jpg");
        }

        FileHistoryManager.Initialize(_configPath);

        var paths = FileHistoryManager.AllEntries.Select(e => e.Path).ToArray();
        Assert.Equal(50, paths.Length);
        Assert.Equal("/path/71.jpg", paths[0]);
        Assert.Equal("/path/120.jpg", paths[^1]);
    }

    [Fact]
    public void Initialize_ASecondTime_DoesNotCarryOverTheFirstOnesEntries()
    {
        FileHistoryManager.Unload();
        FileHistoryManager.Add("/path/early.jpg");
        FileHistoryManager.Initialize(_configPath);
        Assert.Equal(["/path/early.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
        FileHistoryManager.Add("/path/later.jpg");

        FileHistoryManager.Initialize(_configPath);

        Assert.Empty(FileHistoryManager.AllEntries);
    }

    [Fact]
    public void Initialize_PathAddedBeforeTheLoadWithFileHistoryOff_ListsNothing()
    {
        FileHistoryManager.Unload();
        Settings.Navigation.IsFileHistoryEnabled = false;
        FileHistoryManager.Add("/path/early.jpg");
        Settings.Navigation.IsFileHistoryEnabled = true;

        FileHistoryManager.Initialize(_configPath);

        Assert.Empty(FileHistoryManager.AllEntries);
    }

    [Fact]
    public async Task SaveToFileAsync_BeforeTheLoad_DoesNotSaveThePathsWaitingForIt()
    {
        FileHistoryManager.Unload();
        FileHistoryManager.Add("/path/early.jpg");

        await FileHistoryManager.SaveToFileAsync();

        FileHistoryManager.Unload();
        FileHistoryManager.Initialize(_configPath);
        Assert.Empty(FileHistoryManager.AllEntries);
    }

    // A rename in the first moments of start-up must not be undone when the history file loads

    [Fact]
    public void Rename_PathAddedBeforeTheLoad_ListsItUnderTheNewNameWhenTheLoadFinishes()
    {
        FileHistoryManager.Unload();
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Add("/path/b.jpg");

        FileHistoryManager.Rename("/PATH/A.JPG", "/path/z.jpg");
        FileHistoryManager.Initialize(_configPath);

        Assert.Equal(["/path/z.jpg", "/path/b.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public void Rename_PathAddedBeforeTheLoad_OntoAnotherWaitingPath_ListsItOnceInTheNewerPlace()
    {
        FileHistoryManager.Unload();
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Add("/path/b.jpg");
        FileHistoryManager.Add("/path/c.jpg");

        FileHistoryManager.Rename("/path/a.jpg", "/path/c.jpg");
        FileHistoryManager.Initialize(_configPath);

        Assert.Equal(["/path/b.jpg", "/path/c.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public void RenameFolder_PathsAddedBeforeTheLoad_ListsThemUnderTheNewFolderWhenTheLoadFinishes()
    {
        var x = Path.Combine(_tempRoot, "x");
        FileHistoryManager.Unload();
        FileHistoryManager.Add(Path.Combine(x, "a.jpg"));
        FileHistoryManager.Add(Path.Combine(_tempRoot, "xy", "b.jpg"));

        FileHistoryManager.RenameFolder(x, Path.Combine(_tempRoot, "y"));
        FileHistoryManager.Initialize(_configPath);

        Assert.Equal([Path.Combine(_tempRoot, "y", "a.jpg"), Path.Combine(_tempRoot, "xy", "b.jpg")],
            FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public void Add_TheNewestPathAgain_LeavesTheVersion()
    {
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Add("/path/b.jpg");
        var version = FileHistoryManager.Version;

        FileHistoryManager.Add("/PATH/B.JPG");

        Assert.Equal(version, FileHistoryManager.Version);
    }

    [Theory]
    [InlineData("add new")]
    [InlineData("add older")]
    [InlineData("pin")]
    [InlineData("unpin")]
    [InlineData("remove")]
    [InlineData("clear")]
    [InlineData("rename")]
    [InlineData("rename folder")]
    [InlineData("sort")]
    [InlineData("load")]
    public void Change_ToTheList_RaisesTheVersion(string change)
    {
        var x = Path.Combine(_tempRoot, "x");
        FileHistoryManager.Add(Path.Combine(x, "a.jpg"));
        FileHistoryManager.Add(Path.Combine(x, "b.jpg"));
        FileHistoryManager.Pin(Path.Combine(x, "b.jpg"));
        var version = FileHistoryManager.Version;

        switch (change)
        {
            case "add new": FileHistoryManager.Add(Path.Combine(x, "c.jpg")); break;
            case "add older": FileHistoryManager.Add(Path.Combine(x, "a.jpg")); break;
            case "pin": FileHistoryManager.Pin(Path.Combine(x, "a.jpg")); break;
            case "unpin": FileHistoryManager.UnPin(Path.Combine(x, "b.jpg")); break;
            case "remove": FileHistoryManager.Remove(Path.Combine(x, "a.jpg")); break;
            case "clear": FileHistoryManager.Clear(); break;
            case "rename": FileHistoryManager.Rename(Path.Combine(x, "a.jpg"), Path.Combine(x, "z.jpg")); break;
            case "rename folder": FileHistoryManager.RenameFolder(x, Path.Combine(_tempRoot, "y")); break;
            case "sort": FileHistoryManager.IsSortingDescending = !FileHistoryManager.IsSortingDescending; break;
            case "load": FileHistoryManager.Initialize(_configPath); break;
        }

        Assert.NotEqual(version, FileHistoryManager.Version);
    }

    [Fact]
    public void Change_ThatChangesNothing_LeavesTheVersion()
    {
        FileHistoryManager.Add("/path/a.jpg");
        var version = FileHistoryManager.Version;

        FileHistoryManager.Pin("/path/missing.jpg");
        FileHistoryManager.Remove("/path/missing.jpg");
        FileHistoryManager.Rename("/path/missing.jpg", "/path/z.jpg");
        FileHistoryManager.IsSortingDescending = FileHistoryManager.IsSortingDescending;

        Assert.Equal(version, FileHistoryManager.Version);
    }

    /// <summary>
    /// Saves a file that lists <paramref name="paths"/>, then goes back to before the first load.
    /// </summary>
    private static async Task SaveFileListingAsync(params string[] paths)
    {
        foreach (var path in paths)
        {
            FileHistoryManager.Add(path);
        }

        await FileHistoryManager.SaveToFileAsync();
        FileHistoryManager.Unload();
    }
}
