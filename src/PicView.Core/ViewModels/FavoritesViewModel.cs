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
    public ReactiveCommand<FavoriteEntry> AddOrEditCommand { get; }
    public ReactiveCommand<FavoriteEntry> EditFavoritesDialogCommand { get; } = new();

    public FavoritesViewModel(MainWindowViewModel vm)
    {
        _vm = vm;
        ClearFavoritesCommand = new ReactiveCommand(ClearFavorites);
        OpenFavoritesFileCommand = new ReactiveCommand(OpenFavoritesFile);
        AddFavoritesDialogCommand = new ReactiveCommand();
        AddFromFolderCommand = new ReactiveCommand();
        AddOrEditCommand = new ReactiveCommand<FavoriteEntry>();
        AddOrEditCommand.SubscribeAwait(async (entry, _) =>
        {
            if (!string.IsNullOrWhiteSpace(entry.FilePath))
            {
                await AddOrEditAsync(entry).ConfigureAwait(false);
            }
        });
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

    public async ValueTask AddFavorite(string path, string keybind = "")
    {
        await FavoritesManager.AddAsync(path, keybind).ConfigureAwait(false);
        UpdateFavorites();
    }

    public async ValueTask AddFavorite(FavoriteEntry entry)
    {
        await FavoritesManager.AddAsync(entry).ConfigureAwait(false);
        UpdateFavorites();
    }

    public async ValueTask AddOrEditAsync(FavoriteEntry entry)
    {
        await AddOrEditAsync(entry.FilePath, entry.Keybind).ConfigureAwait(false);
    }

    public async ValueTask AddOrEditAsync(string path, string keybind = "")
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var normalizedPath = FavoritesManager.NormalizeDirectoryPath(path) ?? Path.TrimEndingDirectorySeparator(path.Trim());

        if (FavoritesManager.Contains(normalizedPath))
        {
            FavoritesManager.SetKeybind(normalizedPath, keybind);
            await FavoritesManager.SaveToFileAsync().ConfigureAwait(false);
            UpdateFavorites();
        }
        else
        {
            await FavoritesManager.AddAsync(path, keybind).ConfigureAwait(false);
            UpdateFavorites();
        }
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
                entry.Keybind,
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
        AddOrEditCommand.Dispose();
        EditFavoritesDialogCommand.Dispose();

        foreach (var existing in Entries)
        {
            existing.Dispose();
        }
        Entries.Clear();

        GC.SuppressFinalize(this);
    }
}
