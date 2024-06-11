using Avalonia;
using Avalonia.Controls.Primitives;

namespace PicView.Avalonia.CustomControls;

public class AdvancedImageBox : TemplatedControl, IScrollable
{
    public Size Extent { get; }
    public Vector Offset { get; set; }
    public Size Viewport { get; }
}