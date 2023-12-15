namespace PicView.Core.FileHandling;

public static class ArchiveExtraction
{
    /// <summary>
    /// File archive path for the extracted folder
    /// </summary>
    public static string? TempFilePath { get; set; }

    /// <summary>
    /// File archive path for the extracted zip file
    /// </summary>
    public static string? TempZipFile { get; set; }

    public static bool CreateTempDirectory(string pathToArchiveFile)
    {
        TempZipFile = pathToArchiveFile;
        TempFilePath = Path.GetTempPath() + Path.GetRandomFileName();
        Directory.CreateDirectory(TempFilePath);

        return Directory.Exists(TempFilePath);
    }

    public static string? ExtractAndReturnPath(string pathToArchiveFile, string pathToFileArchiver, string arguments)
    {
        var tempDirectory = CreateTempDirectory(pathToArchiveFile);
        if (!tempDirectory)
        {
            return null;
        }

        return string.Empty;
    }
}