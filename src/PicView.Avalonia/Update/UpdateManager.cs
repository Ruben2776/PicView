using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Http;

namespace PicView.Avalonia.Update;

/// <summary>
///     JSON source generation for UpdateInfo deserialization
/// </summary>
[JsonSourceGenerationOptions(AllowTrailingCommas = true)]
[JsonSerializable(typeof(UpdateInfo))]
public partial class UpdateSourceGenerationContext : JsonSerializerContext;

/// <summary>
///     Handles application update operations
/// </summary>
public static class UpdateManager
{
    private const string PrimaryUpdateUrl = "https://picview.org/update.json";
    private const string FallbackUpdateUrl = "https://picview.netlify.app/update.json";


#if DEBUG
    // ReSharper disable once ConvertToConstant.Local
    private static readonly bool ForceUpdate = false;
#endif

    /// <summary>
    ///     Checks for updates and installs if a newer version is available
    /// </summary>
    public static async Task<bool> UpdateCurrentVersion(IPlatformSpecificUpdate platformUpdate)
    {
        // Create temporary directory for update files
        var tempPath = CreateTemporaryDirectory();
        var tempJsonPath = Path.Combine(tempPath, "update.json");

        // Check if update is needed
        Version? currentVersion;
#if DEBUG
        currentVersion = ForceUpdate ? new Version("3.0.0.3") : VersionHelper.GetAssemblyVersion();
        Debug.Assert(currentVersion != null);
#else
        currentVersion = VersionHelper.GetAssemblyVersion();
#endif
        
        var updateInfo = await DownloadAndParseUpdateInfo(tempJsonPath);
        if (updateInfo == null)
        {
            return false;
        }

        var remoteVersion = new Version(updateInfo.Version);
        if (remoteVersion <= currentVersion)
        {
            return false;
        }

        // Handle update based on platform and installation type
        await platformUpdate?.HandlePlatofrmUpdate(updateInfo, tempPath);
        return true;
    }

    /// <summary>
    ///     Creates a temporary directory for update files
    /// </summary>
    private static string CreateTemporaryDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);
        return tempPath;
    }

    /// <summary>
    ///     Downloads and parses the update information
    /// </summary>
    private static async Task<UpdateInfo?> DownloadAndParseUpdateInfo(string tempJsonPath)
    {
        // Try primary URL first, fallback to secondary if needed
        if (await DownloadUpdateJson(PrimaryUpdateUrl, tempJsonPath))
        {
            return await ParseUpdateJson(tempJsonPath);
        }

        if (!await DownloadUpdateJson(FallbackUpdateUrl, tempJsonPath))
        {
            return null;
        }

        return await ParseUpdateJson(tempJsonPath);
    }

    /// <summary>
    ///     Downloads the update JSON file
    /// </summary>
    private static async Task<bool> DownloadUpdateJson(string url, string destinationPath)
    {
        try
        {
            using var downloader = new HttpClientDownloadWithProgress(url, destinationPath);
            await downloader.StartDownloadAsync();
            return true;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(UpdateManager), nameof(DownloadUpdateJson), e);
            return false;
        }
    }

    /// <summary>
    ///     Parses the update JSON file
    /// </summary>
    private static async Task<UpdateInfo?> ParseUpdateJson(string jsonFilePath)
    {
        try
        {
            var jsonString = await File.ReadAllTextAsync(jsonFilePath);

            if (JsonSerializer.Deserialize(
                    jsonString, typeof(UpdateInfo),
                    UpdateSourceGenerationContext.Default) is UpdateInfo updateInfo)
            {
                return updateInfo;
            }

            TooltipHelper.ShowTooltipMessage("Update information is missing or corrupted.");
            return null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(UpdateManager), nameof(ParseUpdateJson), e);
            TooltipHelper.ShowTooltipMessage("Failed to parse update information: \n" + e.Message);
            return null;
        }
    }
    
    public static async Task DownloadUpdateFile(string downloadUrl, string tempPath)
    {
        var vm = UIHelper.GetMainView.DataContext as MainViewModel;
        vm.PlatformService.StopTaskbarProgress();

        using var downloader = new HttpClientDownloadWithProgress(downloadUrl, tempPath);
        try
        {
            downloader.ProgressChanged += (size, downloaded, percentage) =>
                UpdateDownloadProgress(vm, size, downloaded, percentage);

            await downloader.StartDownloadAsync();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(UpdateManager), nameof(DownloadUpdateFile), e);
            TooltipHelper.ShowTooltipMessage(e.Message);
        }
        finally
        {
            vm.PlatformService.StopTaskbarProgress();
        }
    }
    
    private static void UpdateDownloadProgress(
        MainViewModel vm,
        long? totalFileSize,
        long? totalBytesDownloaded,
        double? progressPercentage)
    {
        if (!totalFileSize.HasValue || !totalBytesDownloaded.HasValue || !progressPercentage.HasValue)
        {
            return;
        }

        vm.PlatformService.SetTaskbarProgress((ulong)totalBytesDownloaded.Value, (ulong)totalFileSize.Value);
    }
}