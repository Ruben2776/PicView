using System.Diagnostics;
using System.Runtime.InteropServices;
using PicView.Core.DebugTools;

namespace PicView.Core.WindowsNT.FileHandling;

public static partial class WinFileHelper
{
    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOERRORUI = 0x0400;
    private const ushort FOF_WANTNUKEWARNING = 0x4000;

    [StructLayout(LayoutKind.Sequential)]
    private struct SHFILEOPSTRUCTW
    {
        public IntPtr hwnd;
        public uint wFunc;
        public IntPtr pFrom;
        public IntPtr pTo;
        public ushort fFlags;
        public int fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public IntPtr lpszProgressTitle;
    }

    [LibraryImport("shell32.dll")]
    private static partial int SHFileOperationW(ref SHFILEOPSTRUCTW lpFileOp);

    /// <summary>
    /// Deletes a file through the shell without asking for confirmation, since PicView has its own confirmation dialog.
    /// If a file that should be recycled can't be, e.g. it's too large for the recycle bin, the shell still warns
    /// before deleting it permanently.
    /// </summary>
    /// <returns>True if the file was deleted.</returns>
    public static bool DeleteFile(string filePath, bool moveToRecycleBin) =>
        Delete(filePath, moveToRecycleBin
            ? FOF_SILENT | FOF_NOCONFIRMATION | FOF_ALLOWUNDO | FOF_WANTNUKEWARNING
            : FOF_SILENT | FOF_NOCONFIRMATION);

    /// <summary>
    /// Moves a file to the recycle bin with no dialog at all: no confirmation, no error, and no "delete permanently?"
    /// warning. Without that warning, the shell deletes a file the bin cannot take, too large for it, without asking.
    /// Only for PicView's own copies, which are not worth a question.
    /// </summary>
    /// <returns>True if the file is gone.</returns>
    public static bool RecycleQuietly(string filePath) =>
        Delete(filePath, FOF_SILENT | FOF_NOCONFIRMATION | FOF_ALLOWUNDO | FOF_NOERRORUI);

    private static bool Delete(string filePath, int flags)
    {
        var from = IntPtr.Zero;
        try
        {
            // pFrom is a list of paths ended by an empty one
            from = Marshal.StringToHGlobalUni(Path.GetFullPath(filePath) + '\0');
            using var process = Process.GetCurrentProcess();
            var operation = new SHFILEOPSTRUCTW
            {
                hwnd = process.MainWindowHandle,
                wFunc = FO_DELETE,
                pFrom = from,
                fFlags = (ushort)flags
            };
            _ = SHFileOperationW(ref operation);
            return !File.Exists(filePath);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(WinFileHelper), nameof(Delete), e);
            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(from);
        }
    }

    /// <summary>
    /// Network shares and removable drives have no recycle bin, the shell deletes files on them permanently.
    /// </summary>
    public static bool HasRecycleBin(string filePath)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(filePath));
            // UNC paths (\\server\share) are network shares, and DriveInfo doesn't take them
            if (string.IsNullOrEmpty(root) || root.StartsWith(@"\\", StringComparison.Ordinal))
            {
                return false;
            }

            return new DriveInfo(root).DriveType == DriveType.Fixed;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(WinFileHelper), nameof(HasRecycleBin), e);
            return false;
        }
    }
}
