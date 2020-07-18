using System.Timers;
using static PicView.Library.Fields;
using static PicView.UILogic.Animations.FadeControls;
using static PicView.UILogic.TransformImage.Scroll;

namespace PicView.Library.Resources
{
    internal static class Timers
    {
        #region Add Timers

        // Add timers
        internal static void AddTimers()
        {
            AutoScrollTimer = new Timer()
            {
                Interval = 7,
                AutoReset = true,
                Enabled = false
            };
            AutoScrollTimer.Elapsed += AutoScrollTimerEvent;

            ActivityTimer = new Timer()
            {
                Interval = 6000,
                AutoReset = true,
                Enabled = false
            };
            ActivityTimer.Elapsed += delegate { FadeControlsAsync(false); };

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