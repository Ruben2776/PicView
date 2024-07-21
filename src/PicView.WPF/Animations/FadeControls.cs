﻿using PicView.Core.Config;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.TransformImage;
using System.Windows.Controls.Primitives;
using static PicView.WPF.UILogic.UC;
using Timer = System.Timers.Timer;

namespace PicView.WPF.Animations;

internal static class FadeControls
{
    /// <summary>
    /// Timer used to hide interface and/or scrollbar
    /// </summary>
    internal static Timer? ActivityTimer { get; set; }

    /// <summary>
    /// Hides/shows interface elements with a fade animation
    /// </summary>
    /// <param name="show"></param>
    internal static void Fade(bool show)
    {
        if (SettingsHelper.Settings.UIProperties.ShowInterface && SettingsHelper.Settings.WindowProperties.Fullscreen == false
            || GetClickArrowRight == null
            || GetClickArrowLeft == null
            || GetX2 == null
            || GetGalleryShortcut == null
            || Scroll.IsAutoScrolling
            || GalleryFunctions.IsGalleryOpen)
        {
            return;
        }

        try
        {
            ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                if (GetCroppingTool is { IsVisible: true })
                {
                    return;
                }

                if (SettingsHelper.Settings.Zoom.ScrollEnabled && ConfigureWindows.GetMainWindow?.Scroller?.ScrollableHeight > 0)
                {
                    ScrollbarFade(show);
                }

                // Don't run, if already being animated || prevent lag
                if (GetX2.Opacity is > 0 and < 1)
                {
                    return;
                }

                if (Scroll.IsAutoScrolling)
                {
                    GetClickArrowLeft.Opacity =
                        GetClickArrowRight.Opacity =
                            GetGalleryShortcut.Opacity =
                                GetX2.Opacity =
                                    GetMinus.Opacity = 0;
                    return;
                }

                var timespan = TimeSpan.FromSeconds(show ? .5 : 1);

                var opacity = show ? 1 : 0;

                AnimationHelper.Fade(GetClickArrowLeft, opacity, timespan);
                AnimationHelper.Fade(GetClickArrowRight, opacity, timespan);
                if (!SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
                {
                    AnimationHelper.Fade(GetGalleryShortcut, opacity, timespan);
                }
                AnimationHelper.Fade(GetX2, opacity, timespan);
                AnimationHelper.Fade(GetMinus, opacity, timespan);
                AnimationHelper.Fade(GetRestoreButton, opacity, timespan);
            });
        }
        catch (Exception)
        {
            // #148
        }
    }

    /// <summary>
    /// Find scrollbar and start fade animation
    /// </summary>
    /// <param name="show"></param>
    internal static void ScrollbarFade(bool show)
    {
        var s = ConfigureWindows.GetMainWindow?.Scroller?.Template?.FindName("PART_VerticalScrollBar",
            ConfigureWindows.GetMainWindow?.Scroller) as ScrollBar;

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