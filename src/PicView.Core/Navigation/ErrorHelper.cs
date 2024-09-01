using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;

namespace PicView.Core.Navigation;

public static class ErrorHelper
{
    /// <summary>
    /// Determines the type of resource based on the provided string.
    /// </summary>
    /// <param name="s">The input string representing a URL, base64 data, file path, or directory path.</param>
    /// <returns>
    /// - If the input represents a web URL, returns "web".
    /// - If the input represents base64-encoded data, returns "base64".
    /// - If the input represents a valid file path, returns the file path.
    /// - If the input represents a directory path, returns "directory".
    /// - Otherwise, returns an empty string.
    /// </returns>
    public static string CheckIfLoadableString(string s)
    {
        if (s.StartsWith("file:///"))
        {
            s = s.Replace("file:///", "");
            s = s.Replace("%20", " ");
        }
        
        if (File.Exists(s))
            return Path.GetExtension(s).IsArchive() ? "zip" : s;

        if (Directory.Exists(s))
            return "directory";

        if (!string.IsNullOrWhiteSpace(s.GetURL()))
            return "web";

        return Base64Helper.IsBase64String(s) ? "base64" : string.Empty;
    }
}