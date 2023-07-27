using PicView.Animations;
using PicView.Properties;
using System.Windows;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons;

public partial class CloseButton
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

            ToolTip = Application.Current.TryFindResource("Close");
        };
    }
}