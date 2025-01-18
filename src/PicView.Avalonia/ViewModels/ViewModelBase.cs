using PicView.Core.Extensions;
using PicView.Core.ImageDecoding;
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
        PermanentlyDelete = TranslationHelper.Translation.PermanentlyDelete;
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
        ReadAbleAspectRatio = TranslationHelper.Translation.AspectRatio;
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
        FileCut = TranslationHelper.Translation.Cut;
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
        ShowUI = TranslationHelper.Translation.ShowUI;
        HideUI = TranslationHelper.Translation.HideUI;
        HideBottomToolbar = TranslationHelper.Translation.HideBottomToolbar;
        Center = TranslationHelper.Translation.Center;
        Tile = TranslationHelper.Translation.Tile;
        Fit = TranslationHelper.Translation.Fit;
        Pixels = TranslationHelper.Translation.Pixels;
        Percentage = TranslationHelper.Translation.Percentage;
        Quality = TranslationHelper.Translation.Quality;
        SaveAs = TranslationHelper.Translation.SaveAs;
        Reset = TranslationHelper.Translation.Reset;
        AdvanceBy10Images = TranslationHelper.Translation.AdvanceBy10Images;
        AdvanceBy100Images = TranslationHelper.Translation.AdvanceBy100Images;
        GoBackBy10Images = TranslationHelper.Translation.GoBackBy10Images;
        GoBackBy100Images = TranslationHelper.Translation.GoBackBy100Images;
        ShowFadeInButtonsOnHover = TranslationHelper.Translation.ShowFadeInButtonsOnHover;
        DisableFadeInButtonsOnHover = TranslationHelper.Translation.DisableFadeInButtonsOnHover;
        UsingTouchpad = TranslationHelper.Translation.UsingTouchpad;
        UsingMouse = TranslationHelper.Translation.UsingMouse;
        SourceFolder = TranslationHelper.Translation.SourceFolder;
        OutputFolder = TranslationHelper.Translation.OutputFolder;
        GenerateThumbnails = TranslationHelper.Translation.GenerateThumbnails;
        Lossless = TranslationHelper.Translation.Lossless;
        Lossy = TranslationHelper.Translation.Lossy;
        Start = TranslationHelper.Translation.Start;
        Thumbnail = TranslationHelper.Translation.Thumbnail;
        WidthAndHeight = TranslationHelper.Translation.WidthAndHeight;
        CloseWindowPrompt = TranslationHelper.Translation.CloseWindowPrompt;
        ShowConfirmationOnEsc = TranslationHelper.Translation.ShowConfirmationOnEsc;
        ImageAliasing = TranslationHelper.Translation.ImageAliasing;
        HighQuality = TranslationHelper.Translation.HighQuality;
        Lighting = TranslationHelper.Translation.Lighting;
        BlackAndWhite = TranslationHelper.Translation.BlackAndWhite;
        NegativeColors = TranslationHelper.Translation.NegativeColors;
        Blur = TranslationHelper.Translation.Blur;
        DirectionalBlur = TranslationHelper.Translation.DirectionalBlur;
        PencilSketch = TranslationHelper.Translation.PencilSketch;
        OldMovie = TranslationHelper.Translation.OldMovie;
        Posterize = TranslationHelper.Translation.Posterize;
        ClearEffects = TranslationHelper.Translation.ClearEffects;
        Solarize = TranslationHelper.Translation.Solarize;
    }

    #region Strings
    
    public string? Solarize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? ClearEffects
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? Posterize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? OldMovie
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? PencilSketch
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? DirectionalBlur
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? Blur
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? NegativeColors
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? BlackAndWhite
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? Lighting
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? HighQuality
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? ImageAliasing
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? CloseWindowPrompt
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? ShowConfirmationOnEsc
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? WidthAndHeight
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? Thumbnail
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? Start
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? Lossless
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? Lossy
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? GenerateThumbnails
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SourceFolder
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? OutputFolder
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? UsingTouchpad
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? UsingMouse
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ShowFadeInButtonsOnHover
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? DisableFadeInButtonsOnHover
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? AdvanceBy10Images
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? AdvanceBy100Images
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? GoBackBy10Images
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? GoBackBy100Images
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Reset
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SaveAs
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Quality
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Percentage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Pixels
    {
        get => field.FirstCharToUpper();
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Fit
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Tile
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Center
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? HideBottomToolbar
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ChangingThemeRequiresRestart
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? GlassTheme
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? AllowZoomOut
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? HighlightColor
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SideBySide
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SideBySideTooltip
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FullscreenTxt
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? BottomGalleryThumbnailStretch
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? GalleryThumbnailStretch
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? GallerySettings
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Uniform
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? UniformToFill
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Fill
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FillSquare
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Square
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? BottomGalleryItemSize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ExpandedGalleryItemSize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ColorPickerTool
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ColorPickerToolTooltip
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Credits
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? CheckForUpdates
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ViewLicenseFile
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Version
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? GithubRepo
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? MoveWindow
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? DoubleClick
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? MouseDrag
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? LightTheme
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? DarkTheme
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Theme
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? RemoveStarRating
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? _5Star
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? _4Star
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? _3Star
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? _2Star
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? _1Star
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SetStarRating
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? NormalWindow
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? WindowScaling
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? CenterWindow
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? WindowManagement
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ShowImageGallery
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ToggleFullscreen
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FileManagement
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? InterfaceConfiguration
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ChangeBackground
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ImageControl
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ResetZoom
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Pan
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ZoomOut
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ZoomIn
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Zoom
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ScrollToBottom
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ScrollToTop
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ScrollDown
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ScrollUp
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ScrollAndRotate
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SelectGalleryThumb
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Navigation
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? PrevFolder
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? NextFolder
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FirstImage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? LastImage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? PrevImage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? NextImage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ChangeKeybindingText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ToggleTaskbarProgress
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ChangeKeybindingTooltip
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ShowBottomGalleryWhenUiIsHidden
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ShowBottomToolbar
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ResetButtonText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SecAbbreviation
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? AdjustNavSpeed
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? AdjustTimingForZoom
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? AdjustTimingForSlideshow
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? None
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ApplicationStartup
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Forward
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Reverse
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ScrollDirection
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? OpenInSameWindow
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ShowFileSavingDialog
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? StayCentered
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? MiscSettings
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ScrollToZoom
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? MouseWheel
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Language
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Appearance
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? GeneralSettings
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? CtrlToZoom
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FileCut
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FileCopyPath
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? CopyImage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ImageTxt
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SetAsLockScreenImage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SetAsWallpaper
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? RecentFiles
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Ascending
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Descending
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Random
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? CreationTime
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FileExtension
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SortFilesBy
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? LensModel
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? LensMaker
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ExifVersion
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Orientation
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? PhotometricInterpretation
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Brightness
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? LightSource
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FlashMode
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FlashEnergy
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? WhiteBalance
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Sharpness
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Saturation
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Contrast
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? MeteringMode
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FocalLength35mm
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ISOSpeed
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? DigitalZoom
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ExposureProgram
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ExposureBias
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? MaxAperture
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Fstop
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Fnumber
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FocalLength
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? CameraModel
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? CameraMaker
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Software
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? XPTitle
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Subject
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Created
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Modified
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? LastAccessTime
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ConvertTo
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Copy
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? OptimizeImage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FileProperties
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ImageInfo
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ApplicationShortcuts
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? BatchResize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Effects
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? EffectsTooltip
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? File
    {
        get => field.FirstCharToUpper();
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SelectFile
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? OpenLastFile
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FilePaste
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Reload
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Print
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? DeleteFile
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public string? PermanentlyDelete
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Save
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? CopyFile
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? NewWindow
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Close
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? RestoreDown
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Open
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? OpenFileDialog
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ShowInFolder
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? OpenWith
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? RenameFile
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? DuplicateFile
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? RotateLeft
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? RotateRight
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Flip
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? UnFlip
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ShowUI
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? HideUI
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ShowBottomGallery
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? HideBottomGallery
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? LoopingDisabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? LoopingEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? AutoFitWindow
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Stretch
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Crop
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ResizeImage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? GoToImageAtSpecifiedIndex
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ToggleScroll
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ScrollEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ScrollDisabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Slideshow
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Settings
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? AboutWindow
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? About
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ShowAllSettingsWindow
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? StayTopMost
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SearchSubdirectory
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ToggleLooping
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FileName
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FileSize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Folder
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? FullPath
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Resize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? NoResize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Apply
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Cancel
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? BitDepth
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ReadAbleAspectRatio
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? NoConversion
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Width
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Height
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SizeMp
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Resolution
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? PrintSizeIn
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? PrintSizeCm
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Centimeters
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Inches
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SizeTooltip
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Latitude
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Longitude
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Altitude
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Authors
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? DateTaken
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Copyright
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ResolutionUnit
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ColorRepresentation
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? CompressedBitsPixel
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? Compression
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ExposureTime
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    #endregion Strings

    #endregion Localization

    #region Image

    public FileInfo? FileInfo
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int PixelWidth
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int PixelHeight
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public double DpiX
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public double DpiY
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public EXIFHelper.EXIFOrientation? ExifOrientation
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    #endregion Image
}