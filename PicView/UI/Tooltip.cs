using PicView.UI.Animations;
using System;
using System.Windows;
using System.Windows.Media.Animation;
using static PicView.UI.UserControls.UC;

namespace PicView.UI
{
    internal static class Tooltip
    {
        /// <summary>
        /// Shows a black tooltip on screen in a given time
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="center">If centered or on bottom</param>
        /// <param name="time">How long until it fades away</param>
        internal static void ShowTooltipMessage(object message, bool center, TimeSpan time)
        {
            if (toolTipMessage == null)
            {
                return;
            }

            toolTipMessage.Visibility = Visibility.Visible;

            if (center)
            {
                toolTipMessage.Margin = new Thickness(0, 0, 0, 0);
                toolTipMessage.VerticalAlignment = VerticalAlignment.Center;
            }
            else
            {
                toolTipMessage.Margin = new Thickness(0, 0, 0, 15);
                toolTipMessage.VerticalAlignment = VerticalAlignment.Bottom;
            }

            toolTipMessage.ToolTipUIText.Text = message.ToString();
            var anim = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            anim.Completed += (s, _) => AnimationHelper.Fade(toolTipMessage, TimeSpan.FromSeconds(1.5), time, 1, 0);

            toolTipMessage.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        /// <summary>
        /// Shows a black tooltip on screen for a small time
        /// </summary>
        /// <param name="message">The message to display</param>
        internal static void ShowTooltipMessage(object message, bool center = false)
        {
            ShowTooltipMessage(message, center, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Hides the Messagebox ToolTipStyle
        /// </summary>
        internal static void CloseToolTipMessage()
        {
            toolTipMessage.Visibility = Visibility.Hidden;
        }
    }
}