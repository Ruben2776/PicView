using ImageMagick;
using PicView.Core.DebugTools;

namespace PicView.Core.Gallery;

/// <summary>
/// What a gallery item reads from its file: the image's pixel size, and its XMP packet for the motion photo badge.
/// A class, so an item holds it as one reference that another thread replaces whole: a reader never pairs a width
/// from one read with a height from another.
/// </summary>
public sealed record GalleryFileDetails(uint PixelWidth, uint PixelHeight, string? XmpPacket)
{
    /// <summary>
    /// The details of a file that could not be read as an image: no pixel size and no XMP packet.
    /// </summary>
    public static GalleryFileDetails Unreadable { get; } = new(0, 0, null);

    /// <summary>
    /// Pings <paramref name="file"/> for its details.
    /// </summary>
    /// <returns>The details, or null when the file could not be read as an image.</returns>
    public static GalleryFileDetails? Read(FileInfo file)
    {
        try
        {
            using var magick = new MagickImage();
#pragma warning disable MA0042
            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
            magick.Ping(file);
#pragma warning restore MA0042

            string? xmpPacket = null;
            var data = magick.GetProfile("xmp")?.ToByteArray();
            if (data is not null)
            {
                xmpPacket = System.Text.Encoding.UTF8.GetString(data);
            }

            return new GalleryFileDetails(magick.Width, magick.Height, xmpPacket);
        }
        catch (Exception ex)
        {
#if DEBUG
            DebugHelper.LogDebug(nameof(GalleryFileDetails), nameof(Read), ex);
#endif
            return null;
        }
    }
}
