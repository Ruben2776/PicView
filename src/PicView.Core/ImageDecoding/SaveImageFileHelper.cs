using System.Diagnostics;
using ImageMagick;

namespace PicView.Core.ImageDecoding;

    public static class SaveImageFileHelper
    {
            /// <summary>
    /// Saves an image from a stream or file path asynchronously with optional transformations.
    /// </summary>
    /// <param name="stream">The stream containing the image data.</param>
    /// <param name="path">The path of the image file to read.</param>
    /// <param name="destination">The path of the destination file to save the image.</param>
    /// <param name="width">The target width of the image.</param>
    /// <param name="height">The target height of the image.</param>
    /// <param name="quality">The quality level of the image.</param>
    /// <param name="ext">The file extension of the output image.</param>
    /// <returns>True if the image is saved successfully; otherwise, false.</returns>
    public static async Task<bool> SaveImageAsync(Stream? stream, string? path, string? destination, int? width,
        int? height, int? quality, string? ext)
    {
        try
        {
            using MagickImage magickImage = new();

            if (stream is not null)
            {
                await magickImage.ReadAsync(stream).ConfigureAwait(false);
            }
            else if (path is not null)
            {
                await magickImage.ReadAsync(path).ConfigureAwait(false);
            }
            else
            {
                return false;
            }

            if (quality is not null)
            {
                magickImage.Quality = quality.Value;
            }

            if (width is not null)
            {
                magickImage.Resize(width.Value, 0);
            }
            else if (height is not null)
            {
                magickImage.Resize(0, height.Value);
            }

            if (!string.IsNullOrEmpty(ext))
            {
                switch (ext.ToLowerInvariant())
                {
                    case ".webp":
                        magickImage.Format = MagickFormat.WebP;
                        break;

                    case ".jpg":
                        magickImage.Format = MagickFormat.Jpeg;
                        break;

                    case ".png":
                        magickImage.Format = MagickFormat.Png;
                        break;
                }
            }

            if (destination is not null)
            {
                await magickImage.WriteAsync(ext is not null ? Path.ChangeExtension(destination, ext) : destination)
                    .ConfigureAwait(false);
            }
            else
            {
                await magickImage.WriteAsync(ext is not null ? Path.ChangeExtension(path, ext) : path)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine(exception);
#endif
            return false;
        }

        return true;
    }
    }
