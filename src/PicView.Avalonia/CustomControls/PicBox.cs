using System.Numerics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition;
using ImageMagick;
using PicView.Avalonia.AnimatedImage;
using PicView.Avalonia.ImageHandling;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// Displays a <see cref="Bitmap"/> image.
/// </summary>
public class PicBox : Control
{
    #region Constants and Fields
    /// <summary>
    /// Defines the <see cref="Source"/> property.
    /// </summary>
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<PicBox, IImage?>(nameof(Source));

    /// <summary>
    /// Defines the <see cref="BlendMode"/> property.
    /// </summary>
    public static readonly StyledProperty<BitmapBlendingMode> BlendModeProperty =
        AvaloniaProperty.Register<PicBox, BitmapBlendingMode>(nameof(BlendMode));

    /// <summary>
    /// Defines the <see cref="Stretch"/> property.
    /// </summary>
    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<PicBox, Stretch>(nameof(Stretch), Stretch.Uniform);

    /// <summary>
    /// Defines the <see cref="StretchDirection"/> property.
    /// </summary>
    public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
        AvaloniaProperty.Register<PicBox, StretchDirection>(
            nameof(StretchDirection),
            StretchDirection.Both);
    
    /// <summary>
    ///     Defines the <see cref="ImageType" /> property.
    /// </summary>
    public static readonly AvaloniaProperty<ImageType> ImageTypeProperty =
        AvaloniaProperty.Register<PicBox, ImageType>(nameof(ImageType));
    
    public static readonly StyledProperty<FileInfo?> CurrentFileInfoProperty =
        AvaloniaProperty.Register<PicBox, FileInfo?>(nameof(CurrentFileInfo));
    
    private CompositionCustomVisual? _customVisual;
    private FileStream? _stream;
    private IGifInstance? _animInstance;

    /// <summary>
    ///     Gets or sets the image type.
    ///     Determines if <see cref="Source" /> is an animated image, scalable vector graphics (SVG) or raster image.
    /// </summary>
    public ImageType ImageType
    {
        get => (ImageType)(GetValue(ImageTypeProperty) ?? false);
        set => SetValue(ImageTypeProperty, value);
    }

    public FileInfo? CurrentFileInfo
    {
        get => GetValue(CurrentFileInfoProperty);
        set => SetValue(CurrentFileInfoProperty, value);
    }
    
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
    /// Gets or sets the blend mode for the image.
    /// </summary>
    public BitmapBlendingMode BlendMode
    {
        get => GetValue(BlendModeProperty);
        set => SetValue(BlendModeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value controlling how the image will be stretched.
    /// </summary>
    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>
    /// Gets or sets a value controlling in what direction the image will be stretched.
    /// </summary>
    public StretchDirection StretchDirection
    {
        get => GetValue(StretchDirectionProperty);
        set => SetValue(StretchDirectionProperty, value);
    }

    /// <inheritdoc />
    protected override bool BypassFlowDirectionPolicies => true;
    
    #endregion Constants and Fields
    
    static PicBox()
    {
        AffectsRender<PicBox>(SourceProperty, StretchProperty, StretchDirectionProperty, BlendModeProperty);
        AffectsMeasure<PicBox>(SourceProperty, StretchProperty, StretchDirectionProperty);
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<PicBox>(AutomationControlType.Image);
    }

    #region Animation
    
    private void UpdateAnimatedSource()
    {
        CreateVisual();
        Source = Source as Bitmap;
    }
    
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

        _customVisual.Size = new Vector2((float)sourceSize.Width, (float)sourceSize.Height);
        _customVisual.Offset = new Vector3(0, 0, 0);
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

    private void CleanupAnimatedResources()
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
    /// Renders the control.
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
            default:
                HandleInvalidSource();
                break;
        }
    }

    private void RenderImageSource(DrawingContext context, IImage source)
    {
        var viewPort = new Rect(Bounds.Size);
        var sourceSize = GetImageSize(source);

        var scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
        var scaledSize = sourceSize * scale;
        var destRect = viewPort
            .CenterRect(new Rect(scaledSize))
            .Intersect(viewPort);
        var sourceRect = new Rect(sourceSize)
            .CenterRect(new Rect(destRect.Size / scale));

        var options = new RenderOptions
        {
            BitmapBlendingMode = BlendMode,
            BitmapInterpolationMode = Settings.ImageScaling.IsScalingSetToNearestNeighbor
                ? BitmapInterpolationMode.None
                : BitmapInterpolationMode.HighQuality
        };
        try
        {
            using (context.PushRenderOptions(options))
            {
                context.DrawImage(source, sourceRect, destRect);
                RenderAnimatedImageIfRequired(context);
            }
        }
        catch (ObjectDisposedException e)
        {
            if (Application.Current.DataContext is CoreViewModel core)
            {
                core.SharedCache.Clear();
            }
            if (DataContext is TabViewModel tab)
            {
                try
                {
                    if (ImageType is not ImageType.Bitmap)
                    {
                        return;
                    }
                    var model = GetImageModel.GetImageModelAsync(tab.FileInfo?.CurrentValue).AsTask().Result;
                    var bitmap = model?.Image;
                    if (bitmap is not null)
                    {
                        try
                        {
                            using (context.PushRenderOptions(options))
                            {
                                context.DrawImage(bitmap as IImage, sourceRect, destRect);
                            }

                        }
                        catch (Exception exception)
                        {
                            DebugHelper.LogDebug(nameof(PicBox), nameof(RenderImageSource), exception);
                        }
                    }
                }
                catch (Exception exception)
                {
                    DebugHelper.LogDebug(nameof(PicBox), nameof(RenderImageSource), exception);
                }
            }
            DebugHelper.LogDebug(nameof(PicBox), nameof(RenderImageSource), e);
        }
        catch (Exception e)
        {
            if (ImageType is ImageType.Bitmap)
            {
                var bitmap = GetBitmapFromAlternativeSources();
                if (bitmap is not null)
                {
                    try
                    {
                        using (context.PushRenderOptions(options))
                        {
                            context.DrawImage(source, sourceRect, destRect);
                        }

                    }
                    catch (Exception exception)
                    {
                        DebugHelper.LogDebug(nameof(PicBox), nameof(GetBitmapFromAlternativeSources), exception);
                    }
                }
            }
            
            DebugHelper.LogDebug(nameof(PicBox), nameof(Render), e);
        }
    }
    
    private void RenderAnimatedImageIfRequired(DrawingContext context)
    {
        if (ImageType is not (ImageType.AnimatedGif or ImageType.AnimatedWebp) || CurrentFileInfo is null)
        {
            CleanupAnimatedResources();
            return;
        }
        
        context.Dispose(); // Fixes transparent images
        _stream = new FileStream(CurrentFileInfo.FullName, FileMode.Open, FileAccess.Read);
        UpdateAnimationInstance(_stream);
    }

    private void HandleInvalidSource()
    {
        // TODO
    }
    
    #endregion Rendering

    #region Sizing

    /// <summary>
    /// Measures the control.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size of the control.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        return Source == null ? new Size() : Stretch.CalculateSize(availableSize, GetImageSize(Source as IImage), StretchDirection);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        return Source is null ? new Size() : Stretch.CalculateSize(finalSize, GetImageSize(Source as IImage));
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

    private Bitmap? GetBitmapFromAlternativeSources()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return null;
        }

        return core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.GetCurrentSource() as Bitmap;
    }

    private Size GetSizeFromAlternativeSources()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return new Size();
        }

        var tabs = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs;
        var model = tabs.ActiveTab.CurrentValue.Model;
        
        if (model.FileInfo?.Exists != true)
        {
            return new Size();
        }

        if (tabs.SharedCache is SharedImageCache sharedCache)
        {
            if (sharedCache.TryGet(model.FileInfo, out var preloadValue))
            {
                if (preloadValue?.ImageModel != null)
                {
                    return new Size(preloadValue.ImageModel.PixelWidth, preloadValue.ImageModel.PixelHeight);
                }
            }
        }

        try
        {
            using var magickImage = new MagickImage();
            magickImage.Ping(model.FileInfo);
            return new Size(magickImage.Width, magickImage.Height);
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(PicBox), nameof(GetSizeFromAlternativeSources), exception);
        }

        return new Size();
    }
    
    #endregion Sizing
}