using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.Svg;
using PicView.Core.DebugTools;
using PicView.Core.Exif;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;

namespace PicView.Avalonia.ImageHandling;

public static class GetImageModel
{
    /// <inheritdoc cref="GetImageModelAsync(System.IO.FileInfo, MagickImage)"/>
    public static async ValueTask<ImageModel> GetImageModelAsync(FileInfo fileInfo) =>
        await GetImageModelAsync(fileInfo, null).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously retrieves an <see cref="ImageModel"/> instance based on the provided file and optional <see cref="MagickImage"/>.
    /// </summary>
    /// <param name="fileInfo">The file information of the image to process.</param>
    /// <param name="magickImage">An optional <see cref="MagickImage"/> instance. If null, a new instance will be created internally.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the constructed <see cref="ImageModel"/>.</returns>
    public static async ValueTask<ImageModel> GetImageModelAsync(FileInfo fileInfo, MagickImage? magickImage)
    {
        if (fileInfo is null)
        {
            DebugHelper.LogDebug(nameof(GetImageModel), nameof(GetImageModelAsync), "fileInfo is null");
            return CreateErrorImageModel(null);
        }

        var imageModel = new ImageModel { FileInfo = fileInfo };
        var shouldDisposeMagickImage = magickImage is null;

        try
        {
            // Initialize MagickImage if not provided
            magickImage ??= GetImage.CreateAndPingMagickImage(fileInfo);

            // Extract metadata
            // Check if rotation is needed
            var orientation = ExifOrientationHelper.GetImageOrientation(magickImage);
            var shouldAutoOrient = orientation is not (ExifOrientation.None or ExifOrientation.Horizontal);
            
            imageModel.Format = magickImage.Format;
            
            if (fileInfo.Extension.Equals(".b64", StringComparison.InvariantCultureIgnoreCase))
            {
                await ProcessBase64Async(fileInfo, MagickFormat.Data, imageModel).ConfigureAwait(false);
                return imageModel;
            }

            // Process the image based on type
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (magickImage.Format)
            {
                case MagickFormat.WebP: 
                case MagickFormat.WebM:
                    // If rotation is needed, we use the Magick path (NonStandard) to apply AutoOrient.
                    // Otherwise we use the faster SkBitmap (Avalonia native) path.
                    if (shouldAutoOrient)
                    {
                        await ProcessNonStandardImageAsync(fileInfo, imageModel, magickImage).ConfigureAwait(false);
                    }
                    else
                    {
                        await ProcessSkBitmapAsync(fileInfo, magickImage.Format, imageModel).ConfigureAwait(false);
                    }

                    if (ImageAnalyzer.IsAnimated(fileInfo))
                    {
                        imageModel.ImageType = ImageType.AnimatedWebp;
                    }
                    break;

                case MagickFormat.Gif:
                case MagickFormat.Gif87:
                    if (shouldAutoOrient)
                    {
                        await ProcessNonStandardImageAsync(fileInfo, imageModel, magickImage).ConfigureAwait(false);
                    }
                    else
                    {
                        await ProcessSkBitmapAsync(fileInfo, magickImage.Format, imageModel).ConfigureAwait(false);
                    }

                    if (ImageAnalyzer.IsAnimated(fileInfo))
                    {
                        imageModel.ImageType = ImageType.AnimatedGif;
                    }
                    break;

                case MagickFormat.Png:
                case MagickFormat.Png00:
                case MagickFormat.Png8:
                case MagickFormat.Png24:
                case MagickFormat.Png32:
                case MagickFormat.Png48:
                case MagickFormat.Png64:
                case MagickFormat.APng: // TODO add animation one day
                case MagickFormat.Jpe:
                case MagickFormat.Jpeg:
                case MagickFormat.Pjpeg:
                case MagickFormat.Bmp:
                case MagickFormat.Tif:
                case MagickFormat.Tiff:
                case MagickFormat.Ico:
                case MagickFormat.Icon:
                case MagickFormat.Wbmp:
                    if (shouldAutoOrient)
                    {
                        await ProcessNonStandardImageAsync(fileInfo, imageModel, magickImage).ConfigureAwait(false);
                    }
                    else
                    {
                        await ProcessSkBitmapAsync(fileInfo, magickImage.Format, imageModel).ConfigureAwait(false);
                    }
                    break;

                case MagickFormat.Svg:
                case MagickFormat.Svgz:
                    await ProcessSvg(fileInfo, imageModel, magickImage);
                    break;
                
                case MagickFormat.Arw:
                case MagickFormat.Nef:
                case MagickFormat.Dng:
                case MagickFormat.Cr2:
                case MagickFormat.Rw2:
                    await ProcessRawImageAsync(fileInfo, imageModel, magickImage).ConfigureAwait(false);
                    break;

                default:
                    await ProcessNonStandardImageAsync(fileInfo, imageModel, magickImage).ConfigureAwait(false);
                    break;
            }

            return imageModel;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImageModel), nameof(GetImageModelAsync), e);
            return CreateErrorImageModel(fileInfo);
        }
        finally
        {
            if (shouldDisposeMagickImage)
            {
                magickImage?.Dispose();
            }
        }
    }

    public static void SetBitmapProperties(Bitmap? bitmap, ImageModel imageModel, MagickFormat format, ImageType imageType = ImageType.Bitmap)
    {
        imageModel.Image = bitmap;
        if (bitmap is null)
        {
            imageModel.PixelWidth = 0;
            imageModel.PixelHeight = 0;
            imageModel.ImageType = ImageType.Invalid;
            imageModel.DpiX = 0;
            imageModel.DpiY = 0;
            return;
        }
        imageModel.PixelWidth = bitmap.PixelSize.Width;
        imageModel.PixelHeight = bitmap.PixelSize.Height;
        imageModel.ImageType = imageType;
        imageModel.DpiX = (ushort)bitmap.Dpi.X;
        imageModel.DpiY = (ushort)bitmap.Dpi.Y;
        imageModel.Format = format;
    }

    private static ImageModel CreateErrorImageModel(FileInfo? fileInfo)
    {
        return new ImageModel
        {
            FileInfo = fileInfo,
            ImageType = ImageType.Invalid,
            Image = null, // TODO replace with error image
            PixelHeight = 0,
            PixelWidth = 0,
            DpiX = 0,
            DpiY = 0,
            Orientation = ExifOrientation.None
        };
    }

    #region Image Processing Methods

    private static async ValueTask ProcessSkBitmapAsync(FileInfo fileInfo, MagickFormat format, ImageModel imageModel)
    {
        var bitmap = await GetImage.GetSkBitmapAsync(fileInfo).ConfigureAwait(false);
        SetBitmapProperties(bitmap, imageModel, format);
    }

    private static async Task ProcessSvg(FileInfo fileInfo, ImageModel imageModel, MagickImage magickImage)
    {
        var svgData = await SvgLoader.GetContentFromSvgFileAsync(fileInfo.FullName);
        imageModel.PixelWidth = (int)magickImage.Width;
        imageModel.PixelHeight = (int)magickImage.Height;
        imageModel.ImageType = ImageType.Svg;
        imageModel.Image = svgData;
        imageModel.DpiX = (ushort)magickImage.Density.X;
        imageModel.DpiY = (ushort)magickImage.Density.Y;;
    }

    private static async ValueTask ProcessBase64Async(FileInfo fileInfo, MagickFormat format, ImageModel imageModel)
    {
        var bitmap = await GetImage.GetBase64ImageAsync(fileInfo).ConfigureAwait(false);
        SetBitmapProperties(bitmap, imageModel, format);
    }
    
    private static async ValueTask ProcessRawImageAsync(FileInfo fileInfo, ImageModel imageModel, MagickImage magickImage)
    {
        var bitmap = await GetImage.GetRawBitmapAsync(fileInfo, magickImage).ConfigureAwait(false);
        SetBitmapProperties(bitmap, imageModel, magickImage.Format);
    }

    private static async ValueTask ProcessNonStandardImageAsync(FileInfo fileInfo, ImageModel imageModel, MagickImage magickImage)
    {
        var bitmap = await GetImage.GetNonStandardBitmapAsync(fileInfo, magickImage).ConfigureAwait(false);
        SetBitmapProperties(bitmap, imageModel, magickImage.Format);
    }
    


    #endregion
}