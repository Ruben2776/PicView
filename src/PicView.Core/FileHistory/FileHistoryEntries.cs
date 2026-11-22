namespace PicView.Core.FileHistory;

public class FileHistoryEntries
{
    public bool IsSortingDescending { get; set; } = true;
    
    public List<Entry>? Entries { get; set; }

    /// <summary>The Last File: what the active tab of the Active Main Window showed most recently.</summary>
    public string? LastFile { get; set; }

    /// <summary>The root directory that sub-directory browsing started from.</summary>
    public string? StartUpDirectory { get; set; }

    /// <summary>The Last Session, or null when none was recorded.</summary>
    public LastSession? LastSession { get; set; }
}

public class Entry
{
    public required string Path { get; set; }
    
    public bool IsPinned { get; set; }
}
