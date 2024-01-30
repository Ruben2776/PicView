using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Models;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Services;
using PicView.Avalonia.Views.UC;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using ReactiveUI;
using System.Windows.Input;

namespace PicView.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region Localization

    private void UpdateLanguage()
    {
        SelectFile = TranslationHelper.GetTranslation("OpenFileDialog");
        OpenLastFile = TranslationHelper.GetTranslation("OpenLastFile");
        Paste = TranslationHelper.GetTranslation("FilePaste");
        Copy = TranslationHelper.GetTranslation("Copy");
        Reload = TranslationHelper.GetTranslation("Reload");
        Print = TranslationHelper.GetTranslation("Print");
        DeleteFile = TranslationHelper.GetTranslation("DeleteFile");
        Save = TranslationHelper.GetTranslation("Save");
        CopyFile = TranslationHelper.GetTranslation("CopyFile");
        NewWindow = TranslationHelper.GetTranslation("NewWindow");
        Close = TranslationHelper.GetTranslation("Close");
    }

    private string? _selectFile;

    public string? SelectFile
    {
        get => _selectFile;
        set => this.RaiseAndSetIfChanged(ref _selectFile, value);
    }

    private string? _openLastFile;

    public string? OpenLastFile
    {
        get => _openLastFile;
        set => this.RaiseAndSetIfChanged(ref _openLastFile, value);
    }

    private string? _paste;

    public string? Paste
    {
        get => _paste;
        set => this.RaiseAndSetIfChanged(ref _paste, value);
    }

    private string? _copy;

    public string? Copy
    {
        get => _copy;
        set => this.RaiseAndSetIfChanged(ref _copy, value);
    }

    private string? _reload;

    public string? Reload
    {
        get => _reload;
        set => this.RaiseAndSetIfChanged(ref _reload, value);
    }

    private string? _print;

    public string? Print
    {
        get => _print;
        set => this.RaiseAndSetIfChanged(ref _print, value);
    }

    private string? _deleteFile;

    public string? DeleteFile
    {
        get => _deleteFile;
        set => this.RaiseAndSetIfChanged(ref _deleteFile, value);
    }

    private string? _save;

    public string? Save
    {
        get => _save;
        set => this.RaiseAndSetIfChanged(ref _save, value);
    }

    private string? _copyFile;

    public string? CopyFile
    {
        get => _copyFile;
        set => this.RaiseAndSetIfChanged(ref _copyFile, value);
    }

    private string? _newWindow;

    public string? NewWindow
    {
        get => _newWindow;
        set => this.RaiseAndSetIfChanged(ref _newWindow, value);
    }

    private string? _close;

    public string? Close
    {
        get => _close;
        set => this.RaiseAndSetIfChanged(ref _close, value);
    }

    #endregion Localization

    #region Commands

    public ICommand? ExitCommand { get; }
    public ICommand? MinimizeCommand { get; }
    public ICommand? MaximizeCommand { get; }
    public ICommand? NextCommand { get; private set; }
    public ICommand? PreviousCommand { get; private set; }
    public ICommand? FirstCommand { get; private set; }
    public ICommand? LastCommand { get; private set; }
    public ICommand? OpenFileCommand { get; private set; }
    public ICommand? OpenLastFileCommand { get; private set; }
    public ICommand? PasteCommand { get; private set; }
    public ICommand? CopyCommand { get; private set; }
    public ICommand? ReloadCommand { get; private set; }
    public ICommand? PrintCommand { get; private set; }
    public ICommand? DeleteFileCommand { get; private set; }
    public ICommand? SaveCommand { get; private set; }
    public ICommand? CloseMenuCommand { get; }
    public ICommand? ToggleFileMenuCommand { get; }

    #endregion Commands

    #region Fields

    private string? _title = "Loading...";

    public string? Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    private string? _titleTooltip = "Loading...";

    public string? TitleTooltip
    {
        get => _titleTooltip;
        set => this.RaiseAndSetIfChanged(ref _titleTooltip, value);
    }

    private string? _windowTitle = "PicView";

    public string? WindowTitle
    {
        get => _windowTitle;
        set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
    }

    private UserControl? _currentView;

    public UserControl? CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    private IImage? _image;

    public IImage? Image
    {
        get => _image;
        set => this.RaiseAndSetIfChanged(ref _image, value);
    }

    private FileInfo? _fileInfo;

    public FileInfo? FileInfo
    {
        get => _fileInfo;
        set => this.RaiseAndSetIfChanged(ref _fileInfo, value);
    }

    private bool _isAnimated;

    public bool IsAnimated
    {
        get => _isAnimated;
        set => this.RaiseAndSetIfChanged(ref _isAnimated, value);
    }

    private bool _isFlipped;

    public bool IsFlipped
    {
        get => _isFlipped;
        set => this.RaiseAndSetIfChanged(ref _isFlipped, value);
    }

    private int _rotation;

    public int Rotation
    {
        get => _rotation;
        set => this.RaiseAndSetIfChanged(ref _rotation, value);
    }

    private double _width;

    public double Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    private double _height;

    public double Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    private EXIFHelper.EXIFOrientation? _exifOrientation;

    public EXIFHelper.EXIFOrientation? EXIFOrientation
    {
        get => _exifOrientation;
        set => this.RaiseAndSetIfChanged(ref _exifOrientation, value);
    }

    private double _zoomValue;

    public double ZoomValue
    {
        get => _zoomValue;
        set => this.RaiseAndSetIfChanged(ref _zoomValue, value);
    }

    public ImageIterator? ImageIterator;

    public ImageService? ImageService;

    private bool _isFileMenuVisible;

    public bool IsFileMenuVisible
    {
        get => _isFileMenuVisible;
        set => this.RaiseAndSetIfChanged(ref _isFileMenuVisible, value);
    }

    #endregion Fields

    #region Methods

    public void SetImageModel(ImageModel? imageModel)
    {
        ArgumentNullException.ThrowIfNull(imageModel);

        Image = imageModel.Image;
        FileInfo = imageModel.FileInfo;
        Width = imageModel.PixelWidth;
        Height = imageModel.PixelHeight;
        EXIFOrientation = imageModel.EXIFOrientation;
        IsAnimated = imageModel.IsAnimated;
        IsFlipped = imageModel.IsFlipped;
        Rotation = imageModel.Rotation;
        ZoomValue = 1;
    }

    public void SetTitle(ImageModel? imageModel, ImageIterator imageIterator)
    {
        ArgumentNullException.ThrowIfNull(imageModel);
        ArgumentNullException.ThrowIfNull(imageIterator);

        var titleString = TitleHelper.GetTitle(imageModel.PixelWidth, imageModel.PixelHeight, imageIterator.Index,
            imageModel.FileInfo, ZoomValue, imageIterator.Pics);
        WindowTitle = titleString[0];
        Title = titleString[1];
        TitleTooltip = titleString[2];
    }

    public void ResetTitle()
    {
        WindowTitle = TranslationHelper.GetTranslation("NoImage") + " - PicView";
        TitleTooltip = Title = TranslationHelper.GetTranslation("NoImage");
    }

    private async Task SetImageModelAsync(NavigateTo navigateTo)
    {
        ArgumentNullException.ThrowIfNull(ImageIterator);

        var imageModel = await ImageIterator.GetImageModelAsync(ImageIterator.Index, navigateTo).ConfigureAwait(false);
        SetImageModel(imageModel);
        SetTitle(imageModel, ImageIterator);
        await ImageIterator.Preload();
    }

    private async Task SetImageModelAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(path);
        }

        var imageModel = new ImageModel
        {
            FileInfo = new FileInfo(path)
        };
        ImageService = new ImageService();
        await ImageModel.LoadImageAsync(imageModel).ConfigureAwait(false);
        SetImageModel(imageModel);
        ImageIterator = new ImageIterator(imageModel.FileInfo);
        ImageIterator.Index = ImageIterator.Pics.IndexOf(path);
        SetTitle(imageModel, ImageIterator);
        await ImageIterator.AddAsync(ImageIterator.Index, imageModel);
        await ImageIterator.Preload();
    }

    #endregion Methods

    public MainViewModel()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            // If a valid file path is provided, display the image viewer
            CurrentView = new ImageViewer();
            Task.Run(async () =>
            {
                await SetImageModelAsync(args[1]);
            });
        }
        else
        {
            // Otherwise, display the menu
            CurrentView = new StartUpMenu();
            ResetTitle();
        }

        WindowHelper.InitializeWindowSizeAndPosition(desktop);
        UpdateLanguage();

        ExitCommand = ReactiveCommand.Create(desktop.MainWindow.Close);
        MinimizeCommand = ReactiveCommand.Create(() =>
            desktop.MainWindow.WindowState = WindowState.Minimized);
        MaximizeCommand = ReactiveCommand.Create(() =>
        {
            desktop.MainWindow.WindowState = desktop.MainWindow.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        });

        NextCommand = ReactiveCommand.Create(async () =>
        {
            if (ImageIterator is null)
            {
                return;
            }
            await SetImageModelAsync(NavigateTo.Next).ConfigureAwait(false);
        });

        PreviousCommand = ReactiveCommand.Create(async () =>
        {
            if (ImageIterator is null)
            {
                return;
            }
            await SetImageModelAsync(NavigateTo.Previous).ConfigureAwait(false);
        });

        FirstCommand = ReactiveCommand.Create(async () =>
        {
            if (ImageIterator is null)
            {
                return;
            }
            await SetImageModelAsync(NavigateTo.First).ConfigureAwait(false);
        });

        LastCommand = ReactiveCommand.Create(async () =>
        {
            if (ImageIterator is null)
            {
                return;
            }
            await SetImageModelAsync(NavigateTo.Last).ConfigureAwait(false);
        });

        CloseMenuCommand = ReactiveCommand.Create(() =>
        {
            IsFileMenuVisible = false;
        });

        ToggleFileMenuCommand = ReactiveCommand.Create(() =>
        {
            IsFileMenuVisible = !IsFileMenuVisible;
        });
    }
}