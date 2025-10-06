using ImageMagick;
using PicView.Core.Exif;
using PicView.Core.ImageDecoding;
using PicView.Core.ImageEffects;
using R3;

namespace PicView.Core.ViewModels;

public class PicViewerModel : IDisposable
{
    public void Dispose()
    {
        Disposable.Dispose(FileInfo, PixelWidth, PixelHeight, ImageSource, SecondaryImageSource, ImageType);
        Disposable.Dispose(ImageType, ImageWidth, ImageHeight, SecondaryImageWidth, IsShowingSideBySide, ScrollViewerHeight);
        Disposable.Dispose(ScrollViewerHeight, AspectRatio, EffectConfig, ExifOrientation, ScaleX, Title);
        Disposable.Dispose(TitleTooltip, WindowTitle, Index);
    }
    
    public BindableReactiveProperty<FileInfo?> FileInfo { get; } = new();

    /// <summary>
    /// The image's pixel width
    /// </summary>
    public BindableReactiveProperty<int> PixelWidth { get; } = new(0);

    /// <summary>
    /// The image's pixel height
    /// </summary>
    public BindableReactiveProperty<int> PixelHeight { get; } = new(0);
    
    public BindableReactiveProperty<object?> ImageSource  { get; } = new();

    public BindableReactiveProperty<object?> SecondaryImageSource { get; } = new();

    public BindableReactiveProperty<ImageType> ImageType { get; } = new();
    
    public BindableReactiveProperty<MagickFormat?> Format { get; } = new();

    public BindableReactiveProperty<double> RotationAngle { get; } = new(0);

    public BindableReactiveProperty<double> ZoomValue { get; } = new();

    /// <summary>
    /// The width to scale the image to
    /// </summary>
    public BindableReactiveProperty<double> ImageWidth { get; } = new(0);

    /// <summary>
    /// The height to scale the image to
    /// </summary>
    public BindableReactiveProperty<double> ImageHeight { get; } = new(0);

    public BindableReactiveProperty<double> SecondaryImageWidth { get; } = new(0);

    public BindableReactiveProperty<bool> IsShowingSideBySide { get; } = new();

    public BindableReactiveProperty<double> ScrollViewerWidth { get; } = new(0);

    public BindableReactiveProperty<double> ScrollViewerHeight { get; } = new(0);
    
    public BindableReactiveProperty<double> GalleryWidth { get; } = new(0);

    public BindableReactiveProperty<double> AspectRatio { get; } = new();

    public BindableReactiveProperty<ImageEffectConfig?> EffectConfig { get; } = new();

    public BindableReactiveProperty<ExifOrientation?> ExifOrientation { get; } = new();

    // Used to flip the flip button
    public BindableReactiveProperty<int> ScaleX { get; } = new(1);

    public BindableReactiveProperty<string>? Title { get; } = new();

    public BindableReactiveProperty<string>? TitleTooltip { get; } = new();

    public BindableReactiveProperty<string>? WindowTitle { get; } = new();
    
    public BindableReactiveProperty<int> Index { get; } = new();
    public BindableReactiveProperty<int> Maximum { get; } = new();
    public BindableReactiveProperty<int> GetIndex { get; } = new();
    
    public BindableReactiveProperty<bool> IsSingleImage { get; } = new();
    
    public BindableReactiveProperty<bool> ShouldCropBeEnabled { get; } = new();
    
    public BindableReactiveProperty<bool> ShouldOptimizeImageBeEnabled { get; } = new();
}
