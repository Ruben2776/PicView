using System.Collections.ObjectModel;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Titles;
using R3;

namespace PicView.Core.ViewModels
{
    public enum ConversionTarget
    {
        Keep,
        Png,
        Jpg,
        Webp,
        Avif,
        Heic,
        Jxl
    }

    public enum CompressionMode
    {
        Lossless,
        Lossy
    }

    public enum ResizeMode
    {
        None,
        Percentage,
        Width,
        Height,
        WidthAndHeight
    }

    public class BatchResizeViewModel : IDisposable
    {
        private readonly bool _canNavigate;
        private CancellationTokenSource? _cts;
        private readonly FileInfo? _fileInfo;

        public BatchResizeViewModel(bool canNavigate, Func<Task<string>> selectDirectory, FileInfo? fileInfo,
            Func<FileInfo, List<FileInfo>> getFiles)
        {
            _canNavigate = canNavigate;
            _fileInfo = fileInfo;

            // Commands
            StartCommand = new ReactiveCommand(async (_, x) => await StartBatchResizeAsync(getFiles, x));
            CancelCommand = new ReactiveCommand(async (_, _) => await CancelAsync());
            ResetCommand = new ReactiveCommand(_ => { Reset(); });
            PickSourceFolderCommand = new ReactiveCommand(async (_, _) => await PickSourceFolder(selectDirectory));
            PickOutputFolderCommand = new ReactiveCommand(async (_, _) => await PickOutputFolder(selectDirectory));
            ToggleAspectRatioCommand = new ReactiveCommand(_ =>
            {
                IsKeepingAspectRatio.Value = !IsKeepingAspectRatio.Value;
            });

            // defaults
            IsKeepingAspectRatio.Value = true;
            Quality.Value = 75u;
            Compression.Value = CompressionMode.Lossless;
            Conversion.Value = ConversionTarget.Keep;
            Resize.Value = ResizeMode.None;
        }

        // Commands
        public ReactiveCommand StartCommand { get; }
        public ReactiveCommand CancelCommand { get; }
        public ReactiveCommand ResetCommand { get; }
        public ReactiveCommand PickSourceFolderCommand { get; }
        public ReactiveCommand PickOutputFolderCommand { get; }
        public ReactiveCommand ToggleAspectRatioCommand { get; }

        // Bindable properties (R3)
        public BindableReactiveProperty<string?> SourceFolder { get; } = new();
        public BindableReactiveProperty<string?> OutputFolder { get; } = new();

        public BindableReactiveProperty<bool> IsKeepingAspectRatio { get; } = new();
        public BindableReactiveProperty<bool> IsRunning { get; } = new();
        public BindableReactiveProperty<double> Progress { get; } = new();
        public BindableReactiveProperty<double> ProgressMaximum { get; } = new();

        public BindableReactiveProperty<ConversionTarget> Conversion { get; } = new();
        public BindableReactiveProperty<CompressionMode> Compression { get; } = new();

        public BindableReactiveProperty<bool> IsQualityEnabled { get; } = new();
        public BindableReactiveProperty<uint> Quality { get; } = new();

        public BindableReactiveProperty<ResizeMode> Resize { get; } = new();
        public BindableReactiveProperty<uint> WidthValue { get; } = new();
        public BindableReactiveProperty<uint> HeightValue { get; } = new();
        public BindableReactiveProperty<double> PercentageValue { get; } = new();

        // Logs
        public ObservableCollection<BatchLogEntry> LogEntries { get; } = [];

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async ValueTask PickOutputFolder(Func<Task<string>> selectDirectory)
        {
            var dir = await selectDirectory();
            if (!string.IsNullOrWhiteSpace(dir))
            {
                OutputFolder.Value = dir;
            }
        }

        private async ValueTask PickSourceFolder(Func<Task<string>> selectDirectory)
        {
            var dir = await selectDirectory();
            if (!string.IsNullOrWhiteSpace(dir))
            {
                SourceFolder.Value = dir;
            }
        }

        private async ValueTask CancelAsync()
        {
            await _cts?.CancelAsync();
        }

        private void Reset()
        {
            IsKeepingAspectRatio.Value = true;
            Progress.Value = 0;
            ProgressMaximum.Value = 0;
            IsRunning.Value = false;
            LogEntries.Clear();

            Conversion.Value = ConversionTarget.Keep;
            Compression.Value = CompressionMode.Lossless;
            IsQualityEnabled.Value = false;
            Quality.Value = 75;
            Resize.Value = ResizeMode.None;

            if (_canNavigate)
            {
                SourceFolder.Value = _fileInfo.DirectoryName ?? string.Empty;
            }
        }

        private async ValueTask StartBatchResizeAsync(Func<FileInfo, List<FileInfo>> getFiles,
            CancellationToken cancellationToken)
        {
            if (IsRunning.Value)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SourceFolder.Value))
            {
                return;
            }

            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cts.Token;

            try
            {
                IsRunning.Value = true;
                Progress.Value = 0;

                // collect files
                var files = getFiles(new FileInfo(SourceFolder.Value!));

                // ensure output folder
                var outFolder = string.IsNullOrWhiteSpace(OutputFolder.Value)
                    ? SourceFolder.Value!
                    : OutputFolder.Value!;
                if (!Directory.Exists(outFolder))
                {
                    Directory.CreateDirectory(outFolder);
                }

                ProgressMaximum.Value = files.Count;
                Progress.Value = 0;

                var options = new ParallelOptions
                {
                    CancellationToken = token,
                    MaxDegreeOfParallelism = Environment.ProcessorCount - 1
                };

                // capture a few options locally to avoid property reading during parallel loop
                var toConvert = Conversion.Value != ConversionTarget.Keep;
                var conversion = Conversion.Value;
                var qualityEnabled = IsQualityEnabled.Value;
                var qualityValue = Quality.Value;
                var losslessCompress = Compression.Value == CompressionMode.Lossless;

                await Parallel.ForEachAsync(files, options, async (file, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    var ext = file.Extension.ToLower();
                    var destination = Path.Combine(outFolder, file.Name);

                    // Determine target extension
                    if (toConvert)
                    {
                        static string GetExt(ConversionTarget t, FileInfo file) => t switch
                        {
                            ConversionTarget.Png => ".png",
                            ConversionTarget.Jpg => ".jpg",
                            ConversionTarget.Webp => ".webp",
                            ConversionTarget.Avif => ".avif",
                            ConversionTarget.Heic => ".heic",
                            ConversionTarget.Jxl => ".jxl",
                            _ => Path.GetExtension(file.FullName)
                        };

                        ext = GetExt(conversion, file);
                        destination = Path.ChangeExtension(destination, ext);
                    }

                    uint? quality = null;
                    if (qualityEnabled)
                    {
                        if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                            ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
                        {
                            quality = qualityValue;
                        }
                    }

                    // read original size using Magick ping
                    using var magick = new MagickImage();
                    magick.Ping(file.FullName);
                    var oldSize =
                        $" ({magick.Width} x {magick.Height}{ImageTitleFormatter.FormatAspectRatio((int)magick.Width, (int)magick.Height)}{file.Length.GetReadableFileSize()}";

                    await using var stream = FileStreamUtils.GetOptimizedFileStream(file, true);

                    // Determine width/height/percentage from VM properties
                    uint width = 0, height = 0;
                    Percentage? percentage = null;

                    switch (Resize.Value)
                    {
                        case ResizeMode.Percentage:
                            percentage = new Percentage(PercentageValue.Value);
                            break;
                        case ResizeMode.Width:
                            width = WidthValue.Value;
                            break;
                        case ResizeMode.Height:
                            height = HeightValue.Value;
                            break;
                        case ResizeMode.WidthAndHeight:
                            width = WidthValue.Value;
                            height = HeightValue.Value; // map second control accordingly in XAML
                            break;
                    }

                    var success = await SaveImageFileHelper.SaveImageAsync(
                        stream,
                        null,
                        destination,
                        width,
                        height,
                        quality,
                        ext,
                        null,
                        percentage,
                        losslessCompress,
                        !losslessCompress,
                        IsKeepingAspectRatio.Value
                    ).ConfigureAwait(false);

                    if (success)
                    {
                        using var newMagick = new MagickImage();
                        newMagick.Ping(destination);
                        var newFileInfo = new FileInfo(destination);
                        var newSize =
                            $" ({newMagick.Width} x {newMagick.Height}{ImageTitleFormatter.FormatAspectRatio((int)newMagick.Width, (int)newMagick.Height)}{newFileInfo.Length.GetReadableFileSize()}";

                        LogEntries.Add(new BatchLogEntry
                        {
                            FileName = file.Name,
                            OldSize = oldSize,
                            NewSize = newSize
                        });

                        Progress.Value++;
                    }
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // canceled
            }
            catch (Exception ex)
            {
                DebugHelper.LogDebug(nameof(BatchResizeViewModel), nameof(StartBatchResizeAsync), ex);
            }
            finally
            {
                IsRunning.Value = false;
                // Dispose cancellation token source but keep for future starts
            }
        }
    }
}