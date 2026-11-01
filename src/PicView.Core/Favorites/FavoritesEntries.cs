using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace PicView.Core.Favorites;

/// <summary>
/// A favorite folder. <see cref="Slot"/> is its number in 1..<see cref="FavoritesManager.MaxSlots"/>,
/// which gives it the OpenFavorite action of that number, or <c>null</c> when it has no slot.
/// </summary>
[StructLayout(LayoutKind.Auto)] 
public struct FavoriteEntry(string filePath, int? slot)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Slot { get; set; } = slot;

    [JsonPropertyName("FilePath")]
    public string FilePath { get; set; } = filePath;
}

public class FavoritesEntries
{
    public List<FavoriteEntry>? Entries { get; set; }
}
