using ObservableCollections;
using PicView.Core.FileHistory;
using R3;
using ZLinq;

namespace PicView.Core.ViewModels;

public class FileHistoryViewModel
{
    private readonly CoreViewModel _core;

    public ObservableList<FileHistoryEntryViewModel> PinnedEntries { get; } = [];
    public BindableReactiveProperty<bool> HasPinnedEntries { get; } = new(false);
    public ObservableList<FileHistoryEntryViewModel> Entries { get; } = [];

    public ReactiveCommand ClearHistoryCommand { get; }
    public ReactiveCommand ToggleSortCommand { get; }
    public ReactiveCommand OpenFileHistoryCommand { get; }

    public FileHistoryViewModel(CoreViewModel core)
    {
        _core = core;
        ClearHistoryCommand = new ReactiveCommand(ClearHistory);
        ToggleSortCommand = new ReactiveCommand(ToggleSort);
        OpenFileHistoryCommand = new ReactiveCommand(OpenFileHistory);
    }

    private async ValueTask OpenFileHistory(Unit arg1, CancellationToken arg2)
    {
        await _core.MainWindows.ActiveWindow.CurrentValue.Mapper.ShowRecentHistoryFile().ConfigureAwait(false);
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

    public void UpdateHistory()
    {
        Entries.Clear();
        PinnedEntries.Clear();
        
        if (!Settings.Navigation.IsFileHistoryEnabled)
        {
            return;
        }

        var pinnedEntries = FileHistoryManager.PinnedEntries;
        var entries = FileHistoryManager.AllEntries;
        
        HasPinnedEntries.Value = pinnedEntries.Any();
        
        var currentFilePath = _core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.Value?.Model?.FileInfo?.FullName;
        
        foreach (var entry in pinnedEntries)
        {
            var fileName = Path.GetFileName(entry.Path);
            var pinnedEntry = new FileHistoryEntryViewModel();
            var isCurrentItem = currentFilePath is not null && entry.Path == currentFilePath;
            pinnedEntry.Initialize(
                entry.Path, 
                fileName, 
                true, 
                isCurrentItem,
                -1, 
                _core);
            PinnedEntries.Add(pinnedEntry);
        }

        var count = entries.Count;
        if (FileHistoryManager.IsSortingDescending)
        {
            for (var i = 0; i < count; i++)
            {
                var path = entries.ElementAt(i).Path;
                var index = i + 1;
                var fileName = Path.GetFileName(path);
                var entry = new FileHistoryEntryViewModel();
                var isCurrentItem = currentFilePath is not null && path == currentFilePath;
                entry.Initialize(
                    path, 
                    fileName, 
                    false, 
                    isCurrentItem,
                    index, 
                    _core);
                Entries.Add(entry);
            }
        }
        else
        {
            for (var i = count - 1; i >= 0; i--)
            {
                var entry = entries.ElementAt(i);
                var index = i + 1;
                var fileName = Path.GetFileName(entry.Path);
                var unpinnedEntry = new FileHistoryEntryViewModel();
                
                var isCurrentItem = currentFilePath is not null && entry.Path == currentFilePath;
                unpinnedEntry.Initialize(
                    entry.Path, 
                    fileName, 
                    false, 
                    isCurrentItem,
                    index,
                    _core);
                Entries.Add(unpinnedEntry);
            }
        }
    }
}
