namespace PicView.Core.FileHandling;

public static class TempFileHelper
{
    /// <summary>
    /// File path for the extracted folder
    /// </summary>
    public static string? TempFilePath { get; set; }

    public static bool CreateTempDirectory()
    {
        TempFilePath = Path.GetTempPath() + Path.GetRandomFileName();
        Directory.CreateDirectory(TempFilePath);

        return Directory.Exists(TempFilePath);
    }
}