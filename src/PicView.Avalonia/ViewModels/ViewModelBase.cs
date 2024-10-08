using PicView.Core.Extensions;
using PicView.Core.Localization;
using ReactiveUI;

namespace PicView.Avalonia.ViewModels;

public class ViewModelBase : ReactiveObject
{
    #region Localization

    public void UpdateLanguage()
    {
        File = TranslationHelper.Translation.File;
        SelectFile = TranslationHelper.Translation.OpenFileDialog;
        OpenLastFile = TranslationHelper.Translation.OpenLastFile;
        FilePaste = TranslationHelper.Translation.FilePaste;
        Copy = TranslationHelper.Translation.Copy;
        Reload = TranslationHelper.Translation.Reload;
        Print = TranslationHelper.Translation.Print;
        DeleteFile = TranslationHelper.Translation.DeleteFile;
        Save = TranslationHelper.Translation.Save;
        CopyFile = TranslationHelper.Translation.CopyFile;
        NewWindow = TranslationHelper.Translation.NewWindow;
        Close = TranslationHelper.Translation.Close;
        Open = TranslationHelper.Translation.Open;
        OpenFileDialog = TranslationHelper.Translation.OpenFileDialog;
        ShowInFolder = TranslationHelper.Translation.ShowInFolder;
        OpenWith = TranslationHelper.Translation.OpenWith;
        RenameFile = TranslationHelper.Translation.RenameFile;
        DuplicateFile = TranslationHelper.Translation.DuplicateFile;
        RotateLeft = TranslationHelper.Translation.RotateLeft;
        RotateRight = TranslationHelper.Translation.RotateRight;
        Flip = TranslationHelper.Translation.Flip;
        UnFlip = TranslationHelper.Translation.Unflip;
        ShowBottomGallery = TranslationHelper.Translation.ShowBottomGallery;
        HideBottomGallery = TranslationHelper.Translation.HideBottomGallery;
        AutoFitWindow = TranslationHelper.Translation.AutoFitWindow;
        Stretch = TranslationHelper.Translation.Stretch;
        Crop = TranslationHelper.Translation.Crop;
        ResizeImage = TranslationHelper.Translation.ResizeImage;
        GoToImageAtSpecifiedIndex = TranslationHelper.Translation.GoToImageAtSpecifiedIndex;
        ToggleScroll = TranslationHelper.Translation.ToggleScroll;
        ScrollEnabled = TranslationHelper.Translation.ScrollingEnabled;
        ScrollDisabled = TranslationHelper.Translation.ScrollingDisabled;
        ScrollDirection = TranslationHelper.Translation.ScrollDirection;
        Reverse = TranslationHelper.Translation.Reverse;
        Forward = TranslationHelper.Translation.Forward;
        Slideshow = TranslationHelper.Translation.Slideshow;
        Settings = TranslationHelper.Translation.Settings;
        AboutWindow = TranslationHelper.Translation.InfoWindow;
        ImageInfo = TranslationHelper.Translation.ImageInfo;
        About = TranslationHelper.Translation.About;
        ShowAllSettingsWindow = TranslationHelper.Translation.ShowAllSettingsWindow;
        StayTopMost = TranslationHelper.Translation.StayTopMost;
        SearchSubdirectory = TranslationHelper.Translation.SearchSubdirectory;
        ToggleLooping = TranslationHelper.Translation.ToggleLooping;
        HideShowInterface = TranslationHelper.Translation.HideShowInterface;
        ApplicationShortcuts = TranslationHelper.Translation.ApplicationShortcuts;
        BatchResize = TranslationHelper.Translation.BatchResize;
        Effects = TranslationHelper.Translation.Effects;
        EffectsTooltip = TranslationHelper.Translation.EffectsTooltip;
        FileProperties = TranslationHelper.Translation.FileProperties;
        OptimizeImage = TranslationHelper.Translation.OptimizeImage;
        ImageInfo = TranslationHelper.Translation.ImageInfo;
        FileName = TranslationHelper.Translation.FileName;
        FileSize = TranslationHelper.Translation.FileSize;
        Folder = TranslationHelper.Translation.Folder;
        FullPath = TranslationHelper.Translation.FullPath;
        Created = TranslationHelper.Translation.Created;
        Modified = TranslationHelper.Translation.Modified;
        LastAccessTime = TranslationHelper.Translation.LastAccessTime;
        ConvertTo = TranslationHelper.Translation.ConvertTo;
        NoConversion = TranslationHelper.Translation.NoConversion;
        Resize = TranslationHelper.Translation.Resize;
        NoResize = TranslationHelper.Translation.NoResize;
        Apply = TranslationHelper.Translation.Apply;
        Cancel = TranslationHelper.Translation.Cancel;
        BitDepth = TranslationHelper.Translation.BitDepth;
        AspectRatio = TranslationHelper.Translation.AspectRatio;
        Width = TranslationHelper.Translation.Width;
        Height = TranslationHelper.Translation.Height;
        SizeMp = TranslationHelper.Translation.SizeMp;
        Resolution = TranslationHelper.Translation.Resolution;
        PrintSizeIn = TranslationHelper.Translation.PrintSizeIn;
        PrintSizeCm = TranslationHelper.Translation.PrintSizeCm;
        Centimeters = TranslationHelper.Translation.Centimeters;
        Inches = TranslationHelper.Translation.Inches;
        SizeTooltip = TranslationHelper.Translation.SizeTooltip;
        Latitude = TranslationHelper.Translation.Latitude;
        Longitude = TranslationHelper.Translation.Longitude;
        Altitude = TranslationHelper.Translation.Altitude;
        Authors = TranslationHelper.Translation.Authors;
        DateTaken = TranslationHelper.Translation.DateTaken;
        Copyright = TranslationHelper.Translation.Copyright;
        ResolutionUnit = TranslationHelper.Translation.ResolutionUnit;
        ColorRepresentation = TranslationHelper.Translation.ColorRepresentation;
        CompressedBitsPixel = TranslationHelper.Translation.CompressedBitsPixel;
        Compression = TranslationHelper.Translation.Compression;
        ExposureTime = TranslationHelper.Translation.ExposureTime;
        XPTitle = TranslationHelper.Translation.Title;
        Subject = TranslationHelper.Translation.Subject;
        Software = TranslationHelper.Translation.Software;
        CameraMaker = TranslationHelper.Translation.CameraMaker;
        CameraModel = TranslationHelper.Translation.CameraModel;
        FocalLength = TranslationHelper.Translation.FocalLength;
        Fnumber = TranslationHelper.Translation.FNumber;
        Fstop = TranslationHelper.Translation.Fstop;
        MaxAperture = TranslationHelper.Translation.MaxAperture;
        ExposureBias = TranslationHelper.Translation.ExposureBias;
        ExposureProgram = TranslationHelper.Translation.ExposureProgram;
        DigitalZoom = TranslationHelper.Translation.DigitalZoom;
        ISOSpeed = TranslationHelper.Translation.ISOSpeed;
        FocalLength35mm = TranslationHelper.Translation.FocalLength35mm;
        MeteringMode = TranslationHelper.Translation.MeteringMode;
        Contrast = TranslationHelper.Translation.Contrast;
        Saturation = TranslationHelper.Translation.Saturation;
        Sharpness = TranslationHelper.Translation.Sharpness;
        WhiteBalance = TranslationHelper.Translation.WhiteBalance;
        FlashEnergy = TranslationHelper.Translation.FlashEnergy;
        FlashMode = TranslationHelper.Translation.FlashMode;
        LightSource = TranslationHelper.Translation.LightSource;
        Brightness = TranslationHelper.Translation.Brightness;
        PhotometricInterpretation = TranslationHelper.Translation.PhotometricInterpretation;
        Orientation = TranslationHelper.Translation.Orientation;
        ExifVersion = TranslationHelper.Translation.ExifVersion;
        LensMaker = TranslationHelper.Translation.LensMaker;
        LensModel = TranslationHelper.Translation.LensModel;
        SortFilesBy = TranslationHelper.Translation.SortFilesBy;
        FileExtension = TranslationHelper.Translation.FileExtension;
        CreationTime = TranslationHelper.Translation.CreationTime;
        Random = TranslationHelper.Translation.Random;
        Ascending = TranslationHelper.Translation.Ascending;
        Descending = TranslationHelper.Translation.Descending;
        RecentFiles = TranslationHelper.Translation.RecentFiles;
        SetAsWallpaper = TranslationHelper.Translation.SetAsWallpaper;
        SetAsLockScreenImage = TranslationHelper.Translation.SetAsLockScreenImage;
        ImageTxt = TranslationHelper.Translation.Image;
        CopyImage = TranslationHelper.Translation.CopyImage;
        FileCopyPath = TranslationHelper.Translation.FileCopyPath;
        FileCut = TranslationHelper.Translation.FileCut;
        CtrlToZoom = TranslationHelper.Translation.CtrlToZoom;
        ScrollToZoom = TranslationHelper.Translation.ScrollToZoom;
        GeneralSettings = TranslationHelper.Translation.GeneralSettings;
        Appearance = TranslationHelper.Translation.Appearance;
        Language = TranslationHelper.Translation.Language;
        MouseWheel = TranslationHelper.Translation.MouseWheel;
        MiscSettings = TranslationHelper.Translation.MiscSettings;
        StayCentered = TranslationHelper.Translation.StayCentered;
        ShowFileSavingDialog = TranslationHelper.Translation.ShowFileSavingDialog;
        OpenInSameWindow = TranslationHelper.Translation.OpenInSameWindow;
        ApplicationStartup = TranslationHelper.Translation.ApplicationStartup;
        None = TranslationHelper.Translation.None;
        AdjustTimingForSlideshow = TranslationHelper.Translation.AdjustTimingForSlideshow;
        AdjustTimingForZoom = TranslationHelper.Translation.AdjustTimingForZoom;
        AdjustNavSpeed = TranslationHelper.Translation.AdjustNavSpeed;
        SecAbbreviation = TranslationHelper.Translation.SecAbbreviation;
        ResetButtonText = TranslationHelper.Translation.ResetButtonText;
        ShowBottomToolbar = TranslationHelper.Translation.ShowBottomToolbar;
        ShowBottomGalleryWhenUiIsHidden = TranslationHelper.Translation.ShowBottomGalleryWhenUiIsHidden;
        ChangeKeybindingTooltip = TranslationHelper.Translation.ChangeKeybindingTooltip;
        ToggleTaskbarProgress = TranslationHelper.Translation.ToggleTaskbarProgress;
        ChangeKeybindingText = TranslationHelper.Translation.ChangeKeybindingText;
        Navigation = TranslationHelper.Translation.Navigation;
        NextImage = TranslationHelper.Translation.NextImage;
        PrevImage = TranslationHelper.Translation.PrevImage;
        LastImage = TranslationHelper.Translation.LastImage;
        FirstImage = TranslationHelper.Translation.FirstImage;
        NextFolder = TranslationHelper.Translation.NextFolder;
        PrevFolder = TranslationHelper.Translation.PrevFolder;
        SelectGalleryThumb = TranslationHelper.Translation.SelectGalleryThumb;
        ScrollAndRotate = TranslationHelper.Translation.ScrollAndRotate;
        ScrollUp = TranslationHelper.Translation.ScrollUp;
        ScrollDown = TranslationHelper.Translation.ScrollDown;
        ScrollToTop = TranslationHelper.Translation.ScrollToTop;
        ScrollToBottom = TranslationHelper.Translation.ScrollToBottom;
        Zoom = TranslationHelper.Translation.Zoom;
        ZoomIn = TranslationHelper.Translation.ZoomIn;
        ZoomOut = TranslationHelper.Translation.ZoomOut;
        Pan = TranslationHelper.Translation.Pan;
        ResetZoom = TranslationHelper.Translation.ResetZoom;
        ImageControl = TranslationHelper.Translation.ImageControl;
        ChangeBackground = TranslationHelper.Translation.ChangeBackground;
        InterfaceConfiguration = TranslationHelper.Translation.InterfaceConfiguration;
        FileManagement = TranslationHelper.Translation.FileManagement;
        ToggleFullscreen = TranslationHelper.Translation.ToggleFullscreen;
        FullscreenTxt = TranslationHelper.Translation.Fullscreen;
        ShowImageGallery = TranslationHelper.Translation.ShowImageGallery;
        WindowManagement = TranslationHelper.Translation.WindowManagement;
        CenterWindow = TranslationHelper.Translation.CenterWindow;
        WindowScaling = TranslationHelper.Translation.WindowScaling;
        NormalWindow = TranslationHelper.Translation.NormalWindow;
        SetStarRating = TranslationHelper.Translation.SetStarRating;
        _1Star = TranslationHelper.Translation._1Star;
        _2Star = TranslationHelper.Translation._2Star;
        _3Star = TranslationHelper.Translation._3Star;
        _4Star = TranslationHelper.Translation._4Star;
        _5Star = TranslationHelper.Translation._5Star;
        RemoveStarRating = TranslationHelper.Translation.RemoveStarRating;
        Theme = TranslationHelper.Translation.Theme;
        DarkTheme = TranslationHelper.Translation.DarkTheme;
        LightTheme = TranslationHelper.Translation.LightTheme;
        MouseDrag = TranslationHelper.Translation.MouseDrag;
        DoubleClick = TranslationHelper.Translation.DoubleClick;
        MoveWindow = TranslationHelper.Translation.MoveWindow;
        GithubRepo = TranslationHelper.Translation.GithubRepo;
        Version = TranslationHelper.Translation.Version;
        ViewLicenseFile = TranslationHelper.Translation.ViewLicenseFile;
        CheckForUpdates = TranslationHelper.Translation.CheckForUpdates;
        Credits = TranslationHelper.Translation.Credits;
        ColorPickerTool = TranslationHelper.Translation.ColorPickerTool;
        ColorPickerToolTooltip = TranslationHelper.Translation.ColorPickerToolTooltip;
        ExpandedGalleryItemSize = TranslationHelper.Translation.ExpandedGalleryItemSize;
        BottomGalleryItemSize = TranslationHelper.Translation.BottomGalleryItemSize;
        Square = TranslationHelper.Translation.Square;
        Uniform = TranslationHelper.Translation.Uniform;
        UniformToFill = TranslationHelper.Translation.UniformToFill;
        FillSquare = TranslationHelper.Translation.FillSquare;
        Fill = TranslationHelper.Translation.Fill;
        GallerySettings = TranslationHelper.Translation.GallerySettings;
        GalleryThumbnailStretch = TranslationHelper.Translation.GalleryThumbnailStretch;
        BottomGalleryThumbnailStretch = TranslationHelper.Translation.BottomGalleryThumbnailStretch;
        RestoreDown = TranslationHelper.Translation.RestoreDown;
        SideBySide = TranslationHelper.Translation.SideBySide;
        SideBySideTooltip = TranslationHelper.Translation.SideBySideTooltip;
        HighlightColor = TranslationHelper.Translation.HighlightColor;
        AllowZoomOut = TranslationHelper.Translation.AllowZoomOut;
        GlassTheme = TranslationHelper.Translation.GlassTheme;
        ChangingThemeRequiresRestart = TranslationHelper.Translation.ChangingThemeRequiresRestart;
    }

    #region Strings
    
    private string? _changingThemeRequiresRestart;
    
    public string? ChangingThemeRequiresRestart
    {
        get => _changingThemeRequiresRestart;
        set => this.RaiseAndSetIfChanged(ref _changingThemeRequiresRestart, value);
    }
    
    private string? _glassTheme;
    
    public string? GlassTheme
    {
        get => _glassTheme;
        set => this.RaiseAndSetIfChanged(ref _glassTheme, value);
    }
    
    private string? _allowZoomOut;
    
    public string? AllowZoomOut
    {
        get => _allowZoomOut;
        set => this.RaiseAndSetIfChanged(ref _allowZoomOut, value);
    }
    
    private string? _highlightColor;
    
    public string? HighlightColor
    {
        get => _highlightColor;
        set => this.RaiseAndSetIfChanged(ref _highlightColor, value);
    }
    
    private string? _sideBySide;
    
    public string? SideBySide
    {
        get => _sideBySide;
        set => this.RaiseAndSetIfChanged(ref _sideBySide, value);
    }
    
    private string? _sideBySideTooltip;
    
    public string? SideBySideTooltip
    {
        get => _sideBySideTooltip;
        set => this.RaiseAndSetIfChanged(ref _sideBySideTooltip, value);
    }

    private string? _fullscreenTxt;
    
    public string? FullscreenTxt
    {
        get => _fullscreenTxt;
        set => this.RaiseAndSetIfChanged(ref _fullscreenTxt, value);
    }
    
    private string? _bottomGalleryThumbnailStretch;
    
    public string? BottomGalleryThumbnailStretch
    {
        get => _bottomGalleryThumbnailStretch;
        set => this.RaiseAndSetIfChanged(ref _bottomGalleryThumbnailStretch, value);
    }
    
    private string? _galleryThumbnailStretch;
    
    public string? GalleryThumbnailStretch
    {
        get => _galleryThumbnailStretch;
        set => this.RaiseAndSetIfChanged(ref _galleryThumbnailStretch, value);
    }
    
    private string? _gallerySettings;
    
    public string? GallerySettings
    {
        get => _gallerySettings;
        set => this.RaiseAndSetIfChanged(ref _gallerySettings, value);
    }
    
    private string? _uniform;
    
    public string? Uniform
    {
        get => _uniform;
        set => this.RaiseAndSetIfChanged(ref _uniform, value);
    }
    
    private string? _uniformToFill;
    public string? UniformToFill
    {
        get => _uniformToFill;
        set => this.RaiseAndSetIfChanged(ref _uniformToFill, value);
    }
    
    private string? _fill;
    
    public string? Fill
    {
        get => _fill;
        set => this.RaiseAndSetIfChanged(ref _fill, value);
    }
    
    private string? _fillSquare;
    
    public string? FillSquare
    {
        get => _fillSquare;
        set => this.RaiseAndSetIfChanged(ref _fillSquare, value);
    }
    
    private string? _square;
    
    public string? Square
    {
        get => _square;
        set => this.RaiseAndSetIfChanged(ref _square, value);
    }
    
    private string? _bottomGalleryItemSize;
    
    public string? BottomGalleryItemSize
    {
        get => _bottomGalleryItemSize;
        set => this.RaiseAndSetIfChanged(ref _bottomGalleryItemSize, value);
    }
    
    private string? _expandedGalleryItemSize;
    
    public string? ExpandedGalleryItemSize
    {
        get => _expandedGalleryItemSize;
        set => this.RaiseAndSetIfChanged(ref _expandedGalleryItemSize, value);
    }
    
    private string? _colorPickerTool;
    
    public string? ColorPickerTool
    {
        get => _colorPickerTool;
        set => this.RaiseAndSetIfChanged(ref _colorPickerTool, value);
    }
    
    private string? _colorPickerToolTooltip;
    
    public string? ColorPickerToolTooltip
    {
        get => _colorPickerToolTooltip;
        set => this.RaiseAndSetIfChanged(ref _colorPickerToolTooltip, value);
    }

    private string? _credits;

    public string? Credits
    {
        get => _credits;
        set => this.RaiseAndSetIfChanged(ref _credits, value);
    }

    private string? _checkForUpdates;

    public string? CheckForUpdates
    {
        get => _checkForUpdates;
        set => this.RaiseAndSetIfChanged(ref _checkForUpdates, value);
    }

    private string? _viewLicenseFile;

    public string? ViewLicenseFile
    {
        get => _viewLicenseFile;
        set => this.RaiseAndSetIfChanged(ref _viewLicenseFile, value);
    }

    private string? _version;

    public string? Version
    {
        get => _version;
        set => this.RaiseAndSetIfChanged(ref _version, value);
    }

    private string? _githubRepo;

    public string? GithubRepo
    {
        get => _githubRepo;
        set => this.RaiseAndSetIfChanged(ref _githubRepo, value);
    }

    private string? _moveWindow;

    public string? MoveWindow
    {
        get => _moveWindow;
        set => this.RaiseAndSetIfChanged(ref _moveWindow, value);
    }

    private string? _doubleClick;

    public string? DoubleClick
    {
        get => _doubleClick;
        set => this.RaiseAndSetIfChanged(ref _doubleClick, value);
    }

    private string? _mouseDrag;

    public string? MouseDrag
    {
        get => _mouseDrag;
        set => this.RaiseAndSetIfChanged(ref _mouseDrag, value);
    }

    private string? _lightTheme;

    public string? LightTheme
    {
        get => _lightTheme;
        set => this.RaiseAndSetIfChanged(ref _lightTheme, value);
    }

    private string? _darkTheme;

    public string? DarkTheme
    {
        get => _darkTheme;
        set => this.RaiseAndSetIfChanged(ref _darkTheme, value);
    }

    private string? _theme;

    public string? Theme
    {
        get => _theme;
        set => this.RaiseAndSetIfChanged(ref _theme, value);
    }

    private string? _removeStarRating;

    public string? RemoveStarRating
    {
        get => _removeStarRating;
        set => this.RaiseAndSetIfChanged(ref _removeStarRating, value);
    }

    private string? _5star;

    public string? _5Star
    {
        get => _5star;
        set => this.RaiseAndSetIfChanged(ref _5star, value);
    }

    private string? _4star;

    public string? _4Star
    {
        get => _4star;
        set => this.RaiseAndSetIfChanged(ref _4star, value);
    }

    private string? _3star;

    public string? _3Star
    {
        get => _3star;
        set => this.RaiseAndSetIfChanged(ref _3star, value);
    }

    private string? _2star;

    public string? _2Star
    {
        get => _2star;
        set => this.RaiseAndSetIfChanged(ref _2star, value);
    }

    private string? _1star;

    public string? _1Star
    {
        get => _1star;
        set => this.RaiseAndSetIfChanged(ref _1star, value);
    }

    private string? _setStarRating;

    public string? SetStarRating
    {
        get => _setStarRating;
        set => this.RaiseAndSetIfChanged(ref _setStarRating, value);
    }

    private string? _normalWindow;

    public string? NormalWindow
    {
        get => _normalWindow;
        set => this.RaiseAndSetIfChanged(ref _normalWindow, value);
    }

    private string? _windowScaling;

    public string? WindowScaling
    {
        get => _windowScaling;
        set => this.RaiseAndSetIfChanged(ref _windowScaling, value);
    }

    private string? _centerWindow;

    public string? CenterWindow
    {
        get => _centerWindow;
        set => this.RaiseAndSetIfChanged(ref _centerWindow, value);
    }

    private string? _windowManagement;

    public string? WindowManagement
    {
        get => _windowManagement;
        set => this.RaiseAndSetIfChanged(ref _windowManagement, value);
    }

    private string? _showImageGallery;

    public string? ShowImageGallery
    {
        get => _showImageGallery;
        set => this.RaiseAndSetIfChanged(ref _showImageGallery, value);
    }

    private string? _toggleFullscreen;

    public string? ToggleFullscreen
    {
        get => _toggleFullscreen;
        set => this.RaiseAndSetIfChanged(ref _toggleFullscreen, value);
    }

    private string? _fileManagement;

    public string? FileManagement
    {
        get => _fileManagement;
        set => this.RaiseAndSetIfChanged(ref _fileManagement, value);
    }

    private string? _interfaceConfiguration;

    public string? InterfaceConfiguration
    {
        get => _interfaceConfiguration;
        set => this.RaiseAndSetIfChanged(ref _interfaceConfiguration, value);
    }

    private string? _changeBackground;

    public string? ChangeBackground
    {
        get => _changeBackground;
        set => this.RaiseAndSetIfChanged(ref _changeBackground, value);
    }

    private string? _imageControl;

    public string? ImageControl
    {
        get => _imageControl;
        set => this.RaiseAndSetIfChanged(ref _imageControl, value);
    }

    private string? _resetZoom;

    public string? ResetZoom
    {
        get => _resetZoom;
        set => this.RaiseAndSetIfChanged(ref _resetZoom, value);
    }

    private string? _pan;

    public string? Pan
    {
        get => _pan;
        set => this.RaiseAndSetIfChanged(ref _pan, value);
    }

    private string? _zoomOut;

    public string? ZoomOut
    {
        get => _zoomOut;
        set => this.RaiseAndSetIfChanged(ref _zoomOut, value);
    }

    private string? _zoomIn;

    public string? ZoomIn
    {
        get => _zoomIn;
        set => this.RaiseAndSetIfChanged(ref _zoomIn, value);
    }

    private string? _zoom;

    public string? Zoom
    {
        get => _zoom;
        set => this.RaiseAndSetIfChanged(ref _zoom, value);
    }

    private string? _scrollToBottom;

    public string? ScrollToBottom
    {
        get => _scrollToBottom;
        set => this.RaiseAndSetIfChanged(ref _scrollToBottom, value);
    }

    private string? _scrollToTop;

    public string? ScrollToTop
    {
        get => _scrollToTop;
        set => this.RaiseAndSetIfChanged(ref _scrollToTop, value);
    }

    private string? _scrollDown;

    public string? ScrollDown
    {
        get => _scrollDown;
        set => this.RaiseAndSetIfChanged(ref _scrollDown, value);
    }

    private string? _scrollUp;

    public string? ScrollUp
    {
        get => _scrollUp;
        set => this.RaiseAndSetIfChanged(ref _scrollUp, value);
    }

    private string? _scrollAndRotate;

    public string? ScrollAndRotate
    {
        get => _scrollAndRotate;
        set => this.RaiseAndSetIfChanged(ref _scrollAndRotate, value);
    }

    private string? _selectGalleryThumb;

    public string? SelectGalleryThumb
    {
        get => _selectGalleryThumb;
        set => this.RaiseAndSetIfChanged(ref _selectGalleryThumb, value);
    }

    private string? _navigation;

    public string? Navigation
    {
        get => _navigation;
        set => this.RaiseAndSetIfChanged(ref _navigation, value);
    }

    private string? _prevFolder;

    public string? PrevFolder
    {
        get => _prevFolder;
        set => this.RaiseAndSetIfChanged(ref _prevFolder, value);
    }

    private string? _nextFolder;

    public string? NextFolder
    {
        get => _nextFolder;
        set => this.RaiseAndSetIfChanged(ref _nextFolder, value);
    }

    private string? _firstImage;

    public string? FirstImage
    {
        get => _firstImage;
        set => this.RaiseAndSetIfChanged(ref _firstImage, value);
    }

    private string? _lastImage;

    public string? LastImage
    {
        get => _lastImage;
        set => this.RaiseAndSetIfChanged(ref _lastImage, value);
    }

    private string? _prevImage;

    public string? PrevImage
    {
        get => _prevImage;
        set => this.RaiseAndSetIfChanged(ref _prevImage, value);
    }

    private string? _nextImage;

    public string? NextImage
    {
        get => _nextImage;
        set => this.RaiseAndSetIfChanged(ref _nextImage, value);
    }

    private string? _changeKeybindingText;

    public string? ChangeKeybindingText
    {
        get => _changeKeybindingText;
        set => this.RaiseAndSetIfChanged(ref _changeKeybindingText, value);
    }

    private string? _toggleTaskbarProgress;

    public string? ToggleTaskbarProgress
    {
        get => _toggleTaskbarProgress;
        set => this.RaiseAndSetIfChanged(ref _toggleTaskbarProgress, value);
    }

    private string? _changeKeybindingTooltip;

    public string? ChangeKeybindingTooltip
    {
        get => _changeKeybindingTooltip;
        set => this.RaiseAndSetIfChanged(ref _changeKeybindingTooltip, value);
    }

    private string? _showBottomGalleryWhenUiIsHidden;

    public string? ShowBottomGalleryWhenUiIsHidden
    {
        get => _showBottomGalleryWhenUiIsHidden;
        set => this.RaiseAndSetIfChanged(ref _showBottomGalleryWhenUiIsHidden, value);
    }

    private string? _showBottomToolbar;

    public string? ShowBottomToolbar
    {
        get => _showBottomToolbar;
        set => this.RaiseAndSetIfChanged(ref _showBottomToolbar, value);
    }

    private string? _resetButtonText;

    public string? ResetButtonText
    {
        get => _resetButtonText;
        set => this.RaiseAndSetIfChanged(ref _resetButtonText, value);
    }

    private string? _secAbbreviation;

    public string? SecAbbreviation
    {
        get => _secAbbreviation;
        set => this.RaiseAndSetIfChanged(ref _secAbbreviation, value);
    }

    private string? _adjustNavSpeed;

    public string? AdjustNavSpeed
    {
        get => _adjustNavSpeed;
        set => this.RaiseAndSetIfChanged(ref _adjustNavSpeed, value);
    }

    private string? _adjustTimingForZoom;

    public string? AdjustTimingForZoom
    {
        get => _adjustTimingForZoom;
        set => this.RaiseAndSetIfChanged(ref _adjustTimingForZoom, value);
    }

    private string? _adjustTimingForSlideshow;

    public string? AdjustTimingForSlideshow
    {
        get => _adjustTimingForSlideshow;
        set => this.RaiseAndSetIfChanged(ref _adjustTimingForSlideshow, value);
    }

    private string? _none;

    public string? None
    {
        get => _none;
        set => this.RaiseAndSetIfChanged(ref _none, value);
    }

    private string? _applicationStartup;

    public string? ApplicationStartup
    {
        get => _applicationStartup;
        set => this.RaiseAndSetIfChanged(ref _applicationStartup, value);
    }

    private string? _forward;

    public string? Forward
    {
        get => _forward;
        set => this.RaiseAndSetIfChanged(ref _forward, value);
    }

    private string? _reverse;

    public string? Reverse
    {
        get => _reverse;
        set => this.RaiseAndSetIfChanged(ref _reverse, value);
    }

    private string? _scrollDirection;

    public string? ScrollDirection
    {
        get => _scrollDirection;
        set => this.RaiseAndSetIfChanged(ref _scrollDirection, value);
    }

    private string? _openInSameWindow;

    public string? OpenInSameWindow
    {
        get => _openInSameWindow;
        set => this.RaiseAndSetIfChanged(ref _openInSameWindow, value);
    }

    private string? _showFileSavingDialog;

    public string? ShowFileSavingDialog
    {
        get => _showFileSavingDialog;
        set => this.RaiseAndSetIfChanged(ref _showFileSavingDialog, value);
    }

    private string? _stayCentered;

    public string? StayCentered
    {
        get => _stayCentered;
        set => this.RaiseAndSetIfChanged(ref _stayCentered, value);
    }

    private string? _miscSettings;

    public string? MiscSettings
    {
        get => _miscSettings;
        set => this.RaiseAndSetIfChanged(ref _miscSettings, value);
    }

    private string? _scrollToZoom;

    public string? ScrollToZoom
    {
        get => _scrollToZoom;
        set => this.RaiseAndSetIfChanged(ref _scrollToZoom, value);
    }

    private string? _mouseWheel;

    public string? MouseWheel
    {
        get => _mouseWheel;
        set => this.RaiseAndSetIfChanged(ref _mouseWheel, value);
    }

    private string? _language;

    public string? Language
    {
        get => _language;
        set => this.RaiseAndSetIfChanged(ref _language, value);
    }

    private string? _appearance;

    public string? Appearance
    {
        get => _appearance;
        set => this.RaiseAndSetIfChanged(ref _appearance, value);
    }

    private string? _generalSettings;

    public string? GeneralSettings
    {
        get => _generalSettings;
        set => this.RaiseAndSetIfChanged(ref _generalSettings, value);
    }

    private string? _ctrlToZoom;

    public string? CtrlToZoom
    {
        get => _ctrlToZoom;
        set => this.RaiseAndSetIfChanged(ref _ctrlToZoom, value);
    }

    private string? _fileCut;

    public string? FileCut
    {
        get => _fileCut;
        set => this.RaiseAndSetIfChanged(ref _fileCut, value);
    }

    private string? _fileCopyPath;

    public string? FileCopyPath
    {
        get => _fileCopyPath;
        set => this.RaiseAndSetIfChanged(ref _fileCopyPath, value);
    }

    private string? _copyImage;

    public string? CopyImage
    {
        get => _copyImage;
        set => this.RaiseAndSetIfChanged(ref _copyImage, value);
    }

    private string? _imageTxt;

    public string? ImageTxt
    {
        get => _imageTxt;
        set => this.RaiseAndSetIfChanged(ref _imageTxt, value);
    }

    private string? _setAsLockScreenImage;

    public string? SetAsLockScreenImage
    {
        get => _setAsLockScreenImage;
        set => this.RaiseAndSetIfChanged(ref _setAsLockScreenImage, value);
    }

    private string? _setAsWallpaper;

    public string? SetAsWallpaper
    {
        get => _setAsWallpaper;
        set => this.RaiseAndSetIfChanged(ref _setAsWallpaper, value);
    }

    private string? _recentFiles;

    public string? RecentFiles
    {
        get => _recentFiles;
        set => this.RaiseAndSetIfChanged(ref _recentFiles, value);
    }

    private string? _ascending;

    public string? Ascending
    {
        get => _ascending;
        set => this.RaiseAndSetIfChanged(ref _ascending, value);
    }

    private string? _descending;

    public string? Descending
    {
        get => _descending;
        set => this.RaiseAndSetIfChanged(ref _descending, value);
    }

    private string? _random;

    public string? Random
    {
        get => _random;
        set => this.RaiseAndSetIfChanged(ref _random, value);
    }

    private string? _creationTime;

    public string? CreationTime
    {
        get => _creationTime;
        set => this.RaiseAndSetIfChanged(ref _creationTime, value);
    }

    private string? _fileExtension;

    public string? FileExtension
    {
        get => _fileExtension;
        set => this.RaiseAndSetIfChanged(ref _fileExtension, value);
    }

    private string? _sortFilesBy;

    public string? SortFilesBy
    {
        get => _sortFilesBy;
        set => this.RaiseAndSetIfChanged(ref _sortFilesBy, value);
    }

    private string? _lensModel;

    public string? LensModel
    {
        get => _lensModel;
        set => this.RaiseAndSetIfChanged(ref _lensModel, value);
    }

    private string? _lensMaker;

    public string? LensMaker
    {
        get => _lensMaker;
        set => this.RaiseAndSetIfChanged(ref _lensMaker, value);
    }

    private string? _exifVersion;

    public string? ExifVersion
    {
        get => _exifVersion;
        set => this.RaiseAndSetIfChanged(ref _exifVersion, value);
    }

    private string? _orientation;

    public string? Orientation
    {
        get => _orientation;
        set => this.RaiseAndSetIfChanged(ref _orientation, value);
    }

    private string? _photometricInterpretation;

    public string? PhotometricInterpretation
    {
        get => _photometricInterpretation;
        set => this.RaiseAndSetIfChanged(ref _photometricInterpretation, value);
    }

    private string? _brightness;

    public string? Brightness
    {
        get => _brightness;
        set => this.RaiseAndSetIfChanged(ref _brightness, value);
    }

    private string? _lightSource;

    public string? LightSource
    {
        get => _lightSource;
        set => this.RaiseAndSetIfChanged(ref _lightSource, value);
    }

    private string? _flashMode;

    public string? FlashMode
    {
        get => _flashMode;
        set => this.RaiseAndSetIfChanged(ref _flashMode, value);
    }

    private string? _flashEnergy;

    public string? FlashEnergy
    {
        get => _flashEnergy;
        set => this.RaiseAndSetIfChanged(ref _flashEnergy, value);
    }

    private string? _whiteBalance;

    public string? WhiteBalance
    {
        get => _whiteBalance;
        set => this.RaiseAndSetIfChanged(ref _whiteBalance, value);
    }

    private string? _sharpness;

    public string? Sharpness
    {
        get => _sharpness;
        set => this.RaiseAndSetIfChanged(ref _sharpness, value);
    }

    private string? _saturation;

    public string? Saturation
    {
        get => _saturation;
        set => this.RaiseAndSetIfChanged(ref _saturation, value);
    }

    private string? _contrast;

    public string? Contrast
    {
        get => _contrast;
        set => this.RaiseAndSetIfChanged(ref _contrast, value);
    }

    private string? _meteringMode;

    public string? MeteringMode
    {
        get => _meteringMode;
        set => this.RaiseAndSetIfChanged(ref _meteringMode, value);
    }

    private string? _focalLength35mm;

    public string? FocalLength35mm
    {
        get => _focalLength35mm;
        set => this.RaiseAndSetIfChanged(ref _focalLength35mm, value);
    }

    private string? _isoSpeed;

    public string? ISOSpeed
    {
        get => _isoSpeed;
        set => this.RaiseAndSetIfChanged(ref _isoSpeed, value);
    }

    private string? _digitalZoom;

    public string? DigitalZoom
    {
        get => _digitalZoom;
        set => this.RaiseAndSetIfChanged(ref _digitalZoom, value);
    }

    private string? _exposureProgram;

    public string? ExposureProgram
    {
        get => _exposureProgram;
        set => this.RaiseAndSetIfChanged(ref _exposureProgram, value);
    }

    private string? _exposureBias;

    public string? ExposureBias
    {
        get => _exposureBias;
        set => this.RaiseAndSetIfChanged(ref _exposureBias, value);
    }

    private string? _maxAperture;

    public string? MaxAperture
    {
        get => _maxAperture;
        set => this.RaiseAndSetIfChanged(ref _maxAperture, value);
    }

    private string? _fstop;

    public string? Fstop
    {
        get => _fstop;
        set => this.RaiseAndSetIfChanged(ref _fstop, value);
    }

    private string? _fnumber;

    public string? Fnumber
    {
        get => _fnumber;
        set => this.RaiseAndSetIfChanged(ref _fnumber, value);
    }

    private string? _focalLength;

    public string? FocalLength
    {
        get => _focalLength;
        set => this.RaiseAndSetIfChanged(ref _focalLength, value);
    }

    private string? _cameraModel;

    public string? CameraModel
    {
        get => _cameraModel;
        set => this.RaiseAndSetIfChanged(ref _cameraModel, value);
    }

    private string? _cameraMaker;

    public string? CameraMaker
    {
        get => _cameraMaker;
        set => this.RaiseAndSetIfChanged(ref _cameraMaker, value);
    }

    private string? _software;

    public string? Software
    {
        get => _software;
        set => this.RaiseAndSetIfChanged(ref _software, value);
    }

    private string? _xptitle;

    public string? XPTitle
    {
        get => _xptitle;
        set => this.RaiseAndSetIfChanged(ref _xptitle, value);
    }

    private string? _subject;

    public string? Subject
    {
        get => _subject;
        set => this.RaiseAndSetIfChanged(ref _subject, value);
    }

    private string? _created;

    public string? Created
    {
        get => _created;
        set => this.RaiseAndSetIfChanged(ref _created, value);
    }

    private string? _modified;

    public string? Modified
    {
        get => _modified;
        set => this.RaiseAndSetIfChanged(ref _modified, value);
    }

    private string? _lastAccessTime;

    public string? LastAccessTime
    {
        get => _lastAccessTime;
        set => this.RaiseAndSetIfChanged(ref _lastAccessTime, value);
    }

    private string? _convertTo;

    public string? ConvertTo
    {
        get => _convertTo;
        set => this.RaiseAndSetIfChanged(ref _convertTo, value);
    }

    private string? _copy;

    public string? Copy
    {
        get => _copy;
        set => this.RaiseAndSetIfChanged(ref _copy, value);
    }

    private string? _optimizeImage;

    public string? OptimizeImage
    {
        get => _optimizeImage;
        set => this.RaiseAndSetIfChanged(ref _optimizeImage, value);
    }

    private string? _fileProperties;

    public string? FileProperties
    {
        get => _fileProperties;
        set => this.RaiseAndSetIfChanged(ref _fileProperties, value);
    }

    private string? _imageInfo;

    public string? ImageInfo
    {
        get => _imageInfo;
        set => this.RaiseAndSetIfChanged(ref _imageInfo, value);
    }

    private string? _applicationShortcuts;

    public string? ApplicationShortcuts
    {
        get => _applicationShortcuts;
        set => this.RaiseAndSetIfChanged(ref _applicationShortcuts, value);
    }

    private string? _batchResize;

    public string? BatchResize
    {
        get => _batchResize;
        set => this.RaiseAndSetIfChanged(ref _batchResize, value);
    }

    private string? _effects;

    public string? Effects
    {
        get => _effects;
        set => this.RaiseAndSetIfChanged(ref _effects, value);
    }

    private string? _effectsTooltip;

    public string? EffectsTooltip
    {
        get => _effectsTooltip;
        set => this.RaiseAndSetIfChanged(ref _effectsTooltip, value);
    }

    private string? _file;

    public string? File
    {
        get => _file.FirstCharToUpper();
        set => this.RaiseAndSetIfChanged(ref _file, value);
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

    private string? _restoreDown;
    
    public string? RestoreDown
    {
        get => _restoreDown;
        set => this.RaiseAndSetIfChanged(ref _restoreDown, value);
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

    private string? _loopingDisabled;

    public string? LoopingDisabled
    {
        get => _loopingDisabled;
        set => this.RaiseAndSetIfChanged(ref _loopingDisabled, value);
    }

    private string? _loopingEnabled;

    public string? LoopingEnabled
    {
        get => _loopingEnabled;
        set => this.RaiseAndSetIfChanged(ref _loopingEnabled, value);
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

    private string? _aboutWindow;

    public string? AboutWindow
    {
        get => _aboutWindow;
        set => this.RaiseAndSetIfChanged(ref _aboutWindow, value);
    }

    private string? _about;

    public string? About
    {
        get => _about;
        set => this.RaiseAndSetIfChanged(ref _about, value);
    }

    private string? _showAllSettingsWindow;

    public string? ShowAllSettingsWindow
    {
        get => _showAllSettingsWindow;
        set => this.RaiseAndSetIfChanged(ref _showAllSettingsWindow, value);
    }

    private string? _stayTopMost;

    public string? StayTopMost
    {
        get => _stayTopMost;
        set => this.RaiseAndSetIfChanged(ref _stayTopMost, value);
    }

    private string? _searchSubdirectory;

    public string? SearchSubdirectory
    {
        get => _searchSubdirectory;
        set => this.RaiseAndSetIfChanged(ref _searchSubdirectory, value);
    }

    private string? _toggleLooping;

    public string? ToggleLooping
    {
        get => _toggleLooping;
        set => this.RaiseAndSetIfChanged(ref _toggleLooping, value);
    }

    private string? _hideShowInterface;

    public string? HideShowInterface
    {
        get => _hideShowInterface;
        set => this.RaiseAndSetIfChanged(ref _hideShowInterface, value);
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

    private string? _folder;

    public string? Folder
    {
        get => _folder;
        set => this.RaiseAndSetIfChanged(ref _folder, value);
    }

    private string? _fullPath;

    public string? FullPath
    {
        get => _fullPath;
        set => this.RaiseAndSetIfChanged(ref _fullPath, value);
    }

    private string? _resize;

    public string? Resize
    {
        get => _resize;
        set => this.RaiseAndSetIfChanged(ref _resize, value);
    }

    private string? _noResize;

    public string? NoResize
    {
        get => _noResize;
        set => this.RaiseAndSetIfChanged(ref _noResize, value);
    }

    private string? _apply;

    public string? Apply
    {
        get => _apply;
        set => this.RaiseAndSetIfChanged(ref _apply, value);
    }

    private string? _cancel;

    public string? Cancel
    {
        get => _cancel;
        set => this.RaiseAndSetIfChanged(ref _cancel, value);
    }

    private string? _bitDepth;

    public string? BitDepth
    {
        get => _bitDepth;
        set => this.RaiseAndSetIfChanged(ref _bitDepth, value);
    }

    private string? _aspectRatio;

    public string? AspectRatio
    {
        get => _aspectRatio;
        set => this.RaiseAndSetIfChanged(ref _aspectRatio, value);
    }

    private string? _noConversion;

    public string? NoConversion
    {
        get => _noConversion;
        set => this.RaiseAndSetIfChanged(ref _noConversion, value);
    }

    private string? _width;

    public string? Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    private string? _height;

    public string? Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    private string? _sizeMp;

    public string? SizeMp
    {
        get => _sizeMp;
        set => this.RaiseAndSetIfChanged(ref _sizeMp, value);
    }

    private string? _resolution;

    public string? Resolution
    {
        get => _resolution;
        set => this.RaiseAndSetIfChanged(ref _resolution, value);
    }

    private string? _printSizeIn;

    public string? PrintSizeIn
    {
        get => _printSizeIn;
        set => this.RaiseAndSetIfChanged(ref _printSizeIn, value);
    }

    private string? _printSizeCm;

    public string? PrintSizeCm
    {
        get => _printSizeCm;
        set => this.RaiseAndSetIfChanged(ref _printSizeCm, value);
    }

    private string? _centimeters;

    public string? Centimeters
    {
        get => _centimeters;
        set => this.RaiseAndSetIfChanged(ref _centimeters, value);
    }

    private string? _inches;

    public string? Inches
    {
        get => _inches;
        set => this.RaiseAndSetIfChanged(ref _inches, value);
    }

    private string? _sizeTooltip;

    public string? SizeTooltip
    {
        get => _sizeTooltip;
        set => this.RaiseAndSetIfChanged(ref _sizeTooltip, value);
    }

    private string? _latitude;

    public string? Latitude
    {
        get => _latitude;
        set => this.RaiseAndSetIfChanged(ref _latitude, value);
    }

    private string? _longitude;

    public string? Longitude
    {
        get => _longitude;
        set => this.RaiseAndSetIfChanged(ref _longitude, value);
    }

    private string? _altitude;

    public string? Altitude
    {
        get => _altitude;
        set => this.RaiseAndSetIfChanged(ref _altitude, value);
    }

    private string? _authors;

    public string? Authors
    {
        get => _authors;
        set => this.RaiseAndSetIfChanged(ref _authors, value);
    }

    private string? _dateTaken;

    public string? DateTaken
    {
        get => _dateTaken;
        set => this.RaiseAndSetIfChanged(ref _dateTaken, value);
    }

    private string? _copyright;

    public string? Copyright
    {
        get => _copyright;
        set => this.RaiseAndSetIfChanged(ref _copyright, value);
    }

    private string? _resolutionUnit;

    public string? ResolutionUnit
    {
        get => _resolutionUnit;
        set => this.RaiseAndSetIfChanged(ref _resolutionUnit, value);
    }

    private string? _colorRepresentation;

    public string? ColorRepresentation
    {
        get => _colorRepresentation;
        set => this.RaiseAndSetIfChanged(ref _colorRepresentation, value);
    }

    private string? _compressedBitsPixel;

    public string? CompressedBitsPixel
    {
        get => _compressedBitsPixel;
        set => this.RaiseAndSetIfChanged(ref _compressedBitsPixel, value);
    }

    private string? _compression;

    public string? Compression
    {
        get => _compression;
        set => this.RaiseAndSetIfChanged(ref _compression, value);
    }

    private string? _exposureTime;

    public string? ExposureTime
    {
        get => _exposureTime;
        set => this.RaiseAndSetIfChanged(ref _exposureTime, value);
    }

    #endregion Strings

    #endregion Localization

    #region Image

    private FileInfo? _fileInfo;

    public FileInfo? FileInfo
    {
        get => _fileInfo;
        set => this.RaiseAndSetIfChanged(ref _fileInfo, value);
    }

    private int _pixelWidth;

    public int PixelWidth
    {
        get => _pixelWidth;
        set => this.RaiseAndSetIfChanged(ref _pixelWidth, value);
    }

    private int _pixelHeight;

    public int PixelHeight
    {
        get => _pixelHeight;
        set => this.RaiseAndSetIfChanged(ref _pixelHeight, value);
    }

    private double _dpiX;

    public double DpiX
    {
        get => _dpiX;
        set => this.RaiseAndSetIfChanged(ref _dpiX, value);
    }

    private double _dpiY;

    public double DpiY
    {
        get => _dpiY;
        set => this.RaiseAndSetIfChanged(ref _dpiY, value);
    }

    #endregion Image
}