using System.Globalization;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.Models;

namespace PicView.Core.BatchResize;

/// <summary>
/// Runs a Batch Resize Job.
/// </summary>
public static class BatchResizer
{
    /// <summary>
    /// Writes every file of the job into its output folder.
    /// </summary>
    /// <param name="report">Called from worker threads with one entry per file, and one per Batch Thumbnail, as each
    /// is written.</param>
    public static async Task RunAsync(BatchResizeJob job, Action<BatchLogEntry> report,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(job.OutputFolder);
        var backup = new Backup(job.OutputFolder);
        var options = new ParallelOptions
        {
            // Leaves two cores for the UI, but always at least one for the run
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2),
            CancellationToken = cancellationToken
        };
        await Parallel.ForEachAsync(job.Files, options, (file, _) =>
        {
            try
            {
                ResizeFile(job, file, backup, report);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                DebugHelper.LogDebug(nameof(BatchResizer), nameof(RunAsync), e);
                report(new BatchLogEntry
                {
                    FileName = Path.Combine(file.Directory?.Name ?? string.Empty, file.Name) + Environment.NewLine +
                               e.Message,
                    OldSize = file.Exists ? file.Length.GetReadableFileSize() : "--",
                    NewSize = "--"
                });
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
    }

    private static void ResizeFile(BatchResizeJob job, FileInfo file, Backup backup, Action<BatchLogEntry> report)
    {
        var name = OutputName(file.Name, job.Conversion);
        var logName = Path.Combine(file.Directory?.Name ?? string.Empty, file.Name);
        var oldSize = file.Length.GetReadableFileSize();
        using var source = new MagickImage(file.FullName);

        using (var image = source.Clone())
        {
            Resize(image, job);
            var destination = Path.Combine(job.OutputFolder, name);
            backup.Save(destination);
            Write(image, destination, job);
            report(new BatchLogEntry { FileName = logName, OldSize = oldSize, NewSize = SizeOf(destination) });
        }

        foreach (var thumbnail in job.Thumbnails)
        {
            using var image = source.Clone();
            Resize(image, thumbnail);
            var folder = Path.Combine(job.OutputFolder, thumbnail.Subfolder);
            Directory.CreateDirectory(folder);
            var destination = Path.Combine(folder, name);
            backup.Save(destination);
            Write(image, destination, job);
            report(new BatchLogEntry
            {
                FileName = $"{TranslationManager.Translation.Thumbnail}: {Path.Combine(thumbnail.Subfolder, logName)}",
                OldSize = oldSize,
                NewSize = SizeOf(destination),
                IsThumbnail = true
            });
        }
    }

    /// <summary>
    /// Writes the image in the job's format and quality, then compresses the file.
    /// </summary>
    private static void Write(IMagickImage<byte> image, string destination, BatchResizeJob job)
    {
        if (Format(job.Conversion) is { } format)
        {
            image.Format = format;
        }

        if (job.Quality is { } quality)
        {
            image.Quality = quality;
        }

        image.Write(destination);
        Compress(destination, job.Compression);
    }

    /// <summary>
    /// The folder a run copies each file it would overwrite into, made only once one is needed.
    /// </summary>
    private sealed class Backup(string outputFolder)
    {
        private readonly Lazy<string> _folder = new(() =>
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            var folder = Path.Combine(outputFolder,
                $"PicView {TranslationManager.Translation.BatchResize} backup {timestamp}");
            Directory.CreateDirectory(folder);
            return folder;
        });

        public void Save(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var copy = Path.Combine(_folder.Value, Path.GetRelativePath(outputFolder, path));
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(path, copy, overwrite: false);
        }
    }

    private static string SizeOf(string path) => new FileInfo(path).Length.GetReadableFileSize();

    private static void Resize(IMagickImage<byte> image, BatchThumbnail thumbnail)
    {
        switch (thumbnail.Size)
        {
            case ThumbnailSize.Percentage when thumbnail.Value > 0:
                image.Resize(new Percentage(thumbnail.Value));
                break;
            case ThumbnailSize.Width when thumbnail.Value > 0:
                image.Resize((uint)thumbnail.Value, 0);
                break;
            case ThumbnailSize.Height when thumbnail.Value > 0:
                image.Resize(0, (uint)thumbnail.Value);
                break;
        }
    }

    private static void Resize(IMagickImage<byte> image, BatchResizeJob job)
    {
        switch (job.Resize)
        {
            case ResizeMode.Percentage when job.Percentage > 0:
                image.Resize(new Percentage(job.Percentage));
                break;
            case ResizeMode.Width when job.Width > 0:
                image.Resize(job.Width, 0);
                break;
            case ResizeMode.Height when job.Height > 0:
                image.Resize(0, job.Height);
                break;
            case ResizeMode.WidthAndHeight when job.Width > 0 && job.Height > 0:
                image.Resize(new MagickGeometry(job.Width, job.Height) { IgnoreAspectRatio = !job.KeepAspectRatio });
                break;
        }
    }

    private static void Compress(string path, CompressionMode compression)
    {
        if (compression is CompressionMode.None)
        {
            return;
        }

        var optimizer = new ImageOptimizer { OptimalCompression = compression is CompressionMode.Lossless };
        if (!optimizer.IsSupported(path))
        {
            return;
        }

        if (compression is CompressionMode.Lossless)
        {
            optimizer.LosslessCompress(path);
        }
        else
        {
            optimizer.Compress(path);
        }
    }

    private static string OutputName(string name, ConversionTarget conversion) => conversion switch
    {
        ConversionTarget.Png => Path.ChangeExtension(name, ".png"),
        ConversionTarget.Jpg => Path.ChangeExtension(name, ".jpg"),
        ConversionTarget.Webp => Path.ChangeExtension(name, ".webp"),
        ConversionTarget.Avif => Path.ChangeExtension(name, ".avif"),
        ConversionTarget.Heic => Path.ChangeExtension(name, ".heic"),
        ConversionTarget.Jxl => Path.ChangeExtension(name, ".jxl"),
        _ => name
    };

    private static MagickFormat? Format(ConversionTarget conversion) => conversion switch
    {
        ConversionTarget.Png => MagickFormat.Png,
        ConversionTarget.Jpg => MagickFormat.Jpg,
        ConversionTarget.Webp => MagickFormat.WebP,
        ConversionTarget.Avif => MagickFormat.Avif,
        ConversionTarget.Heic => MagickFormat.Heic,
        ConversionTarget.Jxl => MagickFormat.Jxl,
        _ => null
    };
}
