
namespace PicView.Core.IPlatform;

public interface IFunctionsMapper
{
    Func<ValueTask>? GetFunctionByName(string functionName);

    // Navigation
    ValueTask Next();
    ValueTask NextFolder();
    ValueTask NextArchive();
    ValueTask Last();
    ValueTask Prev();
    ValueTask PrevFolder();
    ValueTask PrevArchive();
    ValueTask First();
    ValueTask Next10();
    ValueTask Next100();
    ValueTask Prev10();
    ValueTask Prev100();
    ValueTask StopRepeatedNavigation();
    ValueTask Search();
    ValueTask ToggleLooping();
    
    // Viewport
    ValueTask Up();
    ValueTask Down();
    
    // Rotation
    ValueTask RotateRight();
    ValueTask RotateLeft();
    ValueTask Rotate0();
    ValueTask Rotate90();
    ValueTask Rotate180();
    ValueTask Rotate270();
    
    // Zoom
    ValueTask ZoomIn();
    ValueTask ZoomOut();
    ValueTask ResetZoom();
    ValueTask ResetZoomAndRotations();
    ValueTask ChangeCtrlZoom();
    
    // Scroll
    ValueTask ScrollDown();
    ValueTask ScrollUp();
    ValueTask ScrollToTop();
    ValueTask ScrollToBottom();
    ValueTask ToggleScroll();
    
    // Interface Toggles
    ValueTask ToggleInterface();
    ValueTask ToggleSubdirectories();
    ValueTask ToggleBottomToolbar();
    ValueTask ToggleHoverBar();
    ValueTask ToggleTaskbarProgress();
    ValueTask ToggleConstrainBackgroundColor();
    ValueTask ChangeBackground();
    ValueTask ToggleDropDownMenu();
    ValueTask ToggleFileHistory();
    ValueTask ToggleShowFullPathInTitleBar();
    
    // Gallery
    ValueTask ToggleGallery();
    ValueTask OpenCloseDockedGallery();
    ValueTask CloseGallery();
    ValueTask GalleryClick();

    // Open Windows/Dialogs
    ValueTask AboutWindow();
    ValueTask CheckForUpdates();
    ValueTask ConvertWindow();
    ValueTask KeybindingsWindow();
    ValueTask EffectsWindow();
    ValueTask ImageInfoWindow();
    ValueTask ResizeWindow();
    ValueTask BatchResizeWindow();
    ValueTask FileAssociationsWindow();
    ValueTask SettingsWindow();
    
    // Windows operations
    ValueTask ZoomToFit();
    ValueTask AutoFitWindow();
    ValueTask NormalWindow();
    ValueTask ToggleFullscreen();
    ValueTask Fullscreen();
    ValueTask SetTopMost();
    ValueTask Close();
    ValueTask Exit();
    ValueTask Center();
    ValueTask Maximize();
    ValueTask Minimize();
    ValueTask Restore();
    ValueTask NewWindow();
    ValueTask ShowStartUpMenu();

    // File Operations
    ValueTask OpenLastFile();
    ValueTask OpenPreviousFileHistoryEntry();
    ValueTask OpenNextFileHistoryEntry();
    ValueTask Print();
    ValueTask Open();
    ValueTask OpenWith();
    ValueTask OpenInExplorer();
    ValueTask Save();
    ValueTask SaveAs();
    ValueTask SaveAsPDF();
    ValueTask DeleteFile();
    ValueTask DeleteFilePermanently();
    ValueTask Rename();
    ValueTask ShowFileProperties();

    // Clipboard & Edit
    ValueTask CopyFile();
    ValueTask CopyFilePath();
    ValueTask CopyImage();
    ValueTask CopyBase64();
    ValueTask DuplicateFile();
    ValueTask CutFile();
    ValueTask Paste();


    // Image Operations
    ValueTask SideBySide();
    ValueTask Reload();
    ValueTask ResizeImage();
    ValueTask Crop();
    ValueTask Flip();
    ValueTask OptimizeImage();
    ValueTask Slideshow();
    ValueTask ColorPicker();

    // Sorting
    ValueTask SortFilesByName();
    ValueTask SortFilesByCreationTime();
    ValueTask SortFilesByLastAccessTime();
    ValueTask SortFilesByLastWriteTime();
    ValueTask SortFilesBySize();
    ValueTask SortFilesByExtension();
    ValueTask SortFilesRandomly();
    ValueTask SortFilesAscending();
    ValueTask SortFilesDescending();

    // Ratings
    ValueTask Set0Star();
    ValueTask Set1Star();
    ValueTask Set2Star();
    ValueTask Set3Star();
    ValueTask Set4Star();
    ValueTask Set5Star();

    // Wallpaper
    ValueTask SetAsWallpaper();
    ValueTask SetAsWallpaperTiled();
    ValueTask SetAsWallpaperCentered();
    ValueTask SetAsWallpaperStretched();
    ValueTask SetAsWallpaperFitted();
    ValueTask SetAsWallpaperFilled();
    ValueTask SetAsLockscreenCentered();
    ValueTask SetAsLockScreen();

    // Tabs
    ValueTask NewTab();
    ValueTask CloseTab();

    // System & Settings
    ValueTask ResetSettings();
    ValueTask Restart();
    ValueTask ShowSettingsFile();
    ValueTask ShowKeybindingsFile();
    ValueTask ShowRecentHistoryFile();
    ValueTask ToggleOpeningInSameWindow();
}
