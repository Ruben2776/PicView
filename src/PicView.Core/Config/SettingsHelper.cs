using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace PicView.Core.Config;

public static class SettingsHelper
{
    public static AppSettings? Settings { get; set; }
    private static JsonSerializerOptions? _jsonSerializerOptions;

    public static async Task LoadSettingsAsync()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/AppSettings.json");
            if (File.Exists(path))
            {
                // Read JSON File with UTF-8 encoding
                var jsonString = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                Settings = JsonSerializer.Deserialize<AppSettings>(jsonString);
            }
            else
            {
                // TODO recreate a default json settings file
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"{nameof(LoadSettingsAsync)} error loading settings:\n {ex.Message}");
        }
    }

    public static async Task SaveSettingsAsync()
    {
        try
        {
            _jsonSerializerOptions ??= new JsonSerializerOptions { WriteIndented = true };
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/AppSettings.json");
            var updatedJson = JsonSerializer.Serialize(Settings, _jsonSerializerOptions);
            await File.WriteAllTextAsync(path, updatedJson).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"{nameof(SaveSettingsAsync)} error saving settings:\n {ex.Message}");
        }
    }
}