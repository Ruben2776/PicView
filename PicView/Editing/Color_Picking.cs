using PicView.UILogic;
using PicView.UILogic.Loading;
using PicView.UILogic.TransformImage;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static PicView.SystemIntegration.NativeMethods;
using Color = System.Drawing.Color;

namespace PicView.Editing
{
    internal static class Color_Picking
    {
        internal static bool IsRunning { get; set; }

        internal static void Start()
        {
            IsRunning = true;

            if (UC.GetColorPicker == null || !ConfigureWindows.GetMainWindow.topLayer.Children.Contains(UC.GetColorPicker))
            {
                LoadControls.LoadColorPicker();
            }

            // Set cursor for coloc picking
            ConfigureWindows.GetMainWindow.Cursor = Cursors.Pen;
        }

        internal static void StartRunning()
        {
            // Get values
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            var c = GetColorAt(w32Mouse.X, w32Mouse.Y);

            // Set color values to usercontrol
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                UC.GetColorPicker.HexCodePresenter.Content = RGBConverter(c);
            }
            else
            {
                UC.GetColorPicker.HexCodePresenter.Content = HexConverter(c); 
            }
            
            UC.GetColorPicker.RectangleColorPresenter.Fill =
            UC.GetColorPicker.MainColorPresenter.Fill = new SolidColorBrush
            {
                Color = System.Windows.Media.Color.FromRgb(
                        c.R, c.G, c.B
                    )
            };

            // Set to follow cursor
            Scroll.AutoScrollOrigin = Mouse.GetPosition(ConfigureWindows.GetMainWindow);
            Canvas.SetTop(UC.GetColorPicker, Scroll.AutoScrollOrigin.Value.Y);
            Canvas.SetLeft(UC.GetColorPicker, Scroll.AutoScrollOrigin.Value.X);
        }

        internal static void StopRunning(bool addValue)
        {
            // Reset cursor from coloc picking
            ConfigureWindows.GetMainWindow.Cursor = Cursors.Arrow;

            if (UC.GetColorPicker != null)
            {
                if (addValue)
                {
                    if (UC.GetColorPicker.HexCodePresenter.Content == null)
                    {
                        IsRunning = false;
                        return;
                    }
                    string clipboardContent = UC.GetColorPicker.HexCodePresenter.Content.ToString() ?? "";
                    if (clipboardContent.StartsWith("RGB "))
                    {
                        clipboardContent = clipboardContent.Remove(0, 4);
                    }
                    Clipboard.SetText(clipboardContent);
                    Tooltip.ShowTooltipMessage(clipboardContent + " " + Application.Current.Resources["AddedToClipboard"]);
                }
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
                {
                    ConfigureWindows.GetMainWindow.topLayer.Children.Remove(UC.GetColorPicker);
                    ConfigureWindows.GetMainWindow.Focus();
                });
            }

            IsRunning = false;
        }

        internal static string HexConverter(Color c)
        {
            return "#" +
                c.R.ToString("X2", CultureInfo.InvariantCulture) +
                c.G.ToString("X2", CultureInfo.InvariantCulture) +
                c.B.ToString("X2", CultureInfo.InvariantCulture);
        }

        internal static string RGBConverter(Color c)
        {
            return $"RGB {c.R}, {c.G}, {c.B}";
        }
    }
}