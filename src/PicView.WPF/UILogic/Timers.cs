using static PicView.WPF.Animations.FadeControls;
using static PicView.WPF.UILogic.TransformImage.Scroll;
using Timer = System.Timers.Timer;

namespace PicView.WPF.UILogic
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