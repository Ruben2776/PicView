using PicView.Animations;
using PicView.ConfigureSettings;
using PicView.Properties;
using System.Windows;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class CloseButton : UserControl
    {
        public CloseButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                MouseEnter += (_, _) => ButtonMouseOverAnim(CloseButtonBrush, true);
                MouseLeave += (_, _) => ButtonMouseLeaveAnim(CloseButtonBrush, true);

                if (!Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

                ToolTip = Application.Current.Resources["Close"];
            };
        }
    }
}