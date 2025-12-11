using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;

namespace PicView.Core.ImageEffects;

public static class ImageEffectsHelper
{
    public static async Task<MagickImage?> ApplyEffects(FileInfo fileInfo, ImageEffectConfig config,
        CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(async () =>
            {
                var magick = await LoadImage(fileInfo, cancellationToken);
                ApplyImageEffects(magick, config, cancellationToken);
                return magick;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Operation was canceled
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ImageEffectsHelper), nameof(ApplyEffects), e);
        }

        return null;
    }

    private static async Task<MagickImage> LoadImage(FileInfo fileInfo, CancellationToken cancellationToken)
    {
        await using var filestream = FileStreamUtils.GetOptimizedFileStream(fileInfo);
        var magick = new MagickImage();
        if (fileInfo.Length >= 2147483648)
        {
            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
            magick.Read(filestream);
        }
        else
        {
            await magick.ReadAsync(filestream, cancellationToken).ConfigureAwait(false);
        }

        return magick;
    }

    private static void ApplyImageEffects(MagickImage magick, ImageEffectConfig config,
        CancellationToken cancellationToken)
    {
        magick.BrightnessContrast(config.Brightness, config.Contrast);
        magick.BackgroundColor = MagickColors.Transparent;
        magick.Settings.BackgroundColor = MagickColors.Transparent;
        magick.Settings.FillColor = MagickColors.Transparent;

        if (config.Negative)
        {
            ApplyNegative(magick);
        }

        if (config.BlackAndWhite)
        {
            ApplyBlackAndWhite(magick);
        }

        if (config.OldMovie)
        {
            ApplyOldMovieEffect(magick);
        }

        if (config.SketchStrokeWidth != 0)
        {
            ApplyPencilSketch(magick, config.SketchStrokeWidth);
        }

        if (config.PosterizeLevel != 0)
        {
            ApplyPosterize(magick, config.PosterizeLevel);
        }

        if (config.BlurLevel != 0)
        {
            ApplyBlur(magick, config.BlurLevel);
        }

        if (config.Solarize.ToUInt32() != 0)
        {
            ApplySolarize(magick, config.Solarize);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static void ApplyNegative(MagickImage magick) => magick.Negate();

    private static void ApplyBlackAndWhite(MagickImage magick) => magick.Grayscale();

    private static void ApplyOldMovieEffect(MagickImage magick)
    {
        magick.SepiaTone(new Percentage(80));
        magick.AddNoise(NoiseType.MultiplicativeGaussian);
        AddVerticalBands(magick);
    }

    private static void AddVerticalBands(MagickImage magick)
    {
        var random = new Random();
        for (var i = 0; i < magick.Width; i += random.Next(1, 50))
        {
            using var band = new MagickImage(new MagickColor("#3E382A"), (uint)random.Next(1, 3), magick.Height);
            band.Evaluate(Channels.Alpha, EvaluateOperator.Set, 0.2);
            magick.Composite(band, i, 0, CompositeOperator.Over);
        }
    }

    private static void ApplyPencilSketch(MagickImage magick, double sketchStrokeWidth)
    {
        magick.Charcoal(sketchStrokeWidth, 3);
    }

    private static void ApplyPosterize(MagickImage magick, int posterizeLevel)
    {
        magick.Posterize(posterizeLevel);
    }

    private static void ApplyBlur(MagickImage magick, double blurLevel)
    {
        magick.Blur(0, blurLevel);
    }

    private static void ApplySolarize(MagickImage magick, Percentage solarize)
    {
        magick.Solarize(solarize);
    }
}