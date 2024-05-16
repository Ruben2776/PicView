
#if DEBUG
using System.Diagnostics;
#endif
using Avalonia.Media.Imaging;
using PicView.Core.Localization;
using ReactiveUI;

namespace PicView.Avalonia.ViewModels;

public class GalleryViewModel : ViewModelBase
{
    public GalleryViewModel(double galleryItemSize)
    {
        GalleryItemSize = galleryItemSize;

        try
        {
            Print = TranslationHelper.GetTranslation("Print");
            OpenWith = TranslationHelper.GetTranslation("OpenWith");
            ShowInFolder = TranslationHelper.GetTranslation("ShowInFolder");
            SetAsWallpaper = TranslationHelper.GetTranslation("SetAsWallpaper");
            SetAsLockScreenImage = TranslationHelper.GetTranslation("SetAsLockScreenImage");
            CopyFile = TranslationHelper.GetTranslation("CopyFile");
            CopyImage = TranslationHelper.GetTranslation("CopyImage");
            Copy = TranslationHelper.GetTranslation("Copy");
            FileCut = TranslationHelper.GetTranslation("FileCut");
            DeleteFile = TranslationHelper.GetTranslation("DeleteFile");
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine(e.ToString());
#endif
        }
    }
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
    
    private Bitmap? _imageSource;
    public Bitmap? ImageSource
    {
        get => _imageSource;
        set => this.RaiseAndSetIfChanged(ref _imageSource, value);
    }
    
    private double _galleryItemSize;

    public double GalleryItemSize
    {
        get => _galleryItemSize;
        set => this.RaiseAndSetIfChanged(ref _galleryItemSize, value);
    }
}
