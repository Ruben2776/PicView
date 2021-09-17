using PicView.ChangeImage;
using PicView.UILogic;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static PicView.ChangeImage.Error_Handling;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.Tooltip;

namespace PicView.FileHandling
{
    public class WebFunctions
    {
        #region UI configured methods

        /// <summary>
        /// Attemps to download image and display it
        /// </summary>
        /// <param name="path"></param>
        internal static async Task PicWeb(string url)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {
                ConfigureWindows.GetMainWindow.TitleText.Text = Application.Current.Resources["Loading"] as string;
            }));


            CanNavigate = false;
            Error_Handling.ChangeFolder(true);

            try
            {
                var destination = await DownloadData(url, true).ConfigureAwait(false);
                var isGif = Path.GetExtension(url).Contains(".gif", StringComparison.OrdinalIgnoreCase);

                await PicAsync(destination, url, isGif).ConfigureAwait(false);

                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    // Fix not having focus after drag and drop
                    if (!ConfigureWindows.GetMainWindow.IsFocused)
                    {
                        ConfigureWindows.GetMainWindow.Focus();
                    }
                }));
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine("PicWeb caught exception, message = " + e.Message);
#endif
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(async () =>
                {
                    await ReloadAsync(true).ConfigureAwait(false);
                    ShowTooltipMessage(e.Message, true);
                }));

                return;
            }
        }

        internal static async Task<string> DownloadData(string url, bool displayProgress)
        {
            // Create temp directory
            var tempPath = Path.GetTempPath();
            var fileName = Path.GetFileName(url);
            ArchiveExtraction.CreateTempDirectory(tempPath);

            // remove past "?" to not get file exceptions
            int index = fileName.IndexOf("?", StringComparison.InvariantCulture);
            if (index >= 0)
            {
                fileName = fileName.Substring(0, index);
            }

            using (var client = new WebFunctions.HttpClientDownloadWithProgress(url, tempPath + fileName))
            {
                if (displayProgress)
                {
                    client.ProgressChanged += async (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                    {
                        await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
                        {
                            if (totalBytesDownloaded == totalFileSize)
                            {
                                if (ConfigureWindows.GetMainWindow.MainImage.Source != null)
                                {
                                    int w = (int)ConfigureWindows.GetMainWindow.MainImage.Source.Width;
                                    int h = (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height;
                                    SetTitle.SetTitleString(w, h, fileName);
                                }
                            }
                            else
                            {
                                ConfigureWindows.GetMainWindow.Title = ConfigureWindows.GetMainWindow.TitleText.Text =
                                    $"{totalBytesDownloaded} / {totalFileSize} {progressPercentage} {Application.Current.Resources["PercentComplete"]}";

                                ConfigureWindows.GetMainWindow.TitleText.ToolTip = ConfigureWindows.GetMainWindow.Title;
                            }
                        }));
                    };
                }

                await client.StartDownload().ConfigureAwait(false);
            }

            return tempPath + fileName;
        }

        #endregion UI configured methods

        #region Logic

        public class HttpClientDownloadWithProgress : IDisposable
        {
            private readonly string _downloadUrl;
            private readonly string _destinationFilePath;

            private HttpClient _httpClient;
            private bool disposedValue;

            public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

            public event ProgressChangedHandler ProgressChanged;

            public HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath)
            {
                _downloadUrl = downloadUrl;
                _destinationFilePath = destinationFilePath;
            }

            public async Task StartDownload()
            {
                _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1) };

                using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    await DownloadFileFromHttpResponseMessage(response).ConfigureAwait(false);
            }

            public async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                    await ProcessContentStream(totalBytes, contentStream).ConfigureAwait(false);
            }

            public async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
            {
                var totalBytesRead = 0L;
                var readCount = 0L;
                var buffer = new byte[8192];
                var isMoreToRead = true;

                using (var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    do
                    {
                        var bytesRead = await contentStream.ReadAsync(buffer);
                        if (bytesRead == 0)
                        {
                            isMoreToRead = false;
                            TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                            continue;
                        }

                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

                        totalBytesRead += bytesRead;
                        readCount += 1;

                        if (readCount % 100 == 0)
                        {
                            TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                        }
                    }
                    while (isMoreToRead);
                }
            }

            private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
            {
                if (ProgressChanged == null)
                {
                    return;
                }

                double? progressPercentage = null;
                if (totalDownloadSize.HasValue)
                {
                    progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);
                }

                ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects)
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        #endregion Logic
    }
}