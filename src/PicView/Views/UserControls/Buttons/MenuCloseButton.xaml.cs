using PicView.Animations;
using PicView.Properties;
using PicView.UILogic;
using System.Windows.Input;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class MenuCloseButton
    {
        public MenuCloseButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                MouseEnter += (s, x) => ButtonMouseOverAnim(CloseButtonBrush, true);
                MouseLeave += (s, x) => AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, CloseButtonBrush);
                ;

                if (!Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

                TheButton.Click += delegate
                {
                    UC.Close_UserControls();
                    Keyboard.ClearFocus();
                    UC.GetImageSettingsMenu?.GoToPic?.GoToPicBox?.Select(0, 0); // Deselect
                };
            };
        }
    }
}