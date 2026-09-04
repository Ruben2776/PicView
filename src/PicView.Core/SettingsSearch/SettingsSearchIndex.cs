using PicView.Core.Localization;

namespace PicView.Core.SettingsSearch;

public static class SettingsSearchIndex
{
    public static List<SettingsSearchItem> Build(SettingsSearchData data)
    {
        var list = new List<SettingsSearchItem>();
        var t = TranslationManager.Translation;

        if (t == null) return list;

        // Startup
        if (t.OpenLastFile != null)
            list.Add(new SettingsSearchItem(t.OpenLastFile, data.StartUpSearchTags));
        if (t.ApplicationStartup != null)
            list.Add(new SettingsSearchItem(t.ApplicationStartup, data.StartUpSearchTags));

        // Delete File Dialog
        if (t.ShowConfirmationDialogWhenPermanentlyDeletingFile != null)
            list.Add(new SettingsSearchItem(t.ShowConfirmationDialogWhenPermanentlyDeletingFile, data.DeleteFileDialogSearchTags));
        if (t.ShowConfirmationDialogWhenMovingFileToRecycleBin != null)
            list.Add(new SettingsSearchItem(t.ShowConfirmationDialogWhenMovingFileToRecycleBin, data.DeleteFileDialogSearchTags));
        if (t.PermanentlyDelete != null)
            list.Add(new SettingsSearchItem(t.PermanentlyDelete, data.DeleteFileDialogSearchTags));
        if (t.DeletedFile != null)
            list.Add(new SettingsSearchItem(t.DeletedFile, data.DeleteFileDialogSearchTags));

        // SubDirectory
        if (t.SearchSubdirectory != null)
            list.Add(new SettingsSearchItem(t.SearchSubdirectory, data.SubDirectorySearchTags));

        // File History
        if (t.OpenFileHistory != null)
            list.Add(new SettingsSearchItem(t.OpenFileHistory, data.FileHistorySearchTags));
        
        if (t.ShowFullPathInTitleBar != null)
            list.Add(new SettingsSearchItem(t.ShowFullPathInTitleBar, data.FullPathInTitleBarySearchTags));

        // When Deleting File (Navigation)
        if (t.WhenDeletingAFile != null)
            list.Add(new SettingsSearchItem(t.WhenDeletingAFile, data.WhenDeletingFileSearchTags));
        if (t.NavigateForwards != null)
            list.Add(new SettingsSearchItem(t.NavigateForwards, data.WhenDeletingFileSearchTags));
        if (t.NavigateBackwards != null)
            list.Add(new SettingsSearchItem(t.NavigateBackwards, data.WhenDeletingFileSearchTags));

        // Theme / Appearance
        if (t.InterfaceConfiguration != null)
            list.Add(new SettingsSearchItem(t.InterfaceConfiguration, data.ColorSearchTags));
        if (t.Color != null)
            list.Add(new SettingsSearchItem(t.Color, data.ColorSearchTags));
        if (t.Theme != null)
            list.Add(new SettingsSearchItem(t.Theme, data.ThemeSearchTags));
        if (t.DarkTheme != null)
            list.Add(new SettingsSearchItem(t.DarkTheme, data.ThemeSearchTags));
        if (t.LightTheme != null)
            list.Add(new SettingsSearchItem(t.LightTheme, data.ThemeSearchTags));
        if (t.GlassTheme != null)
            list.Add(new SettingsSearchItem(t.GlassTheme, data.ThemeSearchTags));

        // Background
        if (t.ChangeBackground != null)
            list.Add(new SettingsSearchItem(t.ChangeBackground, data.BackgroundSearchTags));
        if (t.ConstrainBackgroundToImage != null)
            list.Add(new SettingsSearchItem(t.ConstrainBackgroundToImage, data.BackgroundConstrainSearchTags));

        // UI Configuration
        if (t.ShowBottomToolbar != null)
            list.Add(new SettingsSearchItem(t.ShowBottomToolbar, data.ShowBottomToolbarSearchTags));
        if (t.ShowUI != null)
            list.Add(new SettingsSearchItem(t.ShowUI, data.ShowUISearchTags));
        if (t.HideUI != null)
            list.Add(new SettingsSearchItem(t.HideUI, data.ShowUISearchTags));
        if (t.ShowFadeInButtonsOnHover != null)
            list.Add(new SettingsSearchItem(t.ShowFadeInButtonsOnHover, data.ShowFadeInButtonsSearchTags));
        if (t.ShowHoverNavigationBar != null)
            list.Add(new SettingsSearchItem(t.ShowHoverNavigationBar, data.ShowHoverNavigationBarSearchTags));

        // Image Settings
        if (t.Stretch != null)
            list.Add(new SettingsSearchItem(t.Stretch, data.ImageZoomToFitSearchTags));
        if (t.Scrolling != null)
            list.Add(new SettingsSearchItem(t.Scrolling, data.ImageScrollingSearchTags));
        if (t.SideBySide != null)
            list.Add(new SettingsSearchItem(t.SideBySide, data.ImageSideBySideSearchTags));
        if (t.ImageAliasing != null)
            list.Add(new SettingsSearchItem(t.ImageAliasing, data.ImageScalingSearchTags));
        if (t.HighQuality != null)
            list.Add(new SettingsSearchItem(t.HighQuality, data.ImageScalingSearchTags));
        if (t.NearestNeighbor != null)
            list.Add(new SettingsSearchItem(t.NearestNeighbor, data.ImageScalingSearchTags));
        if (t.AutoPlayMotionPhotos != null)
            list.Add(new SettingsSearchItem(t.AutoPlayMotionPhotos, data.MotionPhotoSearchTags));
        if (t.MotionPhoto != null)
            list.Add(new SettingsSearchItem(t.MotionPhoto, data.MotionPhotoSearchTags));

        // Navigation
        if (t.Navigation != null && t.SearchSubdirectory != null)
            list.Add(new SettingsSearchItem(t.Navigation + " " + t.SearchSubdirectory, data.NavigationSubdirectorySearchTags));
        if (t.ToggleLooping != null)
            list.Add(new SettingsSearchItem(t.ToggleLooping, data.NavigationLoopSearchTags));
        if (t.ToggleTaskbarProgress != null)
            list.Add(new SettingsSearchItem(t.ToggleTaskbarProgress, data.NavigationTaskbarSearchTags));
        if (t.AdjustNavSpeed != null)
            list.Add(new SettingsSearchItem(t.AdjustNavSpeed, data.NavigationSpeedSearchTags));
        if (t.AlwaysUncompressEntireArchive != null)
            list.Add(new SettingsSearchItem(t.AlwaysUncompressEntireArchive, data.NavigationArchiveSearchTags));

        // Gallery
        if (t.ShowDockedGallery != null)
            list.Add(new SettingsSearchItem(t.ShowDockedGallery, data.GalleryVisibilitySearchTags));
        if (t.ShowDockedGalleryWhenUiIsHidden != null)
            list.Add(new SettingsSearchItem(t.ShowDockedGalleryWhenUiIsHidden, data.GalleryVisibilitySearchTags));
        if (t.ExpandedGalleryItemSize != null)
            list.Add(new SettingsSearchItem(t.ExpandedGalleryItemSize, data.GallerySizeSearchTags));
        if (t.DockedGalleryItemSize != null)
            list.Add(new SettingsSearchItem(t.DockedGalleryItemSize, data.GallerySizeSearchTags));
        if (t.GalleryThumbnailStretch != null)
            list.Add(new SettingsSearchItem(t.GalleryThumbnailStretch, data.GalleryStretchSearchTags));
        if (t.DockedGalleryThumbnailStretch != null)
            list.Add(new SettingsSearchItem(t.DockedGalleryThumbnailStretch, data.GalleryStretchSearchTags));

        // Slideshow
        if (t.AdjustTimingForSlideshow != null)
            list.Add(new SettingsSearchItem(t.AdjustTimingForSlideshow, data.SlideshowSearchTags));

        // Window
        if (t.WindowScaling != null)
            list.Add(new SettingsSearchItem(t.WindowScaling, data.WindowScalingSearchTags));
        if (t.AutoFitWindow != null)
            list.Add(new SettingsSearchItem(t.AutoFitWindow, data.WindowScalingSearchTags));
        if (t.WindowMargin != null)
            list.Add(new SettingsSearchItem(t.WindowMargin, data.WindowScalingSearchTags));
        if (t.StayTopMost != null)
            list.Add(new SettingsSearchItem(t.StayTopMost, data.WindowTopMostSearchTags));
        if (t.StayCentered != null)
            list.Add(new SettingsSearchItem(t.StayCentered, data.WindowCenteredSearchTags));
        if (t.OpenInSameWindow != null)
            list.Add(new SettingsSearchItem(t.OpenInSameWindow, data.WindowSameWindowSearchTags));
        if (t.ShowConfirmationOnEsc != null)
            list.Add(new SettingsSearchItem(t.ShowConfirmationOnEsc, data.WindowEscSearchTags));

        // Zoom
        if (t.ResetZoomOnChange != null)
            list.Add(new SettingsSearchItem(t.ResetZoomOnChange, data.ZoomResetSearchTags));
        if (t.AllowZoomOut != null)
            list.Add(new SettingsSearchItem(t.AllowZoomOut, data.ZoomOutSearchTags));
        if (t.UseAnimatedZoom != null)
            list.Add(new SettingsSearchItem(t.UseAnimatedZoom, data.ZoomAnimationSearchTags));
        if (t.ShowZoomPercentagePopup != null)
            list.Add(new SettingsSearchItem(t.ShowZoomPercentagePopup, data.ZoomPopupSearchTags));
        if (t.AdjustTimingForZoom != null)
            list.Add(new SettingsSearchItem(t.AdjustTimingForZoom, data.ZoomSpeedSearchTags));

        // Mouse
        if (t.DoubleClick != null)
            list.Add(new SettingsSearchItem(t.DoubleClick, data.MouseDoubleClickSearchTags));
        if (t.NavigateFileHistory != null)
            list.Add(new SettingsSearchItem(t.NavigateFileHistory, data.MouseNavigationSearchTags));
        if (t.NavigateBetweenDirectories != null)
            list.Add(new SettingsSearchItem(t.NavigateBetweenDirectories, data.MouseNavigationSearchTags));
        if (t.MouseWheel != null)
            list.Add(new SettingsSearchItem(t.MouseWheel, data.MouseWheelBehaviorSearchTags));
        if (t.ScrollDirection != null)
            list.Add(new SettingsSearchItem(t.ScrollDirection, data.MouseScrollDirectionSearchTags));
        if (t.UsingTouchpad != null)
            list.Add(new SettingsSearchItem(t.UsingTouchpad, data.MouseTouchpadSearchTags));

        // Language
        if (t.Language != null)
            list.Add(new SettingsSearchItem(t.Language, data.LanguageSearchTags));
        
        // Keybindings
        if (t.ApplicationShortcuts != null)
            list.Add(new SettingsSearchItem(t.ApplicationShortcuts, data.KeybindingsSearchTags));

        return list;
    }
}
