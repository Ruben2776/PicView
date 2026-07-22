using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace PicView.Core.WindowsNT;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class NativeMethods
{
    // Alphanumeric sort
    [LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int StrCmpLogicalW(string x, string y);

    // Change cursor position
    [LibraryImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetCursorPos(int x, int y);    
    
    // Notify the system about the change
    [LibraryImport("shell32.dll")]
    public static partial void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
    
    
    #region Window management

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetCapture();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out ScreenPoint pointerPosition);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(IntPtr hWnd, out WindowRect windowRect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReleaseCapture();

    [LibraryImport("user32.dll")]
    public static partial IntPtr SetCapture(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    public struct ScreenPoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    #endregion Window management


    #region Disable Screensaver and Power options

    private const uint ES_CONTINUOUS = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;
    private const uint ES_DISPLAY_REQUIRED = 0x00000002;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial uint SetThreadExecutionState(uint esFlags);
    
    public static void DisableScreensaver()
    {
        _ = SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
    }
    
    public static void EnableScreensaver()
    {
        _ = SetThreadExecutionState(ES_CONTINUOUS);
    }

    #endregion Disable Screensaver and Power options
    
    #region Printing

    [LibraryImport("winspool.drv", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnumPrintersW(uint flags, string? name, uint level, IntPtr pPrinterEnum, uint cbBuf, out uint pcbNeeded, out uint pcReturned);

    [LibraryImport("winspool.drv", EntryPoint = "GetDefaultPrinterW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetDefaultPrinter(IntPtr pszBuffer, ref uint pcchBuffer);

    [LibraryImport("winspool.drv", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial int DeviceCapabilitiesW(string pDevice, string pPort, ushort fwCapability, IntPtr pOutput, IntPtr pDevMode);

    [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial IntPtr CreateDCW(string lpszDriver, string lpszDevice, string? lpszOutput, IntPtr lpInitData);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int StartDocW(IntPtr hdc, ref DOCINFO docinfo);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial int EndDoc(IntPtr hdc);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial int StartPage(IntPtr hdc);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial int EndPage(IntPtr hdc);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial int GetDeviceCaps(IntPtr hdc, int nIndex);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial int StretchDIBits(IntPtr hdc, int XDest, int YDest, int nDestWidth, int nDestHeight, int XSrc, int YSrc, int nSrcWidth, int nSrcHeight, IntPtr lpBits, ref BITMAPINFOHEADER lpBitsInfo, uint iUsage, uint dwRop);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DOCINFO
    {
        public int cbSize;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszDocName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszOutput;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszDatatype;
        public int fwType;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PRINTER_INFO_2
    {
        public string pServerName;
        public string pPrinterName;
        public string pShareName;
        public string pPortName;
        public string pDriverName;
        public string pComment;
        public string pLocation;
        public IntPtr pDevMode;
        public string pSepFile;
        public string pPrintProcessor;
        public string pDatatype;
        public string pParameters;
        public IntPtr pSecurityDescriptor;
        public uint Attributes;
        public uint Priority;
        public uint DefaultPriority;
        public uint StartTime;
        public uint UntilTime;
        public uint Status;
        public uint cJobs;
        public uint AveragePPM;
    }

    public const uint PRINTER_ENUM_LOCAL = 0x00000002;
    public const uint PRINTER_ENUM_CONNECTIONS = 0x00000004;
    
    public const ushort DC_PAPERNAMES = 16;
    public const ushort DC_PAPERS = 2;
    public const ushort DC_PAPERSIZE = 3;

    public const int LOGPIXELSX = 88;
    public const int LOGPIXELSY = 90;
    public const int PHYSICALWIDTH = 110;
    public const int PHYSICALHEIGHT = 111;
    public const int PHYSICALOFFSETX = 112;
    public const int PHYSICALOFFSETY = 113;

    public const uint DIB_RGB_COLORS = 0;
    public const uint SRCCOPY = 0x00CC0020;

    #endregion Printing
}
