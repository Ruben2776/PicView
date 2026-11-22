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
    /// <summary>
    /// Shows how far <paramref name="tab"/> is through its files on the taskbar. A Single Image is one of no
    /// files, so showing one in the active tab takes the progress of the files before it away.
    /// </summary>
    internal static void SetTaskbarProgress(MainWindowViewModel vm, TabViewModel tab)
    {
        if (!Settings.UIProperties.IsTaskbarProgressEnabled || vm.Core.PlatformService is not { } platform)
        {
            return;
        }

        // By the Single Image and not by a missing iterator: the tab drops its iterator only after it shows the image
        if (tab.SingleImageType is not SingleImageType.None)
        {
            if (ReferenceEquals(tab, vm.WindowTabs.ActiveTab.CurrentValue))
            {
                platform.StopTaskbarProgress();
            }

            return;
        }

        if (tab.ImageIterator is { CurrentIndex: > -1 } iterator && iterator.Files.Count > 0)
        {
            platform.SetTaskbarProgress((ulong)iterator.CurrentIndex, (ulong)iterator.Files.Count);
        }
    }

    /// <param name="tab">The tab to show it in; the active one when null.</param>
    public static void SetSingleImage(MainWindowViewModel vm, Bitmap image, SingleImageType type, string name,
        TabViewModel? tab = null)
    {
        // Its own model, so the previous file's cached model keeps its own size
        SetSingleImage(vm, new ImageModel
        {
            Image = image,
            PixelWidth = (uint)image.PixelSize.Width,
            PixelHeight = (uint)image.PixelSize.Height,
            ImageType = ImageType.Bitmap
        }, type, name, tab);
    }

    /// <summary>
    /// Shows <paramref name="model"/> as the active tab's Single Image. Showing a kept Single Image again passes back the
    /// model it kept, never a copy, since View History finds the entry by that instance.
    /// </summary>
    /// <param name="tab">
    /// The tab to show it in when that is not simply the active one: a load that takes a while passes the tab it
    /// started in, so switching tabs meanwhile does not send the image to another tab.
    /// </param>
    public static void SetSingleImage(MainWindowViewModel vm, ImageModel model, SingleImageType type, string title,
        TabViewModel? tab = null)
    {
        var tabViewModel = tab ?? vm.WindowTabs.ActiveTab.CurrentValue;

        // A slow load of a file that was still running would replace this image once it ends
        tabViewModel.SupersedeLoads();

        // The window's fitter follows the new image, and its viewer follows that Fit (ADR-0026)
        tabViewModel.DisplaySingleImage(model, type, title);

        // The title follows from the tab's Titles, which derive it once the Single Image shows (ADR-0015)
        tabViewModel.DisposeImageIterator(); // SourceOpener creates a new one when browsing files again
    }

    /// <param name="tab">The tab to show it in; the active one when null.</param>
    /// <returns>Whether it shows: false when the text decodes to no image, or the decoding was cancelled.</returns>
    public static async ValueTask<bool> SetSingeBase64ImageAsync(string base64, MainWindowViewModel vm,
        CancellationToken ct, TabViewModel? tab = null)
    {
        var base64Model =
            await GetImageModel.GetBase64ImageModelAsync(base64, ct)
                .ConfigureAwait(false);

        if (base64Model?.Image is not Bitmap bitmap)
        {
            return false;
        }

        SetSingleImage(vm, bitmap, SingleImageType.Base64, TranslationManager.Translation.Base64Image, tab);
        return true;
    }
}
