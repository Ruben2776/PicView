using ObservableCollections;
using PicView.Core.Favorites;
using R3;
using PicView.Core.Keybindings;

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
        await _vm.ActionRunner.Run(Actions.ShowFavoritesFile).ConfigureAwait(false);
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
    
    /// <summary>
    ///     Whether <see cref="Entries" /> lists the favorites as they are. While it is true, <see cref="ShowFavorites" />
    ///     keeps the entries, so a view of them keeps its layout and scroll position.
    /// </summary>
    public bool IsCached { get; private set; }

    /// <summary>
    ///     Marks the entries as out of date, so the next <see cref="ShowFavorites" /> makes them again.
    /// </summary>
    public void Invalidate() => IsCached = false;

    /// <summary>
    ///     Readies the entries for showing: makes them when they are not cached, otherwise only moves the mark to the
    ///     active tab's folder.
    /// </summary>
    public void ShowFavorites()
    {
        if (!IsCached)
        {
            UpdateFavorites();
            return;
        }

        var currentDir = CurrentDirectory();
        foreach (var entry in Entries)
        {
            entry.IsCurrentItem.Value = IsCurrent(entry.FilePath.CurrentValue, currentDir);
        }
    }

    /// <summary>
    ///     Makes the entries again from the favorites.
    /// </summary>
    public void UpdateFavorites()
    {
        foreach (var existing in Entries)
        {
            existing.Dispose();
        }
        Entries.Clear();

        var entries = FavoritesManager.AllEntries;
        var normalizedCurrentDir = CurrentDirectory();

        foreach (var entry in entries)
        {
            var normalizedEntryPath = Path.TrimEndingDirectorySeparator(entry.FilePath);
            var folderName = Path.GetFileName(normalizedEntryPath);
            if (string.IsNullOrEmpty(folderName))
            {
                folderName = normalizedEntryPath;
            }

            var entryVm = new FavoritesEntryViewModel();
            entryVm.Initialize(
                normalizedEntryPath,
                folderName,
                entry.Slot,
                IsCurrent(normalizedEntryPath, normalizedCurrentDir),
                _vm);
            Entries.Add(entryVm);
        }

        IsCached = true;
    }

    // The active tab's folder without a trailing separator, or null when it has none
    private string? CurrentDirectory()
    {
        var currentDirectoryPath = _vm.WindowTabs?.ActiveTab?.Value?.Model?.FileInfo?.DirectoryName;
        return !string.IsNullOrWhiteSpace(currentDirectoryPath)
            ? Path.TrimEndingDirectorySeparator(currentDirectoryPath)
            : null;
    }

    private static bool IsCurrent(string normalizedEntryPath, string? normalizedCurrentDir) =>
        normalizedCurrentDir is not null &&
        string.Equals(normalizedEntryPath, normalizedCurrentDir, StringComparison.Ordinal);

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
