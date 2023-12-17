using System.Diagnostics;
using System.Text.Json;

namespace PicView.Core.Navigation
{
    /// <summary>
    /// Represents an entry in the file history.
    /// </summary>
    public class FileEntry
    {
        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the top-level directory.
        /// </summary>
        public string TopLevelDirectory { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileEntry"/> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="topLevelDirectory">The top-level directory.</param>
        public FileEntry(string filePath, string topLevelDirectory)
        {
            FilePath = filePath;
            TopLevelDirectory = topLevelDirectory;
        }
    }

    /// <summary>
    /// Manages the history of recently accessed files.
    /// </summary>
    public class FileHistory
    {
        private readonly List<FileEntry?> _fileHistory;
        public const short MaxCount = 15;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHistory"/> class.
        /// </summary>
        public FileHistory()
        {
            _fileHistory ??= new List<FileEntry?>();

            try
            {
                var jsonFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/recent.json");

                if (!File.Exists(jsonFile))
                {
                    using var fs = File.Create(jsonFile);
                    fs.Seek(0, SeekOrigin.Begin);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(FileHistory)} exception, \n{e.Message}");
#endif
            }
            ReadFromFile();
        }

        /// <summary>
        /// Reads the file history from a JSON file.
        /// </summary>
        /// <returns>An empty string if successful, otherwise an error message.</returns>
        public string ReadFromFile()
        {
            lock (_fileHistory)
            {
                _fileHistory.Clear();
            }

            try
            {
                var json = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/recent.json"));
                lock (_fileHistory)
                {
                    _fileHistory.AddRange(JsonSerializer.Deserialize<List<FileEntry>>(json));
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(FileHistory)}: {nameof(ReadFromFile)} exception,\n{e.Message}");
#endif
                return e.Message;
            }
            return string.Empty;
        }

        /// <summary>
        /// Writes the file history to a JSON file.
        /// </summary>
        /// <returns>An empty string if successful, otherwise an error message.</returns>
        public string WriteToFile()
        {
            try
            {
                lock (_fileHistory)
                {
                    var json = JsonSerializer.Serialize(_fileHistory);
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/recent.json"), json);
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return string.Empty;
        }

        public int GetCount()
        {
            lock (_fileHistory)
            {
                return _fileHistory.Count;
            }
        }

        /// <summary>
        /// Adds a file to the history.
        /// </summary>
        /// <param name="fileName">The name of the file to add.</param>
        public void Add(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            lock (_fileHistory)
            {
                if (_fileHistory.Exists(e => e.FilePath.EndsWith(fileName)))
                {
                    return;
                }

                if (_fileHistory.Count >= MaxCount)
                {
                    _fileHistory.RemoveAt(0);
                }

                var entry = new FileEntry(fileName, Path.GetDirectoryName(fileName));

                _fileHistory.Add(entry);
            }
        }

        /// <summary>
        /// Gets the last file in the history.
        /// </summary>
        /// <returns>The last file entry or null if the history is empty.</returns>
        public FileEntry? GetLastFile()
        {
            lock (_fileHistory)
            {
                return _fileHistory.Count > 0 ? _fileHistory[^1] : null;
            }
        }

        /// <summary>
        /// Gets the first file in the history.
        /// </summary>
        /// <returns>The first file entry or null if the history is empty.</returns>
        public FileEntry? GetFirstFile()
        {
            lock (_fileHistory)
            {
                return _fileHistory.Count > 0 ? _fileHistory[0] : null;
            }
        }

        /// <summary>
        /// Gets the file entry at the specified index.
        /// </summary>
        /// <param name="index">The index of the file entry to retrieve.</param>
        /// <returns>The file entry at the specified index or null if the history is empty.</returns>
        public FileEntry? GetEntryAt(int index)
        {
            lock (_fileHistory)
            {
                if (_fileHistory.Count == 0)
                {
                    return null;
                }

                if (index < 0)
                {
                    return _fileHistory[0];
                }

                return index >= _fileHistory.Count ? _fileHistory[^1] : _fileHistory[index];
            }
        }

        /// <summary>
        /// Gets the next file entry based on the current index and list of file names.
        /// </summary>
        /// <param name="looping">Whether to loop to the beginning when reaching the end.</param>
        /// <param name="index">The current index in the list.</param>
        /// <param name="list">The list of file names.</param>
        /// <returns>The next file entry or null if not found or an exception occurs.</returns>
        public FileEntry? GetNextEntry(bool looping, int index, List<string> list)
        {
            if (list.Count <= 0)
            {
                return GetLastFile();
            }

            try
            {
                lock (_fileHistory)
                {
                    var foundIndex = _fileHistory.FindIndex(entry => entry.FilePath == list[index]);

                    if (looping)
                    {
                        return GetEntryAt((foundIndex + 1 + _fileHistory.Count) % _fileHistory.Count);
                    }

                    foundIndex++;
                    return foundIndex >= MaxCount ? null : GetEntryAt(foundIndex);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(FileHistory)}: {nameof(GetNextEntry)} exception,\n{e.Message}");
#endif
                return null;
            }
        }

        /// <summary>
        /// Gets the previous file entry based on the current index and list of file names.
        /// </summary>
        /// <param name="looping">Whether to loop to the end when reaching the beginning.</param>
        /// <param name="index">The current index in the list.</param>
        /// <param name="list">The list of file names.</param>
        /// <returns>The previous file entry or null if not found or an exception occurs.</returns>
        public FileEntry? GetPreviousEntry(bool looping, int index, List<string> list)
        {
            if (list.Count <= 0)
            {
                return GetLastFile();
            }

            try
            {
                lock (_fileHistory)
                {
                    var foundIndex = _fileHistory.FindIndex(entry => entry.FilePath == list[index]);
                    if (looping)
                    {
                        return GetEntryAt((foundIndex - 1 + _fileHistory.Count) % _fileHistory.Count);
                    }

                    index--;
                    return index < 0 ? null : GetEntryAt(index);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(FileHistory)}: {nameof(GetPreviousEntry)} exception,\n{e.Message}");
#endif
                return null;
            }
        }
    }
}