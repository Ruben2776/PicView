using PicView.ChangeImage;
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
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ReloadButtonBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(ReloadButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ReloadButtonBrush, false);

                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush1);
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush2);
                }

                TheButton.Click += delegate { Error_Handling.Reload(); };
            };
        }
    }
}