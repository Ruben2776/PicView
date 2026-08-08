using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Core.Config;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(SettingsWindowConfig.SettingsWindowProperties))]
internal partial class SettingsWindowGenerationContext : JsonSerializerContext;
public class SettingsWindowConfig() : ConfigFile("SettingsWindow.json")
{
    public SettingsWindowProperties? WindowProperties { get; private set;  }

    public async Task LoadAsync()
    {
        CorrectPath ??= ConfigFileManager.ResolveDefaultConfigPath(this);
        try
        {
            if (File.Exists(CorrectPath))
            {
                var jsonString = await File.ReadAllTextAsync(CorrectPath).ConfigureAwait(false);
                if (JsonSerializer.Deserialize(
                        jsonString, typeof(SettingsWindowProperties), SettingsWindowGenerationContext.Default) is SettingsWindowProperties settings)
                {
                    WindowProperties = settings;
                }
                else
                {
                    WindowProperties = new SettingsWindowProperties();
                } 
            }
            else
            {
                WindowProperties = new SettingsWindowProperties();
            }
        }
        catch
        {
            WindowProperties = new SettingsWindowProperties();
        }
    }
    
    public async Task SaveAsync()
    {
        CorrectPath = await ConfigFileManager.SaveConfigFileAndReturnPathAsync(this,
            CorrectPath, WindowProperties, typeof(SettingsWindowProperties), SettingsWindowGenerationContext.Default)
            .ConfigureAwait(false);
    }
    
    public class SettingsWindowProperties : IWindowProperties
    {
        public int? Top { get; set; }
        public int? Left { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public bool Maximized { get; set; }
        
        public int LastTab { get; set; }
    }
}



