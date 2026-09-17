using System.Diagnostics;
using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.WindowBehavior;
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
        var isArm = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == 
            System.Runtime.InteropServices.Architecture.Arm64;
        
        // Select appropriate download URL based on architecture
        var downloadUrl = isArm ? updateInfo.MacArm64 : updateInfo.MacIntel;
        
        await DownloadAndOpenDmg(downloadUrl, tempPath);
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
            await UpdateManager.DownloadUpdateFile(core, downloadUrl, tempFilePath);
            
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
            await Task.Delay(1000);
            
            // Close the application
            await WindowFunctions.WindowClosingBehavior();
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(MacUpdateHelper), nameof(DownloadAndOpenDmg), ex);
            UI.TooltipHelper.ShowTooltipMessage($"Failed to download or open update: {ex.Message}");
        }
    }
}