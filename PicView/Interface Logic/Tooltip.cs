using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static PicView.Fields;
using static PicView.FileFunctions;
using static PicView.Resize_and_Zoom;
using static PicView.FadeControls;

namespace PicView
{
    internal static class Tooltip
    {
        /// <summary>
        /// Shows a black tooltip on screen in a given time
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="center">If centered or on bottom</param>
        /// <param name="time">How long until it fades away</param>
        internal static void ToolTipStyle(object message, bool center, TimeSpan time)
        {
            sexyToolTip.Visibility = Visibility.Visible;

            if (center)
            {
                sexyToolTip.Margin = new Thickness(0, 0, 0, 0);
                sexyToolTip.VerticalAlignment = VerticalAlignment.Center;
            }
            else
            {
                sexyToolTip.Margin = new Thickness(0, 0, 0, 15);
                sexyToolTip.VerticalAlignment = VerticalAlignment.Bottom;
            }

            sexyToolTip.SexyToolTipText.Text = message.ToString();
            var anim = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            anim.Completed += (s, _) => AnimationHelper.Fade(sexyToolTip, TimeSpan.FromSeconds(1.5), time, 1, 0);

            sexyToolTip.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        /// <summary>
        /// Shows a black tooltip on screen for a small time
        /// </summary>
        /// <param name="message">The message to display</param>
        internal static void ToolTipStyle(object message, bool center = false)
        {
            ToolTipStyle(message, center, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Hides the Messagebox ToolTipStyle
        /// </summary>
        internal static void CloseToolTipStyle()
        {
            sexyToolTip.Visibility = Visibility.Hidden;
        }

    }
}
