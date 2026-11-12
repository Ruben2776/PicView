using PicView.Avalonia.StartUp;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.SettingsManagement;

public static class SettingsUpdater
{
    /// <summary>
    /// Restarts PicView with the default settings. The settings are reset after App Shutdown has saved them, so
    /// App Start loads the defaults. The config location mode is kept.
    /// </summary>
    public static Task ResetSettings(MainWindowLifecycle lifecycle) => lifecycle.RestartAsync(DeleteSettingsAsync);

    private static async Task DeleteSettingsAsync()
    {
        try
        {
            await ResetDefaultsAsync().ConfigureAwait(true);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(SettingsUpdater), nameof(ResetSettings), e);
        }
    }
}
