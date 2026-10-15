using System.Globalization;
using Avalonia.Data.Converters;
using PicView.Avalonia.Input;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Converters;

/// <summary>
/// Converts a favorite's slot to the text of the Keybind bound to that slot's action,
/// or an empty string when it has no slot or no Keybind.
/// </summary>
public class FavoriteSlotKeybindConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int slot)
        {
            return string.Empty;
        }

        var keybinds = Keymaps.Current.KeybindsFor(KeybindCategoryDefinition.FavoriteAction(slot));
        return keybinds.Count > 0 ? keybinds[0].ToString() : string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
