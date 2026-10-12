using System.Text.Json.Serialization;

namespace PicView.Core.Favorites;

public struct FavoriteEntry(string filePath, string hotKey)
{
    [JsonPropertyName("FilePath")]
    public string FilePath { get; set; } = filePath;

    public string HotKey { get; set; } = hotKey;
}

public class FavoritesEntries
{
    public List<FavoriteEntry>? Entries { get; set; }
}
