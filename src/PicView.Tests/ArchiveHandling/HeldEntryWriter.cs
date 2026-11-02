using System.Collections.Concurrent;
using PicView.Core.ArchiveHandling;

namespace PicView.Tests.ArchiveHandling;

/// <summary>
/// Writes entries as the app does, but holds the write of each page named open, with its file already there, until the
/// test lets it finish. So a test sees the page while it is written, and an extraction cannot end before the test is
/// ready.
/// </summary>
internal sealed class HeldEntryWriter(params string[] heldFileNames) : IArchiveEntryWriter
{
    // Bounds each hold, so a failing test fails instead of hanging the run
    private static readonly TimeSpan HoldLimit = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<string, Hold> _holds = new(
        heldFileNames.Select(name => new KeyValuePair<string, Hold>(name, new Hold())),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>Completes once the page's file is there and its write has begun.</summary>
    public Task Holding(string fileName) => _holds[fileName].Holding.Task;

    /// <summary>Lets the page's write finish.</summary>
    public void Release(string fileName) => _holds[fileName].Released.Set();

    /// <summary>Lets every held write finish.</summary>
    public void ReleaseAll()
    {
        foreach (var hold in _holds.Values)
        {
            hold.Released.Set();
        }
    }

    public void Write(Stream entry, FileStream file)
    {
        if (_holds.TryGetValue(Path.GetFileName(file.Name), out var hold))
        {
            hold.Holding.TrySetResult();
            hold.Released.Wait(HoldLimit);
        }

        entry.CopyTo(file);
    }

    private sealed class Hold
    {
        public TaskCompletionSource Holding { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ManualResetEventSlim Released { get; } = new();
    }
}
