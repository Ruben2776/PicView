using System.Timers;
using static PicView.Library.Fields;
using static PicView.UI.Animations.FadeControls;
using static PicView.UI.TransformImage.Scroll;

namespace PicView.Library.Resources
{
    internal static class Timers
    {
        #region Add Timers

        // Add timers
        internal static void AddTimers()
        {
            autoScrollTimer = new Timer()
            {
                Interval = 7,
                AutoReset = true,
                Enabled = false
            };
            autoScrollTimer.Elapsed += AutoScrollTimerEvent;

            activityTimer = new Timer()
            {
                Interval = 6000,
                AutoReset = true,
                Enabled = false
            };
            activityTimer.Elapsed += delegate { FadeControlsAsync(false); };

            //fastPicTimer = new Timer()
            //{
            //    Interval = 1,
            //    Enabled = false
            //};
            //fastPicTimer.Elapsed += FastPic;

            //HideCursorTimer = new Timer()
            //{
            //    Interval = 2500,
            //    Enabled = false
            //};
            //HideCursorTimer.Elapsed += delegate { FadeCursor(false); };

            //MouseIdleTimer = new Timer()
            //{
            //    Interval = 2500,
            //    Enabled = false
            //};
            //MouseIdleTimer.Elapsed += delegate { HideCursorTimer.Start(); };
        }

        #endregion Add Timers
    }
}