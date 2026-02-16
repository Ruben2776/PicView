using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using SharpCompress.Readers;

namespace PicView.Core.ArchiveHandling;

/// <summary>
///     Provides methods for extracting supported files from an archive.
/// </summary>
public static class ArchiveExtraction
{
    /// <summary>
    ///     Gets the path of the temporary directory where the archive contents are extracted.
    /// </summary>
    public static string? TempZipDirectory { get; private set; }
        
    public static string? LastOpenedArchive { get; private set; }
    
    public static bool IsArchived => TempZipDirectory != null;

    /// <summary>
    ///     Asynchronously extracts supported files from a given archive to a temporary directory.
    /// </summary>
    /// <param name="archivePath">
    ///     The path of the archive file to extract.
    /// </param>
    /// <param name="extractWithLocalSoftwareAsync">
    ///     A delegate function that attempts to extract the archive using local software (e.g., 7-Zip, WinRAR).
    ///     This function should return a boolean value indicating whether the extraction was successful.
    ///     It takes two parameters: the path to the archive and the path to the temporary extraction directory.
    /// </param>
    /// <returns>
    ///     A task representing the asynchronous operation. The task result is a boolean:
    ///     <c>true</c> if any supported files were successfully extracted; otherwise, <c>false</c>.
    /// </returns>
    public static async Task<bool> ExtractArchiveAsync(string archivePath,
        Func<string, string, Task<bool>> extractWithLocalSoftwareAsync)
    {
        try
        {
            if (string.IsNullOrEmpty(archivePath) || !File.Exists(archivePath))
            {
                throw new ArgumentException("The archive path is invalid or the file does not exist.");
            }

            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            TempZipDirectory = tempDirectory;

            var ext = Path.GetExtension(archivePath);
            if (ext.Equals(".7z", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".cb7", StringComparison.OrdinalIgnoreCase))
            {
                if (!await extractWithLocalSoftwareAsync(archivePath, tempDirectory))
                {
                    return false;
                }

                LastOpenedArchive = archivePath;
                return true;
            }

            await using var stream = File.OpenRead(archivePath);
            await using var reader = await ReaderFactory.OpenAsyncReader(stream);

            var count = 0;

            // Process each entry asynchronously to avoid blocking the thread
            while (await reader.MoveToNextEntryAsync())
            {
                if (reader.Entry.IsDirectory)
                {
                    continue;
                }

                // Extract only if the file is supported
                var entryFileName = reader.Entry.Key;
                if (entryFileName.IsSupported())
                {
                    await reader.WriteEntryToDirectoryAsync(tempDirectory);
#if DEBUG
                    Console.WriteLine($"Extracted: {entryFileName}");
#endif

                    count++;
                }
                else
                {
#if DEBUG
                    Console.WriteLine($"Skipped unsupported file: {entryFileName}");
#endif
                }
            }

            if (count <= 0)
            {
                return false;
            }

            LastOpenedArchive = archivePath;
            return true;

        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ArchiveExtraction), nameof(ExtractArchiveAsync), ex);
            return false;
        }
    }

    /// <summary>
    ///     Deletes the temporary directory created during extraction, if it exists.
    /// </summary>
    public static void Cleanup()
    {
        try
        {
            if (string.IsNullOrEmpty(TempZipDirectory) || !Directory.Exists(TempZipDirectory))
            {
                return;
            }

            Directory.Delete(TempZipDirectory, true);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ArchiveExtraction), nameof(Cleanup), ex);
        }
        finally
        {
            TempZipDirectory = null;
            LastOpenedArchive = null;
        }
    }
}