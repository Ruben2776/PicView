namespace PicView.Core.Config;

public class AppSettings
{
    public double Version { get; set; } = 1;
    public WindowProperties? WindowProperties { get; set; }
    public UIProperties? UIProperties { get; set; }
    public Theme? Theme { get; set; }
    public Gallery? Gallery { get; set; }
    public ImageScaling? ImageScaling { get; set; }
    public Sorting? Sorting { get; set; }
    public Zoom? Zoom { get; set; }
    public StartUp? StartUp { get; set; }
}

public class WindowProperties
{
    public double Top { get; set; } = 300;
    public double Left { get; set; } = 500;
    public double Width { get; set; } = 750;
    public double Height { get; set; } = 1024;
    public bool AutoFit { get; set; } = false;
    public bool TopMost { get; set; } = false;
    public bool Maximized { get; set; } = false;
    public bool Fullscreen { get; set; } = false;
    public bool KeepCentered { get; set; } = false;
}

public class UIProperties
{
    public string UserLanguage { get; set; } = "en";
    public bool ShowInterface { get; set; } = true;
    public bool ShowAltInterfaceButtons { get; set; } = true;
    public bool ShowFileSavingDialog { get; set; } = true;
    public bool ShowBottomNavBar { get; set; } = true;
    public bool IsTaskbarProgressEnabled { get; set; } = true;
    public double NavSpeed { get; set; } = 0.3;
    public bool Looping { get; set; } = false;
    public int BgColorChoice { get; set; } = 0;
    public double SlideShowTimer { get; set; } = 5000;
}

public class Theme
{
    public bool Dark { get; set; } = true;
    public bool Light { get; set; } = false;
    public int ColorTheme { get; set; } = 3;
}

public class Gallery
{
    public bool IsBottomGalleryShown { get; set; } = false;
    public bool ShowBottomGalleryInHiddenUI { get; set; } = false;
    public double BottomGalleryItemSize { get; set; } = 37;
    public double ExpandedGalleryItemSize { get; set; } = 23;
}

public class ImageScaling
{
    public bool StretchImage { get; set; } = false;
    public bool IsScalingSetToNearestNeighbor { get; set; } = false;
}

public class Zoom
{
    public double ZoomSpeed { get; set; } = 0.3;
    public bool AvoidZoomingOut { get; set; } = false;
    public bool CtrlZoom { get; set; } = true;
    public bool HorizontalReverseScroll { get; set; } = true;
    public bool ScrollEnabled { get; set; } = false;
}

public class Sorting
{
    public bool Name { get; set; } = true;
    public bool Ascending { get; set; } = true;
    public int SortPreference { get; set; } = 0;
    public bool IncludeSubDirectories { get; set; } = false;
}

public class StartUp
{
    public bool OpenLastFile { get; set; } = false;
    public bool OpenSpecificFile { get; set; } = false;
    public string OpenSpecificString { get; set; } = "";
}