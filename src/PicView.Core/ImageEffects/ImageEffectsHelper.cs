using System;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;

namespace PicView.Core.ImageEffects;

public static class ImageEffectsHelper
{
    public static async Task<MagickImage?> ApplyEffects(FileInfo fileInfo, ImageEffectConfig config, CancellationToken ct)
    {
        try
        {
            return await Task.Run(async () =>
            {
                var img = await LoadImage(fileInfo, ct).ConfigureAwait(false);
                ApplyImageEffects(img, config, ct);
                return img;
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ImageEffectsHelper), nameof(ApplyEffects), ex);
        }

        return null;
    }

    private static async Task<MagickImage> LoadImage(FileInfo fileInfo, CancellationToken ct)
    {
        await using var fs = FileStreamUtils.GetOptimizedFileStream(fileInfo);
        var img = new MagickImage();

        if (fileInfo.Length >= 2147483648)
            img.Read(fs);
        else
            await img.ReadAsync(fs, ct).ConfigureAwait(false);

        return img;
    }

    private static void ApplyImageEffects(MagickImage img, ImageEffectConfig c, CancellationToken ct)
    {
        ApplyExposure(img, c.ExposureStops);
        img.BrightnessContrast(c.Brightness, c.Contrast);

        if (c.Gamma > 0 && Math.Abs(c.Gamma - 1) > 0.0001)
            img.GammaCorrect(c.Gamma);

        if (c.Shadows != 0 || c.Highlights != 0)
            ApplyShadowsHighlights(img, c.Shadows, c.Highlights);

        if (c.Blacks != 0 || c.Whites != 0)
            ApplyBlackWhitePoints(img, c.Blacks, c.Whites);

        ct.ThrowIfCancellationRequested();

        ApplyHueSaturation(img, c.HueDegrees, c.Saturation);
        ApplyTemperatureTint(img, c.Temperature, c.Tint);
        ApplyVibrance(img, c.Vibrance);

        if (!IsZero(c.CBShadows) || !IsZero(c.CBMidtones) || !IsZero(c.CBHighlights))
            ApplyColorBalance(img, c.CBShadows, c.CBMidtones, c.CBHighlights);

        if (c.Dehaze > 0) ApplyDehaze(img, c.Dehaze);
        if (c.Clarity > 0) ApplyClarity(img, c.Clarity);
        if (c.Grain > 0) ApplyFilmGrain(img, c.Grain);

        if (c.Negative) img.Negate();
        if (c.BlackAndWhite) img.Grayscale();
        if (c.OldMovie) ApplyOldMovieEffect(img);

        if (c.BlurLevel > 0) img.Blur(0, c.BlurLevel);
        if (c.Sharpen > 0) ApplySharpen(img, c.Sharpen);
        if (c.Vignette > 0) ApplySafeVignette(img, c.Vignette);

        if (c.SketchStrokeWidth > 0) img.Charcoal(c.SketchStrokeWidth, 3);
        if (c.PosterizeLevel > 0) img.Posterize(c.PosterizeLevel);
        if (c.Solarize.ToUInt32() != 0) img.Solarize(c.Solarize);

        ct.ThrowIfCancellationRequested();
    }

    // ----------------------------------------------------
    // Helpers
    // ----------------------------------------------------

    private static void ApplyExposure(MagickImage img, double stops)
    {
        if (stops == 0) return;
        img.Evaluate(Channels.RGB, EvaluateOperator.Multiply, Math.Pow(2, stops));
    }

    private static void ApplyShadowsHighlights(MagickImage img, int shadows, int highlights)
    {
        using var lum = new MagickImage(img);
        lum.ColorSpace = ColorSpace.Gray;

        if (shadows != 0)
        {
            using var mask = new MagickImage(lum);
            mask.Negate();
            mask.Level(new Percentage(15), new Percentage(95));
            mask.Blur(0, 2);

            using var adj = new MagickImage(img);
            var a = shadows / 100.0;
            adj.GammaCorrect(1 + a * 0.25);
            CompositeWithMask(img, adj, mask);
        }

        if (highlights != 0)
        {
            using var mask = new MagickImage(lum);
            mask.Level(new Percentage(55), new Percentage(100));
            mask.Blur(0, 2);

            using var adj = new MagickImage(img);
            var a = highlights / 100.0;
            adj.BrightnessContrast(new Percentage(a * 6), new Percentage(-a * 6));
            CompositeWithMask(img, adj, mask);
        }
    }

    private static void ApplyBlackWhitePoints(MagickImage img, int blacks, int whites)
    {
        var bp = Math.Clamp((-blacks / 50.0) * 6.0, 0, 20);
        var wp = Math.Clamp(100 + (whites / 50.0) * 3.0, 80, 100);
        img.Level(new Percentage(bp), new Percentage(wp));
    }

    private static void ApplyHueSaturation(MagickImage img, double hueDeg, Percentage sat)
    {
        if (hueDeg == 0 && sat.ToInt32() == 0) return;

        var huePct = 100.0 + (-hueDeg / 180.0) * -100.0;
        var satPct = 100.0 + sat.ToInt32();

        huePct = Math.Clamp(huePct, 0, 200);
        satPct = Math.Clamp(satPct, 0, 300);

        img.Modulate(new Percentage(100), new Percentage(satPct), new Percentage(huePct));
    }

    private static void ApplyTemperatureTint(MagickImage img, int temp, int tint)
    {
        var q = Quantum.Max;
        img.Evaluate(Channels.Red, EvaluateOperator.Add, (temp / 100.0) * q * 0.1);
        img.Evaluate(Channels.Blue, EvaluateOperator.Add, (-temp / 100.0) * q * 0.1);
        img.Evaluate(Channels.Green, EvaluateOperator.Add, (-tint / 100.0) * q * 0.06);
    }

    private static void ApplyVibrance(MagickImage img, int v)
    {
        var s = 1 + (v / 100.0) * 0.25;
        img.Modulate(new Percentage(100), new Percentage(s * 100), new Percentage(100));
    }

    private static void ApplyColorBalance(MagickImage img, ColorBalanceTriplet sh, ColorBalanceTriplet mid, ColorBalanceTriplet hi)
    {
        using var baseImg = new MagickImage(img);
        using var lum = new MagickImage(img) { ColorSpace = ColorSpace.Gray };

        ApplyBalanceLayer(img, baseImg, lum, sh, 0, 45);
        ApplyBalanceLayer(img, baseImg, lum, mid, 20, 80);
        ApplyBalanceLayer(img, baseImg, lum, hi, 55, 100);
    }

    private static void ApplyBalanceLayer(MagickImage target, MagickImage baseImg, MagickImage lum,
                                         ColorBalanceTriplet t, double lo, double hi)
    {
        if (IsZero(t)) return;

        using var mask = new MagickImage(lum);
        mask.Level(new Percentage(lo), new Percentage(hi));
        mask.Blur(0, 2);

        using var adj = new MagickImage(baseImg);
        ApplyBalanceTriplet(adj, t);

        CompositeWithMask(target, adj, mask);
    }

    private static void ApplyBalanceTriplet(MagickImage img, ColorBalanceTriplet t)
    {
        // -100..100 -> opponent adjustments.
        // Cyan<->Red affects (R) versus (G+B)
        // Magenta<->Green affects (G) versus (R+B)
        // Yellow<->Blue affects (B) versus (R+G)

        var q = Quantum.Max * 0.06;

        var cr = (t.CyanRed / 100.0) * q;        // + => more red, - => more cyan
        var mg = (t.MagentaGreen / 100.0) * q;   // + => more magenta, - => more green
        var yb = (t.YellowBlue / 100.0) * q;     // + => more yellow, - => more blue

        // Cyan/Red
        if (Math.Abs(cr) > 0.0001)
        {
            img.Evaluate(Channels.Red, EvaluateOperator.Add, cr);
            img.Evaluate(Channels.Green, EvaluateOperator.Add, -cr * 0.5);
            img.Evaluate(Channels.Blue, EvaluateOperator.Add, -cr * 0.5);
        }

        // Magenta/Green
        if (Math.Abs(mg) > 0.0001)
        {
            img.Evaluate(Channels.Green, EvaluateOperator.Add, -mg);
            img.Evaluate(Channels.Red, EvaluateOperator.Add, mg * 0.5);
            img.Evaluate(Channels.Blue, EvaluateOperator.Add, mg * 0.5);
        }

        // Yellow/Blue
        if (Math.Abs(yb) > 0.0001)
        {
            img.Evaluate(Channels.Blue, EvaluateOperator.Add, -yb);
            img.Evaluate(Channels.Red, EvaluateOperator.Add, yb * 0.5);
            img.Evaluate(Channels.Green, EvaluateOperator.Add, yb * 0.5);
        }
    }


    private static void CompositeWithMask(MagickImage target, MagickImage src, MagickImage mask)
    {
        src.Alpha(AlphaOption.On);
        src.Composite(mask, CompositeOperator.CopyAlpha);
        target.Composite(src, CompositeOperator.Over);
    }

    private static void ApplyDehaze(MagickImage img, int amt)
    {
        var a = amt / 100.0;
        img.ContrastStretch(new Percentage(0.2 + a), new Percentage(99.8 - a));
    }

    private static void ApplyClarity(MagickImage img, int amt)
    {
        img.UnsharpMask(0, 0.6 + amt / 100.0, 0.3, 0.02);
    }

    private static void ApplyFilmGrain(MagickImage img, int amt)
    {
        using var noise = new MagickImage(MagickColors.Gray, img.Width, img.Height);
        noise.AddNoise(NoiseType.Gaussian);
        noise.Blur(0, 0.6);
        img.Composite(noise, CompositeOperator.Overlay);
    }

    private static void ApplySharpen(MagickImage img, double amt)
    {
        img.UnsharpMask(0, 0.6 + amt / 100.0, 1.0, 0.02);
    }

    private static void ApplySafeVignette(MagickImage img, double amt)
    {
        using var mask = new MagickImage("radial-gradient:white-black", new MagickReadSettings
        {
            Width = img.Width,
            Height = img.Height
        });

        using var overlay = new MagickImage(MagickColors.Black, img.Width, img.Height);
        overlay.Alpha(AlphaOption.On);
        overlay.Composite(mask, CompositeOperator.CopyAlpha);
        img.Composite(overlay, CompositeOperator.Multiply);
    }

    private static void ApplyOldMovieEffect(MagickImage img)
    {
        img.SepiaTone(new Percentage(80));
        img.AddNoise(NoiseType.MultiplicativeGaussian);
    }

    private static bool IsZero(ColorBalanceTriplet t)
        => t.CyanRed == 0 && t.MagentaGreen == 0 && t.YellowBlue == 0;
}
