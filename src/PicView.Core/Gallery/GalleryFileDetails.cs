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
            if (IsCameraRaw(file) || !TryPingStream(magick, file))
            {
                // A camera RAW is read by its extension's raw coder: its bytes look like a TIFF whose first image is a
                // preview. A stream names no format either, so a file whose format has no signature, such as a TGA,
                // is pinged by its path too, where its extension says what it is
#pragma warning disable MA0042
                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                magick.Ping(file);
#pragma warning restore MA0042
            }

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

    /// <summary>
    /// Whether the raw coder reads <paramref name="file"/>'s format, going by its extension.
    /// </summary>
    private static bool IsCameraRaw(FileInfo file) => MagickFormatInfo.Create(file)?.ModuleFormat is MagickFormat.Dng;

    /// <summary>
    /// Pings <paramref name="file"/> as a stream. Before each of the several opens a ping by path makes, ImageMagick
    /// checks the path against its security policy, which costs more than the ping itself. The stream is unbuffered,
    /// as ImageMagick reads in its own chunks, and shared for writing, so a file another program holds open still
    /// reads.
    /// </summary>
    /// <returns>Whether the ping succeeded.</returns>
    private static bool TryPingStream(MagickImage magick, FileInfo file)
    {
        try
        {
            using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                bufferSize: 1);
#pragma warning disable MA0042
            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
            magick.Ping(stream);
#pragma warning restore MA0042
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
