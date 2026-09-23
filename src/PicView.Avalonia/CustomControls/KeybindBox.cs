using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace PicView.Avalonia.CustomControls;


public class KeybindBox : TemplatedControl
{
    /// <summary>
    /// Defines the <see cref="PlaceholderText"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<KeybindBox, string?>(nameof(PlaceholderText));
    
    /// <summary>
    /// Gets or sets the Text content of the TextBox
    /// </summary>
    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }
}