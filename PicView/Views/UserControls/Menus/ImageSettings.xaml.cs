using System.Windows.Media;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.UILogic.UserControls
{
    /// <summary>
    /// Interaction logic for ImageSettings.xaml
    /// </summary>
    public partial class ImageSettings
    {
        public ImageSettings()
        {
            InitializeComponent();

            Fullscreen_Gallery.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(FullscreenBorderBrush);
            Fullscreen_Gallery.MouseEnter += (s, x) => ButtonMouseOverAnim(FullscreenBorderBrush, true);
            Fullscreen_Gallery.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(FullscreenBorderBrush, false);

            Contained_Gallery.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ContainedBorderBrush);
            Contained_Gallery.MouseEnter += (s, x) => ButtonMouseOverAnim(ContainedBorderBrush, true);
            Contained_Gallery.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ContainedBorderBrush, false);
        }
    }
}