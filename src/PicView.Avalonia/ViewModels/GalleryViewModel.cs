
#if DEBUG
#endif
using ReactiveUI;

namespace PicView.Avalonia.ViewModels;

public class GalleryViewModel : ViewModelBase
{
    private string? _fileLocation;
    public string? FileLocation
    {
        get => _fileLocation;
        set => this.RaiseAndSetIfChanged(ref _fileLocation, value);
    }
    private string? _fileName;

    public string? FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }
    
    private string? _fileSize;
    public string? FileSize
    {
        get => _fileSize;
        set => this.RaiseAndSetIfChanged(ref _fileSize, value);
    }
    
    private string? _fileDate;
    public string? FileDate
    {
        get => _fileDate;
        set => this.RaiseAndSetIfChanged(ref _fileDate, value);
    }
}
