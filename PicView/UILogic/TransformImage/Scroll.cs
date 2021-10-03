using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.Tooltip;
using static PicView.UILogic.UC;

namespace PicView.UILogic.TransformImage
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

        internal static readonly Timer AutoScrollTimer = new Timer()
        {
            Interval = 7,
            AutoReset = true,
            Enabled = false
        };

        internal static bool IsAutoScrolling { get; set; }

        /// <summary>
        /// Toggles scroll and displays it with TooltipStle
        /// </summary>
        internal static async Task SetScrollBehaviour(bool scrolling)
        {
            Properties.Settings.Default.ScrollEnabled = scrolling;
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
            {
                ConfigureWindows.GetMainWindow.Scroller.VerticalScrollBarVisibility =
                    scrolling ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
            });

            // TODO fix error when image is from web

            if (ChangeImage.Navigation.Pics != null)
            {
                await TryFitImageAsync().ConfigureAwait(false);
                if (ChangeImage.Navigation.FreshStartup == false)
                {
                    ShowTooltipMessage(scrolling ? Application.Current.Resources["ScrollingEnabled"] : Application.Current.Resources["ScrollingDisabled"]);
                }
            }
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
            Canvas.SetTop(GetAutoScrollSign, AutoScrollOrigin.Value.Y);
            Canvas.SetLeft(GetAutoScrollSign, AutoScrollOrigin.Value.X);
            GetAutoScrollSign.Visibility = Visibility.Visible;
            GetAutoScrollSign.Opacity = 1;
        }

        // Auto scroll

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
        /// Uses timer to scroll vertical up/down every seventh milisecond
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="E"></param>
        internal static async void AutoScrollTimerEvent(object sender, ElapsedEventArgs E)
        {
            // Error checking
            if (AutoScrollPos == null || AutoScrollOrigin == null)
            {
                return;
            }

            // Start in dispatcher because timer is threaded
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(() =>
            {
                if (AutoScrollOrigin.HasValue && AutoScrollPos.HasValue)
                {
                    // Calculate offset by Y coordinate
                    var offset = (AutoScrollPos.Value.Y - AutoScrollOrigin.Value.Y) / 15;

                    if (IsAutoScrolling)
                    {
                        // Tell the scrollviewer to scroll to calculated offset
                        ConfigureWindows.GetMainWindow.Scroller.ScrollToVerticalOffset(
                            ConfigureWindows.GetMainWindow.Scroller.VerticalOffset + offset);
                    }
                }
            });
        }
    }
}