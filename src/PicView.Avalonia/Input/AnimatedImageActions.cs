using System.Globalization;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Animation;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Input;

/// <summary>
/// What the Animated Image Input Mode does with its Contextual Actions: pause, Frame Step and Playback Speed of the
/// active tab's animated images, with a tooltip message saying which frame or speed it is at. The Keybinds and the
/// Animation menu both run them here.
/// </summary>
public static class AnimatedImageActions
{
    /// <param name="window">The Main Window that shows the message; none shows without it.</param>
    /// <returns><c>false</c> when the active tab plays no animated image, or <paramref name="contextualAction"/> is
    /// not one of these, so its Keybind falls through.</returns>
    public static bool TryRun(MainWindow? window, MainWindowViewModel vm, string contextualAction)
    {
        var animation = vm.WindowTabs.ActiveTab.CurrentValue?.Animation;
        if (animation?.Lead is null)
        {
            return false;
        }

        switch (contextualAction)
        {
            case ContextualActionHandler.PlayPause:
                animation.TogglePause();
                return true;
            case ContextualActionHandler.NextFrame:
                animation.Step(1);
                ShowFrame(window, animation);
                return true;
            case ContextualActionHandler.PrevFrame:
                animation.Step(-1);
                ShowFrame(window, animation);
                return true;
            case ContextualActionHandler.SpeedUp:
                Show(window, FormatSpeed(animation.SpeedUp()));
                return true;
            case ContextualActionHandler.SlowDown:
                Show(window, FormatSpeed(animation.SlowDown()));
                return true;
            case ContextualActionHandler.ResetSpeed:
                animation.ResetSpeed();
                Show(window, FormatSpeed(animation.Speed));
                return true;
            default:
                return false;
        }
    }

    /// <summary>Formats a Playback Speed as shown to the user, such as <c>0.5×</c> or <c>2×</c>.</summary>
    internal static string FormatSpeed(double speed) =>
        speed.ToString("0.##", CultureInfo.CurrentCulture) + "×";

    /// <summary>Formats a frame as shown to the user, counting from 1, such as <c>Frame 3 / 12</c>.</summary>
    internal static string FormatFrame(string? frameLabel, int frame, int count) =>
        string.IsNullOrEmpty(frameLabel) ? $"{frame + 1} / {count}" : $"{frameLabel} {frame + 1} / {count}";

    private static void ShowFrame(MainWindow? window, TabAnimation animation)
    {
        // No timeline until the decoder has opened the file, so there is no frame to name yet
        if (animation.Lead is { Timeline: { Count: > 0 } timeline } lead)
        {
            Show(window, FormatFrame(TranslationManager.Translation.Frame, lead.CurrentFrame, timeline.Count));
        }
    }

    /// <summary>
    /// Shown as the zoom percentage is, straight in the window, so a message that follows another quickly replaces it
    /// instead of staying up.
    /// </summary>
    private static void Show(MainWindow? window, string message)
    {
        if (window is not null)
        {
            _ = TooltipHelper.ShowTooltipMessageAsync(message, false, window, TimeSpan.FromSeconds(1.5));
        }
    }
}
