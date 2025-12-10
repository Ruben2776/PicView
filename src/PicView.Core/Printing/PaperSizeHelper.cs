namespace PicView.Core.Printing;

public static class PaperSizeHelper
{
    public static IEnumerable<string> GetAllNames() => Sizes.Keys;
    
    // Dimensions in mm
    private static readonly Dictionary<string, (double W, double H)> Sizes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "A4", (210, 297) },
        { "A3", (297, 420) },
        { "A5", (148, 210) },
        { "Letter", (215.9, 279.4) },
        { "Legal", (215.9, 355.6) },
        { "Tabloid", (279.4, 431.8) },
        { "4x6", (101.6, 152.4) },
        { "5x7", (127, 177.8) },
        { "8x10", (203.2, 254) }
    };

    public static (double Width, double Height) GetMmSize(string paperName)
    {
        // Try exact match or "Starts With" (e.g. "A4 (Borderless)" -> matches "A4")
        if (Sizes.TryGetValue(paperName, out var exact))
        {
            return exact;
        }
        
        foreach (var key in Sizes.Keys.Where(key => paperName.StartsWith(key, StringComparison.OrdinalIgnoreCase)))
        {
            return Sizes[key];
        }

        // Default to A4 if unknown
        return Sizes["A4"];
    }
}