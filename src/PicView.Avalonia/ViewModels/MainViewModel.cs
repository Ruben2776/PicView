using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Services;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.ProcessHandling;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Media;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.Converters;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
using PicView.Core.Calculations;
using PicView.Core.Gallery;
using ImageViewer = PicView.Avalonia.Views.ImageViewer;

namespace PicView.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    public readonly IPlatformSpecificService? PlatformService;
    
    #region Image
    
    private object? _imageSource;
    
    public object? ImageSource
    {
        get => _imageSource;
        set => this.RaiseAndSetIfChanged(ref _imageSource, value);
    }
    
    private object? _secondaryImageSource;
    
    public object? SecondaryImageSource
    {
        get => _secondaryImageSource;
        set => this.RaiseAndSetIfChanged(ref _secondaryImageSource, value);
    }
    
    private ImageType _imageType;
    
    public ImageType ImageType
    {
        get => _imageType;
        set => this.RaiseAndSetIfChanged(ref _imageType, value);
    }
    
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
    
    #endregion

    #region Gallery
    
    private Thickness _galleryMargin;

    public Thickness GalleryMargin
    {
        get => _galleryMargin;
        set => this.RaiseAndSetIfChanged(ref _galleryMargin, value);
    }
    
    private bool _isGalleryShown = SettingsHelper.Settings.UIProperties.ShowInterface && SettingsHelper.Settings.Gallery.IsBottomGalleryShown;

    public bool IsGalleryShown
    {
        get => _isGalleryShown;
        set => this.RaiseAndSetIfChanged(ref _isGalleryShown, value);
    }

    private bool _isBottomGalleryShownInHiddenUi = SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI;

    public bool IsBottomGalleryShownInHiddenUI
    {
        get => _isBottomGalleryShownInHiddenUi;
        set => this.RaiseAndSetIfChanged(ref _isBottomGalleryShownInHiddenUi, value);
    }

    private GalleryMode _galleryMode;

    public GalleryMode GalleryMode
    {
        get => _galleryMode;
        set => this.RaiseAndSetIfChanged(ref _galleryMode, value);
    }

    private Stretch _galleryStretch;

    public Stretch GalleryStretch
    {
        get => _galleryStretch;
        set => this.RaiseAndSetIfChanged(ref _galleryStretch, value);
    }
    
    private int _selectedGalleryItemIndex;

    public int SelectedGalleryItemIndex
    {
        get => _selectedGalleryItemIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedGalleryItemIndex, value);
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
    
    private double _galleryWidth;
    public double GalleryWidth
    {
        get => _galleryWidth;
        set => this.RaiseAndSetIfChanged(ref _galleryWidth, value);
    }
    public double GalleryHeight
    {
        get
        {
            if (!SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                return 0;
            }
            if (!SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI && !IsInterfaceShown)
            {
                return 0;
            }
            return GetBottomGalleryItemHeight + SizeDefaults.ScrollbarSize;
        }
    }

    private double _getGalleryItemWidth = double.NaN;

    public double GetGalleryItemWidth
    {
        get => _getGalleryItemWidth;
        set => this.RaiseAndSetIfChanged(ref _getGalleryItemWidth, value);
    }
    
    private double _getGalleryItemHeight;
    public double GetGalleryItemHeight
    {
        get
        {
            return GalleryFunctions.IsFullGalleryOpen ? GetFullGalleryItemHeight : GetBottomGalleryItemHeight;
        }
        set
        {
            this.RaiseAndSetIfChanged(ref _getGalleryItemHeight, value);
            if (GalleryFunctions.IsBottomGalleryOpen && !GalleryFunctions.IsFullGalleryOpen)
            {
                GetBottomGalleryItemHeight = value;
            }
            else
            {
                GetFullGalleryItemHeight = value;
            }
        }
    }

    private double _getFullGalleryItemHeight = SettingsHelper.Settings.Gallery.ExpandedGalleryItemSize;

    public double GetFullGalleryItemHeight
    {
        get => _getFullGalleryItemHeight;
        set => this.RaiseAndSetIfChanged(ref _getFullGalleryItemHeight, value);
    }
    
    private double _getBottomGalleryItemHeight = SettingsHelper.Settings.Gallery.BottomGalleryItemSize;
    
    public double GetBottomGalleryItemHeight
    {
        get => _getBottomGalleryItemHeight;
        set => this.RaiseAndSetIfChanged(ref _getBottomGalleryItemHeight, value);
    }
    
    public double MaxFullGalleryItemHeight
    {
        get => GalleryDefaults.MaxFullGalleryItemHeight;
    }
    
    public double MinFullGalleryItemHeight
    {
        get => GalleryDefaults.MinFullGalleryItemHeight;
    }
    public double MaxBottomGalleryItemHeight
    {
        get => GalleryDefaults.MaxBottomGalleryItemHeight;
    }
    
    public double MinBottomGalleryItemHeight
    {
        get => GalleryDefaults.MinBottomGalleryItemHeight;
    }

    #region Gallery Stretch IsChecked
    
    private bool _isUniformBottomChecked;
    public bool IsUniformBottomChecked
    {
        get => _isUniformBottomChecked;
        set => this.RaiseAndSetIfChanged(ref _isUniformBottomChecked, value);
    }
    
    private bool _isUniformFullChecked;
    public bool IsUniformFullChecked
    {
        get => _isUniformFullChecked;
        set => this.RaiseAndSetIfChanged(ref _isUniformFullChecked, value);
    }

    private bool _isUniformMenuChecked;
    public bool IsUniformMenuChecked
    {
        get => _isUniformMenuChecked;
        set => this.RaiseAndSetIfChanged(ref _isUniformMenuChecked, value);
    }
    
    private bool _isUniformToFillBottomChecked;
    public bool IsUniformToFillBottomChecked
    {
        get => _isUniformToFillBottomChecked;
        set => this.RaiseAndSetIfChanged(ref _isUniformToFillBottomChecked, value);
    }
    
    private bool _isUniformToFillFullChecked;
    public bool IsUniformToFillFullChecked
    {
        get => _isUniformToFillFullChecked;
        set => this.RaiseAndSetIfChanged(ref _isUniformToFillFullChecked, value);
    }

    private bool _isUniformToFillMenuMenuChecked;
    public bool IsUniformToFillMenuChecked
    {
        get => _isUniformToFillMenuMenuChecked;
        set => this.RaiseAndSetIfChanged(ref _isUniformToFillMenuMenuChecked, value);
    }
    
    private bool _isFillBottomChecked;
    public bool IsFillBottomChecked
    {
        get => _isFillBottomChecked;
        set => this.RaiseAndSetIfChanged(ref _isFillBottomChecked, value);
    }
    
    private bool _isFillFullChecked;
    public bool IsFillFullChecked
    {
        get => _isFillFullChecked;
        set => this.RaiseAndSetIfChanged(ref _isFillFullChecked, value);
    }

    private bool _isFillMenuMenuChecked;
    public bool IsFillMenuChecked
    {
        get => _isFillMenuMenuChecked;
        set => this.RaiseAndSetIfChanged(ref _isFillMenuMenuChecked, value);
    }
    
    private bool _isNoneBottomChecked;
    public bool IsNoneBottomChecked
    {
        get => _isNoneBottomChecked;
        set => this.RaiseAndSetIfChanged(ref _isNoneBottomChecked, value);
    }
    
    private bool _isNoneFullChecked;
    public bool IsNoneFullChecked
    {
        get => _isNoneFullChecked;
        set => this.RaiseAndSetIfChanged(ref _isNoneFullChecked, value);
    }

    private bool _isNoneMenuChecked;
    public bool IsNoneMenuChecked
    {
        get => _isNoneMenuChecked;
        set => this.RaiseAndSetIfChanged(ref _isNoneMenuChecked, value);
    }
    
    private bool _isSquareBottomChecked;
    public bool IsSquareBottomChecked
    {
        get => _isSquareBottomChecked;
        set => this.RaiseAndSetIfChanged(ref _isSquareBottomChecked, value);
    }
    
    private bool _isSquareFullChecked;
    public bool IsSquareFullChecked
    {
        get => _isSquareFullChecked;
        set => this.RaiseAndSetIfChanged(ref _isSquareFullChecked, value);
    }

    private bool _isSquareMenuChecked;
    public bool IsSquareMenuChecked
    {
        get => _isSquareMenuChecked;
        set => this.RaiseAndSetIfChanged(ref _isSquareMenuChecked, value);
    }
    
    private bool _isFillSquareBottomChecked;
    public bool IsFillSquareBottomChecked
    {
        get => _isFillSquareBottomChecked;
        set => this.RaiseAndSetIfChanged(ref _isFillSquareBottomChecked, value);
    }
    
    private bool _isFillSquareFullChecked;
    public bool IsFillSquareFullChecked
    {
        get => _isFillSquareFullChecked;
        set => this.RaiseAndSetIfChanged(ref _isFillSquareFullChecked, value);
    }

    private bool _isFillSquareMenuChecked;
    public bool IsFillSquareMenuChecked
    {
        get => _isFillSquareMenuChecked;
        set => this.RaiseAndSetIfChanged(ref _isFillSquareMenuChecked, value);
    }
    #endregion

    #endregion Gallery

    #region Commands

    public ReactiveCommand<Unit, Unit>? ExitCommand { get; }
    public ReactiveCommand<Unit, Unit>? MinimizeCommand { get; }
    public ReactiveCommand<Unit, Unit>? MaximizeCommand { get; }
    
    public ReactiveCommand<Unit, Unit>? ToggleFullscreenCommand { get; }

    public ReactiveCommand<Unit, Unit>? NextCommand { get; }
    public ReactiveCommand<Unit, Unit>? NextButtonCommand { get; }
    public ReactiveCommand<Unit, Unit>? NextArrowButtonCommand { get; }
    public ReactiveCommand<Unit, Unit>? PreviousCommand { get; }
    public ReactiveCommand<Unit, Unit>? PreviousButtonCommand { get; }
    public ReactiveCommand<Unit, Unit>? PreviousArrowButtonCommand { get; }
    public ReactiveCommand<Unit, Unit>? NextFolderCommand { get; }
    public ReactiveCommand<Unit, Unit>? PreviousFolderCommand { get; }
    public ReactiveCommand<Unit, Unit>? FirstCommand { get; }
    public ReactiveCommand<Unit, Unit>? LastCommand { get; }
    public ReactiveCommand<Unit, Unit>? OpenFileCommand { get; }
    public ReactiveCommand<Unit, Unit>? SaveFileCommand { get; }
    public ReactiveCommand<Unit, Unit>? OpenLastFileCommand { get; }
    public ReactiveCommand<Unit, Unit>? PasteCommand { get; }
    public ReactiveCommand<string, Unit>? CopyFileCommand { get; }
    public ReactiveCommand<string, Unit>? CopyFilePathCommand { get; }
    public ReactiveCommand<string, Unit>? FilePropertiesCommand { get; }
    public ReactiveCommand<string, Unit>? CopyImageCommand { get; }
    public ReactiveCommand<string, Unit>? CutCommand { get; }
    public ReactiveCommand<Unit, Unit>? ReloadCommand { get; }
    public ReactiveCommand<string, Unit>? PrintCommand { get; }
    public ReactiveCommand<string, Unit>? DeleteFileCommand { get; }
    public ReactiveCommand<string, Unit>? RecycleFileCommand { get; }
    public ReactiveCommand<Unit, Unit>? CloseMenuCommand { get; }
    public ReactiveCommand<Unit, Unit>? ToggleFileMenuCommand { get; }
    public ReactiveCommand<Unit, Unit>? ToggleImageMenuCommand { get; }
    public ReactiveCommand<Unit, Unit>? ToggleSettingsMenuCommand { get; }
    public ReactiveCommand<Unit, Unit>? ToggleToolsMenuCommand { get; }
    public ReactiveCommand<string, Unit>? LocateOnDiskCommand { get; }
    public ReactiveCommand<string, Unit>? OpenWithCommand { get; }
    public ReactiveCommand<Unit, Unit>? RenameCommand { get; }
    public ReactiveCommand<Unit, Unit>? NewWindowCommand { get; }
    public ReactiveCommand<string, Unit>? DuplicateFileCommand { get; }
    public ReactiveCommand<Unit, Unit>? ToggleLoopingCommand { get; }
    public ReactiveCommand<Unit, Unit>? RotateLeftCommand { get; }
    public ReactiveCommand<Unit, Unit>? RotateRightCommand { get; }
    public ReactiveCommand<Unit, Unit>? FlipCommand { get; }
    public ReactiveCommand<Unit, Unit>? StretchCommand { get; }
    public ReactiveCommand<Unit, Unit>? CropCommand { get; }
    public ReactiveCommand<Unit, Unit>? ChangeAutoFitCommand { get; }
    public ReactiveCommand<Unit, Unit>? ChangeTopMostCommand { get; }
    public ReactiveCommand<Unit, Unit>? ChangeCtrlZoomCommand { get; }
    public ReactiveCommand<Unit, Unit>? ToggleUICommand { get; }
    public ReactiveCommand<Unit, Unit>? ChangeBackgroundCommand { get; }
    public ReactiveCommand<Unit, Unit>? ToggleBottomNavBarCommand { get; }
    public ReactiveCommand<Unit, Unit>? ToggleBottomGalleryShownInHiddenUICommand { get; }
    public ReactiveCommand<Unit, Unit>? ShowExifWindowCommand { get; }
    public ReactiveCommand<Unit, Unit>? ShowAboutWindowCommand { get; }
    public ReactiveCommand<Unit, Unit>? ShowSettingsWindowCommand { get; }
    public ReactiveCommand<Unit, Unit>? ShowKeybindingsWindowCommand { get; }

    public ReactiveCommand<Unit, Unit>? SetExifRating0Command { get; }
    public ReactiveCommand<Unit, Unit>? SetExifRating1Command { get; }
    public ReactiveCommand<Unit, Unit>? SetExifRating2Command { get; }
    public ReactiveCommand<Unit, Unit>? SetExifRating3Command { get; }
    public ReactiveCommand<Unit, Unit>? SetExifRating4Command { get; }
    public ReactiveCommand<Unit, Unit>? SetExifRating5Command { get; }
    public ReactiveCommand<Unit, Unit>? OpenGoogleLinkCommand { get; }
    public ReactiveCommand<Unit, Unit>? OpenBingLinkCommand { get; }

    public ReactiveCommand<Unit, Unit>? OptimizeImageCommand { get; }
    public ReactiveCommand<int, Unit>? ResizeCommand { get; }
    public ReactiveCommand<int, Unit>? ConvertCommand { get; }

    public ReactiveCommand<Unit, Unit>? SortFilesByNameCommand { get; }
    public ReactiveCommand<Unit, Unit>? SortFilesBySizeCommand { get; }
    public ReactiveCommand<Unit, Unit>? SortFilesByExtensionCommand { get; }
    public ReactiveCommand<Unit, Unit>? SortFilesByCreationTimeCommand { get; }
    public ReactiveCommand<Unit, Unit>? SortFilesByLastAccessTimeCommand { get; }
    public ReactiveCommand<Unit, Unit>? SortFilesRandomlyCommand { get; }
    public ReactiveCommand<Unit, Unit>? SortFilesAscendingCommand { get; }
    public ReactiveCommand<Unit, Unit>? SortFilesDescendingCommand { get; }

    public ReactiveCommand<Unit, Unit>? ToggleGalleryCommand { get; }
    public ReactiveCommand<Unit, Unit>? ToggleBottomGalleryCommand { get; }
    public ReactiveCommand<Unit, Unit>? CloseGalleryCommand { get; }

    public ReactiveCommand<Unit, Unit>? ToggleScrollCommand { get; }

    public ReactiveCommand<Unit, Unit>? ToggleSubdirectoriesCommand { get; }

    public ReactiveCommand<Unit, Unit>? ColorPickerCommand { get; }

    public ReactiveCommand<Unit, Unit>? SlideshowCommand { get; }
    
    public ReactiveCommand<string, Unit>? SetAsWallpaperCommand { get; }
    
    public ReactiveCommand<string, Unit>? SetAsLockScreenCommand { get; }
    
    public ReactiveCommand<string, Unit>? GalleryItemStretchCommand { get; }

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

    private bool _isBottomToolbarShownSetting = SettingsHelper.Settings.UIProperties.ShowBottomNavBar;

    public bool IsBottomToolbarShownSetting
    {
        get => _isBottomToolbarShownSetting;
        set => this.RaiseAndSetIfChanged(ref _isBottomToolbarShownSetting, value);
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
        set => this.RaiseAndSetIfChanged(ref _isLooping, value);
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

    public double WindowMinSize
    {
        get
        {
            return SizeDefaults.WindowMinSize;
        }
    }
    
    private int _titlebarHeight;
    public int TitlebarHeight
    {
        set => this.RaiseAndSetIfChanged(ref _titlebarHeight, value);
        get
        {
            if (IsFullscreen || !IsInterfaceShown || !IsTopToolbarShown)
            {
                return 0;
            }

            return SizeDefaults.TitlebarHeight;
        }
    }
    private int _bottombarHeight;
    public int BottombarHeight         
    {
        set => this.RaiseAndSetIfChanged(ref _bottombarHeight, value);
        get
        {
            if (IsFullscreen || !IsInterfaceShown || !IsBottomToolbarShown)
            {
                return 0;
            }
            return SizeDefaults.BottombarHeight;
        }
    }

    private int _scaleX = 1;

    // Used to flip the flip button
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

    private string? _getBottomGallery;

    public string? GetBottomGallery
    {
        get => _getBottomGallery;
        set => this.RaiseAndSetIfChanged(ref _getBottomGallery, value);
    }

    private string? _getLooping;

    public string? GetLooping
    {
        get => _getLooping;
        set => this.RaiseAndSetIfChanged(ref _getLooping, value);
    }

    private string? _getScrolling;

    public string? GetScrolling
    {
        get => _getScrolling;
        set => this.RaiseAndSetIfChanged(ref _getScrolling, value);
    }

    private string? _getCtrlZoom;

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

    private string? _getFocalLength35Mm;

    public string? GetFocalLength35Mm
    {
        get => _getFocalLength35Mm;
        set => this.RaiseAndSetIfChanged(ref _getFocalLength35Mm, value);
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

    public ImageIterator? ImageIterator;

    #endregion Services

    #region Methods

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
        SetTitleHelper.SetLoadingTitle(this);
        var success = await ConversionHelper.ResizeImageByPercentage(FileInfo, percentage);
        if (success)
        {
            ImageIterator?.PreLoader.Remove(ImageIterator.Index, ImageIterator.Pics);
            await ImageIterator?.IterateToIndex(ImageIterator.Index);
        }
        else
        {
            SetTitleHelper.SetTitle(this);
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
            await NavigationHelper.LoadPicFromStringAsync(newPath, this);
        }

        if (ImageIterator is not null)
        {
            ImageIterator.IsFileBeingRenamed = false;
        }
    }
    
    private async Task CopyFileTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        if (PlatformService is null)
        {
            return;
        }
        await ClipboardHelper.CopyFileToClipboard(path);
    }
    
    private async Task CopyFilePathTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        if (PlatformService is null)
        {
            return;
        }
        await ClipboardHelper.CopyTextToClipboard(path);
    }
    
    private async Task CopyImageTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        if (PlatformService is null)
        {
            return;
        }
        await ClipboardHelper.CopyImageToClipboard(path);
    }
    
    private async Task CopyBase64Task(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        if (PlatformService is null)
        {
            return;
        }
        await ClipboardHelper.CopyBase64ToClipboard(path);
    }
    
    private async Task CutFileTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        if (PlatformService is null)
        {
            return;
        }
        await ClipboardHelper.CutFile(path);
    }
    
    private async Task DeleteFileTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        await Task.Run(() =>
        {
            FileDeletionHelper.DeleteFileWithErrorMsg(path, recycle: false);
        });
    }
    
    private async Task RecycleFileTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        await Task.Run(() =>
        {
            FileDeletionHelper.DeleteFileWithErrorMsg(path, recycle: true);
        });
    }
    
    private async Task DuplicateFileTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (Path.GetFileName(path) == FileInfo.FullName)
        {
            await FunctionsHelper.DuplicateFile();
        }
        else
        {
            await Task.Run(() =>
            {
                FileHelper.DuplicateAndReturnFileName(path);
            });
        }
    }
    
    private async Task ShowFilePropertiesTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        if (PlatformService is null)
        {
            return;
        }
        await Task.Run(() =>
        {
            PlatformService.ShowFileProperties(path);
        });
    }

    private async Task PrintTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        if (PlatformService is null)
        {
            return;
        }
        await Task.Run(() =>
        {
            PlatformService?.Print(path);
        });
    }
    
    private async Task OpenWithTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        if (PlatformService is null)
        {
            return;
        }
        await Task.Run(() =>
        {
            PlatformService?.OpenWith(path);
        });
    }
    
    private async Task LocateOnDiskTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        if (PlatformService is null)
        {
            return;
        }
        await Task.Run(() =>
        {
            PlatformService?.LocateOnDisk(path);
        });
    }
    
    private async Task SetAsWallpaperTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        if (PlatformService is null)
        {
            return;
        }
        await Task.Run(() =>
        {
            PlatformService?.SetAsWallpaper(path, 4);
        });
    }
    
    private async Task SetAsLockScreenTask(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        if (PlatformService is null)
        {
            return;
        }
        await Task.Run(() =>
        {
            PlatformService?.SetAsLockScreen(path);
        });
    }

    public async Task GalleryItemStretchTask(string value)
    {
        if (value.Equals("Square", StringComparison.OrdinalIgnoreCase))
        {
            if (GalleryFunctions.IsFullGalleryOpen)
            {
                await GalleryStretchMode.ChangeFullGalleryStretchSquare(this);
            }
            else
            {
                await GalleryStretchMode.ChangeBottomGalleryStretchSquare(this);
            }
            return;
        }
        
        if (value.Equals("FillSquare", StringComparison.OrdinalIgnoreCase))
        {
            if (GalleryFunctions.IsFullGalleryOpen)
            {
                await GalleryStretchMode.ChangeFullGalleryStretchSquareFill(this);
            }
            else
            {
                await GalleryStretchMode.ChangeBottomGalleryStretchSquareFill(this);
            }
            return;
        }

        if (Enum.TryParse<Stretch>(value, out var stretch))
        {
            if (GalleryFunctions.IsFullGalleryOpen)
            {
                await GalleryStretchMode.ChangeFullGalleryItemStretch(this, stretch);
            }
            else
            {
                await GalleryStretchMode.ChangeBottomGalleryItemStretch(this, stretch);
            }
        }
    }

    #endregion Methods

    public MainViewModel(IPlatformSpecificService? platformSpecificService)
    {
        FunctionsHelper.Vm = this;
        PlatformService = platformSpecificService;

        #region Window commands

        ExitCommand = ReactiveCommand.CreateFromTask(WindowHelper.Close);
        MinimizeCommand = ReactiveCommand.CreateFromTask(WindowHelper.Minimize);
        MaximizeCommand = ReactiveCommand.CreateFromTask(WindowHelper.MaximizeRestore);
        ToggleFullscreenCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Fullscreen);
        NewWindowCommand = ReactiveCommand.Create(ProcessHelper.StartNewProcess);

        ShowExifWindowCommand = ReactiveCommand.Create(platformSpecificService.ShowExifWindow);
        ShowSettingsWindowCommand = ReactiveCommand.Create(platformSpecificService.ShowSettingsWindow);
        ShowKeybindingsWindowCommand = ReactiveCommand.Create(platformSpecificService.ShowKeybindingsWindow);
        ShowAboutWindowCommand = ReactiveCommand.Create(platformSpecificService.ShowAboutWindow);

        #endregion Window commands

        #region Navigation Commands

        NextCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Next);

        NextButtonCommand = ReactiveCommand.CreateFromTask(async () =>
        {
           await NavigationHelper.IterateButton(next:true, arrow: false, vm: this);
        });
        
        NextArrowButtonCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await NavigationHelper.IterateButton(next:true, arrow: true, vm: this);
        });
        
        NextFolderCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.NextFolder);

        PreviousCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Prev);

        PreviousButtonCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await NavigationHelper.IterateButton(next:false, arrow: false, vm: this);
        });
        
        PreviousArrowButtonCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await NavigationHelper.IterateButton(next:false, arrow: true, vm: this);
        });
        
        PreviousFolderCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.PrevFolder);

        FirstCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.First);

        LastCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Last);

        ReloadCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Reload);

        #endregion Navigation Commands

        #region Sort Commands

        SortFilesByNameCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.SortFilesByName);

        SortFilesByCreationTimeCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.SortFilesByCreationTime);

        SortFilesByLastAccessTimeCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.SortFilesByLastAccessTime);

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

        StretchCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Stretch);

        CropCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Crop);

        ToggleScrollCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleScroll);

        OptimizeImageCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.OptimizeImage);
        
        ChangeBackgroundCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ChangeBackground);

        #endregion Image commands

        #region File commands

        OpenFileCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Open, Observable.Return(true));

        OpenLastFileCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.OpenLastFile);

        SaveFileCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Save);

        CopyFileCommand = ReactiveCommand.CreateFromTask<string>(CopyFileTask);

        CopyFilePathCommand = ReactiveCommand.CreateFromTask<string>(CopyFilePathTask);
        
        FilePropertiesCommand = ReactiveCommand.CreateFromTask<string>(ShowFilePropertiesTask);

        CopyImageCommand = ReactiveCommand.CreateFromTask<string>(CopyImageTask);

        CutCommand = ReactiveCommand.CreateFromTask<string>(CutFileTask);

        PasteCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Paste);

        OpenWithCommand = ReactiveCommand.CreateFromTask<string>(OpenWithTask);

        RenameCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Rename);

        ResizeCommand = ReactiveCommand.CreateFromTask<int>(ResizeImageByPercentage);
        ConvertCommand = ReactiveCommand.CreateFromTask<int>(ConvertFileExtension);

        DuplicateFileCommand = ReactiveCommand.CreateFromTask<string>(DuplicateFileTask);

        PrintCommand = ReactiveCommand.CreateFromTask<string>(PrintTask);    

        DeleteFileCommand = ReactiveCommand.CreateFromTask<string>(DeleteFileTask);

        RecycleFileCommand = ReactiveCommand.CreateFromTask<string>(RecycleFileTask);

        LocateOnDiskCommand = ReactiveCommand.CreateFromTask<string>(LocateOnDiskTask);
        
        SetAsWallpaperCommand = ReactiveCommand.CreateFromTask<string>(SetAsWallpaperTask);
        SetAsLockScreenCommand = ReactiveCommand.CreateFromTask<string>(SetAsLockScreenTask);

        #endregion File commands

        #region EXIF commands

        SetExifRating0Command = ReactiveCommand.CreateFromTask(FunctionsHelper.Set0Star);
        SetExifRating1Command = ReactiveCommand.CreateFromTask(FunctionsHelper.Set1Star);
        SetExifRating2Command = ReactiveCommand.CreateFromTask(FunctionsHelper.Set2Star);
        SetExifRating3Command = ReactiveCommand.CreateFromTask(FunctionsHelper.Set3Star);
        SetExifRating4Command = ReactiveCommand.CreateFromTask(FunctionsHelper.Set4Star);
        SetExifRating5Command = ReactiveCommand.CreateFromTask(FunctionsHelper.Set5Star);

        OpenGoogleLinkCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.OpenGoogleMaps);
        OpenBingLinkCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.OpenBingMaps);

        #endregion EXIF commands

        #region Gallery Commands

        ToggleGalleryCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleGallery);

        ToggleBottomGalleryCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.OpenCloseBottomGallery);
        
        CloseGalleryCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.CloseGallery);
        
        GalleryItemStretchCommand = ReactiveCommand.CreateFromTask<string>(GalleryItemStretchTask);

        #endregion Gallery Commands

        #region UI Commands

        ToggleUICommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleInterface);

        ToggleBottomNavBarCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ToggleBottomToolbar);
        
        ToggleBottomGalleryShownInHiddenUICommand = ReactiveCommand.CreateFromTask(async() =>
        {
            await HideInterfaceLogic.ToggleBottomGalleryShownInHiddenUI(this);
        });

        ChangeCtrlZoomCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ChangeCtrlZoom);
        
        ColorPickerCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.ColorPicker);
        
        SlideshowCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.Slideshow);

        #endregion UI Commands

        #region Settings commands

        ChangeAutoFitCommand = ReactiveCommand.CreateFromTask(FunctionsHelper.AutoFitWindow);

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
