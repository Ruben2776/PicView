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
            var httpDownload = HttpNavigation.GetDownloadClient(url);
            using var client = httpDownload.Client;
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) => 
            {
                var displayProgress = HttpNavigation.GetProgressDisplay(totalFileSize, totalBytesDownloaded,
                    progressPercentage);
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
                {
                    ConfigureWindows.GetMainWindow.Title = displayProgress;
                    ConfigureWindows.GetMainWindow.TitleText.Text = displayProgress;
                    ConfigureWindows.GetMainWindow.TitleText.ToolTip = displayProgress;
                });
            };
            await client.StartDownloadAsync();
            destination = httpDownload.DownloadPath;
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
        Navigation.BackupPath = url;
    }
}