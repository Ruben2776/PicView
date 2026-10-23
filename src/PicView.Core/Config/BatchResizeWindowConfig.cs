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

        public uint? Quality { get; set; }

        /// <summary>
        /// The <see cref="BatchResize.ConversionTarget"/> by name.
        /// </summary>
        public string? Conversion { get; set; }

        /// <summary>
        /// The <see cref="BatchResize.CompressionMode"/> by name.
        /// </summary>
        public string? Compression { get; set; }

        /// <summary>
        /// The <see cref="BatchResize.ResizeMode"/> by name.
        /// </summary>
        public string? Resize { get; set; }

        public bool KeepAspectRatio { get; set; } = true;

        public List<RememberedThumbnail>? Thumbnails { get; set; }

        // Settings files from before the choices were remembered by name; read when the name is missing
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ConvertToIndex { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? CompressionIndex { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ResizeIndex { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? GenerateThumbnailsIndex { get; set; }
    }

    public class RememberedThumbnail
    {
        public string Subfolder { get; set; } = string.Empty;

        /// <summary>
        /// The <see cref="BatchResize.ThumbnailSize"/> by name.
        /// </summary>
        public string? Size { get; set; }

        public double Value { get; set; }
    }
}



