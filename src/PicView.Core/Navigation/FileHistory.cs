﻿using System.Diagnostics;

namespace PicView.Core.Navigation;

/// <summary>
/// Manages the history of recently accessed files.
/// </summary>
public class FileHistory
{
    private readonly List<string> _fileHistory;
    public const short MaxCount = 15;
    private readonly string _fileLocation;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileHistory"/> class.
    /// </summary>
    public FileHistory()
    {
        _fileHistory ??= [];
        _fileLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/recent.txt");
        try
        {
            if (!File.Exists(_fileLocation))
            {
                using var fs = File.Create(_fileLocation);
                fs.Seek(0, SeekOrigin.Begin);
            }
        }
        catch (Exception e)
        {
            try
            {
                _fileLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ruben2776/PicView/Config/recent.txt");
            }
            catch (Exception exception)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(FileHistory)} exception, \n{exception.Message}");
#endif
            }
#if DEBUG
            Trace.WriteLine($"{nameof(FileHistory)} exception, \n{e.Message}");
#endif
        }
        ReadFromFile();
    }

    /// <summary>
    /// Reads the file history from the .txt file.
    /// </summary>
    /// <returns>An empty string if successful, otherwise an error message.</returns>
    public string ReadFromFile()
    {
        _fileHistory.Clear();
        try
        {
            using var reader = new StreamReader(_fileLocation);
            while (reader.Peek() >= 0)
            {
                _fileHistory.Add(reader.ReadLine());
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
    /// Writes the file history to the .txt file.
    /// </summary>
    /// <returns>An empty string if successful, otherwise an error message.</returns>
    public string WriteToFile()
    {
        try
        {
            using var writer = new StreamWriter(_fileLocation);
            foreach (var item in _fileHistory)
            {
                writer.WriteLine(item);
            }
        }
        catch (Exception e)
        {
            return e.Message;
        }
        return string.Empty;
    }

    /// <summary>
    ///  Adds a file to the history.
    /// </summary>
    /// <param name="fileName">The name of the file to add to the history.</param>
    public void Add(string fileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName) || _fileHistory.Exists(e => e is not null && e.EndsWith(fileName)))
            {
                return;
            }

            if (_fileHistory.Count >= MaxCount)
            {
                _fileHistory.RemoveAt(0);
            }

            _fileHistory.Add(fileName);
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(FileHistory)}: {nameof(Add)} exception,\n{e.Message}");
#endif
        }
    }

    public void Remove(string fileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }
            _fileHistory.Remove(fileName);
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(FileHistory)}: {nameof(Remove)} exception,\n{e.Message}");
#endif
        }
    }

    public void Rename(string oldName, string newName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
            {
                return;
            }
            var index = _fileHistory.IndexOf(oldName);
            if (index < 0)
            {
                return;
            }
            _fileHistory[index] = newName;
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(FileHistory)}: {nameof(Rename)} exception,\n{e.Message}");
#endif
        }
    }
    
    public int GetCount() => _fileHistory.Count;

    public bool Contains(string name) => !string.IsNullOrWhiteSpace(name) && _fileHistory.Contains(name);


    /// <summary>
    ///  Gets the last file in the history.
    /// </summary>
    /// <returns>The last file entry or null if the history is empty.</returns>
    public string? GetLastFile() => _fileHistory.Count > 0 ? _fileHistory[^1] : null;

    /// <summary>
    /// Gets the first file in the history.
    /// </summary>
    /// <returns>The first file entry or null if the history is empty.</returns>
    public string? GetFirstFile() => _fileHistory.Count > 0 ? _fileHistory[0] : null;

    /// <summary>
    /// Gets the file entry at the specified index.
    /// </summary>
    /// <param name="index">The index of the file entry to retrieve.</param>
    /// <returns>The file entry at the specified index or null if the history is empty.</returns>
    public string? GetEntryAt(int index)
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
            var foundIndex = _fileHistory.IndexOf(list[index]);

            if (looping)
            {
                return GetEntryAt((foundIndex + 1 + _fileHistory.Count) % _fileHistory.Count);
            }

            foundIndex++;
            return foundIndex >= MaxCount ? null : GetEntryAt(foundIndex);
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
            var foundIndex = _fileHistory.IndexOf(list[index]);
            if (looping)
            {
                return GetEntryAt((foundIndex - 1 + _fileHistory.Count) % _fileHistory.Count);
            }

            foundIndex--;
            return foundIndex < 0 ? null : GetEntryAt(foundIndex);
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