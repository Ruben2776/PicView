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
        CorrectPath ??= ConfigFileManager.ResolveDefaultConfigPath(this);
        try
        {
            if (File.Exists(CorrectPath))
            {
                var jsonBytes = File.ReadAllBytes(CorrectPath);
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
        CorrectPath = await ConfigFileManager.SaveConfigFileAndReturnPathAsync(this,
            CorrectPath, WindowProperties, typeof(KeybindingWindowProperties),
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