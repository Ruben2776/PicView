using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Core.Config;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(ImageInfoWindowConfig.ImageInfoWindowProperties))]
internal partial class ImageInfoWindowGenerationContext : JsonSerializerContext;
public class ImageInfoWindowConfig() : ConfigFile("ImageInfoWindow.json")
{
    public ImageInfoWindowProperties? WindowProperties { get; private set;  }

    public async Task LoadAsync()
    {
        CorrectPath ??= ConfigFileManager.ResolveDefaultConfigPath(this);
        try
        {
            if (File.Exists(CorrectPath))
            {
                var jsonString = await File.ReadAllTextAsync(CorrectPath).ConfigureAwait(false);
                if (JsonSerializer.Deserialize(
                        jsonString, typeof(ImageInfoWindowProperties), ImageInfoWindowGenerationContext.Default) is ImageInfoWindowProperties settings)
                {
                    WindowProperties = settings;
                }
                else
                {
                    WindowProperties = new ImageInfoWindowProperties();
                } 
            }
            else
            {
                WindowProperties = new ImageInfoWindowProperties();
            }
        }
        catch
        {
            WindowProperties = new ImageInfoWindowProperties();
        }
    }
    
    public async Task SaveAsync()
    {
        CorrectPath = await ConfigFileManager.SaveConfigFileAndReturnPathAsync(this,
            CorrectPath, WindowProperties, typeof(ImageInfoWindowProperties), ImageInfoWindowGenerationContext.Default).ConfigureAwait(false);
    }
    
    public class ImageInfoWindowProperties : IWindowProperties
    {
        public int? Top { get; set; }
        public int? Left { get; set; }
        public double? Width { get; set; } = 850;
        public double? Height { get; set; } = 495;
        public bool Maximized { get; set; }
    }
}



