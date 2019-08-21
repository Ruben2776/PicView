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
                Interval = 1500,
                AutoReset = true,
                Enabled = false
            };
            activityTimer.Elapsed += ActivityTimer_Elapsed;

            fastPicTimer = new Timer()
            {
                Interval = 1,
                Enabled = false
            };
            fastPicTimer.Elapsed += FastPic;

            Slidetimer = new Timer()
            {
                Interval = Properties.Settings.Default.Slidetimeren,
                Enabled = false
            };
            Slidetimer.Elapsed += SlideShow.SlideTimer_Elapsed;

            HideCursorTimer = new Timer()
            {
                Interval = 2500,
                Enabled = false
            };
            HideCursorTimer.Elapsed += HideCursorTimer_Elapsed;

            MouseIdleTimer = new Timer()
            {
                Interval = 2500,
                Enabled = false
            };
            MouseIdleTimer.Elapsed += MouseIdleTimer_Elapsed;
        }

        #endregion

        /// <summary>
        /// Timer starts FadeControlsAsync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void ActivityTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            FadeControlsAsync(false);
        }

        /// <summary>
        /// Timer to show/hide cursor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void HideCursorTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new System.Threading.ThreadStart(() =>
            {
                AnimationHelper.Fade(clickArrowLeft, 0, TimeSpan.FromSeconds(.5));
                AnimationHelper.Fade(clickArrowRight, 0, TimeSpan.FromSeconds(.5));
                AnimationHelper.Fade(x2, 0, TimeSpan.FromSeconds(.5));
                Mouse.OverrideCursor = Cursors.None;
            }));
            MouseIdleTimer.Stop();
        }

        /// <summary>
        /// Timer to check if Mouse is idle in Slideshow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MouseIdleTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            HideCursorTimer.Start();
        }
    }
}
