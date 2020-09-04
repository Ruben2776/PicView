using PicView.UILogic.Animations;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class SettingsButton : UserControl
    {
        public SettingsButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(SettingsButtonBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(SettingsButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(SettingsButtonBrush, false);

                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush1);
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush2);
                }
            };
        }
    }
}