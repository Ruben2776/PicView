using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using PicView.Core.Config;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// A custom button control that displays an icon, which can be either a <see cref="DrawingImage"/>
/// or a <see cref="StreamGeometry"/>. It also supports dynamic brush changes to reflect hover states.
/// </summary>
public class IconButton : Button
{
    /// <summary>
    /// Defines the <see cref="Icon"/> property.
    /// The icon is displayed as a <see cref="DrawingImage"/> with support for dynamic brush changes.
    /// </summary>
    public static readonly AvaloniaProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<IconButton, DrawingImage?>(nameof(Icon));

    /// <summary>
    /// Defines the <see cref="Data"/> property.
    /// The icon can also be displayed as a <see cref="StreamGeometry"/> for path-based rendering.
    /// </summary>
    public static readonly AvaloniaProperty<StreamGeometry> PathProperty =
        AvaloniaProperty.Register<CopyButton, StreamGeometry>(nameof(Data));

    /// <summary>
    /// Defines the <see cref="IconWidth"/> property.
    /// The width of the icon, whether it is a <see cref="DrawingImage"/> or <see cref="StreamGeometry"/>.
    /// </summary>
    public static readonly AvaloniaProperty<double> IconWidthProperty =
        AvaloniaProperty.Register<IconButton, double>(nameof(IconWidth));

    /// <summary>
    /// Defines the <see cref="IconHeight"/> property.
    /// The height of the icon, whether it is a <see cref="DrawingImage"/> or <see cref="StreamGeometry"/>.
    /// </summary>
    public static readonly AvaloniaProperty<double> IconHeightProperty =
        AvaloniaProperty.Register<IconButton, double>(nameof(IconHeight));

    /// <summary>
    /// Overrides the default style key to <see cref="Button"/>.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(Button);

    /// <summary>
    /// Gets or sets the <see cref="DrawingImage"/> displayed as the icon of the button.
    /// </summary>
    [Content]
    public DrawingImage? Icon
    {
        get => (DrawingImage?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="StreamGeometry"/> used as the icon's path data.
    /// </summary>
    public StreamGeometry Data
    {
        get => (StreamGeometry)GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    /// <summary>
    /// Gets or sets the width of the icon.
    /// </summary>
    public double IconWidth
    {
        get => (double)GetValue(IconWidthProperty);
        set => SetValue(IconWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the height of the icon.
    /// </summary>
    public double IconHeight
    {
        get => (double)GetValue(IconHeightProperty);
        set => SetValue(IconHeightProperty, value);
    }

    /// <summary>
    /// Called when the control is added to a visual tree. Initializes the content of the button with the icon.
    /// </summary>
    /// <param name="e">The event data associated with attaching the visual tree.</param>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Content = BuildIcon();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty)
        {
            Content = BuildIcon();
        }
    }

    /// <summary>
    /// Builds the icon for the button, either from a <see cref="DrawingImage"/> or a <see cref="StreamGeometry"/>.
    /// It also sets up dynamic brush updates on mouse hover.
    /// </summary>
    /// <returns>A <see cref="Control"/> representing the icon, or <c>null</c> if no icon is set.</returns>
    private Control? BuildIcon()
    {
        if (Icon is DrawingImage { Drawing: DrawingGroup drawingGroup })
        {
            // Set the initial pen brush to match the Foreground color
            foreach (var drawing in drawingGroup.Children)
            {
                if (drawing is not GeometryDrawing { Pen: Pen pen })
                {
                    continue;
                }

                if (SettingsHelper.Settings.Theme.GlassTheme)
                {
                    if (!Application.Current.TryGetResource("SecondaryTextColor",
                            Application.Current.RequestedThemeVariant, out var secondaryAccentColor))
                    {
                        continue;
                    }

                    if (secondaryAccentColor is Color color)
                    {
                        pen.Brush = new SolidColorBrush(color);
                    }
                }
                else
                {
                    pen.Brush = Foreground;
                }
            }

            var image = new Image
            {
                Source = Icon,
                Width = IconWidth,
                Height = IconHeight
            };

            // Change brush to secondary accent color on pointer enter
            PointerEntered += delegate
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!Application.Current.TryGetResource("SecondaryTextColor",
                            Application.Current.RequestedThemeVariant, out var secondaryAccentColor))
                    {
                        return;
                    }
#if DEBUG
                    Debug.Assert(secondaryAccentColor != null, nameof(secondaryAccentColor) + " != null");
#endif
                    foreach (var drawing in drawingGroup.Children)
                    {
                        if (drawing is GeometryDrawing { Pen: Pen pen })
                        {
                            pen.Brush = new SolidColorBrush((Color)(secondaryAccentColor));
                        }
                    }
                });
            };

            // Revert brush to main text color on pointer exit
            PointerExited += delegate
            {
                if (SettingsHelper.Settings.Theme.GlassTheme)
                {
return;
                }
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!Application.Current.TryGetResource("MainTextColor", Application.Current.RequestedThemeVariant,
                            out var mainTextColor))
                    {
                        return;
                    }
#if DEBUG
                    Debug.Assert(mainTextColor != null, nameof(mainTextColor) + " != null");
#endif
                    foreach (var drawing in drawingGroup.Children)
                    {
                        if (drawing is GeometryDrawing { Pen: Pen pen })
                        {
                            pen.Brush = new SolidColorBrush((Color)(mainTextColor));
                        }
                    }
                });
            };

            return image;
        }

        // If no DrawingImage, use PathIcon
        // Make sure button has the hover class and the Foreground property is set
        if (Data is null)
        {
            return null;
        }

        var pathIcon = new PathIcon
        {
            Data = Data,
            Width = IconWidth,
            Height = IconHeight
        };

        return pathIcon;
    }
}
