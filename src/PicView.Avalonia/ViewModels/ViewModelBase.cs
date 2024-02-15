using ReactiveUI;

namespace PicView.Avalonia.ViewModels;

public class ViewModelBase : ReactiveObject
{
    #region Localization

    private FileInfo? _fileInfo;

    public FileInfo? FileInfo
    {
        get => _fileInfo;
        set => this.RaiseAndSetIfChanged(ref _fileInfo, value);
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

    #endregion Localization
}