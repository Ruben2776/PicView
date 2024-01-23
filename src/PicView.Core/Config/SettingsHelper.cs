using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PicView.Core.Config;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

public static class SettingsHelper
{
    private const double CurrentSettingsVersion = 1;

    public static AppSettings? Settings { get; private set; }
    private static JsonSerializerOptions? _jsonSerializerOptions;

    public static async Task LoadSettingsAsync()
    {
        InitiateJson();
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/UserSettings.json");
            if (File.Exists(path))
            {
                var jsonString = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                var settings = JsonSerializer.Deserialize(
                        jsonString, typeof(AppSettings), SourceGenerationContext.Default)
                    as AppSettings;
                Settings = await UpgradeSettings(settings).ConfigureAwait(false);
            }
            else
            {
                SetDefaults();
                // Get the default culture from the OS
                Settings.UIProperties.UserLanguage = CultureInfo.CurrentCulture.Name;
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

    private static void InitiateJson()
    {
        _jsonSerializerOptions ??= new JsonSerializerOptions
        {
            TypeInfoResolver = SourceGenerationContext.Default,
            AllowTrailingCommas = true
        };
    }

    public static async Task SaveSettingsAsync()
    {
        InitiateJson();
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/UserSettings.json");
            var updatedJson = JsonSerializer.Serialize(
                Settings, typeof(AppSettings), SourceGenerationContext.Default);
            await using var writer = new StreamWriter(path);
            await writer.WriteAsync(updatedJson).ConfigureAwait(false);
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
                var jsonProperty = existingSettings.GetType().GetProperty(property.Name);
                if (jsonProperty == null)
                {
                    // Property exists in C# class but not in JSON, initialize it
                    property.SetValue(existingSettings, property.GetValue(settings));
                }
            }

            // Save the synchronized settings back to the JSON file
            InitiateJson();
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