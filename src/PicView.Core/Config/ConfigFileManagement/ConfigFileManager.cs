using System.Text.Json.Serialization;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;

namespace PicView.Core.Config.ConfigFileManagement;

/// <summary>
/// Provides functionality to manage configuration files, including saving and retrieving paths for various config file types.
/// </summary>
public static class ConfigFileManager
{
    /// <summary>
    /// Saves the configuration file to the specified path and returns the resulting file path.
    /// </summary>
    /// <param name="file">The type of configuration file to be saved (e.g., user settings, file history, key bindings).</param>
    /// <param name="path">The file path where the configuration file should be saved. If null, a default path is resolved based on the configuration type.</param>
    /// <param name="value">The object containing the configuration data to be saved.</param>
    /// <param name="inputType">The type of the configuration object to be serialized.</param>
    /// <param name="context">The JSON serializer context used for serializing the configuration data.</param>
    /// <returns>The file path where the configuration file was successfully saved, or null if the operation failed.</returns>
    /// <remarks>If the initial save location is not writable, it attempts to save to the roaming app data directory.</remarks>
    public static async Task<string?> SaveConfigFileAndReturnPathAsync(ConfigFile file, string? path, object? value,
        Type inputType, JsonSerializerContext context)
    {
        // If null, try to get the current user file, if exist
        path ??= GetConfigPath(file);

        try
        {
            if (!FileHelper.IsPathWritable(path))
            {
                return await TrySaveRoaming().ConfigureAwait(false);
            }

            await JsonFileHelper.WriteJsonAsync(path, value, inputType, context).ConfigureAwait(false);
            return path;
        }
        catch (UnauthorizedAccessException)
        {
            // If unauthorized, try saving to roaming app data
            try
            {
                return await TrySaveRoaming().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DebugHelper.LogDebug(nameof(ConfigFileManager), nameof(SaveConfigFileAndReturnPathAsync), ex);
                return null;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ConfigFileManager), nameof(SaveConfigFileAndReturnPathAsync), ex);
            return null;
        }

        async Task<string> TrySaveRoaming()
        {
            var roamingPath = file.RoamingConfigPath;

            FileHelper.EnsureDirectoryExists(roamingPath);
            await JsonFileHelper.WriteJsonAsync(roamingPath, value, inputType, context).ConfigureAwait(false);

            return roamingPath;
        }
    }

    /// <summary>
    /// Resolves the default configuration file path based on the specified configuration file type and the operating system in use.
    /// </summary>
    /// <param name="file">The type of configuration file for which the default path needs to be resolved (e.g., user settings, file history, key bindings).</param>
    /// <returns>The resolved default configuration file path as a string.</returns>
    public static string ResolveDefaultConfigPath(ConfigFile file)
    {
        if (OperatingSystem.IsMacOS())
        {
            // On macOS, always use the roaming path. We can't save inside an app bundle.
            return file.RoamingConfigPath;
        }

        var path = GetConfigPath(file);
        
        return OperatingSystem.IsLinux() ? path.Replace('\\', '/') 
            : path.Replace("/", "\\", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetConfigPath(ConfigFile file)
    {
        var currentUserPath = file.TryGetCurrentUserConfigPath;
        if (currentUserPath != string.Empty)
        {
            return currentUserPath;
        }

        var localPath = file.LocalConfigPath;
        return FileHelper.IsPathWritable(localPath) ? localPath : file.RoamingConfigPath;
    }
}
