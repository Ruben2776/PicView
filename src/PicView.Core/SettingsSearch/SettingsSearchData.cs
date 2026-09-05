using Cysharp.Text;
using PicView.Core.Localization;

namespace PicView.Core.SettingsSearch;

public class SettingsSearchData
{
    // Search properties (Tags and Visibility)
    public string StartUpSearchTags { get; }
    public string DeleteFileDialogSearchTags { get; }
    public string SubDirectorySearchTags { get; }
    public string FileHistorySearchTags { get; }
    public string FullPathInTitleBarySearchTags { get; }
    public string WhenDeletingFileSearchTags { get; }
    public string ThemeSearchTags { get; }
    public string ColorSearchTags { get; }
    public string BackgroundSearchTags { get; }
    public string BackgroundConstrainSearchTags { get; }
    public string ShowBottomToolbarSearchTags { get; }
    public string ShowUISearchTags { get; }
    public string ShowFadeInButtonsSearchTags { get; }
    public string ShowHoverNavigationBarSearchTags { get; }
    public string ImageZoomToFitSearchTags { get; }
    public string ImageScrollingSearchTags { get; }
    public string ImageSideBySideSearchTags { get; }
    public string ImageScalingSearchTags { get; }
    public string MotionPhotoSearchTags { get; }
    public string NavigationSubdirectorySearchTags { get; }
    public string NavigationLoopSearchTags { get; }
    public string NavigationTaskbarSearchTags { get; }
    public string NavigationSpeedSearchTags { get; }
    public string NavigationArchiveSearchTags { get; }
    public string GalleryVisibilitySearchTags { get; }
    public string GalleryDockSearchTags { get; }
    public string GallerySizeSearchTags { get; }
    public string GalleryStretchSearchTags { get; }
    public string SlideshowSearchTags { get; }
    public string WindowScalingSearchTags { get; }
    public string WindowTopMostSearchTags { get; }
    public string WindowCenteredSearchTags { get; }
    public string WindowSameWindowSearchTags { get; }
    public string WindowEscSearchTags { get; }
    public string ZoomResetSearchTags { get; }
    public string ZoomOutSearchTags { get; }
    public string ZoomAnimationSearchTags { get; }
    public string ZoomPopupSearchTags { get; }
    public string ZoomPreviewerSearchTags { get; }
    public string ZoomSpeedSearchTags { get; }
    public string MouseDoubleClickSearchTags { get; }
    public string MouseNavigationSearchTags { get; }
    public string MouseWheelBehaviorSearchTags { get; }
    public string MouseScrollDirectionSearchTags { get; }
    public string MouseTouchpadSearchTags { get; }
    public string LanguageSearchTags { get; }
    public string KeybindingsSearchTags { get; }
    public string FileAssociationSearchTags { get; }
    
    public SettingsSearchData()
    {
        // Search Initialization
        const string space = " ";
        var sb = ZString.CreateUtf8StringBuilder();
        
        sb.Append(TranslationManager.Translation.ApplicationStartup);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Start);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Open);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.OpenLastFile);
        sb.Append(space);
        sb.Append("Boot");
        sb.Append(space);
        sb.Append("Launch");
        StartUpSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.DeletedFile);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.DeleteFilePermanently);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.PermanentlyDelete);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ShowConfirmationDialogWhenMovingFileToRecycleBin);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ShowConfirmationDialogWhenPermanentlyDeletingFile);
        DeleteFileDialogSearchTags = sb.ToString();
        
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Navigation);
        sb.Append(TranslationManager.Translation.WhenDeletingAFile);
        WhenDeletingFileSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.SearchSubdirectory);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Folder);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.FileManagement);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Navigation);
        SubDirectorySearchTags = sb.ToString();
        
        sb.Append(TranslationManager.Translation.OpenFileHistory);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.FileManagement);
        FileHistorySearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.ShowFullPathInTitleBar);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.File);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.FileManagement);
        FullPathInTitleBarySearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.InterfaceConfiguration);
        sb.Append(space);
        sb.Append("UI");
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Color);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Theme);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.HighlightColor);
        ColorSearchTags = sb.ToString();
        
        sb.Append(TranslationManager.Translation.DarkTheme);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.GlassTheme);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.LightTheme);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Theme);
        ThemeSearchTags = sb.ToString();
        
        sb.Append(TranslationManager.Translation.ChangeBackground);
        sb.Append(space);
        sb.Append("Background Texture Wallpaper");
        BackgroundSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.InterfaceConfiguration);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ShowBottomToolbar);
        sb.Append(space);
        sb.Append("Interface UI Toolbar");
        ShowBottomToolbarSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.InterfaceConfiguration);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ShowUI);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.HideUI);
        sb.Append(space);
        sb.Append("Interface UI Hidden");
        ShowUISearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.InterfaceConfiguration);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ShowFadeInButtonsOnHover);
        sb.Append(space);
        sb.Append("Interface UI Buttons Fade");
        ShowFadeInButtonsSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.InterfaceConfiguration);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ShowHoverNavigationBar);
        sb.Append(space);
        sb.Append("Interface UI Hover Navigation");
        ShowHoverNavigationBarSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Image);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ZoomToFit);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Stretch);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Scale);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Zoom);
        ImageZoomToFitSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Image);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Scrolling);
        sb.Append(space);
        sb.Append("Scroll");
        ImageScrollingSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Image);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.SideBySide);
        sb.Append(space);
        sb.Append("SideBySide");
        ImageSideBySideSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.ImageAliasing);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.HighQuality);
        sb.Append(space);
        sb.Append("Scaling Pixelated Nearest Neighbor");
        ImageScalingSearchTags = sb.ToString();

        sb.Clear();

        sb.Append(TranslationManager.Translation.Image);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.MotionPhoto);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.AutoPlayMotionPhotos);
        sb.Append(space);
        sb.Append("Motion Photo Live Photo Video Autoplay");
        MotionPhotoSearchTags = sb.ToString();

        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Navigation);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.SearchSubdirectory);
        sb.Append(space);
        sb.Append("Folder File system");
        NavigationSubdirectorySearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Navigation);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ToggleLooping);
        sb.Append(space);
        sb.Append("Loop");
        NavigationLoopSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Navigation);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ToggleTaskbarProgress);
        sb.Append(space);
        sb.Append("Taskbar Progress");
        NavigationTaskbarSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Navigation);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.AdjustNavSpeed);
        sb.Append(space);
        sb.Append("Speed Time");
        NavigationSpeedSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Navigation);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.AlwaysUncompressEntireArchive);
        sb.Append(space);
        sb.Append("Archive Extract Zip Uncompress");
        NavigationArchiveSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.GallerySettings);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ShowDockedGallery);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ShowDockedGalleryWhenUiIsHidden);
        sb.Append(space);
        sb.Append("Gallery Hide Show UI Docked");
        GalleryVisibilitySearchTags = sb.ToString();
        
        sb.Clear();

        sb.Append(TranslationManager.Translation.GallerySettings);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Orientation);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Bottom);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Top);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Left);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.Right);
        sb.Append(space);
        sb.Append("Position Dock Gallery");
        GalleryDockSearchTags = sb.ToString();
        
        sb.Clear();

        sb.Append(TranslationManager.Translation.GallerySettings);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ExpandedGalleryItemSize);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.DockedGalleryItemSize);
        sb.Append(space);
        sb.Append("Size Height Thumbnail Spacing Item Line Margin Padding");
        GallerySizeSearchTags = sb.ToString();
        
        sb.Clear();

        sb.Append(TranslationManager.Translation.GallerySettings);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.GalleryThumbnailStretch);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.DockedGalleryThumbnailStretch);
        sb.Append(space);
        sb.Append("Stretch Fill Uniform");
        GalleryStretchSearchTags = sb.ToString();
        
        sb.Clear();

        sb.Append(TranslationManager.Translation.Slideshow);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.AdjustTimingForSlideshow);
        sb.Append(space);
        sb.Append("Timer Speed Presentation");
        SlideshowSearchTags = sb.ToString();
        
        sb.Clear();

        sb.Append(TranslationManager.Translation.WindowScaling);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.AutoFitWindow);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.WindowMargin);
        sb.Append(space);
        sb.Append("Fit Margin");
        WindowScalingSearchTags = sb.ToString();
        
        sb.Clear();

        sb.Append(TranslationManager.Translation.WindowManagement);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.StayTopMost);
        sb.Append(space);
        sb.Append("TopOn");
        WindowTopMostSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.WindowManagement);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.StayCentered);
        sb.Append(space);
        sb.Append("Center");
        WindowCenteredSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.WindowManagement);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.OpenInSameWindow);
        sb.Append(space);
        sb.Append("New Window");
        WindowSameWindowSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.WindowManagement);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ShowConfirmationOnEsc);
        sb.Append(space);
        sb.Append("Escape Exit");
        WindowEscSearchTags = sb.ToString();
        
        sb.Clear();

        sb.Append(TranslationManager.Translation.Zoom);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ResetZoomOnChange);
        sb.Append(space);
        sb.Append("Reset");
        ZoomResetSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Zoom);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.AllowZoomOut);
        sb.Append(space);
        sb.Append("Out");
        ZoomOutSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Zoom);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.UseAnimatedZoom);
        sb.Append(space);
        sb.Append("Animation");
        ZoomAnimationSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Zoom);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ShowZoomPercentagePopup);
        sb.Append(space);
        sb.Append("Percentage Popup pop-up");
        ZoomPopupSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Zoom);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.ShowZoomPreviewer);
        sb.Append(space);
        sb.Append("Popup pop-up");
        ZoomPreviewerSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Zoom);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.AdjustTimingForZoom);
        sb.Append(space);
        sb.Append("Speed Time");
        ZoomSpeedSearchTags = sb.ToString();
        
        sb.Clear();

        sb.Append(TranslationManager.Translation.DoubleClick);
        sb.Append(space);
        sb.Append("Click");
        MouseDoubleClickSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.MouseSideButtons);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.NavigateFileHistory);
        sb.Append(space);
        sb.Append(TranslationManager.Translation.NavigateBetweenDirectories);
        MouseNavigationSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.MouseWheel);
        sb.Append(space);
        sb.Append("Wheel Scroll");
        MouseWheelBehaviorSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.ScrollDirection);
        sb.Append(space);
        sb.Append("Scroll Direction Reverse");
        MouseScrollDirectionSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.UsingTouchpad);
        sb.Append(space);
        sb.Append("Trackpad Touchpad");
        MouseTouchpadSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.Language);
        sb.Append(space);
        sb.Append("Translate Locale");
        LanguageSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.ConstrainBackgroundToImage);
        sb.Append(space);
        sb.Append("Background Constrain");
        BackgroundConstrainSearchTags = sb.ToString();
        
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.ApplicationShortcuts);
        sb.Append(space);
        sb.Append("Key keybindings");
        KeybindingsSearchTags = sb.ToString();
                
        sb.Clear();
        
        sb.Append(TranslationManager.Translation.FileAssociations);
        FileAssociationSearchTags = sb.ToString();
        
        sb.Dispose();
    }
}
