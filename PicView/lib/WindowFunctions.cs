using System.Windows;

namespace PicView.lib
{
    internal static class WindowFunctions
    {
        internal static void Close(Window window)
        {
            SystemCommands.CloseWindow(window);
        }

        internal static void Restore(Window window)
        {
            SystemCommands.RestoreWindow(window);
        }

        internal static void Maximize(Window window)
        {
            if (window.WindowState == WindowState.Normal)
                SystemCommands.MaximizeWindow(window);
            else if (window.WindowState == WindowState.Maximized)
                Restore(window);
        }

        internal static void Minimize(Window window)
        {
            SystemCommands.MinimizeWindow(window);
        }    

    }
}
