using PicView.Animations;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class LinkButton : UserControl
    {
        public LinkButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(CopyButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(CopyButtonBrush, false);

                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }
            };
        }
    }
}