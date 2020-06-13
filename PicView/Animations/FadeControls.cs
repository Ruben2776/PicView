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
                        TimeSpan timespan = TimeSpan.FromSeconds(time);

                        if (!show)
                        {
                            AnimationHelper.Fade(clickArrowLeft, 0, timespan);
                            AnimationHelper.Fade(clickArrowRight, 0, timespan);
                            AnimationHelper.Fade(galleryShortcut, 0, timespan);
                            AnimationHelper.Fade(x2, 0, timespan);
                            AnimationHelper.Fade(minus, 0, timespan);
                            return;
                        }
                        else if (AutoScrolling)
                        {
                            clickArrowLeft.Opacity =
                            clickArrowRight.Opacity =
                            galleryShortcut.Opacity =
                            x2.Opacity =
                            minus.Opacity = 0;
                            return;
                        }

                        var pos = Utilities.GetMousePos(mainWindow);

                        //Tooltip.ShowTooltipMessage($"x = {pos.X} Y = {pos.Y}");

                        if (pos.X < 1100 && pos.Y < 850)
                        {
                            AnimationHelper.Fade(clickArrowLeft, 1, timespan);
                        }
                        else
                        {
                            AnimationHelper.Fade(clickArrowLeft, 0, timespan);
                        }

                        if (pos.X > 1400 && pos.Y < 1000)
                        {
                            AnimationHelper.Fade(clickArrowRight, 1, timespan);
                        }
                        else
                        {
                            AnimationHelper.Fade(clickArrowRight, 0, timespan);
                        }

                        if (pos.X > 1550 && pos.Y > 1100)
                        {
                            AnimationHelper.Fade(galleryShortcut, 1, timespan);
                        }
                        else
                        {
                            AnimationHelper.Fade(galleryShortcut, 0, timespan);
                        }

                        if (pos.X > 1400 && pos.Y < 500)
                        {
                            AnimationHelper.Fade(x2, 1, timespan);
                            AnimationHelper.Fade(minus, 1, timespan);
                        }
                        else
                        {
                            AnimationHelper.Fade(x2, 0, timespan);
                            AnimationHelper.Fade(minus, 0, timespan);
                        }

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
