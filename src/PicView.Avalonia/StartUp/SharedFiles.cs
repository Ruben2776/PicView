using PicView.Avalonia.Input;
using PicView.Core.Favorites;
using PicView.Core.FileHistory;
using PicView.Core.IPlatform;

namespace PicView.Avalonia.StartUp;

/// <summary>
/// The files every Main Window shares: the settings, the Keymap, file history and favorites.
/// </summary>
internal interface ISharedFiles
{
    /// <summary>
    /// Loads file history, favorites and the Keymap. Runs once, at App Start.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Saves file history, the settings and the Keymap.
    /// </summary>
    Task SaveAsync();
}

/// <summary>
/// The shared files in the user's config folders.
/// </summary>
internal sealed class SharedFiles(IPlatformSpecificService? platform) : ISharedFiles
{
    public Task LoadAsync() =>
        Task.WhenAll(
            Task.Run(() =>
            {
                // App Start may have loaded it already; a second load would drop what was added since
                FileHistoryManager.EnsureInitialized();
                FavoritesManager.Initialize();
            }),
            Task.Run(() => Keymaps.Load(platform)));

    public async Task SaveAsync()
    {
        // File history first: the settings save drops the Last File the record held before, so a crash between the two
        // must not leave it in neither
        await FileHistoryManager.SaveToFileAsync().ConfigureAwait(true);
        await SaveSettingsAsync().ConfigureAwait(true);
        await Keymaps.SaveAsync().ConfigureAwait(true);
    }
}
