using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.ImageTransformations;
using PicView.Core.IPlatform;
using PicView.Core.Navigation;
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
    /// Runs an action on the UI thread, such as resetting the effects after they were written over the file. Null runs
    /// it inline.
    /// </summary>
    public Func<Action, Task>? UiThread { get; set; }

    /// <summary>
    /// Whether a Save Source write of the tab would write an Effects Render rather than its file as it is.
    /// </summary>
    public bool WouldWriteEffects(TabViewModel tab)
    {
        if (tab.FileInfo.CurrentValue is not { } file || tab.ImageType.CurrentValue is ImageType.Svg)
        {
            return false;
        }

        using var render = getEffects()?.CloneRender(file);
        return render is not null;
    }

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
            if (job.Destination is WriteDestination.Copy copy && file is not null &&
                string.Equals(Path.GetFullPath(copy.Path), file.FullName, StringComparison.OrdinalIgnoreCase))
            {
                // A copy onto the shown file is a write over itself, so the tab shows it again
                job = job with { Destination = new WriteDestination.OverItself() };
            }

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
        var overItself = job.Destination is WriteDestination.OverItself;
        var destination = job.Destination switch
        {
            WriteDestination.Copy copy => copy.Path,
            WriteDestination.OverItself => file?.FullName,
            WriteDestination.Replace replace when file is not null => Path.GetFullPath(replace.NewPath),
            WriteDestination.Replace => null,
            _ => throw new NotSupportedException($"{job.Destination.GetType().Name} is not written yet")
        };
        if (destination is null)
        {
            // Over itself or replacing with no file; the caller saves it as a new file instead
            return new WriteResult(WriteOutcome.NothingToWrite, null, false, false);
        }

        if (job.Destination is WriteDestination.Replace && FileHelper.IsNameTaken(file!.FullName, destination))
        {
            return new WriteResult(WriteOutcome.NameTaken, null, false, false);
        }

        WriteResult result;
        if (overItself && job.Edits.LosslessOptimize)
        {
            // Optimizes the file's own bytes; encoding it again would not be lossless
            if (!new ImageOptimizer().IsSupported(destination))
            {
                return new WriteResult(WriteOutcome.NothingToWrite, null, false, false);
            }

            await BackUpToRecycleBinAsync(tab.Services?.Platform, destination).ConfigureAwait(false);
            OptimizeOver(destination);
            result = new WriteResult(WriteOutcome.Written, destination, false, false);
        }
        else
        {
            using var image = Read(tab, job.Source, file, isSvg, out var effectsApplied);
            if (image is null)
            {
                return new WriteResult(WriteOutcome.NothingToWrite, null, false, false);
            }

            var turned = ApplyEdits(image, job.Edits, orientation);
            if (File.Exists(destination))
            {
                await BackUpToRecycleBinAsync(tab.Services?.Platform, destination).ConfigureAwait(false);
            }

            await EncodeThroughTempAsync(image, destination, job.Edits.LosslessOptimize).ConfigureAwait(false);

            result = new WriteResult(WriteOutcome.Written, destination, turned && orientation != Orientation.Upright,
                effectsApplied);
        }

        if (overItself)
        {
            await FollowUpOverItselfAsync(tab, job.Edits, result, destination).ConfigureAwait(false);
        }
        else if (job.Destination is WriteDestination.Replace)
        {
            await FollowUpReplaceAsync(tab, result, file!.FullName, destination).ConfigureAwait(false);
        }

        return result;
    }

    // Moves the tab's histories to the new file and shows it, then removes the original, unless the new file is the
    // original apart from letter case: it is the file itself
    private async ValueTask FollowUpReplaceAsync(TabViewModel tab, WriteResult result, string original,
        string newPath)
    {
        var sameFile = string.Equals(original, newPath, StringComparison.OrdinalIgnoreCase);
        try
        {
            if (!string.Equals(original, newPath, StringComparison.Ordinal))
            {
                FileRenames.Follow(tab.ParentWindowContext?.Core, original, newPath);
            }

            await tab.ShowReplacementAsync(newPath, result.TurnApplied).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // The file is written; only showing it failed
            DebugHelper.LogDebug(nameof(ShownFileWriter), nameof(FollowUpReplaceAsync), e);
        }

        if (!sameFile)
        {
            await RemoveOriginalAsync(tab.Services?.Platform, original).ConfigureAwait(false);
        }

        await ResetEffectsAsync(result).ConfigureAwait(false);
    }

    // Sends the original to the Recycle Bin when its drive has one, and deletes it only on a drive with none. When the
    // recycle fails it is kept: the user may have refused the shell's "delete permanently?" for a file too large for
    // the bin. A failure is logged: the new file is written and shown
    private static async ValueTask RemoveOriginalAsync(IPlatformSpecificService? platform, string original)
    {
        try
        {
            if (platform is not null && platform.CanMoveToRecycleBin(original))
            {
                if (!await platform.DeleteFile(original, recycle: true).ConfigureAwait(false))
                {
                    DebugHelper.LogDebug(nameof(ShownFileWriter), nameof(RemoveOriginalAsync),
                        $"{original} was not recycled and is kept");
                }

                return;
            }

            File.Delete(original);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ShownFileWriter), nameof(RemoveOriginalAsync), e);
        }
    }

    // Shows the rewritten file again unless it holds the pixels the tab shows already, then resets the effects written
    private async ValueTask FollowUpOverItselfAsync(TabViewModel tab, WriteEdits edits, WriteResult result,
        string path)
    {
        try
        {
            if (result.TurnApplied || result.EffectsApplied || edits.Cut is not null || edits.Width > 0 ||
                edits.Height > 0 || edits.LosslessOptimize || edits.Quality is not null)
            {
                await tab.ShowRewrittenAsync(result.TurnApplied).ConfigureAwait(false);
            }
            else
            {
                tab.ForgetRewritten(path);
            }

            await ResetEffectsAsync(result).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // The file is written; only showing it again failed
            DebugHelper.LogDebug(nameof(ShownFileWriter), nameof(FollowUpOverItselfAsync), e);
        }
    }

    // Resets the effects that were written into the file
    private async ValueTask ResetEffectsAsync(WriteResult result)
    {
        if (!result.EffectsApplied || getEffects() is not { } effects)
        {
            return;
        }

        try
        {
            // The effects window binds to these values, so they change on the UI thread; discarding them does not
            // render the file again
            if (UiThread is { } uiThread)
            {
                await uiThread(effects.DiscardEffects).ConfigureAwait(false);
            }
            else
            {
                effects.DiscardEffects();
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ShownFileWriter), nameof(ResetEffectsAsync), e);
        }
    }

    // TODO: a future Undo/Redo (Ctrl+Z, Ctrl+Y) should restore the overwritten file from this recycled copy.
    // Sends a copy of the file at path to the Recycle Bin before it is written over. The copy is made in the temp
    // folder, so it is the temp folder's drive that needs a bin, and it is recycled quietly: a plain Save asks nothing.
    // A failure is logged, the copy is left for TempFileManager.Cleanup and the write goes on
    private static async ValueTask BackUpToRecycleBinAsync(IPlatformSpecificService? platform, string path)
    {
        if (platform is null)
        {
            return;
        }

        try
        {
            var copy = TempFileManager.GetNewTempFilePath(Path.GetFileName(path));
            if (!platform.CanMoveToRecycleBin(copy))
            {
                DeleteTemp(copy);
                return;
            }

            File.Copy(path, copy);
            if (!await platform.RecycleQuietlyAsync(copy).ConfigureAwait(false))
            {
                DebugHelper.LogDebug(nameof(ShownFileWriter), nameof(BackUpToRecycleBinAsync),
                    $"{copy} was not recycled");
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ShownFileWriter), nameof(BackUpToRecycleBinAsync), e);
        }
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
        try
        {
            // The viewer shows the file with its EXIF orientation applied
            image.AutoOrient();
            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
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

    // Encodes the image to a temp file, then copies it to path. A failed encode leaves path as it was, or absent, rather
    // than a partial file in a watched folder; and the folder watcher sees only the file change: a temp file in path's
    // folder would join the tab's file list
    private static async Task EncodeThroughTempAsync(MagickImage image, string path, bool losslessOptimize)
    {
        var temp = TempFileManager.GetNewTempFilePath(Path.GetFileName(path));
        try
        {
            await EncodeAsync(image, temp, losslessOptimize).ConfigureAwait(false);
            // Not atomic: the Recycle Bin copy is the net for a failure part-way
            File.Copy(temp, path, overwrite: true);
        }
        finally
        {
            DeleteTemp(temp);
        }
    }

    // Optimizes the file at path losslessly through a temp copy, as EncodeThroughTempAsync writes
    private static void OptimizeOver(string path)
    {
        var temp = TempFileManager.GetNewTempFilePath(Path.GetFileName(path));
        try
        {
            File.Copy(path, temp);
            var optimizer = new ImageOptimizer { OptimalCompression = true };
            if (!optimizer.IsSupported(temp))
            {
                return;
            }

            optimizer.LosslessCompress(temp);
            // Not atomic: the Recycle Bin copy is the net for a failure part-way
            File.Copy(temp, path, overwrite: true);
        }
        finally
        {
            DeleteTemp(temp);
        }
    }

    // Deletes a temp file with the folder GetNewTempFilePath made for it
    private static void DeleteTemp(string temp)
    {
        try
        {
            Directory.Delete(Path.GetDirectoryName(temp)!, recursive: true);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ShownFileWriter), nameof(DeleteTemp), e);
        }
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
