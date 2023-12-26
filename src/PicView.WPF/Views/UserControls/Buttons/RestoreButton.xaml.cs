using System.Windows;
using PicView.WPF.Animations;
using PicView.Core.Config;
using PicView.WPF.UILogic.Sizing;

namespace PicView.WPF.Views.UserControls.Buttons;

public partial class RestoreButton
{
    public RestoreButton()
    {
        InitializeComponent();

        TheButton.Click += delegate { WindowSizing.Fullscreen_Restore(!SettingsHelper.Settings.WindowProperties.Fullscreen); };

        MouseEnter += delegate
        {
            ToolTip =
                !SettingsHelper.Settings.WindowProperties.Fullscreen
                    ? Application.Current.Resources["Fullscreen"]
                    : Application.Current.Resources["RestoreDown"];

            MouseOverAnimations.AltInterfaceMouseOver(PolyFill, CanvasBGcolor, BorderBrushKey);
        };

        MouseLeave += delegate
        {
            MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, CanvasBGcolor, BorderBrushKey);
        };
    }
}