using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using SharpCompress.Archives;
using ZLinq;

namespace PicView.Core.ArchiveHandling;

/// <summary>
///     Provides instance-based methods for staged extraction of supported files from an archive.
///     Each instance maintains its own temporary directory and extraction state, allowing
///     per-tab isolation of archive operations.
///     <list type="number">
///         <item>List all supported entries via <see cref="PrepareArchiveAsync" />.</item>
///         <item>Sort the returned entry keys.</item>
///         <item>Extract the first entry via <see cref="ExtractEntryAsync" /> and navigate to it.</item>
///         <item>Extract the remaining entries via <see cref="ExtractRemainingAsync" /> while the
///               <c>FileWatcherService</c> picks up created files and updates the UI.</item>
///     </list>
/// </summary>
public class ArchiveExtractionService
{
    /// <summary>
    ///     Gets the path of the temporary directory where the archive contents are extracted.
    /// </summary>
    public string? TempZipDirectory { get; private set; }

    public string? LastOpenedArchive { get; private set; }

    public bool IsArchived => TempZipDirectory != null;

    /// <summary>
    ///     Represents the result of preparing an archive for staged extraction.
    /// </summary>
    /// <param name="TempDirectory">The temporary directory the archive will be extracted into.</param>
    /// <param name="EntryKeys">
    ///     Sorted list of supported entry keys (as reported by the archive). For
    ///     <see cref="IsFullyExtracted" /> = <c>true</c> this list contains the absolute paths of
    ///     already-extracted files instead.
    /// </param>
    /// <param name="IsFullyExtracted">
    ///     When <c>true</c>, the archive has already been fully extracted (for formats handled by
    ///     local software, e.g. <c>.7z</c>). In that case <see cref="ExtractEntryAsync" /> and
    ///     <see cref="ExtractRemainingAsync" /> are no-ops.
    /// </param>
    public readonly record struct ArchivePreparation(
        string TempDirectory,
        string[] EntryKeys,
        bool IsFullyExtracted);

    /// <summary>
    /// Prepares an archive for staged extraction: creates a temporary directory, lists all
    /// supported entries (or fully extracts the archive when local software is required) and
    /// returns a sorted entry list.
    /// </summary>
    public async Task<ArchivePreparation?> PrepareArchiveAsync(
        string archivePath,
        Func<string, string, Task<bool>> extractWithLocalSoftwareAsync,
        Func<string, string, int> stringComparer)
    {
        try
        {
            if (string.IsNullOrEmpty(archivePath) || !File.Exists(archivePath))
            {
#if DEBUG
                DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(PrepareArchiveAsync),
                    "The archive path is invalid or the file does not exist.");
#endif
                return null;
            }

            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            TempZipDirectory = tempDirectory;

            var ext = Path.GetExtension(archivePath);
            if (ext.Equals(".7z", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".cb7", StringComparison.OrdinalIgnoreCase))
            {
                if (!await extractWithLocalSoftwareAsync(archivePath, tempDirectory).ConfigureAwait(false))
                {
                    return null;
                }

                // Enumerate the (potentially nested) directory tree for any supported files.
                var files = await Task.Run(() => new DirectoryInfo(tempDirectory)
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .Where(f => f.FullName.IsSupported())
                    .Select(f => f.FullName)
                    .AsValueEnumerable()
                    .ToArray()).ConfigureAwait(false);

                if (files.Length is 0)
                {
                    return null;
                }

                SortByFileName(files, stringComparer);

                LastOpenedArchive = archivePath;
                return new ArchivePreparation(tempDirectory, files, IsFullyExtracted: true);
            }

            var archive = await ArchiveFactory.OpenAsyncArchive(archivePath).ConfigureAwait(false);
            await using (archive.ConfigureAwait(false))
            {
                var entries = await archive.EntriesAsync
                    .Where(e => !e.IsDirectory
                                && !string.IsNullOrEmpty(e.Key)
                                && e.Key!.IsSupported())
                    .Select(e => e.Key!).ToArrayAsync().ConfigureAwait(false);

                if (entries.Length is 0)
                {
                    return null;
                }

                SortByFileName(entries, stringComparer);

                LastOpenedArchive = archivePath;
                return new ArchivePreparation(tempDirectory, entries, IsFullyExtracted: false);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(PrepareArchiveAsync), ex);
            return null;
        }
    }

    /// <summary>
    ///     Extracts a set of archive entries to the previously created <see cref="TempZipDirectory" />.
    ///     Entries are written flat (without preserving the entry's directory structure) so the
    ///     standard, non-recursive file listing/sorting works on the result.
    ///     The archive is opened once and entries are extracted in order.
    /// </summary>
    /// <returns>List of absolute paths of successfully extracted files in the requested order.</returns>
    public async Task<IReadOnlyList<string>> ExtractEntriesAsync(
        string archivePath,
        IReadOnlyList<string> entryKeys,
        CancellationToken ct = default)
    {
        var tempDirectory = TempZipDirectory;
        if (string.IsNullOrEmpty(tempDirectory) || entryKeys is null || entryKeys.Count == 0)
        {
            return [];
        }

        try
        {
            var keySet = new HashSet<string>(entryKeys, StringComparer.Ordinal);
            var extractedMap = new Dictionary<string, string>(StringComparer.Ordinal);

            // Open archive once and extract matching entries
            var archive = await ArchiveFactory.OpenAsyncArchive(archivePath, cancellationToken: ct).ConfigureAwait(false);
            await using (archive.ConfigureAwait(false))
            {
                await foreach (var entry in archive.EntriesAsync.WithCancellation(ct).ConfigureAwait(false))
                {
                    if (entry.IsDirectory || string.IsNullOrEmpty(entry.Key))
                    {
                        continue;
                    }

                    if (!keySet.Contains(entry.Key))
                    {
                        continue;
                    }

                    var extractedPath = WriteEntryFlat(entry, tempDirectory);
                    if (!string.IsNullOrEmpty(extractedPath))
                    {
                        extractedMap[entry.Key] = extractedPath;
                    }

                    // Stop early once all requested keys have been extracted
                    if (extractedMap.Count == keySet.Count)
                    {
                        break;
                    }
                }
            }

            // Return paths in the original requested order
            var result = new List<string>(entryKeys.Count);
            foreach (var key in entryKeys)
            {
                if (extractedMap.TryGetValue(key, out var path))
                {
                    result.Add(path);
                }
            }
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(ExtractEntriesAsync), ex);
            return [];
        }
    }

    /// <summary>
    ///     Extracts a single archive entry to the previously created <see cref="TempZipDirectory" />.
    ///     Entries are written flat (without preserving the entry's directory structure) so the
    ///     standard, non-recursive file listing/sorting works on the result.
    /// </summary>
    /// <returns>The absolute path of the extracted file, or <c>null</c> on failure.</returns>
    public async Task<string?> ExtractEntryAsync(
        string archivePath,
        string entryKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(entryKey))
        {
            return null;
        }

        var results = await ExtractEntriesAsync(archivePath, [entryKey], ct).ConfigureAwait(false);
        return results.Count > 0 ? results[0] : null;
    }

    /// <summary>
    ///     Extracts every entry whose key is present in <paramref name="remainingKeys" /> to the
    ///     <see cref="TempZipDirectory" /> in batches with low priority. The archive is opened once and walked in its native
    ///     order, reporting extraction count progress after each batch.
    /// </summary>
    public void ExtractRemaining(
        string archivePath,
        IReadOnlyCollection<string> remainingKeys,
        int initialCount = 0,
        int totalCount = 0,
        IProgress<int>? progress = null,
        int batchSize = 10,
        CancellationToken ct = default)
    {
        var tempDirectory = TempZipDirectory;
        if (string.IsNullOrEmpty(tempDirectory) || remainingKeys is null || remainingKeys.Count == 0)
        {
            return;
        }

        try
        {
            var pending = new HashSet<string>(remainingKeys, StringComparer.Ordinal);
            using var archive = ArchiveFactory.OpenArchive(archivePath);

            var extractedCount = initialCount;
            var batchCounter = 0;

            foreach (var entry in archive.Entries)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                if (entry.IsDirectory || string.IsNullOrEmpty(entry.Key))
                {
                    continue;
                }

                if (!pending.Remove(entry.Key))
                {
                    continue;
                }

                WriteEntryFlat(entry, tempDirectory);
                extractedCount++;
                batchCounter++;

                // Yield CPU to higher-priority UI and navigation threads
                Thread.Yield();

                // Report progress after each batch or upon completing extraction
                if (batchCounter >= batchSize || pending.Count == 0)
                {
                    batchCounter = 0;
                    progress?.Report(extractedCount);

                    // Brief pause between batches to give foreground operations maximum CPU/IO priority
                    Thread.Sleep(5);
                }

                if (pending.Count == 0)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignored: cleanup happens elsewhere
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(ExtractRemaining), ex);
        }
    }

    /// <summary>
    ///     Asynchronously extracts remaining archive entries on a lowest-priority background thread.
    /// </summary>
    public Task ExtractRemainingAsync(
        string archivePath,
        IReadOnlyCollection<string> remainingKeys,
        int initialCount = 0,
        int totalCount = 0,
        IProgress<int>? progress = null,
        int batchSize = 10,
        CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                ExtractRemaining(archivePath, remainingKeys, initialCount, totalCount, progress, batchSize, ct);
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.Lowest,
            Name = "ArchiveExtractionWorker"
        };
        thread.Start();
        return tcs.Task;
    }

    private CancellationTokenSource? _extractionCts;

    /// <summary>
    ///     Resets and returns a dedicated cancellation token for background extraction operations.
    /// </summary>
    public CancellationToken ResetExtractionCts()
    {
        _extractionCts?.Cancel();
        _extractionCts?.Dispose();
        _extractionCts = new CancellationTokenSource();
        return _extractionCts.Token;
    }

    /// <summary>
    ///     Deletes the temporary directory created during extraction, if it exists.
    /// </summary>
    public void Cleanup()
    {
        Cleanup(TempZipDirectory);
    }
    
    public void Cleanup(string? tempZipDirectory)
    {
        try
        {
            _extractionCts?.Cancel();
            _extractionCts?.Dispose();
            _extractionCts = null;

            if (string.IsNullOrEmpty(tempZipDirectory) || !Directory.Exists(tempZipDirectory))
            {
                return;
            }

            Directory.Delete(tempZipDirectory, true);
            if (string.Equals(tempZipDirectory, TempZipDirectory, StringComparison.OrdinalIgnoreCase))
            {
                TempZipDirectory = null;
                LastOpenedArchive = null;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(Cleanup), ex);
        }
    }

    private static string WriteEntryFlat(IArchiveEntry entry, string destinationDirectory)
    {
        // Flatten: use just the file name portion of the key so the temp directory
        // ends up with a flat layout that the rest of the navigation pipeline can list
        // without enabling recursive enumeration.
        var fileName = Path.GetFileName(entry.Key!);
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = entry.Key!;
        }

        var destinationPath = Path.Combine(destinationDirectory, fileName);

        // Handle name collisions caused by flattening by appending a numeric suffix.
        if (File.Exists(destinationPath))
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var counter = 1;
            do
            {
                destinationPath = Path.Combine(destinationDirectory, $"{name}_{counter++}{extension}");
            } while (File.Exists(destinationPath));
        }

        using var entryStream = entry.OpenEntryStream();
        using var fileStream = File.Create(destinationPath);
        entryStream.CopyTo(fileStream);

#if DEBUG
        Console.WriteLine($"Extracted: {entry.Key} -> {destinationPath}");
#endif

        return destinationPath;
    }

    private static void SortByFileName(string[] paths, Func<string, string, int> stringComparer)
    {
        if (!Settings.Sorting.Name)
        {
            return;
        }
        if (Settings.Sorting.Ascending)
        {
            paths.Sort((a, b) => stringComparer(Path.GetFileName(a), Path.GetFileName(b)));
        }
        else
        {
            paths.Sort((a, b) => stringComparer(Path.GetFileName(b), Path.GetFileName(a)));
        }
    }
}
