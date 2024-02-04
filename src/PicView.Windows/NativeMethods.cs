using System.Runtime.InteropServices;

namespace PicView.Windows;

public static partial class NativeMethods
{
    // Alphanumeric sort
    [LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int StrCmpLogicalW(string x, string y);

    // Change cursor position
    [LibraryImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetCursorPos(int x, int y);
}
