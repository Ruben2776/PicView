namespace PicView.Core.FileHandling;

public static class ArchiveHelper
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
    
    //TODO use https://github.com/adamhathcock/sharpcompress for multiplatform support
}