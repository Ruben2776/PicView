using PicView.UILogic.TransformImage;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PicView.Shortcuts
{
    internal static class GenericWindowShortcuts
    {
        internal static void KeysDown(ScrollViewer scrollViewer, KeyEventArgs e, Window window)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    window.Hide();
                    UILogic.Loading.LoadWindows.GetMainWindow.Focus();
                    break;

                case Key.S:
                case Key.Down:
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 10);
                    break;

                case Key.W:
                case Key.U:
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 10);
                    break;

                case Key.Q:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Environment.Exit(0);
                    }
                    break;
            }
        }

        internal static void Window_MouseWheel(ScrollViewer scrollViewer, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - ZoomLogic.zoomSpeed);
            }
            else if (e.Delta < 0)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + ZoomLogic.zoomSpeed);
            }
        }
    }
}
