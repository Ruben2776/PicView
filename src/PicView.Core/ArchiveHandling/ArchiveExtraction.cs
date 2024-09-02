using PicView.Core.FileHandling;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace PicView.Core.ArchiveHandling;

/// <summary>
/// Provides methods for extracting supported files from an archive.
/// </summary>
public static class ArchiveExtraction
{
    /// <summary>
    /// Gets the path of the temporary directory where the archive contents are extracted.
    /// </summary>
    public static string? TempZipDirectory { get; private set; }
    
    /// <summary>
    /// Asynchronously extracts supported files from a given archive to a temporary directory.
    /// </summary>
    /// <param name="archivePath">The path of the archive file to extract.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a boolean value:
    /// <c>true</c> if any supported files were extracted successfully; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="archivePath"/> is null, empty, or the file does not exist.</exception>
    public static async Task<bool> ExtractArchiveAsync(string archivePath)
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
            
            await using var stream = File.OpenRead(archivePath);
            using var reader = ReaderFactory.Open(stream);

            var count = 0;
            
            // Process each entry asynchronously to avoid blocking the thread
            while (reader.MoveToNextEntry())
            {
                if (reader.Entry.IsDirectory)
                {
                    continue;
                }

                // Extract only if the file is supported
                var entryFileName = reader.Entry.Key;
                if (entryFileName.IsSupported())
                {
                    await Task.Run(() => {
                        reader.WriteEntryToDirectory(tempDirectory, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    });

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

            return count > 0;
        }
        catch (IOException ioEx)
        {
            #if DEBUG
            Console.WriteLine($"IO Exception during extraction: {ioEx.Message}");
            #endif
            return false;
        }
        catch (UnauthorizedAccessException authEx)
        {
            #if DEBUG
            Console.WriteLine($"Access denied during extraction: {authEx.Message}");
            #endif
            return false;
        }
        catch (Exception ex)
        {
            #if DEBUG
            Console.WriteLine($"Extraction failed: {ex.Message}");
            #endif
            return false;
        }
    }

    /// <summary>
    /// Deletes the temporary directory created during extraction, if it exists.
    /// </summary>
    public static void Cleanup()
    {
        try
        {
            if (string.IsNullOrEmpty(TempZipDirectory) || !Directory.Exists(TempZipDirectory))
            {
                return;
            }
            Directory.Delete(TempZipDirectory, recursive: true);
        }
        catch (Exception ex)
        {
#if DEBUG
            Console.WriteLine($"{nameof(ArchiveExtraction)}: Cleanup exception \n {ex.Message}");
#endif
        }
        finally
        {
            TempZipDirectory = null;
        }
    }
}


