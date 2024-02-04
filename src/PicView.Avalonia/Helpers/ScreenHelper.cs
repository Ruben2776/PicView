using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Platform;

namespace PicView.Avalonia.Helpers;

public static class ScreenHelper
{
    public static Screen? GetScreen(Control control)
    {
        var window = control.GetSelfAndLogicalAncestors().OfType<Window>().First();
        
        return window.Screens.ScreenFromPoint(new PixelPoint());
    }
}
