using PicView.Core.ViewModels;

namespace PicView.Core.SettingsSearch;

/// <summary>
/// One suggestion of the Settings window's search. With a <paramref name="Key"/> it leads to that setting row;
/// without one it leads to the top of its <paramref name="Category"/>.
/// </summary>
/// <param name="Category">The category the setting is in, or the category itself</param>
/// <param name="Key">The setting row, or null for a category</param>
/// <param name="Name">The text the suggestion shows</param>
/// <param name="CategoryName">The category's text, shown beside a setting; empty for a category</param>
/// <param name="Keywords">Further words that find it: its options and synonyms</param>
public record SettingsSearchItem(
    SettingsCategory Category,
    SettingKey? Key,
    string Name,
    string CategoryName,
    string Keywords)
{
    public bool IsCategory => Key is null;
}
