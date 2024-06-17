using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Utilities;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Views;

namespace PicView.Avalonia.CustomControls;

public class PicBox : Control
{
    static PicBox()
    {
        // Registers the SourceProperty to render when the source changes
        AffectsRender<PicBox>(SourceProperty);
    }
    
    #region Properties
    /// <summary>
    /// Defines the <see cref="Source"/> property.
    /// </summary>
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<PicBox, IImage?>(nameof(Source));
    
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
    /// Defines the <see cref="SecondSource"/> property.
    /// </summary>
    public static readonly StyledProperty<IImage?> SecondSourceProperty =
        AvaloniaProperty.Register<PicBox, IImage?>(nameof(SecondSource));
    
    /// <summary>
    /// Gets or sets the second image that will be displayed, when side by side view is enabled
    /// </summary>
    [Content]
    public IImage? SecondSource
    {
        get => GetValue(SecondSourceProperty);
        set => SetValue(SecondSourceProperty, value);
    }
    
    /// <summary>
    /// Defines the <see cref="ImageType"/> property.
    /// </summary>
    public static readonly AvaloniaProperty<ImageType> ImageTypeProperty =
        AvaloniaProperty.Register<PicBox, ImageType>(nameof(ImageType));

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

    #region Rendering
    
    /// <summary>
    /// Renders the control.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    public sealed override void Render(DrawingContext context)
    {
        base.Render(context);
        
        var source = Source;
        if (source == null)
        {
            return;
        }
        
        var sourceSize = source.Size;
        var viewPort = DetermineViewPort();
        
        var is1To1 = false; // TODO: replace with settings value
        var isSideBySide = false; // TODO: replace with settings value
        if (is1To1)
        {
            RenderImage1To1(context, source, viewPort, sourceSize);
        }
        else if (isSideBySide)
        {
            RenderImageSideBySide(context, source, viewPort, sourceSize);
        }
        else
        {
            RenderImage(context, source, viewPort, sourceSize);
        }
    }
    
    private Rect DetermineViewPort()
    {
        if (!(Bounds.Width <= 0) && !(Bounds.Height <= 0))
        {
            return new Rect(Bounds.Size);
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return new Rect();
        }

        var mainView = desktop.MainWindow?.GetControl<MainView>("MainView");
        return mainView == null ? new Rect() : new Rect(Bounds.X, Bounds.Y, mainView.Bounds.Width, mainView.Bounds.Height);
    }
    
    private void RenderImage1To1(DrawingContext context, IImage source, Rect viewPort, Size sourceSize)
    {
        var scale = 1.0;
        var scaledSize = sourceSize * scale;
        var destRect = viewPort.CenterRect(new Rect(scaledSize)).Intersect(viewPort);
        var sourceRect = new Rect(sourceSize).CenterRect(new Rect(destRect.Size / scale));

        context.DrawImage(source, sourceRect, destRect);
    }

    private void RenderImage(DrawingContext context, IImage source, Rect viewPort, Size sourceSize)
    {
        var scale = CalculateScaling(viewPort.Size, sourceSize);
        var scaledSize = sourceSize * scale;
        var destRect = viewPort.CenterRect(new Rect(scaledSize)).Intersect(viewPort);
        var sourceRect = new Rect(sourceSize).CenterRect(new Rect(destRect.Size / scale));

        context.DrawImage(source, sourceRect, destRect);
    }
    
    private void RenderImageSideBySide(DrawingContext context, IImage source, Rect viewPort, Size sourceSize)
    {
        // TODO Add side by side viewing mode
    }
    
    private static Vector CalculateScaling(Size destinationSize, Size sourceSize)
    {
        var isConstrainedWidth = !double.IsPositiveInfinity(destinationSize.Width);
        var isConstrainedHeight = !double.IsPositiveInfinity(destinationSize.Height);

        // Compute scaling factors for both axes
        var scaleX = MathUtilities.IsZero(sourceSize.Width) ? 0.0 : destinationSize.Width / sourceSize.Width;
        var scaleY = MathUtilities.IsZero(sourceSize.Height) ? 0.0 : destinationSize.Height / sourceSize.Height;

        if (!isConstrainedWidth)
        {
            scaleX = scaleY;
        }
        else if (!isConstrainedHeight)
        {
            scaleY = scaleX;
        }

        return new Vector(scaleX, scaleY);
    }
    
    public static Size CalculateSize(Size destinationSize, Size sourceSize)
    {
        return sourceSize * CalculateScaling(destinationSize, sourceSize);
    }

    /// <summary>
    /// Measures the control.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size of the control.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        return Source != null ? CalculateSize(availableSize, Source.Size) : new Size();
    }
    
    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateLayout();
        return base.ArrangeOverride(finalSize);
    }
    
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new ImageAutomationPeer(this);
    }

    #endregion
    
    #region Pan and Zoom
    
    // TODO: Add Pan and Zoom
    
    #endregion 
    
    #region Rotation
    
    // TODO: Add Rotation
    
    #endregion

    #region Animated Pic

    // TODO: Add Animation behavior

    #endregion
}