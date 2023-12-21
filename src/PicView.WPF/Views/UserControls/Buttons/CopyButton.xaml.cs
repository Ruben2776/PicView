using System.Windows.Controls;
using PicView.WPF.Animations;
using PicView.Core.Config;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Buttons
{
    public partial class CopyButton : UserControl
    {
        public CopyButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                if (SettingsHelper.Settings.Theme.Dark)
                {
                    SetButtonIconMouseOverAnimations(TheButton, ButtonBrush, IconBrush);
                }
                else
                {
                    TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(ButtonBrush, true);
                    TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ButtonBrush);
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }
            };
        }
    }
}