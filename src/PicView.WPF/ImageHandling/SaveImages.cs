using ImageMagick;
using PicView.WPF.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PicView.WPF.ImageHandling;

internal static class SaveImages
{
    internal static async Task<bool> SaveImageAsync(string path, string destination)
    {
        return await SaveImageAsync(0, false, null, path, destination, null, false)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Saves an image asynchronously with optional transformations.
    /// </summary>
    /// <param name="rotate">The rotation angle in degrees.</param>
    /// <param name="flipped">Indicates whether the image should be flipped horizontally.</param>
    /// <param name="bitmapSource">The BitmapSource representing the image.</param>
    /// <param name="path">The path of the image file to read.</param>
    /// <param name="destination">The path of the destination file to save the image.</param>
    /// <param name="rect">The rectangle specifying the crop area.</param>
    /// <param name="hlsl">Indicates whether an HLSL effect has been applied.</param>
    /// <returns>True if the image is saved successfully; otherwise, false.</returns>
    internal static async Task<bool> SaveImageAsync(double rotate, bool flipped, BitmapSource? bitmapSource,
        string? path, string destination, Int32Rect? rect, bool hlsl)
    {
        MagickImage? magickImage = new();
        try
        {
            if (hlsl)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    () => { magickImage = Image2BitmapSource.GetRenderedMagickImage(); });
            }
            else if (bitmapSource is not null)
            {
                var encoder = new PngBitmapEncoder();

                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    () => { encoder.Frames.Add(BitmapFrame.Create(bitmapSource)); });

                using var stream = new MemoryStream();
                encoder.Save(stream);
                magickImage.Read(stream.ToArray());
            }
            else if (string.IsNullOrEmpty(path) == false)
            {
                await magickImage.ReadAsync(path).ConfigureAwait(false);
            }
            else
            {
                return false;
            }

            // Apply transformation values
            if (flipped && bitmapSource is not null && hlsl is false)
            {
                magickImage.Flop();
            }

            if (rect is not null && bitmapSource is not null && hlsl is false)
            {
                magickImage.Crop(
                    new MagickGeometry(rect.Value.X, rect.Value.Y, rect.Value.Width, rect.Value.Height));
            }

            if (rotate != 0)
            {
                magickImage.Rotate(rotate);
            }

            if (!string.IsNullOrEmpty(destination))
            {
                if (Path.GetExtension(path) != Path.GetExtension(destination))
                {
                    switch (Path.GetExtension(destination).ToLowerInvariant())
                    {
                        case ".jpeg":
                        case ".jpg":
                            magickImage.Format = MagickFormat.Jpeg;
                            break;

                        case ".png":
                            magickImage.Format = MagickFormat.Png;
                            break;

                        case ".jxl":
                            magickImage.Format = MagickFormat.Jxl;
                            break;

                        case ".gif":
                            magickImage.Format = MagickFormat.Gif;
                            break;

                        case ".webp":
                            magickImage.Format = MagickFormat.WebP;
                            break;

                        case ".heic":
                            magickImage.Format = MagickFormat.Heic;
                            break;

                        case ".heif":
                            magickImage.Format = MagickFormat.Heif;
                            break;
                    }
                }

                await magickImage.WriteAsync(destination).ConfigureAwait(false);
            }

            magickImage.Dispose();
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
    internal static async Task<bool> SaveImageAsync(Stream? stream, string? path, string? destination, int? width,
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