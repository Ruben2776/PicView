using System.Numerics;
using Avalonia;
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
using PicView.Core.Animation;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;
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
    public static readonly StyledProperty<ImageType?> ImageTypeProperty =
        AvaloniaProperty.Register<PicBox, ImageType?>(nameof(ImageType));
    
    /// <summary>
    ///     Defines the <see cref="Playback" /> property.
    /// </summary>
    public static readonly StyledProperty<AnimationPlayback?> PlaybackProperty =
        AvaloniaProperty.Register<PicBox, AnimationPlayback?>(nameof(Playback));

    private CompositionCustomVisual? _customVisual;
    private bool _isAttached;
    private int _openTicket;

    /// <summary>
    ///     Gets or sets the image type.
    ///     Determines if <see cref="Source" /> is an animated image, scalable vector graphics (SVG) or raster image.
    /// </summary>
    public ImageType ImageType
    {
        get => GetValue(ImageTypeProperty) ?? ImageType.Invalid;
        set => SetValue(ImageTypeProperty, value);
    }

    /// <summary>
    ///     Gets or sets the playback of the animated image shown, or null to show <see cref="Source" /> still.
    /// </summary>
    public AnimationPlayback? Playback
    {
        get => GetValue(PlaybackProperty);
        set => SetValue(PlaybackProperty, value);
    }

    /// <summary>Opens the player of a playback; runs off the UI thread. Tests replace it.</summary>
    internal Func<AnimationPlayback, AnimationPlayer> OpenPlayer { get; set; } = AnimationPlayer.Open;

    /// <summary>The player handed to the compositor handler; null while none is running.</summary>
    internal AnimationPlayer? Player { get; private set; }

    /// <summary>Completes when the latest open has been handed over, disposed or has failed.</summary>
    internal Task Opening { get; private set; } = Task.CompletedTask;

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PlaybackProperty)
        {
            ClosePlayer();
            Opening = OpenPlayerAsync(Playback);
        }
        else if (change.Property == BoundsProperty)
        {
            ResizeVisual();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttached = true;
        Opening = OpenPlayerAsync(Playback);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _isAttached = false;
        ClosePlayer();
        DestroyVisual();
    }

    /// <summary>
    ///     Opens the playback's player off the UI thread and hands it to the compositor handler, which owns it from then
    ///     on. A player whose open was overtaken by a newer one, or by leaving the tree, was never handed over and is
    ///     disposed here.
    /// </summary>
    private async Task OpenPlayerAsync(AnimationPlayback? playback)
    {
        if (playback is null || !_isAttached)
        {
            return;
        }

        var ticket = ++_openTicket;
        var open = OpenPlayer;
        AnimationPlayer player;
        try
        {
            // Continues on the UI thread, where the control and its visual are touched
            player = await Task.Run(() => open(playback)).ConfigureAwait(true);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PicBox), nameof(OpenPlayerAsync), e);
            return;
        }

        if (ticket != _openTicket || !_isAttached)
        {
            player.Dispose();
            return;
        }

        var visual = CreateVisual();
        if (visual is null)
        {
            player.Dispose();
            return;
        }

        visual.SendHandlerMessage(player);
        Player = player;
        InvalidateVisual();
    }

    /// <summary>Ends the running player, if any, and makes an open still running stale.</summary>
    private void ClosePlayer()
    {
        _openTicket++;
        if (Player is null)
        {
            return;
        }

        _customVisual?.SendHandlerMessage(CustomVisualHandler.CloseMessage);
        Player = null;
        InvalidateVisual();
    }

    private CompositionCustomVisual? CreateVisual()
    {
        if (_customVisual is not null)
        {
            return _customVisual;
        }

        try
        {
            var compositor = ElementComposition.GetElementVisual(this)?.Compositor;
            if (compositor is null)
            {
                return null;
            }

            _customVisual = compositor.CreateCustomVisual(new CustomVisualHandler());
            ElementComposition.SetElementChildVisual(this, _customVisual);
            _customVisual.SendHandlerMessage(CustomVisualHandler.StartMessage);
            ResizeVisual();
        }
        catch (Exception e)
        {
            // A half-built visual would take the player and never draw it, so there is no visual and the still stays
            _customVisual = null;
            DebugHelper.LogDebug(nameof(PicBox), nameof(CreateVisual), e);
        }

        return _customVisual;
    }

    private void ResizeVisual()
    {
        if (_customVisual is null)
        {
            return;
        }

        var size = Bounds.Size;
        _customVisual.Size = new Vector2((float)size.Width, (float)size.Height);
    }

    private void DestroyVisual()
    {
        if (_customVisual is null)
        {
            return;
        }

        _customVisual.SendHandlerMessage(CustomVisualHandler.StopMessage);
        ElementComposition.SetElementChildVisual(this, null);
        _customVisual = null;
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
        // A running animation draws over the whole control; the still image under it would show through transparency
        if (Player is not null)
        {
            return;
        }

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
            }
        }
        catch (ObjectDisposedException e)
        {
            if (Application.Current.DataContext is CoreViewModel core)
            {
                core.SharedCache.ReleaseAll();
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
        if (DataContext is not TabViewModel tab)
        {
            return null;
        }

        return tab.ParentWindowContext.WindowTabs.GetSourceFromFile(tab.Model?.FileInfo) as Bitmap;
    }

    private Size GetSizeFromAlternativeSources()
    {
        if (DataContext is not TabViewModel tab)
        {
            return new Size();
        }

        var tabs = tab.ParentWindowContext.WindowTabs;
        var model = tab.Model;
        
        if (model.FileInfo?.Exists != true)
        {
            return new Size();
        }

        if (tabs.SharedCache?.TryGetReady(model.FileInfo, out var ready) ?? false)
        {
            return new Size(ready.PixelWidth, ready.PixelHeight);
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