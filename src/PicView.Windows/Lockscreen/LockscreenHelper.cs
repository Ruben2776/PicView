using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace PicView.Windows.Lockscreen;

public static class LockscreenHelper
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr); //If on 64 bit, C# will replace "System32" with "SysWOW64". This disables that.

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