using System.Windows.Media;
using static PicView.MouseOverAnimations;

namespace PicView.UserControls
{
    /// <summary>
    /// Interaction logic for ImageSettings.xaml
    /// </summary>
    public partial class ImageSettings
    {
        public ImageSettings()
        {
            InitializeComponent();

            FlipButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(FlipButtonBrush);
            FlipButton.MouseEnter += (s, x) => ButtonMouseOverAnim(FlipButtonBrush, true);
            FlipButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(FlipButtonBrush, false);
            FlipButton.Checked += FlipButton_Checked;
            FlipButton.Unchecked += FlipButton_Unchecked;

            SlideshowButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(SlideshowButtonBrush);
            SlideshowButton.MouseEnter += (s, x) => ButtonMouseOverAnim(SlideshowButtonBrush, true);
            SlideshowButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(SlideshowButtonBrush, false);

            RotateLeftButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(RotateLeftButtonBrush);
            RotateLeftButton.MouseEnter += (s, x) => ButtonMouseOverAnim(RotateLeftButtonBrush, true);
            RotateLeftButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(RotateLeftButtonBrush, false);

            RotateRightButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(RotateRightButtonBrush);
            RotateRightButton.MouseEnter += (s, x) => ButtonMouseOverAnim(RotateRightButtonBrush, true);
            RotateRightButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(RotateRightButtonBrush, false);

            Fullscreen_Gallery.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(FullscreenBorderBrush);
            Fullscreen_Gallery.MouseEnter += (s, x) => ButtonMouseOverAnim(FullscreenBorderBrush, true);
            Fullscreen_Gallery.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(FullscreenBorderBrush, false);

            Contained_Gallery.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ContainedBorderBrush);
            Contained_Gallery.MouseEnter += (s, x) => ButtonMouseOverAnim(ContainedBorderBrush, true);
            Contained_Gallery.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ContainedBorderBrush, false);

        }

        #region Methods

        // Change FlipButton's icon when (un)checked
        private void FlipButton_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            FlipButton.ToolTip = "Flip image [F]";
            FlipPath.Data = Geometry.Parse("M192,96v64h248c4.4,0,8,3.6,8,8v240c0,4.4-3.6,8-8,8H136c-4.4,0-8-3.6-8-8v-48c0-4.4,3.6-8,8-8h248V224H192v64L64,192 L192, 96z");
        }

        private void FlipButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            FlipButton.ToolTip = "Unflip image [F]";
            FlipPath.Data = Geometry.Parse("M448,192l-128,96v-64H128v128h248c4.4,0,8,3.6,8,8v48c0,4.4-3.6,8-8,8H72c-4.4,0-8-3.6-8-8V168c0-4.4,3.6-8,8-8h248V96 L448, 192z");
        }

        #endregion

    }
}