using ImageMagick;
using ImageMagick.Formats;
using SkiaSharp;
using System.Diagnostics;
using PicView.Core.FileHandling;

namespace PicView.Core.ImageDecoding;

/// <summary>
/// Provides methods for decoding various image formats.
/// </summary>
public static class ImageDecoder
{
    /// <summary>
    /// Asynchronously reads and returns a MagickImage from the specified FileInfo.
    /// </summary>
    /// <param name="fileInfo">The FileInfo representing the image file.</param>
    /// <param name="extension">The file extension</param>
    /// <returns>A Task containing the MagickImage if successful, otherwise null.</returns>
    public static async Task<MagickImage?> GetMagickImageAsync(FileInfo fileInfo, string extension)
    {
        try
        {
            var magickImage = new MagickImage();
            MagickFormat format;

            switch (extension)
            {
                case ".heic":
                case ".heif":
                    magickImage.Settings.SetDefines(new HeicReadDefines
                    {
                        PreserveOrientation = true,
                        DepthImage = true,
                    });
                    format = extension is ".heic" ? MagickFormat.Heic : MagickFormat.Heif;
                    break;

                case ".jp2":
                    magickImage.Settings.SetDefines(new Jp2ReadDefines
                    {
                        QualityLayers = 100,
                    });
                    format = MagickFormat.Jp2;
                    break;

                case ".tif":
                case ".tiff":
                    magickImage.Settings.SetDefines(new TiffReadDefines
                    {
                        IgnoreTags = new[]
                        {
                            "34022", // ColorTable
                            "34025", // ImageColorValue
                            "34026", // BackgroundColorValue
                            "32928",
                        },
                    });
                    format = MagickFormat.Tif;
                    break;

                case ".psd":
                    format = MagickFormat.Psd;
                    break;

                default:
                    format = MagickFormat.Unknown;
                    break;
            }

            magickImage.Format = format;
            magickImage.Read(await FileHelper.GetBytesFromFile(fileInfo), format);

            magickImage?.AutoOrient();
            return magickImage;
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(GetMagickImageAsync)} {fileInfo.Name} exception, \n {e.Message}");
#endif
            return null;
        }
    }

    /// <summary>
    /// Asynchronously reads and returns a MagickImage from the specified FileInfo for SVG format.
    /// </summary>
    /// <param name="fileInfo">The FileInfo representing the SVG file.</param>
    /// <param name="magickFormat">The MagickFormat for the SVG image (SVG or SVGZ).</param>
    /// <returns>A Task containing the MagickImage for SVG if successful, otherwise null.</returns>
    public static async Task<MagickImage?> GetMagickSvgAsync(FileInfo fileInfo, MagickFormat magickFormat)
    {
        try
        {
            var magickImage = new MagickImage
            {
                Quality = 100,
                ColorSpace = ColorSpace.Transparent,
                BackgroundColor = MagickColors.Transparent,
                Format = magickFormat,
            };
            magickImage.Read(await FileHelper.GetBytesFromFile(fileInfo));

            magickImage.Settings.BackgroundColor = MagickColors.Transparent;
            magickImage.Settings.FillColor = MagickColors.Transparent;
            magickImage.Settings.SetDefine("svg:xml-parse-huge", "true");

            return magickImage;
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(GetMagickSvgAsync)} {fileInfo.Name} exception, \n {e.Message}");
#endif
            return null;
        }
    }

    public static async Task<MagickImage?> Base64ToMagickImage(string base64)
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

            await magickImage.ReadAsync(new MemoryStream(base64Data), readSettings).ConfigureAwait(false);
            return magickImage;
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(Base64ToMagickImage)} exception:\n{e.Message}");
#endif
            return null;
        }
    }

    public static async Task<MagickImage?> Base64ToMagickImage(FileInfo fileInfo)
    {
        var base64String = await File.ReadAllTextAsync(fileInfo.FullName).ConfigureAwait(false);
        return await Base64ToMagickImage(base64String).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously reads and returns an SKBitmap from the specified FileInfo.
    /// </summary>
    /// <param name="fileInfo">The FileInfo representing the image file.</param>
    /// <returns>A Task containing the SKBitmap if successful, otherwise null.</returns>
    // ReSharper disable once InconsistentNaming
    public static async Task<SKBitmap?> GetSKBitmapAsync(this FileInfo fileInfo)
    {
        try
        {
            return SKBitmap.Decode(await FileHelper.GetBytesFromFile(fileInfo));
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(GetSKBitmapAsync)} {fileInfo.Name} exception:\n{e.Message}");
#endif
            return null;
        }
    }
}