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
            IsMotionPhoto);
    }

    // Data Properties
    public BindableReactiveProperty<object?> Image { get; } = new();
    public BindableReactiveProperty<string> FileName { get; } = new();
    public BindableReactiveProperty<string> FileLocation { get; } = new();
    public BindableReactiveProperty<string> FileSize { get; } = new();
    public BindableReactiveProperty<string> FileDate { get; } = new();

    /// <summary>Whether the file carries a motion photo video (drives the gallery badge).</summary>
    public BindableReactiveProperty<bool> IsMotionPhoto { get; } = new();

    public FileInfo? FileInfo { get; set; }
}
