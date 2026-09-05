using System.Text;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using ImageMagick;
using PicView.Avalonia.AnimatedImage.Decoding;
using PicView.Avalonia.Svg;
using PicView.Core.DebugTools;
using PicView.Core.Exif;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.MotionPhoto;
using PicView.Core.Navigation.Tiff;

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
    public static async ValueTask<ImageModel?> GetImageModelAsync(FileInfo fileInfo, MagickImage? magickImage, CancellationToken ct = default)
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
            // .livp is a zip container that cannot be pinged by Magick, so it must be
            // handled before the MagickImage is initialized.
            if (fileInfo.Extension.Equals(".livp", StringComparison.InvariantCultureIgnoreCase))
            {
                await ProcessLivpAsync(fileInfo, imageModel).ConfigureAwait(false);
                return imageModel;
            }

            // Initialize MagickImage if not provided
            magickImage ??= GetImage.CreateAndPingMagickImage(fileInfo);

            // Extract metadata
            // Check if rotation is needed
            var orientation = ExifOrientationHelper.GetImageOrientation(magickImage);
            var shouldAutoOrient = orientation is not (ExifOrientation.None or ExifOrientation.Horizontal);
            var shouldColorManage = HasNonSrgbColorProfile(magickImage);
            
            if (fileInfo.Extension.Equals(".b64", StringComparison.InvariantCultureIgnoreCase))
            {
                return await GetBase64ImageModelAsync(fileInfo, ct).ConfigureAwait(false);
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

                    if (IsAnimatedGif(fileInfo))
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
                case MagickFormat.Ico:
                case MagickFormat.Icon:
                case MagickFormat.Wbmp:
                    if (shouldAutoOrient || shouldColorManage)
                    {
                        await ProcessNonStandardImageAsync(fileInfo, imageModel, magickImage).ConfigureAwait(false);
                    }
                    else
                    {
                        await ProcessSkBitmapAsync(fileInfo, magickImage.Format, imageModel).ConfigureAwait(false);
                    }
                    break;
                
                case MagickFormat.Avif:
                    await ProcessNonStandardImageAsync(fileInfo, imageModel, magickImage).ConfigureAwait(false);

                    if (ImageAnalyzer.IsAnimated(fileInfo))
                    {
                        imageModel.ImageType = ImageType.AnimatedAvif;
                    }
                    break;

                case MagickFormat.Tif:
                case MagickFormat.Tiff:
                    await ProcessTiff(fileInfo, imageModel, magickImage);
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

            TryDetectMotionPhoto(fileInfo, magickImage, imageModel);

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
    
    public static async ValueTask<ImageModel?> GetBase64ImageModelAsync(FileInfo fileInfo, CancellationToken ct)
    {
        try
        {
            var base64DataImage = await GetImage.GetBase64ImageAsync(fileInfo, ct).ConfigureAwait(false);
            var model = new ImageModel();
            SetBitmapProperties(base64DataImage, model);
            return model;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImage), nameof(GetBase64ImageModelAsync), e);
            return null;
        }
    }
    
    public static async ValueTask<ImageModel?> GetBase64ImageModelAsync(string base64String, CancellationToken ct)
    {
        try
        {
            var base64DataImage = await GetImage.GetBase64ImageAsync(base64String, ct).ConfigureAwait(false);
            var model = new ImageModel();
            SetBitmapProperties(base64DataImage, model);
            return model;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImage), nameof(GetBase64ImageModelAsync), e);
            return null;
        }
    }

    public static void SetBitmapProperties(Bitmap? bitmap, ImageModel imageModel, ImageType imageType = ImageType.Bitmap)
    {
        imageModel.Image = bitmap;
        if (bitmap is null)
        {
            imageModel.PixelWidth = 0;
            imageModel.PixelHeight = 0;
            imageModel.ImageType = ImageType.Invalid;
            return;
        }
        imageModel.PixelWidth = (uint)bitmap.PixelSize.Width;
        imageModel.PixelHeight = (uint)bitmap.PixelSize.Height;
        imageModel.ImageType = imageType;
    }

    private static ImageModel CreateErrorImageModel(FileInfo? fileInfo)
    {
        return new ImageModel
        {
            FileInfo = fileInfo,
            ImageType = ImageType.Invalid,
            Image = null, // TODO replace with error image
            PixelHeight = 0,
            PixelWidth = 0
        };
    }

    private static bool HasNonSrgbColorProfile(MagickImage magickImage)
    {
        var colorProfile = magickImage.GetColorProfile();
        if (colorProfile is null)
        {
            return false;
        }

        return colorProfile.Description?.Contains("sRGB", StringComparison.OrdinalIgnoreCase) != true;
    }

    internal static bool IsAnimatedGif(FileInfo fileInfo)
    {
        if (ImageAnalyzer.IsAnimated(fileInfo))
        {
            return true;
        }

        try
        {
            using var stream = fileInfo.OpenRead();

            if (stream.Length == 0)
            {
                return false;
            }

            stream.Position = stream.Length - 1;
            if (stream.ReadByte() != (byte)BlockTypes.Extension)
            {
                return false;
            }

            stream.Position = 0;
            using var decoder = new GifDecoder(stream, CancellationToken.None);
            return decoder.Frames.Count > 1;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImageModel), nameof(IsAnimatedGif), e);
            return false;
        }
    }

    /// <summary>
    /// Checks whether a successfully decoded bitmap is actually a motion photo (XMP embedded
    /// video, Samsung trailer or sidecar file) and upgrades the model accordingly.
    /// Only the video location metadata is recorded here; the video bytes are extracted
    /// on demand when playback starts.
    /// </summary>
    private static void TryDetectMotionPhoto(FileInfo fileInfo, MagickImage magickImage, ImageModel imageModel)
    {
        if (imageModel.ImageType is not ImageType.Bitmap)
        {
            return;
        }

        var extension = fileInfo.Extension;
        if (!extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".heic", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".heif", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string? xmpPacket = null;
        try
        {
            var xmpProfile = magickImage.GetXmpProfile();
            if (xmpProfile is not null)
            {
                xmpPacket = Encoding.UTF8.GetString(xmpProfile.ToByteArray());
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImageModel), nameof(TryDetectMotionPhoto), e);
        }

        var info = MotionPhotoDetector.TryDetect(fileInfo, xmpPacket);
        if (info is null)
        {
            return;
        }

        imageModel.ImageType = ImageType.MotionPhoto;
        imageModel.MotionPhoto = info;
    }

    #region Image Processing Methods

    private static async ValueTask ProcessSkBitmapAsync(FileInfo fileInfo, MagickFormat format, ImageModel imageModel)
    {
        var bitmap = await GetImage.GetSkBitmapAsync(fileInfo).ConfigureAwait(false);
        SetBitmapProperties(bitmap, imageModel);
    }

    private static async Task ProcessSvg(FileInfo fileInfo, ImageModel imageModel, MagickImage magickImage)
    {
        var svgData = await SvgLoader.GetContentFromSvgFileAsync(fileInfo.FullName);
        imageModel.PixelWidth = magickImage.Width;
        imageModel.PixelHeight = magickImage.Height;
        imageModel.ImageType = ImageType.Svg;
        imageModel.Image = SvgSource.LoadFromSvg(svgData);
    }
/// <summary>
    /// Handles Apple .livp containers (a zip holding a still image plus a video).
    /// The cover image is extracted to a temporary file and decoded through the regular
    /// pipeline, while the model keeps pointing at the original .livp file.
    /// </summary>
    private static async ValueTask ProcessLivpAsync(FileInfo fileInfo, ImageModel imageModel)
    {
        var tempImagePath = await MotionPhotoExtractor.ExtractLivpCoverToTempFileAsync(fileInfo).ConfigureAwait(false);
        if (tempImagePath is null)
        {
            imageModel.ImageType = ImageType.Invalid;
            return;
        }

        var tempFileInfo = new FileInfo(tempImagePath);
        using var tempMagickImage = GetImage.CreateAndPingMagickImage(tempFileInfo);
        if (tempMagickImage.Format is MagickFormat.Jpe or MagickFormat.Jpeg or MagickFormat.Pjpeg)
        {
            await ProcessSkBitmapAsync(tempFileInfo, tempMagickImage.Format, imageModel).ConfigureAwait(false);
        }
        else
        {
            await ProcessNonStandardImageAsync(tempFileInfo, imageModel, tempMagickImage).ConfigureAwait(false);
        }

        imageModel.FileInfo = fileInfo;
        if (imageModel.ImageType is ImageType.Bitmap)
        {
            imageModel.ImageType = ImageType.MotionPhoto;
            imageModel.MotionPhoto = new MotionPhotoInfo { Source = MotionPhotoSource.LivpContainer };
        }
    }
    
    private static async ValueTask ProcessRawImageAsync(FileInfo fileInfo, ImageModel imageModel, MagickImage magickImage)
    {
        var bitmap = await GetImage.GetRawBitmapAsync(fileInfo, magickImage).ConfigureAwait(false);
        SetBitmapProperties(bitmap, imageModel);
    }

    private static async ValueTask ProcessNonStandardImageAsync(FileInfo fileInfo, ImageModel imageModel, MagickImage magickImage)
    {
        var bitmap = await GetImage.GetNonStandardBitmapAsync(fileInfo, magickImage).ConfigureAwait(false);
        SetBitmapProperties(bitmap, imageModel);
    }
    
    private static async ValueTask ProcessTiff(FileInfo fileInfo, ImageModel imageModel, MagickImage magickImage)
    {
        var bitmap = await GetImage.GetNonStandardBitmapAsync(fileInfo, magickImage).ConfigureAwait(false);
        SetBitmapProperties(bitmap, imageModel);
        var pages = TiffManager.LoadTiffPages(fileInfo.FullName);
        if (pages.Count > 0)
        {
            imageModel.TiffNavigation = new TiffNavigationInfo
            {
                CurrentPage = 0,
                PageCount = pages.Count
            };
            var bitmapPages = new object[pages.Count];
            for (var i = 0; i < pages.Count; i++)
            {
                bitmapPages[i] = pages[i].ToWriteableBitmap();
            }
            imageModel.TiffNavigation.Pages = bitmapPages;
        }
    }
    

    #endregion
}
