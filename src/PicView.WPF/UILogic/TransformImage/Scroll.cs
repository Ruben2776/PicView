using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PicView.WPF.ChangeImage;
using PicView.Core.Config;
using PicView.WPF.UILogic.Loading;
using static PicView.WPF.UILogic.Sizing.ScaleImage;
using static PicView.WPF.UILogic.Tooltip;
using static PicView.WPF.UILogic.UC;
using Timer = System.Timers.Timer;

namespace PicView.WPF.UILogic.TransformImage
{
    internal static class Scroll
    {
        /// <summary>
        /// Starting point of AutoScroll
        /// </summary>
        internal static Point? AutoScrollOrigin { get; set; }

        /// <summary>
        /// Current point of AutoScroll
        /// </summary>
        internal static Point? AutoScrollPos { get; set; }

        internal static readonly Timer AutoScrollTimer = new()
        {
            Interval = 7,
            AutoReset = true,
            Enabled = false
        };

        internal static bool IsAutoScrolling { get; private set; }

        /// <summary>
        /// Toggles scroll and displays it with TooltipStyle
        /// </summary>
        internal static void SetScrollBehaviour(bool scrolling)
        {
            if (SettingsHelper.Settings.WindowProperties.Fullscreen)
            {
                return;
            }

            SettingsHelper.Settings.Zoom.ScrollEnabled = scrolling;
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
            {
                ConfigureWindows.GetMainWindow.Scroller.VerticalScrollBarVisibility =
                    scrolling ? ScrollBarVisibility.Visible : ScrollBarVisibility.Disabled;
            });
            if (Navigation.Pics == null) return;
            TryFitImage();
            ShowTooltipMessage(scrolling
                ? Application.Current.Resources["ScrollingEnabled"]
                : Application.Current.Resources["ScrollingDisabled"]);
        }

        // AutoScrollSign

        internal static void HideAutoScrollSign()
        {
            GetAutoScrollSign.Visibility = Visibility.Collapsed;
            GetAutoScrollSign.Opacity = 0;
        }

        internal static void ShowAutoScrollSign()
        {
            if (AutoScrollOrigin.HasValue == false)
            {
                return;
            }

            if (GetAutoScrollSign is null)
            {
                LoadControls.LoadAutoScrollSign();
            }

            Canvas.SetTop(GetAutoScrollSign, AutoScrollOrigin.Value.Y);
            Canvas.SetLeft(GetAutoScrollSign, AutoScrollOrigin.Value.X);
            GetAutoScrollSign.Visibility = Visibility.Visible;
            GetAutoScrollSign.Opacity = 1;
        }

        /// <summary>
        /// Starts the auto scroll feature and shows the sign on the ui
        /// </summary>
        /// <param name="e"></param>
        internal static void StartAutoScroll(MouseButtonEventArgs e)
        {
            // Don't scroll if not scrollable
            if (ConfigureWindows.GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Collapsed)
            {
                return;
            }

            IsAutoScrolling = true;
            AutoScrollOrigin = e.GetPosition(ConfigureWindows.GetMainWindow.Scroller);

            ShowAutoScrollSign();
        }

        /// <summary>
        /// Stop auto scroll feature and remove sign from the ui
        /// </summary>
        internal static void StopAutoScroll()
        {
            AutoScrollTimer.Stop();
            AutoScrollTimer.Enabled = false;
            IsAutoScrolling = false;
            AutoScrollOrigin = null;
            HideAutoScrollSign();
        }

        /// <summary>
        /// Uses timer to scroll vertical up/down every seventh millisecond
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void AutoScrollTimerEvent(object sender, ElapsedEventArgs e)
        {
            // Error checking
            if (AutoScrollPos == null || AutoScrollOrigin == null)
            {
                return;
            }

            // Start in dispatcher because timer is threaded
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                if (!AutoScrollOrigin.HasValue || !AutoScrollPos.HasValue) return;
                // Calculate offset by Y coordinate
                var offset = (AutoScrollPos.Value.Y - AutoScrollOrigin.Value.Y) / 15;

                if (IsAutoScrolling)
                {
                    // Tell the scrollviewer to scroll to calculated offset
                    ConfigureWindows.GetMainWindow.Scroller.ScrollToVerticalOffset(
                        ConfigureWindows.GetMainWindow.Scroller.VerticalOffset + offset);
                }
            });
        }
    }
}