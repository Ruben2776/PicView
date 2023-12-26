using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;

namespace PicView.Core.Navigation;

public static class ErrorHelper
{
    /// <summary>
    /// If url returns "web", if base64 returns "base64" if file, returns file path, if directory returns "directory" else returns empty
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string CheckIfLoadableString(string s)
    {
        if (s.StartsWith("file:///"))
        {
            s = s.Replace("file:///", "");
        }

        if (File.Exists(s))
            return Path.GetExtension(s).IsArchive() ? "zip" : s;

        if (Directory.Exists(s))
            return s;

        if (!string.IsNullOrWhiteSpace(s.GetURL()))
            return "web";

        if (Base64Helper.IsBase64String(s))
            return "base64";

        s = string.Join("_", s.Split(Path.GetInvalidFileNameChars()));
        return File.Exists(s) ? s : string.Empty;
    }
}