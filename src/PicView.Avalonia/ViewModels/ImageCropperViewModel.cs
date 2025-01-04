using System.Reactive;
using Avalonia;
using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.Crop;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Core.Localization;
using ReactiveUI;

namespace PicView.Avalonia.ViewModels;

public class ImageCropperViewModel : ViewModelBase
{
    public ImageCropperViewModel(Bitmap bitmap)
    {
        Bitmap = bitmap;
        InitializeCommands();
        InitializeTranslations();
    }

    private void InitializeCommands()
    {
        CropImageCommand = ReactiveCommand.CreateFromTask(SaveCroppedImageAsync);
        CloseCropCommand = ReactiveCommand.Create(HandleCloseCrop);
    }

    private void InitializeTranslations()
    {
        Crop = TranslationHelper.Translation.CropPicture;
        Close = TranslationHelper.Translation.Close;
        Width = TranslationHelper.Translation.Width;
        Height = TranslationHelper.Translation.Height;
    }
    
    public ReactiveCommand<Unit, Unit>? CropImageCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? CloseCropCommand { get; private set; }

    public Bitmap Bitmap
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int SelectionX
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int SelectionY
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public double SelectionWidth
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            PixelSelectionWidth = Convert.ToUInt32(SelectionWidth / AspectRatio);
        }
    }
    
    public uint PixelSelectionWidth
    {
        get
        {
            return Convert.ToUInt32(SelectionWidth / AspectRatio);
        }
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public double SelectionHeight
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            PixelSelectionHeight = Convert.ToUInt32(SelectionHeight / AspectRatio);
        } 
    }

    public uint PixelSelectionHeight
    {
        get
        {
            return Convert.ToUInt32(SelectionHeight / AspectRatio);
        }
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public double ImageWidth
    {
        get;
        init => this.RaiseAndSetIfChanged(ref field, value);
    }
    public double ImageHeight
    {
        get;
        init => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public double AspectRatio
    {
        get;
        init => this.RaiseAndSetIfChanged(ref field, value);
    }

    private static void HandleCloseCrop()
    {
        if (UIHelper.GetMainView.DataContext is MainViewModel vm)
        {
            CropFunctions.CloseCropControl(vm);
        }
    }

    private async Task SaveCroppedImageAsync()
    {
        if (UIHelper.GetMainView.DataContext is not MainViewModel vm) return;

        var (fileName, fileInfo, bitmap) = PrepareCropData(vm);
        
        var saveFileDialog = await FilePicker.PickFileForSavingAsync(fileName);
        if (saveFileDialog == null) return;

        await SaveImage(saveFileDialog, fileInfo, bitmap);
        
        CropFunctions.CloseCropControl(vm);

        if (vm.FileInfo.FullName == saveFileDialog)
        {
            await ErrorHandling.ReloadAsync(vm);
        }
    }

    private (string fileName, FileInfo fileInfo, Bitmap? bitmap) PrepareCropData(MainViewModel vm)
    {
        if (vm.ImageIterator?.ImagePaths is null || vm.ImageIterator.ImagePaths.Count < 0)
        {
            return CreateNewCroppedImage();
        }
        
        return (vm.FileInfo.FullName, vm.FileInfo, null);
    }

    private (string fileName, FileInfo fileInfo, Bitmap bitmap) CreateNewCroppedImage()
    {
        var fileName = $"{TranslationHelper.Translation.Crop} {new Random().Next(9999)}.png";
        var x = Convert.ToInt32(SelectionX / AspectRatio);
        var y = Convert.ToInt32(SelectionY / AspectRatio);
        var width = (int)PixelSelectionWidth;
        var height = (int)PixelSelectionHeight;
        var croppedBitmap = new CroppedBitmap(Bitmap, new PixelRect(x, y, width, height));
        var bitmap = ImageHelper.ConvertCroppedBitmapToBitmap(croppedBitmap);
        return (fileName, new FileInfo(fileName), bitmap);
    }

    private async Task SaveImage(string saveFilePath, FileInfo fileInfo, Bitmap? bitmap)
    {
        if (bitmap != null)
        {
            bitmap.Save(saveFilePath);
            return;
        }

        await SaveWithMagickImage(saveFilePath, fileInfo);
    }

    private async Task SaveWithMagickImage(string saveFilePath, FileInfo fileInfo)
    {
        using var image = new MagickImage(fileInfo.FullName);
        var x = Convert.ToInt32(SelectionX / AspectRatio);
        var y = Convert.ToInt32(SelectionY / AspectRatio);
        var geometry = new MagickGeometry(x, y, PixelSelectionWidth, PixelSelectionHeight);
        
        image.Crop(geometry);
        await image.WriteAsync(saveFilePath);
    }
}