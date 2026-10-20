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
    /// Saves the settings, the Keymap and file history.
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
                FileHistoryManager.Initialize();
                FavoritesManager.Initialize();
            }),
            Task.Run(() => Keymaps.Load(platform)));

    public async Task SaveAsync()
    {
        await SaveSettingsAsync().ConfigureAwait(true);
        await Keymaps.SaveAsync().ConfigureAwait(true);
        await FileHistoryManager.SaveToFileAsync().ConfigureAwait(true);
    }
}
