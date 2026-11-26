using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Core.SettingsSearch;

public static class SettingsSearchIndex
{
    /// <summary>
    /// Builds the suggestions in the current language: one for each category and one for each
    /// <see cref="SettingKey"/>. The options of a setting are its keywords, not suggestions of their own.
    /// A text the language lacks leaves its suggestion out.
    /// </summary>
    public static List<SettingsSearchItem> Build()
    {
        var t = TranslationManager.Translation;
        var list = new List<SettingsSearchItem>(80);
        string? categoryName = null;
        var category = SettingsCategory.General;

        // General
        StartCategory(SettingsCategory.General, t.GeneralSettings);
        Add(SettingKey.ApplicationStartup, t.ApplicationStartup,
            t.Start, t.Open, t.OpenLastFile, t.ContinueWhereYouLeftOff, t.None, "Boot Launch Session");
        Add(SettingKey.ConfirmPermanentDelete, t.ShowConfirmationDialogWhenPermanentlyDeletingFile,
            t.FileManagement, t.PermanentlyDelete, t.DeleteFile, "Delete Confirm Dialog");
        Add(SettingKey.ConfirmRecycle, t.ShowConfirmationDialogWhenMovingFileToRecycleBin,
            t.FileManagement, t.MoveToRecycleBin, t.DeleteFile, "Delete Trash Confirm Dialog");
        Add(SettingKey.FileHistory, t.OpenFileHistory,
            t.FileManagement, t.FileHistoryEnabled, t.FileHistoryDisabled, "Recent History");
        Add(SettingKey.FullPathInTitleBar, t.ShowFullPathInTitleBar,
            t.FileManagement, t.File, "Title Path");
        Add(SettingKey.WhenDeletingFile, t.WhenDeletingAFile,
            t.FileManagement, t.NavigateForwards, t.NavigateBackwards, t.DeletedFile, "Delete Next Previous");

        // Appearance
        StartCategory(SettingsCategory.Appearance, t.Appearance);
        Add(SettingKey.Theme, t.Theme,
            t.DarkTheme, t.LightTheme, t.GlassTheme, "Dark Light Glass Mode");
        Add(SettingKey.HighlightColor, t.HighlightColor,
            t.Color, "Accent Colour");
        Add(SettingKey.Background, t.ChangeBackground,
            "Background Texture Wallpaper Checkerboard Transparent");
        Add(SettingKey.ConstrainBackground, t.ConstrainBackgroundToImage,
            "Background Constrain");
        Add(SettingKey.WindowBlur, t.BlurWindowBackground,
            t.Blur, "Acrylic Transparency Translucent");

        // Interface
        StartCategory(SettingsCategory.Interface, t.InterfaceConfiguration);
        Add(SettingKey.BottomToolbar, t.ShowBottomToolbar,
            "Interface UI Toolbar");
        Add(SettingKey.ShowInterface, t.ShowUI,
            t.HideUI, "Interface UI Hidden");
        Add(SettingKey.FadeInButtons, t.ShowFadeInButtonsOnHover,
            t.DisableFadeInButtonsOnHover, "Interface UI Buttons Fade");
        Add(SettingKey.HoverNavigationBar, t.ShowHoverNavigationBar,
            "Interface UI Hover Navigation");

        // Image
        StartCategory(SettingsCategory.Image, t.Image);
        Add(SettingKey.Scrolling, t.Scrolling,
            t.ScrollingEnabled, t.ScrollingDisabled, "Scroll");
        Add(SettingKey.SideBySide, t.SideBySide,
            "SideBySide Two Pages Spread");
        Add(SettingKey.AutoPlayMotionPhotos, t.AutoPlayMotionPhotos,
            t.MotionPhoto, "Motion Photo Live Photo Video Autoplay");
        Add(SettingKey.ImageAliasing, t.ImageAliasing,
            t.HighQuality, t.NearestNeighbor, "Scaling Pixelated Nearest Neighbor");

        // Window
        StartCategory(SettingsCategory.Window, t.Window);
        Add(SettingKey.AutoFitWindow, t.AutoFitWindow,
            t.WindowScaling, "Fit Resize");
        Add(SettingKey.WindowMargin, t.WindowMargin,
            t.WindowScaling, "Margin");
        Add(SettingKey.OpeningMode, t.OpenInSameWindow,
            t.WindowManagement, t.OpenInNewWindow, t.OpenInNewTab, t.NewWindow, t.NewTab);
        Add(SettingKey.StayTopMost, t.StayTopMost,
            t.WindowManagement, "Topmost Always");
        Add(SettingKey.StayCentered, t.StayCentered,
            t.WindowManagement, "Center");
        Add(SettingKey.ConfirmOnEsc, t.ShowConfirmationOnEsc,
            t.WindowManagement, "Escape Exit Close");
        Add(SettingKey.EscLeavesFullscreen, t.EscLeavesFullscreen,
            t.WindowManagement, t.Fullscreen, "Escape Exit Restore");

        // Gallery
        StartCategory(SettingsCategory.Gallery, t.GallerySettings);
        Add(SettingKey.GalleryDockPosition, t.Orientation,
            t.ShowDockedGalleryAtBottom, t.ShowDockedGalleryAtTop, t.ShowDockedGalleryToTheLeft,
            t.ShowDockedGalleryToTheRight, t.Bottom, t.Top, t.Left, t.Right, "Position Dock");
        Add(SettingKey.ShowDockedGallery, t.ShowDockedGallery,
            t.HideDockedGallery, t.ShowDockedGalleryWhenUiIsHidden, "Hide Docked");
        Add(SettingKey.MotionPhotoBadges, t.ShowMotionPhotoBadges,
            t.MotionPhoto, "Badge Live Photo");
        Add(SettingKey.DockedGalleryItemSize, t.DockedGalleryItemSize,
            "Size Height Thumbnail");
        Add(SettingKey.ExpandedGalleryItemSize, t.ExpandedGalleryItemSize,
            "Size Height Thumbnail");
        Add(SettingKey.GalleryItemSpacing, t.ItemSpacing,
            "Margin Padding Gap");
        Add(SettingKey.GalleryLineSpacing, t.LineSpacing,
            "Margin Padding Gap");
        Add(SettingKey.GalleryDisplayMode, t.GalleryDisplayMode,
            t.Grid, t.Masonry, t.Rows, "Justified Waterfall Mosaic Tiles Layout");
        Add(SettingKey.GalleryThumbnailStretch, t.GalleryThumbnailStretch,
            t.Uniform, t.UniformToFill, t.Square, t.FillSquare, "Crop");
        Add(SettingKey.DockedGalleryThumbnailStretch, t.DockedGalleryThumbnailStretch,
            t.Uniform, t.UniformToFill, t.Square, t.FillSquare, "Crop");
        Add(SettingKey.GalleryMouseWheel, t.GalleryMouseWheel,
            t.Mouse, t.MouseWheel, "Scroll");

        // Navigation
        StartCategory(SettingsCategory.Navigation, t.Navigation);
        Add(SettingKey.SearchSubdirectory, t.SearchSubdirectory,
            t.Folder, "Subfolder Recursive");
        Add(SettingKey.Looping, t.Looping,
            t.ToggleLooping, t.LoopingEnabled, t.LoopingDisabled, "Loop Wrap");
        Add(SettingKey.TaskbarProgress, t.ToggleTaskbarProgress,
            "Taskbar Progress");
        Add(SettingKey.ArchiveFolderStructure, t.RespectArchiveFolderStructure,
            "Archive Extract Zip Uncompress Order");
        Add(SettingKey.NavigateByClicking, t.NavigateByClicking,
            "Clicking Mouse");
        Add(SettingKey.NavigationSpeed, t.AdjustNavSpeed,
            "Time Delay Repeat");

        // Zoom
        StartCategory(SettingsCategory.Zoom, t.Zoom);
        Add(SettingKey.ZoomToFit, t.ZoomToFit,
            t.Stretch, t.Scale, "Enlarge Upscale");
        Add(SettingKey.ResetZoom, t.ResetZoomOnChange,
            "Reset");
        Add(SettingKey.AllowZoomOut, t.AllowZoomOut,
            "Out Minimum");
        Add(SettingKey.AnimatedZoom, t.UseAnimatedZoom,
            "Animation Smooth");
        Add(SettingKey.ZoomPercentagePopup, t.ShowZoomPercentagePopup,
            "Percentage Popup");
        Add(SettingKey.ZoomPreviewer, t.ShowZoomPreviewer,
            "Preview Minimap Popup pop-up");
        Add(SettingKey.ZoomSpeed, t.AdjustTimingForZoom,
            "Time Duration");

        // Mouse
        StartCategory(SettingsCategory.Mouse, t.Mouse);
        Add(SettingKey.DoubleClick, t.DoubleClick,
            t.ResetZoom, t.ToggleFullscreen, t.None, "Click");
        Add(SettingKey.Touchpad, t.UsingTouchpad,
            t.UsingMouse, "Trackpad");

        // A category that is one row has no setting of its own: the category leads to it
        StartCategory(SettingsCategory.Language, t.Language, "Translate Translation Locale");
        StartCategory(SettingsCategory.Keybindings, t.ApplicationShortcuts, "Key Keybindings Hotkeys Keyboard");
        StartCategory(SettingsCategory.FileAssociations, t.FileAssociations, "Extension Default Program");

        return list;

        void StartCategory(SettingsCategory next, string? name, string keywords = "")
        {
            category = next;
            categoryName = name;
            if (name is not null)
            {
                list.Add(new SettingsSearchItem(next, null, name, string.Empty, keywords));
            }
        }

        void Add(SettingKey key, string? name, params ReadOnlySpan<string?> keywords)
        {
            if (name is null)
            {
                return;
            }

            // Some labels end in a colon, which reads oddly in a list
            list.Add(new SettingsSearchItem(category, key, name.TrimEnd(':', ' '), categoryName ?? string.Empty,
                string.Join(' ', keywords)));
        }
    }

    /// <summary>
    /// The suggestions that have every word of <paramref name="query"/>, in any order and ignoring case. A category
    /// is found by its name and keywords; a setting by its name, its keywords and its category's name. Categories
    /// come first, then settings whose name has every word, then the rest, each in the order they were built.
    /// </summary>
    public static List<SettingsSearchItem> Search(IReadOnlyList<SettingsSearchItem> items, string? query)
    {
        var found = new List<SettingsSearchItem>();
        if (string.IsNullOrWhiteSpace(query))
        {
            return found;
        }

        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var nameMatches = new List<SettingsSearchItem>();
        var keywordMatches = new List<SettingsSearchItem>();

        foreach (var item in items)
        {
            var inName = true;
            var inAny = true;
            foreach (var word in words)
            {
                if (Has(item.Name, word))
                {
                    continue;
                }

                inName = false;
                if (!Has(item.Keywords, word) && !Has(item.CategoryName, word))
                {
                    inAny = false;
                    break;
                }
            }

            if (!inAny)
            {
                continue;
            }

            if (item.IsCategory)
            {
                found.Add(item);
            }
            else if (inName)
            {
                nameMatches.Add(item);
            }
            else
            {
                keywordMatches.Add(item);
            }
        }

        found.AddRange(nameMatches);
        found.AddRange(keywordMatches);
        return found;
    }

    private static bool Has(string text, string word)
    {
        return text.Contains(word, StringComparison.InvariantCultureIgnoreCase);
    }
}
