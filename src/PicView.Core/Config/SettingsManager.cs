using System.Diagnostics;
using System.Text.Json;

namespace PicView.Core.Config;

public class SettingsManager : ISettingsManager
{
    public AppSettings? AppSettings { get; set; }
    private JsonSerializerOptions? _jsonSerializerOptions;

    public async Task LoadSettingsAsync()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/AppSettings.json");
            if (File.Exists(path))
            {
                // Read JSON File
                var jsonString = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                AppSettings = JsonSerializer.Deserialize<AppSettings>(jsonString);
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

    public async Task SaveSettingsAsync()
    {
        try
        {
            _jsonSerializerOptions ??= new JsonSerializerOptions { WriteIndented = true };
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/AppSettings.json");
            var updatedJson = JsonSerializer.Serialize(AppSettings, _jsonSerializerOptions);
            await File.WriteAllTextAsync(path, updatedJson).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"{nameof(SaveSettingsAsync)} error saving settings:\n {ex.Message}");
        }
    }
}