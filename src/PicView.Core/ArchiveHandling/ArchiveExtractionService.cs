using System.Collections.Concurrent;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileSorting;
using PicView.Core.Navigation;
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
    /// <param name="SharedFolderCount">
    ///     The number of leading folders every entry of the archive shares, which are left out of the extracted file
    ///     names, or <see cref="NotRespectingStructure" /> when files are named by their file name only. Each folder keeps
    ///     its own, so every extraction to it names its files the same way, whichever archive opened since.
    /// </param>
    /// <param name="Number">Which archive to open it is, counting from 1, as <see cref="OpenedArchives" /> counts.</param>
    private sealed record Extraction(
        string Folder,
        string Archive,
        bool IsFullyExtracted,
        int SharedFolderCount,
        int Number,
        ConcurrentDictionary<string, string> EntryByFile,
        ConcurrentDictionary<string, string> FileByEntry)
    {
        public Extraction(string folder, string archive, bool isFullyExtracted, int sharedFolderCount, int number)
            : this(folder, archive, isFullyExtracted, sharedFolderCount, number,
                new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase),
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

    private int _openedArchives;

    /// <summary>
    ///     How many archives have opened in the tab so far. A load takes it as it begins, and once its image shows, it
    ///     cleans up the folders of those archives with <see cref="CleanupOpened" />.
    /// </summary>
    public int OpenedArchives => Volatile.Read(ref _openedArchives);

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
    ///     Follows a rename of the archive <paramref name="oldPath" />: the folders extracted from it count as
    ///     <paramref name="newPath" />'s from now on, so the pages a tab shows from them record under the new name. Paths
    ///     compare ignoring case.
    /// </summary>
    public void RenameArchive(string oldPath, string newPath) =>
        RenameArchives(archive => string.Equals(archive, oldPath, StringComparison.OrdinalIgnoreCase) ? newPath : null);

    /// <summary>
    ///     Follows a rename of the folder <paramref name="oldFolder" />, as <see cref="RenameArchive" /> does for each
    ///     archive in it, subfolders included.
    /// </summary>
    public void RenameArchiveFolder(string oldFolder, string newFolder) =>
        RenameArchives(archive => FolderChange.PathInRenamedFolder(archive, oldFolder, newFolder));

    // newPathOf gives the path an archive moves to, or null for one that stays. A rename never runs on a load, so it may
    // allocate
    private void RenameArchives(Func<string, string?> newPathOf)
    {
        lock (_lock)
        {
            Extraction[]? renamed = null;
            for (var i = 0; i < _extractions.Length; i++)
            {
                if (newPathOf(_extractions[i].Archive) is { } newPath)
                {
                    renamed ??= (Extraction[])_extractions.Clone();
                    renamed[i] = _extractions[i] with { Archive = newPath };
                }
            }

            if (renamed is not null)
            {
                Volatile.Write(ref _extractions, renamed);
            }
        }
    }

    /// <summary>
    ///     The files extracted to <paramref name="folder" /> so far, each written in full, or none once the folder is
    ///     cleaned up. None for an archive that local software extracted whole, which the tab lists from the start.
    /// </summary>
    public string[] ExtractedFilesOf(string folder) =>
        FindExtraction(folder) is { } extraction ? [.. extraction.FileByEntry.Values] : [];

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
                    .ConfigureAwait(false) is (var preparation, var sharedFolderCount))
            {
                Publish(tempDirectory, archivePath, preparation.IsFullyExtracted, sharedFolderCount);
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

    // Lists the archive's entries, or has local software extract it to the folder. Null when it does not open. The
    // shared folder count is how its files are named, as the extraction folder keeps it
    private static async Task<(ArchivePreparation Preparation, int SharedFolderCount)?> OpenAsync(
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

            if (IsRespectingStructure)
            {
                SortByPath(files, SplitPaths(files, tempDirectory), stringComparer);
            }
            else
            {
                SortByFileName(files, stringComparer);
            }

            // Local software names the files itself
            return (new ArchivePreparation(tempDirectory, files, IsFullyExtracted: true), NotRespectingStructure);
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

            var sharedFolderCount = NotRespectingStructure;
            if (IsRespectingStructure)
            {
                sharedFolderCount = CountSharedFolders(SplitPaths(entries));
                SortByFlatFileName(entries, sharedFolderCount, stringComparer);
            }
            else
            {
                SortByFileName(entries, stringComparer);
            }

            return (new ArchivePreparation(tempDirectory, entries, IsFullyExtracted: false), sharedFolderCount);
        }
    }

    /// <summary>
    ///     Extracts a set of archive entries to the previously created <see cref="TempZipDirectory" />.
    ///     Entries are written flat (when respecting the archive structure, their folders are kept in the file name) so the
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
            var sharedFolderCount = SharedFolderCountOf(tempDirectory);
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

                    // A page that fails to extract, such as a corrupt entry, is left out, and the rest are extracted
                    try
                    {
                        extractedMap[entry.Key] = WriteEntryFlat(entry, tempDirectory, sharedFolderCount);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(ExtractEntriesAsync), ex);
                        keySet.Remove(entry.Key);
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
    ///     Extracts the entries whose keys are in <paramref name="remainingKeys" /> and <paramref name="laterKeys" /> to the
    ///     <see cref="TempZipDirectory" /> with low priority, reporting extraction count progress after each batch. The
    ///     archive is opened once, and its entries are extracted in the order of the keys, the two sets taking turns. A
    ///     solid archive is the exception: its entries are compressed as one stream, so reading them out of order would
    ///     decompress it again for each entry. It is walked in its own order instead, once for
    ///     <paramref name="remainingKeys" /> and then once for <paramref name="laterKeys" />. It stops once its folder is
    ///     cleaned up, as the tab has left the archive.
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
            var sharedFolderCount = SharedFolderCountOf(tempDirectory);
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

            // The two sets take turns, so after an archive opened again at a page, the pages either side of it come
            // nearest first, and paging either way soon finds its next page. The longer set finishes in its order
            for (var i = 0; i < Math.Max(remainingKeys.Count, laterKeys.Count); i++)
            {
                if ((i < remainingKeys.Count && !ExtractKey(remainingKeys[i])) ||
                    (i < laterKeys.Count && !ExtractKey(laterKeys[i])))
                {
                    return;
                }
            }
            return;

            // Removed, so that a key given twice is extracted once
            bool ExtractKey(string key) => !entries.Remove(key, out var entry) || Extract(entry);

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

                WriteEntryFlat(entry, tempDirectory, sharedFolderCount);
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
        // What awaits the extraction runs on a pool thread, not on this one of the lowest priority: the end of the
        // extraction lists pages under the lock a key press reads the list under
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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

    private const int NotRespectingStructure = -1;

    /// <summary>
    ///     Whether images are ordered by their folder inside the archive before their file name.
    ///     Only applies when sorting by name, as the folder has no say in the other sort orders.
    /// </summary>
    private static bool IsRespectingStructure =>
        Settings.Navigation.IsRespectingArchiveStructure && FileSortOrder.GetSortOrder == SortFilesBy.Name;

    // The token of the background extraction started last. Stopping or cleaning up cancels it but keeps it the latest, as
    // the tab extracts nothing after that
    private CancellationToken _latestExtraction;

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
            _latestExtraction = cts.Token;
        }

        Cancel(previous);
        return cts.Token;
    }

    /// <summary>
    ///     Whether the background extraction with <paramref name="extraction" /> is the one started last, so that no newer
    ///     archive's extraction has replaced it, whether or not it was stopped since.
    /// </summary>
    public bool IsLatestExtraction(CancellationToken extraction)
    {
        lock (_lock)
        {
            return extraction == _latestExtraction;
        }
    }

    /// <summary>
    ///     Whether <see cref="StopExtraction" /> has run since the background extraction started last. Another archive
    ///     starting to open runs it, and so does <see cref="Cleanup()" />, which a URL load and closing the tab run. The
    ///     pages it had not extracted by then never come. It is true as well when the stop came after the extraction had
    ///     finished, as the extraction stays the latest until another one starts. That is harmless where it is read, as
    ///     the tab is leaving the archive then. Leaving the archive for a file or a folder does not count:
    ///     <see cref="CleanupOpened" /> and <see cref="Cleanup(string?)" /> delete its folder, and the extraction then
    ///     ends by itself without counting as stopped.
    /// </summary>
    public bool IsLatestExtractionStopped
    {
        get
        {
            lock (_lock)
            {
                return _latestExtraction.IsCancellationRequested;
            }
        }
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

    /// <summary>
    ///     Cleans up the folders of the first <paramref name="openedArchives" /> archives that opened, those not cleaned up
    ///     yet, as <see cref="Cleanup(string?)" /> does. A load calls it with the <see cref="OpenedArchives" /> it took as it
    ///     began, once its image shows: the tab has then left the archive it was reading, and the archives older loads
    ///     opened meanwhile, whether those loads were overtaken or not. An archive that opened after the load began is not
    ///     among them, so a newer archive survives an older load.
    /// </summary>
    public void CleanupOpened(int openedArchives)
    {
        foreach (var extraction in Volatile.Read(ref _extractions))
        {
            if (extraction.Number <= openedArchives)
            {
                Cleanup(extraction.Folder);
            }
        }
    }

    // Makes an archive that opened the open one, last in the folders, numbered in the order the archives opened
    private void Publish(string folder, string archive, bool isFullyExtracted, int sharedFolderCount)
    {
        lock (_lock)
        {
            var extraction = new Extraction(folder, archive, isFullyExtracted, sharedFolderCount, ++_openedArchives);
            Volatile.Write(ref _extractions, [.. _extractions, extraction]);
        }
    }

    private Extraction? FindExtraction(string folder)
    {
        var extractions = Volatile.Read(ref _extractions);
        var index = IndexOfFolder(extractions, folder);
        return index >= 0 ? extractions[index] : null;
    }

    // How the files extracted to the folder are named, the way its archive's preparation worked it out
    private int SharedFolderCountOf(string folder) =>
        FindExtraction(folder)?.SharedFolderCount ?? NotRespectingStructure;

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

    // Remembers the file's entry before the file is there, as the watcher lists a page once it is, and a tab that waits
    // for the page shows and records it at once. It remembers the entry's file once the file is written, as a step to
    // the entry looks for the file in the tab's list, which already holds a file being written. A cancelled extraction
    // can still write a file after its folder was cleaned up, so only a file in a folder that is still there is remembered
    private string WriteEntryFlat(IArchiveEntry entry, string destinationDirectory, int sharedFolderCount)
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

        var extraction = FindExtraction(destinationDirectory);
        extraction?.EntryByFile[destinationPath] = entry.Key!;
        using (var entryStream = entry.OpenEntryStream())
        using (var fileStream = File.Create(destinationPath))
        {
            CopyEntry(entryStream, fileStream);
        }

#if DEBUG
        Console.WriteLine($"Extracted: {entry.Key} -> {destinationPath}");
#endif

        extraction?.FileByEntry[entry.Key!] = destinationPath;
        return destinationPath;
    }

    /// <summary>
    ///     Writes an entry into its file, which is already there, as <paramref name="file" /> holds it open. A test holds
    ///     the write open through it, to see the page while it is written.
    /// </summary>
    internal virtual void CopyEntry(Stream entry, FileStream file) => entry.CopyTo(file);

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

    /// <summary>
    ///     Sorts the keys by the names their files are extracted under, as the tab sorts the extracted files by name, so
    ///     the keys are in the order the tab lists the pages in. Since the names keep the folders, that is the folder order,
    ///     but sorting by path instead would differ wherever a folder sorts unlike its name in the file name, as
    ///     <c>Vol 1 (color)</c> does before <c>Vol 1 -</c>. Then the pages extracted nearest the start would not be the
    ///     pages nearest it on screen.
    /// </summary>
    private static void SortByFlatFileName(string[] keys, int sharedFolderCount, Func<string, string, int> stringComparer)
    {
        var names = new string[keys.Length];
        for (var i = 0; i < keys.Length; i++)
        {
            names[i] = GetFlatFileName(keys[i], sharedFolderCount);
        }

        var ascending = Settings.Sorting.Ascending;
        Array.Sort(names, keys, Comparer<string>.Create((a, b) => ascending ? stringComparer(a, b) : stringComparer(b, a)));
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
