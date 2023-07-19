using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;

namespace PicView.SystemIntegration;

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