using PicView.Animations;
using PicView.Properties;
using PicView.UILogic.Sizing;
using System.Windows;

namespace PicView.Views.UserControls.Buttons;

public partial class RestoreButton
{
    public RestoreButton()
    {
        InitializeComponent();

        TheButton.Click += delegate { WindowSizing.Fullscreen_Restore(!Settings.Default.Fullscreen); };

        MouseEnter += delegate
        {
            ToolTip =
                !Settings.Default.Fullscreen ? Application.Current.Resources["Fullscreen"] : Application.Current.Resources["RestoreDown"];

            MouseOverAnimations.AltInterfaceMouseOver(PolyFill, CanvasBGcolor, BorderBrushKey);
        };

        MouseLeave += delegate
        {
            MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, CanvasBGcolor, BorderBrushKey);
        };
    }
}