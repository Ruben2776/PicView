using System.Runtime.InteropServices;

namespace PicView.Windows.FileHandling;

public static class ClipboardHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GlobalFree(IntPtr hMem);

    public const uint CF_HDROP = 15;
    public const uint CF_PREFERREDDROPEFFECT = 0x000D; // CFSTR_PREFERREDDROPEFFECT
    public const uint GMEM_MOVEABLE = 0x0002;
    public const uint DROPEFFECT_MOVE = 2;

    public static bool CopyFileToClipboard(string filePath)
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

            // Set the preferred drop effect to move
            var dropEffect = Marshal.AllocHGlobal(sizeof(uint));
            Marshal.WriteInt32(dropEffect, (int)DROPEFFECT_MOVE);
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
