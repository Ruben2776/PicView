using PicView.Animations;
using PicView.UILogic;
using System.Windows;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class SlideshowButton : UserControl
    {
        public SlideshowButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(IconBrush);
                    ButtonMouseOverAnim(txtBrush);
                    AnimationHelper.MouseEnterBgTexColor(TheButtonBrush);
                };

                TheButton.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnim(IconBrush);
                    ButtonMouseLeaveAnim(txtBrush);
                    AnimationHelper.MouseLeaveBgTexColor(TheButtonBrush);
                };

                TheButton.Click += delegate
                {
                    UC.Close_UserControls();
                    Slideshow.StartSlideshow();
                };

                var s = Application.Current.Resources["StartSlideshow"] as string;
                s += " [F5]";
                ToolTip = s;
            };
        }
    }
}