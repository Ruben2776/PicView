using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PicView.Core.Config;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class SourceGenerationContext : JsonSerializerContext;

public static class SettingsHelper
{
    private const double CurrentSettingsVersion = 1.1;
    private const string ConfigPath = "Config/UserSettings.json";
    private const string RoamingConfigPath = "Ruben2776/PicView/Config/UserSettings.json";

    public static AppSettings? Settings { get; private set; }
    
    /// <summary>
    /// Asynchronously loads the user settings. Loads defaults if not found 
    /// </summary>
    /// <returns>True if settings exists</returns>
    public static async Task<bool> LoadSettingsAsync()
    {
        try
        {
            var path = GetUserSettingsPath();
            if (string.IsNullOrEmpty(path))
            {
                SetDefaults();
                return false;
            }

            try
            {
                await PerformRead(path).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return await Retry().ConfigureAwait(false);
            }
        }
#if DEBUG
        catch (Exception ex)
        {

            Trace.WriteLine($"{nameof(LoadSettingsAsync)} error loading settings:\n {ex.Message}");

            SetDefaults();
            return false;
        }
#else
        catch
        {
            SetDefaults();  
            return false;
        }
#endif
        return true;

        async Task<bool> Retry()
        {
            // TODO test saving location for macOS https://johnkoerner.com/csharp/special-folder-values-on-windows-versus-mac/
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), RoamingConfigPath);
            if (File.Exists(appData))
            {
                try
                {
                    await PerformRead(appData).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    SetDefaults();
                    return false;
                }
            }
            else
            {
                SetDefaults();
                return false;
            }
            return true;
        }
    }

    public static string GetUserSettingsPath()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), RoamingConfigPath);
        if (File.Exists(appData))
        {
            return appData;
        }
        var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath);
        return File.Exists(baseDirectory) ? baseDirectory : string.Empty;
    }

    public static void SetDefaults()
    {
        Settings = new AppSettings
        {
            UIProperties = new UIProperties(),
            Gallery = new Gallery(),
            ImageScaling = new ImageScaling(),
            Sorting = new Sorting(),
            Theme = new Theme(),
            WindowProperties = new WindowProperties(),
            Zoom = new Zoom(),
            StartUp = new StartUp()
        };
        // Get the default culture from the OS
        Settings.UIProperties.UserLanguage = CultureInfo.CurrentCulture.Name;
    }

    public static void DeleteSettingFiles()
    {
        try
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), RoamingConfigPath);
            if (File.Exists(appDataPath))
            {
                File.Delete(appDataPath);
            }

            var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath);
            if (File.Exists(baseDirectory))
            {
                File.Delete(baseDirectory);
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(DeleteSettingFiles)} error deleting settings:\n {ex.Message}");
#endif
        }
    }

    private static async Task PerformSave(string path)
    {
        var updatedJson = JsonSerializer.Serialize(
            Settings, typeof(AppSettings), SourceGenerationContext.Default);
        await using var writer = new StreamWriter(path);
        await writer.WriteAsync(updatedJson).ConfigureAwait(false);
    }

    private static async Task PerformRead(string path)
    {
        var jsonString = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        var settings = JsonSerializer.Deserialize(
                jsonString, typeof(AppSettings), SourceGenerationContext.Default)
            as AppSettings;
        Settings = await UpgradeSettings(settings);
    }

    public static async Task<bool> SaveSettingsAsync()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/UserSettings.json");
            await PerformSave(path).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ruben2776/PicView/Config/UserSettings.json");
            if (!File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                fileInfo.Directory?.Create();
            }
            try
            {
                await PerformSave(path).ConfigureAwait(false);
            }
            catch (Exception)
            {
                Trace.WriteLine($"{nameof(SaveSettingsAsync)} error saving settings:\n {ex.Message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"{nameof(SaveSettingsAsync)} error saving settings:\n {ex.Message}");
            return false;
        }
        return true;
    }

    private static async Task<AppSettings> UpgradeSettings(AppSettings settings)
    {
        if (settings.Version >= CurrentSettingsVersion)
        {
            return settings;
        }

        await SynchronizeSettings(settings).ConfigureAwait(false);

        settings.Version = CurrentSettingsVersion;

        return settings;
    }

    private static async Task SynchronizeSettings(AppSettings settings)
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/UserSettings.json");
            if (!File.Exists(path))
            {
                return;
            }

            var jsonString = await File.ReadAllTextAsync(path).ConfigureAwait(false);

            if (JsonSerializer.Deserialize(
                    jsonString, typeof(AppSettings), SourceGenerationContext.Default) is not AppSettings existingSettings)
            {
                SetDefaults();
                return;
            }

            var properties = typeof(AppSettings).GetProperties();
            foreach (var property in properties)
            {
                // Check if the property exists in the existing JSON
                var jsonProperty = typeof(AppSettings).GetProperty(property.Name);
                if (jsonProperty == null)
                {
                    // Property exists in C# class but not in JSON, initialize it
                    property.SetValue(existingSettings, property.GetValue(settings));
                }
            }

            // Save the synchronized settings back to the JSON file
            var updatedJson = JsonSerializer.Serialize(
                existingSettings, typeof(AppSettings), SourceGenerationContext.Default);
            await File.WriteAllTextAsync(path, updatedJson).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"{nameof(SynchronizeSettings)} error synchronizing settings:\n {ex.Message}");
        }
    }
}