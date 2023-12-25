using System.Windows;
using PicView.WPF.Animations;
using PicView.Core.Config;
using PicView.Core.Localization;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Buttons;

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

            if (!SettingsHelper.Settings.Theme.Dark)
            {
                AnimationHelper.LightThemeMouseEvent(this, IconBrush);
            }

            MouseLeave += (s, x) => ButtonMouseLeaveAnim(MinButtonBrush, true);

            ToolTip = TranslationHelper.GetTranslation("Minimize");
        };
    }
}