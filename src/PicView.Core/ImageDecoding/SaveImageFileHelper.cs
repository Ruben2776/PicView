using ImageMagick;
using PicView.Core.DebugTools;

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
    public static async ValueTask<bool> SaveImageAsync(Stream? stream, string? path, string? destination = null,
        uint? width = null, uint? height = null, uint? quality = null, string? ext = null, double? rotationAngle = null,
        Percentage? percentage = null, bool losslessCompress = false, bool lossyCompress = false,
        bool respectAspectRatio = true, bool flipImage = false)
    {
        string? tempDestination = null;
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
            else if (width is > 0 && height is > 0)
            {
                 magickImage.Resize(width.Value, height.Value);
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

            if (rotationAngle is not null && rotationAngle.Value is not 0)
            {
                magickImage.Rotate(rotationAngle.Value);
            }

            // Turn, then mirror, as the view does
            if (flipImage)
            {
                magickImage.Flop();
            }

            var keepExt = string.IsNullOrEmpty(ext);
            if (!keepExt)
            {
                magickImage.Format = FormatFor(ext, magickImage.Format);
            }

            // ---> Core Fix: Never write directly onto the file you just read
            if (destination is not null)
            {
                await magickImage.WriteAsync(!keepExt ? Path.ChangeExtension(destination, ext) : destination)
                    .ConfigureAwait(false);
                writtenFile = destination;
            }
            else if (path is not null)
            {
                // Write to temp file, then replace original
                var dir = Path.GetDirectoryName(path)!;
                var tempFile = Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(path)}_tmp{Path.GetExtension(path)}");

                tempDestination = tempFile;
                await magickImage.WriteAsync(!keepExt ? Path.ChangeExtension(tempFile, ext) : tempFile)
                    .ConfigureAwait(false);

                // Now move - overwrite
                File.Replace(tempFile, path, null, true);
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
            DebugHelper.LogDebug(nameof(SaveImageFileHelper), nameof(SaveImageAsync), exception);
            // cleanup temp file if exists
            if (tempDestination == null || !File.Exists(tempDestination))
            {
                return false;
            }

            try
            {
                File.Delete(tempDestination);
            }
            catch
            {
                DebugHelper.LogDebug(nameof(SaveImageFileHelper), nameof(SaveImageAsync),"Failed to delete file");
            }
            return false;
        }

        return true;
    }



    /// <summary>
    /// The format a file with the extension <paramref name="ext"/> is saved in, or <paramref name="fallback"/> for an
    /// extension with no format of its own.
    /// </summary>
    internal static MagickFormat FormatFor(string ext, MagickFormat fallback) => ext.ToLowerInvariant() switch
    {
        ".webp" => MagickFormat.WebP,
        ".jpeg" or ".jpg" => MagickFormat.Jpeg,
        ".png" => MagickFormat.Png,
        ".jxl" => MagickFormat.Jxl,
        ".avif" => MagickFormat.Avif,
        ".heic" => MagickFormat.Heic,
        ".heif" => MagickFormat.Heif,
        ".bmp" => MagickFormat.Bmp,
        ".gif" => MagickFormat.Gif,
        ".tif" or ".tiff" => MagickFormat.Tiff,
        _ => fallback
    };
}