using PicView.Core.FileHandling;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace PicView.Core.ArchiveHandling
{
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
                using var reader = ReaderFactory.Open(stream);

                var count = 0;
                
                await Task.Run(() =>
                {
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
                            reader.WriteEntryToDirectory(tempDirectory, new ExtractionOptions
                            {
                                ExtractFullPath = true,
                                Overwrite = true
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
                });

                if (count <= 0)
                {
                    return false;
                }

                LastOpenedArchive = archivePath;
                return true;

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
#if DEBUG
                Console.WriteLine($"{nameof(ArchiveExtraction)}: Cleanup exception \n {ex.Message}");
#endif
            }
            finally
            {
                TempZipDirectory = null;
                LastOpenedArchive = null;
            }
        }
    }
}