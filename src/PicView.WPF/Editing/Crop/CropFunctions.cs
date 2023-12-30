using Microsoft.Win32;
using PicView.Core.Config;
using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.FileHandling;
using PicView.WPF.ImageHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.Loading;
using PicView.WPF.UILogic.TransformImage;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PicView.Core.Localization;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.UILogic.Sizing.ScaleImage;
using static PicView.WPF.UILogic.TransformImage.Rotation;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.Editing.Crop;

internal static class CropFunctions
{
    private static CropService? CropService { get; set; }

    internal static void StartCrop()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Source == null || ZoomLogic.IsZoomed) return;

        switch (RotationAngle)
        {
            case 0:
            case 180:
            case 90:
            case 270:
                break;

            default: return;
        }

        if (GetCroppingTool == null)
        {
            LoadControls.LoadCroppingTool();
        }

        GetCroppingTool.Width = RotationAngle is 0 or 180 ? XWidth : XHeight;
        GetCroppingTool.Height = RotationAngle is 0 or 180 ? XHeight : XWidth;
        GetCroppingTool.Margin = new Thickness(0, 0, 0,
            SettingsHelper.Settings.Gallery.IsBottomGalleryShown
                ? GalleryNavigation.PicGalleryItemSize + GalleryNavigation.ScrollbarSize
                : 0);

        ConfigureWindows.GetMainWindow.TitleText.Text = TranslationHelper.GetTranslation("CropMessage");

        if (!ConfigureWindows.GetMainWindow.ParentContainer.Children.Contains(GetCroppingTool))
        {
            ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetCroppingTool);
        }
    }

    internal static async Task PerformCropAsync()
    {
        var sameFile = await SaveCrop().ConfigureAwait(true);
        if (sameFile)
        {
            await LoadPic.LoadPiFromFileAsync(Pics[FolderIndex]).ConfigureAwait(false);
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(CloseCrop);
    }

    internal static void CloseCrop()
    {
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(SetTitle.SetTitleString);

        ConfigureWindows.GetMainWindow.ParentContainer.Children.Remove(GetCroppingTool);
    }

    internal static void InitializeCrop()
    {
        GetCroppingTool.Width = RotationAngle is 0 or 180 ? XWidth : XHeight;
        GetCroppingTool.Height = RotationAngle is 0 or 180 ? XHeight : XWidth;

        CropService = new CropService(GetCroppingTool);

        var chosenColorBrush = Application.Current.Resources["ChosenColorBrush"] as SolidColorBrush;
        GetCroppingTool.RootGrid.Background =
            new SolidColorBrush(Color.FromArgb(
                25,
                chosenColorBrush.Color.R,
                chosenColorBrush.Color.G,
                chosenColorBrush.Color.B
            ));

        GetCroppingTool.RootGrid.PreviewMouseDown += (_, e) => CropService.Adorner.RaiseEvent(e);
        GetCroppingTool.RootGrid.PreviewMouseLeftButtonUp += (_, e) => CropService.Adorner.RaiseEvent(e);
    }

    /// <summary>
    /// Save crop from file dialog
    /// </summary>
    /// <returns>Whether it's the same file that is being viewed or not</returns>
    private static async Task<bool> SaveCrop()
    {
        string filename;
        string? directory;
        if (ErrorHandling.CheckOutOfRange() == false)
        {
            filename = Path.GetRandomFileName();
            directory = null;
        }
        else
        {
            filename = Path.GetFileName(Pics[FolderIndex]);
            directory = Path.GetDirectoryName(Pics[FolderIndex]);
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = OpenSave.FilterFiles,
            Title = $"{TranslationHelper.GetTranslation("SaveImage")} - PicView",
            FileName = filename,
        };

        if (!string.IsNullOrEmpty(directory))
        {
            saveDialog.InitialDirectory = directory;
        }

        if (!saveDialog.ShowDialog().HasValue)
        {
            return false;
        }

        OpenSave.IsDialogOpen = true;

        var crop = GetCrop();
        var source = ConfigureWindows.GetMainWindow.MainImage.Source as BitmapSource;
        var effectApplied = ConfigureWindows.GetMainWindow.MainImage.Effect != null;

        var success = await SaveImages
            .SaveImageAsync(RotationAngle, IsFlipped, source, null, saveDialog.FileName, crop, effectApplied)
            .ConfigureAwait(false);
        if (success)
        {
            return saveDialog.FileName == Pics[FolderIndex];
        }

        return false;
    }

    /// <summary>
    /// Copies selected crop area to clipholder, with or without effect applied
    /// </summary>
    internal static void CopyCrop()
    {
        var crop = GetCrop();
        var source = ConfigureWindows.GetMainWindow.MainImage.Source as BitmapSource;
        var effectApplied = ConfigureWindows.GetMainWindow.MainImage.Effect != null;

        if (effectApplied)
        {
            var frame = Image2BitmapSource.GetRenderedBitmapFrame();
            var croppedFrame = new CroppedBitmap(frame, crop);
            Clipboard.SetImage(croppedFrame);
        }
        else
        {
            var croppedSource = new CroppedBitmap(source, crop);
            Clipboard.SetImage(croppedSource);
        }

        Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("CopiedImage"));
    }

    private static Int32Rect GetCrop()
    {
        var cropArea = CropService.GetCroppedArea();
        var transformX = ZoomLogic.TranslateTransform.X;
        var transformY = ZoomLogic.TranslateTransform.Y;
        var scaleX = ZoomLogic.ScaleTransform.ScaleX;
        var scaleY = ZoomLogic.ScaleTransform.ScaleY;
        transformX = transformX is 0 ? 1 : transformX;
        transformY = transformY is 0 ? 1 : transformY;

        // Calculate scaled coordinates of the crop area
        var scaledX = cropArea.CroppedRectAbsolute.X / transformX;
        var scaledY = cropArea.CroppedRectAbsolute.Y / transformY;
        var scaledWidth = cropArea.CroppedRectAbsolute.Width / scaleX;
        var scaledHeight = cropArea.CroppedRectAbsolute.Height / scaleY;

        // Apply aspect ratio
        var x = Convert.ToInt32(scaledX / AspectRatio);
        var y = Convert.ToInt32(scaledY / AspectRatio);
        var width = Convert.ToInt32(scaledWidth / AspectRatio);
        var height = Convert.ToInt32(scaledHeight / AspectRatio);

        // Adjust dimensions based on rotation angle
        switch (RotationAngle)
        {
            case 0:
            case 180:
                break;

            case 90:
            case 270:
                // Swap width and height
                (width, height) = (height, width);
                break;

            default:
                var rotationRadians = RotationAngle * Math.PI / 180;

                break;
        }

        return new Int32Rect(x, y, width, height);
    }
}