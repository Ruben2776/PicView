using ObservableCollections;
using PicView.Core.FileHistory;
using R3;
using PicView.Core.Keybindings;

namespace PicView.Core.ViewModels;

public class FileHistoryViewModel
{
    private readonly MainWindowViewModel _vm;

    public ObservableList<FileHistoryEntryViewModel> PinnedEntries { get; } = [];
    public BindableReactiveProperty<bool> HasPinnedEntries { get; } = new(false);
    public ObservableList<FileHistoryEntryViewModel> Entries { get; } = [];

    public ReactiveCommand ClearHistoryCommand { get; }
    public ReactiveCommand ToggleSortCommand { get; }
    public ReactiveCommand OpenFileHistoryCommand { get; }

    public FileHistoryViewModel(MainWindowViewModel vm)
    {
        _vm = vm;
        ClearHistoryCommand = new ReactiveCommand(ClearHistory);
        ToggleSortCommand = new ReactiveCommand(ToggleSort);
        OpenFileHistoryCommand = new ReactiveCommand(OpenFileHistory);
    }

    private async ValueTask OpenFileHistory(Unit arg1, CancellationToken arg2)
    {
        await _vm.ActionRunner.Run(Actions.ShowRecentHistoryFile).ConfigureAwait(false);
    }

    private void ToggleSort(Unit obj)
    {
        FileHistoryManager.IsSortingDescending = !FileHistoryManager.IsSortingDescending;
        UpdateHistory();
    }

    private void ClearHistory(Unit unit)
    {
        FileHistoryManager.Clear();
        UpdateHistory();
    }

    /// <summary>
    ///     Brings <see cref="PinnedEntries" /> and <see cref="Entries" /> in line with the file history. The entries that
    ///     are still listed are kept and moved to their new places rather than made again, so a view of them keeps its
    ///     layout, its scroll position and its thumbnails. When the history has not changed since the last call, only the
    ///     mark on the active tab's entry can move.
    /// </summary>
    public void UpdateHistory()
    {
        if (!Settings.Navigation.IsFileHistoryEnabled)
        {
            RemoveFrom(Entries, 0);
            RemoveFrom(PinnedEntries, 0);
            HasPinnedEntries.Value = false;
            _syncedVersion = NotSynced;
            return;
        }

        // What the active tab shows: its file, its archive or its URL, which is not always the file it has open
        var currentPath = _vm.WindowTabs?.ActiveTab?.Value?.ViewHistory.Current?.HistoryPath;

        // Read before the entries, so a change made while they are read is picked up by the next call
        var version = FileHistoryManager.Version;
        if (version == _syncedVersion)
        {
            if (!string.Equals(currentPath, _syncedCurrentPath, StringComparison.OrdinalIgnoreCase))
            {
                MarkCurrent(PinnedEntries, currentPath);
                MarkCurrent(Entries, currentPath);
                _syncedCurrentPath = currentPath;
            }

            return;
        }

        var pinnedEntries = FileHistoryManager.PinnedEntries;
        var entries = FileHistoryManager.AllEntries;

        Patch(PinnedEntries, pinnedEntries, false, true, currentPath);
        // The history lists the one viewed longest ago first, so the newest first is the history reversed
        Patch(Entries, entries, !FileHistoryManager.IsSortingDescending, false, currentPath);
        HasPinnedEntries.Value = pinnedEntries.Length > 0;

        _syncedVersion = version;
        _syncedCurrentPath = currentPath;
    }

    private const int NotSynced = -1;
    private int _syncedVersion = NotSynced;
    private string? _syncedCurrentPath;

    // Makes list show source, in source's order or reversed, reusing the entries of the paths it shows already. Viewing a
    // listed file comes out as one move, a new one as one insert and, at the cap, one removal
    private void Patch(ObservableList<FileHistoryEntryViewModel> list, Entry[] source, bool reversed, bool isPinned,
        string? currentPath)
    {
        // Drop the paths that went first, so those that stay only have to move past each other
        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (IndexOfPath(source, list[i].FilePath.CurrentValue) >= 0)
            {
                continue;
            }

            var gone = list[i];
            list.RemoveAt(i);
            gone.Dispose();
        }

        var count = source.Length;
        for (var target = 0; target < count; target++)
        {
            var sourceIndex = reversed ? count - 1 - target : target;
            var path = source[sourceIndex].Path;
            var index = isPinned ? -1 : sourceIndex + 1;
            var isCurrentItem = IsCurrent(path, currentPath);

            var listed = IndexOfPath(list, path, target);
            if (listed < 0)
            {
                var entry = new FileHistoryEntryViewModel();
                entry.Initialize(path, Path.GetFileName(path), isPinned, isCurrentItem, index, _vm);
                list.Insert(target, entry);
                continue;
            }

            if (listed != target)
            {
                list.Move(listed, target);
            }

            var kept = list[target];
            // A rename that only changed the case keeps the entry, so it takes the new spelling
            kept.FilePath.Value = path;
            kept.FileName.Value = Path.GetFileName(path);
            kept.IsPinned.Value = isPinned;
            kept.IsCurrentItem.Value = isCurrentItem;
            kept.Index.Value = index;
        }

        RemoveFrom(list, count);
    }

    private static void MarkCurrent(ObservableList<FileHistoryEntryViewModel> list, string? currentPath)
    {
        foreach (var entry in list)
        {
            entry.IsCurrentItem.Value = IsCurrent(entry.FilePath.CurrentValue, currentPath);
        }
    }

    // Removes and disposes the entries from start on
    private static void RemoveFrom(ObservableList<FileHistoryEntryViewModel> list, int start)
    {
        for (var i = list.Count - 1; i >= start; i--)
        {
            var gone = list[i];
            list.RemoveAt(i);
            gone.Dispose();
        }
    }

    private static bool IsCurrent(string path, string? currentPath) =>
        currentPath is not null && string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase);

    private static int IndexOfPath(Entry[] source, string path)
    {
        for (var i = 0; i < source.Length; i++)
        {
            if (string.Equals(source[i].Path, path, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    // The entries before start are placed already, so the search begins there
    private static int IndexOfPath(ObservableList<FileHistoryEntryViewModel> list, string path, int start)
    {
        for (var i = start; i < list.Count; i++)
        {
            if (string.Equals(list[i].FilePath.CurrentValue, path, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
