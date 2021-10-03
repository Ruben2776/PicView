using PicView.Animations;
using System.Windows;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class CloseButton : UserControl
    {
        public CloseButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(CloseButtonBrush);

                MouseEnter += (_, _) => ButtonMouseOverAnim(CloseButtonBrush, true);
                MouseLeave += (_, _) => ButtonMouseLeaveAnim(CloseButtonBrush, true);

                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

                ToolTip = Application.Current.Resources["Close"];
            };
        }
    }
}