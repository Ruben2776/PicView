using PicView.Animations;
using PicView.ChangeImage;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic.Sizing;
using System.Windows;
using static PicView.ChangeImage.Navigation;

namespace PicView.UILogic
{
    internal static class Slideshow
    {
        /// <summary>
        /// Timer used for slideshow
        /// </summary>
        internal static System.Timers.Timer? SlideTimer { get; set; }

        /// <summary>
        /// Maximize and removes Interface and start timer for slideshow.
        /// </summary>
        internal static void StartSlideshow()
        {
            if (Pics?.Count == 0)
            {
                return;
            }

            UC.Close_UserControls();

            if (SlideTimer == null)
            {
                SlideTimer = new System.Timers.Timer
                {
                    Interval = Settings.Default.SlideTimer,
                    Enabled = true
                };
                SlideTimer.Elapsed += async delegate { await SlideTimer_Elapsed().ConfigureAwait(false); };
                SlideTimer.Start();
            }
            else
            {
                SlideTimer.Start();
            }

            if (ConfigureWindows.GetMainWindow.WindowState == WindowState.Normal)
            {
                WindowSizing.RenderFullscreen();
            }

            _ = NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_DISPLAY_REQUIRED); // Stop screensaver when running

            HideInterfaceLogic.ShowNavigation(false);
            HideInterfaceLogic.ShowShortcuts(false);
        }

        internal static void StopSlideshow()
        {
            SlideTimer?.Stop();
            SlideTimer = null;

            if (!Settings.Default.Fullscreen)
            {
                WindowSizing.Fullscreen_Restore(false);
            }

            _ = NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS); // Allow screensaver again
        }

        /// <summary>
        /// Timer starts Slideshow Fade animation.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="e"></param>
        private static async Task SlideTimer_Elapsed()
        {
            AnimationHelper.Fade(ConfigureWindows.GetMainWindow.MainImage, TimeSpan.FromSeconds(0.8), TimeSpan.FromSeconds(0), 0, .5);
            await Navigation.GoToNextImage(NavigateTo.Next).ConfigureAwait(false);
            AnimationHelper.Fade(ConfigureWindows.GetMainWindow.MainImage, TimeSpan.FromSeconds(0.7), TimeSpan.FromSeconds(0), .5, 1);
        }
    }
}