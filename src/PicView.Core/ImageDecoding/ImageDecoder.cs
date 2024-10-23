using System.Diagnostics;
using ImageMagick;
using ImageMagick.Formats;

namespace PicView.Core.ImageDecoding;

/// <summary>
/// Provides methods for decoding various image formats.
/// </summary>
public static class ImageDecoder
{
    private static readonly string[] Defines =
    [
        "34022", // ColorTable
        "34025", // ImageColorValue
        "34026", // BackgroundColorValue
        "32928"
    ];

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
            magickImage.Quality = 100;

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
                        IgnoreTags = Defines,
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

            if (fileInfo.Length >= 2147483648)
            {
                await Task.Run(() =>
                {
                    // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
                    // ReSharper disable once MethodHasAsyncOverload
                    magickImage.Read(fileInfo);
                }).ConfigureAwait(false);
            }
            else
            {
                await magickImage.ReadAsync(fileInfo, format).ConfigureAwait(false);
            }

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
            if (fileInfo.Length >= 2147483648)
            {
                // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
                // ReSharper disable once MethodHasAsyncOverload
                magickImage.Read(fileInfo);
            }
            else
            {
                await magickImage.ReadAsync(fileInfo, magickFormat).ConfigureAwait(false);
            }

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
#if DEBUG
            Trace.WriteLine($"{nameof(Base64ToMagickImage)} exception:\n{e.Message}");
#endif
            return null;
        }
    }

    public static async Task<MagickImage?> Base64ToMagickImage(FileInfo fileInfo)
    {
        var base64String = await File.ReadAllTextAsync(fileInfo.FullName).ConfigureAwait(false);
        return Base64ToMagickImage(base64String);
    }
}