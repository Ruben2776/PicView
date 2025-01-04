using System.Diagnostics;
using ImageMagick;

namespace PicView.Core.ImageDecoding;

public static class SaveImageFileHelper
{
    /// <summary>
    /// Saves an image asynchronously from a stream or file path with optional resizing, rotation, format conversion, 
    /// and compression settings (lossy or lossless).
    /// </summary>
    /// <param name="stream">The stream containing the image data. If null, the image will be loaded from the specified file path.</param>
    /// <param name="path">The path of the image file to load. If null, the image will be loaded from the stream.</param>
    /// <param name="destination">The path to save the processed image. If null, the image will be saved to the original path.</param>
    /// <param name="width">The target width of the image. If null, the image will not be resized based on width.</param>
    /// <param name="height">The target height of the image. If null, the image will not be resized based on height.</param>
    /// <param name="quality">The quality level of the saved image, as a percentage (0-100). If null, the default quality is used.</param>
    /// <param name="ext">The file extension of the output image (e.g., ".jpg", ".png"). If null, the original extension is kept.</param>
    /// <param name="rotationAngle">The angle to rotate the image, in degrees. If null, no rotation is applied.</param>
    /// <param name="percentage">The percentage by which to resize the image. If specified, both width and height are ignored.</param>
    /// <param name="losslessCompress">Indicates whether to apply lossless compression to the image.</param>
    /// <param name="lossyCompress">Indicates whether to apply lossy compression to the image.</param>
    /// <param name="respectAspectRatio">Indicates whether to maintain the aspect ratio when resizing.</param>
    /// <returns>A task representing the asynchronous operation, with a result of <c>true</c> if the image is saved successfully; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// If both width and height are null or zero, the image will not be resized.
    /// If the percentage is specified, it takes precedence over width and height for resizing.
    /// If both lossy and lossless compression are enabled, only one will be applied based on supported formats.
    /// </remarks>
    public static async Task<bool> SaveImageAsync(Stream? stream, string? path, string? destination = null,
        uint? width = null, uint? height = null, uint? quality = null, string? ext = null, double? rotationAngle = null,
        Percentage? percentage = null, bool losslessCompress = false, bool lossyCompress = false,
        bool respectAspectRatio = true)
    {
        try
        {
            using MagickImage magickImage = new();
            string writtenFile;

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

            if (percentage.HasValue)
            {
                magickImage.Resize(percentage.Value);
            }
            else if (width is not null)
            {
                if (height is not null)
                {
                    if (height > 0)
                    {
                        if (!respectAspectRatio)
                        {
                            var geometry = new MagickGeometry(width > 0 ? width.Value : 0, height.Value)
                                { IgnoreAspectRatio = true };
                            magickImage.Resize(geometry);
                        }
                        else
                        {
                            magickImage.Resize(width > 0 ? width.Value : 0, height.Value);
                        }
                    }
                }
                else
                {
                    magickImage.Resize(width.Value, 0);
                }
            }
            else if (height is not null)
            {
                if (width is not null)
                {
                    if (width > 0)
                    {
                        if (!respectAspectRatio)
                        {
                            var geometry = new MagickGeometry(width > 0 ? width.Value : 0, height.Value)
                                { IgnoreAspectRatio = true };
                            magickImage.Resize(geometry);
                        }
                        else
                        {
                            magickImage.Resize(width.Value, height > 0 ? height.Value : 0);
                        }
                    }
                }
                else
                {
                    magickImage.Resize(0, height.Value);
                }
            }

            if (rotationAngle is not null)
            {
                magickImage.Rotate(rotationAngle.Value);
            }

            var keepExt = string.IsNullOrEmpty(ext);
            if (!keepExt)
            {
                magickImage.Format = ext.ToLowerInvariant() switch
                {
                    ".webp" => MagickFormat.WebP,
                    ".jpeg" or ".jpg" => MagickFormat.Jpeg,
                    ".png" => MagickFormat.Png,
                    ".jxl" => MagickFormat.Jxl,
                    ".avif" => MagickFormat.Avif,
                    ".heic" => MagickFormat.Heic,
                    ".heif" => MagickFormat.Heif,
                    _ => magickImage.Format
                };
            }

            if (destination is not null)
            {
                await magickImage.WriteAsync(!keepExt ? Path.ChangeExtension(destination, ext) : destination)
                    .ConfigureAwait(false);
                writtenFile = destination;
            }
            else if (path is not null)
            {
                await magickImage.WriteAsync(!keepExt ? Path.ChangeExtension(path, ext) : path)
                    .ConfigureAwait(false);
                writtenFile = path;
            }
            else
            {
                return false;
            }

            if (lossyCompress || losslessCompress)
            {
                ImageOptimizer imageOptimizer = new()
                {
                    OptimalCompression = losslessCompress
                };
                if (imageOptimizer.IsSupported(writtenFile))
                {
                    imageOptimizer.Compress(writtenFile);
                }
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
    /// Resizes and optionally compresses an image asynchronously, with optional format conversion.
    /// </summary>
    /// <param name="fileInfo">The FileInfo object representing the image file to resize.</param>
    /// <param name="width">The target width of the resized image. Ignored if percentage is specified.</param>
    /// <param name="height">The target height of the resized image. Ignored if percentage is specified.</param>
    /// <param name="quality">The quality level of the resized image, as a percentage (0-100). Defaults to 100.</param>
    /// <param name="percentage">An optional percentage to resize the image by. If specified, width and height are ignored.</param>
    /// <param name="destination">The path to save the resized image. If null, the original file will be overwritten.</param>
    /// <param name="compress">Indicates whether to apply compression to the image after resizing. If null, no compression is applied.</param>
    /// <param name="ext">The file extension of the output image (e.g., ".jpg", ".png"). If null, the original extension is kept.</param>
    /// <returns>A task representing the asynchronous operation, with a result of <c>true</c> if the image is resized and saved successfully; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// If both width and height are provided, they will be used for resizing unless a percentage is specified.
    /// Compression is only applied if the specified format supports it.
    /// </remarks>
    public static async Task<bool> ResizeImageAsync(FileInfo fileInfo, uint width, uint height, uint? quality = 100,
        Percentage? percentage = null, string? destination = null, bool? compress = null, string? ext = null)
    {
        if (fileInfo.Exists == false)
        {
            return false;
        }

        var magick = new MagickImage
        {
            ColorSpace = ColorSpace.Transparent
        };

        if (quality.HasValue) // not inputting quality results in lower file size
        {
            magick.Quality = quality.Value;
        }

        try
        {
            if (fileInfo.Length < 2147483648)
            {
                await magick.ReadAsync(fileInfo).ConfigureAwait(false);
            }
            else
            {
                // ReSharper disable once MethodHasAsyncOverload
                magick.Read(fileInfo);
            }
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
                    magick.Format = Path.GetExtension(ext).ToLowerInvariant() switch
                    {
                        ".jpeg" or ".jpg" => MagickFormat.Jpeg,
                        ".png" => MagickFormat.Png,
                        ".jxl" => MagickFormat.Jxl,
                        ".gif" => MagickFormat.Gif,
                        ".webp" => MagickFormat.WebP,
                        ".heic" => MagickFormat.Heic,
                        ".heif" => MagickFormat.Heif,
                        _ => magick.Format
                    };
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

        if (!compress.HasValue)
        {
            return true;
        }

        ImageOptimizer imageOptimizer = new()
        {
            OptimalCompression = compress.Value
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