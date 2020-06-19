using PicView.Library;
using System;
using static PicView.Library.Fields;
using static PicView.UI.UserControls.UC;

namespace PicView.UI.Animations
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
                if (Properties.Settings.Default.ScrollEnabled && mainWindow.Scroller.ScrollableHeight > 0)
                {
                    ScrollbarFade(show);
                }

                if (Properties.Settings.Default.ShowInterface
                    || clickArrowRight == null
                    || clickArrowLeft == null
                    || x2 == null
                    || galleryShortcut == null)
                { return; }

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

                var pos = Utilities.GetMousePos(mainWindow.bg);

                //Tooltip.ShowTooltipMessage($"x = {pos.X} Y = {pos.Y}");

                if (pos.X < 1100 && pos.Y < 850)
                {
                    AnimationHelper.Fade(clickArrowLeft, 1, timespan);
                }
                else if (clickArrowLeft.Opacity == 1)
                {
                    AnimationHelper.Fade(clickArrowLeft, 0, timespan);
                }

                if (pos.X > 1400 && pos.Y > 550)
                {
                    AnimationHelper.Fade(clickArrowRight, 1, timespan);
                }
                else if (clickArrowRight.Opacity == 1)
                {
                    AnimationHelper.Fade(clickArrowRight, 0, timespan);
                }

                if (pos.X > 1450 && pos.Y > 980)
                {
                    AnimationHelper.Fade(galleryShortcut, 1, timespan);
                }
                else if (galleryShortcut.Opacity == 1)
                {
                    AnimationHelper.Fade(galleryShortcut, 0, timespan);
                }

                if (pos.X > 1400 && pos.Y < 400)
                {
                    AnimationHelper.Fade(x2, 1, timespan);
                    AnimationHelper.Fade(minus, 1, timespan);
                }
                else if (x2.Opacity == 1)
                {
                    AnimationHelper.Fade(x2, 0, timespan);
                    AnimationHelper.Fade(minus, 0, timespan);

                }
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
