using System.IO.Compression;
using PicView.Core.FileHistory;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using PicView.Tests.FileHistory;
using R3;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class FileRenamesTests : IDisposable
{
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PicViewRenames_" + Guid.NewGuid().ToString("N"));
    private readonly CoreViewModel _core;

    public FileRenamesTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        Settings.Navigation.IsFileHistoryEnabled = true;
        Settings.StartUp.LastFile = null;
        Directory.CreateDirectory(_folder);
        FileHistoryManager.Initialize(Path.Combine(_folder, "history.json"));
        _core = new CoreViewModel(null, file => ValueTask.FromResult(new ImageModel { FileInfo = file }));
    }

    public void Dispose()
    {
        // So the hand-edited history file of one test is not what a test of another class finds loaded
        FileHistoryManager.Unload();
        Directory.Delete(_folder, true);
        _fileHistoryBackup.Dispose();
    }

    [Fact]
    public void Follow_ReachesFileHistoryEveryTabInEveryMainWindowAndTheLastFile()
    {
        var first = OpenWindow().WindowTabs.ActiveTab.Value;
        var second = OpenWindow().WindowTabs.ActiveTab.Value;
        first.Display(Model(PathOf("a.jpg")));
        second.Display(Model(PathOf("a.jpg")));

        FileRenames.Follow(_core, PathOf("a.jpg"), PathOf("b.jpg"));

        Assert.Equal(PathOf("b.jpg"), Assert.Single(first.ViewHistory.Entries).Path);
        Assert.Equal(PathOf("b.jpg"), Assert.Single(second.ViewHistory.Entries).Path);
        Assert.Equal(PathOf("b.jpg"), Assert.Single(FileHistoryManager.AllEntries).Path);
        Assert.Equal(PathOf("b.jpg"), Settings.StartUp.LastFile);
    }

    [Fact]
    public void Follow_ReachesEveryTabOfAMainWindow_NotOnlyTheActiveOne()
    {
        var window = OpenWindow();
        var first = window.WindowTabs.ActiveTab.Value;
        var second = window.WindowTabs.CreateTab();
        first.Display(Model(PathOf("a.jpg")));
        second.Display(Model(PathOf("a.jpg")));

        FileRenames.Follow(_core, PathOf("a.jpg"), PathOf("b.jpg"));

        Assert.Equal(PathOf("b.jpg"), Assert.Single(first.ViewHistory.Entries).Path);
        Assert.Equal(PathOf("b.jpg"), Assert.Single(second.ViewHistory.Entries).Path);
    }

    [Fact]
    public void Follow_Archive_ReachesItsFileHistoryEntryItsPagesInTheTabAndTheLastFile()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.ViewHistory.Record(ViewHistoryEntry.ForArchivePage(PathOf("a.zip"), "1.jpg"));
        tab.ViewHistory.Record(ViewHistoryEntry.ForArchivePage(PathOf("a.zip"), "2.jpg"));
        FileHistoryManager.Add(PathOf("a.zip"));
        Settings.StartUp.LastFile = PathOf("a.zip");

        FileRenames.Follow(_core, PathOf("a.zip"), PathOf("b.zip"));

        Assert.Equal(["1.jpg", "2.jpg"], tab.ViewHistory.Entries.Select(e => e.ArchiveEntry));
        Assert.All(tab.ViewHistory.Entries, e => Assert.Equal(PathOf("b.zip"), e.Path));
        Assert.Equal(PathOf("b.zip"), Assert.Single(FileHistoryManager.AllEntries).Path);
        Assert.Equal(PathOf("b.zip"), Settings.StartUp.LastFile);
    }

    [Fact]
    public async Task Follow_ArchiveATabIsReading_RecordsItsNextPageUnderTheNewName()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        var oldZip = CreateZip(PathOf("old.zip"), "1.jpg", "2.jpg");
        var newZip = PathOf("new.zip");
        try
        {
            var (first, second) = await ExtractTwoPagesAsync(tab, oldZip);
            tab.Display(Model(first));

            FileRenames.Follow(_core, oldZip, newZip);
            tab.Display(Model(second));

            Assert.Equal([ViewHistoryEntry.ForArchivePage(newZip, "1.jpg"), ViewHistoryEntry.ForArchivePage(newZip, "2.jpg")],
                tab.ViewHistory.Entries);
            Assert.Equal([newZip], FileHistoryManager.AllEntries.Select(e => e.Path));
            Assert.Equal(newZip, Settings.StartUp.LastFile);
            Assert.Equal(newZip, tab.Archives.OpenArchive);
        }
        finally
        {
            tab.Archives.Cleanup();
        }
    }

    [Fact]
    public async Task FollowFolder_FolderOfAnArchiveATabIsReading_RecordsItsNextPageInTheNewFolder()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        var oldZip = CreateZip(Path.Combine(_folder, "x", "a.zip"), "1.jpg", "2.jpg");
        var newZip = Path.Combine(_folder, "y", "a.zip");
        try
        {
            var (first, second) = await ExtractTwoPagesAsync(tab, oldZip);
            tab.Display(Model(first));

            FileRenames.FollowFolder(_core, Path.Combine(_folder, "x"), Path.Combine(_folder, "y"));
            tab.Display(Model(second));

            Assert.Equal([ViewHistoryEntry.ForArchivePage(newZip, "1.jpg"), ViewHistoryEntry.ForArchivePage(newZip, "2.jpg")],
                tab.ViewHistory.Entries);
            Assert.Equal([newZip], FileHistoryManager.AllEntries.Select(e => e.Path));
            Assert.Equal(newZip, tab.Archives.OpenArchive);
        }
        finally
        {
            tab.Archives.Cleanup();
        }
    }

    [Fact]
    public void FollowFolder_ReachesFileHistoryTheTabsAndTheLastFile()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.Display(Model(Path.Combine(_folder, "x", "a.jpg")));

        FileRenames.FollowFolder(_core, Path.Combine(_folder, "x"), Path.Combine(_folder, "y"));

        var moved = Path.Combine(_folder, "y", "a.jpg");
        Assert.Equal(moved, Assert.Single(tab.ViewHistory.Entries).Path);
        Assert.Equal(moved, Assert.Single(FileHistoryManager.AllEntries).Path);
        Assert.Equal(moved, Settings.StartUp.LastFile);
    }

    [Fact]
    public void Follow_WithoutACore_StillRenamesFileHistoryAndTheLastFile()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.Display(Model(PathOf("a.jpg")));

        FileRenames.Follow(null, PathOf("a.jpg"), PathOf("b.jpg"));

        Assert.Equal(PathOf("b.jpg"), Assert.Single(FileHistoryManager.AllEntries).Path);
        Assert.Equal(PathOf("b.jpg"), Settings.StartUp.LastFile);
        Assert.Equal(PathOf("a.jpg"), Assert.Single(tab.ViewHistory.Entries).Path);
    }

    [Fact]
    public void FollowFolder_WithoutACore_StillRenamesFileHistoryAndTheLastFile()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        var shown = Path.Combine(_folder, "x", "a.jpg");
        tab.Display(Model(shown));

        FileRenames.FollowFolder(null, Path.Combine(_folder, "x"), Path.Combine(_folder, "y"));

        var moved = Path.Combine(_folder, "y", "a.jpg");
        Assert.Equal(moved, Assert.Single(FileHistoryManager.AllEntries).Path);
        Assert.Equal(moved, Settings.StartUp.LastFile);
        Assert.Equal(shown, Assert.Single(tab.ViewHistory.Entries).Path);
    }

    [Fact]
    public void Follow_SameRenameTwice_ChangesNothingTheSecondTime()
    {
        // As when the folder watcher reports a rename that RenameHelper applied already. A rename onto a listed path
        // merges entries, which a second pass must not undo or repeat
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.Display(Model(PathOf("a.jpg")));
        tab.Display(Model(PathOf("b.jpg")));
        tab.Display(Model(PathOf("c.jpg")));
        FileHistoryManager.Pin(PathOf("a.jpg"));
        FileRenames.Follow(_core, PathOf("b.jpg"), PathOf("c.jpg"));
        var viewHistory = (tab.ViewHistory.Entries, tab.ViewHistory.Cursor);
        var fileHistory = FileHistoryManager.AllEntries.Select(e => (e.Path, e.IsPinned)).ToArray();
        var lastFile = Settings.StartUp.LastFile;

        FileRenames.Follow(_core, PathOf("b.jpg"), PathOf("c.jpg"));

        Assert.Equal(viewHistory.Entries, tab.ViewHistory.Entries);
        Assert.Equal(viewHistory.Cursor, tab.ViewHistory.Cursor);
        Assert.Equal(fileHistory, FileHistoryManager.AllEntries.Select(e => (e.Path, e.IsPinned)));
        Assert.Equal(lastFile, Settings.StartUp.LastFile);
    }

    [Theory]
    [InlineData("A.jpg", "b.jpg")]
    [InlineData("a.jpg.bak", "a.jpg.bak")]
    public void Follow_LastFile_FollowsOnlyThePathRenamed_IgnoringCase(string lastFile, string expected)
    {
        Settings.StartUp.LastFile = PathOf(lastFile);

        FileRenames.Follow(_core, PathOf("a.jpg"), PathOf("b.jpg"));

        Assert.Equal(PathOf(expected), Settings.StartUp.LastFile);
    }

    [Fact]
    public void FollowFolder_LastFileInAFolderThatOnlyStartsWithTheOldName_LeavesIt()
    {
        var lastFile = Path.Combine(_folder, "xy", "a.jpg");
        Settings.StartUp.LastFile = lastFile;

        FileRenames.FollowFolder(_core, Path.Combine(_folder, "x"), Path.Combine(_folder, "y"));

        Assert.Equal(lastFile, Settings.StartUp.LastFile);
    }

    [Fact]
    public void FollowFolder_FileHistoryCannotMoveAnEntry_StillReachesTheTabsAndTheLastFile()
    {
        // A hand-edited FileHistory.json can list an entry with no path, which File History can't move to a folder
        var edited = Path.Combine(_folder, "edited.json");
        File.WriteAllText(edited, """{"Entries":[{"Path":null}]}""");
        FileHistoryManager.Initialize(edited);
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.Display(Model(Path.Combine(_folder, "x", "a.jpg")));

        FileRenames.FollowFolder(_core, Path.Combine(_folder, "x"), Path.Combine(_folder, "y"));

        var moved = Path.Combine(_folder, "y", "a.jpg");
        Assert.Equal(moved, Assert.Single(tab.ViewHistory.Entries).Path);
        Assert.Equal(moved, Settings.StartUp.LastFile);
    }

    [Fact]
    public void Follow_AWindowThatFails_StillRenamesFileHistoryAndTheLastFile()
    {
        // The lists of windows and tabs are copied while tabs open and close, and the copy can fail; a window that is
        // null stands in for that
        OpenWindow().WindowTabs.ActiveTab.Value.Display(Model(PathOf("a.jpg")));
        _core.MainWindows.MainWindows.Add(null!);

        FileRenames.Follow(_core, PathOf("a.jpg"), PathOf("b.jpg"));

        Assert.Equal(PathOf("b.jpg"), Assert.Single(FileHistoryManager.AllEntries).Path);
        Assert.Equal(PathOf("b.jpg"), Settings.StartUp.LastFile);
    }

    [Fact]
    public void FollowFolder_AWindowThatFails_StillRenamesFileHistoryAndTheLastFile()
    {
        OpenWindow().WindowTabs.ActiveTab.Value.Display(Model(Path.Combine(_folder, "x", "a.jpg")));
        _core.MainWindows.MainWindows.Add(null!);

        FileRenames.FollowFolder(_core, Path.Combine(_folder, "x"), Path.Combine(_folder, "y"));

        var moved = Path.Combine(_folder, "y", "a.jpg");
        Assert.Equal(moved, Assert.Single(FileHistoryManager.AllEntries).Path);
        Assert.Equal(moved, Settings.StartUp.LastFile);
    }

    private MainWindowViewModel OpenWindow()
    {
        var window = new MainWindowViewModel(_core, null!, null!, null!, null!);
        _core.MainWindows.MainWindows.Add(window);
        _core.MainWindows.ActiveWindow.Value = window;
        return window;
    }

    private string PathOf(string name) => Path.Combine(_folder, name);

    private static ImageModel Model(string path) => new() { FileInfo = new FileInfo(path), Image = new object() };

    // A zip at the path given that holds the images given, each a few bytes
    private string CreateZip(string zip, params string[] images)
    {
        var source = Directory.CreateDirectory(Path.Combine(_folder, "zip_" + Guid.NewGuid().ToString("N"))).FullName;
        foreach (var image in images)
        {
            File.WriteAllBytes(Path.Combine(source, image), [0xFF, 0xD8, 0xFF]);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(zip)!);
        ZipFile.CreateFromDirectory(source, zip);
        return zip;
    }

    // Opens the archive in the tab and extracts its two pages, as a tab reading it has once the start page shows
    private static async Task<(string First, string Second)> ExtractTwoPagesAsync(TabViewModel tab, string zip)
    {
        var opening = (await tab.Archives.OpenAsync(zip, null, (_, _) => Task.FromResult(false), string.CompareOrdinal,
            TestContext.Current.CancellationToken))!;
        opening.ExtractRest();
        await opening.Ended.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var pages = Directory.GetFiles(opening.Folder).Order(StringComparer.Ordinal).ToArray();
        return (pages[0], pages[1]);
    }

    private class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
