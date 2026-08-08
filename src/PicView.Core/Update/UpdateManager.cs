using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Http;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;

namespace PicView.Core.Update;

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
#pragma warning disable MA0069
#pragma warning disable CA2211
    public static bool ForceUpdate = true;
#pragma warning restore CA2211
#pragma warning restore MA0069
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
        // ReSharper disable once JoinDeclarationAndInitializer
        Version? currentVersion;
#if DEBUG
        currentVersion = ForceUpdate ? new Version("3.0.0.3") : VersionHelper.GetAssemblyVersion();
        Debug.Assert(currentVersion != null);
#else
        currentVersion = VersionHelper.GetAssemblyVersion();
#endif
        
        var updateInfo = await DownloadAndParseUpdateInfo(tempJsonPath).ConfigureAwait(false);
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
        await (platformUpdate?.HandlePlatformUpdate(updateInfo, tempPath)).ConfigureAwait(false);
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
        if (await DownloadUpdateJson(PrimaryUpdateUrl, tempJsonPath).ConfigureAwait(false))
        {
            return await ParseUpdateJson(tempJsonPath).ConfigureAwait(false);
        }

        if (!await DownloadUpdateJson(FallbackUpdateUrl, tempJsonPath).ConfigureAwait(false))
        {
            return null;
        }

        return await ParseUpdateJson(tempJsonPath).ConfigureAwait(false);
    }

    /// <summary>
    ///     Downloads the update JSON file
    /// </summary>
    private static async Task<bool> DownloadUpdateJson(string url, string destinationPath)
    {
        try
        {
            using var downloader = new HttpClientDownloadWithProgress(url, destinationPath);
            await downloader.StartDownloadAsync().ConfigureAwait(false);
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
            var jsonString = await File.ReadAllTextAsync(jsonFilePath).ConfigureAwait(false);

            if (JsonSerializer.Deserialize(
                    jsonString, typeof(UpdateInfo),UpdateSourceGenerationContext.Default) is UpdateInfo updateInfo)
            {
                return updateInfo;
            }

            return null;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(UpdateManager), nameof(ParseUpdateJson), e);
            return null;
        }
    }
    
    public static async Task DownloadUpdateFile(CoreViewModel vm, string downloadUrl, string tempPath)
    {
        vm.PlatformService.StopTaskbarProgress();

        using var downloader = new HttpClientDownloadWithProgress(downloadUrl, tempPath);
        try
        {
            downloader.ProgressChanged += (size, downloaded, percentage) =>
                UpdateDownloadProgress(vm, size, downloaded, percentage);

            await downloader.StartDownloadAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(UpdateManager), nameof(DownloadUpdateFile), e);
        }
        finally
        {
            vm.PlatformService.StopTaskbarProgress();
        }
    }
    
    private static void UpdateDownloadProgress(
        CoreViewModel vm,
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