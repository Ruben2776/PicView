using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileSorting;
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

            _extractionCts?.Cancel();
            _extractionCts?.Dispose();
            _extractionCts = null;
            _sharedFolderCount = NotRespectingStructure;

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

                if (IsRespectingStructure)
                {
                    SortByPath(files, SplitPaths(files, tempDirectory), stringComparer);
                }
                else
                {
                    SortByFileName(files, stringComparer);
                }

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

                if (IsRespectingStructure)
                {
                    var segments = SplitPaths(entries);
                    SortByPath(entries, segments, stringComparer);
                    _sharedFolderCount = CountSharedFolders(segments);
                }
                else
                {
                    SortByFileName(entries, stringComparer);
                }

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
    ///     Entries are written flat (when respecting the archive structure, their folders are kept in the file name) so the
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
            var sharedFolderCount = _sharedFolderCount;
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

                    var extractedPath = WriteEntryFlat(entry, tempDirectory, sharedFolderCount);
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
    ///     Entries are written flat (when respecting the archive structure, their folders are kept in the file name) so the
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
            var sharedFolderCount = _sharedFolderCount;
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

                WriteEntryFlat(entry, tempDirectory, sharedFolderCount);
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

    private const int NotRespectingStructure = -1;

    /// <summary>
    ///     The number of leading folders every entry of the prepared archive shares, which are left out of the
    ///     extracted file names, or <see cref="NotRespectingStructure" /> when files are named by their file name only.
    ///     Set by <see cref="PrepareArchiveAsync" /> from the full entry list, so every extraction call names the
    ///     files the same way.
    /// </summary>
    private int _sharedFolderCount = NotRespectingStructure;

    /// <summary>
    ///     Whether images are ordered by their folder inside the archive before their file name.
    ///     Only applies when sorting by name, as the folder has no say in the other sort orders.
    /// </summary>
    private static bool IsRespectingStructure =>
        Settings.Navigation.IsRespectingArchiveStructure && FileSortOrder.GetSortOrder == SortFilesBy.Name;

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
        _extractionCts?.Cancel();
        _extractionCts?.Dispose();
        _extractionCts = null;

        var tempDirectory = TempZipDirectory;
        if (!string.IsNullOrEmpty(tempDirectory) && Directory.Exists(tempDirectory))
        {
            try
            {
                Directory.Delete(tempDirectory, true);
            }
            catch (Exception ex)
            {
                DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(Cleanup), ex);
            }
        }

        TempZipDirectory = null;
        LastOpenedArchive = null;
        _sharedFolderCount = NotRespectingStructure;
    }
    
    public void Cleanup(string? tempZipDirectory)
    {
        if (string.IsNullOrEmpty(tempZipDirectory))
        {
            return;
        }

        if (string.Equals(tempZipDirectory, TempZipDirectory, StringComparison.OrdinalIgnoreCase))
        {
            Cleanup();
            return;
        }

        try
        {
            if (Directory.Exists(tempZipDirectory))
            {
                Directory.Delete(tempZipDirectory, true);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(Cleanup), ex);
        }
    }

    private static string WriteEntryFlat(IArchiveEntry entry, string destinationDirectory, int sharedFolderCount)
    {
        // Flatten so the temp directory ends up with a flat layout that the rest of the navigation
        // pipeline can list without enabling recursive enumeration.
        var fileName = GetFlatFileName(entry.Key!, sharedFolderCount);

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

    /// <summary>
    ///     Gets the name an entry is extracted under. When respecting the archive structure, the entry's folders
    ///     are kept in the name, so <c>Volume 01/001.jpg</c> becomes <c>Volume 01 - 001.jpg</c> and sorting the
    ///     flat folder by name gives the archive's folder-then-file order. Otherwise, it is just the file name.
    /// </summary>
    private static string GetFlatFileName(string key, int sharedFolderCount)
    {
        var segments = sharedFolderCount is NotRespectingStructure ? [] : SplitPath(key);
        if (segments.Length <= sharedFolderCount || segments.Length is 0)
        {
            var fileName = Path.GetFileName(key);
            return string.IsNullOrEmpty(fileName) ? key : fileName;
        }

        return string.Join(" - ", segments, sharedFolderCount, segments.Length - sharedFolderCount);
    }

    private static string[] SplitPath(string path) =>
        path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    ///     Splits each path into its folders and file name, relative to <paramref name="root" /> when given.
    /// </summary>
    private static string[][] SplitPaths(string[] paths, string? root = null)
    {
        var segments = new string[paths.Length][];
        for (var i = 0; i < paths.Length; i++)
        {
            segments[i] = SplitPath(root is null ? paths[i] : Path.GetRelativePath(root, paths[i]));
        }
        return segments;
    }

    /// <summary>
    ///     Sorts the paths by folder, then file name, comparing one path segment at a time, so
    ///     <c>Volume 01/003.jpg</c> comes before <c>Volume 02/001.jpg</c>. Descending reverses the whole order.
    /// </summary>
    private static void SortByPath(string[] paths, string[][] segments, Func<string, string, int> stringComparer)
    {
        var ascending = Settings.Sorting.Ascending;
        Array.Sort(segments, paths, Comparer<string[]>.Create((a, b) =>
            ascending ? CompareSegments(a, b, stringComparer) : CompareSegments(b, a, stringComparer)));
    }

    private static int CompareSegments(string[] a, string[] b, Func<string, string, int> stringComparer)
    {
        var count = Math.Min(a.Length, b.Length);
        for (var i = 0; i < count; i++)
        {
            var result = stringComparer(a[i], b[i]);
            if (result != 0)
            {
                return result;
            }
        }
        return a.Length.CompareTo(b.Length);
    }

    /// <summary>
    ///     Counts the leading folders that every path shares, such as the single <c>Comic</c> folder many archives
    ///     wrap their contents in. Repeating it in every file name would add nothing.
    /// </summary>
    private static int CountSharedFolders(string[][] segments)
    {
        var shared = int.MaxValue;
        foreach (var path in segments)
        {
            // The last segment is the file name, which is never shared.
            shared = Math.Min(shared, path.Length - 1);
        }

        for (var i = 0; i < shared; i++)
        {
            var folder = segments[0][i];
            foreach (var path in segments)
            {
                if (!string.Equals(path[i], folder, StringComparison.Ordinal))
                {
                    return i;
                }
            }
        }
        return Math.Max(shared, 0);
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
