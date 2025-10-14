using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PicView.Avalonia.CustomControls;

public class HighlightableTextBlock : Control
{
    protected override Type StyleKeyOverride => typeof(TextBlock);

    /// <summary>
    /// Defines the Text property.
    /// </summary>
    public static readonly StyledProperty<string?> TextProperty =
        TextBlock.TextProperty.AddOwner<HighlightableTextBlock>();

    /// <summary>
    /// Defines the Foreground property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextBlock.ForegroundProperty.AddOwner<HighlightableTextBlock>();

    /// <summary>
    /// Defines the HighlightBrush property for the highlighted text section.
    /// </summary>
    public static readonly StyledProperty<IBrush?> HighlightBrushProperty =
        AvaloniaProperty.Register<HighlightableTextBlock, IBrush?>(nameof(HighlightBrush));

    /// <summary>
    /// Defines the start index of the highlight.
    /// </summary>
    public static readonly StyledProperty<int> HighlightStartProperty =
        AvaloniaProperty.Register<HighlightableTextBlock, int>(nameof(HighlightStart), -1);

    /// <summary>
    /// Defines the end index of the highlight.
    /// </summary>
    public static readonly StyledProperty<int> HighlightEndProperty =
        AvaloniaProperty.Register<HighlightableTextBlock, int>(nameof(HighlightEnd), -1);

    // Add other text properties for a more complete control
    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextBlock.FontFamilyProperty.AddOwner<HighlightableTextBlock>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextBlock.FontSizeProperty.AddOwner<HighlightableTextBlock>();

    public static readonly StyledProperty<FontStyle> FontStyleProperty =
        TextBlock.FontStyleProperty.AddOwner<HighlightableTextBlock>();

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        TextBlock.FontWeightProperty.AddOwner<HighlightableTextBlock>();

    public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
        TextBlock.TextWrappingProperty.AddOwner<HighlightableTextBlock>();

    // Static constructor to register property change handlers
    static HighlightableTextBlock()
    {
        // Any property change that affects the visual appearance should trigger a redraw.
        AffectsRender<HighlightableTextBlock>(
            TextProperty,
            ForegroundProperty,
            HighlightBrushProperty,
            HighlightStartProperty,
            HighlightEndProperty,
            FontFamilyProperty,
            FontSizeProperty,
            FontStyleProperty,
            FontWeightProperty);

        // Properties that affect the size of the control should trigger a remeasure.
        AffectsMeasure<HighlightableTextBlock>(
            TextProperty,
            FontSizeProperty,
            FontFamilyProperty,
            FontWeightProperty,
            FontStyleProperty,
            TextWrappingProperty);
    }

    // CLR Property Wrappers
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public IBrush? HighlightBrush
    {
        get => GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    public int HighlightStart
    {
        get => GetValue(HighlightStartProperty);
        set => SetValue(HighlightStartProperty, value);
    }

    public int HighlightEnd
    {
        get => GetValue(HighlightEndProperty);
        set => SetValue(HighlightEndProperty, value);
    }

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontStyle FontStyle
    {
        get => GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }


    /// <summary>
    /// Renders the control.
    /// </summary>
    public override void Render(DrawingContext context)
    {
        var text = Text;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // Create the FormattedText object
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle, FontWeight),
            FontSize,
            Foreground);

        // 1. Apply the default foreground color to the entire text
        if (Foreground != null)
        {
            formattedText.SetForegroundBrush(Foreground);
        }

        // 2. Apply the highlight brush if the range and brush are valid
        var highlightBrush = HighlightBrush;
        var start = HighlightStart;
        var end = HighlightEnd;

        if (highlightBrush != null && start >= 0 && end > start && end <= text.Length)
        {
            var length = end - start;
            formattedText.SetForegroundBrush(highlightBrush, start, length);
        }

        // Draw the formatted text to the screen
        context.DrawText(formattedText, new Point(0, 0));
    }

    /// <summary>
    /// Measures the desired size of the control.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var text = Text;
        if (string.IsNullOrEmpty(text))
        {
            return new Size();
        }

        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle, FontWeight),
            FontSize,
            Foreground);

        return new Size(formattedText.Width, formattedText.Height);
    }
}