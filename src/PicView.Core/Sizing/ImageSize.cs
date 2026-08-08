using System.Runtime.InteropServices;

namespace PicView.Core.Sizing;

[StructLayout(LayoutKind.Auto)]
public readonly record struct ImageSize(
    double WindowWidth,
    double WindowHeight,
    double Width,
    double Height,
    double ScrollViewerWidth,
    double ScrollViewerHeight,
    double InitialZoom);