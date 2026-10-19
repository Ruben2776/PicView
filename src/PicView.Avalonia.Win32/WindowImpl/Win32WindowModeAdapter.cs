using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Sizing;

namespace PicView.Avalonia.Win32.WindowImpl;

/// <summary>
/// What Windows does natively when a window changes Window Mode.
/// </summary>
public sealed class Win32WindowModeAdapter : IWindowModeAdapter
{
    public bool SkipsInitialNativeState => false;

    public void EnterNativeState(MainWindow window, WindowModeState state) =>
        WindowModeSwitcher.SetNativeState(window, state.Mode);
}
