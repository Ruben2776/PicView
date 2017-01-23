using Microsoft.WindowsAPICodePack.Taskbar;
using PicView.lib.UserControls;
using PicView.UserControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Timers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Diagnostics;

namespace PicView.lib
{
    //https://msdn.microsoft.com/en-us/library/ms182161.aspx
    internal static class NativeMethods
    {
        #region logical sorting

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int StrCmpLogicalW(String x, String y);

        #endregion

        #region Set Cursor Position

        [DllImport("User32.dll")]
        internal static extern bool SetCursorPos(int x, int y);

        // Used to check for wallpaper support
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam,
        string pvParam, uint fWinIni);

        #endregion

        #region file properties
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

        #endregion
    }

    internal static class Helper
    {
        #region Variables

        #region Strings
        internal const string AppName = "PicView";
        internal const string Loading = "Loading...";
        internal const string TxtCopy = "Filename copied to Clipboard";
        internal const string FileCopy = "File copied to Clipboard";
        internal const string ExpFind = "Locating in file explorer";
        internal const string NoImage = "No image loaded";
        internal const string DragOverString = "Drop to load image";
        internal const string SevenZipFiles = " *.jpg *jpeg. *.png *.gif *.jpe *.bmp *.tiff *.tif *.ico *.wdp *.dds *.svg";

        /// <summary>
        /// File path of current  image
        /// </summary>
        internal static string PicPath { get; set; }

        /// <summary>
        /// Backup of PicPath
        /// </summary>
        internal static string xPicPath;

        /// <summary>
        /// File path for the extracted folder
        /// </summary>
        internal static string TempZipPath { get; set; }

        /// <summary>
        /// Returns string with zoom %
        /// </summary>
        internal static string ZoomPercentage { get { return Math.Round(AspectRatio * 100) + "%"; } }

        /// <summary>
        /// Returns zoom % if not zero. Empty string for zero
        /// </summary>
        internal static string Zoomed
        {
            get
            {
                var zoom = Math.Round(AspectRatio * 100);
                if (zoom == 100)
                    return string.Empty;

                return " - " + zoom + "%";
            }
        }

        /// <summary>
        /// Returns aspect ratio as a formatted string
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        internal static string StringAspect(int width, int height)
        {
            var gcd = GCD(width, height);
            var x = (width / gcd);
            var y = (height / gcd);

            if (x == width && y == height)
                return ") ";

            return ", " + x + ":" + y + ") ";
        }
        #endregion

        #region Booleans
        internal static bool LeftbuttonClicked;
        internal static bool RightbuttonClicked;
        internal static bool imageSettingsMenuOpen;
        internal static bool openMenuOpen;
        internal static bool quickSettingsMenuOpen;
        internal static bool GoToPic;
        //internal static bool cursorHidden;
        internal static bool isZoomed;
        internal static bool Flipped;
        internal static bool canNavigate;
        //internal static bool mouseIsOnArrow;
        internal static bool isDraggedOver;
        internal static bool freshStartup;

        #endregion

        #region Integers and Doubles
        /// <summary>
        /// Used as comfortable space for standard viewing
        /// </summary>
        internal const int ComfySpace = 90;

        /// <summary>
        /// Backup of Width data
        /// </summary>
        internal static double xWidth;

        /// <summary>
        /// Backup of Height data
        /// </summary>
        internal static double xHeight;

        /// <summary>
        /// Counter used to get/set current index
        /// </summary>
        internal static int FolderIndex { get; set; }

        /// <summary>
        /// Backup of FolderIndex
        /// </summary>
        internal static int xFolderIndex;

        /// <summary>
        /// Counter used to check if preloading is neccesary
        /// </summary>
        internal static short PreloadCount { get; set; }

        internal const double MinZoom = 0.3;
        internal static double AspectRatio { get; set; }
        internal static int Rotateint { get; set; }

        #endregion

        #region Controls
        internal static ImageSettings imageSettingsMenu;
        internal static OpenMenu openMenu;
        internal static QuickSettingsMenu quickSettingsMenu;
        internal static AjaxLoading ajaxLoading;
        internal static SexyToolTip sexyToolTip;
        internal static About about_uc;
        internal static Help help_uc;
        #endregion

        #region Points + Scaletransform & TranslateTransform
        internal static Point origin;
        internal static Point start;

        internal static ScaleTransform st;
        internal static TranslateTransform tt;
        #endregion

        #region Lists
        /// <summary>
        /// The list of images
        /// </summary>
        internal static List<string> Pics { get; set; }
        #endregion

        #region Misc
        /// <summary>
        /// Backup of image
        /// </summary>
        internal static ImageSource prevPicResource;

        //internal static System.Timers.Timer activityTimer;
        internal static ContextMenu cm;
        #endregion      

        #endregion

        #region GCD
        internal static int GCD(int x, int y)
        {
            return y == 0 ? x : GCD(y, x % y);
        }
        #endregion

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

        #region Close, Restore and mazimize windows functions

        /// <summary>
        /// Close UserControl with fade animation
        /// </summary>
        /// <param name="usercontrol"></param>
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

        #region File Functions

        #region DeleteTempFiles
        /// <summary>
        /// Deletes the temporary files when an archived file has been opened
        /// </summary>
        internal static void DeleteTempFiles()
        {
            if (!Directory.Exists(TempZipPath))
                return;
            try
            {
                Array.ForEach(Directory.GetFiles(TempZipPath), File.Delete);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                Directory.Delete(TempZipPath);
            }
            catch (Exception)
            {
                return;
            }
        }
        #endregion

        #region Clean file names

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

        #endregion

        #region GetSizeReadable

        /// <summary>
        /// Return file size in a readable format
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
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

        #endregion

        #region File list
       
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
                        || file.ToLower().EndsWith("orf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("cr2", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("crw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("dng", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("raf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("ppm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("raw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("mrw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("nef", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("x3f", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("arw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("webp", StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();

            // Sort like Windows Explorer sorts file names alphanumerically
            foo.Sort((x, y) => { return NativeMethods.StrCmpLogicalW(x, y); });

            return foo;
        }

        #endregion

        #region GetValues
        /// <summary>
        /// Gets values and extracts archives
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static Task GetValues(string path)
        {
            return Task.Run(() =>
            {
                bool zipped = false;
                var extension = Path.GetExtension(path);
                extension = extension.ToLower();
                switch (extension)
                {
                    // Archives
                    case ".zip":
                    case ".7zip":
                    case ".7z":
                    case ".rar":
                    case ".cbr":
                    case ".cb7":
                    case ".cbt":
                    case ".cbz":
                    case ".xz":
                    case ".bzip2":
                    case ".gzip":
                    case ".tar":
                    case ".wim":
                    case ".iso":
                    case ".cab":
                        zipped = Extract(path);                       
                        if (!zipped)
                            goto default;
                        break;
                    case ".jpg":
                    case ".jpeg":
                    case ".jpe":
                    case ".png":
                    case ".bmp":
                    case ".tif":
                    case ".tiff":
                    case ".gif":
                    case ".ico":
                    case ".wdp":
                    case ".dds":
                    case ".svg":
                    case ".psd":
                    case ".psb":
                    case ".orf":
                    case ".cr2":
                    case ".crw":
                    case ".dng":
                    case ".raf":
                    case ".ppm":
                    case ".raw":
                    case ".mrw":
                    case ".nef":
                    case ".pef":
                    case ".x3f": //Questionable if it works :(
                    case ".arw":
                    case ".webp":
                        break;
                    default:
                        Pics = new List<string>();
                        FolderIndex = -1;
                        TempZipPath = string.Empty;
                        return;
                }

                if (zipped)
                {
                    if (FolderIndex > -1)
                    {
                        xFolderIndex = FolderIndex;
                    }
                    if (!string.IsNullOrWhiteSpace(PicPath))
                    {
                        xPicPath = PicPath;
                    }
                    FolderIndex = 0;
                    if (Directory.Exists(TempZipPath))
                    {
                        var test = Directory.EnumerateFileSystemEntries(TempZipPath);
                        if (test.Count() > -1)
                            Pics = FileList(TempZipPath);
                    }
                }
                else
                {
                    Pics = FileList(Path.GetDirectoryName(path));
                    FolderIndex = Pics.IndexOf(path);
                }

                PicPath = path;
            });
        }

        #endregion

        #region Extract
        /// <summary>
        /// Attemps to extract folder
        /// </summary>
        /// <param name="path">The path to the archived file</param>
        /// <returns></returns>
        internal static bool Extract(string path)
        {
            var sevenZip = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\7-Zip\\7z.exe";
            if (!File.Exists(sevenZip))
                sevenZip = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\7-Zip\\7z.exe";
            if (File.Exists(sevenZip))
            {
                TempZipPath = Path.GetTempPath() + Path.GetRandomFileName();
                Directory.CreateDirectory(TempZipPath);

                var x = Process.Start(new ProcessStartInfo
                {
                    FileName = sevenZip,
                    Arguments = "x \"" + path + "\" -o" + TempZipPath + SevenZipFiles + " -r -aou",
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                if (x == null) return false;
                x.EnableRaisingEvents = true;
                x.Exited += (s, e) => Pics = FileList(TempZipPath);
                x.WaitForExit(200);
                return true;
            }
            return false;
        }

        #endregion

        #region Print

        /// <summary>
        /// Sends the file to Windows print system
        /// </summary>
        /// <param name="path">The file path</param>
        internal static void Print(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var p = new Process();
            p.StartInfo.FileName = path;
            p.StartInfo.Verb = "print";
            p.Start();
        }

        #endregion

        #region Wallpaper

        internal static void SetWallpaper(string path, WallpaperStyle style)
        {
            if (canNavigate)
            {
                if (File.Exists(path))
                    Task.Run(() => Wallpaper.SetDesktopWallpaper(path, style));
            }
            else
            {
                Task.Run(() =>
                {
                    //Handle if file from web, need clipboard image solution
                    var tempPath = Path.GetTempPath();
                    var randomName = Path.GetRandomFileName();
                    var webClient = new WebClient();
                    Directory.CreateDirectory(tempPath);
                    webClient.DownloadFile(path, tempPath + randomName);
                    Wallpaper.SetDesktopWallpaper(tempPath + randomName, style);
                    File.Delete(tempPath + randomName);
                    var timer = new Timer(2000);
                    timer.Elapsed += (s,x) => Directory.Delete(tempPath);
                });
            }
        }
        #endregion

        #region GetWindowsThumbnail
        /// <summary>
        /// Returns a Windows Thumbnail
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <returns></returns>
        internal static System.Windows.Media.Imaging.BitmapSource GetWindowsThumbnail(string path)
        {
            return Microsoft.WindowsAPICodePack.Shell.ShellFile.FromFilePath(path).Thumbnail.BitmapSource;
        }
        #endregion
    }
}
