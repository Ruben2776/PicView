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

        // FullscreenGalleryBorder
        SetButtonIconMouseOverAnimations(FullScreenGalleryButton, FullScreenBrush,
            (SolidColorBrush)Resources["FullScreenIcon"]);
        FullscreenGalleryBorder.MouseLeftButtonDown += async delegate
        {
            UC.Close_UserControls();

            if (Settings.Default.FullscreenGallery == false)
            {
                Settings.Default.FullscreenGallery = true;
            }

            await GalleryToggle.OpenFullscreenGalleryAsync().ConfigureAwait(false);
        };
        FullScreenGalleryButton.Click += async delegate
        {
            UC.Close_UserControls();

            if (Settings.Default.FullscreenGallery == false)
            {
                Settings.Default.FullscreenGallery = true;
            }

            await GalleryToggle.OpenFullscreenGalleryAsync().ConfigureAwait(false);
        };

        // ContainedGalleryBorder
        SetButtonIconMouseOverAnimations(ContainedGalleryButton, ContainedButtonBrush,
            (SolidColorBrush)Resources["ContainedIcon"]);
        ContainedGalleryBorder.MouseLeftButtonDown += async delegate
        {
            UC.Close_UserControls();
            Settings.Default.IsBottomGalleryShown = true;
            await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
        };
        ContainedGalleryButton.Click += async delegate
        {
            UC.Close_UserControls();
            Settings.Default.IsBottomGalleryShown = true;
            await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
        };
    }
}