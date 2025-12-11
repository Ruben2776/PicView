using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.Functions;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;

namespace PicView.Avalonia.Crop;

public static class CropFunctions
{
    public static bool IsCropping { get; private set; }

    /// <summary>
    /// Starts the cropping functionality by setting up the ImageCropperViewModel 
    /// and adding the CropControl to the main view.
    /// </summary>
    /// <param name="vm">The main view model instance containing image properties and state.</param>
    /// <remarks>
    /// This method checks if cropping can be enabled and if the image source is valid.
    /// If conditions are met, it configures the crop control with the appropriate dimensions
    /// and updates the view model's title and tooltip to reflect the cropping state.
    /// </remarks>
    public static async Task StartCropControlAsync(MainViewModel vm)
    {
        if (!DetermineIfShouldBeEnabled(vm))
        {
            return;
        }

        if (vm?.PicViewer.ImageSource.CurrentValue is not Bitmap bitmap)
        {
            return;
        }

        var isBottomGalleryShown = Settings.Gallery.IsBottomGalleryShown;
        // Hide bottom gallery when entering crop mode
        if (isBottomGalleryShown)
        {
            vm.Gallery.GalleryMode.Value = GalleryMode.Closed;
            // Reset setting before resizing
            Settings.Gallery.IsBottomGalleryShown = false;
            await WindowResizing.SetSizeAsync(vm);
        }

        var size = new Size(vm.PicViewer.ImageWidth.CurrentValue, vm.PicViewer.ImageHeight.CurrentValue);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            vm.Crop = new ImageCropperViewModel(bitmap);
            vm.Crop.ImageWidth.Value = size.Width;
            vm.Crop.ImageHeight.Value = size.Height;
            vm.Crop.AspectRatio.Value = vm.PicViewer.AspectRatio.CurrentValue;
            
            var cropControl = new CropControl
            {
                DataContext = vm,
                Width = size.Width,
                Height = size.Height,
                Margin = new Thickness(0)
            };
            vm.MainWindow.CurrentView.Value = cropControl;
        });

        IsCropping = true;
        vm.PicViewer.Title.Value = TranslationManager.Translation.CropMessage!;
        vm.PicViewer.TitleTooltip.Value = TranslationManager.Translation.CropMessage!;

        await FunctionsMapper.CloseMenus();

        if (isBottomGalleryShown)
        {
            Settings.Gallery.IsBottomGalleryShown = true;
        }
    }

    public static void CloseCropControl(MainViewModel vm)
    {
        if (Settings.Gallery.IsBottomGalleryShown)
        {
            if (vm.Gallery is {} gallery)
            {
                gallery.GalleryMode.Value = GalleryMode.ClosedToBottom;
            }
            
            WindowResizing.SetSize(vm);
        }

        vm.MainWindow.CurrentView.Value = vm.ImageViewer;
        IsCropping = false;
        TitleManager.SetTitle(vm);

        // Reset image type to fix issue with animated images
        switch (vm.PicViewer.ImageType.CurrentValue)
        {
            case ImageType.AnimatedWebp:
                vm.PicViewer.ImageType.Value = ImageType.Bitmap;
                vm.PicViewer.ImageType.Value = ImageType.AnimatedWebp;
                break;
            case ImageType.AnimatedGif:
                vm.PicViewer.ImageType.Value = ImageType.Bitmap;
                vm.PicViewer.ImageType.Value = ImageType.AnimatedGif;
                break;
        }

        vm.Crop = null;
    }

    public static bool DetermineIfShouldBeEnabled(MainViewModel vm)
    {
        if (IsCropping)
        {
            return false;
        }

        if (vm?.PicViewer.ImageSource.CurrentValue is not Bitmap || Settings.ImageScaling.ShowImageSideBySide)
        {
            vm.PicViewer.ShouldCropBeEnabled.Value = false;
            return false;
        }

        if (DialogManager.IsDialogOpen)
        {
            return false;
        }

        if (vm.MainWindow.IsEditableTitlebarOpen.CurrentValue)
        {
            return false;
        }

        if (vm.PicViewer.RotationAngle.CurrentValue is 0 && vm.PicViewer.ScaleX.CurrentValue is 1)
        {
            vm.PicViewer.ShouldCropBeEnabled.Value = true;
            return true;
        }

        vm.PicViewer.ShouldCropBeEnabled.Value = false;
        return false;
    }
}