using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Core.Config;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(KeybindingWindowConfig.KeybindingWindowProperties))]
internal partial class KeybindingsWindowGenerationContext : JsonSerializerContext;

public class KeybindingWindowConfig() : ConfigFile("KeybindingWindowConfig.json")
{
    public KeybindingWindowProperties? WindowProperties { get; private set; }

    public async Task LoadAsync()
    {
        CorrectPath ??= ConfigFileManager.ResolveDefaultConfigPath(this);
        try
        {
            if (File.Exists(CorrectPath))
            {
                var jsonString = await File.ReadAllTextAsync(CorrectPath).ConfigureAwait(false);
                if (JsonSerializer.Deserialize(
                        jsonString, typeof(KeybindingWindowProperties),
                        KeybindingsWindowGenerationContext.Default) is KeybindingWindowProperties settings)
                {
                    WindowProperties = settings;
                }
                else
                {
                    WindowProperties = new KeybindingWindowProperties();
                }
            }
            else
            {
                WindowProperties = new KeybindingWindowProperties();
            }
        }
        catch
        {
            WindowProperties = new KeybindingWindowProperties();
        }
    }

    public async Task SaveAsync()
    {
        CorrectPath = await ConfigFileManager.SaveConfigFileAndReturnPathAsync(this,
            CorrectPath, WindowProperties, typeof(KeybindingWindowProperties),
            KeybindingsWindowGenerationContext.Default);
    }

    public class KeybindingWindowProperties : IWindowProperties
    {
        public int? Top { get; set; }
        public int? Left { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public bool Maximized { get; set; }
    }
}