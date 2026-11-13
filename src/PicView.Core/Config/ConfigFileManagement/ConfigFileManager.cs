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
    /// Saves a config file. The caller must hold <see cref="ConfigFolders.Gate" />. In Auto mode, a failed write in a folder
    /// that is no longer writable moves every config file to the folder Auto picks now and retries once.
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

        if (await TrySaveAsync(file, value, inputType, context).ConfigureAwait(false))
        {
            return true;
        }

        if (await TryAutoFallbackAsync(file).ConfigureAwait(false) is not { } previous)
        {
            return false;
        }

        if (await TrySaveAsync(file, value, inputType, context).ConfigureAwait(false))
        {
            return true;
        }

        ConfigFolders.SetCurrent(previous);
        return false;
    }

    private static async Task<bool> TrySaveAsync(ConfigFile file, object? value, Type inputType,
        JsonSerializerContext context)
    {
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
    /// After a failed write in Auto mode, when the folder in use is no longer writable, moves every config file to the
    /// folder Auto picks now and makes it <see cref="ConfigFolders.Current" />. A failure in a folder that is still
    /// writable, such as a locked file, moves nothing. The caller must hold <see cref="ConfigFolders.Gate" />.
    /// </summary>
    /// <returns>The folder in use before the fallback, so a failed retry can go back to it; or null when there was
    /// no fallback and the write should not be retried.</returns>
    private static async Task<string?> TryAutoFallbackAsync(ConfigFile file)
    {
        if (file.PathOverride is not null)
        {
            return null;
        }

        var mode = SettingsManager.Settings?.StartUp?.ConfigLocation ?? ConfigLocation.Auto;
        if (mode != ConfigLocation.Auto)
        {
            return null;
        }

        var from = ConfigFolders.Current;
        if (FileHelper.IsDirectoryWritable(from))
        {
            return null;
        }

        var to = ConfigFolders.FolderFor(ConfigLocation.Auto);
        if (ConfigFolders.IsSame(from, to))
        {
            return null;
        }

        await ConfigFolders.MoveAllAsync(from, to, includeSettings: true).ConfigureAwait(false);
        ConfigFolders.SetCurrent(to);
        return from;
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
    /// Writes text to a config file. The caller must hold <see cref="ConfigFolders.Gate" />. In Auto mode, a failed
    /// write in a folder that is no longer writable moves every config file to the folder Auto picks now and retries once.
    /// </summary>
    internal static async Task<bool> WriteTextUnlockedAsync(ConfigFile file, string text)
    {
        if (await TryWriteTextAsync(file, text).ConfigureAwait(false))
        {
            return true;
        }

        if (await TryAutoFallbackAsync(file).ConfigureAwait(false) is not { } previous)
        {
            return false;
        }

        if (await TryWriteTextAsync(file, text).ConfigureAwait(false))
        {
            return true;
        }

        ConfigFolders.SetCurrent(previous);
        return false;
    }

    private static async Task<bool> TryWriteTextAsync(ConfigFile file, string text)
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
