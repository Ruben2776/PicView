using System.Diagnostics;
using System.Text.Json;

namespace PicView.Core.Config;

public class SettingsManager : ISettingsManager
{
    public AppSettings? AppSettings { get; set; }
    private JsonSerializerOptions? _jsonSerializerOptions;

    public void LoadSettings()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/AppSettings.json");
            if (File.Exists(path))
            {
                // Read JSON File
                var jsonString = File.ReadAllText(path);
                AppSettings = JsonSerializer.Deserialize<AppSettings>(jsonString);
            }
            else
            {
                // TODO recreate a default json settings file
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions appropriately (logging, notifying the user, etc.)
            Trace.WriteLine($"{nameof(LoadSettings)} error loading settings:\n {ex.Message}");
        }
    }

    public void SaveSettings()
    {
        try
        {
            _jsonSerializerOptions ??= new JsonSerializerOptions { WriteIndented = true };
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/AppSettings.json");
            var updatedJson = JsonSerializer.Serialize(AppSettings, _jsonSerializerOptions);
            File.WriteAllText(path, updatedJson);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"{nameof(SaveSettings)} error saving settings:\n {ex.Message}");
        }
    }
}