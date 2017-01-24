using PicView.lib;
using System.Windows.Input;
using System.Windows.Media;

namespace PicView.lib.UserControls
{
    /// <summary>
    /// Interaction logic for ImageSettings.xaml
    /// </summary>
    public partial class ImageSettings
    {
        public ImageSettings()
        {
            InitializeComponent();

            CloseButton.MouseEnter += CloseButtonMouseOver;
            CloseButton.MouseLeave += CloseButtonMouseLeave;
            CloseButton.PreviewMouseLeftButtonDown += CloseButtonMouseButtonDown;

            FlipButton.MouseEnter += FlipButtonMouseOver;
            FlipButton.MouseLeave += FlipButtonMouseLeave;
            FlipButton.PreviewMouseLeftButtonDown += FlipButtonMouseButtonDown;

            FlipButton.Checked += FlipButton_Checked;
            FlipButton.Unchecked += FlipButton_Unchecked;

            ro0Border.MouseEnter += ro0Border_MouseEnter;
            ro0Border.MouseLeave += ro0Border_MouseLeave;
            ro0Border.PreviewMouseLeftButtonDown += ro0Border_PreviewMouseLeftButtonDown;

            ro90Border.MouseEnter += ro90Border_MouseEnter;
            ro90Border.MouseLeave += ro90Border_MouseLeave;
            ro90Border.PreviewMouseLeftButtonDown += ro90Border_PreviewMouseLeftButtonDown;

            ro180Border.MouseEnter += ro180Border_MouseEnter;
            ro180Border.MouseLeave += ro180Border_MouseLeave;
            ro180Border.PreviewMouseLeftButtonDown += ro180Border_PreviewMouseLeftButtonDown;

            ro270Border.MouseEnter += ro270Border_MouseEnter;
            ro270Border.MouseLeave += ro270Border_MouseLeave;
            ro270Border.PreviewMouseLeftButtonDown += ro270Border_PreviewMouseLeftButtonDown;

        }

        private void FlipButton_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            FlipButton.ToolTip = "Flip image";
            FlipPath.Data = Geometry.Parse("M192,96v64h248c4.4,0,8,3.6,8,8v240c0,4.4-3.6,8-8,8H136c-4.4,0-8-3.6-8-8v-48c0-4.4,3.6-8,8-8h248V224H192v64L64,192 L192, 96z");
        }

        private void FlipButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            FlipButton.ToolTip = "Unflip image";
            FlipPath.Data = Geometry.Parse("M448,192l-128,96v-64H128v128h248c4.4,0,8,3.6,8,8v48c0,4.4-3.6,8-8,8H72c-4.4,0-8-3.6-8-8V168c0-4.4,3.6-8,8-8h248V96 L448, 192z");
        }

        #region 0 border
        void ro0Border_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, ro0BorderBrush, true);
        }

        void ro0Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ro0BorderBrush, true);
        }

        void ro0Border_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, ro0BorderBrush, true);
        }
        #endregion

        #region 90 border
        void ro90Border_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, ro90BorderBrush, true);
        }

        void ro90Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ro90BorderBrush, true);
        }

        void ro90Border_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, ro90BorderBrush, true);
        }
        #endregion

        #region 180 border
        void ro180Border_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, ro180BorderBrush, true);
        }

        void ro180Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ro180BorderBrush, true);
        }

        void ro180Border_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, ro180BorderBrush, true);
        }
        #endregion

        #region 270 border
        void ro270Border_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, ro270BorderBrush, true);
        }

        void ro270Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ro270BorderBrush, true);
        }

        void ro270Border_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, ro270BorderBrush, true);
        }
        #endregion

        #region Close Button

        private void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(250, 17, 17, 17, CloseButtonBrush, false);
        }

        private void CloseButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CloseButtonBrush, false);
        }

        private void CloseButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(250, 17, 17, 17, CloseButtonBrush, false);
        }

        #endregion

        #region Flip Button

        private void FlipButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(250, 17, 17, 17, FlipButtonBrush, false);
        }

        private void FlipButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FlipButtonBrush, false);
        }

        private void FlipButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(250, 17, 17, 17, FlipButtonBrush, false);
        }

        #endregion

    }
}