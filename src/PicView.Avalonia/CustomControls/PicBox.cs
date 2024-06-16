using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Metadata;
using PicView.Avalonia.Navigation;
using PicView.Core.Config;

namespace PicView.Avalonia.CustomControls;

public class PicBox : Control
{
    #region Properties
    /// <summary>
    /// Defines the <see cref="Source"/> property.
    /// </summary>
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<Image, IImage?>(nameof(Source));
    
    /// <summary>
    /// Gets or sets the image that will be displayed.
    /// </summary>
    [Content]
    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }
    
    /// <summary>
    /// Defines the <see cref="ImageType"/> property.
    /// </summary>
    public static readonly AvaloniaProperty<ImageType> ImageTypeProperty =
        AvaloniaProperty.Register<AnimatedMenu, ImageType>(nameof(ImageType));

    /// <summary>
    /// Gets or sets the image type.
    /// Determines if <see cref="Source"/> is an animated image, scalable vector graphics (SVG) or raster image.
    /// </summary>
    public ImageType ImageType
    {
        get => (ImageType)(GetValue(ImageTypeProperty) ?? false);
        set => SetValue(ImageTypeProperty, value);
    }
    
    #endregion
    
    static PicBox()
    {
        // Registers the SourceProperty to render when the source changes
        AffectsRender<PicBox>(SourceProperty);
    }

    #region Rendering
    
    /// <summary>
    /// Renders the control.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    public sealed override void Render(DrawingContext context)
    {
        var source = Source;

        if (source == null || Bounds is not { Width: > 0, Height: > 0 })
        {
            return;
        }

        var is1to1 = true; // TODO: replace with settings value

        if (is1to1)
        {
            var viewPort = new Rect(Bounds.Size);
            var sourceSize = source.Size;
        
            var scale = 1; 
            var scaledSize = sourceSize * scale;
            var destRect = viewPort
                .CenterRect(new Rect(scaledSize))
                .Intersect(viewPort);
            var sourceRect = new Rect(sourceSize)
                .CenterRect(new Rect(destRect.Size / scale));

            context.DrawImage(source, sourceRect, destRect);
        }
        // TODO: Implement other size modes
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        return Source?.Size ?? availableSize;
    }
    
    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        return Source?.Size ?? finalSize;
    }
    
    #endregion
}