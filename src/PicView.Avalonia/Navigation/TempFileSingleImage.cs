using Avalonia.Media.Imaging;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Svg;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Svg;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// Shows a file from the temp folder as a Single Image. Such a file may be deleted right after it arrives, as a
/// browser does with a dragged image, and its folder holds unrelated files, so it is never browsed or watched: it
/// is loaded whole into memory, and the model it is shown with names no file.
/// </summary>
internal static class TempFileSingleImage
{
    /// <summary>
    /// Loads <paramref name="fileInfo"/> and shows it under <paramref name="title"/>. Returns false, showing
    /// nothing, when the file could not be loaded.
    /// </summary>
    /// <param name="tab">The tab to show it in; the tab that is active once the file has loaded when null.</param>
    /// <param name="svgAsDrawing">See <see cref="LoadAsync"/>.</param>
    public static async ValueTask<bool> ShowAsync(MainWindowViewModel vm, FileInfo fileInfo, string title,
        TabViewModel? tab = null, bool svgAsDrawing = true)
    {
        var model = await LoadAsync(fileInfo, svgAsDrawing).ConfigureAwait(false);
        if (model is null)
        {
            return false;
        }

        UpdateImage.SetSingleImage(vm, model, SingleImageType.TempFile, title, tab);
        return true;
    }

    /// <summary>
    /// The file as a model that no longer depends on it: an SVG as its drawing, or as the fallback bitmap when the
    /// parser does not support it, and any other file as a bitmap. Null when the file could not be loaded.
    /// </summary>
    /// <param name="svgAsDrawing">
    /// False loads an SVG as a bitmap like any other file. A drawing stays sharp when zoomed, but with no file
    /// behind it, it cannot yet be saved, copied as an image or printed, which a bitmap can.
    /// </param>
    internal static async ValueTask<ImageModel?> LoadAsync(FileInfo fileInfo, bool svgAsDrawing = true)
    {
        try
        {
            if (svgAsDrawing && SvgFileReader.HasSvgExtension(fileInfo.Name))
            {
                return await LoadSvgModelAsync(fileInfo).ConfigureAwait(false);
            }

            var bitmap = await GetImage.GetNonStandardBitmapAsync(fileInfo).ConfigureAwait(false);
            if (bitmap is null)
            {
                return null;
            }

            return new ImageModel
            {
                Image = bitmap,
                PixelWidth = (uint)bitmap.PixelSize.Width,
                PixelHeight = (uint)bitmap.PixelSize.Height,
                ImageType = ImageType.Bitmap
            };
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(TempFileSingleImage), nameof(LoadAsync), e);
            return null;
        }
    }

    /// <summary>
    /// An SVG file's <see cref="SvgDrawing"/>, or its fallback bitmap, or null when it cannot be read as an image.
    /// </summary>
    internal static async ValueTask<object?> LoadSvgAsync(FileInfo fileInfo)
    {
        var (image, _, _) = await LoadSvgWithSizeAsync(fileInfo).ConfigureAwait(false);
        return image;
    }

    private static async ValueTask<ImageModel?> LoadSvgModelAsync(FileInfo fileInfo)
    {
        var (image, width, height) = await LoadSvgWithSizeAsync(fileInfo).ConfigureAwait(false);
        switch (image)
        {
            case SvgDrawing drawing:
                width = (uint)Math.Ceiling(drawing.Width);
                height = (uint)Math.Ceiling(drawing.Height);
                break;
            case Bitmap:
                // The fallback is rendered larger than the SVG, which keeps the size the ping gave
                break;
            default:
                return null;
        }

        return new ImageModel
        {
            Image = image,
            PixelWidth = width,
            PixelHeight = height,
            ImageType = ImageType.Svg,
            // Not as FileInfo, which would make the tab browse the temp folder: only so the source can be read again
            SvgSourcePath = fileInfo.FullName
        };
    }

    /// <summary>The image with the SVG's own size from the ping, which the fallback needs.</summary>
    private static async ValueTask<(object? Image, uint Width, uint Height)> LoadSvgWithSizeAsync(FileInfo fileInfo)
    {
        uint width, height;
        try
        {
            using var magickImage = GetImage.CreateAndPingMagickImage(fileInfo);
            width = magickImage.Width;
            height = magickImage.Height;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(TempFileSingleImage), nameof(LoadSvgWithSizeAsync), e);
            return (null, 0, 0);
        }

        var image = await SvgImageLoader.LoadAsync(fileInfo, width, height).ConfigureAwait(false);
        return (image, width, height);
    }
}
