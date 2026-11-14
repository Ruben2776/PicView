using PicView.Core.DebugTools;

namespace PicView.Core.FileHandling;

public static class TempFileManager
{
    private static List<string>? _tempFiles;
    private static Lock? _lock;

    public static string GetNewTempFilePath(string fileName)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempPath);
        var filePath = Path.Combine(tempPath, fileName);
        _lock ??= new Lock();
        lock (_lock)
        {
            _tempFiles ??= [];
            _tempFiles.Add(filePath);
        }

        return filePath;
    }

    /// <summary>
    ///     Whether <paramref name="path" /> is inside the system's temp folder, at any depth. Such a file may be deleted
    ///     at any moment by whoever put it there, as a browser does with a dragged image.
    /// </summary>
    public static bool IsInTempFolder(string? path)
    {
        return IsInTempFolder(path, Path.GetTempPath(),
            OperatingSystem.IsWindows() || OperatingSystem.IsMacOS(), OperatingSystem.IsMacOS());
    }

    /// <param name="path">The path to check.</param>
    /// <param name="tempRoot">The temp folder.</param>
    /// <param name="ignoreCase">Whether the file system ignores letter case, as on Windows and macOS.</param>
    /// <param name="stripPrivatePrefix">
    ///     Whether a leading <c>/private</c> is ignored. On macOS <c>/var</c> and <c>/tmp</c> are links into
    ///     <c>/private</c>, so the same file arrives as <c>/var/folders/…</c> or <c>/private/var/folders/…</c>.
    /// </param>
    internal static bool IsInTempFolder(string? path, string tempRoot, bool ignoreCase, bool stripPrivatePrefix)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(tempRoot))
        {
            return false;
        }

        var file = Normalize(path, stripPrivatePrefix);
        var root = Normalize(tempRoot, stripPrivatePrefix);
        if (root.Length is 0 || file.Length <= root.Length + 1)
        {
            return false;
        }

        // Whole segments only, so C:/Temp2 is not inside C:/Temp
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return file[root.Length] == '/' && file.StartsWith(root, comparison);
    }

    /// <summary>Forward slashes only, no trailing slash, and optionally no leading <c>/private</c>.</summary>
    private static string Normalize(string path, bool stripPrivatePrefix)
    {
        const string privatePrefix = "/private/";
        var normalized = path.Replace('\\', '/').TrimEnd('/');
        if (stripPrivatePrefix && normalized.StartsWith(privatePrefix, StringComparison.Ordinal))
        {
            normalized = normalized[(privatePrefix.Length - 1)..];
        }

        return normalized;
    }

    public static void Cleanup()
    {
        if (_lock is null || _tempFiles is null)
        {
            return;
        }
        
        lock (_lock)
        {
            foreach (var file in _tempFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                    
                    var dir = Path.GetDirectoryName(file);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                catch (Exception e)
                {
                    DebugHelper.LogDebug(nameof(TempFileManager), nameof(Cleanup), e);
                }
            }
            _tempFiles.Clear();
        }
    }
}