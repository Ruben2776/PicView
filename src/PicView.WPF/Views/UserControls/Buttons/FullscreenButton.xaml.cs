using System.Windows;
using PicView.WPF.Animations;
using PicView.Core.Config;
using PicView.WPF.UILogic.Sizing;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Buttons
{
    public partial class FullscreenButton
    {
        public FullscreenButton()
        {
            InitializeComponent();

            MouseEnter += delegate
            {
                if (SettingsHelper.Settings.WindowProperties.Fullscreen)
                {
                    ToolTip = Application.Current.Resources["RestoreDown"];
                }
                else
                {
                    ToolTip = Application.Current.Resources["Fullscreen"];
                }

                ButtonMouseOverAnim(FullscreenButtonBrush, true);
            };

            MouseLeave += delegate { ButtonMouseLeaveAnim(FullscreenButtonBrush, true); };

            TheButton.Click += delegate { WindowSizing.Fullscreen_Restore(!SettingsHelper.Settings.WindowProperties.Fullscreen); };

            if (!SettingsHelper.Settings.Theme.Dark)
            {
                AnimationHelper.LightThemeMouseEvent(this, IconBrush);
            }
        }
    }
}