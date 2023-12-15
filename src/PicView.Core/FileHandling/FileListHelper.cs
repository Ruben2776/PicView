namespace PicView.Core.FileHandling;

public static class FileListHelper
{
    /// <summary>
    /// Enumeration of different options to sort files by.
    /// </summary>
    public enum SortFilesBy
    {
        /// <summary>
        /// Sort files by name.
        /// </summary>
        Name = 0,

        /// <summary>
        /// Sort files by file size.
        /// </summary>
        FileSize = 1,

        /// <summary>
        /// Sort files by creation time.
        /// </summary>
        CreationTime = 2,

        /// <summary>
        /// Sort files by extension.
        /// </summary>
        Extension = 3,

        /// <summary>
        /// Sort files by last access time.
        /// </summary>
        LastAccessTime = 4,

        /// <summary>
        /// Sort files by last write time.
        /// </summary>
        LastWriteTime = 5,

        /// <summary>
        /// Sort files randomly.
        /// </summary>
        Random = 6
    }
}