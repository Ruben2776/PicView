using PicView.Core.ArchiveHandling;

namespace PicView.Tests.ArchiveHandling;

/// <summary>
/// Extracts as the app does, but holds the write of one page open, with its file already there, until the test lets it
/// finish. So a test sees the page while it is written, and the extraction cannot end before the test is ready.
/// </summary>
internal sealed class HeldWriteExtraction(string heldFileName) : ArchiveExtractionService
{
    // Bounds the hold, so a failing test fails instead of hanging the run
    private static readonly TimeSpan HoldLimit = TimeSpan.FromSeconds(10);

    private readonly ManualResetEventSlim _released = new();

    /// <summary>Completes once the held page's file is there and its write has begun.</summary>
    public TaskCompletionSource Holding { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Lets the held write finish.</summary>
    public void Release() => _released.Set();

    internal override void CopyEntry(Stream entry, FileStream file)
    {
        if (string.Equals(Path.GetFileName(file.Name), heldFileName, StringComparison.OrdinalIgnoreCase))
        {
            Holding.TrySetResult();
            _released.Wait(HoldLimit);
        }

        base.CopyEntry(entry, file);
    }
}
