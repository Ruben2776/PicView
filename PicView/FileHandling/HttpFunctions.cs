using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.UILogic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using static PicView.ChangeImage.ErrorHandling;
using static PicView.UILogic.Tooltip;

namespace PicView.FileHandling
{
    public class HttpFunctions
    {
        #region UI configured methods

        /// <summary>
        /// Attemps to download image and display it
        /// </summary>
        /// <param name="path"></param>
        internal static async Task LoadPicFromURL(string url)
        {
            ChangeFolder(true);

            string destination;

            try
            {
                destination = await DownloadData(url).ConfigureAwait(false);
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine("PicWeb caught exception, message = " + e.Message);
#endif
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, async () =>
                {
                    await ReloadAsync(true).ConfigureAwait(false);
                    ShowTooltipMessage(e.Message, true);
                });

                return;
            }

            string check = CheckIfLoadableString(destination);
            switch (check)
            {
                default:
                    var pic = await ImageDecoder.ReturnBitmapSourceAsync(new FileInfo(check)).ConfigureAwait(false);
                    await UpdateImage.UpdateImageAsync(url, pic, Path.GetExtension(url).Contains(".gif", StringComparison.OrdinalIgnoreCase), destination).ConfigureAwait(false);
                    break;

                case "base64":
                    await UpdateImage.UpdateImageFromBase64PicAsync(check).ConfigureAwait(false);
                    break;

                case "zip":
                    await LoadPic.LoadPicFromArchiveAsync(check).ConfigureAwait(!false);
                    break;

                case "directory":
                case "": ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () => Unload(true)); return;
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                // Fix not having focus after drag and drop
                if (!ConfigureWindows.GetMainWindow.IsFocused)
                {
                    ConfigureWindows.GetMainWindow.Focus();
                }
            });
            if (Navigation.GetFileHistory is null)
            {
                Navigation.GetFileHistory = new FileHistory();
            }
            Navigation.GetFileHistory.Add(url);
            Navigation.InitialPath = url;
        }

        /// <summary>
        /// Downloads data from the specified URL to a temporary directory and returns the path to the downloaded file.
        /// </summary>
        /// <param name="url">The URL of the data to be downloaded.</param>
        /// <param name="displayProgress">True if a progress display should be updated during the download, otherwise false.</param>
        /// <returns>The path to the downloaded file in the temporary directory.</returns>
        internal static async Task<string> DownloadData(string url, bool displayProgress = true)
        {
            // Create temp directory
            var tempPath = Path.GetTempPath();
            var fileName = Path.GetFileName(url);
            ArchiveExtraction.CreateTempDirectory(tempPath);

            // Remove past "?" to not get file exceptions
            int index = fileName.IndexOf("?", StringComparison.InvariantCulture);
            if (index >= 0)
            {
                fileName = fileName.Substring(0, index);
            }
            ArchiveExtraction.TempFilePath = tempPath + fileName;

            using (var client = new HttpClientDownloadWithProgress(url, ArchiveExtraction.TempFilePath))
            {
                if (displayProgress) // Set up progress display
                {
                    client.ProgressChanged += async (totalFileSize, _totalBytesDownloaded, progressPercentage) =>
                    await UpdateProgressDisplay(totalFileSize, _totalBytesDownloaded, progressPercentage).ConfigureAwait(false);
                }

                await client.StartDownload().ConfigureAwait(false);
            }

            return ArchiveExtraction.TempFilePath;
        }

        /// <summary>
        /// Updates the progress display during the download.
        /// </summary>
        /// <param name="totalFileSize">The total size of the file to be downloaded.</param>
        /// <param name="totalBytesDownloaded">The total number of bytes downloaded so far.</param>
        /// <param name="progressPercentage">The percentage of the download that has been completed.</param>
        private static async Task UpdateProgressDisplay(
            long? totalFileSize, long? totalBytesDownloaded, double? progressPercentage)
        {
            if (totalFileSize.HasValue && totalBytesDownloaded.HasValue && progressPercentage.HasValue)
            {
                string percentComplete = (string)Application.Current.Resources["PercentComplete"];
                string displayProgress = $"{(int)totalBytesDownloaded}/{(int)totalBytesDownloaded} {(int)progressPercentage} {percentComplete}";

                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    () =>
                    {
                        ConfigureWindows.GetMainWindow.Title = displayProgress;
                        ConfigureWindows.GetMainWindow.TitleText.Text = displayProgress;
                        ConfigureWindows.GetMainWindow.TitleText.ToolTip = displayProgress;
                    });
            }
        }

        #endregion UI configured methods

        #region Logic

        public class HttpClientDownloadWithProgress : IDisposable
        {
            private readonly string _downloadUrl;
            private readonly string _destinationFilePath;
            private HttpClient? _httpClient;
            private bool _disposedValue;

            public delegate void ProgressChangedHandler(long? totalFileSize, long? totalBytesDownloaded, double? progressPercentage);

            public event ProgressChangedHandler? ProgressChanged;

            public HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath)
            {
                _downloadUrl = downloadUrl;
                _destinationFilePath = destinationFilePath;
            }

            public async Task StartDownload()
            {
                _httpClient = new HttpClient { Timeout = TimeSpan.FromHours(6) };
                using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    await DownloadFileFromHttpResponseMessage(response).ConfigureAwait(false);
                }
            }

            public async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength;
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                {
                    await ProcessContentStream(totalBytes, contentStream).ConfigureAwait(false);
                }
            }

            public async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
            {
                var buffer = new byte[8192];
                using (var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var totalBytesRead = 0L;
                    while (true)
                    {
                        int bytesRead = await contentStream.ReadAsync(buffer);
                        if (bytesRead == 0)
                        {
                            break;
                        }

                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
                        totalBytesRead += bytesRead;

                        if (totalDownloadSize.HasValue)
                        {
                            double progressPercentage = (double)totalBytesRead / totalDownloadSize.Value * 100;
                            OnProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
                        }
                    }
                }
            }

            protected virtual void OnProgressChanged(long? totalDownloadSize, long totalBytesRead, double progressPercentage)
            {
                ProgressChanged?.Invoke(totalDownloadSize, totalBytesRead, progressPercentage);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _httpClient?.Dispose();
                    }

                    _disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        #endregion Logic
    }
}