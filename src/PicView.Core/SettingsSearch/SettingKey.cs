namespace PicView.Core.SettingsSearch;

/// <summary>
/// Names a setting row in the Settings window, so a suggestion and the row it leads to share one name instead of
/// matching text. A row that is the whole of its category has no key: its category suggestion leads to it.
/// </summary>
public enum SettingKey
{
    // General
    ApplicationStartup,
    ConfirmPermanentDelete,
    ConfirmRecycle,
    FileHistory,
    FullPathInTitleBar,
    WhenDeletingFile,

    // Appearance
    Theme,
    HighlightColor,
    Background,
    ConstrainBackground,
    WindowBlur,

    // Interface
    BottomToolbar,
    ShowInterface,
    FadeInButtons,
    HoverNavigationBar,

    // Image
    Scrolling,
    SideBySide,
    SideBySideOrder,
    AutoPlayMotionPhotos,
    ImageAliasing,

    // Window
    AutoFitWindow,
    WindowMargin,
    OpeningMode,
    StayTopMost,
    StayCentered,
    ConfirmOnEsc,
    EscLeavesFullscreen,

    // Gallery
    GalleryDockPosition,
    ShowDockedGallery,
    MotionPhotoBadges,
    DockedGalleryItemSize,
    ExpandedGalleryItemSize,
    GalleryItemSpacing,
    GalleryLineSpacing,
    GalleryDisplayMode,
    GalleryThumbnailStretch,
    DockedGalleryThumbnailStretch,
    GalleryMouseWheel,

    // Navigation
    SearchSubdirectory,
    Looping,
    TaskbarProgress,
    ArchiveFolderStructure,
    NavigateByClicking,
    NavigationSpeed,

    // Zoom
    ZoomToFit,
    ResetZoom,
    AllowZoomOut,
    AnimatedZoom,
    ZoomPercentagePopup,
    ZoomPreviewer,
    ZoomSpeed,

    // Mouse
    DoubleClick,
    Touchpad
}
