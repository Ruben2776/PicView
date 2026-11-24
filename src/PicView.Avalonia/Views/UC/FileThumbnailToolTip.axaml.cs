using Avalonia;
using Avalonia.Controls;

namespace PicView.Avalonia.Views.UC;

/// <summary>
/// The tooltip of a file in a list, such as File History or File Search: its thumbnail above its path.
/// </summary>
public partial class FileThumbnailToolTip : UserControl
{
    public static readonly StyledProperty<Task<object?>?> ThumbnailProperty =
        AvaloniaProperty.Register<FileThumbnailToolTip, Task<object?>?>(nameof(Thumbnail));

    public static readonly StyledProperty<string?> FilePathProperty =
        AvaloniaProperty.Register<FileThumbnailToolTip, string?>(nameof(FilePath));

    /// <summary>
    /// The file's thumbnail, shown once it is made. The logo shows until then, and when the file gives none.
    /// </summary>
    public Task<object?>? Thumbnail
    {
        get => GetValue(ThumbnailProperty);
        set => SetValue(ThumbnailProperty, value);
    }

    /// <summary>The path shown under the thumbnail.</summary>
    public string? FilePath
    {
        get => GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    public FileThumbnailToolTip()
    {
        InitializeComponent();
        // The content binds to this control, while the control itself keeps the item it is the tooltip of, which
        // its own properties bind to
        Root.DataContext = this;
    }
}
