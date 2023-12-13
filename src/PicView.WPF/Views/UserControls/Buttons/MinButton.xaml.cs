using System.Windows;
using PicView.WPF.Animations;
using PicView.WPF.Properties;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Buttons
{
    public partial class MinButton
    {
        public MinButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.Click += (_, _) =>
                    SystemCommands.MinimizeWindow(Application.Current.Windows.OfType<Window>()
                        .SingleOrDefault(x => x.IsActive));

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