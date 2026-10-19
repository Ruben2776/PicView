using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Sizing;

namespace PicView.Avalonia.Linux.WindowImpl;

/// <summary>
/// What Linux does natively when a window changes Window Mode.
/// </summary>
public sealed class LinuxWindowModeAdapter : IWindowModeAdapter
{
    public bool SkipsInitialNativeState => false;

    public void EnterNativeState(MainWindow window, WindowMode mode) =>
        WindowModeSwitcher.SetNativeState(window, mode);
}
