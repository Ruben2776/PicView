using PicView.Core.FileHistory;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.FileHistory;

[Collection("Sequential")]
public class FileHistoryViewModelTests : IDisposable
{
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PicViewHistoryVm_" + Guid.NewGuid().ToString("N"));
    private readonly MainWindowViewModel _window;
    private readonly FileHistoryViewModel _history;

    public FileHistoryViewModelTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        Settings.Navigation.IsFileHistoryEnabled = true;
        Directory.CreateDirectory(_folder);
        FileHistoryManager.Initialize(Path.Combine(_folder, "history.json"));
        FileHistoryManager.IsSortingDescending = false;
        _window = new MainWindowViewModel(null!, null!, null!, null!);
        _history = new FileHistoryViewModel(_window);
    }

    public void Dispose()
    {
        FileHistoryManager.IsSortingDescending = false;
        Directory.Delete(_folder, true);
        _fileHistoryBackup.Dispose();
    }

    [Fact]
    public void UpdateHistory_AgainWithNothingChanged_ChangesNoEntry()
    {
        Add("a.jpg", "b.jpg");
        _history.UpdateHistory();
        var before = _history.Entries.ToArray();
        var changes = CountChanges();

        _history.UpdateHistory();

        Assert.Equal(0, changes());
        Assert.Equal(before, _history.Entries);
    }

    [Fact]
    public void UpdateHistory_AfterViewingAListedFile_MovesItsEntryAndKeepsTheOthers()
    {
        Add("a.jpg", "b.jpg", "c.jpg");
        _history.UpdateHistory();
        var a = EntryFor("a.jpg");
        var b = EntryFor("b.jpg");
        var c = EntryFor("c.jpg");

        Add("a.jpg");
        _history.UpdateHistory();

        Assert.Equal([a, c, b], _history.Entries);
        Assert.Equal([3, 2, 1], _history.Entries.Select(e => e.Index.CurrentValue));
    }

    [Fact]
    public void UpdateHistory_AfterViewingANewFile_InsertsOneEntry()
    {
        Add("a.jpg", "b.jpg");
        _history.UpdateHistory();
        var a = EntryFor("a.jpg");
        var b = EntryFor("b.jpg");
        var changes = CountChanges();

        Add("c.jpg");
        _history.UpdateHistory();

        Assert.Equal(1, changes());
        Assert.Equal(PathOf("c.jpg"), _history.Entries[0].FilePath.CurrentValue);
        Assert.Same(b, _history.Entries[1]);
        Assert.Same(a, _history.Entries[2]);
    }

    [Fact]
    public void UpdateHistory_AtTheCap_RemovesAndDisposesTheOldest()
    {
        for (var i = 0; i < 50; i++)
        {
            Add($"{i}.jpg");
        }
        _history.UpdateHistory();
        var oldest = EntryFor("0.jpg");

        Add("new.jpg");
        _history.UpdateHistory();

        Assert.Equal(50, _history.Entries.Count);
        Assert.DoesNotContain(oldest, _history.Entries);
        Assert.True(oldest.FilePath.IsDisposed);
    }

    [Fact]
    public void UpdateHistory_AfterPinning_AddsAPinnedEntryAndKeepsTheRecentOnes()
    {
        Add("a.jpg", "b.jpg");
        _history.UpdateHistory();
        var before = _history.Entries.ToArray();

        FileHistoryManager.Pin(PathOf("a.jpg"));
        _history.UpdateHistory();

        // A pinned file is listed under Pinned and still among the recent files
        var pinned = Assert.Single(_history.PinnedEntries);
        Assert.Equal(PathOf("a.jpg"), pinned.FilePath.CurrentValue);
        Assert.True(pinned.IsPinned.CurrentValue);
        Assert.True(_history.HasPinnedEntries.CurrentValue);
        Assert.Equal(before, _history.Entries);
    }

    [Fact]
    public void UpdateHistory_AfterUnpinning_EmptiesThePinnedList()
    {
        Add("a.jpg");
        FileHistoryManager.Pin(PathOf("a.jpg"));
        _history.UpdateHistory();
        var pinned = Assert.Single(_history.PinnedEntries);

        FileHistoryManager.UnPin(PathOf("a.jpg"));
        _history.UpdateHistory();

        Assert.Empty(_history.PinnedEntries);
        Assert.False(_history.HasPinnedEntries.CurrentValue);
        Assert.True(pinned.FilePath.IsDisposed);
    }

    [Fact]
    public void UpdateHistory_AfterTogglingSort_ReversesTheOrderWithTheSameEntries()
    {
        Add("a.jpg", "b.jpg", "c.jpg");
        _history.UpdateHistory();
        var before = _history.Entries.ToArray();

        _history.ToggleSortCommand.Execute(Unit.Default);

        Assert.Equal(before.Reverse(), _history.Entries);
    }

    [Fact]
    public void UpdateHistory_WhenOnlyTheCurrentFileChanged_OnlyMovesTheMark()
    {
        var tab = _window.WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg"));
        tab.Display(Model("b.jpg"));
        Add("a.jpg");
        _history.UpdateHistory();
        var before = _history.Entries.ToArray();
        var changes = CountChanges();

        // a.jpg is the newest entry already, so showing it changes the history's order not at all
        tab.Display(Model("a.jpg"));
        _history.UpdateHistory();

        Assert.Equal(0, changes());
        Assert.Equal(before, _history.Entries);
        Assert.True(EntryFor("a.jpg").IsCurrentItem.CurrentValue);
        Assert.False(EntryFor("b.jpg").IsCurrentItem.CurrentValue);
    }

    [Fact]
    public void UpdateHistory_WithHistoryTurnedOff_EmptiesTheLists()
    {
        Add("a.jpg");
        _history.UpdateHistory();

        Settings.Navigation.IsFileHistoryEnabled = false;
        _history.UpdateHistory();

        Assert.Empty(_history.Entries);
    }

    private void Add(params string[] names)
    {
        foreach (var name in names)
        {
            FileHistoryManager.Add(PathOf(name));
        }
    }

    private FileHistoryEntryViewModel EntryFor(string name) =>
        _history.Entries.Single(e => e.FilePath.CurrentValue == PathOf(name));

    private Func<int> CountChanges()
    {
        var count = 0;
        _history.Entries.CollectionChanged += (in _) => count++;
        _history.PinnedEntries.CollectionChanged += (in _) => count++;
        return () => count;
    }

    private string PathOf(string name) => Path.Combine(_folder, name);

    private ImageModel Model(string name) => new() { FileInfo = new FileInfo(PathOf(name)), Image = new object() };

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
