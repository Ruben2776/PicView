using Avalonia.Media.Imaging;
using PicView.Avalonia.ImageHandling;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// What a Main Window does around what its tab shows: the taskbar progress, and showing a Single Image in the active
/// tab. It sets none of the tab's shown-state bindables: the tab is their one writer.
/// </summary>
public static class UpdateImage
{
    internal static void SetTaskbarProgress(MainWindowViewModel vm, TabViewModel tab)
    {
        if (Settings.UIProperties.IsTaskbarProgressEnabled && vm.Core.PlatformService is { } platform &&
            tab.ImageIterator is { CurrentIndex: > -1 } iterator && iterator.Files.Count > 0)
        {
            platform.SetTaskbarProgress((ulong)iterator.CurrentIndex, (ulong)iterator.Files.Count);
        }
    }

    public static void SetSingleImage(MainWindowViewModel vm, Bitmap image, SingleImageType type, string name)
    {
        // Its own model, so the previous file's cached model keeps its own size
        SetSingleImage(vm, new ImageModel
        {
            Image = image,
            PixelWidth = (uint)image.PixelSize.Width,
            PixelHeight = (uint)image.PixelSize.Height,
            ImageType = ImageType.Bitmap
        }, type, name);
    }

    /// <summary>
    /// Shows <paramref name="model"/> as the active tab's Single Image. Showing a kept Single Image again passes back the
    /// model it kept, never a copy, since View History finds the entry by that instance.
    /// </summary>
    public static void SetSingleImage(MainWindowViewModel vm, ImageModel model, SingleImageType type, string title)
    {
        var tabViewModel = vm.WindowTabs.ActiveTab.CurrentValue;

        // The window's fitter follows the new image, and its viewer follows that Fit (ADR-0026)
        tabViewModel.DisplaySingleImage(model, type, title);

        // The title follows from the tab's Titles, which derive it once the Single Image shows (ADR-0015)
        tabViewModel.DisposeImageIterator(); // SourceOpener creates a new one when browsing files again
    }

    public static async ValueTask SetSingeBase64ImageAsync(string base64, MainWindowViewModel vm, CancellationToken ct)
    {
        var base64Model =
            await GetImageModel.GetBase64ImageModelAsync(base64, ct)
                .ConfigureAwait(false);

        if (base64Model is null)
        {
            return;
        }

        SetSingleImage(vm, base64Model.Image as Bitmap, SingleImageType.Base64, TranslationManager.Translation.Base64Image);
    }
}
