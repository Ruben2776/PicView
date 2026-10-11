namespace PicView.Core.FileHistory;

public struct FavoriteEntry(string filePath, string hotKey)
{
    public string FilePath { get; set; } = filePath;
    public string HotKey { get; set; } = hotKey;
}

public class FavoritesEntries
{
    public List<FavoriteEntry>? Entries { get; set; }
}
