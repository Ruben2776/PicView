using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Core.Config;

[JsonSourceGenerationOptions(AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(BatchResizeWindowConfig.BatchResizeWindowProperties))]
internal partial class BatchResizeWindowGenerationContext : JsonSerializerContext;
public class BatchResizeWindowConfig() : ConfigFile("BatchResizeWindow.json")
{
    public BatchResizeWindowProperties? WindowProperties { get; private set;  }

    public async Task LoadAsync()
    {
        CorrectPath ??= ConfigFileManager.ResolveDefaultConfigPath(this);
        try
        {
            if (File.Exists(CorrectPath))
            {
                var jsonString = await File.ReadAllTextAsync(CorrectPath).ConfigureAwait(false);
                if (JsonSerializer.Deserialize(
                        jsonString, typeof(BatchResizeWindowProperties), BatchResizeWindowGenerationContext.Default) is BatchResizeWindowProperties settings)
                {
                    WindowProperties = settings;
                }
                else
                {
                    WindowProperties = new BatchResizeWindowProperties();
                } 
            }
            else
            {
                WindowProperties = new BatchResizeWindowProperties();
            }
        }
        catch
        {
            WindowProperties = new BatchResizeWindowProperties();
        }
    }
    
    public async Task SaveAsync()
    {
        CorrectPath = await ConfigFileManager.SaveConfigFileAndReturnPathAsync(this,
            CorrectPath, WindowProperties, typeof(BatchResizeWindowProperties), BatchResizeWindowGenerationContext.Default).ConfigureAwait(false);
    }
    
    public class BatchResizeWindowProperties : IWindowProperties
    {
        public int? Top { get; set; }
        public int? Left { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public bool Maximized { get; set; }
        
        
        public bool IsQualityEnabled { get; set; }
        
        public int ConvertToIndex { get; set; }
        public int CompressionIndex { get; set; }
        public int ResizeIndex { get; set; }
        public int GenerateThumbnailsIndex { get; set; }
    }
}



