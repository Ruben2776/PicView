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

    private class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
