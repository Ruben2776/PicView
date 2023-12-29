using PicView.Core.FileHandling;
using PicView.Core.Localization;
using System.Globalization;

namespace PicView.Core.Gallery;

public class GalleryThumbInfo
{
    /// <summary>
    /// Represents the data for a gallery thumbnail.
    /// </summary>
    public struct GalleryThumbHolder
    {
        /// <summary>
        /// Gets or sets the file location of the thumbnail.
        /// </summary>
        public string FileLocation { get; set; }

        /// <summary>
        /// Gets or sets the file name of the thumbnail.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file size of the thumbnail.
        /// </summary>
        public string FileSize { get; set; }

        /// <summary>
        /// Gets or sets the file date of the thumbnail.
        /// </summary>
        public string FileDate { get; set; }

        /// <summary>
        /// Gets or sets the source of the thumbnail.
        /// </summary>
        public object ImageSource { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GalleryThumbHolder"/> class.
        /// </summary>
        /// <param name="fileLocation">The file location of the thumbnail.</param>
        /// <param name="fileName">The file name of the thumbnail.</param>
        /// <param name="fileSize">The file size of the thumbnail.</param>
        /// <param name="fileDate">The file date of the thumbnail.</param>
        /// <param name="imageSource">The source of the thumbnail.</param>
        public GalleryThumbHolder(string fileLocation, string fileName, string fileSize, string fileDate,
            object imageSource)
        {
            FileLocation = fileLocation;
            FileName = fileName;
            FileSize = fileSize;
            FileDate = fileDate;
            ImageSource = imageSource;
        }

        /// <summary>
        /// Gets thumbnail data for the specified index.
        /// </summary>
        /// <param name="index">The index of the thumbnail.</param>
        /// <returns>The <see cref="GalleryThumbHolder"/> instance containing thumbnail data.</returns>
        public static GalleryThumbHolder GetThumbData(int index, object? imageSource, FileInfo fileInfo)
        {
            var fileNameLength = 60;
            var fileLocation = fileInfo.FullName;
            fileLocation = fileLocation.Length > fileNameLength ? fileLocation.Shorten(fileNameLength) : fileLocation;
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            fileName = fileName.Length > fileNameLength ? fileName.Shorten(fileNameLength) : fileName;
            var getFileSizeResource = TranslationHelper.GetTranslation("FileSize");
            var getFileDateResource = TranslationHelper.GetTranslation("Modified");
            var fileSize = "";
            var fileDate = "";
            if (getFileSizeResource != null)
            {
                fileSize = $"{getFileSizeResource}: {fileInfo.Length.GetReadableFileSize()}";
            }

            if (getFileDateResource != null)
            {
                fileDate =
                    $"{getFileDateResource}: {fileInfo.LastWriteTimeUtc.ToString(CultureInfo.CurrentCulture)}";
            }

            return new GalleryThumbHolder(fileLocation, fileName, fileSize, fileDate, imageSource);
        }
    }
}