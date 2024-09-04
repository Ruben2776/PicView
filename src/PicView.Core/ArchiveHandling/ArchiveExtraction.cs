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
    /// <param name="archivePath">
    /// The path of the archive file to extract. The method throws an <see cref="ArgumentException"/>
    /// if this path is null, empty, or the file does not exist.
    /// </param>
    /// <param name="extractWithLocalSoftwareAsync">
    /// A delegate function that attempts to extract the archive using local software (e.g., 7-Zip, WinRAR).
    /// This function should return a boolean value indicating whether the extraction was successful.
    /// It takes two parameters: the path to the archive and the path to the temporary extraction directory.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is a boolean:
    /// <c>true</c> if any supported files were successfully extracted; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="archivePath"/> is null, empty, or the file does not exist.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if there is an I/O error during the extraction process (e.g., issues with reading or writing files).
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown if the extraction process encounters access issues (e.g., insufficient permissions to access files or directories).
    /// </exception>
    /// <exception cref="Exception">
    /// A general exception that can be thrown for any other unexpected issues during extraction.
    /// </exception>
    /// <remarks>
    /// The method first checks the file extension of the archive to determine if it should be extracted using local software.
    /// If the file is supported by the local software (e.g., 7z, cb7), it delegates the extraction to the provided
    /// <paramref name="extractWithLocalSoftwareAsync"/> function. If the archive is in another format, it uses SharpCompress
    /// to read and extract supported files asynchronously.
    /// </remarks>
    public static async Task<bool> ExtractArchiveAsync(string archivePath, Func<string, string, Task<bool>> extractWithLocalSoftwareAsync)
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
            if (ext.Equals(".7z", StringComparison.OrdinalIgnoreCase) || ext.Equals(".cb7", StringComparison.OrdinalIgnoreCase))
            {
                return await extractWithLocalSoftwareAsync(archivePath, tempDirectory);
            }
            
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


