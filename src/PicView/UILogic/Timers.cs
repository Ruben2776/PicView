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
    }
}