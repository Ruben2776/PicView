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
    /// It is updated when settings are loaded via <see cref="SettingsManager.LoadSettings"/>
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
    /// are loaded via <see cref="SettingsManager.LoadSettings"/> or set to defaults using
    /// <see cref="SettingsManager.SetDefaults"/>. Changes to the settings can be saved using
    /// <see cref="SettingsManager.SaveSettingsAsync"/>.
    /// </remarks>
    public static AppSettings? Settings { get; private set; }

    private static SettingsConfiguration? Configuration { get; set; }

    /// <summary>
    /// Loads application settings synchronously from a file or initializes them to default if loading fails.
    /// </summary>
    /// <returns>
    /// True if a config file was present and loaded. False if not and reverted to default settings. 
    /// </returns>
    /// <exception cref="JsonException">Thrown if deserialization of the settings file fails.</exception>
    public static bool LoadSettings()
    {
        try
        {
            Configuration ??= new SettingsConfiguration();
            var path = ConfigFileManager.ResolveDefaultConfigPath(Configuration);
            
            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                var settings = JsonSerializer.Deserialize<AppSettings>(
                    bytes, SettingsGenerationContext.Default.AppSettings);
                Settings = EnsureSettings(settings);
            }
            else
            {
                // Fallback to defaults if no user config found
                Settings = GetDefaults();
                return false;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SettingsManager), nameof(LoadSettings), ex);
            SetDefaults();
            return false;
        }

        return true;
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
        var settings = new AppSettings
        {
            UIProperties = GetDefaultUIProperties(),
            Gallery = new Gallery(),
            ImageScaling = new ImageScaling(),
            Sorting = new Sorting(),
            Theme = new Theme(),
            WindowProperties = new WindowProperties(),
            Zoom = GetDefaultZoom(),
            StartUp = new StartUp(),
            Navigation = new Navigation(),
            Version = SettingsConfiguration.CurrentSettingsVersion
        };

        // Get the default culture from the OS
        settings.UIProperties.UserLanguage = CultureInfo.CurrentCulture.Name;

        return settings;
    }

    public static UIProperties GetDefaultUIProperties()
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

        return uiProperties;
    }

    public static Zoom GetDefaultZoom()
    {
        Zoom zoom;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            zoom = new Zoom
            {
                ZoomSpeed = 0.15
            };
        }
        else
        {
            zoom = new Zoom();
        }

        return zoom;
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

    public static AppSettings EnsureSettings(AppSettings existingSettings)
    {
        existingSettings.UIProperties ??= GetDefaultUIProperties();
        existingSettings.Gallery ??= new Gallery();
        existingSettings.Theme ??= new Theme();
        existingSettings.Sorting ??= new Sorting();
        existingSettings.ImageScaling ??= new ImageScaling();
        existingSettings.WindowProperties ??= new WindowProperties();
        existingSettings.Zoom ??= GetDefaultZoom();
        existingSettings.StartUp ??= new StartUp();
        existingSettings.Navigation ??= new Navigation();

        existingSettings.Version = SettingsConfiguration.CurrentSettingsVersion;
        return existingSettings;
    }
}