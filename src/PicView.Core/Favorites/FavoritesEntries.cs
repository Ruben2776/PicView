using System.Text.Json.Serialization;

namespace PicView.Core.Favorites;

public struct FavoriteEntry(string filePath, string keybind)
{
    [JsonPropertyName("FilePath")]
    public string FilePath { get; set; } = filePath;

    public string Keybind { get; set; } = keybind;
}

public class FavoritesEntries
{
    public List<FavoriteEntry>? Entries { get; set; }
}
