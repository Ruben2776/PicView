using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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

        private bool _isScrollingEnabled = SettingsHelper.Settings.Zoom.ScrollEnabled;

        public bool IsScrollingEnabled
        {
            get => _isScrollingEnabled;
            set => this.RaiseAndSetIfChanged(ref _isScrollingEnabled, value);
        }

        #endregion Fields

        #region Services

        public FileService? FileService;

        public ImageIterator? ImageIterator;

        public ImageService? ImageService;

        #endregion Services

        #region Methods

        public void SetImageModel(ImageModel? imageModel)
        {
            Image = imageModel.Image;
            FileInfo = imageModel.FileInfo;
            Width = imageModel.PixelWidth;
            Height = imageModel.PixelHeight;
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

                    var imageModel = preLoadValue.ImageModel;
                    SetImageModel(imageModel);
                    SetTitle(imageModel, ImageIterator);
                    await ImageIterator.Preload(ImageService);
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
                    CloseMenuCommand?.Execute(null);
                    await ImageIterator.AddAsync(ImageIterator.Index, ImageService, imageModel);
                    await ImageIterator.Preload(ImageService);
                    ImageIterator.FileAdded += (_, e) => { SetTitle(); };
                    ImageIterator.FileRenamed += (_, e) => { SetTitle(); };
                    ImageIterator.FileDeleted += (_, e) =>
                    {
                        // TODO error handling if deleting current file
                        SetTitle();
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

            WindowHelper.InitializeWindowSizeAndPosition(desktop);
            UpdateLanguage();

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

            OpenWithCommand = ReactiveCommand.Create(() => { ProcessHelper.OpenWith(FileInfo?.FullName); });

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