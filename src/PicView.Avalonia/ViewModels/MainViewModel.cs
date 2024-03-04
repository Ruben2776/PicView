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
using PicView.Core.ImageTransformations;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using PicView.Core.ProcessHandling;
using ReactiveUI;
using System.Diagnostics;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Windows.Input;
using ImageViewer = PicView.Avalonia.Views.ImageViewer;

namespace PicView.Avalonia.ViewModels
{
    public class MainViewModel : ViewModelBase, IActivatableViewModel
    {
        public ViewModelActivator? Activator { get; }

        public event EventHandler<ImageModel>? ImageChanged;

        private readonly IPlatformSpecificService? _platformService;

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
        public ICommand? CopyImageCommand { get; private set; }
        public ICommand? CutCommand { get; private set; }
        public ICommand? ReloadCommand { get; private set; }
        public ICommand? PrintCommand { get; private set; }
        public ICommand? DeleteFileCommand { get; private set; }
        public ICommand? RecycleFileCommand { get; private set; }
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
        public ICommand? ChangeAutoFitCommand { get; }

        public ICommand? ChangeTopMostCommand { get; }

        public ICommand? ChangeIncludeSubdirectoriesCommand { get; }

        public ICommand? ToggleUICommand { get; }

        private ICommand? _showExifWindowCommand;

        public ICommand? ShowExifWindowCommand
        {
            get => _showExifWindowCommand;
            set => this.RaiseAndSetIfChanged(ref _showExifWindowCommand, value);
        }

        private ICommand? _showSettingsWindowCommand;

        public ICommand? ShowSettingsWindowCommand
        {
            get => _showSettingsWindowCommand;
            set => this.RaiseAndSetIfChanged(ref _showSettingsWindowCommand, value);
        }

        public ICommand? SetExifRating1Command { get; }
        public ICommand? SetExifRating2Command { get; }
        public ICommand? SetExifRating3Command { get; }
        public ICommand? SetExifRating4Command { get; }
        public ICommand? SetExifRating5Command { get; }
        public ICommand? OpenGoogleLinkCommand { get; }
        public ICommand? OpenBingLinkCommand { get; }

        public ICommand? OptimizeImageCommand { get; }
        public ReactiveCommand<int, Unit>? ResizeCommand { get; }
        public ReactiveCommand<int, Unit>? ConvertCommand { get; }

        #endregion Commands

        #region Fields

        private string? _getFlipped;

        public string? GetFlipped
        {
            get => _getFlipped;
            set => this.RaiseAndSetIfChanged(ref _getFlipped, value);
        }

        public string? GetBottomGallery => IsBottomGalleryShown ? HideBottomGallery : ShowBottomGallery;

        private int _scaleX = 1;

        public int ScaleX
        {
            get => _scaleX;
            set => this.RaiseAndSetIfChanged(ref _scaleX, value);
        }

        private UserControl? _currentView;

        public UserControl? CurrentView
        {
            get => _currentView;
            set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        private readonly SpinWaiter? _spinWaiter;

        public ImageViewer? ImageViewer;

        private IImage? _image;

        public IImage? Image
        {
            get => _image;
            set => this.RaiseAndSetIfChanged(ref _image, value);
        }

        private uint _exifRating;

        public uint EXIFRating
        {
            get => _exifRating;
            set => this.RaiseAndSetIfChanged(ref _exifRating, value);
        }

        private int _getIndex;

        public int GetIndex
        {
            get => _getIndex;
            set => this.RaiseAndSetIfChanged(ref _getIndex, value);
        }

        private string? _getPrintSizeInch;

        public string? GetPrintSizeInch
        {
            get => _getPrintSizeInch;
            set => this.RaiseAndSetIfChanged(ref _getPrintSizeInch, value);
        }

        private string? _getPrintSizeCm;

        public string? GetPrintSizeCm
        {
            get => _getPrintSizeCm;
            set => this.RaiseAndSetIfChanged(ref _getPrintSizeCm, value);
        }

        private string? _getSizeMp;

        public string? GetSizeMp
        {
            get => _getSizeMp;
            set => this.RaiseAndSetIfChanged(ref _getSizeMp, value);
        }

        private string? _getResolution;

        public string? GetResolution
        {
            get => _getResolution;
            set => this.RaiseAndSetIfChanged(ref _getResolution, value);
        }

        private string? _getBitDepth;

        public string? GetBitDepth
        {
            get => _getBitDepth;
            set => this.RaiseAndSetIfChanged(ref _getBitDepth, value);
        }

        private string? _getAspectRatio;

        public string? GetAspectRatio
        {
            get => _getAspectRatio;
            set => this.RaiseAndSetIfChanged(ref _getAspectRatio, value);
        }

        private string? _getLatitude;

        public string? GetLatitude
        {
            get => _getLatitude;
            set => this.RaiseAndSetIfChanged(ref _getLatitude, value);
        }

        private string? _getLongitude;

        public string? GetLongitude
        {
            get => _getLongitude;
            set => this.RaiseAndSetIfChanged(ref _getLongitude, value);
        }

        private string? _getAltitude;

        public string? GetAltitude
        {
            get => _getAltitude;
            set => this.RaiseAndSetIfChanged(ref _getAltitude, value);
        }

        private string? _googleLink;

        public string? GoogleLink
        {
            get => _googleLink;
            set => this.RaiseAndSetIfChanged(ref _googleLink, value);
        }

        private string? _bingLink;

        public string? BingLink
        {
            get => _bingLink;
            set => this.RaiseAndSetIfChanged(ref _bingLink, value);
        }

        private string? _getAuthors;

        public string? GetAuthors
        {
            get => _getAuthors;
            set => this.RaiseAndSetIfChanged(ref _getAuthors, value);
        }

        private string? _getDateTaken;

        public string? GetDateTaken
        {
            get => _getDateTaken;
            set => this.RaiseAndSetIfChanged(ref _getDateTaken, value);
        }

        private string? _getCopyright;

        public string? GetCopyright
        {
            get => _getCopyright;
            set => this.RaiseAndSetIfChanged(ref _getCopyright, value);
        }

        private string? _getTitle;

        public string? GetTitle
        {
            get => _getTitle;
            set => this.RaiseAndSetIfChanged(ref _getTitle, value);
        }

        private string? _getSubject;

        public string? GetSubject
        {
            get => _getSubject;
            set => this.RaiseAndSetIfChanged(ref _getSubject, value);
        }

        private string? _getSoftware;

        public string? GetSoftware
        {
            get => _getSoftware;
            set => this.RaiseAndSetIfChanged(ref _getSoftware, value);
        }

        private string? _getResolutionUnit;

        public string? GetResolutionUnit
        {
            get => _getResolutionUnit;
            set => this.RaiseAndSetIfChanged(ref _getResolutionUnit, value);
        }

        private string? _getColorRepresentation;

        public string? GetColorRepresentation
        {
            get => _getColorRepresentation;
            set => this.RaiseAndSetIfChanged(ref _getColorRepresentation, value);
        }

        private string? _getCompression;

        public string? GetCompression
        {
            get => _getCompression;
            set => this.RaiseAndSetIfChanged(ref _getCompression, value);
        }

        private string? _getCompressedBitsPixel;

        public string? GetCompressedBitsPixel
        {
            get => _getCompressedBitsPixel;
            set => this.RaiseAndSetIfChanged(ref _getCompressedBitsPixel, value);
        }

        private string? _getCameraMaker;

        public string? GetCameraMaker
        {
            get => _getCameraMaker;
            set => this.RaiseAndSetIfChanged(ref _getCameraMaker, value);
        }

        private string? _getCameraModel;

        public string? GetCameraModel
        {
            get => _getCameraModel;
            set => this.RaiseAndSetIfChanged(ref _getCameraModel, value);
        }

        private string? _GetExposureProgram;

        public string? GetExposureProgram
        {
            get => _GetExposureProgram;
            set => this.RaiseAndSetIfChanged(ref _GetExposureProgram, value);
        }

        private string? _getExposureTime;

        public string? GetExposureTime
        {
            get => _getExposureTime;
            set => this.RaiseAndSetIfChanged(ref _getExposureTime, value);
        }

        private string? _GetExposureBias;

        public string? GetExposureBias
        {
            get => _GetExposureBias;
            set => this.RaiseAndSetIfChanged(ref _GetExposureBias, value);
        }

        private string? _GetFNumber;

        public string? GetFNumber
        {
            get => _GetFNumber;
            set => this.RaiseAndSetIfChanged(ref _GetFNumber, value);
        }

        private string? _getMaxAperture;

        public string? GetMaxAperture
        {
            get => _getMaxAperture;
            set => this.RaiseAndSetIfChanged(ref _getMaxAperture, value);
        }

        private string? _getDigitalZoom;

        public string? GetDigitalZoom
        {
            get => _getDigitalZoom;
            set => this.RaiseAndSetIfChanged(ref _getDigitalZoom, value);
        }

        private string? _getFocalLength35mm;

        public string? GetFocalLength35mm
        {
            get => _getFocalLength35mm;
            set => this.RaiseAndSetIfChanged(ref _getFocalLength35mm, value);
        }

        private string? _getFocalLength;

        public string? GetFocalLength
        {
            get => _getFocalLength;
            set => this.RaiseAndSetIfChanged(ref _getFocalLength, value);
        }

        private string? _getISOSpeed;

        public string? GetISOSpeed
        {
            get => _getISOSpeed;
            set => this.RaiseAndSetIfChanged(ref _getISOSpeed, value);
        }

        private string? _getMeteringMode;

        public string? GetMeteringMode
        {
            get => _getMeteringMode;
            set => this.RaiseAndSetIfChanged(ref _getMeteringMode, value);
        }

        private string? _getContrast;

        public string? GetContrast
        {
            get => _getContrast;
            set => this.RaiseAndSetIfChanged(ref _getContrast, value);
        }

        private string? _getSaturation;

        public string? GetSaturation
        {
            get => _getSaturation;
            set => this.RaiseAndSetIfChanged(ref _getSaturation, value);
        }

        private string? _getSharpness;

        public string? GetSharpness
        {
            get => _getSharpness;
            set => this.RaiseAndSetIfChanged(ref _getSharpness, value);
        }

        private string? _getWhiteBalance;

        public string? GetWhiteBalance
        {
            get => _getWhiteBalance;
            set => this.RaiseAndSetIfChanged(ref _getWhiteBalance, value);
        }

        private string? _getFlashMode;

        public string? GetFlashMode
        {
            get => _getFlashMode;
            set => this.RaiseAndSetIfChanged(ref _getFlashMode, value);
        }

        private string? _getFlashEnergy;

        public string? GetFlashEnergy
        {
            get => _getFlashEnergy;
            set => this.RaiseAndSetIfChanged(ref _getFlashEnergy, value);
        }

        private string? _getLightSource;

        public string? GetLightSource
        {
            get => _getLightSource;
            set => this.RaiseAndSetIfChanged(ref _getLightSource, value);
        }

        private string? _getBrightness;

        public string? GetBrightness
        {
            get => _getBrightness;
            set => this.RaiseAndSetIfChanged(ref _getBrightness, value);
        }

        private string? _getPhotometricInterpretation;

        public string? GetPhotometricInterpretation
        {
            get => _getPhotometricInterpretation;
            set => this.RaiseAndSetIfChanged(ref _getPhotometricInterpretation, value);
        }

        private string? _getOrientation;

        public string? GetOrientation
        {
            get => _getOrientation;
            set => this.RaiseAndSetIfChanged(ref _getOrientation, value);
        }

        private string? _getExifVersion;

        public string? GetExifVersion
        {
            get => _getExifVersion;
            set => this.RaiseAndSetIfChanged(ref _getExifVersion, value);
        }

        private string? _GetLensModel;

        public string? GetLensModel
        {
            get => _GetLensModel;
            set => this.RaiseAndSetIfChanged(ref _GetLensModel, value);
        }

        private string? _getLensMaker;

        public string? GetLensMaker
        {
            get => _getLensMaker;
            set => this.RaiseAndSetIfChanged(ref _getLensMaker, value);
        }

        #region Window Properties

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

        private bool _isRotationAnimationEnabled;

        public bool IsRotationAnimationEnabled
        {
            get => _isRotationAnimationEnabled;
            set => this.RaiseAndSetIfChanged(ref _isRotationAnimationEnabled, value);
        }

        #endregion Window Properties

        #region Size

        private double _imageWidth;

        public double ImageWidth
        {
            get => _imageWidth;
            set => this.RaiseAndSetIfChanged(ref _imageWidth, value);
        }

        private double _imageHeight;

        public double ImageHeight
        {
            get => _imageHeight;
            set => this.RaiseAndSetIfChanged(ref _imageHeight, value);
        }

        private double _titleMaxWidth;

        public double TitleMaxWidth
        {
            get => _titleMaxWidth;
            set => this.RaiseAndSetIfChanged(ref _titleMaxWidth, value);
        }

        #endregion Size

        #region Zoom

        private double _rotationAngle;

        public double RotationAngle
        {
            get => _rotationAngle;
            set => this.RaiseAndSetIfChanged(ref _rotationAngle, value);
        }

        private double _zoomValue;

        public double ZoomValue
        {
            get => _zoomValue;
            set => this.RaiseAndSetIfChanged(ref _zoomValue, value);
        }

        private ScrollBarVisibility _toggleScrollBarVisibility;

        public ScrollBarVisibility ToggleScrollBarVisibility
        {
            get => _toggleScrollBarVisibility;
            set => this.RaiseAndSetIfChanged(ref _toggleScrollBarVisibility, value);
        }

        #endregion Zoom

        #region Settings

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

        private bool _isTopMost = SettingsHelper.Settings.WindowProperties.TopMost;

        public bool IsTopMost
        {
            get => _isTopMost;
            set => this.RaiseAndSetIfChanged(ref _isTopMost, value);
        }

        private bool _includeSubdirectories = SettingsHelper.Settings.Sorting.IncludeSubDirectories;

        public bool IncludeSubdirectories
        {
            get => _includeSubdirectories;
            set
            {
                this.RaiseAndSetIfChanged(ref _includeSubdirectories, value);
                SettingsHelper.Settings.Sorting.IncludeSubDirectories = value;
                _ = SettingsHelper.SaveSettingsAsync();
            }
        }

        private bool _isScrollingEnabled = SettingsHelper.Settings.Zoom.ScrollEnabled;

        public bool IsScrollingEnabled
        {
            get => _isScrollingEnabled;
            set
            {
                this.RaiseAndSetIfChanged(ref _isScrollingEnabled, value);
                ToggleScrollBarVisibility = value ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
                SettingsHelper.Settings.Zoom.ScrollEnabled = value;
                SetSize();
                _ = SettingsHelper.SaveSettingsAsync();
            }
        }

        private bool _isStretched = SettingsHelper.Settings.ImageScaling.StretchImage;

        public bool IsStretched
        {
            get => _isStretched;
            set
            {
                this.RaiseAndSetIfChanged(ref _isStretched, value);
                SettingsHelper.Settings.ImageScaling.StretchImage = value;
                SetSize();
                _ = SettingsHelper.SaveSettingsAsync();
            }
        }

        private bool _isLooping = SettingsHelper.Settings.UIProperties.Looping;

        public bool IsLooping
        {
            get => _isLooping;
            set
            {
                this.RaiseAndSetIfChanged(ref _isLooping, value);
                SettingsHelper.Settings.UIProperties.Looping = value;
                _ = SettingsHelper.SaveSettingsAsync();
            }
        }

        private bool _isAutoFit = SettingsHelper.Settings.WindowProperties.AutoFit;

        public bool IsAutoFit
        {
            get => _isAutoFit;
            set => this.RaiseAndSetIfChanged(ref _isAutoFit, value);
        }

        private bool _isInterfaceShown = SettingsHelper.Settings.UIProperties.ShowInterface;

        public bool IsInterfaceShown
        {
            get => _isInterfaceShown;
            set => this.RaiseAndSetIfChanged(ref _isInterfaceShown, value);
        }

        #endregion Settings

        #region Menus

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

        #endregion Menus

        #endregion Fields

        #region Services

        public FileService? FileService;

        public ImageIterator? ImageIterator;

        public ImageService? ImageService;

        #endregion Services

        #region Methods

        #region Size

        public void SetSize()
        {
            if (Image is null)
            {
                return;
            }
            var preloadValue = ImageIterator?.PreLoader.Get(ImageIterator.Index, ImageIterator.Pics);
            SetSize(preloadValue?.ImageModel?.PixelWidth ?? (int)ImageWidth, preloadValue?.ImageModel?.PixelHeight ?? (int)ImageHeight, RotationAngle);
        }

        public void SetSize(double width, double height, double rotation)
        {
            width = width == 0 ? ImageWidth : width;
            height = height == 0 ? ImageHeight : height;
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            var monitor = ScreenHelper.GetScreen(desktop.MainWindow);
            double desktopMinWidth = 0, desktopMinHeight = 0, containerWidth = 0, containerHeight = 0;
            var uiTopSize = SettingsHelper.Settings.UIProperties.ShowInterface ? 32 : 0; // Height of the titlebar, TODO get actual size
            var uiBottomSize =
                SettingsHelper.Settings.UIProperties.ShowInterface || SettingsHelper.Settings.UIProperties.ShowBottomNavBar
                    ? 28 : 0;
            var galleryHeight = IsBottomGalleryShown ? 100 : 0;
            if (Dispatcher.UIThread.CheckAccess())
            {
                desktopMinWidth = desktop.MainWindow.MinWidth;
                desktopMinHeight = desktop.MainWindow.MinHeight;
                containerWidth = desktop.MainWindow.Width;
                containerHeight = desktop.MainWindow.Height - (uiTopSize + uiBottomSize);
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    desktopMinWidth = desktop.MainWindow.MinWidth;
                    desktopMinHeight = desktop.MainWindow.MinHeight;
                    containerWidth = desktop.MainWindow.Width;
                    containerHeight = desktop.MainWindow.Height - (uiTopSize + uiBottomSize);
                }, DispatcherPriority.Normal).Wait();
            }
            var size = ImageSizeCalculationHelper.GetImageSize(
                width,
                height,
                monitor.Bounds.Width,
                monitor.Bounds.Height,
                desktopMinWidth,
                desktopMinHeight,
                ImageSizeCalculationHelper.GetInterfaceSize(),
                rotation,
                IsStretched,
                75,
                monitor.Scaling,
                SettingsHelper.Settings.WindowProperties.Fullscreen,
                uiTopSize,
                uiBottomSize,
                galleryHeight,
                IsAutoFit,
                containerWidth,
                containerHeight,
                IsScrollingEnabled);

            TitleMaxWidth = size.TitleMaxWidth;
            ImageWidth = size.Width;
            ImageHeight = size.Height;
        }

        #endregion Size

        public void SetImageModel(ImageModel imageModel)
        {
            Image = imageModel?.Image ?? null; // TODO replace with broken image graphic if it is null
            FileInfo = imageModel?.FileInfo ?? null;
            if (imageModel?.EXIFOrientation.HasValue ?? false)
            {
                switch (imageModel.EXIFOrientation.Value)
                {
                    default:
                        ScaleX = 1;
                        RotationAngle = 0;
                        GetOrientation = TranslationHelper.GetTranslation("Normal");
                        break;

                    case EXIFHelper.EXIFOrientation.Flipped:
                        ScaleX = -1;
                        RotationAngle = 0;
                        GetOrientation = TranslationHelper.GetTranslation("Flipped");
                        break;

                    case EXIFHelper.EXIFOrientation.Rotated180:
                        RotationAngle = 180;
                        ScaleX = 1;
                        GetOrientation = $"{TranslationHelper.GetTranslation("Rotated")} 180\u00b0";
                        break;

                    case EXIFHelper.EXIFOrientation.Rotated180Flipped:
                        RotationAngle = 180;
                        ScaleX = -1;
                        GetOrientation = $"{TranslationHelper.GetTranslation("Rotated")} 180\u00b0, {TranslationHelper.GetTranslation("Flipped")}";
                        break;

                    case EXIFHelper.EXIFOrientation.Rotated270Flipped:
                        RotationAngle = 270;
                        ScaleX = -1;
                        GetOrientation = $"{TranslationHelper.GetTranslation("Rotated")} 270\u00b0, {TranslationHelper.GetTranslation("Flipped")}";
                        break;

                    case EXIFHelper.EXIFOrientation.Rotated90:
                        RotationAngle = 90;
                        ScaleX = 1;
                        GetOrientation = $"{TranslationHelper.GetTranslation("Rotated")} 90\u00b0";
                        break;

                    case EXIFHelper.EXIFOrientation.Rotated90Flipped:
                        RotationAngle = 90;
                        ScaleX = -1;
                        GetOrientation = $"{TranslationHelper.GetTranslation("Rotated")} 90\u00b0, {TranslationHelper.GetTranslation("Flipped")}";
                        break;

                    case EXIFHelper.EXIFOrientation.Rotated270:
                        RotationAngle = 270;
                        ScaleX = 1;
                        GetOrientation = $"{TranslationHelper.GetTranslation("Rotated")} 270\u00b0";
                        break;
                }
            }
            else
            {
                ScaleX = 1;
                RotationAngle = 0;
                GetOrientation = string.Empty;
            }

            ZoomValue = 1;
            PixelWidth = imageModel?.PixelWidth ?? 0;
            PixelHeight = imageModel?.PixelHeight ?? 0;

            if (FileInfo is null)
            {
                return;
            }

            Task.Run(() =>
            {
                using var magick = new MagickImage();
                try
                {
                    magick.Ping(FileInfo);
                    var profile = magick.GetExifProfile();

                    if (profile != null)
                    {
                        DpiY = profile?.GetValue(ExifTag.YResolution)?.Value.ToDouble() ?? 0;
                        DpiX = profile?.GetValue(ExifTag.XResolution)?.Value.ToDouble() ?? 0;
                        var depth = profile?.GetValue(ExifTag.BitsPerSample)?.Value;
                        if (depth is not null)
                        {
                            var x = depth.Aggregate(0, (current, value) => current + value);
                            GetBitDepth = x.ToString();
                        }
                        else
                        {
                            GetBitDepth = (magick.Depth * 3).ToString();
                        }
                    }

                    if (DpiX is 0)
                    {
                        var bmp = Image as Bitmap;
                        DpiX = bmp?.Dpi.X ?? 0;
                        DpiY = bmp?.Dpi.Y ?? 0;
                    }

                    if (string.IsNullOrEmpty(GetBitDepth))
                    {
                        GetBitDepth = (magick.Depth * 3).ToString();
                    }

                    if (DpiX == 0)
                    {
                        GetPrintSizeCm = GetPrintSizeInch = GetSizeMp = GetResolution = string.Empty;
                    }
                    else
                    {
                        var inchesWidth = PixelWidth / DpiX;
                        var inchesHeight = PixelHeight / DpiY;
                        GetPrintSizeInch =
                            $"{inchesWidth.ToString("0.##", CultureInfo.CurrentCulture)} x {inchesHeight.ToString("0.##", CultureInfo.CurrentCulture)} {TranslationHelper.GetTranslation("Inches")}";

                        var cmWidth = PixelWidth / DpiX * 2.54;
                        var cmHeight = PixelHeight / DpiY * 2.54;
                        GetPrintSizeCm =
                            $"{cmWidth.ToString("0.##", CultureInfo.CurrentCulture)} x {cmHeight.ToString("0.##", CultureInfo.CurrentCulture)} {TranslationHelper.GetTranslation("Centimeters")}";
                        GetSizeMp =
                            $"{((float)PixelHeight * PixelWidth / 1000000).ToString("0.##", CultureInfo.CurrentCulture)} {TranslationHelper.GetTranslation("MegaPixels")}";

                        GetResolution = $"{DpiX} x {DpiY} {TranslationHelper.GetTranslation("Dpi")}";
                    }

                    var firstRatio = PixelWidth / TitleHelper.GCD(PixelWidth, PixelHeight);
                    var secondRatio = PixelHeight / TitleHelper.GCD(PixelWidth, PixelHeight);

                    if (firstRatio == secondRatio)
                    {
                        GetAspectRatio = $"{firstRatio}:{secondRatio} ({TranslationHelper.GetTranslation("Square")})";
                    }
                    else if (firstRatio > secondRatio)
                    {
                        GetAspectRatio =
                            $"{firstRatio}:{secondRatio} ({TranslationHelper.GetTranslation("Landscape")})";
                    }
                    else
                    {
                        GetAspectRatio = $"{firstRatio}:{secondRatio} ({TranslationHelper.GetTranslation("Portrait")})";
                    }

                    EXIFRating = profile?.GetValue(ExifTag.Rating)?.Value ?? 0;

                    var gpsValues = EXIFHelper.GetGPSValues(profile);

                    if (gpsValues is not null)
                    {
                        GetLatitude = gpsValues[0];
                        GetLongitude = gpsValues[1];

                        GoogleLink = gpsValues[2];
                        BingLink = gpsValues[3];
                    }
                    else
                    {
                        GetLatitude = GetLongitude = GoogleLink = BingLink = string.Empty;
                    }

                    var altitude = profile?.GetValue(ExifTag.GPSAltitude)?.Value;
                    GetAltitude = altitude.HasValue
                        ? $"{altitude.Value.ToDouble()} {TranslationHelper.GetTranslation("Meters")}"
                        : string.Empty;
                    var getAuthors = profile?.GetValue(ExifTag.Artist)?.Value;
                    GetAuthors = getAuthors ?? string.Empty;
                    GetDateTaken = EXIFHelper.GetDateTaken(profile);
                    GetCopyright = profile?.GetValue(ExifTag.Copyright)?.Value ?? string.Empty;
                    GetTitle = profile?.GetValue(ExifTag.XPTitle)?.Value.ToString() ?? string.Empty;
                    GetSubject = profile?.GetValue(ExifTag.XPSubject)?.Value.ToString() ?? string.Empty;
                    GetSoftware = profile?.GetValue(ExifTag.Software)?.Value ?? string.Empty;
                    GetResolutionUnit = EXIFHelper.GetResolutionUnit(profile);
                    GetColorRepresentation = EXIFHelper.GetColorSpace(profile);
                    GetCompression = profile?.GetValue(ExifTag.Compression)?.Value.ToString() ?? string.Empty;
                    GetCompressedBitsPixel = profile?.GetValue(ExifTag.CompressedBitsPerPixel)?.Value.ToString() ?? string.Empty;
                    GetCameraMaker = profile?.GetValue(ExifTag.Make)?.Value ?? string.Empty;
                    GetCameraModel = profile?.GetValue(ExifTag.Model)?.Value ?? string.Empty;
                    GetExposureProgram = EXIFHelper.GetExposureProgram(profile);
                    GetExposureTime = profile?.GetValue(ExifTag.ExposureTime)?.Value.ToString() ?? string.Empty;
                    GetFNumber = profile?.GetValue(ExifTag.FNumber)?.Value.ToString() ?? string.Empty;
                    GetMaxAperture = profile?.GetValue(ExifTag.MaxApertureValue)?.Value.ToString() ?? string.Empty;
                    GetExposureBias = profile?.GetValue(ExifTag.ExposureBiasValue)?.Value.ToString() ?? string.Empty;
                    GetDigitalZoom = profile?.GetValue(ExifTag.DigitalZoomRatio)?.Value.ToString() ?? string.Empty;
                    GetFocalLength35mm = profile?.GetValue(ExifTag.FocalLengthIn35mmFilm)?.Value.ToString() ?? string.Empty;
                    GetFocalLength = profile?.GetValue(ExifTag.FocalLength)?.Value.ToString() ?? string.Empty;
                    GetISOSpeed = EXIFHelper.GetISOSpeed(profile);
                    GetMeteringMode = profile?.GetValue(ExifTag.MeteringMode)?.Value.ToString() ?? string.Empty;
                    GetContrast = EXIFHelper.GetContrast(profile);
                    GetSaturation = EXIFHelper.GetSaturation(profile);
                    GetSharpness = EXIFHelper.GetSharpness(profile);
                    GetWhiteBalance = EXIFHelper.GetWhiteBalance(profile);
                    GetFlashMode = EXIFHelper.GetFlashMode(profile);
                    GetFlashEnergy = profile?.GetValue(ExifTag.FlashEnergy)?.Value.ToString() ?? string.Empty;
                    GetLightSource = EXIFHelper.GetLightSource(profile);
                    GetBrightness = profile?.GetValue(ExifTag.BrightnessValue)?.Value.ToString() ?? string.Empty;
                    GetPhotometricInterpretation = EXIFHelper.GetPhotometricInterpretation(profile);
                    GetExifVersion = EXIFHelper.GetExifVersion(profile);
                    GetLensModel = profile?.GetValue(ExifTag.LensModel)?.Value ?? string.Empty;
                    GetLensMaker = profile?.GetValue(ExifTag.LensMake)?.Value ?? string.Empty;
                }
                catch (Exception)
                {
                    // TODO display exception to user
                }
            });
        }

        public void SetTitle(ImageModel? imageModel, ImageIterator imageIterator)
        {
            if (imageModel is null || ImageIterator is null)
            {
                ReturnError();
                return;
            }

            if (imageModel.FileInfo is null)
            {
                ReturnError();
                return;
            }

            var titleString = TitleHelper.GetTitle(imageModel.PixelWidth, imageModel.PixelHeight, imageIterator.Index,
                imageModel.FileInfo, ZoomValue, imageIterator.Pics);
            WindowTitle = titleString[0];
            Title = titleString[1];
            TitleTooltip = titleString[2];

            return;

            void ReturnError()
            {
                WindowTitle =
                Title =
                TitleTooltip = TranslationHelper.GetTranslation("UnableToRender");
            }
        }

        public void SetTitle()
        {
            if (ImageIterator is null)
            {
                WindowTitle =
                    Title =
                        TitleTooltip = TranslationHelper.GetTranslation("UnableToRender");
                return;
            }

            var titleString = TitleHelper.GetTitle((int)ImageWidth, (int)ImageHeight, ImageIterator.Index,
                    FileInfo, ZoomValue, ImageIterator.Pics);
            WindowTitle = titleString[0];
            Title = titleString[1];
            TitleTooltip = titleString[2];
        }

        public void RefreshTitle()
        {
            var path = FileInfo.FullName;
            FileInfo = new FileInfo(path);
            SetTitle();
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

        public async Task LoadNextPic(NavigateTo navigateTo)
        {
            if (ImageIterator is null)
            {
                return;
            }
            var index = ImageIterator.GetIteration(ImageIterator.Index, navigateTo);
            if (index < 0)
            {
                return;
            }
            await LoadPicAtIndex(index);
        }

        public async Task LoadPicAtIndex(int index) => await Task.Run(async () =>
        {
            if (ImageIterator is null)
            {
                return;
            }

            try
            {
                ImageIterator.Index = index;

                var preLoadValue = ImageIterator.PreLoader.Get(index, ImageIterator.Pics);
                var viewChanged = false;
                var x = 0;
                if (preLoadValue is not null)
                {
                    if (preLoadValue.IsLoading)
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
                        else if (!viewChanged)
                        {
                            CurrentView = _spinWaiter;
                            viewChanged = true;
                        }
                    }

                    while (preLoadValue.IsLoading)
                    {
                        x++;
                        await Task.Delay(20);
                        if (ImageIterator.Index != index)
                        {
                            await ImageIterator.Preload(ImageService);
                            CurrentView = ImageViewer;
                            return;
                        }

                        if (x > 200)
                        {
                            await GetPreload();
                            break;
                        }
                    }
                }

                if (preLoadValue is null)
                {
                    if (!viewChanged)
                    {
                        CurrentView = _spinWaiter;
                    }
                    await GetPreload();
                }

                CurrentView = ImageViewer;

                if (ImageIterator.Index != index)
                {
                    await ImageIterator.Preload(ImageService);
                    return;
                }

                SetImageModel(preLoadValue.ImageModel);
                SetSize(preLoadValue.ImageModel.PixelWidth, preLoadValue.ImageModel.PixelHeight, 0);
                SetTitle(preLoadValue.ImageModel, ImageIterator);
                GetIndex = ImageIterator.Index + 1;
                ImageChanged?.Invoke(this, preLoadValue.ImageModel);
                await ImageIterator.AddAsync(ImageIterator.Index, ImageService, preLoadValue?.ImageModel);
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
                        throw new ArgumentNullException(nameof(LoadNextPic),
                            nameof(preLoadValue) + " is null");
                    }
                }
            }
            catch (Exception)
            {
                // TODO display exception to user
            }
        }).ConfigureAwait(false);

        public async Task LoadPicFromString(string path)
        {
            if (!Path.Exists(path))
            {
                // TODO load from URL if not a file
                throw new FileNotFoundException(path);
            }

            await LoadPicFromFile(new FileInfo(path)).ConfigureAwait(false);
        }

        public async Task LoadPicFromFile(FileInfo fileInfo)
        {
            SetLoadingTitle();
            try
            {
                ArgumentNullException.ThrowIfNull(fileInfo);

                var imageModel = new ImageModel
                {
                    FileInfo = fileInfo
                };

                ImageService ??= new ImageService();
                await ImageService.LoadImageAsync(imageModel);
                SetImageModel(imageModel);
                SetSize(imageModel.PixelWidth, imageModel.PixelHeight, imageModel.Rotation);
                ImageIterator = new ImageIterator(imageModel.FileInfo, _platformService);
                await ImageIterator.AddAsync(ImageIterator.Index, ImageService, imageModel);
                await LoadPicAtIndex(ImageIterator.Index);
                ImageIterator.FileAdded += (_, e) => { SetTitle(); };
                ImageIterator.FileRenamed += (_, e) => { SetTitle(); };
                ImageIterator.FileDeleted += async (_, isSameFile) =>
                {
                    if (isSameFile) //change if deleting current file
                    {
                        if (ImageIterator?.Index < 0 || ImageIterator?.Index >= ImageIterator?.Pics.Count)
                        {
                            return;
                        }
                        await LoadPicFromString(ImageIterator?.Pics[ImageIterator.Index]);
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
        }

        private async Task ResizeImageByPercentage(int percentage)
        {
            SetLoadingTitle();
            var success = await ConversionHelper.ResizeImageByPercentage(FileInfo, percentage);
            if (success)
            {
                ImageIterator?.PreLoader.Remove(ImageIterator.Index, ImageIterator.Pics);
                await LoadPicAtIndex(ImageIterator.Index);
            }
            else
            {
                SetTitle();
            }
        }

        private async Task ConvertFileExtension(int index)
        {
            if (FileInfo is null)
            {
                return;
            }

            if (ImageIterator is not null)
            {
                ImageIterator.IsFileBeingRenamed = true;
            }
            var newPath = await ConversionHelper.ConvertTask(FileInfo, index);
            if (!string.IsNullOrWhiteSpace(newPath))
            {
                await LoadPicFromString(newPath);
            }
            if (ImageIterator is not null)
            {
                ImageIterator.IsFileBeingRenamed = false;
            }
        }

        public async Task StartUpTask()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            ImageViewer = new ImageViewer();
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                await Task.Run(async () => { await LoadPicFromString(args[1]); }).ConfigureAwait(false);
            }
            else if (SettingsHelper.Settings.StartUp.OpenLastFile)
            {
                await Task.Run(async () => { await LoadPicFromString(SettingsHelper.Settings.StartUp.LastFile); }).ConfigureAwait(false);
            }
            else
            {
                CurrentView = new StartUpMenu();
            }
        }

        #endregion Methods

        public MainViewModel(IPlatformSpecificService? platformSpecificService)
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            SetLoadingTitle();

            _spinWaiter = new SpinWaiter();
            CurrentView = _spinWaiter;

            Task.Run(UpdateLanguage);

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

            ChangeAutoFitCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (SettingsHelper.Settings.WindowProperties.AutoFit)
                {
                    SizeToContent = SizeToContent.Manual;
                    CanResize = true;
                    SettingsHelper.Settings.WindowProperties.AutoFit = false;
                    IsAutoFit = false;
                }
                else
                {
                    SizeToContent = SizeToContent.WidthAndHeight;
                    CanResize = false;
                    SettingsHelper.Settings.WindowProperties.AutoFit = true;
                    IsAutoFit = true;
                }
                SetSize();
                await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
            });

            ChangeTopMostCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (SettingsHelper.Settings.WindowProperties.TopMost)
                {
                    IsTopMost = false;
                    desktop.MainWindow.Topmost = false;
                    SettingsHelper.Settings.WindowProperties.TopMost = false;
                }
                else
                {
                    IsTopMost = true;
                    desktop.MainWindow.Topmost = true;
                    SettingsHelper.Settings.WindowProperties.TopMost = true;
                }

                await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
            });
            if (SettingsHelper.Settings.WindowProperties.TopMost)
            {
                desktop.MainWindow.Topmost = true;
            }

            ChangeIncludeSubdirectoriesCommand = ReactiveCommand.Create(() =>
            {
                IncludeSubdirectories = !IncludeSubdirectories;
                SetTitle();
            });

            ToggleUICommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (SettingsHelper.Settings.UIProperties.ShowInterface)
                {
                    IsInterfaceShown = false;
                    SettingsHelper.Settings.UIProperties.ShowInterface = false;
                }
                else
                {
                    IsInterfaceShown = true;
                    SettingsHelper.Settings.UIProperties.ShowInterface = true;
                }
                CloseMenuCommand.Execute(null);
                await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
            });

            ShowExifWindowCommand = ReactiveCommand.Create(() =>
            {
            });

            #endregion Window commands

            #region Navigation Commands

            NextCommand = ReactiveCommand.Create(async () =>
            {
                if (ImageIterator is null)
                {
                    return;
                }

                await LoadNextPic(NavigateTo.Next).ConfigureAwait(false);
            });

            PreviousCommand = ReactiveCommand.Create(async () =>
            {
                if (ImageIterator is null)
                {
                    return;
                }

                await LoadNextPic(NavigateTo.Previous).ConfigureAwait(false);
            });

            FirstCommand = ReactiveCommand.Create(async () =>
            {
                if (ImageIterator is null)
                {
                    return;
                }

                await LoadNextPic(NavigateTo.First).ConfigureAwait(false);
            });

            LastCommand = ReactiveCommand.Create(async () =>
            {
                if (ImageIterator is null)
                {
                    return;
                }

                await LoadNextPic(NavigateTo.Last).ConfigureAwait(false);
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
                await LoadPicFromString(FileInfo.FullName);
            });

            #endregion Navigation Commands

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
                if (Image is null)
                {
                    return;
                }
                if (RotationHelper.IsValidRotation(RotationAngle))
                {
                    var nextAngle = RotationHelper.Rotate(RotationAngle, true);
                    if (nextAngle == 360)
                    {
                        RotationAngle = 0;
                    }
                    else
                    {
                        RotationAngle = nextAngle;
                    }
                }
                else
                {
                    RotationAngle = RotationHelper.NextRotationAngle(RotationAngle, true);
                }
                SetSize();
            });

            RotateRightCommand = ReactiveCommand.Create(() =>
            {
                if (Image is null)
                {
                    return;
                }
                if (RotationHelper.IsValidRotation(RotationAngle))
                {
                    var nextAngle = RotationHelper.Rotate(RotationAngle, false);
                    if (nextAngle == -90)
                    {
                        RotationAngle = 270;
                    }
                    else
                    {
                        RotationAngle = nextAngle;
                    }
                }
                else
                {
                    RotationAngle = RotationHelper.NextRotationAngle(RotationAngle, false);
                }

                SetSize();
            });

            GetFlipped = Flip;
            FlipCommand = ReactiveCommand.Create(() =>
            {
                if (Image is null)
                {
                    return;
                }
                if (ScaleX == 1)
                {
                    ScaleX = -1;
                    GetFlipped = UnFlip;
                }
                else
                {
                    ScaleX = 1;
                    GetFlipped = Flip;
                }
            });

            OptimizeImageCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (FileInfo is null)
                {
                    return;
                }
                await Task.Run(() =>
                {
                    try
                    {
                        ImageOptimizer imageOptimizer = new()
                        {
                            OptimalCompression = true
                        };
                        imageOptimizer.LosslessCompress(FileInfo.FullName);
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e);
                    }
                });
                RefreshTitle();
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
                    await LoadPicFromFile(new FileInfo(path));
                }
                else
                {
                    await LoadPicFromFile(new FileInfo(file.Path.LocalPath));
                }
            });

            OpenLastFileCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var lastFile = SettingsHelper.Settings.StartUp.LastFile;
                if (string.IsNullOrEmpty(lastFile))
                {
                    return;
                }

                await LoadPicFromString(lastFile);
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

            CopyImageCommand = ReactiveCommand.Create(() => { });

            CutCommand = ReactiveCommand.Create(() => { });

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

            RenameCommand = ReactiveCommand.Create(() =>
            {
            });

            ResizeCommand = ReactiveCommand.CreateFromTask<int>(ResizeImageByPercentage);
            ConvertCommand = ReactiveCommand.CreateFromTask<int>(ConvertFileExtension);

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

            #endregion File commands

            #region EXIF commands

            SetExifRating1Command = ReactiveCommand.CreateFromTask(async () =>
            {
                await EXIFHelper.SetEXIFRating(FileInfo.FullName, 1);
                EXIFRating = 1;
            });
            SetExifRating2Command = ReactiveCommand.CreateFromTask(async () =>
            {
                await EXIFHelper.SetEXIFRating(FileInfo.FullName, 2);
                EXIFRating = 2;
            });
            SetExifRating3Command = ReactiveCommand.CreateFromTask(async () =>
            {
                await EXIFHelper.SetEXIFRating(FileInfo.FullName, 3);
                EXIFRating = 3;
            });
            SetExifRating4Command = ReactiveCommand.CreateFromTask(async () =>
            {
                await EXIFHelper.SetEXIFRating(FileInfo.FullName, 4);
                EXIFRating = 4;
            });
            SetExifRating5Command = ReactiveCommand.CreateFromTask(async () =>
            {
                await EXIFHelper.SetEXIFRating(FileInfo.FullName, 5);
                EXIFRating = 5;
            });

            OpenGoogleLinkCommand = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrEmpty(GoogleLink))
                {
                    return;
                }
                ProcessHelper.OpenLink(GoogleLink);
            });

            OpenBingLinkCommand = ReactiveCommand.Create(() =>
            {
                if (string.IsNullOrEmpty(BingLink))
                {
                    return;
                }
                ProcessHelper.OpenLink(BingLink);
            });

            #endregion EXIF commands

            _platformService = platformSpecificService;
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

        public MainViewModel()
        {
            throw new NotImplementedException();
        }
    }
}