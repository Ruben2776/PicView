namespace PicView.Core.Config;

public record AppSettings
{
    public double Version { get; set; } = 1.3;
    public WindowProperties? WindowProperties { get; set; }
    public UIProperties? UIProperties { get; set; }
    public Theme? Theme { get; set; }
    public Gallery? Gallery { get; set; }
    public ImageScaling? ImageScaling { get; set; }
    public Sorting? Sorting { get; set; }
    public Zoom? Zoom { get; set; }
    public StartUp? StartUp { get; set; }
}

public record WindowProperties
{
    public double Top { get; set; } = 0;
    public double Left { get; set; } = 0;
    public double Width { get; set; } = 750;
    public double Height { get; set; } = 1024;
    public bool AutoFit { get; set; } = true;
    public bool TopMost { get; set; } = false;
    public bool Maximized { get; set; } = false;
    public bool Fullscreen { get; set; } = false;
    public bool KeepCentered { get; set; } = false;
}

public record UIProperties
{
    public string UserLanguage { get; set; } = "en";
    public bool ShowInterface { get; set; } = true;
    public bool ShowAltInterfaceButtons { get; set; } = true;
    public bool ShowBottomNavBar { get; set; } = true;
    public bool IsTaskbarProgressEnabled { get; set; } = true;
    public double NavSpeed { get; set; } = 0.3;
    public bool Looping { get; set; } = false;
    public int BgColorChoice { get; set; } = 0;
    public double SlideShowTimer { get; set; } = 5000;
    public bool OpenInSameWindow { get; set; } = false;
    public bool ShowConfirmationOnEsc { get; set; } = false;
}

public record Theme
{
    public bool Dark { get; set; } = true;
    public int ColorTheme { get; set; } = 3;
    public bool UseSystemTheme { get; set; } = false;
    
    public bool GlassTheme { get; set; } = false;
}

public record Gallery
{
    public bool IsBottomGalleryShown { get; set; } = false;
    public bool ShowBottomGalleryInHiddenUI { get; set; } = false;
    public double BottomGalleryItemSize { get; set; } = 37;
    public double ExpandedGalleryItemSize { get; set; } = 23;
    public string FullGalleryStretchMode { get; set; } = "Uniform";
    public string BottomGalleryStretchMode { get; set; } = "Uniform";
}

public record ImageScaling
{
    public bool StretchImage { get; set; } = false;
    public bool IsScalingSetToNearestNeighbor { get; set; } = false;
    
    public bool ShowImageSideBySide { get; set; } = false;
}

public record Zoom
{
    public double ZoomSpeed { get; set; } = 0.3;
    public bool AvoidZoomingOut { get; set; } = false;
    public bool CtrlZoom { get; set; } = true;
    public bool HorizontalReverseScroll { get; set; } = true;
    public bool ScrollEnabled { get; set; } = false;
    public bool IsUsingTouchPad { get; set; } = false;
}

public record Sorting
{
    public bool Name { get; set; } = true;
    public bool Size { get; set; } = false;
    public bool CreationTime { get; set; } = false;
    public bool LastAccessTime { get; set; } = false;
    public bool LastWriteTime { get; set; } = false;
    public bool Random { get; set; } = false;

    public bool Ascending { get; set; } = true;
    public int SortPreference { get; set; } = 0;
    public bool IncludeSubDirectories { get; set; } = false;
}

public record StartUp
{
    public bool OpenLastFile { get; set; } = false;
    public bool OpenSpecificFile { get; set; } = false;
    public string OpenSpecificString { get; set; } = "";
    public string? LastFile { get; set; } = "";
}