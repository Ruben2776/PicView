using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;
using PicView.Core.ImageTransformations;
using PicView.Core.ViewModels;
using Orientation = PicView.Core.ImageTransformations.Orientation;

namespace PicView.Core.Saving;

/// <summary>
/// Writes the image a tab shows: reads its source, applies the tab's turn and flip and the job's edits, and encodes
/// the result where the job says. Every writer of the shown image goes through it.
/// </summary>
/// <param name="getEffects">The effects view model, if the effects window has been opened.</param>
public sealed class ShownFileWriter(Func<EffectsViewModel?> getEffects)
{
    /// <summary>
    /// Reads a Single Image's pixels, which only the UI layer can. Without it a tab with no file has nothing to write.
    /// </summary>
    public ISingleImagePixels? SingleImagePixels { get; set; }

    /// <summary>
    /// Writes the tab's shown image as <paramref name="job"/> says, off the UI thread, with the tab Busy meanwhile.
    /// </summary>
    public async ValueTask<WriteResult> WriteAsync(TabViewModel tab, ShownFileWrite job)
    {
        using var busy = tab.ClaimBusy();
        var file = tab.FileInfo.CurrentValue;
        var isSvg = tab.ImageType.CurrentValue is ImageType.Svg;
        var orientation = tab.Orientation.Current.CurrentValue;
        try
        {
            return await Task.Run(() => WriteCoreAsync(tab, job, file, isSvg, orientation)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ShownFileWriter), nameof(WriteAsync), e);
            return new WriteResult(WriteOutcome.Failed, null, false, false);
        }
    }

    private async Task<WriteResult> WriteCoreAsync(TabViewModel tab, ShownFileWrite job, FileInfo? file, bool isSvg,
        Orientation orientation)
    {
        var destination = job.Destination switch
        {
            WriteDestination.Copy copy => copy.Path,
            _ => throw new NotSupportedException($"{job.Destination.GetType().Name} is not written yet")
        };

        using var image = Read(tab, job.Source, file, isSvg, out var effectsApplied);
        if (image is null)
        {
            return new WriteResult(WriteOutcome.NothingToWrite, null, false, false);
        }

        var turned = ApplyEdits(image, job.Edits, orientation);
        await EncodeAsync(image, destination, job.Edits.LosslessOptimize).ConfigureAwait(false);
        return new WriteResult(WriteOutcome.Written, destination, turned && orientation != Orientation.Upright,
            effectsApplied);
    }

    // The Effects Render of the file (Save Source only, and not for SVG), else the file as the viewer shows it, else
    // the Single Image pixels
    private MagickImage? Read(TabViewModel tab, WriteSource source, FileInfo? file, bool isSvg,
        out bool effectsApplied)
    {
        effectsApplied = false;
        if (file is null)
        {
            return SingleImagePixels?.Read(tab);
        }

        if (source is WriteSource.SaveSource && !isSvg && getEffects()?.CloneRender(file) is { } render)
        {
            effectsApplied = true;
            return render;
        }

        var image = new MagickImage(file);
        // The viewer shows the file with its EXIF orientation applied
        image.AutoOrient();
        return image;
    }

    // Cut, or turn then mirror as the view does; then resize and quality. Whether the turn and flip were applied
    private static bool ApplyEdits(MagickImage image, WriteEdits edits, Orientation orientation)
    {
        var turned = false;
        if (edits.Cut is { } cut)
        {
            CropRender.CutAndOrient(image, cut, orientation);
            turned = true;
        }
        else if (!edits.LosslessOptimize)
        {
            if (orientation.Degrees != 0)
            {
                image.Rotate(orientation.Degrees);
            }

            if (orientation.IsFlipped)
            {
                image.Flop();
            }

            turned = true;
        }

        if (edits.Width > 0 || edits.Height > 0)
        {
            image.Resize(edits.Width, edits.Height);
        }

        if (edits.Quality is { } quality)
        {
            image.Quality = quality;
        }

        return turned;
    }

    // Encodes the image to path, in the format of its extension
    private static async Task EncodeAsync(MagickImage image, string path, bool losslessOptimize)
    {
        image.Format = SaveImageFileHelper.FormatFor(Path.GetExtension(path), image.Format);
        await image.WriteAsync(path).ConfigureAwait(false);

        if (!losslessOptimize)
        {
            return;
        }

        var optimizer = new ImageOptimizer { OptimalCompression = true };
        if (optimizer.IsSupported(path))
        {
            optimizer.LosslessCompress(path);
        }
    }
}
