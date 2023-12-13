using PicView.WPF.Animations;
using PicView.WPF.Properties;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Buttons
{
    public partial class LinkButton
    {
        public LinkButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(CopyButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(CopyButtonBrush);

                if (!Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }
            };
        }
    }
}