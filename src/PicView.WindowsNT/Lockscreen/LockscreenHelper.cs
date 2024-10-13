using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PicView.WindowsNT.Lockscreen;

public static partial class LockscreenHelper
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);

    public static bool SetLockScreenImage(string path)
    {
        const string registryKey =
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP";
        var ptr = new IntPtr();
        Wow64DisableWow64FsRedirection(ref ptr);

        try
        {
            Registry.SetValue(registryKey, "LockScreenImageStatus", 1, RegistryValueKind.DWord);
            Registry.SetValue(registryKey, "LockScreenImagePath", path, RegistryValueKind.String);
            Registry.SetValue(registryKey, "LockScreenImageUrl", path, RegistryValueKind.String);
        }
        catch
        {
            return false;
        }

        return true;
    }
}