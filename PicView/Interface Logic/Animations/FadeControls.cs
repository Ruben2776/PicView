using System;
using static PicView.Fields;
using static PicView.UC;

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
                if (!Properties.Settings.Default.ShowInterface)
                {
                    if (clickArrowRight != null && clickArrowLeft != null && x2 != null && galleryShortcut != null)
                    {
                        int fadeTo;
                        TimeSpan timespan;

                        if (AutoScrolling)
                        {
                            fadeTo = 0;
                            timespan = TimeSpan.FromSeconds(0);
                        }
                        else
                        {
                            fadeTo = show ? 1 : 0;
                            timespan = TimeSpan.FromSeconds(time);
                        }

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
    }
}
