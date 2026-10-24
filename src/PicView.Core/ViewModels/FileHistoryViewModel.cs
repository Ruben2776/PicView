using ObservableCollections;
using PicView.Core.FileHistory;
using R3;
using ZLinq;
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
        await _vm.Mapper.Run(Actions.ShowRecentHistoryFile).ConfigureAwait(false);
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
        foreach (var existing in Entries)
        {
            existing.Dispose();
        }
        Entries.Clear();

        foreach (var existing in PinnedEntries)
        {
            existing.Dispose();
        }
        PinnedEntries.Clear();
        
        if (!Settings.Navigation.IsFileHistoryEnabled)
        {
            HasPinnedEntries.Value = false;
            return;
        }

        var pinnedEntries = FileHistoryManager.PinnedEntries;
        var entries = FileHistoryManager.AllEntries;
        
        HasPinnedEntries.Value = pinnedEntries.Any();
        
        var currentFilePath = _vm.WindowTabs?.ActiveTab?.Value?.Model?.FileInfo?.FullName;
        
        foreach (var entry in pinnedEntries)
        {
            var fileName = Path.GetFileName(entry.Path);
            var pinnedEntry = new FileHistoryEntryViewModel();
            var isCurrentItem = currentFilePath is not null && string.Equals(entry.Path, currentFilePath, StringComparison.Ordinal);
            pinnedEntry.Initialize(
                entry.Path, 
                fileName, 
                true, 
                isCurrentItem,
                -1, 
                _vm);
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
                var isCurrentItem = currentFilePath is not null && string.Equals(path, currentFilePath, StringComparison.Ordinal);
                entry.Initialize(
                    path, 
                    fileName, 
                    false, 
                    isCurrentItem,
                    index, 
                    _vm);
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
                
                var isCurrentItem = currentFilePath is not null && string.Equals(entry.Path, currentFilePath, StringComparison.Ordinal);
                unpinnedEntry.Initialize(
                    entry.Path, 
                    fileName, 
                    false, 
                    isCurrentItem,
                    index,
                    _vm);
                Entries.Add(unpinnedEntry);
            }
        }
    }
}
