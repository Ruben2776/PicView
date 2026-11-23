using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.Saving;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

/// <summary>
/// Reads the pixels of a tab's in-memory Avalonia bitmap, such as a clipboard image.
/// </summary>
public sealed class AvaloniaSingleImagePixels : ISingleImagePixels
{
    /// <inheritdoc />
    /// <remarks>Runs off the UI thread: saving a bitmap to a stream does not touch UI state.</remarks>
    public MagickImage? Read(TabViewModel tab)
    {
        if (tab.Image.CurrentValue is not Bitmap bitmap)
        {
            return null;
        }

        using var pngStream = new MemoryStream();
        bitmap.Save(pngStream, PngBitmapEncoderOptions.Default);
        pngStream.Position = 0;
        return new MagickImage(pngStream);
    }
}
