using PicView.Core.Localization;
using ReactiveUI;

namespace PicView.Avalonia.ViewModels;

public class ViewModelBase : ReactiveObject
{
    #region Localization

    public void UpdateLanguage()
    {
        File = TranslationHelper.GetTranslation("File");
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
        UnFlip = TranslationHelper.GetTranslation("Unflip");
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
        ImageInfo = TranslationHelper.GetTranslation("ImageInfo");
        About = TranslationHelper.GetTranslation("About");
        ShowAllSettingsWindow = TranslationHelper.GetTranslation("ShowAllSettingsWindow");
        StayTopMost = TranslationHelper.GetTranslation("StayTopMost");
        SearchSubdirectory = TranslationHelper.GetTranslation("SearchSubdirectory");
        ToggleLooping = TranslationHelper.GetTranslation("ToggleLooping");
        HideShowInterface = TranslationHelper.GetTranslation("HideShowInterface");
        ApplicationShortcuts = TranslationHelper.GetTranslation("ApplicationShortcuts");
        BatchResize = TranslationHelper.GetTranslation("BatchResize");
        Effects = TranslationHelper.GetTranslation("Effects");
        EffectsTooltip = TranslationHelper.GetTranslation("EffectsTooltip");
        FileProperties = TranslationHelper.GetTranslation("FileProperties");
        OptimizeImage = TranslationHelper.GetTranslation("OptimizeImage");
        ImageInfo = TranslationHelper.GetTranslation("ImageInfo");
        FileName = TranslationHelper.GetTranslation("FileName");
        FileSize = TranslationHelper.GetTranslation("FileSize");
        Folder = TranslationHelper.GetTranslation("Folder");
        FullPath = TranslationHelper.GetTranslation("FullPath");
        Created = TranslationHelper.GetTranslation("Created");
        Modified = TranslationHelper.GetTranslation("Modified");
        LastAccessTime = TranslationHelper.GetTranslation("LastAccessTime");
        ConvertTo = TranslationHelper.GetTranslation("ConvertTo");
        NoConversion = TranslationHelper.GetTranslation("NoConversion");
        Resize = TranslationHelper.GetTranslation("Resize");
        NoResize = TranslationHelper.GetTranslation("NoResize");
        Apply = TranslationHelper.GetTranslation("Apply");
        Cancel = TranslationHelper.GetTranslation("Cancel");
        BitDepth = TranslationHelper.GetTranslation("BitDepth");
        AspectRatio = TranslationHelper.GetTranslation("AspectRatio");
        Width = TranslationHelper.GetTranslation("Width");
        Height = TranslationHelper.GetTranslation("Height");
        SizeMp = TranslationHelper.GetTranslation("SizeMp");
        Resolution = TranslationHelper.GetTranslation("Resolution");
        PrintSizeIn = TranslationHelper.GetTranslation("PrintSizeIn");
        PrintSizeCm = TranslationHelper.GetTranslation("PrintSizeCm");
        Centimeters = TranslationHelper.GetTranslation("Centimeters");
        Inches = TranslationHelper.GetTranslation("Inches");
        SizeTooltip = TranslationHelper.GetTranslation("SizeTooltip");
        Latitude = TranslationHelper.GetTranslation("Latitude");
        Longitude = TranslationHelper.GetTranslation("Longitude");
        Altitude = TranslationHelper.GetTranslation("Altitude");
        Authors = TranslationHelper.GetTranslation("Authors");
        DateTaken = TranslationHelper.GetTranslation("DateTaken");
        Copyright = TranslationHelper.GetTranslation("Copyright");
        ResolutionUnit = TranslationHelper.GetTranslation("ResolutionUnit");
        ColorRepresentation = TranslationHelper.GetTranslation("ColorRepresentation");
        CompressedBitsPixel = TranslationHelper.GetTranslation("CompressedBitsPixel");
        Compression = TranslationHelper.GetTranslation("Compression");
        ExposureTime = TranslationHelper.GetTranslation("ExposureTime");
        XPTitle = TranslationHelper.GetTranslation("Title");
        Subject = TranslationHelper.GetTranslation("Subject");
        Software = TranslationHelper.GetTranslation("Software");
        CameraMaker = TranslationHelper.GetTranslation("CameraMaker");
        CameraModel = TranslationHelper.GetTranslation("CameraModel");
        FocalLength = TranslationHelper.GetTranslation("FocalLength");
        Fnumber = TranslationHelper.GetTranslation("Fnumber");
        Fstop = TranslationHelper.GetTranslation("Fstop");
        MaxAperture = TranslationHelper.GetTranslation("MaxAperture");
        ExposureBias = TranslationHelper.GetTranslation("ExposureBias");
        ExposureProgram = TranslationHelper.GetTranslation("ExposureProgram");
        DigitalZoom = TranslationHelper.GetTranslation("DigitalZoom");
        ISOSpeed = TranslationHelper.GetTranslation("ISOSpeed");
        FocalLength35mm = TranslationHelper.GetTranslation("FocalLength35mm");
        MeteringMode = TranslationHelper.GetTranslation("MeteringMode");
        Contrast = TranslationHelper.GetTranslation("Contrast");
        Saturation = TranslationHelper.GetTranslation("Saturation");
        Sharpness = TranslationHelper.GetTranslation("Sharpness");
        WhiteBalance = TranslationHelper.GetTranslation("WhiteBalance");
        FlashEnergy = TranslationHelper.GetTranslation("FlashEnergy");
        FlashMode = TranslationHelper.GetTranslation("FlashMode");
        LightSource = TranslationHelper.GetTranslation("LightSource");
        Brightness = TranslationHelper.GetTranslation("Brightness");
        PhotometricInterpretation = TranslationHelper.GetTranslation("PhotometricInterpretation");
        Orientation = TranslationHelper.GetTranslation("Orientation");
        ExifVersion = TranslationHelper.GetTranslation("ExifVersion");
        LensMaker = TranslationHelper.GetTranslation("LensMaker");
        LensModel = TranslationHelper.GetTranslation("LensModel");
        SortFilesBy = TranslationHelper.GetTranslation("SortFilesBy");
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
        get => _file;
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