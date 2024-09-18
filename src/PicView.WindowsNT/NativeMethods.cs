using System.Runtime.InteropServices;

namespace PicView.WindowsNT;

public static partial class NativeMethods
{
    // Alphanumeric sort
    [LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int StrCmpLogicalW(string x, string y);

    // Change cursor position
    [LibraryImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetCursorPos(int x, int y);    
    
    
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


}
