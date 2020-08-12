using PicView.UILogic.Animations;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Interaction logic for ImageSettings.xaml
    /// </summary>
    public partial class ImageSettings
    {
        public ImageSettings()
        {
            InitializeComponent();

            // FullscreenGalleryBorder
            FullscreenGalleryBorder.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(FullscreenFill1); };
            FullscreenGalleryBorder.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(FullscreenFill2); };
            FullscreenGalleryBorder.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(FullscreenTextBrush); };

            FullscreenGalleryBorder.MouseEnter += delegate { ButtonMouseOverAnim(FullscreenFill1); };
            FullscreenGalleryBorder.MouseEnter += delegate { ButtonMouseOverAnim(FullscreenFill2); };
            FullscreenGalleryBorder.MouseEnter += delegate { ButtonMouseOverAnim(FullscreenTextBrush); };
            FullscreenGalleryBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(FullscreenBrush); };

            FullscreenGalleryBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(FullscreenFill1); };
            FullscreenGalleryBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(FullscreenFill2); };
            FullscreenGalleryBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(FullscreenTextBrush); };
            FullscreenGalleryBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(FullscreenBrush); };

            // ContainedGalleryBorder
            ContainedGalleryBorder.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ContainedFill); };
            ContainedGalleryBorder.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ContainedTextBrush); };

            ContainedGalleryBorder.MouseEnter += delegate { ButtonMouseOverAnim(ContainedFill); };
            ContainedGalleryBorder.MouseEnter += delegate { ButtonMouseOverAnim(ContainedTextBrush); };
            ContainedGalleryBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(ContainedButtonBrush); };

            ContainedGalleryBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(ContainedFill); };
            ContainedGalleryBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(ContainedTextBrush); };
            ContainedGalleryBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(ContainedButtonBrush); };
        }
    }
}