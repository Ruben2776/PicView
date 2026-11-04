using Microsoft.Win32;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.Titles;
#if DEBUG
using System.Diagnostics;
#endif

namespace PicView.Core.WindowsNT.FileAssociation;

public static class WindowsFileAssociation
{
    public static bool RegisterFileAssociation(string extension, string description)
    {
        try
        {
            // Remove leading period if present
            if (extension.StartsWith('.'))
            {
                extension = extension[1..];
            }

            var progId = $"{StringExtensions.AppName}.{extension}";
            var executablePath = Environment.ProcessPath;

            // Associate extension with progID
            using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\.{extension}"))
            {
                extKey.SetValue("", progId);
            }

            // Create progID entry
            using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
            {
                progIdKey.SetValue("", description);

                // Set default icon
                using (var iconKey = progIdKey.CreateSubKey("DefaultIcon"))
                {
                    iconKey.SetValue("", $"{executablePath},0");
                }

                // Set open command
                using (var commandKey = progIdKey.CreateSubKey(@"shell\open\command"))
                {
                    commandKey.SetValue("", $"\"{executablePath}\" \"%1\"");
                }
            }

            // Notify the system about the change
            NativeMethods.SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(WindowsFileAssociation), nameof(RegisterFileAssociation), ex);
            return false;
        }
    }
    
    public static bool UnregisterFileAssociation(string extension)
    {
        try
        {
            // Remove leading period if present
            if (extension.StartsWith('.'))
                extension = extension[1..];
                
            var progId = $"{StringExtensions.AppName}.{extension}";
                
            // Delete the extension association
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\.{extension}", false);
                
            // Delete the progID
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{progId}", false);
                
            // Notify the system about the change
            NativeMethods.SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
                
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(WindowsFileAssociation), nameof(UnregisterFileAssociation), ex);
            return false;
        }
    }
    
    public static bool IsFileAssociated(string extension)
    {
        try
        {
            // Remove leading period if present
            if (extension.StartsWith('.', StringComparison.Ordinal))
                extension = extension[1..];
                
            var progId = $"{StringExtensions.AppName}.{extension}";

            using var extKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\.{extension}");
            return extKey != null && string.Equals(extKey.GetValue("") as string, progId, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

}