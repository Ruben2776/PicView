using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Sizing;

namespace PicView.Avalonia.MacOS.WindowImpl;

/// <summary>
/// What macOS does natively when a window changes Window Mode.
/// </summary>
public sealed class MacWindowModeAdapter : IWindowModeAdapter
{
    /// <summary>
    /// macOS reports the window's state once as it opens, which is not a change the user made.
    /// </summary>
    public bool SkipsInitialNativeState => true;

    public void EnterNativeState(MainWindow window, WindowModeState state)
    {
        ScreenHelper.UpdateScreenSize(window);
        if (state.Mode is not WindowMode.Normal)
        {
            WindowModeSwitcher.SetNativeState(window, state.Mode);
            return;
        }

        // Disabling ExtendClientAreaToDecorationsHint and enabling it again fixes not being able to draw in title bar
        window.ExtendClientAreaToDecorationsHint = false;
        WindowModeSwitcher.SetNativeState(window, state.Mode);
        Dispatcher.UIThread.Post(() =>
        {
            window.ExtendClientAreaToDecorationsHint = true;
            if (state.AutoFit)
            {
                window.WindowFitter.FollowContent();
                WindowResizing.FastCenterWindow(window);
            }
        });
    }
}
