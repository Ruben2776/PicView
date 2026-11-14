using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.UI;
using PicView.Core.DebugTools;
using PicView.Core.Update;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.MacOS.PlatformUpdate;

/// <summary>
///     Handles macOS-specific update logic
/// </summary>
public static class MacUpdateHelper
{
    /// <summary>
    ///     Handles the update process for macOS
    /// </summary>
    public static async Task HandleMacOSUpdate(UpdateInfo updateInfo, string tempPath)
    {
        // Determine architecture - Apple Silicon (ARM) vs Intel
        var isArm = RuntimeInformation.ProcessArchitecture == 
            Architecture.Arm64;
        
        // Select appropriate download URL based on architecture
        var downloadUrl = isArm ? updateInfo.MacArm64 : updateInfo.MacIntel;
        
        await DownloadAndOpenDmg(downloadUrl, tempPath).ConfigureAwait(false);
    }
    
    /// <summary>
    ///     Downloads and opens the DMG file
    /// </summary>
    private static async Task DownloadAndOpenDmg(string downloadUrl, string tempPath)
    {
        try
        {
            // Extract filename from URL
            var fileName = Path.GetFileName(downloadUrl);
            var tempFilePath = Path.Combine(tempPath, fileName);
            
            var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
            if (core is null)
            {
                return;
            }
            
            // Download the DMG file
            await UpdateManager.DownloadUpdateFile(core, downloadUrl, tempFilePath).ConfigureAwait(false);
            
            // Open the DMG file
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = "open",
                    Arguments = $"\"{tempFilePath}\"",
                }
            };
            
            process.Start();
            
            // Give some time for the DMG to mount before exiting
            await Task.Delay(1000).ConfigureAwait(false);
            
            // Close the application
            await (AppLifecycle.Current?.ShutdownAsync()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(MacUpdateHelper), nameof(DownloadAndOpenDmg), ex);
            // Updating is not done from a Main Window, so tell the user in the active one
            var vm = await Dispatcher.UIThread.InvokeAsync(() =>
                (Application.Current?.DataContext as CoreViewModel)?.MainWindows.ActiveWindow.CurrentValue);
            if (vm is not null)
            {
                TooltipHelper.ShowTooltipMessage(vm, $"Failed to download or open update: {ex.Message}");
            }
        }
    }
}