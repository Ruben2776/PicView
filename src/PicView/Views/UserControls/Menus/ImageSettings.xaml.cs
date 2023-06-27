using PicView.ConfigureSettings;
using PicView.Editing.Crop;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.TransformImage;
using System.Windows;
using System.Windows.Media;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Menus;

/// <summary>
/// Interaction logic for ImageSettings.xaml
/// </summary>
public partial class ImageSettings
{
    public ImageSettings()
    {
        InitializeComponent();

        // RotateLeftButton
        RotateLeftButton.Click += async (_, _) =>
            await Rotation.RotateAndMoveCursor(true, RotateLeftButton).ConfigureAwait(false);
        SetButtonIconMouseOverAnimations(RotateLeftButton, RotateLeftButtonBrush, RotateLeftIconBrush);

        // RotateRightButton
        RotateRightButton.Click += async (_, _) =>
            await Rotation.RotateAndMoveCursor(false, RotateRightButton).ConfigureAwait(false);
        SetButtonIconMouseOverAnimations(RotateRightButton, RotateRightButtonBrush, RotateRightIconBrush);

        // FlipButton
        FlipButton.Click += (_, _) => Rotation.Flip();
        SetButtonIconMouseOverAnimations(FlipButton, FlipButtonBrush, FlipIconBrush);

        // ResizeButton
        SetButtonIconMouseOverAnimations(ResizeButtonBorder, ResizeBorderBrush,
            (SolidColorBrush)Resources["ResizeIcon"]);
        ResizeButton.Click += (_, _) => UpdateUIValues.ToggleQuickResize();
        ResizeButtonBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.ToggleQuickResize();

        // CropButton
        SetButtonIconMouseOverAnimations(CropButtonBorder, CropBorderBrush, (SolidColorBrush)Resources["CropIcon"]);
        CropButton.Click += (_, _) => CropFunctions.StartCrop();
        CropButtonBorder.MouseLeftButtonDown += (_, _) => CropFunctions.StartCrop();

        // OptimizeButton
        SetButtonIconMouseOverAnimations(OptimizeBorder, BgBorderBrush, (SolidColorBrush)Resources["OptimizeIcon"]);
        OptimizeButton.Click += async (_, _) =>
            await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);
        OptimizeBorder.MouseLeftButtonDown += async (_, _) =>
            await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);

        // SlideShowBorder
        SetButtonIconMouseOverAnimations(SlideShowBorder, SlideShowBorderBrush,
            (SolidColorBrush)Resources["SlideshowIcon"]);
        SlideShowButton.Click += delegate
        {
            UC.Close_UserControls();
            Slideshow.StartSlideshow();
        };
        SlideShowBorder.MouseLeftButtonDown += delegate
        {
            UC.Close_UserControls();
            Slideshow.StartSlideshow();
        };
        var s = Application.Current.Resources["StartSlideshow"] as string;
        s += " [F5]";
        SlideShowBorder.ToolTip = s;

        // BottomGalleryBorder
        SetButtonIconMouseOverAnimations(BottomGalleryButton, BottomGalleryBrush,
            (SolidColorBrush)Resources["BottomGalleryIcon"]);
        BottomGalleryBorder.MouseLeftButtonDown += async (_, _) => await ContainedGalleryClick().ConfigureAwait(false);
        BottomGalleryButton.Click += async (_, _) => await ContainedGalleryClick().ConfigureAwait(false);
    }

    private static async Task ContainedGalleryClick()
    {
        UC.Close_UserControls();
        Settings.Default.IsBottomGalleryShown = !Settings.Default.IsBottomGalleryShown;
        if (Settings.Default.IsBottomGalleryShown)
        {
            await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
        }
        else
        {
            GalleryToggle.CloseBottomGallery();
        }
    }
}