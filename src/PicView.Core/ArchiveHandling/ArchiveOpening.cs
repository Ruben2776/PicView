namespace PicView.Core.ArchiveHandling;

/// <summary>
///     One Archive Opening of a tab: an archive opened at a page, with the pages extracted along with it, and the rest
///     left to extract in the background once the start page shows. Made by <see cref="ArchiveOpenings.OpenAsync" />.
/// </summary>
public sealed class ArchiveOpening
{
    private readonly ArchiveOpenings _owner;
    private readonly CancellationTokenSource _extracting = new();
    private readonly TaskCompletionSource _ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _state = (int)ExtractionState.Finished;

    internal ArchiveOpening(ArchiveOpenings owner, string archive, string folder, IReadOnlyList<FileInfo> files,
        FileInfo startFile, string[] afterKeys, string[] beforeKeys, int totalCount,
        Func<string, string, int> compareNames)
    {
        _owner = owner;
        Archive = archive;
        Folder = folder;
        Files = files;
        StartFile = startFile;
        AfterKeys = afterKeys;
        BeforeKeys = beforeKeys;
        TotalCount = totalCount;
        CompareNames = compareNames;
        if (afterKeys.Length + beforeKeys.Length is 0)
        {
            _ended.TrySetResult();
        }
    }

    /// <summary>The archive opened.</summary>
    public string Archive { get; }

    /// <summary>The folder its pages are extracted to.</summary>
    public string Folder { get; }

    /// <summary>
    ///     The pages extracted along with the start page, in the archive's order: the start page and up to nine after
    ///     it, and its side-by-side partner when that is elsewhere. A page that failed to extract is left out. For an
    ///     archive that local software extracted whole, every page.
    /// </summary>
    public IReadOnlyList<FileInfo> Files { get; }

    /// <summary>The page to show first: the entry asked for, else the first page.</summary>
    public FileInfo StartFile { get; }

    /// <summary>
    ///     Where the background extraction stands. <see cref="ExtractionState.Finished" /> before
    ///     <see cref="ExtractRest" /> runs, and when there is nothing left to extract.
    /// </summary>
    public ExtractionState State => (ExtractionState)Volatile.Read(ref _state);

    /// <summary>Completes once the background extraction has ended, finished or stopped.</summary>
    public Task Ended => _ended.Task;

    internal string[] AfterKeys { get; }
    internal string[] BeforeKeys { get; }
    internal int TotalCount { get; }
    internal Func<string, string, int> CompareNames { get; }
    internal CancellationToken ExtractionToken => _extracting.Token;
    internal bool IsStopped => _extracting.IsCancellationRequested;

    /// <summary>
    ///     Extracts the pages the first batch left out, in the background, in batches of ten: those after the batch in
    ///     order and those before the start page nearest first, taking turns. Call once the start page shows. The
    ///     opening's owner lists the pages the tab does not have yet when the extraction ends.
    /// </summary>
    public void ExtractRest() => _owner.ExtractRest(this);

    /// <summary>
    ///     Gives the opening up, as its start page never showed: a newer load overtook it. Its folder goes, and the tab's
    ///     Open Archive stays as it was.
    /// </summary>
    public void Abandon() => _owner.Abandon(this);

    private readonly List<Task> _listings = [];

    // A page being listed in the tab's files, which the end of the extraction waits for
    internal void AddListing(Task listing)
    {
        lock (_listings)
        {
            _listings.Add(listing);
        }
    }

    internal Task ListingsEnded()
    {
        Task[] listings;
        lock (_listings)
        {
            listings = [.. _listings];
        }

        return Task.WhenAll(listings);
    }

    internal void Stop()
    {
        try
        {
            _extracting.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already ended
        }
    }

    internal void SetState(ExtractionState state) => Volatile.Write(ref _state, (int)state);

    internal void End()
    {
        _ended.TrySetResult();
        _extracting.Dispose();
    }
}
