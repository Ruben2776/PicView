namespace PicView.Core.ArchiveHandling;

/// <summary>
///     How far the latest Archive Opening of a tab has got with extracting the pages after its first batch. A Next past
///     the last listed page waits for the page while <see cref="Extracting" />, steps on the whole list once
///     <see cref="Finished" />, and gives up when <see cref="Stopped" />, since the list stays as it is then.
/// </summary>
public enum ExtractionState
{
    /// <summary>Every page is extracted, or nothing was extracting.</summary>
    Finished,

    /// <summary>Pages are still being extracted in the background.</summary>
    Extracting,

    /// <summary>
    ///     The extraction was stopped before it finished, as another archive started opening or the tab left its
    ///     archives. The pages it had not extracted by then never come.
    /// </summary>
    Stopped
}
