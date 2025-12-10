using System.Globalization;
using PicView.Core.Extensions;
using PicView.Core.Localization;

namespace PicView.Core.Gallery;

public static class GalleryThumbInfo
{
    /// <summary>
    /// Represents the data for a gallery thumbnail.
    /// </summary>
    public readonly struct GalleryThumbHolder : IEquatable<GalleryThumbHolder>
    {
        public string FileLocation { get; }
        public string FileName { get; }
        public string FileSize { get; }
        public string FileDate { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GalleryThumbHolder"/> struct.
        /// </summary>
        private GalleryThumbHolder(string fileLocation, string fileName, string fileSize, string fileDate)
        {
            FileLocation = fileLocation;
            FileName = fileName;
            FileSize = fileSize;
            FileDate = fileDate;
        }

        /// <summary>
        /// Gets thumbnail data for the specified index.
        /// </summary>
        public static GalleryThumbHolder GetThumbData(FileInfo? fileInfo)
        {
            if (fileInfo == null)
            {
                return default; // Safety check
            }

            var fileLocation = fileInfo.FullName;
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            var fileSize =
                $"{TranslationManager.Translation.FileSize}: {fileInfo.Length.GetReadableFileSize()}";
            var fileDate =
                $"{TranslationManager.Translation.Modified}: {fileInfo.LastWriteTimeUtc.ToString(CultureInfo.CurrentCulture)}";

            return new GalleryThumbHolder(fileLocation, fileName, fileSize, fileDate);
        }

        /// <summary>
        /// Performance: Highly efficient strongly-typed comparison.
        /// Avoids boxing and reflection.
        /// </summary>
        public bool Equals(GalleryThumbHolder other)
        {
            // Compare FileLocation first as it is the most likely to differ, providing a fast exit.
            return FileLocation == other.FileLocation &&
                   FileName == other.FileName &&
                   FileSize == other.FileSize &&
                   FileDate == other.FileDate;
        }

        /// <summary>
        /// Performance: Standard object override.
        /// Checks type compatibility before calling the strong Equals.
        /// </summary>
        public override bool Equals(object? obj) => obj is GalleryThumbHolder other && Equals(other);

        /// <summary>
        /// Performance: Essential for using this struct as a key in Dictionaries or HashSets.
        /// </summary>
        public override int GetHashCode() => HashCode.Combine(FileLocation, FileName, FileSize, FileDate);

        public static bool operator ==(GalleryThumbHolder left, GalleryThumbHolder right) => left.Equals(right);

        public static bool operator !=(GalleryThumbHolder left, GalleryThumbHolder right) => !(left == right);
    }
}