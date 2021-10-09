using PicView.Animations;
using PicView.UILogic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class MinButton : UserControl
    {
        public MinButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.Click += (_, _) => SystemCommands.MinimizeWindow(Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive));

                PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(MinButtonBrush);
                MouseEnter += (s, x) => ButtonMouseOverAnim(MinButtonBrush, true);

                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

                MouseLeave += (s, x) => ButtonMouseLeaveAnim(MinButtonBrush, true);

                ToolTip = Application.Current.Resources["Minimize"];
            };
        }
    }
}