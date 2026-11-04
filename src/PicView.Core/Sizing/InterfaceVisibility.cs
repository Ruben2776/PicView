using System.Runtime.InteropServices;

namespace PicView.Core.Sizing;

/// <summary>
/// Which of a Main Window's bars show, their heights, and whether the hover bar may show, derived from the
/// three interface settings and the window's shown Window Mode (ADR-0021).
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct InterfaceVisibility(
    bool IsInterfaceShown,
    bool IsBottomBarShown,
    double TitlebarHeight,
    double BottombarHeight,
    bool ShowsHoverBar)
{
    /// <param name="showInterface">The interface setting.</param>
    /// <param name="showBottomBar">The bottom bar setting.</param>
    /// <param name="showHoverBar">The hover bar setting.</param>
    /// <param name="shownMode">The mode the window is shown in, or comes back to after Minimized.</param>
    public static InterfaceVisibility For(bool showInterface, bool showBottomBar, bool showHoverBar, WindowMode shownMode)
    {
        if (shownMode is WindowMode.Fullscreen)
        {
            return new InterfaceVisibility(false, false, 0, 0, showHoverBar);
        }

        var bottomBarShown = showInterface && showBottomBar;
        return new InterfaceVisibility(
            showInterface,
            bottomBarShown,
            showInterface ? SizeDefaults.MainTitlebarHeight : 0,
            bottomBarShown ? SizeDefaults.BottombarHeight : 0,
            showHoverBar && !bottomBarShown);
    }
}
