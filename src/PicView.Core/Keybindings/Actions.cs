namespace PicView.Core.Keybindings;

/// <summary>
/// The name of every ordinary Action, for running it through the window's Action seam and for binding it in axaml
/// with <c>{x:Static}</c>. Which Actions exist is decided by <see cref="KeybindCategoryDefinition"/>.
/// </summary>
public static class Actions
{
    // Navigation
    public const string Next = nameof(Next);
    public const string Prev = nameof(Prev);
    public const string Next10 = nameof(Next10);
    public const string Prev10 = nameof(Prev10);
    public const string Next100 = nameof(Next100);
    public const string Prev100 = nameof(Prev100);
    public const string First = nameof(First);
    public const string Last = nameof(Last);
    public const string NextFolder = nameof(NextFolder);
    public const string PrevFolder = nameof(PrevFolder);
    public const string NextArchive = nameof(NextArchive);
    public const string PrevArchive = nameof(PrevArchive);
    public const string FileHistoryNext = nameof(FileHistoryNext);
    public const string FileHistoryPrev = nameof(FileHistoryPrev);
    public const string Search = nameof(Search);
    public const string ToggleLooping = nameof(ToggleLooping);

    // Scroll and rotate
    public const string Up = nameof(Up);
    public const string Down = nameof(Down);
    public const string ScrollUp = nameof(ScrollUp);
    public const string ScrollDown = nameof(ScrollDown);
    public const string ScrollToTop = nameof(ScrollToTop);
    public const string ScrollToBottom = nameof(ScrollToBottom);
    public const string ToggleScroll = nameof(ToggleScroll);
    public const string RotateLeft = nameof(RotateLeft);
    public const string RotateRight = nameof(RotateRight);
    public const string Rotate0 = nameof(Rotate0);
    public const string Rotate90 = nameof(Rotate90);
    public const string Rotate180 = nameof(Rotate180);
    public const string Rotate270 = nameof(Rotate270);

    // Zoom
    public const string ZoomIn = nameof(ZoomIn);
    public const string ZoomOut = nameof(ZoomOut);
    public const string ResetZoom = nameof(ResetZoom);
    public const string ResetZoomAndRotations = nameof(ResetZoomAndRotations);
    public const string ZoomToFit = nameof(ZoomToFit);

    // Image control
    public const string SideBySide = nameof(SideBySide);
    public const string Flip = nameof(Flip);
    public const string Crop = nameof(Crop);
    public const string ChangeBackground = nameof(ChangeBackground);
    public const string OptimizeImage = nameof(OptimizeImage);
    public const string ResizeImage = nameof(ResizeImage);
    public const string ColorPicker = nameof(ColorPicker);

    // Interface
    public const string ToggleInterface = nameof(ToggleInterface);
    public const string Slideshow = nameof(Slideshow);
    public const string ToggleGallery = nameof(ToggleGallery);
    public const string OpenCloseDockedGallery = nameof(OpenCloseDockedGallery);
    public const string ToggleDockedGalleryInHiddenUI = nameof(ToggleDockedGalleryInHiddenUI);
    public const string ToggleBottomToolbar = nameof(ToggleBottomToolbar);
    public const string ToggleHoverBar = nameof(ToggleHoverBar);
    public const string ToggleConstrainBackgroundColor = nameof(ToggleConstrainBackgroundColor);

    // File management
    public const string Open = nameof(Open);
    public const string OpenWith = nameof(OpenWith);
    public const string OpenInExplorer = nameof(OpenInExplorer);
    public const string Reload = nameof(Reload);
    public const string Save = nameof(Save);
    public const string SaveAs = nameof(SaveAs);
    public const string SaveAsPDF = nameof(SaveAsPDF);
    public const string Print = nameof(Print);
    public const string DeleteFile = nameof(DeleteFile);
    public const string DeleteFilePermanently = nameof(DeleteFilePermanently);
    public const string Rename = nameof(Rename);
    public const string ShowFileProperties = nameof(ShowFileProperties);

    // Sorting
    public const string SortFilesByName = nameof(SortFilesByName);
    public const string SortFilesBySize = nameof(SortFilesBySize);
    public const string SortFilesByExtension = nameof(SortFilesByExtension);
    public const string SortFilesByCreationTime = nameof(SortFilesByCreationTime);
    public const string SortFilesByLastAccessTime = nameof(SortFilesByLastAccessTime);
    public const string SortFilesByLastWriteTime = nameof(SortFilesByLastWriteTime);
    public const string SortFilesRandomly = nameof(SortFilesRandomly);
    public const string SortFilesAscending = nameof(SortFilesAscending);
    public const string SortFilesDescending = nameof(SortFilesDescending);

    // Copy
    public const string CopyFile = nameof(CopyFile);
    public const string CutFile = nameof(CutFile);
    public const string CopyFilePath = nameof(CopyFilePath);
    public const string CopyImage = nameof(CopyImage);
    public const string CopyBase64 = nameof(CopyBase64);
    public const string Paste = nameof(Paste);
    public const string DuplicateFile = nameof(DuplicateFile);

    // Tabs
    public const string NewTab = nameof(NewTab);
    public const string CloseTab = nameof(CloseTab);

    // Tool and app windows
    public const string AboutWindow = nameof(AboutWindow);
    public const string SettingsWindow = nameof(SettingsWindow);
    public const string ImageInfoWindow = nameof(ImageInfoWindow);
    public const string EffectsWindow = nameof(EffectsWindow);
    public const string ConvertWindow = nameof(ConvertWindow);
    public const string KeybindingsWindow = nameof(KeybindingsWindow);
    public const string BatchResizeWindow = nameof(BatchResizeWindow);
    public const string ResizeWindow = nameof(ResizeWindow);
    public const string FileAssociationsWindow = nameof(FileAssociationsWindow);
    public const string CheckForUpdates = nameof(CheckForUpdates);

    // Window management
    public const string Close = nameof(Close);
    public const string CloseWindow = nameof(CloseWindow);
    public const string NewWindow = nameof(NewWindow);
    public const string Center = nameof(Center);
    public const string SetTopMost = nameof(SetTopMost);
    public const string ToggleFullscreen = nameof(ToggleFullscreen);
    public const string Maximize = nameof(Maximize);
    public const string Minimize = nameof(Minimize);
    public const string Restore = nameof(Restore);
    public const string AutoFitWindow = nameof(AutoFitWindow);
    public const string NormalWindow = nameof(NormalWindow);

    // Star rating
    public const string Set0Star = nameof(Set0Star);
    public const string Set1Star = nameof(Set1Star);
    public const string Set2Star = nameof(Set2Star);
    public const string Set3Star = nameof(Set3Star);
    public const string Set4Star = nameof(Set4Star);
    public const string Set5Star = nameof(Set5Star);

    // Wallpaper and lock screen
    public const string SetAsWallpaper = nameof(SetAsWallpaper);
    public const string SetAsWallpaperTiled = nameof(SetAsWallpaperTiled);
    public const string SetAsWallpaperCentered = nameof(SetAsWallpaperCentered);
    public const string SetAsWallpaperStretched = nameof(SetAsWallpaperStretched);
    public const string SetAsWallpaperFitted = nameof(SetAsWallpaperFitted);
    public const string SetAsWallpaperFilled = nameof(SetAsWallpaperFilled);
    public const string SetAsLockScreen = nameof(SetAsLockScreen);
    public const string SetAsLockscreenCentered = nameof(SetAsLockscreenCentered);

    // Settings and system
    public const string ToggleSubdirectories = nameof(ToggleSubdirectories);
    public const string ToggleTaskbarProgress = nameof(ToggleTaskbarProgress);
    public const string ToggleFileHistory = nameof(ToggleFileHistory);
    public const string ToggleOpeningInSameWindow = nameof(ToggleOpeningInSameWindow);
    public const string ToggleShowFullPathInTitleBar = nameof(ToggleShowFullPathInTitleBar);
    public const string ResetSettings = nameof(ResetSettings);
    public const string Restart = nameof(Restart);
    public const string ShowSettingsFile = nameof(ShowSettingsFile);
    public const string ShowKeybindingsFile = nameof(ShowKeybindingsFile);
    public const string ShowRecentHistoryFile = nameof(ShowRecentHistoryFile);
    public const string ShowFavoritesFile = nameof(ShowFavoritesFile);
}
