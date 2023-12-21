using System.Windows.Input;
using PicView.WPF.Animations;
using PicView.Core.Config;
using PicView.WPF.UILogic;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Buttons
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

                if (!SettingsHelper.Settings.Theme.Dark)
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