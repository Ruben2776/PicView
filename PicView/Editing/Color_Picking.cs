using PicView.Library;
using PicView.SystemIntegration;
using PicView.UI;
using PicView.UI.Loading;
using PicView.UI.TransformImage;
using PicView.UI.UserControls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.SystemIntegration.NativeMethods;

namespace PicView.Editing
{
    internal static class Color_Picking
    {
        internal static void StartRunning()
        {
            if (UC.GetColorPicker == null)
            {
                LoadControls.LoadColorPicker();
            }

            UC.GetColorPicker.Opacity = 1;

            // Set cursor for coloc picking
            Fields.TheMainWindow.Cursor = Cursors.Pen;

            // Get values
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            var c = GetColorAt(w32Mouse.X, w32Mouse.Y);

            // Set color values to usercontrol
            UC.GetColorPicker.HexCodePresenter.Content = Utilities.HexConverter(c);
            UC.GetColorPicker.RectangleColorPresenter.Fill =
            UC.GetColorPicker.MainColorPresenter.Fill = new SolidColorBrush {
                    Color = Color.FromRgb(
                        c.R, c.G, c.B
                    )
            };

            // Set to follow cursor
            Scroll.AutoScrollOrigin = Mouse.GetPosition(Fields.TheMainWindow);
            Canvas.SetTop(UC.GetColorPicker, Scroll.AutoScrollOrigin.Value.Y);
            Canvas.SetLeft(UC.GetColorPicker, Scroll.AutoScrollOrigin.Value.X);
        }

        internal static void StopRunning(bool setText)
        {
            // Reset cursor from coloc picking
            Fields.TheMainWindow.Cursor = Cursors.Arrow;

            // Hide it
            UC.GetColorPicker.Opacity = 0;

            // Set values, if needed
            if (setText)
            {
                var x = UC.GetColorPicker.HexCodePresenter.Content.ToString();
                Clipboard.SetText(x);
                Tooltip.ShowTooltipMessage($"Copied {x}"); // TODO add translation
            }
            
        }
    }
}
