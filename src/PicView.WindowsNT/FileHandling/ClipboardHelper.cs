using System.Runtime.InteropServices;

namespace PicView.WindowsNT.FileHandling;

public static partial class ClipboardHelper
{
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool OpenClipboard(IntPtr hWndNewOwner);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EmptyClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr GlobalLock(IntPtr hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GlobalUnlock(IntPtr hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr GlobalFree(IntPtr hMem);

    public const uint CF_HDROP = 15;
    public const uint CF_PREFERREDDROPEFFECT = 0x000D; // CFSTR_PREFERREDDROPEFFECT
    public const uint GMEM_MOVEABLE = 0x0002;
    public const uint DROPEFFECT_MOVE = 2;
    public const uint DROPEFFECT_COPY = 1;

    public static bool CopyFileToClipboard(bool cut, string filePath)
    {
        if (!OpenClipboard(IntPtr.Zero))
        {
            return false;
        }

        try
        {
            EmptyClipboard();

            var dropFiles = new Dropfiles
            {
                pFiles = Marshal.SizeOf<Dropfiles>()
            };
            
            dropFiles.pt.x = 0;
            dropFiles.pt.y = 0;
            dropFiles.fNC = true;
            dropFiles.fWide = true;

            var filePathBytes = (filePath + "\0\0").ToCharArray();
            var filePathSize = filePathBytes.Length * 2;

            var globalMemSize = Marshal.SizeOf(dropFiles) + filePathSize;
            var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)globalMemSize);

            if (hGlobal == IntPtr.Zero)
            {
                return false;
            }

            var lockedMem = GlobalLock(hGlobal);
            if (lockedMem == IntPtr.Zero)
            {
                GlobalFree(hGlobal);
                return false;
            }

            Marshal.StructureToPtr(dropFiles, lockedMem, false);

            var filePathStart = (IntPtr)((long)lockedMem + Marshal.SizeOf(dropFiles));
            Marshal.Copy(filePathBytes, 0, filePathStart, filePathBytes.Length);

            GlobalUnlock(hGlobal);

            if (SetClipboardData(CF_HDROP, hGlobal) == IntPtr.Zero)
            {
                GlobalFree(hGlobal);
                return false;
            }

            
            var dropEffect = Marshal.AllocHGlobal(sizeof(uint));
            if (cut)
            {
                // Setting the drop effect to move doesn't work.
                Marshal.WriteInt32(dropEffect, (int)DROPEFFECT_MOVE);
            }
            else
            {
                Marshal.WriteInt32(dropEffect, (int)DROPEFFECT_COPY);
            }
            if (SetClipboardData(CF_PREFERREDDROPEFFECT, dropEffect) == IntPtr.Zero)
            {
                Marshal.FreeHGlobal(dropEffect);
                return false;
            }

        }
        finally
        {
            CloseClipboard();
        }

        return true;

    }



    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Dropfiles
    {
        public int pFiles;
        public Point pt;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fNC;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fWide;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int x;
        public int y;
    }
}
