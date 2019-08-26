using System;
using System.Timers;
using System.Windows.Input;
using System.Windows.Threading;
using static PicView.Fields;
using static PicView.Interface;
using static PicView.Navigation;
using static PicView.Resize_and_Zoom;

namespace PicView
{
    class Timers
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

            Slidetimer = new Timer()
            {
                Interval = Properties.Settings.Default.Slidetimeren,
                Enabled = false
            };
            Slidetimer.Elapsed += SlideShow.SlideTimer_Elapsed;

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

        #endregion
    }
}
