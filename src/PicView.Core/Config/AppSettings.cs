namespace PicView.Core.Config
{
    public class AppSettings
    {
        public WindowProperties WindowProperties { get; set; }
        public UIProperties UIProperties { get; set; }
        public Theme Theme { get; set; }
        public Gallery Gallery { get; set; }
        public ImageScaling ImageScaling { get; set; }
        public Sorting Sorting { get; set; }
        public Zoom Zoom { get; set; }
    }

    public class WindowProperties
    {
        public double Top { get; set; }
        public double Left { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool AutoFit { get; set; }
        public bool TopMost { get; set; }
        public bool Maximized { get; set; }
        public bool Fullscreen { get; set; }
        public bool KeepCentered { get; set; }
    }

    public class UIProperties
    {
        public string UserLanguage { get; set; }
        public bool ShowInterface { get; set; }
        public bool ShowAltInterfaceButtons { get; set; }
        public bool ShowFileSavingDialog { get; set; }
        public bool ShowBottomNavBar { get; set; }
        public bool IsTaskbarProgressEnabled { get; set; }
        public double NavSpeed { get; set; }
        public bool Looping { get; set; }
        public int BgColorChoice { get; set; }
        public double SlideShowTimer { get; set; }
    }

    public class Theme
    {
        public bool Dark { get; set; }
        public bool Light { get; set; }
        public int ColorTheme { get; set; }
    }

    public class Gallery
    {
        public bool IsBottomGalleryShown { get; set; }
        public bool ShowBottomGalleryInHiddenUI { get; set; }
        public double BottomGalleryItemSize { get; set; }
        public double ExpandedGalleryItemSize { get; set; }
    }

    public class ImageScaling
    {
        public bool StretchImage { get; set; }
        public bool IsScalingSetToNearestNeighbor { get; set; }
    }

    public class Zoom
    {
        public double ZoomSpeed { get; set; }
        public bool AvoidZoomingOut { get; set; }
        public bool CtrlZoom { get; set; }
        public bool HorizontalReverseScroll { get; set; }
        public bool ScrollEnabled { get; set; }
    }

    public class Sorting
    {
        public bool Name { get; set; }
        public bool FileSize { get; set; }
        public bool Creationtime { get; set; }
        public bool Extension { get; set; }
        public bool Lastaccesstime { get; set; }
        public bool Lastwritetime { get; set; }
        public bool Random { get; set; }
        public bool Ascending { get; set; }
        public int SortPreference { get; set; }
        public bool IncludeSubDirectories { get; set; }
    }
}