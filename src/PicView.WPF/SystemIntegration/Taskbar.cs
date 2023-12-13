using System.Windows.Shell;
using System.Windows.Threading;
using PicView.WPF.Properties;
using PicView.WPF.UILogic;

namespace PicView.WPF.SystemIntegration
{
    internal static class Taskbar
    {
        #region Progress

        /// <summary>
        /// Show progress on taskbar
        /// </summary>
        /// <param name="i">index</param>
        /// <param name="ii">size</param>
        internal static void Progress(double d)
        {
            if (!Settings.Default.IsTaskbarProgressEnabled)
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

        #endregion Progress
    }
}