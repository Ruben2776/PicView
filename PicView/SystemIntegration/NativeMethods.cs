using Microsoft.Win32;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PicView.SystemIntegration
{
    //https://msdn.microsoft.com/en-us/library/ms182161.aspx
    internal static partial class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point : IEquatable<Win32Point>
        {
            public int X;
            public int Y;

            public bool Equals(Win32Point other)
            {
                return X == other.X && Y == other.Y;
            }

            public override bool Equals(object? obj)
            {
                return obj is Win32Point && Equals((Win32Point)obj);
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }

        // Alphanumeric sort
        [LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int StrCmpLogicalW(string x, string y);

        // Change cursor position
        [LibraryImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetCursorPos(int x, int y);

        // Used to check for wallpaper support
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam,
        string pvParam, uint fWinIni);

        #region Disable Screensaver and Power options

        internal const uint ES_CONTINUOUS = 0x80000000;

        internal const uint ES_SYSTEM_REQUIRED = 0x00000001;
        internal const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SetThreadExecutionState([In] uint esFlags);

        #endregion Disable Screensaver and Power options

        #region windproc

        // https://stackoverflow.com/a/60938929/13646636
        private const int WM_SIZING = 0x214;

        // Message list = https://wiki.winehq.org/List_Of_Windows_Messages

        /// Supress warnings about unused parameters, because they are required by OS.
        /// Executes when user manually resized window
        public static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (Settings.Default.AutoFitWindow || GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                return IntPtr.Zero;
            }

            if (msg == WM_SIZING || msg == 0x0005)
            {
                var w = ConfigureWindows.GetMainWindow;
                if (w == null) { return IntPtr.Zero; }

                if (w.MainImage.Source == null)
                {
                    if (UC.GetStartUpUC is not null)
                    {
                        UC.GetStartUpUC.ResponsiveSize(w.Width);
                    }
                }
                else
                {
                    if (w.WindowState == WindowState.Maximized)
                    {
                        WindowSizing.Restore_From_Move();
                    }
                    if (w.MainImage.Source == null) { return IntPtr.Zero; }

                    // Resize gallery
                    if (UC.GetPicGallery != null && GalleryFunctions.IsHorizontalOpen)
                    {
                        UC.GetPicGallery.Width = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth;
                        UC.GetPicGallery.Height = ConfigureWindows.GetMainWindow.ParentContainer.ActualHeight;
                    }

                    ScaleImage.FitImage(w.MainImage.Source.Width, w.MainImage.Source.Height);
                }
            }

            return IntPtr.Zero;
        }

        #endregion windproc

        #region GetPixelColor

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        // https://stackoverflow.com/a/24759418/13646636

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowDC(IntPtr window);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern uint GetPixel(IntPtr dc, int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr window, IntPtr dc);

        public static Color GetColorAt(int x, int y)
        {
            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);
            int a = (int)GetPixel(dc, x, y);
            _ = ReleaseDC(desk, dc);
            return Color.FromArgb(255, (a >> 0) & 0xff, (a >> 8) & 0xff, (a >> 16) & 0xff);
        }

        #endregion GetPixelColor

    }
}