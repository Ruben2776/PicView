using System.Collections.Concurrent;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileSorting;
using PicView.Core.Navigation;
using SharpCompress.Archives;
using ZLinq;

namespace PicView.Core.ArchiveHandling;

/// <summary>
///     The mechanics of extracting a tab's archives, which <see cref="ArchiveOpenings" /> owns (ADR-0013): one folder
///     per archive opened, kept until the tab leaves it, with how its files are named and which entry each came from;
///     listing an archive's entries, or having local software extract it whole; and extracting entries into a folder,
///     at once or in batches on a low-priority thread. Which opening is the latest, when the extraction ends and what
///     the tab is told are the owner's.
/// </summary>
/// <param name="entryWriter">Writes each entry into its file; the app's copies the bytes.</param>
internal class ArchiveExtractionService(IArchiveEntryWriter? entryWriter = null)
{
    private readonly IArchiveEntryWriter _entryWriter = entryWriter ?? ArchiveEntryWriter.Instance;

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
    ///     One extraction folder: the archive extracted to it, the name each entry's file has in it, and which entry each
    ///     file in it came from.
    /// </summary>
    /// <param name="FlatNameByEntry">
    ///     The file name each entry is extracted under, given to every entry when the archive is prepared, in key order, so
    ///     which of two pages with one name takes the suffix does not depend on which is extracted first. Empty for an
    ///     archive local software extracted, whose files are in the maps from the start.
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
        int SharedFolderCount,
        int Number,
        IReadOnlyDictionary<string, string> FlatNameByEntry,
        ConcurrentDictionary<string, string> EntryByFile,
        ConcurrentDictionary<string, string> FileByEntry)
    {
        public string? EntryOf(string extractedPath) => EntryByFile.GetValueOrDefault(extractedPath);
    }

    /// <summary>
    ///     What preparing an archive works out for its folder: how its files are named, and for an archive local software
    ///     extracted, the file each entry is.
    /// </summary>
    private sealed record Naming(
        int SharedFolderCount,
        IReadOnlyDictionary<string, string> FlatNameByEntry,
        IReadOnlyDictionary<string, string> FileByEntry);

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
    ///     The file that <paramref name="entry" /> of the open archive was extracted to, or <c>null</c> when it is not
    ///     extracted.
    /// </summary>
    public string? ExtractedPathOf(string entry) => Open?.FileByEntry.GetValueOrDefault(entry);

    /// <summary>
    ///     Represents the result of preparing an archive for staged extraction.
    /// </summary>
    /// <param name="TempDirectory">The temporary directory the archive will be extracted into.</param>
    /// <param name="EntryKeys">
    ///     The supported entry keys (as reported by the archive), in the File Ordering's order. For
    ///     <see cref="IsFullyExtracted" /> = <c>true</c> this list contains the absolute paths of
    ///     already-extracted files instead.
    /// </param>
    /// <param name="IsFullyExtracted">
    ///     When <c>true</c>, the archive has already been fully extracted (for formats handled by
    ///     local software, e.g. <c>.7z</c>), and its files laid out flat under the names the other archives' pages are
    ///     extracted under, so the File Ordering alone gives their order. Each file's entry is the path local software
    ///     wrote it at, relative to the folder. In that case <see cref="ExtractEntryAsync" /> and
    ///     <see cref="ExtractRemainingAsync" /> are no-ops.
    /// </param>
    public readonly record struct ArchivePreparation(
        string TempDirectory,
        string[] EntryKeys,
        bool IsFullyExtracted);

    /// <summary>
    /// Prepares an archive for staged extraction: creates a temporary directory, lists all
    /// supported entries (or fully extracts the archive when local software is required) and
    /// returns them in <paramref name="ordering" />'s order. Only an archive that opens becomes the open one, with its
    /// folder as <see cref="TempZipDirectory" />. One that fails to open, such as a corrupt archive or one with no images,
    /// deletes its folder and leaves the open archive as it was, extracting as it did, since the tab still shows it.
    /// </summary>
    /// <param name="ordering">
    ///     The tab's File Ordering, which the entries are put in from what the archive says of each before anything is
    ///     extracted: its name, size and modified time. By name, the archive's folders count first when the app respects
    ///     the archive structure. Random shuffles them once here, so the start page and the extraction follow the shuffle.
    /// </param>
    public async Task<ArchivePreparation?> PrepareArchiveAsync(
        string archivePath,
        Func<string, string, Task<bool>> extractWithLocalSoftwareAsync,
        FileOrdering ordering)
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
            if (await OpenAsync(archivePath, tempDirectory, extractWithLocalSoftwareAsync, ordering)
                    .ConfigureAwait(false) is (var preparation, var naming))
            {
                Publish(tempDirectory, archivePath, naming);
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
    // naming is how its files are named, as the extraction folder keeps it
    private static async Task<(ArchivePreparation Preparation, Naming Naming)?> OpenAsync(
        string archivePath,
        string tempDirectory,
        Func<string, string, Task<bool>> extractWithLocalSoftwareAsync,
        FileOrdering ordering)
    {
        var isRespectingStructure = IsRespectingStructure(ordering);
        var ext = Path.GetExtension(archivePath);
        if (ext.Equals(".7z", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".cb7", StringComparison.OrdinalIgnoreCase))
        {
            if (!await extractWithLocalSoftwareAsync(archivePath, tempDirectory).ConfigureAwait(false))
            {
                return null;
            }

            var fileByEntry = await Task.Run(() => LayOutFlat(tempDirectory, isRespectingStructure)).ConfigureAwait(false);
            if (fileByEntry.Count is 0)
            {
                return null;
            }

            // Flat under the names a zip's pages take, so this is the order the tab sorts the files in, folders and all
            var files = ordering.Sort(fileByEntry.Values.Select(path => new FileInfo(path)))
                .Select(file => file.FullName).ToArray();
            return (new ArchivePreparation(tempDirectory, files, IsFullyExtracted: true),
                new Naming(NotRespectingStructure, EmptyNames, fileByEntry));
        }

        var archive = await ArchiveFactory.OpenAsyncArchive(archivePath).ConfigureAwait(false);
        await using (archive.ConfigureAwait(false))
        {
            // What each entry says of itself in the archive's directory, read before anything is extracted. The name is
            // filled in once the shared folders are known
            var entries = await archive.EntriesAsync
                .Where(e => !e.IsDirectory
                            && !string.IsNullOrEmpty(e.Key)
                            && e.Key!.IsSupported())
                .Select(e => new ArchiveEntryOrderKey(string.Empty, e.Key!, e.Size, ModifiedTimeOf(e)))
                .ToArrayAsync().ConfigureAwait(false);

            if (entries.Length is 0)
            {
                return null;
            }

            var keys = new string[entries.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                keys[i] = entries[i].Key;
            }

            var sharedFolderCount = isRespectingStructure ? CountSharedFolders(SplitPaths(keys)) : NotRespectingStructure;
            var flatNames = AssignFlatNames(keys, sharedFolderCount);
            OrderEntries(entries, flatNames, ordering);
            for (var i = 0; i < entries.Length; i++)
            {
                keys[i] = entries[i].Key;
            }

            return (new ArchivePreparation(tempDirectory, keys, IsFullyExtracted: false),
                new Naming(sharedFolderCount, flatNames, EmptyNames));
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
    /// <param name="pageWritten">Told the path of each page once its file is written in full.</param>
    public void ExtractRemaining(
        string archivePath,
        IReadOnlyList<string> remainingKeys,
        IReadOnlyList<string>? laterKeys = null,
        int initialCount = 0,
        int totalCount = 0,
        IProgress<int>? progress = null,
        int batchSize = 10,
        CancellationToken ct = default,
        string? folder = null,
        Action<string>? pageWritten = null)
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

                // A page that fails to extract, such as a corrupt entry, is left out, and the rest are extracted
                string? path;
                try
                {
                    path = WriteEntryFlat(entry, tempDirectory, sharedFolderCount);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(ExtractRemaining), ex);
                    path = null;
                }

                if (path is not null)
                {
                    pageWritten?.Invoke(path);
                    extractedCount++;
                }
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
    ///     Asynchronously extracts remaining archive entries on a background thread below normal priority: it gives way to
    ///     showing an image, but is not starved by the gallery's thumbnails keeping every core busy, as a lowest-priority
    ///     thread was, which kept a Next waiting for the next page waiting with it.
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
        string? folder = null,
        Action<string>? pageWritten = null)
    {
        // Read here, so the folder is the open archive's when this starts, not when the thread does
        folder ??= TempZipDirectory;
        // What awaits the extraction runs on a pool thread, not on this one of lower priority
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                ExtractRemaining(archivePath, remainingKeys, laterKeys, initialCount, totalCount, progress, batchSize, ct,
                    folder, pageWritten);
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal,
            Name = "ArchiveExtractionWorker"
        };
        thread.Start();
        return tcs.Task;
    }

    private const int NotRespectingStructure = -1;

    /// <summary>
    ///     Whether images are ordered by their folder inside the archive before their file name.
    ///     Only applies when sorting by name, as the folder has no say in the other sort orders.
    /// </summary>
    private static bool IsRespectingStructure(FileOrdering ordering) =>
        Settings.Navigation.IsRespectingArchiveStructure && ordering.Order.By == SortFilesBy.Name;

    // A zip with no time holds a zero date, which reads as the first day of 1980, and a gzip a zero time, which reads as
    // 1970: neither is a time the entry was modified
    private static readonly DateTime NoModifiedTime = new(1980, 1, 1);

    /// <summary>
    ///     The time a page whose entry has none is stamped with, on all three of its times. An entry without a time sorts
    ///     as the earliest of all and ties with the others without one, which then go by name; this time does the same for
    ///     their files, so the tab's list of the files keeps the entries' order. It is below the earliest time an entry
    ///     can have, the start of 1980, in any time zone, and is not the file time zero, which leaves a time unchanged.
    /// </summary>
    internal static readonly DateTime NoModifiedTimeStamp = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    ///     When the entry was last modified, as the archive says, in UTC, or null when it does not say.
    /// </summary>
    private static DateTime? ModifiedTimeOf(IArchiveEntry entry) => ModifiedTimeOf(entry.LastModifiedTime);

    /// <summary>
    ///     The UTC instant <paramref name="lastModified" /> stands for, or null for none. The entry's key and its file's
    ///     stamp both take this one value, so an archive that mixes UTC and local entry times orders its entries the same
    ///     way by key and on disk.
    /// </summary>
    internal static DateTime? ModifiedTimeOf(DateTime? lastModified)
    {
        // Compared in the entry's own time, before any conversion, as the zero dates it is there for read as that time
        if (lastModified is not { } modified || modified <= NoModifiedTime)
        {
            return null;
        }

        // Unspecified counts as local, as the archives that give one keep local time
        return modified.Kind is DateTimeKind.Utc
            ? modified
            : DateTime.SpecifyKind(modified, DateTimeKind.Local).ToUniversalTime();
    }

    /// <summary>
    ///     Deletes every extraction folder and forgets it. An extraction to any of them stops by itself.
    /// </summary>
    public void Cleanup()
    {
        Extraction[] extractions;
        lock (_lock)
        {
            extractions = _extractions;
            Volatile.Write(ref _extractions, []);
        }

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

    /// <summary>Whether <paramref name="folder" /> is an extraction folder that is not cleaned up yet.</summary>
    public bool HasFolder(string folder) => FindExtraction(folder) is not null;

    /// <summary>
    ///     Deletes <paramref name="folder" /> once more when it was cleaned up while a page was still being written to
    ///     it, which kept the folder. Nothing happens for a folder that is still an extraction's.
    /// </summary>
    public void DeleteIfForgotten(string folder)
    {
        if (FindExtraction(folder) is null)
        {
            DeleteFolder(folder);
        }
    }

    // Makes an archive that opened the open one, last in the folders, numbered in the order the archives opened
    private void Publish(string folder, string archive, Naming naming)
    {
        var entryByFile = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var fileByEntry = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (entry, file) in naming.FileByEntry)
        {
            entryByFile[file] = entry;
            fileByEntry[entry] = file;
        }

        lock (_lock)
        {
            var extraction = new Extraction(folder, archive, naming.SharedFolderCount, ++_openedArchives,
                naming.FlatNameByEntry, entryByFile, fileByEntry);
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

    // Remembers the file's entry before the file is there, as the watcher lists a page once it is, and a tab that waits
    // for the page shows and records it at once. It remembers the entry's file once the file is written, as a step to
    // the entry looks for the file in the tab's list, which already holds a file being written. A cancelled extraction
    // can still write a file after its folder was cleaned up, so only a file in a folder that is still there is remembered
    private string WriteEntryFlat(IArchiveEntry entry, string destinationDirectory, int sharedFolderCount)
    {
        var extraction = FindExtraction(destinationDirectory);
        // Written once: an entry asked for again, as the start page can be by the extraction of the rest, is its file
        if (extraction is not null && extraction.FileByEntry.TryGetValue(entry.Key!, out var written)
                                   && File.Exists(written))
        {
            return written;
        }

        // Flat, so the folder lists without recursion, under the name the preparation gave the entry
        string destinationPath;
        if (extraction is not null && extraction.FlatNameByEntry.TryGetValue(entry.Key!, out var flatName))
        {
            destinationPath = Path.Combine(destinationDirectory, flatName);
        }
        else if (extraction is not null)
        {
            // An entry the preparation did not name has no name of its own, and a free one could be the name of an entry
            // not written yet, which would write over it. The callers log it and leave the entry out
            throw new InvalidOperationException($"The archive's preparation gave no name to the entry {entry.Key}.");
        }
        else
        {
            // The folder was cleaned up meanwhile, so nothing lists the file and any free name does
            var fileName = GetFlatFileName(entry.Key!, sharedFolderCount);
            destinationPath = Path.Combine(destinationDirectory, fileName);
            for (var counter = 1; File.Exists(destinationPath); counter++)
            {
                destinationPath = Path.Combine(destinationDirectory, SuffixedName(fileName, counter));
            }
        }

        extraction?.EntryByFile[destinationPath] = entry.Key!;
        using (var entryStream = entry.OpenEntryStream())
        using (var fileStream = File.Create(destinationPath))
        {
            _entryWriter.Write(entryStream, fileStream);
            // Written out first, so closing the file writes nothing that would change its times again
            fileStream.Flush();
            StampModifiedTime(fileStream, ModifiedTimeOf(entry));
        }

#if DEBUG
        Console.WriteLine($"Extracted: {entry.Key} -> {destinationPath}");
#endif

        extraction?.FileByEntry[entry.Key!] = destinationPath;
        return destinationPath;
    }

    /// <summary>
    ///     Gives the file its entry's modified time as its last write, creation and last access time, so the tab's list,
    ///     sorted again by any date, keeps the order the pages were put in from the entries. An entry with no time gets
    ///     <see cref="NoModifiedTimeStamp" />, so its file ties with the others without one and goes by name, as the entry
    ///     does. A time the file system refuses is left out, as the page is written all the same. Set on the open file, so
    ///     it is not opened again for each time.
    /// </summary>
    /// <param name="modified">The entry's time as <see cref="ModifiedTimeOf(DateTime?)" /> gives it, in UTC.</param>
    internal static void StampModifiedTime(FileStream file, DateTime? modified)
    {
        try
        {
            // In UTC already, from ModifiedTimeOf, so the file's times are the very value its entry was ordered by
            var utc = modified ?? NoModifiedTimeStamp;
            var handle = file.SafeFileHandle;
            File.SetCreationTimeUtc(handle, utc);
            File.SetLastWriteTimeUtc(handle, utc);
            File.SetLastAccessTimeUtc(handle, utc);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(StampModifiedTime), ex);
        }
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
    ///     Splits each path into its folders and file name.
    /// </summary>
    private static string[][] SplitPaths(string[] paths)
    {
        var segments = new string[paths.Length][];
        for (var i = 0; i < paths.Length; i++)
        {
            segments[i] = SplitPath(paths[i]);
        }
        return segments;
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyNames = new Dictionary<string, string>(StringComparer.Ordinal);

    private static string SuffixedName(string fileName, int counter) =>
        $"{Path.GetFileNameWithoutExtension(fileName)}_{counter}{Path.GetExtension(fileName)}";

    /// <summary>
    ///     Gives each entry the file name it is extracted under, as <see cref="GetFlatFileName" /> names it. Entries that
    ///     share a name, such as <c>v1/001.jpg</c> and <c>v2/001.jpg</c> with the folders left out, are told apart by a
    ///     suffix given in the keys' ordinal order: the first keeps the name, the next is <c>001_1.jpg</c>, and so on, past
    ///     any name another entry has of itself. Decided before any page is extracted, so the names, and the order the
    ///     File Ordering puts the files in, are the same whichever page is extracted first. Names compare ignoring case,
    ///     as the file system may.
    /// </summary>
    private static Dictionary<string, string> AssignFlatNames(string[] keys, int sharedFolderCount)
    {
        var ordered = (string[])keys.Clone();
        Array.Sort(ordered, StringComparer.Ordinal);

        var names = new Dictionary<string, string>(keys.Length, StringComparer.Ordinal);
        var taken = new HashSet<string>(keys.Length, StringComparer.OrdinalIgnoreCase);
        var sharing = new List<string>();
        // Each name goes to the first entry that has it, before any suffix is given, so no suffix takes another's name
        foreach (var key in ordered)
        {
            if (names.ContainsKey(key) || (sharing.Count > 0 && string.Equals(sharing[^1], key, StringComparison.Ordinal)))
            {
                continue;
            }

            var name = GetFlatFileName(key, sharedFolderCount);
            if (taken.Add(name))
            {
                names[key] = name;
            }
            else
            {
                sharing.Add(key);
            }
        }

        foreach (var key in sharing)
        {
            var name = GetFlatFileName(key, sharedFolderCount);
            var counter = 1;
            var suffixed = SuffixedName(name, counter);
            while (!taken.Add(suffixed))
            {
                suffixed = SuffixedName(name, ++counter);
            }
            names[key] = suffixed;
        }

        return names;
    }

    /// <summary>
    ///     Lays the files local software extracted to <paramref name="folder" /> out flat in it, under the names
    ///     <see cref="AssignFlatNames" /> gives their paths in it, as a zip's pages are named, so the File Ordering alone
    ///     orders them and a re-sort cannot interleave the archive's folders. Everything is moved aside first, so no file
    ///     is moved onto one not moved yet. What is not a supported file goes, with the folders.
    /// </summary>
    /// <returns>The flat file of each supported file, by its path in the folder as local software wrote it.</returns>
    private static Dictionary<string, string> LayOutFlat(string folder, bool isRespectingStructure)
    {
        var extracted = new DirectoryInfo(folder).GetFileSystemInfos();
        var aside = Path.Combine(folder, Path.GetRandomFileName());
        while (Path.Exists(aside))
        {
            aside = Path.Combine(folder, Path.GetRandomFileName());
        }

        Directory.CreateDirectory(aside);
        foreach (var item in extracted)
        {
            var target = Path.Combine(aside, item.Name);
            if (item is DirectoryInfo directory)
            {
                directory.MoveTo(target);
            }
            else
            {
                File.Move(item.FullName, target);
            }
        }

        var entries = new DirectoryInfo(aside)
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(f => f.FullName.IsSupported())
            .Select(f => Path.GetRelativePath(aside, f.FullName))
            .AsValueEnumerable()
            .ToArray();

        var sharedFolderCount = isRespectingStructure ? CountSharedFolders(SplitPaths(entries)) : NotRespectingStructure;
        var flatNames = AssignFlatNames(entries, sharedFolderCount);
        var fileByEntry = new Dictionary<string, string>(entries.Length, StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            // A page that cannot be moved, such as one whose flat name is longer than a file name can be, is left out,
            // and the rest open, as a page that fails to extract is
            try
            {
                var file = Path.Combine(folder, flatNames[entry]);
                File.Move(Path.Combine(aside, entry), file);
                fileByEntry[entry] = file;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                           or NotSupportedException)
            {
                DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(LayOutFlat), ex);
            }
        }

        DeleteLeftovers(aside);
        return fileByEntry;
    }

    // What is left once the pages are moved out goes, read-only files too. A file another process holds, as antivirus
    // or indexing may, is left for the extraction folder's own cleanup, as the pages are laid out all the same
    private static void DeleteLeftovers(string folder)
    {
        try
        {
            foreach (var item in new DirectoryInfo(folder).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
            {
                if (item.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    item.Attributes &= ~FileAttributes.ReadOnly;
                }
            }

            Directory.Delete(folder, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DebugHelper.LogDebug(nameof(ArchiveExtractionService), nameof(LayOutFlat), ex);
        }
    }

    /// <summary>
    ///     Puts the entries in the ordering's order, each compared by the name its file is extracted under, as the tab
    ///     compares the extracted files, so the entries are in the order the tab lists the pages in. When respecting the
    ///     archive structure the names keep the folders, so by name that is the folder order. Sorting by path instead
    ///     would differ wherever a folder sorts unlike its name in the file name, as <c>Vol 1 (color)</c> does before
    ///     <c>Vol 1 -</c>, and then the pages extracted nearest the start would not be the pages nearest it on screen.
    ///     Random shuffles them.
    /// </summary>
    private static void OrderEntries(ArchiveEntryOrderKey[] entries, IReadOnlyDictionary<string, string> flatNames,
        FileOrdering ordering)
    {
        for (var i = 0; i < entries.Length; i++)
        {
            entries[i] = entries[i] with { Name = flatNames[entries[i].Key] };
        }

        if (ordering.Order.By is SortFilesBy.Random)
        {
            Random.Shared.Shuffle(entries);
            return;
        }

        // The comparison ends in the key, so no two entries tie and an unstable sort cannot reorder any
        Array.Sort(entries, (a, b) => ordering.Compare(in a, in b));
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
}
