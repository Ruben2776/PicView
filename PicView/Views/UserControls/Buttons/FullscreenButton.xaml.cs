using PicView.UILogic;
using PicView.Animations;
using System.Windows;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class FullscreenButton : UserControl
    {
        public FullscreenButton()
        {
            InitializeComponent();

            MouseEnter += delegate
            {
                if (Properties.Settings.Default.Fullscreen)
                {
                    ToolTip = Application.Current.Resources["RestoreDown"];
                }
                else
                {
                    ToolTip = Application.Current.Resources["Fullscreen"];
                }

                ButtonMouseOverAnim(FullscreenButtonBrush, true);
            };

            MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(FullscreenButtonBrush, true);
            };

            TheButton.Click += delegate
            {
                UILogic.Sizing.WindowSizing.Fullscreen_Restore();
            };

            if (!Properties.Settings.Default.DarkTheme)
            {
                AnimationHelper.LightThemeMouseEvent(this, IconBrush);
            }
        }
    }
}