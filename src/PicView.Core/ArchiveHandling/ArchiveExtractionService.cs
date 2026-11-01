using System.Collections.Concurrent;
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
///         <item>Extract the entry to start at via <see cref="ExtractEntryAsync" /> and navigate to it.</item>
///         <item>Extract the remaining entries via <see cref="ExtractRemainingAsync" /> while the
///               <c>FileWatcherService</c> picks up created files and updates the UI.</item>
///     </list>
/// </summary>
public class ArchiveExtractionService
{
    /// <summary>
    ///     The folder the open archive is extracted to: the folder of the archive that opened last, of those whose
    ///     folders are not cleaned up yet.
    /// </summary>
    public string? TempZipDirectory => Open?.Folder;

    /// <summary>
    ///     The open archive, whose folder is <see cref="TempZipDirectory" />.
    /// </summary>
    public string? LastOpenedArchive => Open?.Archive;

    public bool IsArchived => TempZipDirectory != null;

    /// <summary>
    ///     One extraction folder: the archive extracted to it, and which entry each file in it came from.
    /// </summary>
    /// <param name="IsFullyExtracted">
    ///     Local software extracted the whole archive, keeping its folders, so the files are not in the maps: a file's path
    ///     in the folder is its entry.
    /// </param>
    private sealed record Extraction(
        string Folder,
        string Archive,
        bool IsFullyExtracted,
        ConcurrentDictionary<string, string> EntryByFile,
        ConcurrentDictionary<string, string> FileByEntry)
    {
        public Extraction(string folder, string archive, bool isFullyExtracted)
            : this(folder, archive, isFullyExtracted, new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                new ConcurrentDictionary<string, string>(StringComparer.Ordinal))
        {
        }

        public string? EntryOf(string extractedPath) =>
            EntryByFile.TryGetValue(extractedPath, out var entry)
                ? entry
                : IsFullyExtracted ? PathInFolder(extractedPath, Folder) : null;
    }

    // Held for a few in-memory operations only, never across disk access or an await
    private readonly Lock _lock = new();

    // Every extraction folder that is not cleaned up yet, the one published last at the end. A page shows from its folder
    // after another archive started opening, or after one failed to open, so each folder keeps its own archive and maps
    // until it is cleaned up. Replaced, never changed, so a display thread reads it without the lock
    private Extraction[] _extractions = [];

    private Extraction? Open => Volatile.Read(ref _extractions) is { Length: > 0 } extractions ? extractions[^1] : null;

    /// <summary>
    ///     The archive, and the entry of it, that <paramref name="extractedPath" /> was extracted from, found by the
    ///     extraction folder it is in, or null for a file outside them. Every folder counts until it is cleaned up, so a page
    ///     shown after another archive started opening, or failed to open, still counts as its own archive's. A tab asks
    ///     this for each image it shows, so it reads no disk, and allocates nothing for a file outside the folders.
    /// </summary>
    /// <returns>The archive, and its entry, which is null for a file in the folder that the service did not extract.</returns>
    public (string Archive, string? Entry)? ArchivePageOf(string extractedPath)
    {
        var extractions = Volatile.Read(ref _extractions);
        for (var i = 0; i < extractions.Length; i++)
        {
            var extraction = extractions[i];
            if (IsInFolder(extractedPath, extraction.Folder))
            {
                return (extraction.Archive, extraction.EntryOf(extractedPath));
            }
        }

        return null;
    }

    /// <summary>
    ///     The entry that <paramref name="extractedPath" /> was extracted from, or <c>null</c> for a file the service did
    ///     not extract, as <see cref="ArchivePageOf" /> finds it.
    /// </summary>
    public string? EntryOf(string extractedPath) => ArchivePageOf(extractedPath)?.Entry;

    /// <summary>
    ///     The file that <paramref name="entry" /> of the open archive was extracted to, or <c>null</c> when it is not
    ///     extracted. For a fully extracted archive it checks the disk.
    /// </summary>
    public string? ExtractedPathOf(string entry)
    {
        if (Open is not { } open)
        {
            return null;
        }

        if (open.FileByEntry.TryGetValue(entry, out var file))
        {
            return file;
        }

        if (!open.IsFullyExtracted)
        {
            return null;
        }

        var path = Path.Combine(open.Folder, entry);
        return File.Exists(path) ? path : null;
    }

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
    /// returns a sorted entry list. Only an archive that opens becomes the open one, with its folder as
    /// <see cref="TempZipDirectory" />. One that fails to open, such as a corrupt archive or one with no images, deletes
    /// its folder and leaves the open archive as it was, extracting as it did, since the tab still shows it.
    /// </summary>
    public async Task<ArchivePreparation?> PrepareArchiveAsync(
        string archivePath,
        Func<string, string, Task<bool>> extractWithLocalSoftwareAsync,
        Func<string, string, int> stringComparer)
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
        try
        {
            Directory.CreateDirectory(tempDirectory);
            if (await OpenAsync(archivePath, tempDirectory, extractWithLocalSoftwareAsync, stringComparer)
                    .ConfigureAwait(false) is { } preparation)
            {
                Publish(new Extraction(tempDirectory, archivePath, preparation.IsFullyExtracted));
                return preparation;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(PrepareArchiveAsync), ex);
        }

        DeleteFolder(tempDirectory);
        return null;
    }

    // Lists the archive's entries, or has local software extract it to the folder. Null when it does not open
    private static async Task<ArchivePreparation?> OpenAsync(
        string archivePath,
        string tempDirectory,
        Func<string, string, Task<bool>> extractWithLocalSoftwareAsync,
        Func<string, string, int> stringComparer)
    {
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
            return new ArchivePreparation(tempDirectory, entries, IsFullyExtracted: false);
        }
    }

    /// <summary>
    ///     Extracts a set of archive entries to the previously created <see cref="TempZipDirectory" />.
    ///     Entries are written flat (without preserving the entry's directory structure) so the
    ///     standard, non-recursive file listing/sorting works on the result.
    ///     The archive is opened once and entries are extracted in order.
    /// </summary>
    /// <param name="folder">
    ///     The folder to extract to, which <see cref="PrepareArchiveAsync" /> made for the archive, else the open archive's.
    ///     A load passes its own, as another archive may open meanwhile.
    /// </param>
    /// <returns>List of absolute paths of successfully extracted files in the requested order.</returns>
    public async Task<IReadOnlyList<string>> ExtractEntriesAsync(
        string archivePath,
        IReadOnlyList<string> entryKeys,
        CancellationToken ct = default,
        string? folder = null)
    {
        var tempDirectory = folder ?? TempZipDirectory;
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
                        Remember(entry.Key, extractedPath, tempDirectory);
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
    /// <param name="folder">The folder to extract to, as <see cref="ExtractEntriesAsync" /> takes it.</param>
    /// <returns>The absolute path of the extracted file, or <c>null</c> on failure.</returns>
    public async Task<string?> ExtractEntryAsync(
        string archivePath,
        string entryKey,
        CancellationToken ct = default,
        string? folder = null)
    {
        if (string.IsNullOrEmpty(entryKey))
        {
            return null;
        }

        var results = await ExtractEntriesAsync(archivePath, [entryKey], ct, folder).ConfigureAwait(false);
        return results.Count > 0 ? results[0] : null;
    }

    /// <summary>
    ///     Extracts the entries whose keys are in <paramref name="remainingKeys" />, then those in
    ///     <paramref name="laterKeys" />, to the <see cref="TempZipDirectory" /> with low priority, reporting extraction
    ///     count progress after each batch. The archive is opened once, and its entries are extracted in the order of the
    ///     keys. A solid archive is the exception: its entries are compressed as one stream, so reading them out of order
    ///     would decompress it again for each entry. It is walked in its own order instead, once for each set of keys.
    ///     It stops once its folder is cleaned up, as the tab has left the archive.
    /// </summary>
    /// <param name="folder">The folder to extract to, as <see cref="ExtractEntriesAsync" /> takes it.</param>
    public void ExtractRemaining(
        string archivePath,
        IReadOnlyList<string> remainingKeys,
        IReadOnlyList<string>? laterKeys = null,
        int initialCount = 0,
        int totalCount = 0,
        IProgress<int>? progress = null,
        int batchSize = 10,
        CancellationToken ct = default,
        string? folder = null)
    {
        var tempDirectory = folder ?? TempZipDirectory;
        laterKeys ??= [];
        if (string.IsNullOrEmpty(tempDirectory) || remainingKeys is null || remainingKeys.Count + laterKeys.Count == 0)
        {
            return;
        }

        try
        {
            using var archive = ArchiveFactory.OpenArchive(archivePath);

            var extractedCount = initialCount;
            var batchCounter = 0;
            var left = remainingKeys.Count + laterKeys.Count;

            if (archive.IsSolid)
            {
                if (ExtractInArchiveOrder(remainingKeys))
                {
                    ExtractInArchiveOrder(laterKeys);
                }
                return;
            }

            // Any other archive reads each entry on its own, so the keys set the order
            var entries = new Dictionary<string, IArchiveEntry>(StringComparer.Ordinal);
            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory && !string.IsNullOrEmpty(entry.Key))
                {
                    entries.TryAdd(entry.Key, entry);
                }
            }

            if (ExtractInKeyOrder(remainingKeys))
            {
                ExtractInKeyOrder(laterKeys);
            }
            return;

            bool ExtractInKeyOrder(IReadOnlyList<string> keys)
            {
                foreach (var key in keys)
                {
                    // Removed, so that a key given twice is extracted once
                    if (entries.Remove(key, out var entry) && !Extract(entry))
                    {
                        return false;
                    }
                }
                return true;
            }

            bool ExtractInArchiveOrder(IReadOnlyList<string> keys)
            {
                var pending = new HashSet<string>(keys, StringComparer.Ordinal);
                foreach (var entry in archive.Entries)
                {
                    if (pending.Count == 0)
                    {
                        return true;
                    }

                    if (!entry.IsDirectory && !string.IsNullOrEmpty(entry.Key) && pending.Remove(entry.Key)
                        && !Extract(entry))
                    {
                        return false;
                    }
                }
                return true;
            }

            // Whether to go on, as the extraction may be cancelled, or its folder cleaned up
            bool Extract(IArchiveEntry entry)
            {
                if (ct.IsCancellationRequested || FindExtraction(tempDirectory) is null)
                {
                    return false;
                }

                var extractedPath = WriteEntryFlat(entry, tempDirectory);
                Remember(entry.Key!, extractedPath, tempDirectory);
                extractedCount++;
                batchCounter++;
                left--;

                // Yield CPU to higher-priority UI and navigation threads
                Thread.Yield();

                // Report progress after each batch or upon completing extraction
                if (batchCounter >= batchSize || left == 0)
                {
                    batchCounter = 0;
                    progress?.Report(extractedCount);

                    // Brief pause between batches to give foreground operations maximum CPU/IO priority
                    Thread.Sleep(5);
                }
                return true;
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
        IReadOnlyList<string> remainingKeys,
        IReadOnlyList<string>? laterKeys = null,
        int initialCount = 0,
        int totalCount = 0,
        IProgress<int>? progress = null,
        int batchSize = 10,
        CancellationToken ct = default,
        string? folder = null)
    {
        // Read here, so the folder is the open archive's when this starts, not when the thread does
        folder ??= TempZipDirectory;
        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                ExtractRemaining(archivePath, remainingKeys, laterKeys, initialCount, totalCount, progress, batchSize, ct,
                    folder);
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
        var cts = new CancellationTokenSource();
        CancellationTokenSource? previous;
        lock (_lock)
        {
            previous = _extractionCts;
            _extractionCts = cts;
        }

        Cancel(previous);
        return cts.Token;
    }

    /// <summary>
    ///     Stops the background extraction, as the tab opens another archive, whose pages then come sooner.
    /// </summary>
    public void StopExtraction()
    {
        CancellationTokenSource? cts;
        lock (_lock)
        {
            cts = _extractionCts;
            _extractionCts = null;
        }

        Cancel(cts);
    }

    /// <summary>
    ///     Stops the background extraction, and deletes every extraction folder and forgets it.
    /// </summary>
    public void Cleanup()
    {
        Extraction[] extractions;
        lock (_lock)
        {
            extractions = _extractions;
            Volatile.Write(ref _extractions, []);
        }

        StopExtraction();
        foreach (var extraction in extractions)
        {
            DeleteFolder(extraction.Folder);
        }
    }

    /// <summary>
    ///     Deletes the extraction folder <paramref name="tempZipDirectory" />, and forgets the archive extracted to it and
    ///     which entry each file came from. An extraction to it stops by itself, so a newer archive's goes on.
    /// </summary>
    public void Cleanup(string? tempZipDirectory)
    {
        if (string.IsNullOrEmpty(tempZipDirectory))
        {
            return;
        }

        lock (_lock)
        {
            var index = IndexOfFolder(_extractions, tempZipDirectory);
            if (index >= 0)
            {
                Volatile.Write(ref _extractions, [.. _extractions[..index], .. _extractions[(index + 1)..]]);
            }
        }

        DeleteFolder(tempZipDirectory);
    }

    // Makes an archive that opened the open one, last in the folders
    private void Publish(Extraction extraction)
    {
        lock (_lock)
        {
            Volatile.Write(ref _extractions, [.. _extractions, extraction]);
        }
    }

    private Extraction? FindExtraction(string folder)
    {
        var extractions = Volatile.Read(ref _extractions);
        var index = IndexOfFolder(extractions, folder);
        return index >= 0 ? extractions[index] : null;
    }

    private static int IndexOfFolder(Extraction[] extractions, string folder)
    {
        for (var i = 0; i < extractions.Length; i++)
        {
            if (string.Equals(extractions[i].Folder, folder, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    // A cancelled extraction can still finish a file after its folder was cleaned up, so only a file in a folder that is
    // still there is remembered
    private void Remember(string entryKey, string extractedPath, string folder)
    {
        if (FindExtraction(folder) is not { } extraction)
        {
            return;
        }

        extraction.EntryByFile[extractedPath] = entryKey;
        extraction.FileByEntry[entryKey] = extractedPath;
    }

    // Outside the lock, since cancelling runs the token's callbacks
    private static void Cancel(CancellationTokenSource? cts)
    {
        if (cts is null)
        {
            return;
        }

        cts.Cancel();
        cts.Dispose();
    }

    private static void DeleteFolder(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(Cleanup), ex);
        }
    }

    // Compares spans, so a file recorded in every held-key view allocates nothing
    private static bool IsInFolder(string path, string folder)
    {
        var trimmed = Path.TrimEndingDirectorySeparator(folder.AsSpan());
        return path.Length > trimmed.Length + 1
               && path.AsSpan().StartsWith(trimmed, StringComparison.OrdinalIgnoreCase)
               && path[trimmed.Length] is '\\' or '/';
    }

    // The path of a file in the folder relative to it, or null for a file outside it
    private static string? PathInFolder(string path, string folder) =>
        IsInFolder(path, folder) ? path[(Path.TrimEndingDirectorySeparator(folder.AsSpan()).Length + 1)..] : null;

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
