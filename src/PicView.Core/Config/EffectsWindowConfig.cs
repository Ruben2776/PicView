using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Core.Config;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(EffectsWindowConfig.EffectsWindowProperties))]
internal partial class EffectsWindowGenerationContext : JsonSerializerContext;

public class EffectsWindowConfig() : ConfigFile("EffectsWindow.json")
{
    public EffectsWindowProperties? WindowProperties { get; private set; }

    public async Task LoadAsync()
    {
        CorrectPath ??= ConfigFileManager.ResolveDefaultConfigPath(this);
        try
        {
            if (File.Exists(CorrectPath))
            {
                var jsonString = await File.ReadAllTextAsync(CorrectPath).ConfigureAwait(false);
                if (JsonSerializer.Deserialize(
                        jsonString, typeof(EffectsWindowProperties), EffectsWindowGenerationContext.Default) is EffectsWindowProperties settings)
                {
                    WindowProperties = settings;
                }
                else
                {
                    WindowProperties = new EffectsWindowProperties();
                }
            }
            else
            {
                WindowProperties = new EffectsWindowProperties();
            }
        }
        catch
        {
            WindowProperties = new EffectsWindowProperties();
        }
    }

    public async Task SaveAsync()
    {
        CorrectPath = await ConfigFileManager.SaveConfigFileAndReturnPathAsync(this,
            CorrectPath, WindowProperties, typeof(EffectsWindowProperties), EffectsWindowGenerationContext.Default);
    }

    public class EffectsWindowProperties : IWindowProperties
    {
        public int? Top { get; set; }
        public int? Left { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public bool Maximized { get; set; }
    }
}
