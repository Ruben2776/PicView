using PicView.Core.Extensions;
using PicView.Core.FileHandling;
using PicView.Core.Localization;

namespace PicView.Core.Http;

public static class HttpManager
{
    public class HttpDownload
    {
        public string DownloadPath { get; init; } = string.Empty;
        public HttpClientDownloadWithProgress? Client { get; init; }
    }
    
    /// <summary>
    /// Creates a download client and prepares temporary file path
    /// </summary>
    /// <param name="url">URL to download from</param>
    /// <returns>HttpDownload object with client and destination path</returns>
    /// <exception cref="Exception">Thrown when unable to create temp directory</exception>
    public static HttpDownload GetDownloadClient(string url)
    {
        // Create temp directory
        var tempPath = TempFileManager.GetNewTempFilePath(url);
        var fileName = GetSafeFileName(url);
        var downloadPath = Path.Combine(tempPath, fileName);

        var client = new HttpClientDownloadWithProgress(url, downloadPath);

        return new HttpDownload
        {
            DownloadPath = downloadPath,
            Client = client
        };
    }
    
    /// <summary>
    /// Gets a file name from URL that is safe to use as a file path
    /// </summary>
    /// <param name="url">URL to extract filename from</param>
    /// <returns>Safe filename</returns>
    public static string GetSafeFileName(string url)
    {
        var fileName = Path.GetFileName(url);

        // Remove query string parameters to avoid file exceptions
        var index = fileName.IndexOf('?');
        if (index >= 0)
        {
            fileName = fileName[..index];
        }

        // If filename is empty or invalid after processing, use a default name
        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            fileName = $"download_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        return fileName;
    }
    
    /// <summary>
    /// Formats download progress information for display
    /// </summary>
    /// <param name="totalFileSize">Total file size in bytes</param>
    /// <param name="totalBytesDownloaded">Downloaded bytes</param>
    /// <param name="progressPercentage">Progress percentage</param>
    /// <returns>Formatted string showing download progress</returns>
    public static string GetProgressDisplay(long? totalFileSize, long? totalBytesDownloaded, double? progressPercentage)
    {
        if (!totalFileSize.HasValue || !totalBytesDownloaded.HasValue || !progressPercentage.HasValue) 
            return string.Empty;

        var percentComplete = TranslationManager.Translation.PercentComplete;
        var downloadedMb = totalBytesDownloaded.Value.GetReadableFileSize();
        var totalMb = totalFileSize.Value.GetReadableFileSize();
        
        return $"{downloadedMb}/{totalMb} ({(int)progressPercentage.Value}% {percentComplete})";
    }
}