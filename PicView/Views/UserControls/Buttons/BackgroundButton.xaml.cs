using PicView.UILogic.Animations;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class BackGroundButton : UserControl
    {
        public BackGroundButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(bgBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(bgBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(bgBrush, false);

                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush1);
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush2);
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush3);
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush4);
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush5);
                }

                TheButton.Click += ConfigureSettings.ConfigColors.ChangeBackground;
            };
        }
    }
}