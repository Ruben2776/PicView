using PicView.Animations;
using PicView.ChangeImage;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic.Sizing;
using System.Windows;
using PicView.PicGallery;
using static PicView.ChangeImage.Navigation;
using Timer = System.Timers.Timer;

namespace PicView.UILogic;

internal static class Slideshow
{
    /// <summary>
    /// Timer used for slideshow
    /// </summary>
    internal static Timer? SlideTimer { get; set; }

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
            SlideTimer = new Timer
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
            WindowSizing.Fullscreen_Restore(true);
        }

        _ = NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_DISPLAY_REQUIRED); // Stop screensaver when running

        HideInterfaceLogic.IsNavigationShown(false);
        HideInterfaceLogic.IsShortcutsShown(false);
        if (Settings.Default.IsBottomGalleryShown)
        {
            GalleryToggle.CloseBottomGallery();
        }
    }

    internal static void StopSlideshow()
    {
        SlideTimer?.Stop();
        SlideTimer = null;

        if (!Settings.Default.Fullscreen)
        {
            WindowSizing.Fullscreen_Restore(gotoFullscreen: false);
        }

        if (Settings.Default.IsBottomGalleryShown)
        {
            _ = GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
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
        await GoToNextImage(NavigateTo.Next).ConfigureAwait(false);
        AnimationHelper.Fade(ConfigureWindows.GetMainWindow.MainImage, TimeSpan.FromSeconds(0.7), TimeSpan.FromSeconds(0), .5, 1);
    }
}