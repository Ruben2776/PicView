using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace PicView.Data.Screen;

public static class ScreenHelper
{
    public static Avalonia.Platform.Screen GetScreen(Control control)
    {
        var window = control.GetSelfAndLogicalAncestors().OfType<Window>().First();
            
        return window.Screens.ScreenFromPoint(new PixelPoint());
    }
}