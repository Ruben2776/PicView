using PicView.Core.FileHandling;
using PicView.Core.Localization;

namespace PicView.Core.Navigation;

public static class HttpNavigation
{
    public struct HttpDownload
    {
        public string DownloadPath { get; init; }
        public HttpHelper.HttpClientDownloadWithProgress? Client { get; init; }
    }
    
    public static HttpDownload GetDownloadClient(string url)
    {
        // Create temp directory
        var createTempPath = TempFileHelper.CreateTempDirectory();
        var tempPath = TempFileHelper.TempFilePath;
        if (createTempPath == false)
        {
            throw new Exception(TranslationHelper.GetTranslation("UnexpectedError"));
        }
        
        var fileName = Path.GetFileName(url);

        // Remove past "?" to not get file exceptions
        var index = fileName.IndexOf('?');
        if (index >= 0)
        {
            fileName = fileName[..index];
        }

        tempPath = Path.Combine(tempPath, fileName);
        TempFileHelper.TempFilePath = string.Empty; // Reset it, since not browsing archive

        var client = new HttpHelper.HttpClientDownloadWithProgress(url, tempPath);

        return new HttpDownload
        {
            DownloadPath = tempPath,
            Client = client
        };
    }
    
    public static string GetProgressDisplay(long? totalFileSize, long? totalBytesDownloaded,
        double? progressPercentage)
    {
        if (!totalFileSize.HasValue || !totalBytesDownloaded.HasValue || !progressPercentage.HasValue) 
            return string.Empty;

        var percentComplete = TranslationHelper.Translation.PercentComplete;
        var displayProgress =
            $"{(int)totalBytesDownloaded}/{(int)totalBytesDownloaded} {(int)progressPercentage} {percentComplete}";

        return displayProgress;
    }
}
