using R3;

namespace PicView.Core.ViewModels;

public class GalleryItemViewModel : IDisposable
{
    public void Dispose()
    {
        Disposable.Dispose(
            Image,
            FileName,
            FileLocation,
            FileSize,
            FileDate,
            ImageSize);
    }
    
    // Data Properties
    public BindableReactiveProperty<object?> Image { get; } = new();
    public BindableReactiveProperty<string> FileName { get; } = new();
    public BindableReactiveProperty<string> FileLocation { get; } = new();
    public BindableReactiveProperty<string> FileSize { get; } = new();
    public BindableReactiveProperty<string> FileDate { get; } = new();
    public BindableReactiveProperty<string> ImageSize { get; } = new();
    
    public FileInfo? FileInfo { get; set; }
    public uint PixelWidth { get; set; }
    public uint PixelHeight { get; set; }
}
