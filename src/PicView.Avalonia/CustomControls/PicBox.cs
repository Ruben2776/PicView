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
using ReactiveUI;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using Avalonia.Threading;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Calculations;
using PicView.Core.Config;


namespace PicView.Avalonia.CustomControls;
public class PicBox : Control
{
    #region Constructors
    
    static PicBox()
    {
        // Registers the SourceProperty to render when the source changes
        AffectsRender<PicBox>(SourceProperty);
    }
    public PicBox()
    {
        _imageTypeSubscription = this.WhenAnyValue(x => x.ImageType)
            .Subscribe(UpdateSource);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _imageTypeSubscription.Dispose();
    }

    #endregion

    #region Properties
    
    private readonly IDisposable? _imageTypeSubscription;
    
    /// <summary>
    /// Defines the <see cref="Source"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> SourceProperty =
        AvaloniaProperty.Register<PicBox, object?>(nameof(Source));

    /// <summary>
    /// Gets or sets the image that will be displayed.
    /// </summary>
    [Content]
    public object? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="SecondarySource"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> SecondarySourceProperty =
        AvaloniaProperty.Register<PicBox, object?>(nameof(SecondarySource));

    /// <summary>
    /// Gets or sets the second image that will be displayed, when side by side view is enabled
    /// </summary>
    [Content]
    public object? SecondarySource
    {
        get => GetValue(SecondarySourceProperty);
        set => SetValue(SecondarySourceProperty, value);
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

        switch (Source)
        {
            case IImage source:
                RenderBasedOnSettings(context, source);
                break;
            case string svg:
            {
                var svgSource = SvgSource.Load(svg);
                var svgImage = new SvgImage { Source = svgSource };
                RenderBasedOnSettings(context, svgImage);
                break;
            }
            default:
                // Handle invalid source or log error
#if DEBUG
                Console.WriteLine("Invalid source type.");
                TooltipHelper.ShowTooltipMessage("Invalid source type.", true);
#endif
                break;
        }
    }

    private void RenderBasedOnSettings(DrawingContext context, IImage source)
    {
        var viewPort = DetermineViewPort();
        var sourceSize = source.Size;
    
        var is1To1 = false; // TODO: replace with settings value
        var isSideBySide = false; // TODO: replace with settings value
    
        if (is1To1)
        {
            RenderImage1To1(context, source, viewPort, sourceSize);
        }
        else if (isSideBySide)
        {
            if (SecondarySource is IImage secondarySource)
            {
                RenderImageSideBySide(context, source, secondarySource, viewPort);
            }
            else
            {
                // Handle invalid secondary source
#if DEBUG
                Console.WriteLine("Invalid secondary source type.");
                TooltipHelper.ShowTooltipMessage("Invalid secondary source type.", true);
#endif
            }
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

        var mainView = UIHelper.GetMainView;
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

    private void RenderImageSideBySide(DrawingContext context, IImage source, IImage secondarySource, Rect viewPort)
    {
        // Get the aspect ratios of the images
        var sourceAspectRatio = source.Size.Width / source.Size.Height;
        var secondarySourceAspectRatio = secondarySource.Size.Width / secondarySource.Size.Height;

        // Calculate the width for each image
        var halfViewportWidth = viewPort.Width / 2;

        // Calculate heights based on the aspect ratio
        var sourceHeight = halfViewportWidth / sourceAspectRatio;
        var secondarySourceHeight = halfViewportWidth / secondarySourceAspectRatio;

        // Calculate the rectangles for each image
        var sourceRect = new Rect(0, 0, halfViewportWidth, sourceHeight);
        var secondarySourceRect = new Rect(halfViewportWidth, 0, halfViewportWidth, secondarySourceHeight);

        // Draw the first image
        context.DrawImage(source, new Rect(source.Size), sourceRect);

        // Draw the second image
        context.DrawImage(secondarySource, new Rect(secondarySource.Size), secondarySourceRect);
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
        return Source is not IImage source ? new Size() : CalculateSize(availableSize, source.Size);
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
    
    #region Helper Methods

    private void UpdateSource(ImageType imageType)
    {
        switch (imageType)
        {
            case ImageType.Svg:
                if (Source is not string svg)
                {
                    goto default;
                }
                var svgSource = SvgSource.Load(svg);
                Source = new SvgImage { Source = svgSource };
                break;
            case ImageType.Bitmap:
                Source = Source as Bitmap;
                break;
            case ImageType.AnimatedBitmap:
                Source = Source as Bitmap;
                // TODO: Add animation
                break;
            case ImageType.Invalid:
            default:
                // TODO: Add invalid image graphic
                break;
        }
    }

    #endregion

    #region Animation

    // TODO: Add Animation behavior

    #endregion
}