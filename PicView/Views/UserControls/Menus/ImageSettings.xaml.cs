using System.Windows;
using System.Windows.Media;
using PicView.Animations;
using PicView.ConfigureSettings;
using PicView.Editing.Crop;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.TransformImage;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Menus
{
    /// <summary>
    /// Interaction logic for ImageSettings.xaml
    /// </summary>
    public partial class ImageSettings
    {
        public ImageSettings()
        {
            InitializeComponent();

            // RotateLeftButton
            SetIconButterMouseOverAnimations(RotateLeftButton, RotateLeftButtonBrush, RotateLeftIconBrush);
            RotateLeftButton.Click += async (_, _) => await Rotation.RotateAndMoveCursor(true, RotateLeftButton).ConfigureAwait(false);

            // RotateRightButton
            SetIconButterMouseOverAnimations(RotateRightButton, RotateRightButtonBrush, RotateRightIconBrush);
            RotateRightButton.Click += async (_, _) => await Rotation.RotateAndMoveCursor(false, RotateLeftButton).ConfigureAwait(false);

            // ResizeButton
            SetIconButterMouseOverAnimations(ResizeButtonBorder, ResizeBorderBrush, (SolidColorBrush)Resources["ResizeIcon"]);
            ResizeButton.Click +=  (_, _) => UpdateUIValues.ToggleQuickResize();
            ResizeButtonBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.ToggleQuickResize();

            // CropButton
            SetIconButterMouseOverAnimations(CropButtonBorder, CropBorderBrush, (SolidColorBrush)Resources["CropIcon"]);
            CropButton.Click += (_, _) => CropFunctions.StartCrop();
            CropButtonBorder.MouseLeftButtonDown += (_, _) => CropFunctions.StartCrop();

            // OptimizeButton
            SetIconButterMouseOverAnimations(OptimizeBorder, BgBorderBrush, (SolidColorBrush)Resources["OptimizeIcon"]);
            OptimizeButton.Click += async(_, _) => await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);
            OptimizeBorder.MouseLeftButtonDown += async (_, _) => await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);

            // SlideShowBorder
            SetIconButterMouseOverAnimations(SlideShowBorder, SlideShowBorderBrush, (SolidColorBrush)Resources["SlideshowIcon"]);
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
            SetIconButterMouseOverAnimations(FullScreenGalleryButton, FullScreenBrush, (SolidColorBrush)Resources["FullScreenIcon"]);
            FullscreenGalleryBorder.MouseLeftButtonDown += async delegate
            {
                UC.Close_UserControls();

                if (Settings.Default.FullscreenGalleryHorizontal == false)
                {
                    Settings.Default.FullscreenGalleryHorizontal = true;
                }

                await GalleryToggle.OpenFullscreenGalleryAsync(false).ConfigureAwait(false);
            };
            FullScreenGalleryButton.Click += async delegate
            {
                UC.Close_UserControls();

                if (Settings.Default.FullscreenGalleryHorizontal == false)
                {
                    Settings.Default.FullscreenGalleryHorizontal = true;
                }

                await GalleryToggle.OpenFullscreenGalleryAsync(false).ConfigureAwait(false);
            };

            // ContainedGalleryBorder
            SetIconButterMouseOverAnimations(ContainedGalleryButton, ContainedButtonBrush, (SolidColorBrush)Resources["ContainedIcon"]);
            ContainedGalleryBorder.MouseLeftButtonDown += async delegate
            {
                UC.Close_UserControls();
                await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
            };
            ContainedGalleryButton.Click += async delegate
            {
                UC.Close_UserControls();
                await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
            };
        }
    }
}