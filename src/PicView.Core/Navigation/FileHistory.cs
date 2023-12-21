using System.Diagnostics;
using System.Text.Json;

namespace PicView.Core.Navigation
{
    /// <summary>
    /// Manages the history of recently accessed files.
    /// </summary>
    public class FileHistory
    {
        private readonly List<string> _fileHistory;
        public const short MaxCount = 15;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHistory"/> class.
        /// </summary>
        public FileHistory()
        {
            _fileHistory ??= new();

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
                    _fileHistory.AddRange(JsonSerializer.Deserialize<List<string>>(json));
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

        public void Add(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            lock (_fileHistory)
            {
                if (_fileHistory.Exists(e => e.EndsWith(fileName)))
                {
                    return;
                }

                if (_fileHistory.Count >= MaxCount)
                {
                    _fileHistory.RemoveAt(0);
                }

                _fileHistory.Add(fileName);
            }
        }

        public string? GetLastFile()
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
        public string? GetFirstFile()
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
        public string? GetEntryAt(int index)
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
        public string? GetNextEntry(bool looping, int index, List<string> list)
        {
            if (list.Count <= 0)
            {
                return GetLastFile();
            }

            try
            {
                lock (_fileHistory)
                {
                    var foundIndex = _fileHistory.IndexOf(list[index]);

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
        public string? GetPreviousEntry(bool looping, int index, List<string> list)
        {
            if (list.Count <= 0)
            {
                return GetFirstFile();
            }

            try
            {
                lock (_fileHistory)
                {
                    var foundIndex = _fileHistory.IndexOf(list[index]);
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