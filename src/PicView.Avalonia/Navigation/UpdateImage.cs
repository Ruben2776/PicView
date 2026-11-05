using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Views.UC;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// What a Main Window does around what its tab shows: the reaction to a tab's <see cref="TabViewModel.Shown"/> change,
/// and showing a Single Image in the active tab. It sets none of the tab's shown-state bindables: the tab is their one
/// writer.
/// </summary>
public static class UpdateImage
{
    /// <summary>
    /// What the window does once <paramref name="tab"/> shows something new. For a new image: zoom and orientation
    /// reset when the setting asks, a Fit and the motion photo overlays. For a shown file that changed on disk: the
    /// taskbar progress. Nothing while the tab's view is not the image viewer. Call on the UI thread.
    /// </summary>
    public static void ReactToShown(MainWindow mainWindow, TabViewModel tab, MainWindowViewModel vm, ShownChange change)
    {
        if (tab.CurrentView.Value is not ImageViewer imageViewer)
        {
            return;
        }

        if (change is ShownChange.Image)
        {
            var resets = Settings.Zoom.ResetZoomOnChange;
            if (resets)
            {
                // Upright before the Fit, which reads the Quarter Turn
                tab.Orientation.Reset();
            }

            // The Fit writes the tab's Fit Scale, which marks the tab's Titles when the percent changes (ADR-0016)
            mainWindow.WindowFitter.Refit();
            if (resets)
            {
                // The tab reset its Zoom Factor when it published; the control's transform follows here, after the
                // Fit, so the scale it reports meets the new Fit Scale (ADR-0016)
                imageViewer.ResetZoomSlim();
            }

            imageViewer.UpdateMotionPhoto(tab);
        }

        SetTaskbarProgress(vm, tab);
    }

    internal static void SetTaskbarProgress(MainWindowViewModel vm, TabViewModel tab)
    {
        if (Settings.UIProperties.IsTaskbarProgressEnabled && vm.Core.PlatformService is { } platform &&
            tab.ImageIterator is { CurrentIndex: > -1 } iterator && iterator.Files.Count > 0)
        {
            platform.SetTaskbarProgress((ulong)iterator.CurrentIndex, (ulong)iterator.Files.Count);
        }
    }

    public static void SetSingleImage(MainWindowViewModel vm, MainWindow mainWindow, Bitmap image, SingleImageType type, string name)
    {
        // Its own model, so the previous file's cached model keeps its own size
        SetSingleImage(vm, mainWindow, new ImageModel
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
    public static void SetSingleImage(MainWindowViewModel vm, MainWindow mainWindow, ImageModel model, SingleImageType type, string title)
    {
        var tabViewModel = vm.WindowTabs.ActiveTab.CurrentValue;

        // Upright before fitting, since the new image is shown unturned and unflipped
        tabViewModel.Orientation.Reset();
        tabViewModel.DisplaySingleImage(model, type, title);

        // Fitted once its view is ready, with AutoFit or without, as a file is
        Dispatcher.UIThread.Invoke(() =>
        {
            if (tabViewModel.CurrentView?.Value is not ImageViewer imageViewer)
            {
                // The tab shows something other than its viewer, such as a crop; fit now, the viewer binds the image when it returns
                mainWindow.WindowFitter.Refit();
                if (Settings.WindowProperties.KeepCentered)
                {
                    mainWindow.WindowFitter.Center();
                }
                return;
            }

            // The Fit first, so the scale the control's reset reports meets the new Fit Scale (ADR-0016)
            mainWindow.WindowFitter.Refit();
            imageViewer.ResetZoomSlim();
            imageViewer.UpdateMotionPhoto(tabViewModel);

            if (Settings.WindowProperties.KeepCentered)
            {
                mainWindow.WindowFitter.Center();
            }
        });

        // The title follows from the tab's Titles, which derive it once the Single Image shows (ADR-0015)
        tabViewModel.DisposeImageIterator(); // SourceOpener creates a new one when browsing files again
    }

    public static async ValueTask SetSingeBase64ImageAsync(string base64, MainWindowViewModel vm, MainWindow mainWindow, CancellationToken ct)
    {
        var base64Model =
            await GetImageModel.GetBase64ImageModelAsync(base64, ct)
                .ConfigureAwait(false);

        if (base64Model is null)
        {
            return;
        }

        SetSingleImage(vm, mainWindow, base64Model.Image as Bitmap, SingleImageType.Base64, TranslationManager.Translation.Base64Image);
    }
}
