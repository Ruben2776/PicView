using PicView.Animations;
using PicView.UILogic;
using System.Windows;

namespace PicView.Views.UserControls.Buttons;

public partial class Minus
{
    public Minus()
    {
        InitializeComponent();

        MouseEnter += delegate
        {
            MouseOverAnimations.AltInterfaceMouseOver(PolyFill, CanvasBGcolor, BorderBrushKey);
        };

        MouseLeave += delegate
        {
            MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, CanvasBGcolor, BorderBrushKey);
        };

        TheButton.Click += (_, _) => SystemCommands.MinimizeWindow(ConfigureWindows.GetMainWindow);
    }
}