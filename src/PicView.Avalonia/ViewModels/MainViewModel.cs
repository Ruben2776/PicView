using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Models;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Services;
using PicView.Avalonia.Views.UC;
using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using PicView.Core.ProcessHandling;
using ReactiveUI;

namespace PicView.Avalonia.ViewModels
{
    public class MainViewModel : ViewModelBase, IActivatableViewModel
    {
        public ViewModelActivator? Activator { get; }

        public event EventHandler ImageChanged;

        #region Localization

        private void UpdateLanguage()
        {
            SelectFile = TranslationHelper.GetTranslation("OpenFileDialog");
            OpenLastFile = TranslationHelper.GetTranslation("OpenLastFile");
            FilePaste = TranslationHelper.GetTranslation("FilePaste");
            Copy = TranslationHelper.GetTranslation("Copy");
            Reload = TranslationHelper.GetTranslation("Reload");
            Print = TranslationHelper.GetTranslation("Print");
            DeleteFile = TranslationHelper.GetTranslation("DeleteFile");
            Save = TranslationHelper.GetTranslation("Save");
            CopyFile = TranslationHelper.GetTranslation("CopyFile");
            NewWindow = TranslationHelper.GetTranslation("NewWindow");
            Close = TranslationHelper.GetTranslation("Close");
            Open = TranslationHelper.GetTranslation("Open");
            OpenFileDialog = TranslationHelper.GetTranslation("OpenFileDialog");
            ShowInFolder = TranslationHelper.GetTranslation("ShowInFolder");
            OpenWith = TranslationHelper.GetTranslation("OpenWith");
            RenameFile = TranslationHelper.GetTranslation("RenameFile");
            DuplicateFile = TranslationHelper.GetTranslation("DuplicateFile");
            RotateLeft = TranslationHelper.GetTranslation("RotateLeft");
            RotateRight = TranslationHelper.GetTranslation("RotateRight");
            Flip = TranslationHelper.GetTranslation("Flip");
            UnFlip = TranslationHelper.GetTranslation("UnFlip");
            ShowBottomGallery = TranslationHelper.GetTranslation("ShowBottomGallery");
            HideBottomGallery = TranslationHelper.GetTranslation("HideBottomGallery");
            AutoFitWindow = TranslationHelper.GetTranslation("AutoFitWindow");
            Stretch = TranslationHelper.GetTranslation("Stretch");
            Crop = TranslationHelper.GetTranslation("Crop");
            ResizeImage = TranslationHelper.GetTranslation("ResizeImage");
            GoToImageAtSpecifiedIndex = TranslationHelper.GetTranslation("GoToImageAtSpecifiedIndex");
            ToggleScroll = TranslationHelper.GetTranslation("ToggleScroll");
            ScrollEnabled = TranslationHelper.GetTranslation("ScrollEnabled");
            ScrollDisabled = TranslationHelper.GetTranslation("ScrollDisabled");
            Slideshow = TranslationHelper.GetTranslation("Slideshow");
            Settings = TranslationHelper.GetTranslation("Settings");
            InfoWinow = TranslationHelper.GetTranslation("InfoWindow");
            ShowAllSettingsWindow = TranslationHelper.GetTranslation("ShowAllSettingsWindow");
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

        private string? _filePaste;

        public string? FilePaste
        {
            get => _filePaste;
            set => this.RaiseAndSetIfChanged(ref _filePaste, value);
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

        private string? _open;

        public string? Open
        {
            get => _open;
            set => this.RaiseAndSetIfChanged(ref _open, value);
        }

        private string? _openFileDialog;

        public string? OpenFileDialog
        {
            get => _openFileDialog;
            set => this.RaiseAndSetIfChanged(ref _openFileDialog, value);
        }

        private string? _showInFolder;

        public string? ShowInFolder
        {
            get => _showInFolder;
            set => this.RaiseAndSetIfChanged(ref _showInFolder, value);
        }

        private string? _openWith;

        public string? OpenWith
        {
            get => _openWith;
            set => this.RaiseAndSetIfChanged(ref _openWith, value);
        }

        private string? _renameFile;

        public string? RenameFile
        {
            get => _renameFile;
            set => this.RaiseAndSetIfChanged(ref _renameFile, value);
        }

        private string? _duplicateFile;

        public string? DuplicateFile
        {
            get => _duplicateFile;
            set => this.RaiseAndSetIfChanged(ref _duplicateFile, value);
        }
        
        private string? _rotateLeft;
        public string? RotateLeft
        {
            get => _rotateLeft;
            set => this.RaiseAndSetIfChanged(ref _rotateLeft, value);
        }
        
        private string? _rotateRight;
        public string? RotateRight
        {
            get => _rotateRight;
            set => this.RaiseAndSetIfChanged(ref _rotateRight, value);
        }
        
        private string? _flip;
        public string? Flip
        {
            get => _flip;
            set => this.RaiseAndSetIfChanged(ref _flip, value);
        }
        
        private string? _unFlip;
        public string? UnFlip
        {
            get => _unFlip;
            set => this.RaiseAndSetIfChanged(ref _unFlip, value);
        }
        
        private string? _showBottomGallery;
        public string? ShowBottomGallery
        {
            get => _showBottomGallery;
            set => this.RaiseAndSetIfChanged(ref _showBottomGallery, value);
        }
        private string? _hideBottomGallery;
        public string? HideBottomGallery
        {
            get => _hideBottomGallery;
            set => this.RaiseAndSetIfChanged(ref _hideBottomGallery, value);
        }

        private string? _autoFitWindow;
        public string? AutoFitWindow
        {
            get => _autoFitWindow;
            set => this.RaiseAndSetIfChanged(ref _autoFitWindow, value);
        }
        
        private string? _stretch;
        public string? Stretch
        {
            get => _stretch;
            set => this.RaiseAndSetIfChanged(ref _stretch, value);
        }

        private string? _crop;
        public string? Crop
        {
            get => _crop;
            set => this.RaiseAndSetIfChanged(ref _crop, value);
        }

        private string? _resizeImage;
        public string? ResizeImage
        {
            get => _resizeImage;
            set => this.RaiseAndSetIfChanged(ref _resizeImage, value);
        }

        private string? _goToImageAtSpecifiedIndex;
        public string? GoToImageAtSpecifiedIndex
        {
            get => _goToImageAtSpecifiedIndex;
            set => this.RaiseAndSetIfChanged(ref _goToImageAtSpecifiedIndex, value);
        }

        private string? _toggleScroll;
        public string? ToggleScroll
        {
            get => _toggleScroll;
            set => this.RaiseAndSetIfChanged(ref _toggleScroll, value);
        }
        
        private string? _scrollEnabled;
        public string? ScrollEnabled
        {
            get => _scrollEnabled;
            set => this.RaiseAndSetIfChanged(ref _scrollEnabled, value);
        }
        
        private string? _scrollDisabled;
        public string? ScrollDisabled
        {
            get => _scrollDisabled;
            set => this.RaiseAndSetIfChanged(ref _scrollDisabled, value);
        }
        
        private string? _slideshow;
        public string? Slideshow
        {
            get => _slideshow;
            set => this.RaiseAndSetIfChanged(ref _slideshow, value);
        }

        private string? _settings;
        public string? Settings
        {
            get => _settings;
            set => this.RaiseAndSetIfChanged(ref _settings, value);
        }
        
        private string? _infoWinow;
        public string? InfoWinow
        {
            get => _infoWinow;
            set => this.RaiseAndSetIfChanged(ref _infoWinow, value);
        }
        
        private string? _showAllSettingsWindow;
        public string? ShowAllSettingsWindow
        {
            get => _showAllSettingsWindow;
            set => this.RaiseAndSetIfChanged(ref _showAllSettingsWindow, value);
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
        public ICommand? SaveFileCommand { get; private set; }
        public ICommand? OpenLastFileCommand { get; }
        public ICommand? PasteCommand { get; private set; }
        public ICommand? CopyFileCommand { get; private set; }
        public ICommand? CopyFilePathCommand { get; private set; }
        public ICommand? ReloadCommand { get; private set; }
        public ICommand? PrintCommand { get; private set; }
        public ICommand? DeleteFileCommand { get; private set; }
        public ICommand? RecycleFileCommand { get; private set; }
        public ICommand? SaveCommand { get; }
        public ICommand? CloseMenuCommand { get; }
        public ICommand? ToggleFileMenuCommand { get; }
        public ICommand? ToggleImageMenuCommand { get; }
        public ICommand? ToggleSettingsMenuCommand { get; }
        public ICommand? ToggleToolsMenuCommand { get; }

        private ICommand? _showInFolderCommand;

        public ICommand? ShowInFolderCommand
        {
            get => _showInFolderCommand;
            set => this.RaiseAndSetIfChanged(ref _showInFolderCommand, value);
        }

        public ICommand? OpenWithCommand { get; }
        public ICommand? RenameCommand { get; }
        public ICommand? NewWindowCommand { get; }
        public ICommand? DuplicateFileCommand { get; }
        
        public ICommand? RotateLeftCommand { get; }
        public ICommand? RotateRightCommand { get; }
        public ICommand? FlipCommand { get; }

        #endregion Commands

        #region Fields
        
        public string? GetFlipped => IsFlipped ? UnFlip : Flip;
        public string? GetBottomGallery => IsBottomGalleryShown ? HideBottomGallery : ShowBottomGallery;

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

        #region Image

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
        
        private double _titleMaxWidth;
        public double TitleMaxWidth
        {
            get => _titleMaxWidth;
            set => this.RaiseAndSetIfChanged(ref _titleMaxWidth, value);
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
        
        private bool _isAutoFit;
        
        public bool IsAutoFit
        {
            get => _isAutoFit;
            set
            {
                SettingsHelper.Settings.WindowProperties.AutoFit = value;
                if (value)
                {
                    SizeToContent = SizeToContent.WidthAndHeight;
                    CanResize = false;
                }
                else
                {
                    SizeToContent = SizeToContent.Manual;
                    CanResize = true;
                }
                this.RaiseAndSetIfChanged(ref _isAutoFit, value);
                _= SettingsHelper.SaveSettingsAsync();
            } 
        }
        
        private SizeToContent _sizeToContent;
        public SizeToContent SizeToContent
        {
            get => _sizeToContent;
            set => this.RaiseAndSetIfChanged(ref _sizeToContent, value);
        }
        
        private bool _canResize;
        public bool CanResize
        {
            get => _canResize;
            set => this.RaiseAndSetIfChanged(ref _canResize, value);
        }

        #endregion Image

        private bool _isBottomGalleryShown = SettingsHelper.Settings.Gallery.IsBottomGalleryShown;

        public bool IsBottomGalleryShown
        {
            get => _isBottomGalleryShown;
            set => this.RaiseAndSetIfChanged(ref _isBottomGalleryShown, value);
        }

        private bool _showBottomGalleryInHiddenUI = SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI;

        public bool ShowBottomGalleryInHiddenUI
        {
            get => _showBottomGalleryInHiddenUI;
            set => this.RaiseAndSetIfChanged(ref _showBottomGalleryInHiddenUI, value);
        }

        private bool _isFileMenuVisible;

        public bool IsFileMenuVisible
        {
            get => _isFileMenuVisible;
            set => this.RaiseAndSetIfChanged(ref _isFileMenuVisible, value);
        }

        private bool _isImageMenuVisible;

        public bool IsImageMenuVisible
        {
            get => _isImageMenuVisible;
            set => this.RaiseAndSetIfChanged(ref _isImageMenuVisible, value);
        }

        private bool _isSettingsMenuVisible;

        public bool IsSettingsMenuVisible
        {
            get => _isSettingsMenuVisible;
            set => this.RaiseAndSetIfChanged(ref _isSettingsMenuVisible, value);
        }

        private bool _isToolsMenuVisible;

        public bool IsToolsMenuVisible
        {
            get => _isToolsMenuVisible;
            set => this.RaiseAndSetIfChanged(ref _isToolsMenuVisible, value);
        }

        private ScrollBarVisibility _toggleScrollBarVisibility;

        public ScrollBarVisibility ToggleScrollBarVisibility
        {
            get => _toggleScrollBarVisibility;
            set => this.RaiseAndSetIfChanged(ref _toggleScrollBarVisibility, value);
        }

        private bool _isScrollingEnabled;

        public bool IsScrollingEnabled
        {
            get => _isScrollingEnabled;
            set
            {
                this.RaiseAndSetIfChanged(ref _isScrollingEnabled, value);
                ToggleScrollBarVisibility = value ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
                SettingsHelper.Settings.Zoom.ScrollEnabled = value;
                SetImageModel();
                _= SettingsHelper.SaveSettingsAsync();
            }
        }
        
        private bool _isStretched;
        public bool IsStretched
        {
            get => _isStretched;
            set  
            {
                this.RaiseAndSetIfChanged(ref _isStretched, value);
                SettingsHelper.Settings.ImageScaling.StretchImage = value;
                SetImageModel();
                _= SettingsHelper.SaveSettingsAsync();
            } 
        }

        private int _getIndex;
        public int GetIndex
        {
            get => _getIndex;
            set => this.RaiseAndSetIfChanged(ref _getIndex, value);
        }
        
        
        #endregion Fields

        #region Services

        public FileService? FileService;

        public ImageIterator? ImageIterator;

        public ImageService? ImageService;

        #endregion Services

        #region Methods

        public void SetImageModel()
        {
            var imageModel = new ImageModel
            {
                Image = Image,
                FileInfo = FileInfo,
                PixelWidth = (int)Width,
                PixelHeight = (int)Height,
                EXIFOrientation = EXIFOrientation,
                IsFlipped = IsFlipped,
                Rotation = Rotation
            };
            SetImageModel(imageModel);
        }

        public void SetImageModel(ImageModel imageModel)
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            var monitor = ScreenHelper.GetScreen(desktop.MainWindow);
            double desktopMinWidth = 0, desktopMinHeight = 0;
            if (Dispatcher.UIThread.CheckAccess())
            {
                desktopMinWidth = desktop.MainWindow.MinWidth;
                desktopMinHeight = desktop.MainWindow.MinHeight;
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    desktopMinWidth = desktop.MainWindow.MinWidth;
                    desktopMinHeight = desktop.MainWindow.MinHeight;
                }, DispatcherPriority.Normal).Wait();
            }
            var size = ImageSizeCalculationHelper.GetImageSize(
                imageModel.PixelWidth,
                imageModel.PixelHeight,
                monitor.Bounds.Width,
                monitor.Bounds.Height,
                desktopMinWidth,
                desktopMinHeight,
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 195 : 225,
                imageModel.Rotation,
                IsStretched,
                40,
                monitor.Scaling,
                SettingsHelper.Settings.WindowProperties.Fullscreen,
                35,
                35,
                0,
                IsAutoFit,
                SettingsHelper.Settings.WindowProperties.Width,
                SettingsHelper.Settings.WindowProperties.Height,
                IsScrollingEnabled);
            SetImageModel(imageModel, size.Width, size.Height, size.TitleMaxWidth);
        }

        public void SetImageModel(ImageModel? imageModel, double width, double height, double titleMaxWidth)
        {
            ArgumentNullException.ThrowIfNull(imageModel);
            Image = imageModel.Image;
            FileInfo = imageModel.FileInfo;
            TitleMaxWidth = titleMaxWidth;
            Width = width;
            Height = height;
            EXIFOrientation = imageModel.EXIFOrientation;
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

        public void SetTitle()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(ImageIterator);

                var titleString = TitleHelper.GetTitle((int)Width, (int)Height, ImageIterator.Index,
                    FileInfo, ZoomValue, ImageIterator.Pics);
                WindowTitle = titleString[0];
                Title = titleString[1];
                TitleTooltip = titleString[2];
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e);
#endif
            }
        }

        public void ResetTitle()
        {
            WindowTitle = TranslationHelper.GetTranslation("NoImage") + " - PicView";
            TitleTooltip = Title = TranslationHelper.GetTranslation("NoImage");
        }

        public void SetLoadingTitle()
        {
            WindowTitle = TranslationHelper.GetTranslation("Loading") + " - PicView";
            TitleTooltip = Title = TranslationHelper.GetTranslation("Loading");
        }

        public async Task SetImageNavigation(NavigateTo navigateTo)
        {
            if (ImageIterator is null)
            {
                return;
            }
            
            await Task.Run(async () =>
            {
                try
                {

                    var index = ImageIterator.GetIteration(ImageIterator.Index, navigateTo);
                    if (index < 0)
                    {
                        return;
                    }

                    ImageIterator.Index = index;
                    var x = 0;

                    var preLoadValue = ImageIterator.PreLoader.Get(index, ImageIterator.Pics);
                    if (preLoadValue is not null)
                    {
                        while (preLoadValue.IsLoading)
                        {
                            if (x == 0)
                            {
                                SetLoadingTitle();
                                using var image = new MagickImage();
                                image.Ping(ImageIterator.Pics[index]);
                                var thumb = image.GetExifProfile()?.CreateThumbnail();
                                if (thumb is not null)
                                {
                                    var stream = new MemoryStream(thumb?.ToByteArray());
                                    Image = new Bitmap(stream);
                                }
                                else
                                {
                                    Image = null;
                                }
                            }

                            x++;
                            await Task.Delay(20);
                            if (ImageIterator.Index != index)
                            {
                                await ImageIterator.Preload(ImageService);
                                return;
                            }

                            if (x <= 50)
                            {
                                continue;
                            }

                            await GetPreload();
#if DEBUG
                            Trace.WriteLine("Loading timeout");
#endif
                            break;
                        }
                    }

                    if (preLoadValue is null)
                    {
                        await GetPreload();
                    }

                    if (ImageIterator.Index != index)
                    {
                        await ImageIterator.Preload(ImageService);
                        return;
                    }

                    SetImageModel(preLoadValue.ImageModel);
                    SetTitle(preLoadValue.ImageModel, ImageIterator);
                    GetIndex = ImageIterator.Index + 1;
                    await ImageIterator.Preload(ImageService);
                    await Task.Delay(100); // Need to delay to allow UI to render
                    ImageChanged?.Invoke(this, EventArgs.Empty);
                    return;

                    async Task GetPreload()
                    {
                        await ImageIterator.PreLoader.AddAsync(index, ImageService, ImageIterator.Pics)
                            .ConfigureAwait(false);
                        preLoadValue = ImageIterator.PreLoader.Get(index, ImageIterator.Pics);
                        if (ImageIterator.Index != index)
                        {
                            await ImageIterator.Preload(ImageService);
                            return;
                        }

                        if (preLoadValue is null)
                        {
                            throw new ArgumentNullException(nameof(SetImageNavigation),
                                nameof(preLoadValue) + " is null");
                        }
                    }
                }
                catch (Exception)
                {
                    // TODO display exception to user
                }
            });
        }

        private async Task SetImageModelAsync(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            await SetImageModelAsync(new FileInfo(path)).ConfigureAwait(false);
        }

        private async Task SetImageModelAsync(FileInfo fileInfo)
        {
            await Task.Run(async () =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(fileInfo);

                    var imageModel = new ImageModel
                    {
                        FileInfo = fileInfo
                    };

                    ImageService ??= new ImageService();
                    await ImageService.LoadImageAsync(imageModel).ConfigureAwait(false);
                    SetImageModel(imageModel);

                    ImageIterator = new ImageIterator(imageModel.FileInfo);
                    ImageIterator.Index = ImageIterator.Pics.IndexOf(fileInfo.FullName);
                    SetTitle(imageModel, ImageIterator);
                    GetIndex = ImageIterator.Index + 1;
                    CloseMenuCommand?.Execute(null);
                    await ImageIterator.AddAsync(ImageIterator.Index, ImageService, imageModel);
                    await ImageIterator.Preload(ImageService);
                    ImageIterator.FileAdded += (_, e) => { SetTitle(); };
                    ImageIterator.FileRenamed += (_, e) => { SetTitle(); };
                    ImageIterator.FileDeleted += async (_, e) =>
                    {
                        if (e) //change if deleting current file
                        {
                            await SetImageModelAsync(ImageIterator.Pics[ImageIterator.Index]);
                        }
                        else
                        {
                            SetTitle();
                        }
                    };
                }
                catch (Exception)
                {
                    if (ImageIterator is null)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => { CurrentView = new StartUpMenu(); });
                    }
                }
            });
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
                CurrentView = new ImageViewer();
                Task.Run(async () => { await SetImageModelAsync(args[1]); });
            }
            else if (SettingsHelper.Settings.StartUp.OpenLastFile)
            {
                CurrentView = new ImageViewer();
                Task.Run(async () => { await SetImageModelAsync(SettingsHelper.Settings.StartUp.LastFile); });
            }
            else
            {
                CurrentView = new StartUpMenu();
                ResetTitle();
            }

            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                desktop.MainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                WindowHelper.InitializeWindowSizeAndPosition(desktop);
            }
            UpdateLanguage();
            IsScrollingEnabled = SettingsHelper.Settings.Zoom.ScrollEnabled;
            IsStretched = SettingsHelper.Settings.ImageScaling.StretchImage;
            IsAutoFit = SettingsHelper.Settings.WindowProperties.AutoFit;

            #region Window commands

            ExitCommand = ReactiveCommand.Create(desktop.MainWindow.Close);
            MinimizeCommand = ReactiveCommand.Create(() =>
                desktop.MainWindow.WindowState = WindowState.Minimized);
            MaximizeCommand = ReactiveCommand.Create(() =>
            {
                desktop.MainWindow.WindowState = desktop.MainWindow.WindowState == WindowState.Normal
                    ? WindowState.Maximized
                    : WindowState.Normal;
            });

            NewWindowCommand = ReactiveCommand.Create(ProcessHelper.StartNewProcess);

            #endregion

            #region Navigation Commands

            NextCommand = ReactiveCommand.Create(async () =>
            {
                if (ImageIterator is null)
                {
                    return;
                }

                await SetImageNavigation(NavigateTo.Next).ConfigureAwait(false);
            });

            PreviousCommand = ReactiveCommand.Create(async () =>
            {
                if (ImageIterator is null)
                {
                    return;
                }

                await SetImageNavigation(NavigateTo.Previous).ConfigureAwait(false);
            });

            FirstCommand = ReactiveCommand.Create(async () =>
            {
                if (ImageIterator is null)
                {
                    return;
                }

                await SetImageNavigation(NavigateTo.First).ConfigureAwait(false);
            });

            LastCommand = ReactiveCommand.Create(async () =>
            {
                if (ImageIterator is null)
                {
                    return;
                }

                await SetImageNavigation(NavigateTo.Last).ConfigureAwait(false);
            });

            ReloadCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (FileInfo is null || ImageIterator is null)
                {
                    return;
                }

                ImageIterator.PreLoader.Clear();
                CurrentView = new ImageViewer();
                Image = null;
                await SetImageModelAsync(FileInfo.FullName);
            });

            #endregion

            #region Menus

            CloseMenuCommand = ReactiveCommand.Create(() =>
            {
                IsFileMenuVisible = false;
                IsImageMenuVisible = false;
                IsSettingsMenuVisible = false;
                IsToolsMenuVisible = false;
            });

            ToggleFileMenuCommand = ReactiveCommand.Create(() =>
            {
                IsFileMenuVisible = !IsFileMenuVisible;
                IsImageMenuVisible = false;
                IsSettingsMenuVisible = false;
                IsToolsMenuVisible = false;
            });

            ToggleImageMenuCommand = ReactiveCommand.Create(() =>
            {
                IsFileMenuVisible = false;
                IsImageMenuVisible = !IsImageMenuVisible;
                IsSettingsMenuVisible = false;
                IsToolsMenuVisible = false;
            });

            ToggleSettingsMenuCommand = ReactiveCommand.Create(() =>
            {
                IsFileMenuVisible = false;
                IsImageMenuVisible = false;
                IsSettingsMenuVisible = !IsSettingsMenuVisible;
                IsToolsMenuVisible = false;
            });

            ToggleToolsMenuCommand = ReactiveCommand.Create(() =>
            {
                IsFileMenuVisible = false;
                IsImageMenuVisible = false;
                IsSettingsMenuVisible = false;
                IsToolsMenuVisible = !IsToolsMenuVisible;
            });

            #endregion Menus
            
            #region Image commands
            
            RotateLeftCommand = ReactiveCommand.Create(() =>
            {
            });
            
            RotateRightCommand = ReactiveCommand.Create(() =>
            {
            });
            
            FlipCommand = ReactiveCommand.Create(() =>
            {
                IsFlipped = !IsFlipped;
            });
            
            #endregion Image commands

            #region File commands

            OpenFileCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                FileService ??= new FileService();
                var file = await FileService.OpenFile();
                if (file is null)
                {
                    return;
                }

                CurrentView = new ImageViewer();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var path = file.Path.AbsolutePath;
                    await SetImageModelAsync(new FileInfo(path));
                }
                else
                {
                    await SetImageModelAsync(new FileInfo(file.Path.LocalPath));
                }
            });

            SaveFileCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                FileService ??= new FileService();
                await FileService.SaveFileAsync(FileInfo?.FullName);
            });

            CopyFileCommand = ReactiveCommand.Create(async () =>
            {
                var clipboard = desktop.MainWindow.Clipboard;
                var dataObject = new DataObject();
                dataObject.Set(DataFormats.Files, new[] { FileInfo?.FullName });
                await clipboard.SetDataObjectAsync(dataObject);
            });

            CopyFilePathCommand = ReactiveCommand.Create(async () =>
            {
                await desktop.MainWindow.Clipboard.SetTextAsync(FileInfo?.FullName);
            });

            PasteCommand = ReactiveCommand.Create(() => { });

            OpenWithCommand = ReactiveCommand.Create(() =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // TODO: implement open with on mac
                }
                else
                {
                    ProcessHelper.OpenWith(FileInfo?.FullName);
                }
            });

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ShowInFolderCommand = ReactiveCommand.Create(() =>
                {
                    Process.Start("open", $"-R \"{FileInfo?.FullName}\""); 
                });
            }

            RenameCommand = ReactiveCommand.Create(() => { });

            DuplicateFileCommand = ReactiveCommand.Create(() =>
            {
                FileHelper.DuplicateAndReturnFileName(FileInfo?.FullName);
            });

            PrintCommand = ReactiveCommand.Create(() => { ProcessHelper.Print(FileInfo?.FullName); });

            DeleteFileCommand = ReactiveCommand.Create(() =>
            {
                FileDeletionHelper.DeleteFileWithErrorMsg(FileInfo?.FullName, false);
            });

            RecycleFileCommand = ReactiveCommand.Create(() =>
            {
                FileDeletionHelper.DeleteFileWithErrorMsg(FileInfo?.FullName, true);
            });

            #endregion

            Activator = new ViewModelActivator();
            this.WhenActivated(disposables =>
            {
                /* handle activation */
                Disposable
                    .Create(() =>
                    {
                        /* handle deactivation */
                    })
                    .DisposeWith(disposables);
            });
        }
    }
}