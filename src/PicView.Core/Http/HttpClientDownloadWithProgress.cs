using System.Net;

namespace PicView.Core.Http
{
    public sealed class HttpClientDownloadWithProgress : IDisposable
    {
        public delegate void ProgressChangedHandler(long? fileSize, long? bytesDownloaded, double? progressPercentage);

        private readonly string _destinationFilePath;

        private readonly string _downloadUrl;
        private readonly HttpClient _httpClient;
        private bool _disposed;

        /// <summary>
        ///     Initializes a new instance of HttpClientDownloadWithProgress
        /// </summary>
        /// <param name="downloadUrl">URL to download from</param>
        /// <param name="destinationFilePath">Where to save the downloaded file</param>
        /// <param name="client">Optional custom HttpClient instance</param>
        public HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath, HttpClient? client = null)
        {
            _downloadUrl = downloadUrl ?? throw new ArgumentNullException(nameof(downloadUrl));
            _destinationFilePath = destinationFilePath ?? throw new ArgumentNullException(nameof(destinationFilePath));
            _httpClient = client ?? new HttpClient { Timeout = TimeSpan.FromHours(1) };
        }

        public event ProgressChangedHandler? ProgressChanged;

        /// <summary>
        ///     Starts downloading the file asynchronously
        /// </summary>
        /// <param name="ct">Token to cancel the download</param>
        /// <returns>Task representing the download operation</returns>
        /// <exception cref="HttpRequestException">Thrown when the download fails</exception>
        public async Task StartDownloadAsync(CancellationToken ct = default)
        {
            try
            {
                using var response = await _httpClient.GetAsync(
                    _downloadUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct).ConfigureAwait(false);

                await DownloadFileFromHttpResponseMessage(response, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // Clean up partial downloads
                if (!File.Exists(_destinationFilePath))
                {
                    return;
                }

                try
                {
                    File.Delete(_destinationFilePath);
                }
                catch
                {
                    /* Ignore cleanup failures */
                }

                throw;
            }
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response, CancellationToken ct)
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException($"The requested file at {_downloadUrl} was not found.",
                        _downloadUrl);
                }

                throw new HttpRequestException(
                    $"Download failed with status code {response.StatusCode}: {response.ReasonPhrase}");
            }

            var totalBytes = response.Content.Headers.ContentLength;
            var contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using (contentStream.ConfigureAwait(false))
            {
                await ProcessContentStream(totalBytes, contentStream, ct).ConfigureAwait(false);
            }
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream stream, CancellationToken ct)
        {
            const int bufferSize = 81920; // Larger buffer for better performance
            var buffer = new byte[bufferSize];
            var totalBytesRead = 0L;

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_destinationFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var fileStream = new FileStream(
                _destinationFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize,
                true);
            await using (fileStream.ConfigureAwait(false))
            {
                int bytesRead;

                do
                {
                    bytesRead = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);

                    if (bytesRead <= 0)
                    {
                        continue;
                    }

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
                    totalBytesRead += bytesRead;

                    if (totalDownloadSize.HasValue)
                    {
                        var progressPercentage = (double)totalBytesRead / totalDownloadSize.Value * 100;
                        OnProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
                    }
                    else
                    {
                        // If we don't know the total size, just report bytes downloaded
                        OnProgressChanged(null, totalBytesRead, null);
                    }
                } while (bytesRead > 0 && !ct.IsCancellationRequested);

                // Flush to ensure all data is written
                await fileStream.FlushAsync(ct).ConfigureAwait(false);

                if (ct.IsCancellationRequested)
                {
                    throw new TaskCanceledException("Download was canceled");
                }
            }
        }

        private void OnProgressChanged(long? totalDownloadSize, long totalBytesRead, double? progressPercentage)
        {
            ProgressChanged?.Invoke(totalDownloadSize, totalBytesRead, progressPercentage);
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _httpClient.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}