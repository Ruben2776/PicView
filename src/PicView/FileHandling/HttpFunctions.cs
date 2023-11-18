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
    public abstract class HttpFunctions
    {
        #region UI configured methods

        /// <summary>
        /// Attempts to download image and display it
        /// </summary>
        /// <param name="url"></param>
        internal static async Task LoadPicFromUrlAsync(string url)
        {
            ChangeFolder(true);

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

            var check = CheckIfLoadableString(destination);
            switch (check)
            {
                default:
                    var pic = await ImageDecoder.ReturnBitmapSourceAsync(new FileInfo(check)).ConfigureAwait(false);
                    await UpdateImage.UpdateImageAsync(url, pic,
                            Path.GetExtension(url).Contains(".gif", StringComparison.OrdinalIgnoreCase), destination)
                        .ConfigureAwait(false);
                    break;

                case "base64":
                    await UpdateImage.UpdateImageFromBase64PicAsync(check).ConfigureAwait(false);
                    break;

                case "zip":
                    await LoadPic.LoadPicFromArchiveAsync(check).ConfigureAwait(!false);
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
            Navigation.GetFileHistory ??= new FileHistory();
            Navigation.GetFileHistory.Add(url);
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
            ArchiveExtraction.CreateTempDirectory(tempPath);

            // Remove past "?" to not get file exceptions
            var index = fileName.IndexOf("?", StringComparison.InvariantCulture);
            if (index >= 0)
            {
                fileName = fileName[..index];
            }

            ArchiveExtraction.TempFilePath = tempPath + fileName;

            using (var client = new HttpClientDownloadWithProgress(url, ArchiveExtraction.TempFilePath))
            {
                if (displayProgress) // Set up progress display
                {
                    client.ProgressChanged += UpdateProgressDisplay;
                }

                await client.StartDownloadAsync().ConfigureAwait(false);
            }

            return ArchiveExtraction.TempFilePath;
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
            var percentComplete = (string)Application.Current.Resources["PercentComplete"];
            var displayProgress =
                $"{(int)totalBytesDownloaded}/{(int)totalBytesDownloaded} {(int)progressPercentage} {percentComplete}";

            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
            {
                ConfigureWindows.GetMainWindow.Title = displayProgress;
                ConfigureWindows.GetMainWindow.TitleText.Text = displayProgress;
                ConfigureWindows.GetMainWindow.TitleText.ToolTip = displayProgress;
            });
        }

        #endregion UI configured methods

        #region Logic

        public sealed class HttpClientDownloadWithProgress : IDisposable
        {
            private readonly string _downloadUrl;
            private readonly string _destinationFilePath;
            private HttpClient? _httpClient;
            private bool _disposedValue;

            public delegate void ProgressChangedHandler(long? totalFileSize, long? totalBytesDownloaded,
                double? progressPercentage);

            public event ProgressChangedHandler? ProgressChanged;

            public HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath)
            {
                _downloadUrl = downloadUrl;
                _destinationFilePath = destinationFilePath;
            }

            public async Task StartDownloadAsync()
            {
                _httpClient = new HttpClient { Timeout = TimeSpan.FromHours(6) };
                using var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead)
                    .ConfigureAwait(false);
                await DownloadFileFromHttpResponseMessage(response).ConfigureAwait(false);
            }

            private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength;
                await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await ProcessContentStream(totalBytes, contentStream).ConfigureAwait(false);
            }

            private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
            {
                var buffer = new byte[8192];
                await using var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write,
                    FileShare.None, 8192, true);
                var totalBytesRead = 0L;
                do
                {
                    var bytesRead = await contentStream.ReadAsync(buffer).ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
                    totalBytesRead += bytesRead;

                    if (!totalDownloadSize.HasValue) continue;
                    var progressPercentage = (double)totalBytesRead / totalDownloadSize.Value * 100;
                    OnProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
                } while (true);
            }

            private void OnProgressChanged(long? totalDownloadSize, long totalBytesRead, double progressPercentage)
            {
                ProgressChanged?.Invoke(totalDownloadSize, totalBytesRead, progressPercentage);
            }

            private void Dispose(bool disposing)
            {
                if (_disposedValue) return;
                if (disposing)
                {
                    _httpClient?.Dispose();
                }

                _disposedValue = true;
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