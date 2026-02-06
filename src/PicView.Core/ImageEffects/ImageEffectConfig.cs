using ImageMagick;

namespace PicView.Core.ImageEffects;

public readonly record struct ColorBalanceTriplet(int CyanRed, int MagentaGreen, int YellowBlue);

public class ImageEffectConfig
{
    // Existing
    public Percentage Brightness { get; set; }
    public Percentage Contrast  { get; set; }
    public Percentage Solarize { get; set; }
    public double SketchStrokeWidth { get; set; }
    public int PosterizeLevel { get; set; }
    public bool Negative { get; set; }
    public bool BlackAndWhite { get; set; }
    public bool OldMovie { get; set; }
    public double BlurLevel { get; set; }

    // Color
    public double HueDegrees { get; set; }
    public Percentage Saturation { get; set; }
    public int Temperature { get; set; }
    public int Tint { get; set; }
    public int Vibrance { get; set; }

    // Lighting
    public double ExposureStops { get; set; }
    public double Gamma { get; set; } = 1;

    // Tonal controls
    public int Highlights { get; set; }
    public int Shadows { get; set; }
    public int Blacks { get; set; }
    public int Whites { get; set; }

    // Effects
    public int Dehaze { get; set; }
    public int Clarity { get; set; }
    public int Grain { get; set; }

    public double Sharpen { get; set; }
    public double Vignette { get; set; }

    // Color Balance
    public ColorBalanceTriplet CBShadows { get; set; } = new(0,0,0);
    public ColorBalanceTriplet CBMidtones { get; set; } = new(0,0,0);
    public ColorBalanceTriplet CBHighlights { get; set; } = new(0,0,0);
}
