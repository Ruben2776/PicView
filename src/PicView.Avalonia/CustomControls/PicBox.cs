using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Navigation;
using PicView.Core.Calculations;
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
        var viewPort = new Rect(Bounds.Size);
        var sourceSize = source.Size;
        
        var is1To1 = false; // TODO: replace with settings value

        if (is1To1)
        {
            var scale = 1; 
            var scaledSize = sourceSize * scale;
            var destRect = viewPort
                .CenterRect(new Rect(scaledSize))
                .Intersect(viewPort);
            var sourceRect = new Rect(sourceSize)
                .CenterRect(new Rect(destRect.Size / scale));

            context.DrawImage(source, sourceRect, destRect);
        }
        else
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            double desktopMinWidth = 0, desktopMinHeight = 0, containerWidth = 0, containerHeight = 0;
            var uiTopSize = SettingsHelper.Settings.UIProperties.ShowInterface ? SizeDefaults.TitlebarHeight : 0;
            var uiBottomSize = SettingsHelper.Settings.WindowProperties.Fullscreen || !SettingsHelper.Settings.UIProperties.ShowInterface
                || !SettingsHelper.Settings.UIProperties.ShowBottomNavBar 
                    ? 0 : SizeDefaults.BottombarHeight + SizeDefaults.ScrollbarSize;
            var galleryHeight = GalleryFunctions.IsBottomGalleryOpen
                ? SettingsHelper.Settings.Gallery.BottomGalleryItemSize
                : 0;
            if (Dispatcher.UIThread.CheckAccess())
            {
                desktopMinWidth = desktop.MainWindow.MinWidth;
                desktopMinHeight = desktop.MainWindow.MinHeight;
                containerWidth = desktop.MainWindow.Width;
                containerHeight = desktop.MainWindow.Height - (uiTopSize + uiBottomSize);
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    desktopMinWidth = desktop.MainWindow.MinWidth;
                    desktopMinHeight = desktop.MainWindow.MinHeight;
                    containerWidth = desktop.MainWindow.Width;
                    containerHeight = desktop.MainWindow.Height - (uiTopSize + uiBottomSize);
                }, DispatcherPriority.Normal).Wait();
            }
            var size = ImageSizeCalculationHelper.GetImageSize(
                source.Size.Width,
                source.Size.Height,
                ScreenHelper.ScreenSize.Width,
                ScreenHelper.ScreenSize.Height,
                desktopMinWidth,
                desktopMinHeight,
                ImageSizeCalculationHelper.GetInterfaceSize(),
                rotationAngle: 0,
                75,
                ScreenHelper.ScreenSize.Scaling,
                uiTopSize,
                uiBottomSize,
                galleryHeight,
                containerWidth,
                containerHeight);
            
            var destRect = viewPort
                .CenterRect(new Rect(0,0,size.Width, size.Height))
                .Intersect(viewPort);
            var sourceRect = new Rect(sourceSize)
                .CenterRect(new Rect(destRect.Size / size.AspectRatio));

            context.DrawImage(source, sourceRect, destRect);
        }
        // TODO: Implement other size modes
    }

    /// <inheritdoc/>
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