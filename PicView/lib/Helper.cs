using Microsoft.WindowsAPICodePack.Taskbar;
using PicView.lib.UserControls;
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

        // Supported files needs update
        internal const string SupportedFiles = " *.jpg *jpeg. *.png *.gif *.jpe *.bmp *.tiff *.tif *.ico *.wdp *.dds *.svg";

        internal const string FilterFiles =
            "All Supported files|*.bmp;*.jpg;*.png;*.tif;*.gif;*.ico;*.jpeg;*.wdp;*.psd;*.psb;*.cbr;*.cb7;*.cbt;"
            + "*.cbz;*.xz;*.orf;*.cr2;*.crw;*.dng;*.raf;*.ppm;*.raw;*.mrw;*.nef;*.pef;*.3xf;*.arw;*.webp;"
            + "*.zip;*.7zip;*.7z;*.rar;*.bzip2;*.tar;*.wim;*.iso;*.cab"
            ////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
            + "|Pictures|*.bmp;*.jpg;*.png;.tif;*.gif;*.ico;*.jpeg*.wdp*"                                   // Common pics
            + "|jpg| *.jpg;*.jpeg*"                                                                         // JPG
            + "|bmp|*.bmp;"                                                                                 // BMP
            + "|PNG|*.png;"                                                                                 // PNG
            + "|gif|*.gif;"                                                                                 // GIF
            + "|ico|*.ico;"                                                                                 // ICO
            + "|wdp|*.wdp;"                                                                                 // WDP
            + "|svg|*.svg;"                                                                                 // SVG
            + "|tif|*.tif;"                                                                                 // Tif
            + "|Photoshop|*.psd;*.psb"                                                                      // PSD
            + "|Archives|*.zip;*.7zip;*.7z;*.rar;*.bzip2;*.tar;*.wim;*.iso;*.cab"                           // Archives
            + "|Comics|*.cbr;*.cb7;*.cbt;*.cbz;*.xz"                                                        // Comics
            + "|Camera files|*.orf;*.cr2;*.crw;*.dng;*.raf;*.ppm;*.raw;*.mrw;*.nef;*.pef;*.3xf;*.arw";      // Camera files
            ////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\

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
        internal static bool fileMenuOpen;
        internal static bool quickSettingsMenuOpen;
        internal static bool GoToPic;
        internal static bool isZoomed;
        internal static bool Flipped;
        internal static bool canNavigate;
        internal static bool isDraggedOver;
        internal static bool freshStartup;
        internal static bool autoScrolling;

        #endregion

        #region Integers and Doubles
        /// <summary>
        /// Used as comfortable space for standard viewing
        /// </summary>
        internal const int ComfySpace = 350;

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

        /// <summary>
        /// Used to get and set Aspect Ratio
        /// </summary>
        internal static double AspectRatio { get; set; }

        /// <summary>
        /// Used to get how much image is rotated
        /// </summary>
        internal static int Rotateint { get; set; }

        #endregion

        #region Controls
        internal static ImageSettings imageSettingsMenu;
        internal static FileMenu fileMenu;
        internal static QuickSettingsMenu quickSettingsMenu;
        internal static AjaxLoading ajaxLoading;
        internal static SexyToolTip sexyToolTip;
        internal static AutoScrollSign autoScrollSign;
        internal static ClickArrow clickArrowLeft;
        internal static ClickArrow clickArrowRight;
        #endregion

        #region Points + Scaletransform & TranslateTransform
        internal static Point origin;
        internal static Point start;
        internal static Point? autoScrollOrigin;
        internal static Point autoScrollPos;

        internal static ScaleTransform st;
        internal static TranslateTransform tt;
        #endregion

        #region Lists
        /// <summary>
        /// List of file paths to supported files
        /// </summary>
        internal static List<string> Pics { get; set; }
        #endregion

        #region Misc
        /// <summary>
        /// Backup of image
        /// </summary>
        internal static ImageSource prevPicResource;

        internal static ContextMenu cm;

        internal static Timer autoScrollTimer;
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
                        || file.ToLower().EndsWith("aai", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("ai", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("art", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("bgra", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("bgro", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("canvas", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("cin", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("cmyk", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("cmyka", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("cur", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("cut", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("dcm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("dcr", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("dcx", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("dds", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("dfont", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("dlib", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("dpx", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("dxt1", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("dxt5", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("emf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("epdf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("epi", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("eps", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("ept", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("ept2", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("ept3", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("exr", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("fax", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("fits", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("flif", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("g3", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("g4", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("gif87", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("gradient", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("gray", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("group4", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("hald", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("hdr", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("hrz", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("icb", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("icon", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("ipl", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("jc2", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("j2k", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("jng", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("jnx", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("jpm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("jps", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("jpt", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("kdc", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("label", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("map", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("nrw", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("otb", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("otf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pbm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pcd", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pcds", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pcl", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pct", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pcx", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pfa", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pfb", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pfm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("picon", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pict", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pix", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pjpeg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("png00", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("png24", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("png32", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("png48", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("png64", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("png8", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("pnm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("ppm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("ps", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("radialgradient", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("ras", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("rgb", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("rgba", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("rgbo", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("rla", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("rle", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("scr", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("screenshot", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("sgi", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("srf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("sun", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("svgz", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("tiff64", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("ttf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("vda", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("vicar", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("vid", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("viff", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("vst", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("vmf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("wpg", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("xbm", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("xcf", StringComparison.OrdinalIgnoreCase)
                        || file.ToLower().EndsWith("yuv", StringComparison.OrdinalIgnoreCase)
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
                        if (!zipped) {
                            Pics = new List<string>();
                            FolderIndex = -1;
                            TempZipPath = string.Empty;
                            return;
                        }
                        break;
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
                Extract(path, sevenZip, false);
                return true;
            }

            var Winrar = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\WinRAR\\unRAR.exe";
            if (!File.Exists(Winrar))
                Winrar = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\WinRAR\\unRAR.exe";
            if (File.Exists(Winrar))
            {
                Extract(path, Winrar, true);
                return true;
            }

            return false;
        }

        private static void Extract(string path, string exe, bool winrar)
        {
            TempZipPath = Path.GetTempPath() + Path.GetRandomFileName();
            Directory.CreateDirectory(TempZipPath);

            var arguments = winrar ? 
                // Add WinRAR specifics
                "x -o- \"" + path + "\" " 
                :
                // Add 7-Zip specifics
                "x \"" + path + "\" -o";

            arguments += TempZipPath + SupportedFiles + " -r -aou";

            var x = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            if (x == null) return;
            x.EnableRaisingEvents = true;
            x.Exited += (s, e) => Pics = FileList(TempZipPath);
            x.WaitForExit(200);
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
