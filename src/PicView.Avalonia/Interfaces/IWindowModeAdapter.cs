using PicView.Avalonia.CustomControls;
using PicView.Core.Sizing;

namespace PicView.Avalonia.Interfaces;

/// <summary>
/// What one platform does natively when a window changes Window Mode. It only puts the window in a native state and
/// handles the platform's quirks; <see cref="WindowBehavior.WindowModeSwitcher"/> runs the rest of the change.
/// </summary>
public interface IWindowModeAdapter
{
    /// <summary>
    /// Whether the first native state a window reports is ignored, so only later changes reach its Window Mode.
    /// </summary>
    bool SkipsInitialNativeState { get; }

    /// <summary>
    /// Puts the window in the native state of the mode of <paramref name="state"/>. Never called with Minimized.
    /// </summary>
    void EnterNativeState(MainWindow window, WindowModeState state);
}
