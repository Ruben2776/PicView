using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.Gallery;
using PicView.Core.Localization;

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
    /// Gets the path of the settings file in the config folder in use.
    /// </summary>
    /// <remarks>
    /// The path is computed from <see cref="ConfigFolders.Current" /> on every read, so it follows a folder switch.
    /// The file does not have to exist.
    /// </remarks>
    public static string CurrentSettingsPath => (Configuration ??= new SettingsConfiguration()).FilePath;

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

    /// <summary>
    /// The settings version of the loaded settings file, before <see cref="EnsureSettings"/> migrated it to
    /// <see cref="SettingsConfiguration.CurrentSettingsVersion"/>. Other config files, such as the keybindings,
    /// use it to migrate themselves. It is the current version when no settings file was loaded.
    /// </summary>
    public static double LoadedSettingsVersion { get; private set; } = SettingsConfiguration.CurrentSettingsVersion;

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
            ConfigFolders.ResolveAtStartup();

            var info = new FileInfo(Configuration.FilePath);
            if (info.Exists && info.Length > 0)
            {
                var bytes = File.ReadAllBytes(info.FullName);
                var settings = JsonSerializer.Deserialize<AppSettings>(
                    bytes, SettingsGenerationContext.Default.AppSettings);
                LoadedSettingsVersion = settings.Version;
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
    /// <remarks>
    /// If the mode is Portable or Roaming and its folder is not the one in use, the settings are written into
    /// the mode's folder and every other config file is moved there.
    /// </remarks>
    public static async ValueTask<bool> SaveSettingsAsync()
    {
        if (Settings == null)
        {
            return false;
        }

        Configuration ??= new SettingsConfiguration();
        await ConfigFolders.Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var mode = Settings.StartUp?.ConfigLocation ?? ConfigLocation.Auto;
            if (mode is ConfigLocation.Portable or ConfigLocation.Roaming)
            {
                var target = ConfigFolders.FolderFor(mode);
                var from = ConfigFolders.Current;
                if (!ConfigFolders.IsSame(target, from) &&
                    (mode != ConfigLocation.Portable || FileHelper.IsDirectoryWritable(target)))
                {
                    ConfigFolders.SetCurrent(target);
                    if (!await SaveSettingsUnlockedAsync().ConfigureAwait(false))
                    {
                        ConfigFolders.SetCurrent(from);
                        return false;
                    }

                    await ConfigFolders.MoveAllAsync(from, target, includeSettings: false).ConfigureAwait(false);
                    DeleteSettingsFile(from);
                    return true;
                }
            }

            return await SaveSettingsUnlockedAsync().ConfigureAwait(false);
        }
        finally
        {
            ConfigFolders.Gate.Release();
        }
    }

    /// <summary>
    /// Switches the config location mode, saves it, and moves every config file into the mode's folder.
    /// </summary>
    /// <returns>What happened. <see cref="ConfigLocationChange.PortableNotWritable" /> and
    /// <see cref="ConfigLocationChange.SaveFailed" /> leave the mode and the folder as they were.</returns>
    public static async Task<ConfigLocationChange> SetConfigLocationAsync(ConfigLocation mode)
    {
        // No await before Gate.WaitAsync: a caller that starts this before a save must reach the queue first
        if (Settings?.StartUp is null)
        {
            return ConfigLocationChange.SaveFailed;
        }

        if (OperatingSystem.IsMacOS() || Settings.StartUp.ConfigLocation == mode)
        {
            return ConfigLocationChange.Unchanged;
        }

        var target = ConfigFolders.FolderFor(mode);
        if (mode == ConfigLocation.Portable && !FileHelper.IsDirectoryWritable(target))
        {
            return ConfigLocationChange.PortableNotWritable;
        }

        Configuration ??= new SettingsConfiguration();
        await ConfigFolders.Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var oldMode = Settings.StartUp.ConfigLocation;
            var from = ConfigFolders.Current;
            var moving = !ConfigFolders.IsSame(target, from);

            Settings.StartUp.ConfigLocation = mode;
            if (moving)
            {
                ConfigFolders.SetCurrent(target);
            }

            if (!await SaveSettingsUnlockedAsync().ConfigureAwait(false))
            {
                Settings.StartUp.ConfigLocation = oldMode;
                ConfigFolders.SetCurrent(from);
                return ConfigLocationChange.SaveFailed;
            }

            var ok = true;
            if (moving)
            {
                ok = await ConfigFolders.MoveAllAsync(from, target, includeSettings: false).ConfigureAwait(false);
                DeleteSettingsFile(from);
            }

            return ok ? ConfigLocationChange.Changed : ConfigLocationChange.MoveIncomplete;
        }
        finally
        {
            ConfigFolders.Gate.Release();
        }
    }

    /// <summary>
    /// Writes the settings. The caller must hold <see cref="ConfigFolders.Gate" />.
    /// </summary>
    private static Task<bool> SaveSettingsUnlockedAsync() =>
        ConfigFileManager.SaveUnlockedAsync(Configuration ??= new SettingsConfiguration(), Settings,
            typeof(AppSettings), SettingsGenerationContext.Default);

    private static void DeleteSettingsFile(string directory)
    {
        try
        {
            var path = Path.Combine(directory, ConfigFolders.SettingsFileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(SettingsManager), nameof(DeleteSettingsFile), ex);
        }
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
        if (OperatingSystem.IsMacOS())
        {
            uiProperties = new UIProperties
            {
                IsTaskbarProgressEnabled = false,
                OpenInSameWindow = true
            };
        }
        else
        {
            if (OperatingSystem.IsLinux())
            {
                uiProperties = new UIProperties
                {
                    BgColorChoice = 3
                };
            }
            else
            {
                uiProperties = new UIProperties();
            }
            
        }

        return uiProperties;
    }

    public static Zoom GetDefaultZoom()
    {
        Zoom zoom;
        if (OperatingSystem.IsMacOS())
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
    /// Resets the application's settings to their default values, keeping the config location mode.
    /// </summary>
    /// <remarks>
    /// In Auto mode, when the other folder has no settings file, the settings file is deleted, so the next start
    /// behaves like a first run. Otherwise the defaults are written, with the mode kept and the language detected
    /// from the system, because a deleted file would lose the mode or let a stale copy in the other folder win.
    /// </remarks>
    public static async Task ResetDefaultsAsync()
    {
        var mode = Settings?.StartUp?.ConfigLocation ?? ConfigLocation.Auto;
        SetDefaults();

        if (mode == ConfigLocation.Auto && !HasContent(Path.Combine(ConfigFolders.OtherFolder,
                ConfigFolders.SettingsFileName)))
        {
            try
            {
                var path = (Configuration ??= new SettingsConfiguration()).FilePath;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                DebugHelper.LogDebug(nameof(SettingsManager), nameof(ResetDefaultsAsync), ex);
            }

            return;
        }

        Settings!.StartUp!.ConfigLocation = mode;
        Settings.UIProperties!.UserLanguage = TranslationManager.DetermineCorrectLanguage();
        await SaveSettingsAsync().ConfigureAwait(false);
    }

    private static bool HasContent(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists && info.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public static AppSettings EnsureSettings(AppSettings existingSettings)
    {
        existingSettings.UIProperties ??= GetDefaultUIProperties();
        existingSettings.Gallery ??= new Gallery();
        if (existingSettings.Gallery.ExpandedGalleryItemSize is < GalleryDefaults.MinExpandedGalleryItemHeight
            or > GalleryDefaults.MaxExpandedGalleryItemHeight)
        {
            existingSettings.Gallery.ExpandedGalleryItemSize = GalleryDefaults.DefaultExpandedGalleryHeight;
        }

        if (existingSettings.Gallery.DockedGalleryItemSize is < GalleryDefaults.MinDockedGalleryItemHeight
            or > GalleryDefaults.MaxDockedGalleryItemHeight)
        {
            existingSettings.Gallery.DockedGalleryItemSize = GalleryDefaults.DefaultDockedGalleryHeight;
        }
        
        if (existingSettings.Version < 3.0)
        {
            if (existingSettings.Version < 2.0)
            {
                existingSettings.Gallery.DockPosition = GalleryDockPosition.Bottom;
            }
            // Fill and None stretch mode existed in older version
            // and were deleted in 5.1.0, so we reset them to default
            existingSettings.Gallery.ExpandedGalleryStretchMode = GalleryStretchMode.Uniform;
            existingSettings.Gallery.DockedGalleryStretchMode = GalleryStretchMode.UniformToFill;
        }

        // The dock position once had a Closed value (0) that doubled as "not docked"; whether the gallery starts
        // docked is IsGalleryDocked alone now
        if (existingSettings.Gallery.DockPosition is not (GalleryDockPosition.Bottom or GalleryDockPosition.Top
            or GalleryDockPosition.Left or GalleryDockPosition.Right))
        {
            existingSettings.Gallery.DockPosition = GalleryDockPosition.Bottom;
        }

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