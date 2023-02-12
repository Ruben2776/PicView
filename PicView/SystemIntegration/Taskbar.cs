using PicView.UILogic;
using System.Threading.Tasks;
using System.Windows.Shell;
using System.Windows.Threading;

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
            await SetAsync(taskbar).ConfigureAwait(false);
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
            await SetAsync(taskbar).ConfigureAwait(false);
        }

        private static async Task SetAsync(TaskbarItemInfo taskbar)
        {
            taskbar.Freeze();
            try
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                {
                    ConfigureWindows.GetMainWindow.TaskbarItemInfo = taskbar;
                });
            }
            catch (System.Exception)
            {
                // Catch task canceled exception
            }
        }

        #endregion Progress
    }
}