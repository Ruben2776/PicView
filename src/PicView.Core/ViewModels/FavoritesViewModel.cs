using ObservableCollections;
using PicView.Core.Favorites;
using R3;

namespace PicView.Core.ViewModels;

public class FavoritesViewModel : IDisposable
{
    private readonly MainWindowViewModel _vm;

    public ObservableList<FavoritesEntryViewModel> Entries { get; } = [];

    public ReactiveCommand ClearFavoritesCommand { get; }
    public ReactiveCommand OpenFavoritesFileCommand { get; }
    public ReactiveCommand AddFavoritesDialogCommand { get; }
    public ReactiveCommand AddFromFolderCommand { get; }
    public ReactiveCommand<FavoriteEntry> EditFavoritesDialogCommand { get; } = new();

    public FavoritesViewModel(MainWindowViewModel vm)
    {
        _vm = vm;
        ClearFavoritesCommand = new ReactiveCommand(ClearFavorites);
        OpenFavoritesFileCommand = new ReactiveCommand(OpenFavoritesFile);
        AddFavoritesDialogCommand = new ReactiveCommand();
        AddFromFolderCommand = new ReactiveCommand();
    }

    private async ValueTask OpenFavoritesFile(Unit arg1, CancellationToken arg2)
    {
        await _vm.Mapper.ShowFavoritesFile().ConfigureAwait(false);
    }

    private async ValueTask ClearFavorites(Unit arg1, CancellationToken arg2)
    {
        await FavoritesManager.ClearAsync().ConfigureAwait(false);
        UpdateFavorites();
    }

    /// <summary>
    /// Adds <paramref name="path"/> to the favorites, or leaves it as it is when it already is one.
    /// </summary>
    /// <returns>The favorite's slot, or <c>null</c> when it has none.</returns>
    public async ValueTask<int?> AddFavorite(string path)
    {
        var slot = await FavoritesManager.AddAsync(path).ConfigureAwait(false);
        UpdateFavorites();
        return slot;
    }
    
    public void UpdateFavorites()
    {
        foreach (var existing in Entries)
        {
            existing.Dispose();
        }
        Entries.Clear();

        var entries = FavoritesManager.AllEntries;
        var currentDirectoryPath = _vm.WindowTabs?.ActiveTab?.Value?.Model?.FileInfo?.DirectoryName;
        var normalizedCurrentDir = !string.IsNullOrWhiteSpace(currentDirectoryPath)
            ? Path.TrimEndingDirectorySeparator(currentDirectoryPath)
            : null;

        foreach (var entry in entries)
        {
            var normalizedEntryPath = Path.TrimEndingDirectorySeparator(entry.FilePath);
            var folderName = Path.GetFileName(normalizedEntryPath);
            if (string.IsNullOrEmpty(folderName))
            {
                folderName = normalizedEntryPath;
            }

            var isCurrentItem = normalizedCurrentDir is not null &&
                                string.Equals(normalizedEntryPath, normalizedCurrentDir, StringComparison.Ordinal);

            var entryVm = new FavoritesEntryViewModel();
            entryVm.Initialize(
                normalizedEntryPath,
                folderName,
                entry.Slot,
                isCurrentItem,
                _vm);
            Entries.Add(entryVm);
        }
    }

    public void Dispose()
    {
        ClearFavoritesCommand.Dispose();
        OpenFavoritesFileCommand.Dispose();
        AddFavoritesDialogCommand.Dispose();
        AddFromFolderCommand.Dispose();
        EditFavoritesDialogCommand.Dispose();

        foreach (var existing in Entries)
        {
            existing.Dispose();
        }
        Entries.Clear();

        GC.SuppressFinalize(this);
    }
}
