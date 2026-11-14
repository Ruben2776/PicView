using System.Runtime.InteropServices;

namespace PicView.Core.ImageTransformations;

/// <summary>
/// A rectangle in whole pixels, with its origin at the top left.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PixelArea(int X, int Y, uint Width, uint Height);
