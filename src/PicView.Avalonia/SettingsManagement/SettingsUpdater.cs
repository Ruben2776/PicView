using PicView.Avalonia.StartUp;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.SettingsManagement;

public static class SettingsUpdater
{
    /// <summary>
    /// Restarts PicView with the default settings. The settings file is deleted after App Shutdown has saved it, so
    /// App Start finds none and uses the defaults.
    /// </summary>
    public static Task ResetSettings(MainWindowLifecycle lifecycle) => lifecycle.RestartAsync(DeleteSettingsAsync);

    private static async Task DeleteSettingsAsync()
    {
        if (File.Exists(CurrentSettingsPath))
        {
            try
            {
                File.Delete(CurrentSettingsPath);
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(SettingsUpdater), nameof(ResetSettings), e);
                await SetAndSave().ConfigureAwait(true);
            }
        }
        else
        {
            await SetAndSave().ConfigureAwait(true);
        }

        return;

        async Task SetAndSave()
        {
            ResetDefaults();
            await SaveSettingsAsync().ConfigureAwait(true);
        }
    }
}
