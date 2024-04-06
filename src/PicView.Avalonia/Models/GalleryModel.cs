using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace PicView.Avalonia.Models;

public struct GalleryModel
{
    public string FileLocation { get; set; }
    public string FileName { get; set; }
    public string FileSize { get; set; }
    public string FileDate { get; set; }
    public Bitmap? Image { get; set; }
}