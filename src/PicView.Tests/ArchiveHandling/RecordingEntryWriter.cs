using System.Collections.Concurrent;
using PicView.Core.ArchiveHandling;

namespace PicView.Tests.ArchiveHandling;

/// <summary>
/// Writes entries as the app does, and records the name of each page in the order it was written.
/// </summary>
internal sealed class RecordingEntryWriter : IArchiveEntryWriter
{
    private readonly ConcurrentQueue<string> _written = new();

    /// <summary>The pages written so far, in the order they were written.</summary>
    public string[] Written => [.. _written];

    public void Write(Stream entry, FileStream file)
    {
        entry.CopyTo(file);
        _written.Enqueue(Path.GetFileName(file.Name));
    }
}
