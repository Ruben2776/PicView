using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace PicView.Avalonia.CustomControls;

[TemplatePart(PartTagBox, typeof(TagBox))]
public class KeybindBox : TemplatedControl
{
    public const string PartTagBox = "PART_TagBox";

    internal TagBox? TagBox { get; private set; }

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

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        TagBox = e.NameScope.Find<TagBox>(PartTagBox);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if (ReferenceEquals(e.Source, this))
        {
            TagBox?.Focus();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        TagBox?.Focus();
    }
}