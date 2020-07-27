using System.Windows;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.UILogic.UserControls
{
    public partial class SlideshowButton : UserControl
    {
        public SlideshowButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(SlideshowButtonBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(SlideshowButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(SlideshowButtonBrush, false);
                TheButton.Click += delegate { UC.Close_UserControls(); Slideshow.StartSlideshow(); };

                var s = Application.Current.Resources["StartSlideshow"] as string;
                s += " [F5]";
                ToolTip = s;
            };
        }
    }
}