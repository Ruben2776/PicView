using System;
using static PicView.Fields;

namespace PicView
{
    internal static class FadeControls
    {
        /// <summary>
        /// Hides/shows interface elements with a fade animation
        /// </summary>
        /// <param name="show"></param>
        internal static async void FadeControlsAsync(bool show, double time = .5)
        {
            await mainWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (!Properties.Settings.Default.ShowInterface || Slidetimer.Enabled)
                {
                    if (clickArrowRight != null && clickArrowLeft != null && x2 != null && galleryShortcut != null)
                    {
                        var fadeTo = show ? 1 : 0;
                        var timespan = TimeSpan.FromSeconds(time);

                        AnimationHelper.Fade(clickArrowLeft, fadeTo, timespan);
                        AnimationHelper.Fade(clickArrowRight, fadeTo, timespan);
                        AnimationHelper.Fade(x2, fadeTo, timespan);
                        AnimationHelper.Fade(minus, fadeTo, timespan);
                        AnimationHelper.Fade(galleryShortcut, fadeTo, timespan);
                    }
                }

                ScrollbarFade(show);
            }));
        }

        /// <summary>
        /// Find scrollbar and start fade animation
        /// </summary>
        /// <param name="show"></param>
        internal static void ScrollbarFade(bool show)
        {
            var s = mainWindow.Scroller.Template.FindName("PART_VerticalScrollBar", mainWindow.Scroller) as System.Windows.Controls.Primitives.ScrollBar;

            if (show)
            {
                AnimationHelper.Fade(s, 1, TimeSpan.FromSeconds(.7));
            }
            else
            {
                AnimationHelper.Fade(s, 0, TimeSpan.FromSeconds(1));
            }
        }

        //internal static async void FadeCursor(double time = .5)
        //{
        //    /// Might cause unnecessary cpu usage? Need to check
        //    await mainWindow.Dispatcher.BeginInvoke((Action)(() =>
        //    {
        //        AnimationHelper.Fade(clickArrowLeft, 0, TimeSpan.FromSeconds(.5));
        //        AnimationHelper.Fade(clickArrowRight, 0, TimeSpan.FromSeconds(.5));
        //        AnimationHelper.Fade(x2, 0, TimeSpan.FromSeconds(.5));
        //        Mouse.OverrideCursor = Cursors.None;

        //        MouseIdleTimer.Stop();
        //    }));
        //}
    }
}
