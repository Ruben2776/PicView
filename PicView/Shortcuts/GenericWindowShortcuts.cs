using PicView.UILogic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PicView.Shortcuts
{
    internal static class GenericWindowShortcuts
    {
        internal static void KeysDown(ScrollViewer? scrollViewer, KeyEventArgs e, Window window)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    window.Hide();
                    ConfigureWindows.GetMainWindow.Focus();
                    break;

                case Key.S:
                case Key.Down:
                    if (scrollViewer == null) { return; }
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 10);
                    break;

                case Key.W:
                case Key.Up:
                    if (scrollViewer == null) { return; }
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 10);
                    break;
            }
        }

        internal static void Window_MouseWheel(ScrollViewer? scrollViewer, MouseWheelEventArgs e)
        {
            if (scrollViewer == null) { return; }

            if (e.Delta > 0)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 10);
            }
            else
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 10);
            }
        }
    }
}