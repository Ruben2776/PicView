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

                    case ".avif":
                        magickImage.Format = MagickFormat.Avif;
                        break;

                    case ".heic":
                        magickImage.Format = MagickFormat.Heic;
                        break;

                    case ".heif":
                        magickImage.Format = MagickFormat.Heif;
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

    /// <summary>
    /// Resizes an image asynchronously with optional compression and format conversion.
    /// </summary>
    /// <param name="fileInfo">The FileInfo of the image file to resize.</param>
    /// <param name="width">The target width of the image.</param>
    /// <param name="height">The target height of the image.</param>
    /// <param name="quality">The quality level of the image.</param>
    /// <param name="percentage">The percentage value to resize the image.</param>
    /// <param name="destination">The path of the destination file to save the resized image.</param>
    /// <param name="compress">Indicates whether to compress the image.</param>
    /// <param name="ext">The file extension of the output image.</param>
    /// <returns>True if the image is resized and saved successfully; otherwise, false.</returns>
    public static async Task<bool> ResizeImageAsync(FileInfo fileInfo, int width, int height, int quality = 100,
        Percentage? percentage = null, string? destination = null, bool? compress = null, string? ext = null)
    {
        if (fileInfo.Exists == false)
        {
            return false;
        }

        if (width < 0 && percentage is not null || height < 0 && percentage is not null)
        {
            return false;
        }

        var magick = new MagickImage
        {
            ColorSpace = ColorSpace.Transparent
        };

        if (quality > 0) // not inputting quality results in lower file size
        {
            magick.Quality = quality;
        }

        try
        {
            if (fileInfo.Length < 2147483648)
                await magick.ReadAsync(fileInfo).ConfigureAwait(false);
            // ReSharper disable once MethodHasAsyncOverload
            else magick.Read(fileInfo);
        }
        catch (MagickException e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(ResizeImageAsync)} magic read exception caught \n {e.Message}");
#endif
            return false;
        }

        try
        {
            if (percentage is not null)
            {
                magick.Resize(percentage.Value);
            }
            else
            {
                magick.Resize(width, height);
            }

            if (destination is null)
            {
                if (ext is not null)
                {
                    Path.ChangeExtension(fileInfo.Extension, ext);
                    switch (Path.GetExtension(ext).ToLowerInvariant())
                    {
                        case ".jpeg":
                        case ".jpg":
                            magick.Format = MagickFormat.Jpeg;
                            break;

                        case ".png":
                            magick.Format = MagickFormat.Png;
                            break;

                        case ".jxl":
                            magick.Format = MagickFormat.Jxl;
                            break;

                        case ".gif":
                            magick.Format = MagickFormat.Gif;
                            break;

                        case ".webp":
                            magick.Format = MagickFormat.WebP;
                            break;

                        case ".heic":
                            magick.Format = MagickFormat.Heic;
                            break;

                        case ".heif":
                            magick.Format = MagickFormat.Heif;
                            break;
                    }
                }

                await magick.WriteAsync(fileInfo).ConfigureAwait(false);
            }
            else
            {
                var dir = Path.GetDirectoryName(destination);
                if (dir is null)
                {
                    return false;
                }

                if (Directory.Exists(dir) == false)
                {
                    Directory.CreateDirectory(dir);
                }

                if (ext is not null)
                {
                    Path.ChangeExtension(destination, ext);
                }

                await magick.WriteAsync(destination).ConfigureAwait(false);
            }
        }
        catch (MagickException e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(ResizeImageAsync)} exception caught \n {e.Message}");
#endif
            return false;
        }

        magick.Dispose();

        if (!compress.HasValue) return true;
        ImageOptimizer imageOptimizer = new()
        {
            OptimalCompression = compress.Value,
        };

        var x = destination ?? fileInfo.FullName;

        if (imageOptimizer.IsSupported(x) == false)
        {
            return true;
        }

        try
        {
            imageOptimizer.Compress(x);
        }
        catch (Exception)
        {
            return true;
        }

        return true;
    }
}