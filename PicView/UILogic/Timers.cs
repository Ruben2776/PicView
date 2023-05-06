using System.Windows.Threading;
using PicView.PicGallery;
using static PicView.Animations.FadeControls;
using static PicView.UILogic.TransformImage.Scroll;
using Timer = System.Timers.Timer;

namespace PicView.UILogic
{
    internal static class Timers
    {
        // Add timers
        internal static void AddTimers()
        {
            AutoScrollTimer.Elapsed += AutoScrollTimerEvent;

            ActivityTimer = new Timer
            {
                Interval = 6000,
                AutoReset = true,
                Enabled = false
            };
            ActivityTimer.Elapsed += (_, _) => Fade(false);
        }

        internal static void PicGalleryTimerHack()
        {
            Timer timer = new() // Dirty code to make it scroll to selected item after start up
            {
                AutoReset = false,
                Enabled = true,
                Interval = 100
            };
            timer.Elapsed += async delegate
            {
                try
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(GalleryNavigation.ScrollTo));
                }
                catch (Exception)
                {
                    // Suppress task cancellation
                }
            };
        }
    }
}