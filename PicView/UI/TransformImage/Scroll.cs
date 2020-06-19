using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PicView.Library.Fields;
using static PicView.UI.Sizing.ScaleImage;
using static PicView.UI.Tooltip;
using static PicView.UI.UserControls.UC;

namespace PicView.UI.TransformImage
{
    internal static class Scroll
    {
        /// <summary>
        /// Toggles scroll and displays it with TooltipStle
        /// </summary>
        internal static bool IsScrollEnabled
        {
            get { return Properties.Settings.Default.ScrollEnabled; }
            set
            {
                Properties.Settings.Default.ScrollEnabled = value;
                mainWindow.Scroller.VerticalScrollBarVisibility = value ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
                if (Pics != null)
                {
                    TryFitImage();
                    ShowTooltipMessage(value ? "Scrolling enabled" : "Scrolling disabled");
                }
            }
        }

        // AutoScrollSign

        internal static void HideAutoScrollSign()
        {
            autoScrollSign.Visibility = Visibility.Collapsed;
            autoScrollSign.Opacity = 0;
        }

        internal static void ShowAutoScrollSign()
        {
            Canvas.SetTop(autoScrollSign, autoScrollOrigin.Value.Y);
            Canvas.SetLeft(autoScrollSign, autoScrollOrigin.Value.X);
            autoScrollSign.Visibility = Visibility.Visible;
            autoScrollSign.Opacity = 1;
        }

        // Auto scroll

        /// <summary>
        /// Starts the auto scroll feature and shows the sign on the ui
        /// </summary>
        /// <param name="e"></param>
        internal static void StartAutoScroll(MouseButtonEventArgs e)
        {
            // Don't scroll if not scrollable
            if (mainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Collapsed)
            {
                return;
            }

            AutoScrolling = true;
            autoScrollOrigin = e.GetPosition(mainWindow.Scroller);

            ShowAutoScrollSign();
        }

        /// <summary>
        /// Stop auto scroll feature and remove sign from the ui
        /// </summary>
        internal static void StopAutoScroll()
        {
            autoScrollTimer.Stop();
            //window.ReleaseMouseCapture();
            autoScrollTimer.Enabled = false;
            AutoScrolling = false;
            autoScrollOrigin = null;
            HideAutoScrollSign();
        }

        /// <summary>
        /// Uses timer to scroll vertical up/down every seventh milisecond
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="E"></param>
        internal static async void AutoScrollTimerEvent(object sender, System.Timers.ElapsedEventArgs E)
        {
            // Error checking
            if (autoScrollPos == null || autoScrollOrigin == null)
            {
                return;
            }

            // Start in dispatcher because timer is threaded
            await mainWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (autoScrollOrigin.HasValue)
                {
                    // Calculate offset by Y coordinate
                    var offset = (autoScrollPos.Y - autoScrollOrigin.Value.Y) / 15;

                    //ToolTipStyle("pos = " + autoScrollPos.Y.ToString() + " origin = " + autoScrollOrigin.Value.Y.ToString()
                    //    + Environment.NewLine + "offset = " + offset, false);

                    if (AutoScrolling)
                    {
                        // Tell the scrollviewer to scroll to calculated offset
                        mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset + offset);
                    }
                }
            }));
        }
    }
}
