using ImageMagick;
using PicView.Core.DebugTools;

namespace PicView.Core.ImageDecoding;

/// <summary>
/// Provides methods to decode Base64 strings into MagickImage objects and validate Base64 strings.
/// </summary>
public static class Base64Decoder
{
    /// <summary>
    /// Converts a Base64 encoded string into a MagickImage object.
    /// </summary>
    /// <param name="base64">The Base64 encoded string to convert.</param>
    /// <returns>A MagickImage object if successful, otherwise null.</returns>
    public static MagickImage? Base64ToMagickImage(string base64)
    {
        try
        {
            var base64Data = Convert.FromBase64String(base64);
            var magickImage = new MagickImage
            {
                Quality = 100,
                ColorSpace = ColorSpace.Transparent
            };

            var readSettings = new MagickReadSettings
            {
                Density = new Density(300, 300),
                BackgroundColor = MagickColors.Transparent
            };

            magickImage.Read(new MemoryStream(base64Data), readSettings);
            return magickImage;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(Base64Decoder), nameof(Base64ToMagickImage), e);
            return null;
        }
    }

    /// <summary>
    /// Converts a Base64 encoded file from a FileInfo object into a MagickImage object.
    /// </summary>
    /// <param name="fileInfo">The base64 FileInfo</param>
    /// <returns>A MagickImage object if successful, otherwise null.</returns>
    public static async Task<MagickImage?> Base64ToMagickImage(FileInfo fileInfo)
    {
        var base64String = await File.ReadAllTextAsync(fileInfo.FullName).ConfigureAwait(false);
        return Base64ToMagickImage(base64String);
    }

    /// <summary>
    /// Determines whether a string is a valid Base64 string.
    /// </summary>
    /// <param name="base64">The string to check.</param>
    /// <returns>String as a valid Base64 string; otherwise, "".</returns>
    public static string IsBase64String(string base64)
    {
        if (string.IsNullOrEmpty(base64))
        {
            return "";
        }

        if (base64.StartsWith("data:image/webp;base64,", StringComparison.OrdinalIgnoreCase))
        {
            base64 = base64["data:image/webp;base64,".Length..];
        }

        if (base64.StartsWith("data:image/jpeg;base64,", StringComparison.OrdinalIgnoreCase))
        {
            base64 = base64["data:image/jpeg;base64,".Length..];
        }
        
        if (base64.StartsWith("data:image/png;base64,", StringComparison.OrdinalIgnoreCase))
        {
            base64 = base64["data:image/png;base64,".Length..];
        }
        
        if (base64.StartsWith("data:image/gif;base64,", StringComparison.OrdinalIgnoreCase))
        {
            base64 = base64["data:image/gif;base64,".Length..];
        }

        var buffer = new Span<byte>(new byte[base64.Length]);
        return Convert.TryFromBase64String(base64, buffer, out _) ? base64 : "";
    }
}