using ObservableCollections;
using PicView.Core.Favorites;
using R3;

namespace PicView.Core.ViewModels;

public class FavoritesViewModel
{
    private readonly MainWindowViewModel _vm;

    public ObservableList<FavoritesEntryViewModel> Entries { get; } = [];

    public ReactiveCommand ClearFavoritesCommand { get; }
    public ReactiveCommand OpenFavoritesFileCommand { get; }
    public ReactiveCommand AddFavoritesDialogCommand { get; }
    public ReactiveCommand AddFromFolderCommand { get; }

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

    public async ValueTask AddFavorite(string path, string hotKey = "")
    {
        await FavoritesManager.AddAsync(path, hotKey).ConfigureAwait(false);
        UpdateFavorites();
    }

    public async ValueTask AddFavorite(FavoriteEntry entry)
    {
        await FavoritesManager.AddAsync(entry).ConfigureAwait(false);
        UpdateFavorites();
    }

    public async ValueTask RemoveFavorite(string path)
    {
        await FavoritesManager.RemoveAsync(path).ConfigureAwait(false);
        UpdateFavorites();
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
                entry.HotKey,
                isCurrentItem,
                _vm);
            Entries.Add(entryVm);
        }
    }
}
