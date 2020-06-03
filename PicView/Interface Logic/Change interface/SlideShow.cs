using System;
using System.Timers;
using System.Windows.Threading;
using static PicView.Fields;
using static PicView.Navigation;
using static PicView.WindowLogic;

namespace PicView
{
    internal static class SlideShow
    {
        /// <summary>
        /// Maximize and removes Interface and start timer for slideshow.
        /// </summary>
        internal static void StartSlideshow()
        {
            if (Pics.Count == 0)
            {
                return;
            }

            if (SlideTimer == null)
            {
                SlideTimer = new Timer()
                {
                    //Interval = 3000,
                    Interval = Properties.Settings.Default.SlideTimer,
                    Enabled = true
                };
                SlideTimer.Elapsed += SlideTimer_Elapsed;
                SlideTimer.Start();
            }
            else
            {
                StopSlideshow();
                return;
            }

            if (!Properties.Settings.Default.Fullscreen)
            {
                Fullscreen_Restore(true);
            }

            _ = NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_DISPLAY_REQUIRED); // Stop screensaver when running
        }

        internal static void StopSlideshow()
        {
            SlideTimer.Stop();

            if (Properties.Settings.Default.Fullscreen)
            {
                Fullscreen_Restore();
            }

            _ = NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS); // Allow screensaver again
        }

        /// <summary>
        /// Timer starts Slideshow Fade animation.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="e"></param>
        internal static async void SlideTimer_Elapsed(object server, ElapsedEventArgs e)
        {
            await mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                // TODO make fancy pants animations work
                //AnimationHelper.Fade(mainWindow.img, TimeSpan.FromSeconds(0.6), TimeSpan.FromSeconds(1), 1, 0);
                //AnimationHelper.Fade(mainWindow.img, TimeSpan.FromSeconds(0.6), TimeSpan.FromSeconds(1), 0, 1);
                Pic();
            }));
        }
    }
}
