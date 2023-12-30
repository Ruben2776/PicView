using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.WPF.ChangeImage;
using PicView.WPF.ImageHandling;
using PicView.WPF.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using PicView.Core.Navigation;
using static PicView.WPF.ChangeImage.ErrorHandling;
using static PicView.WPF.UILogic.Tooltip;

namespace PicView.WPF.FileHandling;

public abstract class HttpFunctions
{
    /// <summary>
    /// Attempts to download image and display it
    /// </summary>
    /// <param name="url"></param>
    internal static async Task LoadPicFromUrlAsync(string url)
    {
        ChangeFolder();

        string destination;

        try
        {
            destination = await DownloadDataAsync(url).ConfigureAwait(false);
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine("LoadPicFromUrlAsync exception = \n" + e.Message);
#endif
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(async () =>
            {
                await ReloadAsync(true).ConfigureAwait(false);
                ShowTooltipMessage(e.Message, true);
            });

            return;
        }

        var check = ErrorHelper.CheckIfLoadableString(destination);
        switch (check)
        {
            default:
                var pic = await Image2BitmapSource.ReturnBitmapSourceAsync(new FileInfo(check)).ConfigureAwait(false);
                await UpdateImage.UpdateImageAsync(url, pic,
                        Path.GetExtension(url).Contains(".gif", StringComparison.OrdinalIgnoreCase), destination)
                    .ConfigureAwait(false);
                break;

            case "base64":
                await UpdateImage.UpdateImageFromBase64PicAsync(destination).ConfigureAwait(false);
                break;

            case "zip":
                await LoadPic.LoadPicFromArchiveAsync(check).ConfigureAwait(false);
                break;

            case "directory":
            case "":
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () => Unload(true));
                return;
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            // Fix not having focus after drag and drop
            if (!ConfigureWindows.GetMainWindow.IsFocused)
            {
                ConfigureWindows.GetMainWindow.Focus();
            }
        });
        FileHistoryNavigation.Add(url);
        Navigation.InitialPath = url;
    }

    /// <summary>
    /// Downloads data from the specified URL to a temporary directory and returns the path to the downloaded file.
    /// </summary>
    /// <param name="url">The URL of the data to be downloaded.</param>
    /// <param name="displayProgress">True if a progress display should be updated during the download, otherwise false.</param>
    /// <returns>The path to the downloaded file in the temporary directory.</returns>
    internal static async Task<string> DownloadDataAsync(string url, bool displayProgress = true)
    {
        // Create temp directory
        var tempPath = Path.GetTempPath();
        var fileName = Path.GetFileName(url);
        var createTempPath = Core.FileHandling.ArchiveHelper.CreateTempDirectory(tempPath);
        if (createTempPath == false)
        {
            return TranslationHelper.GetTranslation("UnexpectedError");
        }

        // Remove past "?" to not get file exceptions
        var index = fileName.IndexOf("?", StringComparison.InvariantCulture);
        if (index >= 0)
        {
            fileName = fileName[..index];
        }

        tempPath += fileName;
        Core.FileHandling.ArchiveHelper.TempFilePath = string.Empty; // Reset it, since not browsing archive

        using var client = new HttpHelper.HttpClientDownloadWithProgress(url, tempPath);
        if (displayProgress) // Set up progress display
        {
            client.ProgressChanged += UpdateProgressDisplay;
        }

        await client.StartDownloadAsync().ConfigureAwait(false);

        return tempPath;
    }

    /// <summary>
    /// Updates the progress display during the download.
    /// </summary>
    /// <param name="totalFileSize">The total size of the file to be downloaded.</param>
    /// <param name="totalBytesDownloaded">The total number of bytes downloaded so far.</param>
    /// <param name="progressPercentage">The percentage of the download that has been completed.</param>
    private static void UpdateProgressDisplay(long? totalFileSize, long? totalBytesDownloaded,
        double? progressPercentage)
    {
        if (!totalFileSize.HasValue || !totalBytesDownloaded.HasValue || !progressPercentage.HasValue) return;
        var percentComplete = TranslationHelper.GetTranslation("PercentComplete");
        var displayProgress =
            $"{(int)totalBytesDownloaded}/{(int)totalBytesDownloaded} {(int)progressPercentage} {percentComplete}";

        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
        {
            ConfigureWindows.GetMainWindow.Title = displayProgress;
            ConfigureWindows.GetMainWindow.TitleText.Text = displayProgress;
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = displayProgress;
        });
    }
}