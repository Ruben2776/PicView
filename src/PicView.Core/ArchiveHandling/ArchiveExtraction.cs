using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using SharpCompress.Archives;

namespace PicView.Core.ArchiveHandling;

/// <summary>
///     Static shim that delegates to a default <see cref="ArchiveExtractionService"/> instance.
///     Preserved for backward compatibility with callers outside the per-tab refactor scope
///     (e.g. <c>FileHistoryManager</c>, <c>WindowFunctions</c>, <c>QuickLoad</c>).
/// </summary>
public static class ArchiveExtraction
{
    private static readonly ArchiveExtractionService Default = new();

    /// <summary>
    ///     Gets the path of the temporary directory where the archive contents are extracted.
    /// </summary>
    public static string? TempZipDirectory => Default.TempZipDirectory;

    public static string? LastOpenedArchive => Default.LastOpenedArchive;

    public static bool IsArchived => Default.IsArchived;

    /// <summary>
    /// Prepares an archive for staged extraction: creates a temporary directory, lists all
    /// supported entries (or fully extracts the archive when local software is required) and
    /// returns a sorted entry list.
    /// </summary>
    public static async Task<ArchiveExtractionService.ArchivePreparation?> PrepareArchiveAsync(
        string archivePath,
        Func<string, string, Task<bool>> extractWithLocalSoftwareAsync,
        Func<string, string, int> stringComparer)
    {
        return await Default.PrepareArchiveAsync(archivePath, extractWithLocalSoftwareAsync, stringComparer).ConfigureAwait(false);
    }

    /// <summary>
    ///     Extracts a single archive entry to the previously created <see cref="TempZipDirectory" />.
    /// </summary>
    /// <returns>The absolute path of the extracted file, or <c>null</c> on failure.</returns>
    public static async Task<string?> ExtractEntryAsync(
        string archivePath,
        string entryKey,
        CancellationToken ct = default)
    {
        return await Default.ExtractEntryAsync(archivePath, entryKey, ct).ConfigureAwait(false);
    }

    /// <summary>
    ///     Extracts every entry whose key is present in <paramref name="remainingKeys" /> to the
    ///     <see cref="TempZipDirectory" />.
    /// </summary>
    public static async Task ExtractRemainingAsync(
        string archivePath,
        IReadOnlyCollection<string> remainingKeys,
        CancellationToken ct = default)
    {
        await Default.ExtractRemainingAsync(archivePath, remainingKeys, ct).ConfigureAwait(false);
    }

    /// <summary>
    ///     Deletes the temporary directory created during extraction, if it exists.
    /// </summary>
    public static void Cleanup()
    {
        Default.Cleanup();
    }
    
    public static void Cleanup(string? tempZipDirectory)
    {
        Default.Cleanup(tempZipDirectory);
    }
}
