namespace PicView.Core.FileHandling;

public static class HttpHelper
{
    public sealed class HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath) : IDisposable
    {
        private HttpClient? _httpClient;
        private bool _disposedValue;

        public delegate void ProgressChangedHandler(long? totalFileSize, long? totalBytesDownloaded,
            double? progressPercentage);

        public event ProgressChangedHandler? ProgressChanged;

        public async Task StartDownloadAsync()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromHours(6) };
            using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead)
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
            const int bufferSize = 8192;
            var buffer = new byte[bufferSize];
            await using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write,
                FileShare.None, bufferSize, true);
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
}