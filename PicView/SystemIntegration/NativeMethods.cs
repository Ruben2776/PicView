using PicView.UILogic.Loading;
using PicView.UILogic.PicGallery;
using PicView.UILogic.Sizing;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Interop;

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
        private static bool WindowWasResized;

        /// Supress warnings about unused parameters, because they are required by OS.
        /// Executes when user manually resized window
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "<Pending>")]
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning restore IDE0060 // Remove unused parameter
        public static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SIZING)
            {
                if (WindowWasResized == false)
                {
                    // 'indicate the the user is resizing and not moving the window
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

                    if (UILogic.UC.GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsOpen)
                        {
                            UILogic.UC.GetPicGallery.Width = LoadWindows.GetMainWindow.ParentContainer.Width;
                            UILogic.UC.GetPicGallery.Height = LoadWindows.GetMainWindow.ParentContainer.Height;
                        }
                    }

                    // 'set it back to false for the next resize/move
                    WindowWasResized = false;
                }
            }

            return IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public int X;
            public int Y;
        };

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        private struct AccentPolicy : IEquatable<AccentPolicy>
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;

            public override bool Equals(object obj)
            {
                return obj is AccentPolicy policy && Equals(policy);
            }

            public bool Equals(AccentPolicy other)
            {
                return AnimationId == other.AnimationId;
            }

            public static bool operator ==(AccentPolicy left, AccentPolicy right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(AccentPolicy left, AccentPolicy right)
            {
                return !(left == right);
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }

        private struct WindowCompositionAttributeData : IEquatable<WindowCompositionAttributeData>
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;

            public override bool Equals(object obj)
            {
                return obj is WindowCompositionAttributeData data && Equals(data);
            }

            public bool Equals(WindowCompositionAttributeData other)
            {
                return Attribute == other.Attribute &&
                       Data.Equals(other.Data) &&
                       SizeOfData == other.SizeOfData;
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        internal static void EnableBlur(Window window)
        {
            var windowHelper = new WindowInteropHelper(window);
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var Data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            _ = SetWindowCompositionAttribute(windowHelper.Handle, ref Data);
            Marshal.FreeHGlobal(accentPtr);
        }

        #region GetPixelColor

        // https://stackoverflow.com/a/24759418/13646636

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowDC(IntPtr window);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern uint GetPixel(IntPtr dc, int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr window, IntPtr dc);

        public static System.Drawing.Color GetColorAt(int x, int y)
        {
            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);
            int a = (int)GetPixel(dc, x, y);
            _ = ReleaseDC(desk, dc);
            return System.Drawing.Color.FromArgb(255, (a >> 0) & 0xff, (a >> 8) & 0xff, (a >> 16) & 0xff);
        }

        #endregion GetPixelColor
    }
}