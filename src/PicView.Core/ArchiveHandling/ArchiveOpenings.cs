using PicView.Core.DebugTools;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.ArchiveHandling;

/// <summary>
///     What a load takes as it begins, to tell <see cref="ArchiveOpenings.Shown" /> which archives the tab had opened by
///     then: those it leaves once the load's image shows.
/// </summary>
public readonly record struct ArchiveMark(int OpenedBefore);

/// <summary>
///     The Archive Openings of one tab (ADR-0013). <see cref="OpenAsync" /> opens an archive at a page: it prepares the
///     archive, extracts the start page alone, and hands back an <see cref="ArchiveOpening" /> whose
///     <see cref="ArchiveOpening.ExtractRest" /> extracts the others in the background once the start page shows. The
///     owner keeps every opening's folder until the tab leaves it, so a page shown from an older opening still counts as
///     its archive's, answers which archive and entry a shown file is a page of, follows renames, and tells the iterator
///     whether the latest extraction is extracting, finished or stopped. Everything else about extracting is its
///     implementation.
/// </summary>
public sealed class ArchiveOpenings
{
    private const int BatchSize = 10;

    private readonly TabViewModel _tab;
    private readonly ArchiveExtractionService _extraction;
    private readonly Lock _lock = new();

    // How many openings began, so an opening notices a newer one starting
    private int _openings;

    // The opening whose ExtractRest ran last, whose state State and ProgressText follow
    private ArchiveOpening? _extracting;

    private readonly BindableReactiveProperty<string?> _progressText = new(null, StringComparer.OrdinalIgnoreCase);

    public ArchiveOpenings(TabViewModel tab, IArchiveEntryWriter? entryWriter = null)
    {
        _tab = tab;
        _extraction = new ArchiveExtractionService(entryWriter);
    }

    /// <summary>The Open Archive: the archive whose page the tab shows, or null when it shows none.</summary>
    public string? OpenArchive => _extraction.LastOpenedArchive;

    /// <summary>The folder the Open Archive's pages are extracted to.</summary>
    public string? OpenFolder => _extraction.TempZipDirectory;

    public bool HasOpenArchive => _extraction.IsArchived;

    /// <summary>
    ///     Where the latest extraction stands. <see cref="ExtractionState.Finished" /> when nothing is extracting. It
    ///     leaves <see cref="ExtractionState.Extracting" /> only once every page the extraction wrote is listed, so a
    ///     list read after it has them all. Publishes each change, so the tab derives whether Next runs from it.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<ExtractionState> State => _state;

    private readonly BindableReactiveProperty<ExtractionState> _state = new(ExtractionState.Finished);

    internal void SetStateForTest(ExtractionState state) => _state.Value = state;

    /// <summary>
    ///     How far the latest extraction has come, as "extracted/total" for the UI, and null when nothing is extracting.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<string?> ProgressText => _progressText;

    /// <summary>
    ///     Whether <paramref name="folder" /> is the extraction folder of one of the tab's archives. The folder watcher
    ///     leaves such a folder alone, since the opening lists each page as it is written.
    /// </summary>
    public bool IsExtractionFolder(string folder) => _extraction.HasFolder(folder);

    /// <inheritdoc cref="ArchiveExtractionService.ArchivePageOf" />
    public (string Archive, string? Entry)? ArchivePageOf(string extractedPath) => _extraction.ArchivePageOf(extractedPath);

    /// <inheritdoc cref="ArchiveExtractionService.EntryOf" />
    public string? EntryOf(string extractedPath) => _extraction.EntryOf(extractedPath);

    /// <inheritdoc cref="ArchiveExtractionService.ExtractedPathOf" />
    public string? ExtractedPathOf(string entry) => _extraction.ExtractedPathOf(entry);

    /// <inheritdoc cref="ArchiveExtractionService.RenameArchive" />
    public void RenameArchive(string oldPath, string newPath) => _extraction.RenameArchive(oldPath, newPath);

    /// <inheritdoc cref="ArchiveExtractionService.RenameArchiveFolder" />
    public void RenameArchiveFolder(string oldFolder, string newFolder) =>
        _extraction.RenameArchiveFolder(oldFolder, newFolder);

    /// <summary>
    ///     Marks the archives the tab has opened so far, for a load that begins now. The load passes the mark to
    ///     <see cref="Shown" /> once its image shows.
    /// </summary>
    public ArchiveMark Mark() => new(_extraction.OpenedArchives);

    /// <summary>
    ///     The image of the load that took <paramref name="mark" /> shows, so the tab has left the archives it had opened
    ///     by then: the one it was reading, and those older loads opened meanwhile, overtaken or not. Their folders go.
    ///     An archive that opened after the mark, this load's own or a newer load's, stays.
    /// </summary>
    public void Shown(ArchiveMark mark) => _extraction.CleanupOpened(mark.OpenedBefore);

    /// <summary>
    ///     Stops the extraction, and deletes every folder and forgets it, as the tab leaves its archives for a URL or
    ///     closes.
    /// </summary>
    public void Cleanup()
    {
        StopExtracting();
        _extraction.Cleanup();
    }

    /// <summary>
    ///     Opens <paramref name="archive" /> at <paramref name="startEntry" />, else at its first page: lists its entries,
    ///     stops the extraction of the archive the tab leaves, and extracts the start page with its side-by-side partner,
    ///     so it shows before anything else is extracted. Null when the archive does not open, has no images, is
    ///     cancelled, or a newer opening started on the tab meanwhile; the Open Archive then stays as it was.
    /// </summary>
    /// <param name="extractWithLocalSoftwareAsync">
    ///     Extracts an archive that needs local software, such as a .7z, whole into the folder given.
    /// </param>
    /// <param name="compareNames">How the tab sorts file names, which the pages are listed by.</param>
    public async ValueTask<ArchiveOpening?> OpenAsync(string archive, string? startEntry,
        Func<string, string, Task<bool>> extractWithLocalSoftwareAsync, Func<string, string, int> compareNames,
        CancellationToken ct)
    {
        var number = Interlocked.Increment(ref _openings);
        var preparation = await _extraction.PrepareArchiveAsync(archive, extractWithLocalSoftwareAsync, compareNames)
            .ConfigureAwait(false);
        if (preparation is not { } prep)
        {
            return null;
        }

        var folder = prep.TempDirectory;
        if (IsOvertaken())
        {
            _extraction.Cleanup(folder);
            return null;
        }

        // The archive the tab leaves stops extracting, so this one's pages come sooner
        StopExtracting();

        try
        {
            if (prep.IsFullyExtracted)
            {
                // Local software wrote every file, so the pages are listed from the paths it wrote
                var files = Array.ConvertAll(prep.EntryKeys, path => new FileInfo(path));
                var startFile = startEntry is null
                    ? null
                    : Array.Find(files, f => string.Equals(_extraction.EntryOf(f.FullName), startEntry, StringComparison.Ordinal));
                return new ArchiveOpening(this, archive, folder, files, startFile ?? files[0], [], [], files.Length,
                    compareNames);
            }

            var keys = prep.EntryKeys;
            var start = Math.Max(Array.FindIndex(keys, key => string.Equals(key, startEntry, StringComparison.Ordinal)), 0);
            // The start page alone, with its side-by-side partner, so it shows before anything else is extracted: with ten
            // pages first, an archive of ten showed nothing until it was extracted whole. A start page that fails to
            // extract, such as a corrupt entry, is left out for the page after it
            var partner = SideBySidePair.PartnerOf(start, keys.Length);
            var extracted = new List<string>();
            var end = start;
            do
            {
                string[] batch = end == start ? FirstKeys(keys, start, start + 1, partner) : [keys[end]];
                extracted.AddRange(await _extraction.ExtractEntriesAsync(archive, batch, ct, folder).ConfigureAwait(false));
                end++;
            } while (extracted.Count is 0 && end < keys.Length && !IsOvertaken());

            if (extracted.Count is 0 || IsOvertaken())
            {
                _extraction.Cleanup(folder);
                return null;
            }

            var seed = extracted.Select(path => new FileInfo(path)).ToArray();
            var startPath = _extraction.ExtractedPathOf(keys[start]);
            var shown = startPath is null ? seed[0] : Array.Find(seed, f => f.FullName == startPath) ?? seed[0];
            var (afterKeys, beforeKeys) = RemainingKeys(keys, start, end, partner);
            return new ArchiveOpening(this, archive, folder, seed, shown, afterKeys, beforeKeys, keys.Length, compareNames);
        }
        catch (OperationCanceledException)
        {
            _extraction.Cleanup(folder);
            return null;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ArchiveOpenings), nameof(OpenAsync), ex);
            _extraction.Cleanup(folder);
            return null;
        }

        // The load was cancelled, or a newer opening began, which shows what the tab shows next
        bool IsOvertaken() => ct.IsCancellationRequested || Volatile.Read(ref _openings) != number;
    }

    internal void ExtractRest(ArchiveOpening opening)
    {
        // The state and progress follow this opening from here, also when it has nothing left: it is then finished, not
        // still extracting or stopped as the opening the tab leaves may be
        lock (_lock)
        {
            _extracting?.Stop();
            _extracting = opening;
        }

        if (opening.AfterKeys.Length + opening.BeforeKeys.Length is 0)
        {
            _progressText.Value = null;
            _state.Value = ExtractionState.Finished;
            return;
        }

        var firstCount = opening.Files.Count;
        var totalCount = opening.TotalCount;
        opening.SetState(ExtractionState.Extracting);
        _state.Value = ExtractionState.Extracting;
        _progressText.Value = $"{firstCount}/{totalCount}";

        // Under the lock, so a report of the last pages lands before the end clears the text, not after it
        var progress = new Progress<int>(current =>
        {
            lock (_lock)
            {
                if (ReferenceEquals(_extracting, opening) && opening.State is ExtractionState.Extracting)
                {
                    _progressText.Value = $"{current}/{totalCount}";
                }
            }
        });

        // Not on the extraction's token: an opening stopped before this runs must still end
        _ = Task.Run(async () =>
        {
            try
            {
                await _extraction.ExtractRemainingAsync(opening.Archive, opening.AfterKeys, opening.BeforeKeys,
                    firstCount, totalCount, progress, BatchSize, opening.ExtractionToken, opening.Folder,
                    path => opening.AddListing(Task.Run(() => ListPage(opening, path))))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DebugHelper.LogDebug(nameof(ArchiveOpenings), nameof(ExtractRest), ex);
            }
            finally
            {
                await EndAsync(opening).ConfigureAwait(false);
            }
        });
    }

    internal void Abandon(ArchiveOpening opening) => _extraction.Cleanup(opening.Folder);

    /// <summary>
    ///     Lists a page of <paramref name="opening" /> in the tab's files as soon as it is written, so a Next waiting for
    ///     it steps to it. Runs on a pool thread, not the low-priority extraction thread, since it takes the lock a key
    ///     press reads the list under. Only while the tab still lists the opening's pages and the opening is not stopped:
    ///     a stopped extraction may finish writing one more page, but the tab is leaving the archive, and a Next waiting
    ///     for the page gives up instead.
    /// </summary>
    private void ListPage(ArchiveOpening opening, string path)
    {
        try
        {
            if (!opening.IsStopped && _tab.ImageIterator is { } iterator)
            {
                iterator.InsertMissing([path], opening.Folder, opening.CompareNames);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ArchiveOpenings), nameof(ListPage), ex);
        }
    }

    /// <summary>
    ///     Ends the background extraction of <paramref name="opening" />, extracted or not, once every page it wrote is
    ///     listed: the tab no longer counts as extracting, and a Next waiting for a page looks at the list again. The
    ///     progress goes too, unless a newer opening's extraction has replaced it since, as when Back opens an archive
    ///     again: the tab shows that one's progress, and Next waits for its pages.
    /// </summary>
    private async Task EndAsync(ArchiveOpening opening)
    {
        await opening.ListingsEnded().ConfigureAwait(false);
        var stopped = opening.IsStopped;
        lock (_lock)
        {
            var state = stopped ? ExtractionState.Stopped : ExtractionState.Finished;
            opening.SetState(state);
            // Only the latest opening's state is published: an older one ending leaves the newer one's as it is
            if (ReferenceEquals(_extracting, opening))
            {
                _state.Value = state;
                _progressText.Value = null;
            }
        }

        // A folder cleaned up while a page was being written to it stayed, so it goes now that the write has ended
        _extraction.DeleteIfForgotten(opening.Folder);
        opening.End();
        _tab.ImageIterator?.NotifyFileAdded();
    }

    private void StopExtracting()
    {
        lock (_lock)
        {
            _extracting?.Stop();
        }
    }

    // The entries of the first batch in the archive's order: from start up to end, and the partner
    private static string[] FirstKeys(string[] keys, int start, int end, int? partner)
    {
        if (partner is not { } p)
        {
            return keys[start..end];
        }

        var first = new string[end - start + 1];
        if (p < start)
        {
            first[0] = keys[p];
            Array.Copy(keys, start, first, 1, end - start);
        }
        else
        {
            Array.Copy(keys, start, first, 0, end - start);
            first[^1] = keys[p];
        }

        return first;
    }

    /// <summary>
    ///     The entries left to extract once those from <paramref name="start" /> up to <paramref name="end" /> and
    ///     <paramref name="alsoExtracted" /> are: those after them in order, and those before the start, nearest first.
    ///     The extraction takes turns between the two, so that paging either way from the start soon finds its pages.
    /// </summary>
    private static (string[] After, string[] Before) RemainingKeys(string[] keys, int start, int end, int? alsoExtracted)
    {
        var after = new List<string>(keys.Length - end);
        for (var i = end; i < keys.Length; i++)
        {
            if (i != alsoExtracted)
            {
                after.Add(keys[i]);
            }
        }

        var before = new List<string>(start);
        for (var i = start - 1; i >= 0; i--)
        {
            if (i != alsoExtracted)
            {
                before.Add(keys[i]);
            }
        }

        return (after.ToArray(), before.ToArray());
    }
}
