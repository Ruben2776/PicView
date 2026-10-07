using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using PicView.Core.DebugTools;

namespace PicView.Core.WindowsNT.FileHandling;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class Win32Clipboard
{
    private const uint CF_HDROP = 15;
    private const uint GMEM_MOVEABLE = 0x0002;
    private const uint GMEM_ZEROINIT = 0x0040;
    private const uint DROPEFFECT_MOVE = 2;
    private const string PreferredDropEffectFormatName = "Preferred DropEffect";

    private const int MaxOpenClipboardRetries = 10;
    private const int OpenClipboardRetryDelayMs = 20;

    #region Native Methods

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(IntPtr hWndNewOwner);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [LibraryImport("user32.dll", EntryPoint = "RegisterClipboardFormatW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial uint RegisterClipboardFormat(string lpszFormat);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GlobalAlloc(uint uFlags, nuint dwBytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GlobalLock(IntPtr hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(IntPtr hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GlobalFree(IntPtr hMem);

    #endregion

    #region Native Structures

    [StructLayout(LayoutKind.Sequential)]
    private struct DROPFILES
    {
        public uint pFiles;
        public int ptX;
        public int ptY;
        public int fNC;
        public int fWide;
    }

    #endregion

    /// <summary>
    /// Cuts the specified file to the Windows clipboard, placing CF_HDROP
    /// with "Preferred DropEffect" set to DROPEFFECT_MOVE.
    /// </summary>
    /// <param name="filePath">The path of the file to cut.</param>
    /// <returns>True if the file was successfully placed on the clipboard; otherwise, false.</returns>
    public static bool CutFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        return CutFiles([filePath]);
    }

    /// <summary>
    /// Cuts the specified files to the Windows clipboard, placing CF_HDROP
    /// with "Preferred DropEffect" set to DROPEFFECT_MOVE.
    /// </summary>
    /// <param name="filePaths">The collection of file paths to cut.</param>
    /// <returns>True if the files were successfully placed on the clipboard; otherwise, false.</returns>
    public static bool CutFiles(IEnumerable<string>? filePaths)
    {
        if (filePaths is null)
        {
            return false;
        }

        var validPaths = new List<string>();
        foreach (var path in filePaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            try
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath) || Directory.Exists(fullPath))
                {
                    validPaths.Add(fullPath);
                }
            }
            catch (Exception ex)
            {
                DebugHelper.LogDebug(nameof(Win32Clipboard), nameof(CutFiles), ex);
            }
        }

        if (validPaths.Count == 0)
        {
            return false;
        }

        var preferredDropEffectFormat = RegisterClipboardFormat(PreferredDropEffectFormatName);
        if (preferredDropEffectFormat == 0)
        {
            return false;
        }

        var hDropFiles = CreateDropFilesHandle(validPaths);
        if (hDropFiles == IntPtr.Zero)
        {
            return false;
        }

        var hDropEffect = CreateDropEffectHandle(DROPEFFECT_MOVE);
        if (hDropEffect == IntPtr.Zero)
        {
            GlobalFree(hDropFiles);
            return false;
        }

        if (!TryOpenClipboard())
        {
            GlobalFree(hDropFiles);
            GlobalFree(hDropEffect);
            return false;
        }

        try
        {
            if (!EmptyClipboard())
            {
                GlobalFree(hDropFiles);
                GlobalFree(hDropEffect);
                return false;
            }

            if (SetClipboardData(CF_HDROP, hDropFiles) == IntPtr.Zero)
            {
                GlobalFree(hDropFiles);
                GlobalFree(hDropEffect);
                return false;
            }

            // CF_HDROP is now owned by the system
            hDropFiles = IntPtr.Zero;

            if (SetClipboardData(preferredDropEffectFormat, hDropEffect) == IntPtr.Zero)
            {
                GlobalFree(hDropEffect);
                return false;
            }

            // Preferred DropEffect is now owned by the system
            hDropEffect = IntPtr.Zero;

            return true;
        }
        finally
        {
            CloseClipboard();

            if (hDropFiles != IntPtr.Zero)
            {
                GlobalFree(hDropFiles);
            }

            if (hDropEffect != IntPtr.Zero)
            {
                GlobalFree(hDropEffect);
            }
        }
    }

    private static bool TryOpenClipboard()
    {
        for (var i = 0; i < MaxOpenClipboardRetries; i++)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                return true;
            }

            if (i < MaxOpenClipboardRetries - 1)
            {
                Thread.Sleep(OpenClipboardRetryDelayMs);
            }
        }

        return false;
    }

    private static IntPtr CreateDropFilesHandle(IReadOnlyList<string> paths)
    {
        var totalChars = 0;
        for (var i = 0; i < paths.Count; i++)
        {
            totalChars += paths[i].Length + 1;
        }
        totalChars += 1;

        var structSize = (uint)Marshal.SizeOf<DROPFILES>();
        var totalBytes = (nuint)(structSize + (uint)(totalChars * sizeof(char)));

        var hDropFiles = GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, totalBytes);
        if (hDropFiles == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var pMem = GlobalLock(hDropFiles);
        if (pMem == IntPtr.Zero)
        {
            GlobalFree(hDropFiles);
            return IntPtr.Zero;
        }

        try
        {
            unsafe
            {
                var pDropFiles = (DROPFILES*)pMem;
                pDropFiles->pFiles = structSize;
                pDropFiles->ptX = 0;
                pDropFiles->ptY = 0;
                pDropFiles->fNC = 0;
                pDropFiles->fWide = 1;

                var pChars = (char*)(pMem + (int)structSize);
                var offset = 0;
                for (var i = 0; i < paths.Count; i++)
                {
                    var path = paths[i];
                    path.AsSpan().CopyTo(new Span<char>(pChars + offset, path.Length));
                    offset += path.Length;
                    pChars[offset++] = '\0';
                }
                pChars[offset] = '\0';
            }
        }
        finally
        {
            GlobalUnlock(hDropFiles);
        }

        return hDropFiles;
    }

    private static IntPtr CreateDropEffectHandle(uint dropEffect)
    {
        var totalBytes = (nuint)sizeof(uint);
        var hMem = GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, totalBytes);
        if (hMem == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var pMem = GlobalLock(hMem);
        if (pMem == IntPtr.Zero)
        {
            GlobalFree(hMem);
            return IntPtr.Zero;
        }

        try
        {
            unsafe
            {
                *(uint*)pMem = dropEffect;
            }
        }
        finally
        {
            GlobalUnlock(hMem);
        }

        return hMem;
    }
}
