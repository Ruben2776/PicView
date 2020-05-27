using System.Windows.Input;
using System.Windows.Media;
using static PicView.Fields;

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

            #region Add events

            CloseButton.MouseEnter += CloseButtonMouseOver;
            CloseButton.MouseLeave += CloseButtonMouseLeave;
            CloseButton.PreviewMouseLeftButtonDown += CloseButtonMouseButtonDown;

            FlipButton.MouseEnter += FlipButtonMouseOver;
            FlipButton.MouseLeave += FlipButtonMouseLeave;
            FlipButton.PreviewMouseLeftButtonDown += FlipButtonMouseButtonDown;

            FlipButton.Checked += FlipButton_Checked;
            FlipButton.Unchecked += FlipButton_Unchecked;

            SlideshowButton.MouseEnter += SlideshowButtonMouseOver;
            SlideshowButton.MouseLeave += SlideshowButtonMouseLeave;
            SlideshowButton.PreviewMouseLeftButtonDown += SlideshowButtonMouseButtonDown;

            RotateLeftButton.MouseEnter += RotateLeftButtonMouseOver;
            RotateLeftButton.MouseLeave += RotateLeftButtonMouseLeave;
            RotateLeftButton.PreviewMouseLeftButtonDown += RotateLeftButtonMouseButtonDown;
            RotateLeftButton.Click += (s, x) => Rotate_and_Flip.Rotate(false);

            RotateRightButton.MouseEnter += RotateRightButtonMouseOver;
            RotateRightButton.MouseLeave += RotateRightButtonMouseLeave;
            RotateRightButton.PreviewMouseLeftButtonDown += RotateRightButtonMouseButtonDown;
            RotateRightButton.Click += (s, x) => Rotate_and_Flip.Rotate(true);

            Fullscreen_Gallery.MouseEnter += Fullscreen_GalleryMouseOver;
            Fullscreen_Gallery.MouseLeave += Fullscreen_GalleryMouseLeave;
            Fullscreen_Gallery.PreviewMouseLeftButtonDown += Fullscreen_GalleryMouseButtonDown;

            Contained_Gallery.MouseEnter += Contained_GalleryMouseOver;
            Contained_Gallery.MouseLeave += Contained_GalleryMouseLeave;
            Contained_Gallery.PreviewMouseLeftButtonDown += Contained_GalleryMouseButtonDown;

            #endregion

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

        private void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(0, 0, 0, 0, CloseButtonBrush, false);
        }

        private void CloseButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CloseButtonBrush, false);
        }

        private void CloseButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                CloseButtonBrush,
                false
            );
        }

        // Flip Button
        private void FlipButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(0, 0, 0, 0, FlipButtonBrush, false);
        }

        private void FlipButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FlipButtonBrush, false);
        }

        private void FlipButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                FlipButtonBrush,
                false
            );
        }

        // RotateLeftButton
        private void RotateLeftButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(0, 0, 0, 0, RotateLeftButtonBrush, false);
        }

        private void RotateLeftButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(RotateLeftButtonBrush, false);
        }

        private void RotateLeftButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                RotateLeftButtonBrush,
                false
            );
        }

        // RotateRightButton
        private void RotateRightButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(0, 0, 0, 0, RotateRightButtonBrush, false);
        }

        private void RotateRightButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(RotateRightButtonBrush, false);
        }

        private void RotateRightButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                RotateRightButtonBrush,
                false
            );
        }

        // Slideshow Button
        private void SlideshowButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(0, 0, 0, 0, SlideshowButtonBrush, false);
        }

        private void SlideshowButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SlideshowButtonBrush, false);
        }

        private void SlideshowButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                SlideshowButtonBrush,
                false
            );
        }


        // Fullscreen_Gallery
        private void Fullscreen_GalleryMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(0, 0, 0, 0, FullscreenBorderBrush, false);
        }

        private void Fullscreen_GalleryMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FullscreenBorderBrush, false);
        }

        private void Fullscreen_GalleryMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                FullscreenBorderBrush,
                false
            );
        }

        //// Contained_Gallery
        private void Contained_GalleryMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(0, 0, 0, 0, ContainedBorderBrush, false);
        }

        private void Contained_GalleryMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ContainedBorderBrush, false);
        }

        private void Contained_GalleryMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                ContainedBorderBrush,
                false
            );
        }

        #endregion

    }
}