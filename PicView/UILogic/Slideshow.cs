using PicView.SystemIntegration;
using PicView.UILogic.Animations;
using System;
using System.Timers;
using System.Windows;
using static PicView.ChangeImage.Navigation;

namespace PicView.UILogic
{
    internal static class Slideshow
    {
        /// <summary>
        /// Timer used for slideshow
        /// </summary>
        internal static Timer SlideTimer;

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

            if (ConfigureWindows.GetMainWindow.WindowState == WindowState.Normal)
            {
                ConfigureWindows.RenderFullscreen();
            }

            _ = NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_DISPLAY_REQUIRED); // Stop screensaver when running

            HideInterfaceLogic.ShowNavigation(false);
            HideInterfaceLogic.ShowShortcuts(false);
        }

        internal static void StopSlideshow()
        {
            SlideTimer.Stop();

            if (!Properties.Settings.Default.Fullscreen)
            {
                ConfigureWindows.Fullscreen_Restore();
            }

            _ = NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS); // Allow screensaver again
        }

        /// <summary>
        /// Timer starts Slideshow Fade animation.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="e"></param>
        private static void SlideTimer_Elapsed(object server, ElapsedEventArgs e)
        {
            AnimationHelper.Fade(ConfigureWindows.GetMainWindow.MainImage, TimeSpan.FromSeconds(0.8), TimeSpan.FromSeconds(0), 0, .5);
            _ = PicAsync();
            AnimationHelper.Fade(ConfigureWindows.GetMainWindow.MainImage, TimeSpan.FromSeconds(0.7), TimeSpan.FromSeconds(0), .5, 1);
        }
    }
}