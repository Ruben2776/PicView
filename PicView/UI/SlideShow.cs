using System;
using System.Timers;
using System.Windows.Threading;
using static PicView.Library.Fields;
using static PicView.ChangeImage.Navigation;
using static PicView.UI.Sizing.WindowLogic;
using PicView.UI.Animations;
using PicView.UI.UserControls;

namespace PicView.UI
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

            UC.Close_UserControls();

            if (SlideTimer == null)
            {
                SlideTimer = new Timer()
                {
                    Interval = Properties.Settings.Default.SlideTimer,
                    Enabled = true
                };
                SlideTimer.Elapsed += SlideTimer_Elapsed;
                SlideTimer.Start();
            }
            else
            {
                SlideTimer.Start();
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
                AnimationHelper.Fade(mainWindow.img, TimeSpan.FromSeconds(0.8), TimeSpan.FromSeconds(0), 0, .5);               
                Pic();
                AnimationHelper.Fade(mainWindow.img, TimeSpan.FromSeconds(0.7), TimeSpan.FromSeconds(0), .5, 1);
            }));
        }
    }
}
