using System.Runtime.InteropServices;

namespace PicView.WindowsNT.FileHandling;
public static class Shell32Wrapper
{
    [DllImport("shell32.dll", SetLastError = true)]
    public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl,
        [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

    [DllImport("shell32.dll", SetLastError = true)]
    public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name,
        IntPtr bindingContext, [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);
}

public static class FileExplorer
{
    #region Open folder and select file
    
    public static void OpenFolderAndSelectFile(string folderPath, string fileName)
    {
        var nativeFolder = GetNativeFolder(folderPath);
        if (nativeFolder == IntPtr.Zero)
        {
            return;
        }

        var nativeFile = GetNativeFile(folderPath, fileName);
        var fileArray = GetFileArray(nativeFile);

        var result = Shell32Wrapper.SHOpenFolderAndSelectItems(nativeFolder, (uint)fileArray.Length, fileArray, 0);
        if (result != 0)
        {
            // Log error, operation failed
        }

        FreeNativeFolder(nativeFolder);
        FreeNativeFile(nativeFile);
    }

    private static IntPtr GetNativeFolder(string folderPath)
    {
        Shell32Wrapper.SHParseDisplayName(folderPath, IntPtr.Zero, out var nativeFolder, 0, out _);

        if (nativeFolder == IntPtr.Zero)
        {
            // Log error, can't find folder
        }

        return nativeFolder;
    }

    private static IntPtr GetNativeFile(string folderPath, string fileName)
    {
        Shell32Wrapper.SHParseDisplayName(Path.Combine(folderPath, fileName), IntPtr.Zero, out var nativeFile, 0,
            out _);

        if (nativeFile == IntPtr.Zero)
        {
            // Log error, can't find file
        }

        return nativeFile;
    }

    private static IntPtr[] GetFileArray(IntPtr nativeFile)
    {
        return nativeFile == IntPtr.Zero ? Array.Empty<IntPtr>() : new[] { nativeFile };
    }

    private static void FreeNativeFolder(IntPtr nativeFolder)
    {
        Marshal.FreeCoTaskMem(nativeFolder);
    }

    private static void FreeNativeFile(IntPtr nativeFile)
    {
        if (nativeFile != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(nativeFile);
        }
    }
    #endregion

    #region Show File Properties

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHELLEXECUTEINFO
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
        public string lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIcon;
        public IntPtr hProcess;
    }

    private const int SW_SHOW = 5;
    private const uint SEE_MASK_INVOKEIDLIST = 12;
    public static bool ShowFileProperties(string Filename)
    {
        var info = new SHELLEXECUTEINFO();
        info.cbSize = Marshal.SizeOf(info);
        info.lpVerb = "properties";
        info.lpFile = Filename;
        info.nShow = SW_SHOW;
        info.fMask = SEE_MASK_INVOKEIDLIST;
        return ShellExecuteEx(ref info);        
    }
    

    #endregion
    
}
