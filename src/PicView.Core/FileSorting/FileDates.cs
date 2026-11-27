using System.Collections.Concurrent;
using PicView.Core.DebugTools;

namespace PicView.Core.FileSorting;

/// <summary>
/// Reads the date the platform's file manager shows for a file: on Windows, Explorer's Date column, which is the date
/// taken of a file that has one.
/// </summary>
public interface IFileDateSource
{
    /// <summary>
    /// The date the file manager shows for the file at <paramref name="path"/>; <see cref="DateTime.MinValue"/> when
    /// it shows none; null when it could not be asked. Opens the file, so it is slow: not for the UI thread, and
    /// not once per comparison.
    /// </summary>
    DateTime? DateOf(string path);
}

/// <summary>
/// The dates read from the file manager so far, for the Date field of a File Order (ADR-0051). One for the whole
/// application, so a file's date is read once, whichever tab lists it.
/// </summary>
/// <param name="source">Reads a file's date; null where the file system's dates are all there is.</param>
public sealed class FileDates(IFileDateSource? source = null)
{
    // Enough for the folders a session browses, and dropped whole past it rather than tracked by age
    private const int MostKept = 200_000;

    // By path, with the last write time the date was read at, so a file written since is read again
    private readonly ConcurrentDictionary<string, (DateTime Written, DateTime Date)> _read =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether dates can be read here at all.
    /// </summary>
    public bool CanRead => source is not null;

    /// <summary>
    /// The date read for <paramref name="file"/>, or null when none was read since the file was last written, and
    /// the file system's dates stand in for it.
    /// </summary>
    public DateTime? Known(FileInfo file)
    {
        if (!_read.TryGetValue(file.FullName, out var read))
        {
            return null;
        }

        try
        {
            return read.Written == file.LastWriteTimeUtc ? read.Date : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the dates of the <paramref name="files"/> that have none read yet, several at a time, off the caller's
    /// thread. A file whose date cannot be read keeps the file system's.
    /// </summary>
    public async ValueTask ReadAsync(IReadOnlyList<FileInfo> files, CancellationToken ct = default)
    {
        if (source is null)
        {
            return;
        }

        if (_read.Count > MostKept)
        {
            _read.Clear();
        }

        var options = new ParallelOptions
        {
            CancellationToken = ct,
            // Each read opens a file, so more of them at once than this gains little
            MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 1, 8)
        };
        await Parallel.ForEachAsync(files, options, (file, _) =>
        {
            Read(file);
            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
    }

    private void Read(FileInfo file)
    {
        try
        {
            if (Known(file) is not null)
            {
                return;
            }

            var written = file.LastWriteTimeUtc;
            if (source!.DateOf(file.FullName) is { } date)
            {
                _read[file.FullName] = (written, date);
            }
        }
        catch (Exception e)
        {
            // A file gone or locked meanwhile, or a shell that fails for it: the file system's dates stand
            DebugHelper.LogDebug(nameof(FileDates), nameof(Read), e);
        }
    }
}
