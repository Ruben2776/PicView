using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;

namespace PicView.Core.Navigation;

/// <summary>
/// Provides methods for determining the type of file or resource from a given input string
/// and encapsulates logic to classify the resource as a loadable entity.
/// </summary>
public static class FileTypeResolver
{
    /// <summary>
    /// Represents a structure containing a loadable file type and its associated data.
    /// </summary>
    /// <param name="type">The type of the loadable file.</param>
    /// <param name="data">The data associated with the loadable file.</param>
    public readonly struct FileTypeStruct(LoadAbleFileType type, string data)
    {
        /// <summary>
        /// Gets the type of the loadable file.
        /// </summary>
        public LoadAbleFileType Type => type;
        
        /// <summary>
        /// Gets the data associated with the loadable file.
        /// </summary>
        public string Data => data;
    }

    /// <summary>
    /// Specifies the different types of loadable files.
    /// </summary>
    public enum LoadAbleFileType
    {
        /// <summary>
        /// Represents a regular file.
        /// </summary>
        File,
        
        /// <summary>
        /// Represents a directory.
        /// </summary>
        Directory,
        
        /// <summary>
        /// Represents a web URL.
        /// </summary>
        Web,
        
        /// <summary>
        /// Represents a Base64 encoded string.
        /// </summary>
        Base64,
        
        /// <summary>
        /// Represents a zip archive.
        /// </summary>
        Zip
    }
    
    /// <summary>
    /// Checks if the provided string is a loadable file type and returns its type and associated data.
    /// </summary>
    /// <param name="s">The string to check.</param>
    /// <returns>
    /// A <see cref="FileTypeStruct"/> containing the type and data of the loadable file if the string is loadable, otherwise null.
    ///fd </returns>
    public static FileTypeStruct? CheckIfLoadableString(string s)
    {
        if (s.StartsWith('"') && s.EndsWith('"'))
        {
            s = s[1..^1];
        }
        
        var path = s; // Use a separate variable for the potentially decoded path

        // Attempt to parse the string as a URI.
        // This handles all special characters.
        if (Uri.TryCreate(s, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            path = uri.LocalPath; // Decodes the path correctly (e.g., "%5B%5D" -> "[]")
            path = path.Replace("%20", " ");
        }

        // Use the decoded 'path' variable for file system checks
        if (File.Exists(path))
        {
            var type = path.IsArchive() ? LoadAbleFileType.Zip : LoadAbleFileType.File;
            return new FileTypeStruct(type, path);
        }

        if (Directory.Exists(path))
        {
            return new FileTypeStruct(LoadAbleFileType.Directory, path);
        }

        if (!string.IsNullOrWhiteSpace(s.GetURL()))
        {
            return new FileTypeStruct(LoadAbleFileType.Web, s);
        }

        var base64String = Base64Decoder.IsBase64String(s);

        if (!string.IsNullOrEmpty(base64String))
        {
            return new FileTypeStruct(LoadAbleFileType.Base64, base64String);
        }

        return null;
    }
}