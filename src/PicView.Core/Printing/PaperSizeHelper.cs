namespace PicView.Core.Printing;

public static class PaperSizeHelper
{
    // Dimensions in mm, portrait.
    // Ordered: a name containing more than one key resolves to the first match.
    private static readonly (string Name, double Width, double Height)[] Sizes =
    [
        ("A4", 210, 297),
        ("A3", 297, 420),
        ("A5", 148, 210),
        ("Letter", 215.9, 279.4),
        ("Legal", 215.9, 355.6),
        ("Tabloid", 279.4, 431.8),
        ("4x6", 101.6, 152.4),
        ("5x7", 127, 177.8),
        ("8x10", 203.2, 254)
    ];

    private static readonly string[] Names = Array.ConvertAll(Sizes, size => size.Name);

    public static IEnumerable<string> GetAllNames() => Names;

    /// <summary>
    /// Resolves a printer's paper name to the paper's size as it lies in the given orientation.
    /// </summary>
    /// <param name="paperName">
    /// A paper name as the platform reports it, e.g. "A4", "Letter Small" (Windows),
    /// "iso_a4_210x297mm" or "A4.Borderless" (CUPS). Unknown or missing names resolve to A4.
    /// </param>
    /// <param name="orientation">An <see cref="Orientations"/> value.</param>
    public static Paper Resolve(string? paperName, int orientation)
    {
        var (width, height) = GetPortraitMmSize(paperName);
        return orientation == (int)Orientations.Landscape
            ? new Paper(height, width)
            : new Paper(width, height);
    }

    private static (double Width, double Height) GetPortraitMmSize(string? paperName)
    {
        if (!string.IsNullOrWhiteSpace(paperName))
        {
            foreach (var size in Sizes)
            {
                if (paperName.Contains(size.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return (size.Width, size.Height);
                }
            }
        }

        return (Sizes[0].Width, Sizes[0].Height);
    }
}
