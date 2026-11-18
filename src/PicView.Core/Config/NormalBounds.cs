using System.Runtime.InteropServices;

namespace PicView.Core.Config;

/// <summary>
/// A Main Window's last size and position in Normal mode.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct NormalBounds(double Left, double Top, double Width, double Height);
