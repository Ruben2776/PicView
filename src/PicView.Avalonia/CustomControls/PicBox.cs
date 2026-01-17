using System.Numerics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Svg.Skia;
using ImageMagick;
using PicView.Avalonia.AnimatedImage;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;
using R3;
using CompositeDisposable = R3.CompositeDisposable;
using Vector = Avalonia.Vector;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// Custom control for displaying images with additional functionalities
/// such as handling image types, side-by-side with a secondary source, and animated rendering.
/// </summary>
public class PicBox : Control, IDisposable
{
    #region Helper Methods

    private Rect DetermineViewPort()
    {
        if (Bounds is { Width: > 0, Height: > 0 })
        {
            return new Rect(Bounds.Size);
        }

        var mainView = UIHelper.GetMainView;
        return mainView == null
            ? new Rect()
            : new Rect(Bounds.X, Bounds.Y, mainView.Bounds.Width, mainView.Bounds.Height);
    }

    #endregion

    #region Fields and Properties

    private CompositionCustomVisual? _customVisual;
    private FileStream? _stream;
    private IGifInstance? _animInstance;
    public string? InitialAnimatedSource;
    private readonly CompositeDisposable _imageTypeSubscription = new(); // Should be used for disposal when tab navigation arrives
    private bool _isDisposed;

    public static readonly StyledProperty<object?> SourceProperty =
        AvaloniaProperty.Register<PicBox, object?>(nameof(Source));

    /// <summary>
    ///     Gets or sets the image that will be displayed.
    /// </summary>
    public object? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly StyledProperty<object?> SecondarySourceProperty =
        AvaloniaProperty.Register<PicBox, object?>(nameof(SecondarySource));

    /// <summary>
    ///     Gets or sets the second image that will be displayed, when side by side view is enabled
    /// </summary>
    public object? SecondarySource
    {
        get => GetValue(SecondarySourceProperty);
        set => SetValue(SecondarySourceProperty, value);
    }

    public static readonly StyledProperty<double> SecondaryImageWidthProperty =
        AvaloniaProperty.Register<PicBox, double>(nameof(SecondaryImageWidth));

    public double SecondaryImageWidth
    {
        get => GetValue(SecondaryImageWidthProperty);
        set => SetValue(SecondaryImageWidthProperty, value);
    }

    /// <summary>
    ///     Defines the <see cref="ImageType" /> property.
    /// </summary>
    public static readonly AvaloniaProperty<ImageType> ImageTypeProperty =
        AvaloniaProperty.Register<PicBox, ImageType>(nameof(ImageType));

    /// <summary>
    ///     Gets or sets the image type.
    ///     Determines if <see cref="Source" /> is an animated image, scalable vector graphics (SVG) or raster image.
    /// </summary>
    public ImageType ImageType
    {
        get => (ImageType)(GetValue(ImageTypeProperty) ?? false);
        set => SetValue(ImageTypeProperty, value);
    }

    #endregion

    #region Constructors

    static PicBox()
    {
        // Registers the SourceProperty to render when the source changes
        AffectsRender<PicBox>(SourceProperty);
    }

    public PicBox() =>
        this.GetObservable(ImageTypeProperty).ToObservable()
            .Skip(1) // Skip the initial unset one
            .Subscribe(UpdateSource)
            .AddTo(_imageTypeSubscription);

    private void UpdateSource(ImageType imageType)
    {
        switch (imageType)
        {
            case ImageType.Svg:
                UpdateSvgSource();
                CleanupResources();
                break;
            case ImageType.AnimatedGif:
            case ImageType.AnimatedWebp:
                UpdateAnimatedSource();
                break;
            case ImageType.Bitmap:
                UpdateBitmapSource();
                CleanupResources();
                break;
            case ImageType.Invalid:
            default:
                CleanupResources();
                // TODO: Add invalid image graphic
                break;
        }
    }

    #endregion

    #region Source Management

    private void UpdateSvgSource()
    {
        if (Source is not string svg)
        {
            return;
        }

        var svgSource = SvgSource.LoadFromSvg(svg);
        Source = new SvgImage { Source = svgSource };
    }

    private void UpdateAnimatedSource()
    {
        CreateVisual();
        Source = Source as Bitmap;
    }

    private void UpdateBitmapSource()
    {
        Source = Source as Bitmap;
    }

    private void CleanupResources()
    {
        DestroyVisual();
        _animInstance?.Dispose();
        _animInstance = null;
        _stream?.Dispose();
        _stream = null;
    }

    #endregion

    #region Rendering

    /// <summary>
    ///     Renders the image represented by <see cref="Source" />.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    public sealed override void Render(DrawingContext context)
    {
        base.Render(context);

        switch (Source)
        {
            case IImage source:
                RenderImageSource(context, source);
                break;
            case string svg:
                RenderSvgSource(context, svg);
                break;
            default:
                HandleInvalidSource();
                break;
        }
    }

    private void RenderImageSource(DrawingContext context, IImage source)
    {
        RenderBasedOnSettings(context, source);
        RenderAnimatedImageIfRequired(context);
    }

    private void RenderSvgSource(DrawingContext context, string svg)
    {
        var svgSource = SvgSource.LoadFromSvg(svg);
        var svgImage = new SvgImage { Source = svgSource };
        RenderBasedOnSettings(context, svgImage);
    }

    private void HandleInvalidSource()
    {
        if (Source != null)
        {
            DebugHelper.LogDebug(nameof(PicBox), nameof(HandleInvalidSource), "Invalid source type.");
        }
    }

    private void RenderAnimatedImageIfRequired(DrawingContext context)
    {
        if (ImageType is not (ImageType.AnimatedGif or ImageType.AnimatedWebp) ||
            string.IsNullOrWhiteSpace(InitialAnimatedSource))
        {
            return;
        }
        
        context.Dispose(); // Fixes transparent images
        _stream = new FileStream(InitialAnimatedSource, FileMode.Open, FileAccess.Read);
        UpdateAnimationInstance(_stream);
        AnimationUpdate();
    }

    private void RenderBasedOnSettings(DrawingContext context, IImage source)
    {
        if (source == null)
        {
            return;
        }

        var viewPort = DetermineViewPort();

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            var secondarySource = SecondarySource as IImage;
            RenderImageSideBySide(context, source, secondarySource, viewPort, GetImageSize(source),
                GetSecondaryImageInfo(secondarySource));
        }
        else
        {
            RenderImage(context, source, viewPort, GetImageSize(source));
        }
    }

    private Size GetImageSize(IImage source)
    {
        try
        {
            return source?.Size ?? GetSizeFromAlternativeSources();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PicBox), nameof(GetImageSize), e);
            return GetSizeFromAlternativeSources();
        }
    }

    private Size GetSizeFromAlternativeSources()
    {
        if (DataContext is not MainViewModel vm)
        {
            return new Size();
        }

        var preloadValue = NavigationManager.GetCurrentPreLoadValue();
        if (preloadValue?.ImageModel != null)
        {
            return new Size(preloadValue.ImageModel.PixelWidth, preloadValue.ImageModel.PixelHeight);
        }

        if (vm.PicViewer.FileInfo?.CurrentValue?.Exists != true)
        {
            return new Size();
        }

        try
        {
            using var magickImage = new MagickImage();
            magickImage.Ping(vm.PicViewer.FileInfo.CurrentValue);
            return new Size(magickImage.Width, magickImage.Height);
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(PicBox), nameof(GetSizeFromAlternativeSources), exception);
        }

        return new Size();
    }

    private Size GetSecondaryImageInfo(IImage? secondarySource)
    {
        if (secondarySource == null)
        {
            return new Size();
        }

        try
        {
            return secondarySource.Size;
        }
        catch (Exception)
        {
            if (DataContext is not MainViewModel vm)
            {
                return new Size();
            }

            var nextPreloadValue = NavigationManager.GetNextPreLoadValue();
            if (nextPreloadValue?.ImageModel != null)
            {
                return new Size(nextPreloadValue.ImageModel.PixelWidth, nextPreloadValue.ImageModel.PixelHeight);
            }

            if (NavigationManager.CanNavigate(vm))
            {
                try
                {
                    using var magickImage = new MagickImage();
                    magickImage.Ping(NavigationManager.GetNextFileName);
                    return new Size(magickImage.Width, magickImage.Height);
                }
                catch
                {
                    return new Size();
                }
            }
        }

        return new Size();
    }

    private void RenderImage(DrawingContext context, IImage source, Rect viewPort, Size sourceSize)
    {
        if (source is null)
        {
            DebugHelper.LogDebug(nameof(PicBox), nameof(RenderImage), "source is null");
            return;
        }
        var scale = CalculateScaling(viewPort.Size, sourceSize);
        var scaledSize = sourceSize * scale;
        var destRect = viewPort.CenterRect(new Rect(scaledSize)).Intersect(viewPort);
        var sourceRect = new Rect(sourceSize).CenterRect(new Rect(destRect.Size / scale));

        try
        {
            context.DrawImage(source, sourceRect, destRect);
        }
        catch (ObjectDisposedException e)
        {
            DebugHelper.LogDebug(nameof(PicBox), nameof(RenderImage), e);
            
            var preloadValue = NavigationManager.GetCurrentPreLoadValue();
            if (preloadValue?.ImageModel?.Image != null)
            {
                try
                {
                    context.DrawImage(preloadValue?.ImageModel?.Image as IImage, sourceRect, destRect);
                }
                catch (Exception exception)
                {
                    DebugHelper.LogDebug(nameof(PicBox), nameof(RenderImage), exception);
                }
            }
            else
            {
                // Last resort bug fix
                var asyncPreloadValue = NavigationManager.GetCurrentPreLoadValueAsync().GetAwaiter().GetResult();
                if (asyncPreloadValue?.ImageModel?.Image is IImage image)
                {
                    context.DrawImage(image, sourceRect, destRect);
                }
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PicBox), nameof(RenderImage), e);
        }
    }

    private void RenderImageSideBySide(DrawingContext context, IImage source, IImage? secondarySource, Rect viewPort,
        Size sourceSize, Size secondarySourceSize)
    {
        if (source == null || secondarySource == null)
        {
            return;
        }

        // Scale both images based on the height of the viewport
        var scale = viewPort.Height / Math.Max(sourceSize.Height, secondarySourceSize.Height);

        // Calculate the scaled size of the second image based on the specified width (SecondaryImageWidth)
        var scaledSecondarySize = new Size(SecondaryImageWidth, secondarySourceSize.Height * scale);

        // Calculate the remaining width for the first image
        var firstImageWidth = viewPort.Width - scaledSecondarySize.Width;

        if (firstImageWidth <= 0)
        {
            // If there's no space left for the first image, don't render anything
            return;
        }

        // Calculate the destination rectangles for both images
        var sourceDestRect = new Rect(0, 0, firstImageWidth, viewPort.Height);
        var secondaryDestRect = new Rect(firstImageWidth, 0, SecondaryImageWidth, viewPort.Height);

        // Calculate the source rectangles (ensuring the aspect ratio is maintained)
        var sourceRect = new Rect(sourceSize);
        var secondarySourceRect = new Rect(secondarySourceSize);

        try
        {
            // Render the first image (filling the remaining space)
            context.DrawImage(source, sourceRect, sourceDestRect);

            // Render the second image (with the fixed SecondaryImageWidth)
            context.DrawImage(secondarySource, secondarySourceRect, secondaryDestRect);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PicBox), nameof(RenderImageSideBySide), e);
        }
    }

    #endregion

    #region Measurement and Layout

    /// <summary>
    ///     Measures the control.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size of the control.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Source is not IImage source)
        {
            return new Size();
        }

        try
        {
            return CalculateSize(availableSize, source.Size);
        }
        catch (Exception)
        {
            return GetSizeFromAlternativeSources();
        }
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateLayout();
        return base.ArrangeOverride(finalSize);
    }

    #endregion

    #region Calculations

    private static Vector CalculateScaling(Size destinationSize, Size sourceSize)
    {
        var isConstrainedWidth = !double.IsPositiveInfinity(destinationSize.Width);
        var isConstrainedHeight = !double.IsPositiveInfinity(destinationSize.Height);

        // Compute scaling factors for both axes
        var scaleX = Math.Abs(sourceSize.Width) < double.Epsilon ? 0.0 : destinationSize.Width / sourceSize.Width;
        var scaleY = Math.Abs(sourceSize.Height) < double.Epsilon ? 0.0 : destinationSize.Height / sourceSize.Height;

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

    private static Size CalculateSize(Size destinationSize, Size sourceSize)
    {
        return sourceSize * CalculateScaling(destinationSize, sourceSize);
    }

    #endregion

    #region Animation

    private void UpdateAnimationInstance(FileStream fileStream)
    {
        _animInstance?.Dispose();
        try
        {
            _animInstance = ImageType == ImageType.AnimatedGif
                ? new GifInstance(fileStream)
                : new WebpInstance(fileStream);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PicBox), nameof(UpdateAnimatedSource), e);
        }

        _animInstance.IterationCount = IterationCount.Infinite;
        if (_customVisual is null)
        {
            CreateVisual();
        }
        _customVisual?.SendHandlerMessage(_animInstance);
        AnimationUpdate();
    }

    private void AnimationUpdate()
    {
        if (_customVisual is null)
        {
            CreateVisual();
        }

        var sourceSize = Bounds.Size;
        var viewPort = DetermineViewPort();

        var scale = CalculateScaling(viewPort.Size, sourceSize);
        var scaledSize = sourceSize * scale;
        var destRect = viewPort.CenterRect(new Rect(scaledSize)).Intersect(viewPort);

        _customVisual.Size = new Vector2((float)sourceSize.Width, (float)sourceSize.Height);
        _customVisual.Offset = new Vector3((float)destRect.Position.X, (float)destRect.Position.Y, 0);
    }

    private void CreateVisual()
    {
        try
        {
            var compositor = ElementComposition.GetElementVisual(this)?.Compositor;
            if (compositor == null || _customVisual?.Compositor == compositor)
            {
                return;
            }

            _customVisual ??= compositor.CreateCustomVisual(new CustomVisualHandler());
            ElementComposition.SetElementChildVisual(this, _customVisual);
            _customVisual.SendHandlerMessage(CustomVisualHandler.StartMessage);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PicBox), nameof(CreateVisual), e);
            _customVisual?.SendHandlerMessage(CustomVisualHandler.StartMessage);
        }
    }

    private void DestroyVisual()
    {
        if (_customVisual == null)
        {
            return;
        }

        _customVisual.SendHandlerMessage(CustomVisualHandler.StopMessage);
        _customVisual = null;
    }

    #endregion

    #region Visual Tree and Disposal

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DestroyVisual();
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new ImageAutomationPeer(this);

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _animInstance?.Dispose();
        _stream?.Dispose();
        DestroyVisual();

        _isDisposed = true;
    }

    #endregion
}