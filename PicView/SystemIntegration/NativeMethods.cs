using Microsoft.Win32;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Interop;

namespace PicView.SystemIntegration
{
    //https://msdn.microsoft.com/en-us/library/ms182161.aspx
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

        #region Disable Screensaver and Power options

        internal const uint ES_CONTINUOUS = 0x80000000;

        internal const uint ES_SYSTEM_REQUIRED = 0x00000001;
        internal const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SetThreadExecutionState([In] uint esFlags);

        #endregion Remove from Alt + tab

        #region windproc

        // https://stackoverflow.com/a/60938929/13646636
        private const int WM_SIZING = 0x214;
        // Message list = https://wiki.winehq.org/List_Of_Windows_Messages

        /// Supress warnings about unused parameters, because they are required by OS.
        /// Executes when user manually resized window
        public static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (Settings.Default.AutoFitWindow || GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalFullscreenOpen)
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

        #region Blur

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point : IEquatable<Win32Point>
        {
            public int X;
            public int Y;

            public override bool Equals(object obj)
            {
                return obj is Win32Point point && Equals(point);
            }

            public bool Equals(Win32Point other)
            {
                return X == other.X &&
                       Y == other.Y;
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }
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

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
            public override bool Equals(object obj)
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
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

        public static Color GetColorAt(int x, int y)
        {
            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);
            int a = (int)GetPixel(dc, x, y);
            _ = ReleaseDC(desk, dc);
            return Color.FromArgb(255, (a >> 0) & 0xff, (a >> 8) & 0xff, (a >> 16) & 0xff);
        }

        #endregion GetPixelColor

        #region Check if application exists

        internal static string? GetPathForExe(string fileName)
        {
            RegistryKey localMachine = Registry.LocalMachine;
            RegistryKey? fileKey = localMachine.OpenSubKey(string.Format(@"{0}\{1}", @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths", fileName));
            if (fileKey == null) { return null; }
            object? result = fileKey.GetValue(string.Empty);
            if (result == null) { return null; }
            fileKey.Close();


            return (string)result;
        }

        #endregion Check if application exists
    }
}