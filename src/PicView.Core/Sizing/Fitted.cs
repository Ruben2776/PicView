using System.Runtime.InteropServices;

namespace PicView.Core.Sizing;

/// <summary>
/// Why a Fit was applied: for a new image, or because one of the inputs of the Fit changed.
/// </summary>
public enum FitCause
{
    NewImage,
    Input
}

/// <summary>
/// A Fit the window applied, with what caused it.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct Fitted(Fit Fit, FitCause Cause);
