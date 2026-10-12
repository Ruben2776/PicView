using ObservableCollections;
using PicView.Core.FileHistory;
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
        Entries.Clear();

        var entries = FavoritesManager.AllEntries;

        foreach (var entry in entries)
        {
            var entryVm = new FavoritesEntryViewModel();
            entryVm.Initialize(
                entry.FilePath,
                Path.GetFileName(entry.FilePath),
                entry.HotKey,
                _vm);
            Entries.Add(entryVm);
        }
    }
}
