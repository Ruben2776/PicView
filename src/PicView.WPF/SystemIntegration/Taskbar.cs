using PicView.Core.Config;
using PicView.WPF.UILogic;
using System.Windows.Shell;
using System.Windows.Threading;

namespace PicView.WPF.SystemIntegration;

internal static class Taskbar
{
    /// <summary>
    /// Updates the progress of the taskbar.
    /// </summary>
    /// <param name="d">The progress value.</param>
    internal static void Progress(double d)
    {
        if (!SettingsHelper.Settings.UIProperties.IsTaskbarProgressEnabled)
            return;

        TaskbarItemInfo taskbar = new()
        {
            ProgressState = TaskbarItemProgressState.Normal,
            ProgressValue = d
        };
        Set(taskbar);
    }

    /// <summary>
    /// Stop showing taskbar progress, return to default
    /// </summary>
    internal static void NoProgress()
    {
        TaskbarItemInfo taskbar = new()
        {
            ProgressState = TaskbarItemProgressState.Normal,
            ProgressValue = 0.0
        };
        Set(taskbar);
    }

    private static void Set(TaskbarItemInfo taskbar)
    {
        taskbar.Freeze();
        try
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Background,
                () => ConfigureWindows.GetMainWindow.TaskbarItemInfo = taskbar);
        }
        catch (Exception e)
        {
            Tooltip.ShowTooltipMessage(e, true, TimeSpan.FromSeconds(5));
            // Catch task canceled exception
        }
    }
}