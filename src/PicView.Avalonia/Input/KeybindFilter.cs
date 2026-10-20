namespace PicView.Avalonia.Input;

/// <summary>
/// Decides whether a row of the keybindings view matches the filter typed into its search box. A row matches when the
/// filter is found, ignoring case, in its display name, its function name, or the text of one of its Keybinds.
/// </summary>
public static class KeybindFilter
{
    /// <param name="filterText">The text typed into the search box. A blank filter matches every row.</param>
    /// <param name="functionName">The name of the row's Action.</param>
    /// <param name="displayName">The name the row shows to the user.</param>
    /// <param name="keybindTexts">The text of each Keybind the row's Action has.</param>
    public static bool Matches(
        string? filterText,
        string functionName,
        string displayName,
        IEnumerable<string> keybindTexts)
    {
        if (string.IsNullOrWhiteSpace(filterText))
        {
            return true;
        }

        if (displayName.Contains(filterText, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (functionName.Contains(filterText, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var keybindText in keybindTexts)
        {
            if (keybindText.Contains(filterText, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
