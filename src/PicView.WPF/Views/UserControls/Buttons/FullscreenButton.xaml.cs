using PicView.WPF.Animations;
using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.WPF.UILogic.Sizing;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Buttons;

public partial class FullscreenButton
{
    public FullscreenButton()
    {
        InitializeComponent();

        MouseEnter += delegate
        {
            ToolTip = TranslationHelper.GetTranslation(SettingsHelper.Settings.WindowProperties.Fullscreen ? "RestoreDown" : "Fullscreen");

            ButtonMouseOverAnim(FullscreenButtonBrush, true);
        };

        MouseLeave += delegate { ButtonMouseLeaveAnim(FullscreenButtonBrush, true); };

        TheButton.Click += delegate { WindowSizing.Fullscreen_Restore(!SettingsHelper.Settings.WindowProperties.Fullscreen); };

        if (!SettingsHelper.Settings.Theme.Dark)
        {
            AnimationHelper.LightThemeMouseEvent(this, IconBrush);
        }
    }
}