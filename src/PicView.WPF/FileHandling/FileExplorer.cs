using System.IO;
using System.Runtime.InteropServices;

namespace PicView.WPF.FileHandling
{
    public static class Shell32Wrapper
    {
        [DllImport("shell32.dll", SetLastError = true)]
        public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl,
            [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name,
            IntPtr bindingContext, [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);
    }

    internal static class FileExplorer
    {
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
    }
}