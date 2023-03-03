using System.IO;
using System.Runtime.InteropServices;

namespace PicView.FileHandling
{
    public static class Shell32Wrapper
    {
        [DllImport("shell32.dll", SetLastError = true)]
        public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);
    }

    internal static class FileExplorer
    {
        public static void OpenFolderAndSelectFile(string folderPath, string fileName)
        {
            IntPtr nativeFolder = GetNativeFolder(folderPath);
            if (nativeFolder == IntPtr.Zero)
            {
                return;
            }

            IntPtr nativeFile = GetNativeFile(folderPath, fileName);
            IntPtr[] fileArray = GetFileArray(nativeFile);

            int result = Shell32Wrapper.SHOpenFolderAndSelectItems(nativeFolder, (uint)fileArray.Length, fileArray, 0);
            if (result != 0)
            {
                // Log error, operation failed
            }

            FreeNativeFolder(nativeFolder);
            FreeNativeFile(nativeFile);
        }

        private static IntPtr GetNativeFolder(string folderPath)
        {
            IntPtr nativeFolder;
            uint psfgaoOut;
            Shell32Wrapper.SHParseDisplayName(folderPath, IntPtr.Zero, out nativeFolder, 0, out psfgaoOut);

            if (nativeFolder == IntPtr.Zero)
            {
                // Log error, can't find folder
            }

            return nativeFolder;
        }

        private static IntPtr GetNativeFile(string folderPath, string fileName)
        {
            IntPtr nativeFile;
            uint psfgaoOut;
            Shell32Wrapper.SHParseDisplayName(Path.Combine(folderPath, fileName), IntPtr.Zero, out nativeFile, 0, out psfgaoOut);

            if (nativeFile == IntPtr.Zero)
            {
                // Log error, can't find file
            }

            return nativeFile;
        }

        private static IntPtr[] GetFileArray(IntPtr nativeFile)
        {
            if (nativeFile == IntPtr.Zero)
            {
                return Array.Empty<IntPtr>();
            }

            return new[] { nativeFile };
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