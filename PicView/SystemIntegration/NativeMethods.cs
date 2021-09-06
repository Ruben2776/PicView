using Microsoft.Win32;
using PicView.PicGallery;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System;
using System.Linq;
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

        #region File properties

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

        #endregion File properties

        #region Remove from Alt + tab

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

        #endregion Remove from Alt + tab

        #region windproc

        // https://stackoverflow.com/a/60938929/13646636
        private const int WM_SIZING = 0x214;

        private const int WM_EXITSIZEMOVE = 0x232;
        private static bool WindowWasResized;

        /// Supress warnings about unused parameters, because they are required by OS.
        /// Executes when user manually resized window
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

                    if (UC.GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsOpen)
                        {
                            UC.GetPicGallery.Width = ConfigureWindows.GetMainWindow.ParentContainer.Width;
                            UC.GetPicGallery.Height = ConfigureWindows.GetMainWindow.ParentContainer.Height;
                        }
                    }

                    // 'set it back to false for the next resize/move
                    WindowWasResized = false;
                }
            }

            return IntPtr.Zero;
        }

        #endregion windproc

        #region Blur

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

        #endregion Blur

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

        public static System.Drawing.Color GetColorAt(int x, int y)
        {
            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);
            int a = (int)GetPixel(dc, x, y);
            _ = ReleaseDC(desk, dc);
            return System.Drawing.Color.FromArgb(255, (a >> 0) & 0xff, (a >> 8) & 0xff, (a >> 16) & 0xff);
        }

        #endregion GetPixelColor

        #region Set Associations

        // needed so that Explorer windows get refreshed after the registry is updated
        [DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        internal static bool SetAssociation(string extension, string progId)
        {
            bool madeChanges = false;
            madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + extension, progId);
            return madeChanges;
        }

        private static bool SetKeyDefaultValue(string keyPath, string value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
            {
                if (key.GetValue(null) as string != value)
                {
                    key.SetValue(null, value);
                    return true;
                }
            }

            return false;
        }

        public static void DeleteAssociation(string Extension, string progId, string applicationFilePath)
        {
            try
            {
                // Delete the key instead of trying to change it
                var defaultApp = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\" + Extension, true);
                if (defaultApp == null)
                {
                    return;
                }
                defaultApp.DeleteSubKey("UserChoice", false);
                defaultApp.Close();

                var openWithContextMenuItem = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}\shell\open\command", true);
                if (openWithContextMenuItem == null)
                {
                    return;
                }
                openWithContextMenuItem.DeleteSubKey("\"" + applicationFilePath + "\" \"%1\"");
                openWithContextMenuItem.Close();
            }
            catch (Exception)
            {
                return;
            }

            // Tell explorer the file association has been changed
            _ = SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        #endregion Set Associations

        #region Check if application exists

        internal static bool IsSoftwareInstalled(string softwareName)
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall") ??
                      Registry.LocalMachine.OpenSubKey(
                          @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");

            if (key == null)
            {
                return false;
            }

            return key.GetSubKeyNames()
                .Select(keyName => key.OpenSubKey(keyName))
                .Select(subkey => subkey.GetValue("DisplayName") as string)
                .Any(displayName => displayName != null && displayName.Contains(softwareName, StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion Check if application exists
    }
}