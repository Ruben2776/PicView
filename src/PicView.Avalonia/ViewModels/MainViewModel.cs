using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Models;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using ReactiveUI;
using System.Windows.Input;
using PicView.Avalonia.Services;

namespace PicView.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region Localization

    private string? _currentLanguageKey;

    public string CurrentLanguageValue
    {
        get => SettingsHelper.Settings.UIProperties.UserLanguage;
    }

    public string? CurrentLanguageKey
    {
        get => _currentLanguageKey ?? "en";
        set
        {
            if (_currentLanguageKey == value)
            {
                return;
            }

            _currentLanguageKey = value;
            this.RaisePropertyChanged(nameof(CurrentLanguageValue));
            this.RaisePropertyChanged(nameof(CurrentLanguageValue));
            this.RaisePropertyChanged(nameof(SelectFile));
            this.RaisePropertyChanged(nameof(OpenLastFile));
            this.RaisePropertyChanged(nameof(Paste));
        }
    }

    public string SelectFile => TranslationHelper.GetTranslation("OpenFileDialog");

    public string OpenLastFile => TranslationHelper.GetTranslation("OpenLastFile");
    public string Paste => TranslationHelper.GetTranslation("FilePaste");

    #endregion Localization

    #region Commands

    public ICommand? ExitCommand { get; }
    public ICommand? MinimizeCommand { get; }
    public ICommand? MaximizeCommand { get; }
    public ICommand? NextCommand { get; private set; }
    public ICommand? PreviousCommand { get; private set; }
    public ICommand? FirstCommand { get; private set; }
    public ICommand? LastCommand { get; private set; }

    #endregion Commands

    #region Fields

    private string _title = "Loading...";

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    private string _titleTooltip = "Loading...";

    public string TitleTooltip
    {
        get => _titleTooltip;
        set => this.RaiseAndSetIfChanged(ref _titleTooltip, value);
    }

    private string _windowTitle = "PicView";

    public string WindowTitle
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

        WindowHelper.InitializeWindowSizeAndPosition(desktop);

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
        }

        IsFileMenuVisible = true;

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
    }
}