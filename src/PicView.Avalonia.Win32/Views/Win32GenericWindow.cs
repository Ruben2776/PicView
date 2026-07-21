using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Win32.WindowImpl;

namespace PicView.Avalonia.Win32.Views;

public class Win32GenericWindow : GenericWindow
{
    protected Win32GenericWindow() => CaptionButtonCornerHandler.Attach(this);
}

public class Win32PrintWindow : PrintWindow
{
    protected Win32PrintWindow() => CaptionButtonCornerHandler.Attach(this);
}
