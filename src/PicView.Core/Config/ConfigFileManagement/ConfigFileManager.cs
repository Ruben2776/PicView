using System.Text.Json.Serialization;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;

namespace PicView.Core.Config.ConfigFileManagement;

/// <summary>
/// Writes config files into the resolved config folder.
/// </summary>
public static class ConfigFileManager
{
    /// <summary>
    /// Saves a config file into the resolved config folder while holding <see cref="ConfigFolders.Gate" />.
    /// </summary>
    /// <returns>True if the file was written; otherwise false.</returns>
    public static async Task<bool> SaveAsync(ConfigFile file, object? value, Type inputType,
        JsonSerializerContext context)
    {
        await ConfigFolders.Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await SaveUnlockedAsync(file, value, inputType, context).ConfigureAwait(false);
        }
        finally
        {
            ConfigFolders.Gate.Release();
        }
    }

    /// <summary>
    /// Saves a config file. The caller must hold <see cref="ConfigFolders.Gate" />.
    /// </summary>
    internal static async Task<bool> SaveUnlockedAsync(ConfigFile file, object? value, Type inputType,
        JsonSerializerContext context)
    {
        if (value is IWindowProperties windowProps)
        {
            if (windowProps.Width.HasValue && !double.IsFinite(windowProps.Width.Value))
            {
                windowProps.Width = null;
            }

            if (windowProps.Height.HasValue && !double.IsFinite(windowProps.Height.Value))
            {
                windowProps.Height = null;
            }
        }

        try
        {
            var path = file.FilePath;
            FileHelper.EnsureDirectoryExists(path);
            await JsonFileHelper.WriteJsonAsync(path, value, inputType, context).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ConfigFileManager), nameof(SaveUnlockedAsync), ex);
            return false;
        }
    }

    /// <summary>
    /// Writes text to a config file in the resolved config folder while holding <see cref="ConfigFolders.Gate" />.
    /// </summary>
    /// <returns>True if the file was written; otherwise false.</returns>
    public static async Task<bool> WriteTextAsync(ConfigFile file, string text)
    {
        await ConfigFolders.Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await WriteTextUnlockedAsync(file, text).ConfigureAwait(false);
        }
        finally
        {
            ConfigFolders.Gate.Release();
        }
    }

    /// <summary>
    /// Writes text to a config file. The caller must hold <see cref="ConfigFolders.Gate" />.
    /// </summary>
    internal static async Task<bool> WriteTextUnlockedAsync(ConfigFile file, string text)
    {
        try
        {
            var path = file.FilePath;
            FileHelper.EnsureDirectoryExists(path);
            await File.WriteAllTextAsync(path, text).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ConfigFileManager), nameof(WriteTextUnlockedAsync), ex);
            return false;
        }
    }
}
