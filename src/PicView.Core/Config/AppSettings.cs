namespace PicView.Core.Config;

public class AppSettings
{
    /// <summary>
    /// The version of the configurable settings, not the application itself.
    /// </summary>
    public double Version { get; set; } = SettingsConfiguration.CurrentSettingsVersion;
    public WindowProperties? WindowProperties { get; set; }
    public UIProperties? UIProperties { get; set; }
    public Theme? Theme { get; set; }
    public Gallery? Gallery { get; set; }
    public ImageScaling? ImageScaling { get; set; }
    public Sorting? Sorting { get; set; }
    public Zoom? Zoom { get; set; }
    public StartUp? StartUp { get; set; }
    public Navigation? Navigation { get; set; }
}

public class WindowProperties
{
    /// <summary>
    /// Gets or sets the vertical position of the window relative to the top edge of the screen.
    /// </summary>
    /// <remarks>
    /// This property determines the Y-coordinate of the window's top boundary in screen pixels.
    /// Adjusting this value changes the vertical placement of the window on the screen.
    /// </remarks>
    public double Top { get; set; } = 0;

    /// <summary>
    /// Gets or sets the horizontal position of the window relative to the left edge of the screen.
    /// </summary>
    /// <remarks>
    /// This property represents the X-coordinate of the window's left boundary in screen pixels.
    /// Modifying this value adjusts the horizontal placement of the window on the screen.
    /// </remarks>
    public double Left { get; set; } = 0;

    /// <summary>
    /// Gets or sets the width of the window in pixels.
    /// </summary>
    public double Width { get; set; } = 750;

    /// <summary>
    /// Gets or sets the height of the window in pixels.
    /// </summary>
    public double Height { get; set; } = 1024;

    /// <summary>
    /// Determines whether the application window should automatically adjust
    /// its size to fit the displayed content.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, the window automatically resizes to fit the content, disabling manual resizing.
    /// When set to <c>false</c> the image will be centered in the container, and manual resizing and window behavior is enabled
    /// </remarks>
    public bool AutoFit { get; set; } = true;

    /// <summary>
    /// Determines whether the window should be displayed as the top-most window.
    /// </summary>
    /// <remarks>
    /// When this property is set to true, the window stays above all other non-topmost windows, even when it is not active.
    /// Setting it to false removes the top-most behavior, allowing other windows to overlay it.
    /// </remarks>
    public bool TopMost { get; set; } = false;

    /// <summary>
    /// Determines whether the window is in a maximized state.
    /// </summary>
    /// <remarks>
    /// This property determines if the window occupies the entire working area of the screen,
    /// excluding system-reserved spaces like the taskbar or dock. When set to true, the window
    /// adapts its dimensions to match the available screen area within these constraints.
    /// </remarks>
    public bool Maximized { get; set; } = false;

    /// <summary>
    /// Determines whether the window is displayed in fullscreen mode.
    /// </summary>
    /// <remarks>
    /// When this property is set to true, the application window occupies the entire screen
    /// and does not display window decorations such as the title bar or borders.
    /// </remarks>
    public bool Fullscreen { get; set; } = false;

    /// <summary>
    /// Determines whether the window remains centered on the screen.
    /// </summary>
    /// <remarks>
    /// When this property is set to true, the window will automatically adjust its position
    /// to stay centered on the screen when navigating. Only applies when <see cref="AutoFit"/> is <c>true</c>.
    /// </remarks>
    public bool KeepCentered { get; set; } = false;

    /// <summary>
    /// The vertical spacing between the taskbar, or the dock, and the top of the window.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="AutoFit"/> is <c>true</c>.
    /// </remarks>
    public double Margin { get; set; } = 15;
}

public class UIProperties
{
    /// <summary>
    /// Determines the language used for the application's user interface.
    /// </summary>
    /// <remarks>It will load the corresponding .json language file, in the `Config/Languages` directory.
    /// E.G., "en" will load "en.json".
    /// </remarks>
    public string UserLanguage { get; set; } = "en";

    /// <summary>
    /// Determines whether the primary user interface is displayed in the application.
    /// </summary>
    public bool ShowInterface { get; set; } = true;

    /// <summary>
    /// Indicates whether alternative interface buttons are displayed in the user interface.
    /// </summary>
    /// <remarks>
    /// The alternative interface buttons refer to the hidden buttons that show when the cursor hovers nearby.
    /// </remarks>
    public bool ShowAltInterfaceButtons { get; set; } = true;

    /// <summary>
    /// Indicates whether the bottom navigation bar is displayed within the application UI.
    /// </summary>
    /// <remarks>
    /// If <see cref="ShowInterface"/> is <c>false</c> it will be hidden regardless.
    /// </remarks>
    public bool ShowBottomNavBar { get; set; } = true;

    /// <summary>
    /// Indicates whether the taskbar progress is enabled when navigating pictures.
    /// </summary>
    public bool IsTaskbarProgressEnabled { get; set; } = true;

    /// <summary>
    /// Represents the navigation speed, determining the seconds interval at which navigation operations occur,
    /// where the corresponding key or button is held down.
    /// </summary>
    public double NavSpeed { get; set; } = 0.3;

    /// <summary>
    /// Specifies whether looping functionality is enabled or disabled in the application.
    /// When set to true, the media or gallery transitions seamlessly from the end to the beginning.
    /// When false, the transition will stop at the last item.
    /// </summary>
    public bool Looping { get; set; } = false;

    /// <summary>
    /// Represents the selected background color choice for the user interface.
    /// This value determines which background color or theme is applied to the main window.
    /// </summary>
    /// <remarks>
    /// Refer to the <see cref="PicView.Avalonia.ColorMangement.BackgroundManager"/> for the mapping.
    /// </remarks>
    public int BgColorChoice { get; set; } = 0;

    /// <summary>
    /// Gets or sets the time interval, in milliseconds, for the slideshow transition in the application's UI.
    /// Determines how long each slide is displayed during a slideshow.
    /// </summary>
    public double SlideShowTimer { get; set; } = 5000;

    /// <summary>
    /// Determines whether the application should open pictures in the same window
    /// or open a new window for each action.
    /// When set to true, all actions are handled within the existing application window.
    /// </summary>
    public bool OpenInSameWindow { get; set; } = false;

    /// <summary>
    /// Determines whether a confirmation dialog is displayed when the Escape key is pressed.
    /// </summary>
    public bool ShowConfirmationOnEsc { get; set; } = false;

    /// <summary>
    /// Determines whether a confirmation dialog is shown when attempting to recycle items.
    /// </summary>
    public bool ShowRecycleConfirmation { get; set; } = false;

    /// <summary>
    /// Indicates whether a confirmation dialog should be displayed before performing a permanent deletion action.
    /// </summary>
    public bool ShowPermanentDeletionConfirmation { get; set; } = true;

    /// <summary>
    /// Indicates whether the application limits the background color to just behind the picture.
    /// </summary>
    public bool IsConstrainBackgroundColorEnabled { get; set; } = false;
}

public class Theme
{
    /// <summary>
    /// Indicates whether the application theme is in dark mode.
    /// </summary>
    public bool Dark { get; set; } = true;

    /// <summary>
    /// Represents the color theme configuration setting, which determines the accent and primary colors used in the application.
    /// </summary>
    public int ColorTheme { get; set; } = 3;
    // Not used yet...
    public bool UseSystemTheme { get; set; } = false;

    /// <summary>
    /// Indicates whether the glass theme is enabled. When it is, light/dark theme is ignored.
    /// </summary>
    public bool GlassTheme { get; set; } = false;
}

public class Gallery
{
    /// <summary>
    /// A property indicating whether the bottom gallery is currently visible in the application.
    /// </summary>
    public bool IsBottomGalleryShown { get; set; } = false;

    /// <summary>
    /// Determines whether the bottom gallery is shown when the user interface is hidden.
    /// </summary>
    public bool ShowBottomGalleryInHiddenUI { get; set; } = false;

    /// <summary>
    /// Specifies the height of the gallery items displayed at the bottom section of the gallery view.
    /// </summary>
    public double BottomGalleryItemSize { get; set; } = 37;

    /// <summary>
    /// Specifies the height of gallery thumbnails, when the gallery is in full/expanded mode.
    /// </summary>
    public double ExpandedGalleryItemSize { get; set; } = 23;

    /// <summary>
    /// Determines how images will be stretched or scaled, when the gallery is in full/expanded mode.
    /// </summary>
    public string FullGalleryStretchMode { get; set; } = "Uniform";

    /// <summary>
    /// Determines how images will be stretched or scaled.
    /// </summary>
    public string BottomGalleryStretchMode { get; set; } = "Uniform";
}

public class ImageScaling
{
    /// <summary>
    /// Indicates whether images should be stretched to fill the available space.
    /// </summary>
    public bool StretchImage { get; set; } = false;
    /// Legacy setting, only used for switching between high quality image scaling. Set false for High Quality.
    public bool IsScalingSetToNearestNeighbor { get; set; } = false;

    /// <summary>
    /// Specifies whether images should be displayed side by side in the viewer.
    /// When enabled, the application shows two images next to each other for comparison purposes.
    /// </summary>
    public bool ShowImageSideBySide { get; set; } = false;
}

public class Zoom
{
    /// <summary>
    /// Specifies the speed at which zooming operations are performed.
    /// Determines how quickly the zoom level changes in response to user input or programmatic adjustments.
    /// </summary>
    public double ZoomSpeed { get; set; } = 0.3;

    /// <summary>
    /// A value indicating whether the application prevents zooming out beyond the original size of the image.
    /// </summary>
    public bool AvoidZoomingOut { get; set; } = false;

    /// <summary>
    /// Indicates whether the zoom operation is animated when performed.
    /// If set to true, zooming will include an animation effect; otherwise, it will occur instantly.
    /// </summary>
    public bool IsZoomAnimated { get; set; } = false;

    /// <summary>
    /// Determines whether the Ctrl key must be held while using the mouse wheel to zoom.
    /// If set to true, holding the Ctrl key enables zoom functionality with the mouse wheel.
    /// Otherwise, the mouse wheel will be used for navigation.
    /// </summary>
    public bool CtrlZoom { get; set; } = true;

    /// <summary>
    /// Indicates whether the zoom percentage popup is displayed when adjusting the zoom level.
    /// This setting can be changed dynamically during application runtime.
    /// </summary>
    public bool IsShowingZoomPercentagePopup { get; set; } = false;

    /// <summary>
    /// Determines whether to reverse the horizontal scrolling direction for navigation.
    /// When set to true, the horizontal scroll behavior is reversed, I.E, enable "Natural scroll".
    /// </summary>
    public bool HorizontalReverseScroll { get; set; } = true;

    /// <summary>
    /// A property that determines whether scrolling functionality is enabled for images. Does not work for zoom operations.
    /// </summary>
    public bool ScrollEnabled { get; set; } = false;

    /// <summary>
    /// Indicates whether a touchpad is being used for interaction.
    /// This property can affect input behavior and user interface adaptations based on touchpad usage.
    /// </summary>
    public bool IsUsingTouchPad { get; set; } = false;
}

public class Sorting
{
    /// <summary>
    /// Sort alphabetically by file name.
    /// </summary>
    public bool Name { get; set; } = true;

    /// <summary>
    /// Sort by file size.
    /// </summary>
    public bool Size { get; set; } = false;

    /// <summary>
    /// Sort by file creation time.
    /// </summary>
    public bool CreationTime { get; set; } = false;
    /// <summary>
    /// Sort by when the file was last accessed.
    /// </summary>
    public bool LastAccessTime { get; set; } = false;
    /// <summary>
    /// Sort by when the file was last written to.
    /// </summary>
    public bool LastWriteTime { get; set; } = false;
    /// <summary>
    /// Sort randomly.
    /// </summary>
    public bool Random { get; set; } = false;

    /// <summary>
    /// Whether sorting should be performed in ascending order.
    /// </summary>
    public bool Ascending { get; set; } = true;
    
    /// <summary>
    ///  Maps the sort preference
    /// </summary>
    /// <remarks>
    /// 0 => Name,
    /// 1 => FileSize,
    /// 2 => CreationTime,
    /// 3 => Extension,
    /// 4 => LastAccessTime,
    /// 5 => LastWriteTime,
    /// 6 => Random
    /// </remarks>
    public int SortPreference { get; set; } = 0;

    /// <summary>
    /// Indicates whether subdirectories should be included during sorting and navigation.
    /// </summary>
    public bool IncludeSubDirectories { get; set; } = false;
}

public class StartUp
{
    /// <summary>
    /// Indicates whether the application should automatically open the last file used upon startup.
    /// </summary>
    public bool OpenLastFile { get; set; } = false;
    // Not used yet...
    public bool OpenSpecificFile { get; set; } = false;
    // Not used yet...
    public string OpenSpecificString { get; set; } = "";

    /// <summary>
    /// Represents the path to the last file accessed or opened.
    /// </summary>
    /// <remarks>
    /// Used to load the file when <see cref="OpenLastFile"/> is <c>true</c>.
    /// </remarks>
    public string? LastFile { get; set; } = "";

    /// <summary>
    /// Specifies the initial directory path used by the application at startup.
    /// </summary>
    /// <remarks>
    /// Used to remember directory structure upon startup, when <see cref="OpenLastFile"/> and <see cref="Sorting.IncludeSubDirectories"/> are <c>true</c>.
    /// </remarks>
    public string? StartUpDirectory { get; set; } = "";
}


public class Navigation
{
    /// <summary>
    /// Indicates whether the file watcher feature is enabled.
    /// </summary>
    /// <remarks>
    /// Experimental feature, it is recommended not to turn off.
    /// </remarks>
    public bool IsFileWatcherEnabled { get; set; } = true;

    /// <summary>
    /// The number of iterations to process in the forward direction while navigating through items and preloading.
    /// </summary>
    /// <remarks>
    /// Experimental feature, it is recommended not to change.
    /// </remarks>
    public int PositiveIterations { get; set; } = 6;

    /// <summary>
    /// The number of iterations to process in the backwards direction while navigating through items and preloading.
    /// </summary>
    ///     /// <remarks>
    /// Experimental feature, it is recommended not to change.
    /// </remarks>
    public int NegativeIterations { get; set; } = 4;

    /// <summary>
    /// Determines whether navigating through the file history is enabled, with the mouse side buttons.
    /// When set to true, navigation will cycle through the historical entries of accessed files.
    /// </summary>
    public bool IsNavigatingFileHistory { get; set; } = true;
    
    /// <summary>
    /// Determines if the file history should be saved.
    /// </summary>
    public bool IsFileHistoryEnabled { get; set; } = true;
    
    /// <summary>
    /// Determines whether navigation between directories is enabled, with the mouse side buttons.
    /// When set to true, navigation will cycle through the directories, next to the current file order.
    /// </summary>
    public bool IsNavigatingBetweenDirectories { get; set; } = false;

    /// <summary>
    /// Indicates whether navigation should move backward in the sequence when an image file is deleted.
    /// </summary>
    public bool IsNavigatingBackwardsWhenDeleting { get; set; } = true;

}