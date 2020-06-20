using PicView.UI.Sizing;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace PicView.SystemIntegration
{
    //https://msdn.microsoft.com/en-us/library/ms182161.aspx
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        // Alphanumeric sort
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int StrCmpLogicalW(string x, string y);

        // Change cursor position
        [DllImport("User32.dll")]
        internal static extern bool SetCursorPos(int x, int y);

        // Used to check for wallpaper support
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam,
        string pvParam, uint fWinIni);

        // file properties
        //http://stackoverflow.com/a/1936957

        private const int SW_SHOW = 5;
        private const uint SEE_MASK_INVOKEIDLIST = 12;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        internal static bool ShowFileProperties(string Filename)
        {
            var info = new SHELLEXECUTEINFO();
            info.cbSize = Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpParameters = "details";
            info.lpFile = Filename;
            info.nShow = SW_SHOW;
            info.fMask = SEE_MASK_INVOKEIDLIST;
            return ShellExecuteEx(ref info);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;

            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;

            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;

            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;

            [MarshalAs(UnmanagedType.LPTStr)]
            public readonly string lpDirectory;

            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;

            [MarshalAs(UnmanagedType.LPTStr)]
            public readonly string lpClass;

            public IntPtr hkeyClass;
            public readonly uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        //[DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        //public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        // Remove from Alt + tab
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        internal const int GWL_EX_STYLE = -20;
        internal const int WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080;

        // Disable Screensaver and Power options.
        internal const uint ES_CONTINUOUS = 0x80000000;

        internal const uint ES_SYSTEM_REQUIRED = 0x00000001;
        internal const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SetThreadExecutionState([In] uint esFlags);

        // https://stackoverflow.com/a/60938929/13646636
        private const int WM_SIZING = 0x214;

        private const int WM_EXITSIZEMOVE = 0x232;
        private static bool WindowWasResized = false;

        public static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SIZING)
            {
                if (WindowWasResized == false)
                {
                    //    'indicate the the user is resizing and not moving the window
                    WindowWasResized = true;
                }
            }

            if (msg == WM_EXITSIZEMOVE)
            {
                // 'check that this is the end of resize and not move operation
                if (WindowWasResized == true)
                {
                    // your stuff to do
                    ScaleImage.TryFitImage();

                    // 'set it back to false for the next resize/move
                    WindowWasResized = false;
                }
            }

            return IntPtr.Zero;
        }
    }
}