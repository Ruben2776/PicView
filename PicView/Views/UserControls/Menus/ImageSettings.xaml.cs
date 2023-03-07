using PicView.Animations;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
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

            switch (Settings.Default.UserLanguage)
            {
                case "ru":
                case "pl":
                case "es":
                    Contained_Gallery.FontSize = Fullscreen_Gallery.FontSize = 13;
                    break;
            }

            FullscreenGalleryBorder.MouseEnter += delegate { ButtonMouseOverAnim(FullscreenFill1); };
            FullscreenGalleryBorder.MouseEnter += delegate { ButtonMouseOverAnim(FullscreenFill2); };
            FullscreenGalleryBorder.MouseEnter += delegate { ButtonMouseOverAnim(FullscreenTextBrush); };
            FullscreenGalleryBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(FullscreenBrush); };

            FullscreenGalleryBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(FullscreenFill1); };
            FullscreenGalleryBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(FullscreenFill2); };
            FullscreenGalleryBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(FullscreenTextBrush); };
            FullscreenGalleryBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(FullscreenBrush); };

            ContainedGalleryBorder.MouseEnter += delegate { ButtonMouseOverAnim(ContainedFill); };
            ContainedGalleryBorder.MouseEnter += delegate { ButtonMouseOverAnim(ContainedTextBrush); };
            ContainedGalleryBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(ContainedButtonBrush); };

            ContainedGalleryBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(ContainedFill); };
            ContainedGalleryBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(ContainedTextBrush); };
            ContainedGalleryBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(ContainedButtonBrush); };

            Contained_Gallery.Click += async delegate
            {
                UC.Close_UserControls();
                await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
            };
            Fullscreen_Gallery.Click += async delegate
            {
                UC.Close_UserControls();

                if (Settings.Default.FullscreenGalleryHorizontal == false)
                {
                    Settings.Default.FullscreenGalleryHorizontal = true;
                }

                await GalleryToggle.OpenFullscreenGalleryAsync(false).ConfigureAwait(false);
            };
        }
    }
}