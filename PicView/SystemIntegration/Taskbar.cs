

using PicView.UILogic;
using System.Threading.Tasks;

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
            System.Windows.Shell.TaskbarItemInfo taskbar = new()
            {
                ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal,
                ProgressValue = d
            };
            taskbar.Freeze();
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
            {
                ConfigureWindows.GetMainWindow.TaskbarItemInfo = taskbar;
            });
        }

        /// <summary>
        /// Stop showing taskbar progress, return to default
        /// </summary>
        internal static async Task NoProgress()
        {
            System.Windows.Shell.TaskbarItemInfo taskbar = new()
            {
                ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal,
                ProgressValue = 0.0
            };
            taskbar.Freeze();
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
            {
                ConfigureWindows.GetMainWindow.TaskbarItemInfo = taskbar;
            });
        }

        #endregion Progress
    }
}