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
        const string personalizationcsp =
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP";
        const string enforceLockScreenAndLogonImage =
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\DeviceLock\EnforceLockScreenAndLogonImage";
        const string enforcelockscreenprovider =
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\DeviceLock\EnforceLockScreenProvider";
        var ptr = new IntPtr();
        Wow64DisableWow64FsRedirection(ref ptr);

        try
        {
            Registry.SetValue(personalizationcsp, "LockScreenImageStatus", 1, RegistryValueKind.DWord);
            Registry.SetValue(personalizationcsp, "LockScreenImagePath", path, RegistryValueKind.String);
            Registry.SetValue(personalizationcsp, "LockScreenImageUrl", path, RegistryValueKind.String);
            
            Registry.SetValue(enforceLockScreenAndLogonImage, "policytype", 0, RegistryValueKind.DWord);
            Registry.SetValue(enforcelockscreenprovider, "policytype", 0, RegistryValueKind.DWord);
            
            // Seems to only work once, and then have to restart the machine to make it work again. 
            // It seems to disable setting the lock screen image in the settings app with the text:
            // *Some settings are managed by your organization
            // enforceLockScreenAndLogonImage and enforcelockscreenprovider are both set to 0 to try to disable it,
            // but it doesn't seem to work.
            // Need to investigate further and disable it for now.
        }
        catch
        {
            return false;
        }

        return true;
    }
}