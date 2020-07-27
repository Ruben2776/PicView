using PicView.Library;
using PicView.UILogic;
using PicView.UILogic.Loading;
using PicView.UILogic.TransformImage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.SystemIntegration.NativeMethods;

namespace PicView.Editing
{
    internal static class Color_Picking
    {
        internal static bool IsRunning { get; set; }

        internal static void Start()
        {
            IsRunning = true;

            if (UC.GetColorPicker == null || !LoadWindows.GetMainWindow.topLayer.Children.Contains(UC.GetColorPicker))
            {
                LoadControls.LoadColorPicker();
            }

            // Set cursor for coloc picking
            LoadWindows.GetMainWindow.Cursor = Cursors.Pen;
        }

        internal static void StartRunning()
        {
            // Get values
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            var c = GetColorAt(w32Mouse.X, w32Mouse.Y);

            // Set color values to usercontrol
            UC.GetColorPicker.HexCodePresenter.Content = Utilities.HexConverter(c);
            UC.GetColorPicker.RectangleColorPresenter.Fill =
            UC.GetColorPicker.MainColorPresenter.Fill = new SolidColorBrush
            {
                Color = Color.FromRgb(
                        c.R, c.G, c.B
                    )
            };

            // Set to follow cursor
            Scroll.AutoScrollOrigin = Mouse.GetPosition(LoadWindows.GetMainWindow);
            Canvas.SetTop(UC.GetColorPicker, Scroll.AutoScrollOrigin.Value.Y);
            Canvas.SetLeft(UC.GetColorPicker, Scroll.AutoScrollOrigin.Value.X);
        }

        internal static void StopRunning(bool addValue)
        {
            // Reset cursor from coloc picking
            LoadWindows.GetMainWindow.Cursor = Cursors.Arrow;

            if (UC.GetColorPicker != null)
            {
                if (addValue)
                {
                    var x = UC.GetColorPicker.HexCodePresenter.Content.ToString();
                    Clipboard.SetText(x);
                    Tooltip.ShowTooltipMessage(x + " " + Application.Current.Resources["AddedToClipboard"]);
                }

                LoadWindows.GetMainWindow.topLayer.Children.Remove(UC.GetColorPicker);
            }

            IsRunning = false;

            LoadWindows.GetMainWindow.Focus();
        }
    }
}