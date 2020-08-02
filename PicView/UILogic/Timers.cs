using System.Timers;
using static PicView.UILogic.Animations.FadeControls;
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
            ActivityTimer.Elapsed += delegate 
            {
                FadeControlsAsync(false);
            };
        }
    }
}