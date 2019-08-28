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

            Rotation0Border.MouseEnter += Rotation0Border_MouseEnter;
            Rotation0Border.MouseLeave += Rotation0Border_MouseLeave;
            Rotation0Border.PreviewMouseLeftButtonDown += Rotation0Border_PreviewMouseLeftButtonDown;

            Rotation90Border.MouseEnter += Rotation90Border_MouseEnter;
            Rotation90Border.MouseLeave += Rotation90Border_MouseLeave;
            Rotation90Border.PreviewMouseLeftButtonDown += Rotation90Border_PreviewMouseLeftButtonDown;

            Rotation180Border.MouseEnter += Rotation180Border_MouseEnter;
            Rotation180Border.MouseLeave += Rotation180Border_MouseLeave;
            Rotation180Border.PreviewMouseLeftButtonDown += Rotation180Border_PreviewMouseLeftButtonDown;

            Rotation270Border.MouseEnter += Rotation270Border_MouseEnter;
            Rotation270Border.MouseLeave += Rotation270Border_MouseLeave;
            Rotation270Border.PreviewMouseLeftButtonDown += Rotation270Border_PreviewMouseLeftButtonDown;

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

        // Rotation0Border
        void Rotation0Border_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, Rotation0BorderBrush, true);
        }

        void Rotation0Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(Rotation0BorderBrush, true);
        }

        private void Rotation0Border_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                Rotation0BorderBrush,
                true
            );
        }

        // 90 border
        void Rotation90Border_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, Rotation90BorderBrush, true);
        }

        void Rotation90Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(Rotation90BorderBrush, true);
        }

        private void Rotation90Border_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                Rotation90BorderBrush,
                true
            );
        }

        // Rotation180Border
        void Rotation180Border_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, Rotation180BorderBrush, true);
        }

        void Rotation180Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(Rotation180BorderBrush, true);
        }

        private void Rotation180Border_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                Rotation180BorderBrush,
                true
            );
        }

        // Rotation270Border
        void Rotation270Border_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, Rotation270BorderBrush, true);
        }

        void Rotation270Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(Rotation270BorderBrush, true);
        }

        private void Rotation270Border_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                Rotation270BorderBrush,
                true
            );
        }

        // Close Button
        private void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, CloseButtonBrush, false);
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
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, FlipButtonBrush, false);
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

        #endregion

    }
}