using PicView.Core.Gallery;

namespace PicView.Core.Navigation;

/// <summary>
/// Reads a changed file's details once, the first time a tab asks, and gives every tab the same result.
/// </summary>
/// <param name="waitUntilReady">Whether to wait until the file can be read, as a file still being written cannot.</param>
public sealed class FileDetailsSource(FileInfo file, bool waitUntilReady)
{
    private readonly Lock _lock = new();
    private Task<GalleryFileDetails?>? _details;

    /// <returns>The details, or null when the file could not be read as an image.</returns>
    public Task<GalleryFileDetails?> GetAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            return _details ??= ReadAsync(ct);
        }
    }

    private async Task<GalleryFileDetails?> ReadAsync(CancellationToken ct)
    {
        if (waitUntilReady)
        {
            await WaitForFileReadyAsync(file.FullName, ct: ct).ConfigureAwait(false);
        }
        return GalleryFileDetails.Read(file);
    }

    private static async ValueTask<bool> WaitForFileReadyAsync(string path, int maxWaitMs = 2000, CancellationToken ct = default)
    {
        var startTime = Environment.TickCount64;
        while (Environment.TickCount64 - startTime < maxWaitMs)
        {
            if (ct.IsCancellationRequested)
            {
                return false;
            }

            try
            {
                if (System.IO.File.Exists(path))
                {
                    var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    await using (fs.ConfigureAwait(false))
                    {
                        if (fs.Length > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (IOException)
            {
                // File locked by writer
            }
            catch (UnauthorizedAccessException)
            {
                // Access restriction or transient lock
            }

            try
            {
                await Task.Delay(50, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return false;
    }
}
