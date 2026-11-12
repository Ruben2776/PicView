using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Core.Config;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(ConvertWindowConfig.ConvertWindowProperties))]
internal partial class ConvertWindowGenerationContext : JsonSerializerContext;

public class ConvertWindowConfig() : ConfigFile("ConvertWindow.json")
{
    public ConvertWindowProperties? WindowProperties { get; private set; }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var jsonString = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
                if (JsonSerializer.Deserialize(
                        jsonString, typeof(ConvertWindowProperties), ConvertWindowGenerationContext.Default) is ConvertWindowProperties settings)
                {
                    if (settings.Width.HasValue && !double.IsFinite(settings.Width.Value))
                    {
                        settings.Width = null;
                    }
                    if (settings.Height.HasValue && !double.IsFinite(settings.Height.Value))
                    {
                        settings.Height = null;
                    }
                    WindowProperties = settings;
                }
                else
                {
                    WindowProperties = new ConvertWindowProperties();
                }
            }
            else
            {
                WindowProperties = new ConvertWindowProperties();
            }
        }
        catch
        {
            WindowProperties = new ConvertWindowProperties();
        }
    }

    public async Task SaveAsync()
    {
        if (WindowProperties is not null)
        {
            if (WindowProperties.Width.HasValue && !double.IsFinite(WindowProperties.Width.Value))
            {
                WindowProperties.Width = null;
            }
            if (WindowProperties.Height.HasValue && !double.IsFinite(WindowProperties.Height.Value))
            {
                WindowProperties.Height = null;
            }
        }

        await ConfigFileManager.SaveAsync(this, WindowProperties, typeof(ConvertWindowProperties), ConvertWindowGenerationContext.Default).ConfigureAwait(false);
    }

    public class ConvertWindowProperties : IWindowProperties
    {
        public int? Top { get; set; }
        public int? Left { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public bool Maximized { get; set; }
    }
}
