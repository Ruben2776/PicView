using Avalonia;
using Avalonia.Controls;
using PicView.Core.SettingsSearch;

namespace PicView.Avalonia.UI;

public abstract class SearchProperties : AvaloniaObject
{
    /// <summary>
    /// Marks a setting row in the Settings window with the key its search suggestion leads to.
    /// </summary>
    public static readonly AttachedProperty<SettingKey?> KeyProperty =
        AvaloniaProperty.RegisterAttached<SearchProperties, Control, SettingKey?>("Key");

    public static SettingKey? GetKey(Control element) => element.GetValue(KeyProperty);
    public static void SetKey(Control element, SettingKey? value) => element.SetValue(KeyProperty, value);
}
