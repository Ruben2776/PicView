using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;

namespace PicView.Core.Config;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class SettingsGenerationContext : JsonSerializerContext;

/// <summary>
/// Provides functionality to manage loading, saving, and modifying application settings.
/// </summary>
public static class SettingsManager
{
    /// <summary>
    /// Gets the file path to the currently loaded settings file, if available.
    /// </summary>
    /// <remarks>
    /// This property reflects the full path to the settings file currently in use by the application.
    /// It is updated when settings are loaded via <see cref="SettingsManager.LoadSettingsAsync"/>
    /// or saved using <see cref="SettingsManager.SaveSettingsAsync"/>. If no settings file is loaded,
    /// the value will be null. This path can be used to reference the specific configuration file
    /// being utilized or modified.
    /// </remarks>
    public static string? CurrentSettingsPath => Configuration.CorrectPath;

    /// <summary>
    /// Gets or sets the current application settings instance, which stores all configurable
    /// values for the application's behavior, appearance, and functionality.
    /// </summary>
    /// <remarks>
    /// This property can be used to retrieve or assign settings related to application preferences,
    /// such as UI, theme, sorting, scaling, and startup configurations. The associated settings
    /// are loaded via <see cref="SettingsManager.LoadSettingsAsync"/> or set to defaults using
    /// <see cref="SettingsManager.SetDefaults"/>. Changes to the settings can be saved using
    /// <see cref="SettingsManager.SaveSettingsAsync"/>.
    /// </remarks>
    public static AppSettings? Settings { get; private set; }

    public static SettingsConfiguration? Configuration { get; private set; }

    /// <summary>
    /// Global Configuration Support
    /// </summary>
    /// <remarks>
    /// Overrides any UserSettings with GlobalSettings if they are set
    /// </remarks>
    public static GlobalSettingsConfiguration? GlobalConfig { get; private set; }
    public static AppSettings? GlobalSettings { get; private set; }

    /// <summary>
    /// Loads application settings asynchronously from a file or initializes them to default if loading fails.
    /// </summary>
    /// <returns>
    /// A boolean indicating whether the settings were successfully loaded.
    /// </returns>
    /// <exception cref="JsonException">Thrown if deserialization of the settings file fails.</exception>
    public static async ValueTask<bool> LoadSettingsAsync()
    {
        try
        {
            // Load global config (read-only, Program Path)
            GlobalConfig ??= new GlobalSettingsConfiguration();
            string globalPath = GlobalConfig.LocalConfigPath;
            if (File.Exists(globalPath))
            {
                await using var globalStream = File.OpenRead(globalPath);
                if (globalStream.Length > 0)
                {
                    GlobalSettings = await JsonSerializer.DeserializeAsync<AppSettings>(
                        globalStream, SettingsGenerationContext.Default.AppSettings).ConfigureAwait(false);
                }
            }

            // Load user config (User Profile or Program Path)
            Configuration ??= new SettingsConfiguration();
            var userPath = ConfigFileManager.ResolveDefaultConfigPath(Configuration);
            Configuration.CorrectPath = userPath;

            if (File.Exists(userPath))
            {
                await using var userStream = File.OpenRead(userPath);
                if (userStream.Length > 0)
                {
                    Settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                        userStream, SettingsGenerationContext.Default.AppSettings).ConfigureAwait(false);
                }
            }

            // Fallback to defaults if no user config found
            Settings ??= GetDefaults();

            // Apply Global Overrides
            if (GlobalSettings != null)
                ApplyOverrides(Settings, GlobalSettings);

            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SettingsManager), nameof(LoadSettingsAsync), ex);
            SetDefaults();
            return false;
        }
    }

    /// <summary>
    /// Asynchronously saves the current application settings to the appropriate file location.
    /// </summary>
    /// <returns>
    /// Whether the settings were successfully saved.
    /// </returns>
    public static async ValueTask<bool> SaveSettingsAsync()
    {
        if (Settings == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(Configuration.CorrectPath))
        {
            Configuration.CorrectPath = ConfigFileManager.ResolveDefaultConfigPath(Configuration);
        }

        if (!FileHelper.IsPathWritable(CurrentSettingsPath))
        {
            Configuration.CorrectPath = Configuration.RoamingConfigPath;
        }

        var saveLocation = await ConfigFileManager.SaveConfigFileAndReturnPathAsync(Configuration,
            CurrentSettingsPath,
            Settings, typeof(AppSettings), SettingsGenerationContext.Default).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(saveLocation))
        {
            DebugHelper.LogDebug(nameof(SettingsManager), nameof(SaveSettingsAsync), "Empty save location");
            return false;
        }

        Configuration.CorrectPath = saveLocation;
        return true;
    }

    private static AppSettings EnsureSettingsIfNeeded(AppSettings settings)
    {
        if (settings?.WindowProperties is null)
        {
            return GetDefaults();
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (settings.Version != SettingsConfiguration.CurrentSettingsVersion)
        {
            return EnsureSettings(settings);
        }

        // If navigation settings is null, it is an upgrade from an old version or the config is otherwise invalid
        if (settings.Navigation is null)
        {
            return EnsureSettings(settings);
        }

        settings.Version = SettingsConfiguration.CurrentSettingsVersion;
        return settings;
    }

    private static AppSettings EnsureSettings(AppSettings existingSettings)
    {
        var newSettings = GetDefaults();

        existingSettings.UIProperties ??= newSettings.UIProperties;
        existingSettings.Gallery ??= newSettings.Gallery;
        existingSettings.Theme ??= newSettings.Theme;
        existingSettings.Sorting ??= newSettings.Sorting;
        existingSettings.ImageScaling ??= newSettings.ImageScaling;
        existingSettings.WindowProperties ??= newSettings.WindowProperties;
        existingSettings.Zoom ??= newSettings.Zoom;
        existingSettings.StartUp ??= newSettings.StartUp;
        existingSettings.Navigation ??= newSettings.Navigation;

        existingSettings.Version = SettingsConfiguration.CurrentSettingsVersion;
        return existingSettings;
    }

    /// <summary>
    /// Sets the application's settings to their default values.
    /// </summary>
    public static void SetDefaults() => Settings = GetDefaults();

    /// <summary>
    /// Initializes and returns an instance of the default application settings.
    /// </summary>
    /// <returns>
    /// An <see cref="AppSettings"/> object populated with default values.
    /// </returns>
    public static AppSettings GetDefaults()
    {
        UIProperties uiProperties;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            uiProperties = new UIProperties
            {
                IsTaskbarProgressEnabled = false,
                OpenInSameWindow = true
            };
        }
        else
        {
            uiProperties = new UIProperties();
        }

        var settings = new AppSettings
        {
            UIProperties = uiProperties,
            Gallery = new Gallery(),
            ImageScaling = new ImageScaling(),
            Sorting = new Sorting(),
            Theme = new Theme(),
            WindowProperties = new WindowProperties(),
            Zoom = new Zoom(),
            StartUp = new StartUp(),
            Navigation = new Navigation(),
            Version = SettingsConfiguration.CurrentSettingsVersion
        };

        // Get the default culture from the OS
        settings.UIProperties.UserLanguage = CultureInfo.CurrentCulture.Name;

        return settings;
    }

    /// <summary>
    /// Resets the application's settings to their default values and removes any existing settings file
    /// to start with a clean configuration.
    /// </summary>
    /// <exception cref="IOException">Thrown if an error occurs while attempting to delete the existing settings file.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if access to the settings file is denied during deletion.</exception>
    /// <remarks>
    /// This method ensures that the default settings are applied and deletes the user settings file if it exists,
    /// allowing the application configuration to be completely reset.
    /// </remarks>
    public static void ResetDefaults()
    {
        try
        {
            DeleteFileIfExists(Configuration.TryGetCurrentUserConfigPath);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SettingsManager), nameof(ResetDefaults), ex);
        }
        finally
        {
            SetDefaults();
        }

        return;

        void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
    
    private static void ApplyOverrides(AppSettings target, AppSettings global)
    {
        MergeObjects(target, global);
    }

    /// <summary>
    /// Recursively merges all non-null properties from source into target.
    /// Complex nested types (like UIProperties, Theme, etc.) are merged recursively.
    /// Value types and simple properties are directly overwritten.
    /// </summary>
    private static void MergeObjects(object? target, object? source)
    {
        if (target == null || source == null)
            return;

        var targetType = target.GetType();
        var sourceType = source.GetType();

        foreach (var prop in sourceType.GetProperties())
        {
            var sourceValue = prop.GetValue(source);
            if (sourceValue == null)
                continue;

            var targetProp = targetType.GetProperty(prop.Name);
            if (targetProp == null || !targetProp.CanWrite)
                continue;

            var targetValue = targetProp.GetValue(target);

            // If this is a nested object (class) and not a string, merge recursively
            if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
            {
                if (targetValue == null)
                {
                    // If user doesn't have that object at all, copy it fully
                    targetProp.SetValue(target, sourceValue);
                }
                else
                {
                    // Recursively merge individual properties
                    MergeObjects(targetValue, sourceValue);
                }
            }
            else
            {
                // Simple value type or string – overwrite directly
                targetProp.SetValue(target, sourceValue);
            }
        }
    }
}