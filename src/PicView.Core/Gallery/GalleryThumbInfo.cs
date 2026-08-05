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

            // --- Optimization for File Size ---
            var sizeLabel = TranslationManager.Translation.FileSize;
            var sizeValue = fileInfo.Length.GetReadableFileSize(); 

            // Calculate exact length: Label + ": " (2 chars) + Value
            var sizeLen = sizeLabel.Length + 2 + sizeValue.Length;

            var fileSize = string.Create(sizeLen, (sizeLabel, sizeValue), (span, state) =>
            {
                var (label, value) = state;
        
                // Copy Label
                label.AsSpan().CopyTo(span);
        
                // Write ": "
                span[label.Length] = ':';
                span[label.Length + 1] = ' ';
        
                // Copy Value
                value.AsSpan().CopyTo(span.Slice(label.Length + 2));
            });

            // --- Optimization for File Date ---
            var dateLabel = TranslationManager.Translation.Modified;
            // Formatting date to string first is necessary to know the exact length for String.Create
            var dateValue = fileInfo.LastWriteTimeUtc.ToString(CultureInfo.CurrentCulture);

            var dateLen = dateLabel.Length + 2 + dateValue.Length;

            var fileDate = string.Create(dateLen, (dateLabel, dateValue), (span, state) =>
            {
                var (label, value) = state;
        
                label.AsSpan().CopyTo(span);
                span[label.Length] = ':';
                span[label.Length + 1] = ' ';
                value.AsSpan().CopyTo(span.Slice(label.Length + 2));
            });

            return new GalleryThumbHolder(fileLocation, fileName, fileSize, fileDate);
        }

        /// <summary>
        /// Performance: Highly efficient strongly-typed comparison.
        /// Avoids boxing and reflection.
        /// </summary>
        public bool Equals(GalleryThumbHolder other)
        {
            // Compare FileLocation first as it is the most likely to differ, providing a fast exit.
            return string.Equals(FileLocation, other.FileLocation, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(FileName, other.FileName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(FileSize, other.FileSize, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(FileDate, other.FileDate, StringComparison.OrdinalIgnoreCase);
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