using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Microsoft.Win32;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.Update;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Win32.PlatformUpdate;

/// <summary>
///     Handles Windows-specific update logic
/// </summary>
public static class WinUpdateHelper
{
    private const string ExecutableName = "PicView.exe";

    public static async Task HandleWindowsUpdate(UpdateInfo updateInfo, string tempPath)
    {
        // Determine if application is installed or portable
        var isInstalled = IsApplicationInstalled();

        // Determine architecture
        var architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => isInstalled ? InstalledArchitecture.X64Install : InstalledArchitecture.X64Portable,
            Architecture.Arm64 => isInstalled
                ? InstalledArchitecture.Arm64Install
                : InstalledArchitecture.Arm64Portable,
            _ => InstalledArchitecture.X64Install
        };

        // Apply update based on architecture and installation type
        switch (architecture)
        {
            case InstalledArchitecture.Arm64Install:
                await InstallWindowsUpdate(updateInfo.Arm64Install, tempPath);
                break;

            case InstalledArchitecture.Arm64Portable:
                await OpenDownloadInBrowser(updateInfo.Arm64Portable);
                break;

            case InstalledArchitecture.X64Install:
                await InstallWindowsUpdate(updateInfo.X64Install, tempPath);
                break;

            case InstalledArchitecture.X64Portable:
                await OpenDownloadInBrowser(updateInfo.X64Portable);
                break;
        }
    }

    /// <summary>
    ///     Downloads and runs the installer for Windows
    /// </summary>
    private static async Task InstallWindowsUpdate(string downloadUrl, string tempPath)
    {
        var fileName = Path.GetFileName(downloadUrl);
        var tempFileDownloadPath = Path.Combine(tempPath, fileName);
        
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        await UpdateManager.DownloadUpdateFile(core, downloadUrl, tempFileDownloadPath);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = tempFileDownloadPath
            }
        };

        process.Start();
        await WindowFunctions.WindowClosingBehavior();
    }

    /// <summary>
    ///     Opens a download link in the browser for portable versions
    /// </summary>
    private static async Task OpenDownloadInBrowser(string downloadUrl)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo(downloadUrl)
            {
                UseShellExecute = true,
                Verb = "open"
            }
        };

        process.Start();
        await process.WaitForExitAsync();
    }

    /// <summary>
    ///     Checks if the application is installed or running as portable
    /// </summary>
    private static bool IsApplicationInstalled()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        // Check if executable exists in Program Files
        var x64Path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "PicView",
            ExecutableName);

        return File.Exists(x64Path) || CheckRegistryForInstallation();
    }

    /// <summary>
    ///     Checks Windows registry to determine if the application is installed
    /// </summary>
    private static bool CheckRegistryForInstallation()
    {
        const string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        const string registryKey64 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

        try
        {
            // Check 32-bit registry path
            return CheckRegistryKeyForInstallation(Registry.LocalMachine, registryKey) ||
                   // Check 64-bit registry path
                   CheckRegistryKeyForInstallation(Registry.LocalMachine, registryKey64);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(WinUpdateHelper), nameof(CheckRegistryForInstallation), e);
            return false;
        }
    }

    /// <summary>
    ///     Checks a specific registry key for PicView installation
    /// </summary>
    private static bool CheckRegistryKeyForInstallation(RegistryKey baseKey, string keyPath)
    {
        using var key = baseKey.OpenSubKey(keyPath);
        if (key == null)
        {
            return false;
        }

        foreach (var subKeyName in key.GetSubKeyNames())
        {
            using var subKey = key.OpenSubKey(subKeyName);
            var installDir = subKey?.GetValue("InstallLocation")?.ToString();

            if (string.IsNullOrWhiteSpace(installDir))
            {
                continue;
            }

            if (Path.Exists(Path.Combine(installDir, ExecutableName)))
            {
                return true;
            }
        }

        return false;
    }
}