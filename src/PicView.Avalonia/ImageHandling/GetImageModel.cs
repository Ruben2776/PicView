using System.Text;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.AnimatedImage.Decoding;
using PicView.Avalonia.Svg;
using PicView.Core.DebugTools;
using PicView.Core.Exif;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.MotionPhoto;
using PicView.Core.Navigation.Tiff;
using PicView.Core.Svg;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.ImageHandling;

/// <summary>
/// Builds an <see cref="ImageModel"/> from a file. The work is split in three steps shared by the synchronous and
/// asynchronous entry points:
/// <list type="number">
/// <item><see cref="PlanDecode"/> pings the file once and maps its format to a <see cref="Decoder"/>.</item>
/// <item><see cref="Decode"/> / <see cref="DecodeAsync"/> run that decoder. Only the decoders with real async I/O
/// differ between the two.</item>
/// <item><see cref="CompleteModel"/> upgrades the model to an animated or motion photo type.</item>
/// </list>
/// </summary>
public static class GetImageModel
{
    /// <summary>The decoder a pinged image is routed to.</summary>
    private enum Decoder : byte
    {
        /// <summary>Skia, straight into the bitmap. The fastest path, for common formats that need no conversion.</summary>
        Skia,

        /// <summary>ImageMagick, with EXIF orientation and sRGB conversion.</summary>
        Magick,

        /// <summary>ImageMagick, reading the file by path so the full raw image is decoded instead of its thumbnail.</summary>
        Raw,

        /// <summary>ImageMagick for the first page, plus every page for navigation.</summary>
        Tiff,

        /// <summary>ImageMagick, reading the file by path.</summary>
        Tga,

        /// <summary>The SVG parser, with an ImageMagick fallback.</summary>
        Svg
    }

    /// <summary>The check that tells whether a decoded image is animated.</summary>
    private enum AnimationProbe : byte
    {
        None,
        WebP,
        Gif,
        Avif
    }

    /// <param name="Decoder">The decoder to run.</param>
    /// <param name="Animation">The animation check to run after decoding.</param>
    /// <param name="TryShellFirst">Try the platform's imaging component first (8-bit grayscale PNG).</param>
    private readonly record struct DecodePlan(
        Decoder Decoder,
        AnimationProbe Animation = AnimationProbe.None,
        bool TryShellFirst = false);

    #region Entry points

    /// <summary>
    /// Synchronously retrieves an <see cref="ImageModel"/> instance based on the provided file and optional <see cref="MagickImage"/>.
    /// Prefer <see cref="GetImageModelAsync"/> where the caller can await. This overload is for callers that cannot,
    /// such as rendering, and never blocks on a task. It is safe on the UI thread: the few UI values it needs
    /// (the core view model, the SVG theme color) are then read inline; from other threads it waits for the UI thread.
    /// </summary>
    /// <inheritdoc cref="GetImageModelAsync"/>
    /// <returns>The constructed <see cref="ImageModel"/>, or null when <paramref name="ct"/> was canceled.</returns>
    public static ImageModel GetImageModelSync(
        FileInfo fileInfo,
        MagickImage? magickImage = null,
        CoreViewModel? core = null,
        CancellationToken ct = default)
    {
        if (fileInfo is null)
        {
            DebugHelper.LogDebug(nameof(GetImageModel), nameof(GetImageModelSync), $"{nameof(fileInfo)} is null");
            return CreateErrorImageModel(null);
        }

        ct.ThrowIfCancellationRequested();

        var imageModel = new ImageModel { FileInfo = fileInfo };
        var shouldDisposeMagickImage = magickImage is null;

        try
        {
            switch (GetContainer(fileInfo))
            {
                case Container.Livp:
                    DecodeLivpCover(fileInfo, imageModel, MotionPhotoExtractor.ExtractLivpCoverToTempFile(fileInfo));
                    return imageModel;

                case Container.Base64:
                    return GetBase64ImageModel(fileInfo) ?? CreateErrorImageModel(fileInfo);
            }

            magickImage ??= GetImage.CreateAndPingMagickImage(fileInfo);

            ct.ThrowIfCancellationRequested();

            var plan = PlanDecode(magickImage);
            if (plan.TryShellFirst && TryProcessShellBitmap(fileInfo, imageModel, core ?? GetCoreViewModel()))
            {
                return imageModel;
            }

            Decode(plan.Decoder, fileInfo, imageModel, magickImage);
            CompleteModel(fileInfo, imageModel, magickImage, plan.Animation);

            return imageModel;
        }
        catch (OperationCanceledException)
        {
            return null!;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImageModel), nameof(GetImageModelSync), e);
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

    /// <summary>
    /// Asynchronously retrieves an <see cref="ImageModel"/> instance based on the provided file and optional <see cref="MagickImage"/>.
    /// </summary>
    /// <param name="fileInfo">The file information of the image to process.</param>
    /// <param name="magickImage">An optional <see cref="MagickImage"/> instance. If null, a new instance will be created internally.</param>
    /// <param name="core">Optional core view model used for shell-based decoders.</param>
    /// <param name="ct">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the constructed <see cref="ImageModel"/>.</returns>
    public static async ValueTask<ImageModel> GetImageModelAsync(
        FileInfo fileInfo,
        MagickImage? magickImage = null,
        CoreViewModel? core = null,
        CancellationToken ct = default)
    {
        if (fileInfo is null)
        {
            DebugHelper.LogDebug(nameof(GetImageModel), nameof(GetImageModelAsync), $"{nameof(fileInfo)} is null");
            return CreateErrorImageModel(null);
        }

        ct.ThrowIfCancellationRequested();

        var imageModel = new ImageModel { FileInfo = fileInfo };
        var shouldDisposeMagickImage = magickImage is null;

        try
        {
            switch (GetContainer(fileInfo))
            {
                case Container.Livp:
                    var coverPath = await MotionPhotoExtractor.ExtractLivpCoverToTempFileAsync(fileInfo).ConfigureAwait(false);
                    DecodeLivpCover(fileInfo, imageModel, coverPath);
                    return imageModel;

                case Container.Base64:
                    return await GetBase64ImageModelAsync(fileInfo, ct).ConfigureAwait(false) ?? CreateErrorImageModel(fileInfo);
            }

            magickImage ??= GetImage.CreateAndPingMagickImage(fileInfo);

            ct.ThrowIfCancellationRequested();

            var plan = PlanDecode(magickImage);
            if (plan.TryShellFirst &&
                TryProcessShellBitmap(fileInfo, imageModel, core ?? await GetCoreViewModelAsync()))
            {
                return imageModel;
            }

            await DecodeAsync(plan.Decoder, fileInfo, imageModel, magickImage).ConfigureAwait(false);
            CompleteModel(fileInfo, imageModel, magickImage, plan.Animation);

            return imageModel;
        }
        catch (OperationCanceledException)
        {
            return null!;
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

    /// <summary>Synchronously builds a model from a file holding base64 image data.</summary>
    public static ImageModel? GetBase64ImageModel(FileInfo fileInfo) =>
        fileInfo is null ? null : CreateBitmapModel(GetImage.GetBase64Image(fileInfo), fileInfo);

    public static async ValueTask<ImageModel?> GetBase64ImageModelAsync(FileInfo fileInfo, CancellationToken ct)
    {
        if (fileInfo is null)
        {
            return null;
        }

        try
        {
            var base64DataImage = await GetImage.GetBase64ImageAsync(fileInfo, ct).ConfigureAwait(false);
            return CreateBitmapModel(base64DataImage, fileInfo);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImageModel), nameof(GetBase64ImageModelAsync), e);
            return null;
        }
    }

    /// <summary>Synchronously builds a model from base64 image data.</summary>
    public static ImageModel? GetBase64ImageModel(string base64String) =>
        string.IsNullOrEmpty(base64String) ? null : CreateBitmapModel(GetImage.GetBase64Image(base64String), null);

    public static async ValueTask<ImageModel?> GetBase64ImageModelAsync(string base64String, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(base64String))
        {
            return null;
        }

        try
        {
            var base64DataImage = await GetImage.GetBase64ImageAsync(base64String, ct).ConfigureAwait(false);
            return CreateBitmapModel(base64DataImage, null);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImageModel), nameof(GetBase64ImageModelAsync), e);
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

    #endregion

    #region Planning

    /// <summary>Files that are containers around an image rather than an image ImageMagick can ping.</summary>
    private enum Container : byte
    {
        None,

        /// <summary>An Apple .livp zip holding a still image and a video.</summary>
        Livp,

        /// <summary>A text file holding base64 image data.</summary>
        Base64
    }

    private static Container GetContainer(FileInfo fileInfo)
    {
        // A span over the name avoids allocating the extension string for every image
        var extension = Path.GetExtension(fileInfo.Name.AsSpan());
        if (extension.Equals(".livp", StringComparison.OrdinalIgnoreCase))
        {
            return Container.Livp;
        }

        if (extension.Equals(".b64", StringComparison.OrdinalIgnoreCase))
        {
            return Container.Base64;
        }

        return Container.None;
    }

    /// <summary>
    /// Maps a pinged image to its decoder. Common formats go through Skia unless they need rotating or color
    /// managing, which only the ImageMagick path does.
    /// </summary>
    private static DecodePlan PlanDecode(MagickImage magickImage)
    {
        var shouldAutoOrient = ShouldAutoOrient(magickImage);
        var standard = shouldAutoOrient || HasNonSrgbColorProfile(magickImage) ? Decoder.Magick : Decoder.Skia;

        return magickImage.Format switch
        {
            MagickFormat.WebP or MagickFormat.WebM => new DecodePlan(standard, AnimationProbe.WebP),

            // Only orientation sends GIFs through ImageMagick
            MagickFormat.Gif or MagickFormat.Gif87 =>
                new DecodePlan(shouldAutoOrient ? Decoder.Magick : Decoder.Skia, AnimationProbe.Gif),

            MagickFormat.Png or MagickFormat.Png00 or MagickFormat.Png8 or MagickFormat.Png24 or MagickFormat.Png32
                or MagickFormat.Png48 or MagickFormat.Png64 =>
                new DecodePlan(standard, TryShellFirst: IsEightBitGray(magickImage)),

            MagickFormat.APng // TODO add animation one day
                or MagickFormat.Jpe or MagickFormat.Jpeg or MagickFormat.Pjpeg
                or MagickFormat.Bmp or MagickFormat.Ico or MagickFormat.Icon or MagickFormat.Wbmp =>
                new DecodePlan(standard),

            MagickFormat.Avif => new DecodePlan(Decoder.Magick, AnimationProbe.Avif),

            MagickFormat.Tif or MagickFormat.Tiff => new DecodePlan(Decoder.Tiff),

            MagickFormat.Tga => new DecodePlan(Decoder.Tga),

            MagickFormat.Svg or MagickFormat.Svgz => new DecodePlan(Decoder.Svg),

            MagickFormat.Arw or MagickFormat.Nef or MagickFormat.Dng or MagickFormat.Cr2 or MagickFormat.Rw2 =>
                new DecodePlan(Decoder.Raw),

            _ => new DecodePlan(Decoder.Magick)
        };
    }

    private static bool ShouldAutoOrient(MagickImage magickImage) =>
        ExifOrientationHelper.GetImageOrientation(magickImage) is not (ExifOrientation.None or ExifOrientation.Horizontal);

    private static bool IsEightBitGray(MagickImage magickImage) =>
        magickImage.ColorSpace is ColorSpace.Gray && magickImage.Depth is 8;

    private static bool HasNonSrgbColorProfile(MagickImage magickImage)
    {
        try
        {
            var colorProfile = magickImage.GetColorProfile();
            if (colorProfile is null)
            {
                return false;
            }

            return colorProfile.Description?.Contains("sRGB", StringComparison.OrdinalIgnoreCase) != true;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GetImageModel), nameof(HasNonSrgbColorProfile), e);
            return false;
        }
    }

    #endregion

    #region Decoding

    /// <summary>Runs <paramref name="decoder"/> and stores the result in <paramref name="imageModel"/>.</summary>
    private static void Decode(Decoder decoder, FileInfo fileInfo, ImageModel imageModel, MagickImage magickImage)
    {
        switch (decoder)
        {
            case Decoder.Skia:
                SetBitmapProperties(GetImage.GetSkBitmap(fileInfo), imageModel);
                break;

            case Decoder.Magick:
                SetBitmapProperties(GetImage.GetNonStandardBitmap(fileInfo, magickImage), imageModel);
                break;

            case Decoder.Raw:
                SetBitmapProperties(GetImage.GetRawBitmap(fileInfo, magickImage), imageModel);
                break;

            case Decoder.Tiff:
                SetBitmapProperties(GetImage.GetNonStandardBitmap(fileInfo, magickImage), imageModel);
                LoadTiffPages(fileInfo, imageModel);
                break;

            default:
            case Decoder.Tga:
                magickImage.Read(fileInfo);
                SetBitmapProperties(magickImage.ToWriteableBitmap(), imageModel);
                break;

            case Decoder.Svg:
                SetSvgProperties(SvgImageLoader.Load(fileInfo, magickImage.Width, magickImage.Height), imageModel, magickImage);
                break;
        }
    }

    /// <summary>
    /// <see cref="Decode"/> for async callers. Only the decoders that do async I/O are awaited; the others are
    /// CPU-bound and run synchronously without allocating a state machine.
    /// </summary>
    private static ValueTask DecodeAsync(Decoder decoder, FileInfo fileInfo, ImageModel imageModel, MagickImage magickImage)
    {
        switch (decoder)
        {
            case Decoder.Raw:
                return DecodeRawAsync(fileInfo, imageModel, magickImage);

            case Decoder.Svg:
                return DecodeSvgAsync(fileInfo, imageModel, magickImage);

            default:
                Decode(decoder, fileInfo, imageModel, magickImage);
                return ValueTask.CompletedTask;
        }
    }

    private static async ValueTask DecodeRawAsync(FileInfo fileInfo, ImageModel imageModel, MagickImage magickImage)
    {
        var bitmap = await GetImage.GetRawBitmapAsync(fileInfo, magickImage).ConfigureAwait(false);
        SetBitmapProperties(bitmap, imageModel);
    }

    private static async ValueTask DecodeSvgAsync(FileInfo fileInfo, ImageModel imageModel, MagickImage magickImage)
    {
        var image = await SvgImageLoader.LoadAsync(fileInfo, magickImage.Width, magickImage.Height).ConfigureAwait(false);
        SetSvgProperties(image, imageModel, magickImage);
    }

    private static void SetSvgProperties(object? image, ImageModel imageModel, MagickImage magickImage)
    {
        if (image is SvgDrawing drawing)
        {
            imageModel.PixelWidth = (uint)Math.Ceiling(drawing.Width);
            imageModel.PixelHeight = (uint)Math.Ceiling(drawing.Height);
        }
        else
        {
            imageModel.PixelWidth = magickImage.Width;
            imageModel.PixelHeight = magickImage.Height;
        }

        imageModel.Image = image;
        imageModel.ImageType = image is not null ? ImageType.Svg : ImageType.Invalid;
    }

    private static void LoadTiffPages(FileInfo fileInfo, ImageModel imageModel)
    {
        var pages = TiffManager.LoadTiffPages(fileInfo.FullName);
        if (pages.Count == 0)
        {
            return;
        }

        var bitmapPages = new object[pages.Count];
        for (var i = 0; i < pages.Count; i++)
        {
            bitmapPages[i] = pages[i].ToWriteableBitmap();
        }

        imageModel.TiffNavigation = new TiffNavigationInfo
        {
            CurrentPage = 0,
            PageCount = pages.Count,
            Pages = bitmapPages
        };
    }

    private static bool TryProcessShellBitmap(FileInfo fileInfo, ImageModel imageModel, CoreViewModel? core)
    {
        if (core is null)
        {
            return false;
        }

        var bitmap = GetImage.GetShellBitmap(fileInfo.FullName, core);
        if (bitmap is null)
        {
            return false;
        }

        SetBitmapProperties(bitmap, imageModel);
        return true;
    }

    private static CoreViewModel? GetCoreViewModel() =>
        Dispatcher.UIThread.Invoke(static () => Application.Current.DataContext as CoreViewModel);

    private static DispatcherOperation<CoreViewModel?> GetCoreViewModelAsync() =>
        Dispatcher.UIThread.InvokeAsync(static () => Application.Current.DataContext as CoreViewModel);

    /// <summary>
    /// Decodes the cover image of an Apple .livp container, extracted to <paramref name="coverPath"/>, through the
    /// regular pipeline, while the model keeps pointing at the original .livp file.
    /// </summary>
    private static void DecodeLivpCover(FileInfo livpFile, ImageModel imageModel, string? coverPath)
    {
        if (coverPath is null)
        {
            imageModel.ImageType = ImageType.Invalid;
            return;
        }

        var coverFile = new FileInfo(coverPath);
        using var coverMagickImage = GetImage.CreateAndPingMagickImage(coverFile);

        var requiresMagick = ShouldAutoOrient(coverMagickImage) | HasNonSrgbColorProfile(coverMagickImage);
        var isJpeg = coverMagickImage.Format is MagickFormat.Jpe or MagickFormat.Jpeg or MagickFormat.Pjpeg;
        var decoder = isJpeg && !requiresMagick ? Decoder.Skia : Decoder.Magick;

        Decode(decoder, coverFile, imageModel, coverMagickImage);

        imageModel.FileInfo = livpFile;
        if (imageModel.ImageType is ImageType.Bitmap)
        {
            imageModel.ImageType = ImageType.MotionPhoto;
            imageModel.MotionPhoto = new MotionPhotoInfo { Source = MotionPhotoSource.LivpContainer };
        }
    }

    #endregion

    #region Completion

    /// <summary>Upgrades a decoded model to its animated or motion photo type.</summary>
    private static void CompleteModel(FileInfo fileInfo, ImageModel imageModel, MagickImage magickImage, AnimationProbe probe)
    {
        switch (probe)
        {
            case AnimationProbe.WebP when ImageAnalyzer.IsAnimated(fileInfo):
                imageModel.ImageType = ImageType.AnimatedWebp;
                break;

            case AnimationProbe.Gif when IsAnimatedGif(fileInfo):
                imageModel.ImageType = ImageType.AnimatedGif;
                break;

            case AnimationProbe.Avif when ImageAnalyzer.IsAnimated(fileInfo):
                imageModel.ImageType = ImageType.AnimatedAvif;
                break;
        }

        TryDetectMotionPhoto(fileInfo, magickImage, imageModel);
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

        if (!MotionPhotoDetector.IsMotionPhotoExtension(fileInfo.Extension))
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

    #endregion

    private static ImageModel CreateBitmapModel(Bitmap? bitmap, FileInfo? fileInfo)
    {
        var model = new ImageModel { FileInfo = fileInfo };
        SetBitmapProperties(bitmap, model);
        return model;
    }

    private static ImageModel CreateErrorImageModel(FileInfo? fileInfo) =>
        new()
        {
            FileInfo = fileInfo,
            ImageType = ImageType.Invalid,
            Image = null, // TODO replace with error image
            PixelHeight = 0,
            PixelWidth = 0
        };
}
