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

    public void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var jsonBytes = File.ReadAllBytes(FilePath);
                if (JsonSerializer.Deserialize(
                        jsonBytes, typeof(KeybindingWindowProperties),
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
        await ConfigFileManager.SaveAsync(this, WindowProperties, typeof(KeybindingWindowProperties),
            KeybindingsWindowGenerationContext.Default).ConfigureAwait(false);
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