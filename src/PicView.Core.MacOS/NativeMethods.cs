using System.Runtime.InteropServices;

namespace PicView.Core.MacOS;

internal static partial class NativeMethods
{
    private const string CoreGraphicsLib = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string LibCups = "libcups.2.dylib"; 
    
    #region macOS Native Interop SetCursorPos
    
    [LibraryImport(CoreGraphicsLib, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int CGWarpMouseCursorPosition(CGPoint newCursorPosition);
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct CGPoint
    {
        public double X;
        public double Y;
        
        public CGPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
    
    #endregion
    
    #region macOS Native Interop Print
    
    // --- Data Structures ---
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct cups_dest_t
    {
        public IntPtr name;        // char*
        public IntPtr instance;    // char*
        public int is_default;     // int (boolean)
        public int num_options;    // int
        public IntPtr options;     // cups_option_t*
    }

    // --- P/Invoke Definitions ---

    [LibraryImport(LibCups)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int cupsGetDests(out IntPtr dests);

    [LibraryImport(LibCups)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void cupsFreeDests(int num_dests, IntPtr dests);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int cupsPrintFile(string name, string filename, string title, int num_options, IntPtr options);
    
    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial IntPtr cupsGetPPD(string printerName);

    [LibraryImport(LibCups, StringMarshalling = StringMarshalling.Utf16)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial IntPtr ppdOpenFile(string filename);

    [LibraryImport(LibCups)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void ppdClose(IntPtr ppd);

    [StructLayout(LayoutKind.Sequential)]
    internal struct ppd_size_t
    {
        public IntPtr name;     // char*
        public float width;     // PostScript points (1/72 inch)
        public float length;
        public float left;
        public float bottom;
        public float right;
        public float top;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ppd_file_t
    {
        public IntPtr filename;
        public IntPtr name;
        public int num_sizes;
        public IntPtr sizes; // ppd_size_t*
    }
    
    #endregion
}