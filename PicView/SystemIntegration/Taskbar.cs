

using System.Threading.Tasks;
using System.Windows.Shell;
using System.Windows.Threading;
using PicView.UILogic;

namespace PicView.SystemIntegration
{
    internal static class Taskbar
    {
        #region Progress

        /// <summary>
        /// Show progress on taskbar
        /// </summary>
        /// <param name="i">index</param>
        /// <param name="ii">size</param>
        internal static async Task Progress(double d)
        {
            TaskbarItemInfo taskbar = new()
            {
                ProgressState = TaskbarItemProgressState.Normal,
                ProgressValue = d
            };
            taskbar.Freeze();
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                ConfigureWindows.GetMainWindow.TaskbarItemInfo = taskbar;
            });
        }

        /// <summary>
        /// Stop showing taskbar progress, return to default
        /// </summary>
        internal static async Task NoProgress()
        {
            TaskbarItemInfo taskbar = new()
            {
                ProgressState = TaskbarItemProgressState.Normal,
                ProgressValue = 0.0
            };
            taskbar.Freeze();
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                ConfigureWindows.GetMainWindow.TaskbarItemInfo = taskbar;
            });
        }

        #endregion Progress
    }
}