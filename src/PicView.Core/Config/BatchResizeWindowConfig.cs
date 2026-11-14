using System.Text.Json.Serialization;

namespace PicView.Core.Config;

public sealed class BatchResizeWindowConfig()
    : RememberedWindowConfig<BatchResizeWindowConfig.BatchResizeWindowProperties>("BatchResizeWindow.json")
{
    public class BatchResizeWindowProperties : RememberedWindowProperties
    {
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
