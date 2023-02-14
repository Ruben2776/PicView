using PicView.Animations;
using PicView.Properties;
using System.Windows;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class MinButton : UserControl
    {
        public MinButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.Click += (_, _) => SystemCommands.MinimizeWindow(Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive));

                MouseEnter += (s, x) => ButtonMouseOverAnim(MinButtonBrush, true);

                if (!Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

                MouseLeave += (s, x) => ButtonMouseLeaveAnim(MinButtonBrush, true);

                ToolTip = Application.Current.Resources["Minimize"];
            };
        }
    }
}