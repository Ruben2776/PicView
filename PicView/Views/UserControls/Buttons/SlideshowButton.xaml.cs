using PicView.UILogic;
using PicView.UILogic.Animations;
using System.Windows;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class SlideshowButton : UserControl
    {
        public SlideshowButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += delegate
                {
                    PreviewMouseButtonDownAnim(IconBrush);
                    PreviewMouseButtonDownAnim(txtBrush);
                };

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