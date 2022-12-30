using PicView.Animations;
using PicView.Properties;
using PicView.UILogic;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class MenuCloseButton : UserControl
    {
        public MenuCloseButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                MouseEnter += (s, x) => ButtonMouseOverAnim(CloseButtonBrush, true);
                MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(CloseButtonBrush);

                if (!Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

                TheButton.Click += delegate { UC.Close_UserControls(); };
            };
        }
    }
}