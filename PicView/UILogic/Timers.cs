using System;
using System.Timers;
using static PicView.Animations.FadeControls;
using static PicView.UILogic.TransformImage.Scroll;

namespace PicView.UILogic
{
    internal static class Timers
    {
        // Add timers
        internal static void AddTimers()
        {
            AutoScrollTimer.Elapsed += AutoScrollTimerEvent;

            ActivityTimer = new Timer()
            {
                Interval = 6000,
                AutoReset = true,
                Enabled = false
            };
            ActivityTimer.Elapsed += (s, e) => FadeAsync(false);
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
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
                {
                    PicGallery.GalleryNavigation.ScrollTo();
                }));
            };
        }
    }
}