using PicView.UILogic.Animations;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using static PicView.UILogic.UC;

namespace PicView.UILogic
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
            if (GetToolTipMessage == null)
            {
                return;
            }

            GetToolTipMessage.Visibility = Visibility.Visible;

            if (center)
            {
                GetToolTipMessage.Margin = new Thickness(0, 0, 0, 0);
                GetToolTipMessage.VerticalAlignment = VerticalAlignment.Center;
            }
            else
            {
                GetToolTipMessage.Margin = new Thickness(0, 0, 0, 15);
                GetToolTipMessage.VerticalAlignment = VerticalAlignment.Bottom;
            }

            GetToolTipMessage.ToolTipUIText.Text = message.ToString();
            var anim = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            anim.Completed += (s, _) => AnimationHelper.Fade(GetToolTipMessage, TimeSpan.FromSeconds(1.5), time, 1, 0);

            GetToolTipMessage.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        /// <summary>
        /// Shows a black tooltip on screen for a small time
        /// </summary>
        /// <param name="message">The message to display</param>
        internal static async Task ShowTooltipMessage(object message, bool center = false)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
            {
                ShowTooltipMessage(message, center, TimeSpan.FromSeconds(1));
            }));
        }

        /// <summary>
        /// Hides the Messagebox ToolTipStyle
        /// </summary>
        internal static void CloseToolTipMessage()
        {
            if (GetToolTipMessage.CheckAccess())
            {
                GetToolTipMessage.Visibility = Visibility.Hidden;
            }
        }
    }
}