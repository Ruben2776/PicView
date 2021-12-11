namespace PicView.Settings;

public class UserConfig
{
    public UserConfig()
    {
        WindowProperties windowProperties = new();
        Theme theme = new();
        Sorting sorting = new();
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
        public bool ShowInterface { get; set; }
    }

    public class Theme
    {
        public bool Dark { get; set; }
        public bool Light { get; set; }
    }

    public class Sorting
    {
        public bool Name { get; set; }
        public bool FileSize { get; set; }
        public bool CreationTime { get; set; }
        public bool Extension { get; set; }
        public bool LastAccessTime { get; set; }
        public bool LastWriteTime { get; set; }
        public bool Random { get; set; }
        public bool Ascending { get; set; }
    }

    public string? UserLanguage { get; set; }
    public bool Looping { get; set; }
    public double ZoomSpeed { get; set; }
}