using System.Diagnostics;
using System.Text.Json;

namespace PicView.Core.Config;

public static class SettingsHelper
{
    private const double CurrentSettingsVersion = 1;

    public static AppSettings? Settings { get; private set; }
    private static JsonSerializerOptions? _jsonSerializerOptions;

    public static async Task LoadSettingsAsync()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/UserSettings.json");
            if (File.Exists(path))
            {
                var jsonString = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                Settings = await UpgradeSettings(JsonSerializer.Deserialize<AppSettings>(jsonString)).ConfigureAwait(false);
            }
            else
            {
                SetDefaults();
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LoadSettingsAsync)} error loading settings:\n {ex.Message}");
#endif
            SetDefaults();
        }
    }

    private static void SetDefaults()
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
    }

    public static async Task SaveSettingsAsync()
    {
        try
        {
            _jsonSerializerOptions ??= new JsonSerializerOptions { WriteIndented = true };
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/UserSettings.json");
            var updatedJson = JsonSerializer.Serialize(Settings, _jsonSerializerOptions);
            await File.WriteAllTextAsync(path, updatedJson).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"{nameof(SaveSettingsAsync)} error saving settings:\n {ex.Message}");
        }
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
            var existingSettings = JsonSerializer.Deserialize<AppSettings>(jsonString);

            if (existingSettings == null)
            {
                SetDefaults();
                return;
            }

            var properties = typeof(AppSettings).GetProperties();
            foreach (var property in properties)
            {
                // Check if the property exists in the existing JSON
                var jsonProperty = existingSettings.GetType().GetProperty(property.Name);
                if (jsonProperty == null)
                {
                    // Property exists in C# class but not in JSON, initialize it
                    property.SetValue(existingSettings, property.GetValue(settings));
                }
            }

            // Save the synchronized settings back to the JSON file
            _jsonSerializerOptions ??= new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = JsonSerializer.Serialize(existingSettings, _jsonSerializerOptions);
            await File.WriteAllTextAsync(path, updatedJson).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"{nameof(SynchronizeSettings)} error synchronizing settings:\n {ex.Message}");
        }
    }
}