namespace PicView.Core.ArchiveHandling;

/// <summary>
///     Writes an archive entry into its file, which is already there, as <paramref name="file" /> holds it open. The
///     app copies the entry's bytes; a test can hold a page's write open, to see the page while it is written.
/// </summary>
public interface IArchiveEntryWriter
{
    void Write(Stream entry, FileStream file);
}

/// <summary>
///     Copies an entry's bytes into its file, as the app does.
/// </summary>
public sealed class ArchiveEntryWriter : IArchiveEntryWriter
{
    public static ArchiveEntryWriter Instance { get; } = new();

    public void Write(Stream entry, FileStream file) => entry.CopyTo(file);
}
