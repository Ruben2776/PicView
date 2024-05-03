using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Windows.Input;
using static PicView.Core.Gallery.GalleryThumbInfo;
using ImageViewer = PicView.Avalonia.Views.ImageViewer;

namespace PicView.Avalonia.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public event EventHandler<ImageModel>? ImageChanged;

        public readonly IPlatformSpecificService? PlatformService;

        #region Gallery

        private ObservableCollection<GalleryThumbHolder>? _galleryItems;

        public ObservableCollection<GalleryThumbHolder>? GalleryItems
        {
            get => _galleryItems;
            set => this.RaiseAndSetIfChanged(ref _galleryItems, value);
        }

        private GalleryThumbHolder? _selectedGalleryItem;

        public GalleryThumbHolder? SelectedGalleryItem
        {
            get => _selectedGalleryItem;
            set => this.RaiseAndSetIfChanged(ref _selectedGalleryItem, value);
        }

        private VerticalAlignment _galleryVerticalAlignment;

        public VerticalAlignment GalleryVerticalAlignment
        {
            get => _galleryVerticalAlignment;
            set => this.RaiseAndSetIfChanged(ref _galleryVerticalAlignment, value);
        }

        private Orientation _galleryOrientation;

        public Orientation GalleryOrientation
        {
            set => this.RaiseAndSetIfChanged(ref _galleryOrientation, value);
            get => _galleryOrientation;
        }

        private bool _isGalleryCloseIconVisible;

        public bool IsGalleryCloseIconVisible
        {
            get => _isGalleryCloseIconVisible;
            set => this.RaiseAndSetIfChanged(ref _isGalleryCloseIconVisible, value);
        }

        private double _galleryItemSize;

        public double GalleryItemSize
        {
            get => _galleryItemSize;
            set => this.RaiseAndSetIfChanged(ref _galleryItemSize, value);
        }

        #endregion Gallery

        #region Commands

        public ICommand? ExitCommand { get; }
        public ICommand? MinimizeCommand { get; }
        public ICommand? MaximizeCommand { get; }

        public ICommand? NextCommand { get; }
        public ICommand? NextButtonCommand { get; }
        public ICommand? PreviousCommand { get; }
        public ICommand? PreviousButtonCommand { get; }
        public ICommand? FirstCommand { get; }
        public ICommand? LastCommand { get; }
        public ICommand? OpenFileCommand { get; }
        public ICommand? SaveFileCommand { get; }
        public ICommand? OpenLastFileCommand { get; }
        public ICommand? PasteCommand { get; }
        public ICommand? CopyFileCommand { get; }
        public ICommand? CopyFilePathCommand { get; }
        public ICommand? CopyImageCommand { get; }
        public ICommand? CutCommand { get; }
        public ICommand? ReloadCommand { get; }
        public ICommand? PrintCommand { get; }
        public ICommand? DeleteFileCommand { get; }
        public ICommand? RecycleFileCommand { get; }
        public ICommand? CloseMenuCommand { get; }
        public ICommand? ToggleFileMenuCommand { get; }
        public ICommand? ToggleImageMenuCommand { get; }
        public ICommand? ToggleSettingsMenuCommand { get; }
        public ICommand? ToggleToolsMenuCommand { get; }
        public ICommand? ShowInFolderCommand { get; }
        public ICommand? OpenWithCommand { get; }
        public ICommand? RenameCommand { get; }
        public ICommand? NewWindowCommand { get; }
        public ICommand? DuplicateFileCommand { get; }

        public ICommand? ToggleLoopingCommand { get; }
        public ICommand? RotateLeftCommand { get; }
        public ICommand? RotateRightCommand { get; }
        public ICommand? FlipCommand { get; }
        public ICommand? ChangeAutoFitCommand { get; }

        public ICommand? ChangeTopMostCommand { get; }

        public ICommand? ChangeCtrlZoomCommand { get; }

        public ICommand? ToggleUICommand { get; }

        public ICommand? ToggleBottomNavBarCommand { get; }
        public ICommand? ShowExifWindowCommand { get; }
        public ICommand? ShowAboutWindowCommand { get; }
        public ICommand? ShowSettingsWindowCommand { get; }
        public ICommand? ShowKeybindingsWindowCommand { get; }

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

        public ICommand? SortFilesByNameCommand { get; }
        public ICommand? SortFilesBySizeCommand { get; }
        public ICommand? SortFilesByExtensionCommand { get; }
        public ICommand? SortFilesByCreationTimeCommand { get; }
        public ICommand? SortFilesByLastAccessTimeCommand { get; }
        public ICommand? SortFilesByLastWriteTimeCommand { get; }
        public ICommand? SortFilesRandomlyCommand { get; }
        public ICommand? SortFilesAscendingCommand { get; }
        public ICommand? SortFilesDescendingCommand { get; }

        public ICommand? ToggleGalleryCommand { get; }

        public ICommand? ToggleBottomGalleryCommand { get; }

        public ICommand? ToggleScrollCommand { get; }

        public ICommand? ToggleSubdirectoriesCommand { get; }

        public ICommand? ColorPickerCommand { get; }

        #endregion Commands

        #region Fields

        #region Booleans

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        private bool _isInterfaceShown = SettingsHelper.Settings.UIProperties.ShowInterface;

        public bool IsInterfaceShown
        {
            get => _isInterfaceShown;
            set => this.RaiseAndSetIfChanged(ref _isInterfaceShown, value);
        }

        private bool _isTopToolbarShown = SettingsHelper.Settings.UIProperties.ShowInterface;

        public bool IsTopToolbarShown
        {
            get => _isTopToolbarShown;
            set => this.RaiseAndSetIfChanged(ref _isTopToolbarShown, value);
        }

        private bool _isBottomToolbarShown = SettingsHelper.Settings.UIProperties.ShowBottomNavBar &&
                                             SettingsHelper.Settings.UIProperties.ShowInterface;

        public bool IsBottomToolbarShown
        {
            get => _isBottomToolbarShown;
            set => this.RaiseAndSetIfChanged(ref _isBottomToolbarShown, value);
        }

        private bool _isBottomGalleryShown = SettingsHelper.Settings.Gallery.IsBottomGalleryShown;

        public bool IsBottomGalleryShown
        {
            get => _isBottomGalleryShown;
            set => this.RaiseAndSetIfChanged(ref _isBottomGalleryShown, value);
        }

        private bool _isBottomGalleryShownInHiddenUi = SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI;

        public bool IsBottomGalleryShownInHiddenUI
        {
            get => _isBottomGalleryShownInHiddenUi;
            set => this.RaiseAndSetIfChanged(ref _isBottomGalleryShownInHiddenUi, value);
        }

        private bool _isShowingButtonsInHiddenUI = SettingsHelper.Settings.UIProperties.ShowAltInterfaceButtons;

        public bool IsShowingButtonsInHiddenUI
        {
            get => _isShowingButtonsInHiddenUI;
            set => this.RaiseAndSetIfChanged(ref _isShowingButtonsInHiddenUI, value);
        }

        private bool _isGalleryOpen;

        public bool IsGalleryOpen
        {
            get => _isGalleryOpen;
            set => this.RaiseAndSetIfChanged(ref _isGalleryOpen, value);
        }

        private bool _isShowingTaskbarProgress = SettingsHelper.Settings.UIProperties.IsTaskbarProgressEnabled;

        public bool IsShowingTaskbarProgress
        {
            get => _isShowingTaskbarProgress;
            set => this.RaiseAndSetIfChanged(ref _isShowingTaskbarProgress, value);
        }

        private bool _isFullscreen = SettingsHelper.Settings.WindowProperties.Fullscreen;

        public bool IsFullscreen
        {
            get => _isFullscreen;
            set => this.RaiseAndSetIfChanged(ref _isFullscreen, value);
        }

        private bool _isTopMost = SettingsHelper.Settings.WindowProperties.TopMost;

        public bool IsTopMost
        {
            get => _isTopMost;
            set => this.RaiseAndSetIfChanged(ref _isTopMost, value);
        }

        private bool _isIncludingSubdirectories = SettingsHelper.Settings.Sorting.IncludeSubDirectories;

        public bool IsIncludingSubdirectories
        {
            get => _isIncludingSubdirectories;
            set => this.RaiseAndSetIfChanged(ref _isIncludingSubdirectories, value);
        }

        private bool _isScrollingEnabled;

        public bool IsScrollingEnabled
        {
            get => _isScrollingEnabled;
            set => this.RaiseAndSetIfChanged(ref _isScrollingEnabled, value);
        }

        private bool _isStretched = SettingsHelper.Settings.ImageScaling.StretchImage;

        public bool IsStretched
        {
            get => _isStretched;
            set
            {
                this.RaiseAndSetIfChanged(ref _isStretched, value);
                SettingsHelper.Settings.ImageScaling.StretchImage = value;
                WindowHelper.SetSize(this);
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
                GetLooping = value
                    ? TranslationHelper.GetTranslation("LoopingEnabled")
                    : TranslationHelper.GetTranslation("LoopingDisabled");
                _ = SettingsHelper.SaveSettingsAsync();
            }
        }

        private bool _isAutoFit = SettingsHelper.Settings.WindowProperties.AutoFit;

        public bool IsAutoFit
        {
            get => _isAutoFit;
            set => this.RaiseAndSetIfChanged(ref _isAutoFit, value);
        }

        private bool _isCtrlToZoomEnabled = SettingsHelper.Settings.Zoom.CtrlZoom;

        public bool IsCtrlToZoomEnabled
        {
            get => _isCtrlToZoomEnabled;
            set
            {
                this.RaiseAndSetIfChanged(ref _isCtrlToZoomEnabled, value);
                SettingsHelper.Settings.Zoom.CtrlZoom = value;
                _ = SettingsHelper.SaveSettingsAsync();
            }
        }

        private bool _isNavigatingInReverse = SettingsHelper.Settings.Zoom.HorizontalReverseScroll;

        public bool IsNavigatingInReverse
        {
            get => _isNavigatingInReverse;
            set
            {
                this.RaiseAndSetIfChanged(ref _isNavigatingInReverse, value);
                SettingsHelper.Settings.Zoom.HorizontalReverseScroll = value;
                _ = SettingsHelper.SaveSettingsAsync();
            }
        }

        private bool _isOpeningLastFileOnStartup = SettingsHelper.Settings.StartUp.OpenLastFile;

        public bool IsOpeningLastFileOnStartup
        {
            get => _isOpeningLastFileOnStartup;
            set
            {
                this.RaiseAndSetIfChanged(ref _isOpeningLastFileOnStartup, value);
                SettingsHelper.Settings.StartUp.OpenLastFile = value;
                _ = SettingsHelper.SaveSettingsAsync();
            }
        }

        private bool _isStayingCentered = SettingsHelper.Settings.WindowProperties.KeepCentered;

        public bool IsStayingCentered
        {
            get => _isStayingCentered;
            set
            {
                this.RaiseAndSetIfChanged(ref _isStayingCentered, value);
                SettingsHelper.Settings.WindowProperties.KeepCentered = value;
                _ = SettingsHelper.SaveSettingsAsync();
            }
        }

        private bool _isFileSavingDialogShown = SettingsHelper.Settings.UIProperties.ShowFileSavingDialog;

        public bool IsFileSavingDialogShown
        {
            get => _isFileSavingDialogShown;
            set
            {
                this.RaiseAndSetIfChanged(ref _isFileSavingDialogShown, value);
                SettingsHelper.Settings.UIProperties.ShowFileSavingDialog = value;
                _ = SettingsHelper.SaveSettingsAsync();
            }
        }

        private bool _isOpeningInSameWindow;

        public bool IsOpeningInSameWindow
        {
            get => _isOpeningInSameWindow;
            set => this.RaiseAndSetIfChanged(ref _isOpeningInSameWindow, value);
        }

        #endregion Booleans

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

        public ImageViewer? ImageViewer;

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

        private double _getSlideshowSpeed = SettingsHelper.Settings.UIProperties.SlideShowTimer;

        public double GetSlideshowSpeed
        {
            get => _getSlideshowSpeed;
            set
            {
                var roundedValue = Math.Round(value, 2);
                this.RaiseAndSetIfChanged(ref _getSlideshowSpeed, roundedValue);
                SettingsHelper.Settings.UIProperties.SlideShowTimer = roundedValue;
            }
        }

        private double _getNavSpeed = SettingsHelper.Settings.UIProperties.NavSpeed;

        public double GetNavSpeed
        {
            get => _getNavSpeed;
            set
            {
                var roundedValue = Math.Round(value, 2);
                this.RaiseAndSetIfChanged(ref _getNavSpeed, roundedValue);
                SettingsHelper.Settings.UIProperties.NavSpeed = roundedValue;
                _ = SettingsHelper.SaveSettingsAsync();
            }
        }

        private double _getZoomSpeed = SettingsHelper.Settings.Zoom.ZoomSpeed;

        public double GetZoomSpeed
        {
            get => _getZoomSpeed;
            set
            {
                var roundedValue = Math.Round(value, 2);
                this.RaiseAndSetIfChanged(ref _getZoomSpeed, roundedValue);
                SettingsHelper.Settings.Zoom.ZoomSpeed = roundedValue;
            }
        }

        #region strings

        private string? _getFlipped;

        public string? GetFlipped
        {
            get => _getFlipped;
            set => this.RaiseAndSetIfChanged(ref _getFlipped, value);
        }

        public string? GetBottomGallery => IsBottomGalleryShown ? HideBottomGallery : ShowBottomGallery;

        private string? _getLooping = SettingsHelper.Settings.UIProperties.Looping
            ? TranslationHelper.GetTranslation("LoopingEnabled")
            : TranslationHelper.GetTranslation("LoopingDisabled");

        public string? GetLooping
        {
            get => _getLooping;
            set => this.RaiseAndSetIfChanged(ref _getLooping, value);
        }

        private string? _getScrolling = SettingsHelper.Settings.Zoom.ScrollEnabled
            ? TranslationHelper.GetTranslation("ScrollingEnabled")
            : TranslationHelper.GetTranslation("ScrollingDisabled");

        public string? GetScrolling
        {
            get => _getScrolling;
            set => this.RaiseAndSetIfChanged(ref _getScrolling, value);
        }

        private string? _getCtrlZoom = SettingsHelper.Settings.Zoom.CtrlZoom
            ? TranslationHelper.GetTranslation("CtrlToZoom")
            : TranslationHelper.GetTranslation("ScrollToZoom");

        public string? GetCtrlZoom
        {
            get => _getCtrlZoom;
            set => this.RaiseAndSetIfChanged(ref _getCtrlZoom, value);
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

        #endregion strings

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

        private Thickness _imageMargin;

        public Thickness ImageMargin
        {
            get => _imageMargin;
            set => this.RaiseAndSetIfChanged(ref _imageMargin, value);
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

        #endregion Services

        #region Methods

        #region Set model and title

        public void SetImageModel(ImageModel imageModel)
        {
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
                        GetOrientation =
                            $"{TranslationHelper.GetTranslation("Rotated")} 180\u00b0, {TranslationHelper.GetTranslation("Flipped")}";
                        break;

                    case EXIFHelper.EXIFOrientation.Rotated270Flipped:
                        RotationAngle = 270;
                        ScaleX = -1;
                        GetOrientation =
                            $"{TranslationHelper.GetTranslation("Rotated")} 270\u00b0, {TranslationHelper.GetTranslation("Flipped")}";
                        break;

                    case EXIFHelper.EXIFOrientation.Rotated90:
                        RotationAngle = 90;
                        ScaleX = 1;
                        GetOrientation = $"{TranslationHelper.GetTranslation("Rotated")} 90\u00b0";
                        break;

                    case EXIFHelper.EXIFOrientation.Rotated90Flipped:
                        RotationAngle = 90;
                        ScaleX = -1;
                        GetOrientation =
                            $"{TranslationHelper.GetTranslation("Rotated")} 90\u00b0, {TranslationHelper.GetTranslation("Flipped")}";
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

                    if (DpiX is 0 && imageModel.ImageType is ImageType.Bitmap or ImageType.AnimatedBitmap)
                    {
                        if (imageModel.Image is Bitmap bmp)
                        {
                            DpiX = bmp?.Dpi.X ?? 0;
                            DpiY = bmp?.Dpi.Y ?? 0;
                        }
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
                    GetTitle = EXIFHelper.GetTitle(profile);
                    GetSubject = profile?.GetValue(ExifTag.XPSubject)?.Value.ToString() ?? string.Empty;
                    GetSoftware = profile?.GetValue(ExifTag.Software)?.Value ?? string.Empty;
                    GetResolutionUnit = EXIFHelper.GetResolutionUnit(profile);
                    GetColorRepresentation = EXIFHelper.GetColorSpace(profile);
                    GetCompression = profile?.GetValue(ExifTag.Compression)?.Value.ToString() ?? string.Empty;
                    GetCompressedBitsPixel = profile?.GetValue(ExifTag.CompressedBitsPerPixel)?.Value.ToString() ??
                                             string.Empty;
                    GetCameraMaker = profile?.GetValue(ExifTag.Make)?.Value ?? string.Empty;
                    GetCameraModel = profile?.GetValue(ExifTag.Model)?.Value ?? string.Empty;
                    GetExposureProgram = EXIFHelper.GetExposureProgram(profile);
                    GetExposureTime = profile?.GetValue(ExifTag.ExposureTime)?.Value.ToString() ?? string.Empty;
                    GetFNumber = profile?.GetValue(ExifTag.FNumber)?.Value.ToString() ?? string.Empty;
                    GetMaxAperture = profile?.GetValue(ExifTag.MaxApertureValue)?.Value.ToString() ?? string.Empty;
                    GetExposureBias = profile?.GetValue(ExifTag.ExposureBiasValue)?.Value.ToString() ?? string.Empty;
                    GetDigitalZoom = profile?.GetValue(ExifTag.DigitalZoomRatio)?.Value.ToString() ?? string.Empty;
                    GetFocalLength35mm = profile?.GetValue(ExifTag.FocalLengthIn35mmFilm)?.Value.ToString() ??
                                         string.Empty;
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

        #endregion Set model and title

        #region LoadPic

        public async Task LoadNextPic(NavigateTo navigateTo)
        {
            if (ImageIterator is null)
            {
                return;
            }

            await ImageIterator.LoadNextPic(navigateTo, this).ConfigureAwait(false);
        }

        public async Task LoadPicAtIndex(int index) => await Task.Run(async () =>
        {
            if (ImageIterator is null)
            {
                return;
            }

            await ImageIterator.LoadPicAtIndex(index, this).ConfigureAwait(false);
            var preloadValue = ImageIterator.PreLoader.Get(index, ImageIterator.Pics);
            ImageChanged?.Invoke(this, preloadValue.ImageModel);
        }).ConfigureAwait(false);

        public async Task LoadPicFromString(string path)
        {
            ImageIterator = new ImageIterator(new FileInfo(path), PlatformService);
            await ImageIterator.LoadPicFromString(path, this).ConfigureAwait(false);
        }

        public async Task LoadPicFromFile(FileInfo fileInfo)
        {
            SetLoadingTitle();
            try
            {
                ImageIterator = new ImageIterator(fileInfo, PlatformService);
                await ImageIterator.LoadPicFromFile(fileInfo, this).ConfigureAwait(false);
            }
            catch (Exception)
            {
                if (ImageIterator is null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => { CurrentView = new StartUpMenu(); });
                }
            }
        }

        #endregion LoadPic

        #region Sorting Order

        private FileListHelper.SortFilesBy _sortOrder;

        public FileListHelper.SortFilesBy SortOrder
        {
            get => _sortOrder;
            set => this.RaiseAndSetIfChanged(ref _sortOrder, value);
        }

        private bool _isAscending = SettingsHelper.Settings.Sorting.Ascending;

        public bool IsAscending
        {
            get => _isAscending;
            set => this.RaiseAndSetIfChanged(ref _isAscending, value);
        }

        #endregion Sorting Order

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

        #endregion Methods

        public MainViewModel(IPlatformSpecificService? platformSpecificService)
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            FunctionsHelper.Vm = this;
            PlatformService = platformSpecificService;

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

            ShowExifWindowCommand = ReactiveCommand.Create(platformSpecificService.ShowExifWindow);
            ShowSettingsWindowCommand = ReactiveCommand.Create(platformSpecificService.ShowSettingsWindow);
            ShowKeybindingsWindowCommand = ReactiveCommand.Create(platformSpecificService.ShowKeybindingsWindow);
            ShowAboutWindowCommand = ReactiveCommand.Create(platformSpecificService.ShowAboutWindow);

            #endregion Window commands

            #region Navigation Commands

            NextCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Next);

            NextButtonCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.NextButton);

            PreviousCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Prev);

            PreviousButtonCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.PrevButton);

            FirstCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.First);

            LastCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Last);

            ReloadCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Reload);

            #endregion Navigation Commands

            #region Sort Commands

            SortFilesByNameCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.SortFilesByName);

            SortFilesByCreationTimeCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.SortFilesByCreationTime);

            SortFilesByLastAccessTimeCommand =
                ReactiveCommand.CreateFromTask(FunctionsHelper.SortFilesByLastAccessTime);

            SortFilesByLastWriteTimeCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.SortFilesByLastWriteTime);

            SortFilesBySizeCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.SortFilesBySize);

            SortFilesByExtensionCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.SortFilesByExtension);

            SortFilesRandomlyCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.SortFilesRandomly);

            SortFilesAscendingCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.SortFilesAscending);

            SortFilesDescendingCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.SortFilesDescending);

            #endregion Sort Commands

            #region Menus

            CloseMenuCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.CloseMenus);

            ToggleFileMenuCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleFileMenu);

            ToggleImageMenuCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleImageMenu);

            ToggleSettingsMenuCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleSettingsMenu);

            ToggleToolsMenuCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleToolsMenu);

            #endregion Menus

            #region Image commands

            RotateLeftCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.RotateLeft);

            RotateRightCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.RotateRight);

            FlipCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Flip);

            ToggleScrollCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleScroll);

            OptimizeImageCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.OptimizeImage);

            #endregion Image commands

            #region File commands

            OpenFileCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Open);

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

            OpenWithCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.OpenWith);

            RenameCommand = ReactiveCommand.Create(() => { });

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

            ShowInFolderCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.OpenInExplorer);

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

            #region Gallery Commands

            ToggleGalleryCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleGallery);

            ToggleBottomGalleryCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.OpenCloseBottomGallery);

            #endregion Gallery Commands

            #region UI Commands

            ToggleUICommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleUI);

            ToggleBottomNavBarCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleBottomToolbar);

            ChangeCtrlZoomCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ChangeCtrlZoom);
            
            ColorPickerCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ColorPicker);

            #endregion UI Commands

            #region Settings commands

            ChangeAutoFitCommand = ReactiveCommand.CreateFromTask(async () => await WindowHelper.ToggleAutoFit(this));

            ChangeTopMostCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.SetTopMost);

            ToggleSubdirectoriesCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleSubdirectories);

            ToggleLoopingCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleLooping);

            #endregion Settings commands
        }

        public MainViewModel()
        {
            // Only use for unit test
        }
    }
}