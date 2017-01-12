using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Nepharia.lib
{
    internal static class Helper
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int StrCmpLogicalW(String x, String y); // logical sorting

        [DllImport("User32.dll")]
        internal static extern bool SetCursorPos(int x, int y);

        internal const string AppName = "PicView";
        internal const string Loading = "Loading...";

        internal static int GCD(int x, int y)
        {
            return y == 0 ? x : GCD(y, x % y);
        }

        #region Win 7 Taskbar Stuff

        internal static void Progress(int i, int ii)
        {
            TaskbarManager prog = TaskbarManager.Instance;
            prog.SetProgressState(TaskbarProgressBarState.Normal);
            prog.SetProgressValue(i, ii);
        }

        internal static void NoProgress()
        {
            TaskbarManager prog = TaskbarManager.Instance;
            prog.SetProgressState(TaskbarProgressBarState.NoProgress);
        }

        #endregion

        #region window stuff

        internal static void Close(Window window)
        {
            SystemCommands.CloseWindow(window);
        }

        internal static void Close(UserControl usercontrol)
        {
            usercontrol.Visibility = Visibility.Visible;
            var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
            da.To = 0;
            da.Completed += delegate { usercontrol.Visibility = Visibility.Hidden; };

            if (usercontrol != null)
                usercontrol.BeginAnimation(UIElement.OpacityProperty, da); ;
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

        #endregion

        #region File stuff
        internal static bool FilePathHasInvalidChars(string path)
        {
            return (!string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0);
        }

        internal static string MakeValidFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(name, invalidRegStr, "_");
        }

        internal static bool IsSameFolder(string source, string target)
        {
            source = Path.GetDirectoryName(source);
            target = Path.GetDirectoryName(target);

            return source.Equals(target);
        }
        internal static List<string> FileList(string path)
        {
            var foo = Directory.GetFiles(path)
                .AsParallel()
                .Where(file =>
                        file.ToLower().EndsWith("jpg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("jpeg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("jpe", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("png", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("bmp", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("tif", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("tiff", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("gif", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("ico", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("wdp", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("dds", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("svg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("psd", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("psb", StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();

            foo.Sort((x, y) => { return StrCmpLogicalW(x, y); });

            return foo;
        }

        internal static string GetSizeReadable(long i)
        {
            string sign = (i < 0 ? "-" : string.Empty);
            double readable = (i < 0 ? -i : i);
            char suffix;

            if (i >= 0x1000000000000000) // Exabyte
            {
                suffix = 'E';
                readable = (i >> 50);
            }
            else if (i >= 0x4000000000000) // Petabyte
            {
                suffix = 'P';
                readable = (i >> 40);
            }
            else if (i >= 0x10000000000) // Terabyte
            {
                suffix = 'T';
                readable = (i >> 30);
            }
            else if (i >= 0x40000000) // Gigabyte
            {
                suffix = 'G';
                readable = (i >> 20);
            }
            else if (i >= 0x100000) // Megabyte
            {
                suffix = 'M';
                readable = (i >> 10);
            }
            else if (i >= 0x400) // Kilobyte
            {
                suffix = 'K';
                readable = i;
            }
            else
            {
                return i.ToString(sign + "0 B"); // Byte
            }
            readable = readable / 1024;

            return sign + readable.ToString("0.## ") + suffix + 'B';
        }
        /// Credits to http://www.somacon.com/p576.php

        #endregion

        #region file properties

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

        #endregion

        #region Print
        internal static void Print(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName = path;
            p.StartInfo.Verb = "print";
            p.Start();
        }

        #endregion
    }
}
