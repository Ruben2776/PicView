using System.Collections.ObjectModel;
using System.Diagnostics;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.Models;
using R3;

namespace PicView.Core.ViewModels
{
    public enum ConversionTarget
    {
        NoConversion,
        Png,
        Jpg,
        Webp,
        Avif,
        Heic,
        Jxl
    }

    public enum CompressionMode
    {
        None,
        Lossless,
        Lossy
    }

    public enum ResizeMode
    {
        None,
        Percentage,
        WidthAndHeight,
        Width,
        Height
    }

    public struct BatchThumb(
        string saveDestination,
        Percentage? percentage = null,
        double? width = null,
        double? height = null)
    {
        public string SaveDestination = saveDestination;
        public Percentage? Percentage = percentage;
        public double? Width = width;
        public double? Height = height;
    }

    public class BatchResizeViewModel : IDisposable
    {
        private readonly bool _canNavigate;
        private readonly FileInfo? _fileInfo;

        private readonly Func<FileInfo, List<FileInfo>> _getFiles;

        private readonly Lock _lock = new();
        private CancellationTokenSource? _cts;
        public BatchThumb[] Thumbs = new BatchThumb[7];

        public BatchResizeViewModel(bool canNavigate, Func<Task<string>> selectDirectory,
            Func<Task<string?>> selectFile, FileInfo? fileInfo,
            Func<FileInfo, List<FileInfo>> getFiles)
        {
            _canNavigate = canNavigate;
            _fileInfo = fileInfo;
            _getFiles = getFiles;

            // Commands
            StartCommand = new ReactiveCommand(async (_, x) => await StartBatchResizeAsync(x));
            CancelCommand = new ReactiveCommand(async (_, _) => await CancelAsync());
            CloseProgressCommand = new ReactiveCommand(_ =>
            {
                IsFinished.Value = false;
                IsRunning.Value = false;
            });
            ResetCommand = new ReactiveCommand(_ => { Reset(); });
            SelectAndAddFolderCommand = new ReactiveCommand(async (_, _) => await SelectAndAddFolder(selectDirectory));
            SelectAndAddFileCommand = new ReactiveCommand(async (_, _) => await SelectAndAddFile(selectFile));
            PickOutputFolderCommand = new ReactiveCommand(async (_, _) => await PickOutputFolder(selectDirectory));
            ToggleAspectRatioCommand = new ReactiveCommand(_ =>
            {
                IsKeepingAspectRatio.Value = !IsKeepingAspectRatio.Value;
            });
            ClearFilterCommand = new ReactiveCommand(_ => { FilterText.Value = string.Empty; });
            RemoveFileFromListCommand = new ReactiveCommand<FileInfo>((value, _) =>
            {
                SelectedFiles.Value.Remove(value);
                return ValueTask.CompletedTask;
            });
            RemoveAllCommand = new ReactiveCommand(_ => { SelectedFiles.Value.Clear(); });

            Debug.Assert(TranslationManager.Translation.NoConversion != null);

            ConversionTargets = new BindableReactiveProperty<string[]>([
                TranslationManager.Translation.NoConversion,
                nameof(ConversionTarget.Png),
                nameof(ConversionTarget.Jpg),
                nameof(ConversionTarget.Webp),
                nameof(ConversionTarget.Avif),
                nameof(ConversionTarget.Heic),
                nameof(ConversionTarget.Jxl)
            ]);

            Debug.Assert(TranslationManager.Translation.NoResize != null);
            Debug.Assert(TranslationManager.Translation.Width != null);
            Debug.Assert(TranslationManager.Translation.Height != null);
            Debug.Assert(TranslationManager.Translation.WidthAndHeight != null);
            Debug.Assert(TranslationManager.Translation.Percentage != null);

            ResizeModes = new BindableReactiveProperty<string[]>([
                TranslationManager.Translation.NoResize,
                TranslationManager.Translation.Width,
                TranslationManager.Translation.Height,
                TranslationManager.Translation.WidthAndHeight,
                TranslationManager.Translation.Percentage
            ]);

            ResizeModes = new BindableReactiveProperty<string[]>([
                TranslationManager.Translation.NoResize,
                TranslationManager.Translation.Percentage,
                TranslationManager.Translation.WidthAndHeight,
                TranslationManager.Translation.Width,
                TranslationManager.Translation.Height
            ]);

            Debug.Assert(TranslationManager.Translation.None != null);
            Debug.Assert(TranslationManager.Translation.Lossless != null);
            Debug.Assert(TranslationManager.Translation.Lossy != null);
            CompressionModes = new BindableReactiveProperty<string[]>([
                TranslationManager.Translation.None,
                TranslationManager.Translation.Lossless,
                TranslationManager.Translation.Lossy
            ]);

            ThumbnailAmounts = new BindableReactiveProperty<string[]>([
                TranslationManager.Translation.None,
                "1",
                "2",
                "3",
                "4",
                "5",
                "6",
                "7"
            ]);

            // defaults
            IsKeepingAspectRatio.Value = true;
            Quality.Value = 75;
            Compression.Value = CompressionMode.Lossless;
            Conversion.Value = ConversionTarget.NoConversion;
            Resize.Value = ResizeMode.None;

            Observable.EveryValueChanged(FilterText, x => x.CurrentValue)
                .Skip(1)
                .Subscribe(_ => { UpdateFilteredFiles(); });
        }

        public int ThumbnailAmount { get; set; }

        // Commands
        public ReactiveCommand StartCommand { get; }
        public ReactiveCommand CancelCommand { get; }
        public ReactiveCommand CloseProgressCommand { get; }
        public ReactiveCommand ResetCommand { get; }
        public ReactiveCommand SelectAndAddFolderCommand { get; }
        public ReactiveCommand SelectAndAddFileCommand { get; }
        public ReactiveCommand PickOutputFolderCommand { get; }
        public ReactiveCommand ToggleAspectRatioCommand { get; }

        public ReactiveCommand? ClearFilterCommand { get; }
        public ReactiveCommand? RemoveAllCommand { get; }

        public ReactiveCommand<FileInfo> RemoveFileFromListCommand { get; }

        // Bindable properties (R3) \\
        public BindableReactiveProperty<string?> SourceFolder { get; } = new();
        public BindableReactiveProperty<string?> OutputFolder { get; } = new();

        // Progress
        public BindableReactiveProperty<bool> IsIndeterminate { get; } = new(true);
        public BindableReactiveProperty<bool> IsRunning { get; } = new(false);
        public BindableReactiveProperty<bool> IsFinished { get; } = new(false);
        public BindableReactiveProperty<double> Progress { get; } = new();
        public BindableReactiveProperty<double> ProgressMaximum { get; } = new();

        public BindableReactiveProperty<ConversionTarget> Conversion { get; } = new();
        public BindableReactiveProperty<CompressionMode> Compression { get; } = new();

        // Quality
        public BindableReactiveProperty<bool> IsQualityEnabled { get; } = new();
        public BindableReactiveProperty<uint> Quality { get; } = new();

        // Resize
        public BindableReactiveProperty<ResizeMode> Resize { get; } = new();
        public BindableReactiveProperty<uint> SingleWidthValue { get; } = new();
        public BindableReactiveProperty<uint> SingleHeightValue { get; } = new();
        public BindableReactiveProperty<uint> WidthValue { get; } = new();
        public BindableReactiveProperty<uint> HeightValue { get; } = new();
        public BindableReactiveProperty<double> PercentageValue { get; } = new(100);
        public BindableReactiveProperty<bool> IsKeepingAspectRatio { get; } = new(true);
        public BindableReactiveProperty<bool> IsPercentageResizing { get; } = new();
        public BindableReactiveProperty<bool> IsWidthAndHeightResizing { get; } = new();
        public BindableReactiveProperty<bool> IsWidthResizing { get; } = new();
        public BindableReactiveProperty<bool> IsHeightResizing { get; } = new();

        public BindableReactiveProperty<string?> FilterText { get; } = new(string.Empty);

        public BindableReactiveProperty<string[]> ConversionTargets { get; }
        public BindableReactiveProperty<string[]> ResizeModes { get; }
        public BindableReactiveProperty<string[]> CompressionModes { get; }
        public BindableReactiveProperty<string[]> ThumbnailAmounts { get; }


        // Logs
        public BindableReactiveProperty<ObservableCollection<FileInfo>> SelectedFiles { get; } = new();
        public BindableReactiveProperty<ObservableCollection<FileInfo>> FilteredFiles { get; } = new();
        public BindableReactiveProperty<ObservableCollection<BatchLogEntry>>? ProcessedFiles { get; } = new([]);
        public BindableReactiveProperty<bool> IsFiltering { get; } = new(false);

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private void UpdateFilteredFiles()
        {
            if (string.IsNullOrWhiteSpace(FilterText.CurrentValue))
            {
                FilteredFiles.Value.Clear();
                IsFiltering.Value = false;
                return;
            }

            var filtered = SelectedFiles.CurrentValue
                .Where(file => file.Name.Contains(FilterText.CurrentValue, StringComparison.OrdinalIgnoreCase));

            FilteredFiles.Value = new ObservableCollection<FileInfo>(filtered);
            IsFiltering.Value = true;
        }

        private async ValueTask PickOutputFolder(Func<Task<string>> selectDirectory)
        {
            var dir = await selectDirectory();
            if (!string.IsNullOrWhiteSpace(dir))
            {
                OutputFolder.Value = dir;
            }
        }

        private async ValueTask SelectAndAddFolder(Func<Task<string>> selectDirectory)
        {
            var directory = await selectDirectory();
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            var files = _getFiles.Invoke(new FileInfo(directory));
            foreach (var file in files)
            {
                SelectedFiles.Value.Add(file);
            }
        }

        private async ValueTask SelectAndAddFile(Func<Task<string?>> selectFile)
        {
            var file = await selectFile();
            if (!string.IsNullOrWhiteSpace(file))
            {
                SelectedFiles.Value.Add(new FileInfo(file));
            }
        }

        private async ValueTask CancelAsync()
        {
            await _cts?.CancelAsync();
            IsFinished.Value = false;
            IsRunning.Value = false;
        }

        private void Reset()
        {
            IsKeepingAspectRatio.Value = true;
            Progress.Value = 0;
            ProgressMaximum.Value = 100;
            IsRunning.Value = false;

            Conversion.Value = ConversionTarget.NoConversion;
            Compression.Value = CompressionMode.Lossless;
            IsQualityEnabled.Value = false;
            Quality.Value = 75;
            Resize.Value = ResizeMode.None;

            if (_canNavigate)
            {
                SourceFolder.Value = _fileInfo.DirectoryName ?? string.Empty;
            }
        }

        private async ValueTask StartBatchResizeAsync(CancellationToken cancellationToken)
        {
            if (IsRunning.Value)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(OutputFolder.Value))
            {
                return;
            }

            IsRunning.Value = true;

            await Task.Run(async () =>
            {
                _cts?.Dispose();
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                string? backupCopyDir = null;

                try
                {
                    var destinationDirectory = OutputFolder.CurrentValue;

                    if (!Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    var options = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount - 2,
                        CancellationToken = cancellationToken
                    };

                    if (SelectedFiles.Value.FirstOrDefault().DirectoryName.Equals(destinationDirectory))
                    {
                        // First create a backup directory in case something goes wrong
                        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                        var backupDirName = $"PicView {TranslationManager.Translation.BatchResize} backup {timestamp}";
                        backupCopyDir = Path.Combine(destinationDirectory, backupDirName);

                        if (!Directory.Exists(backupCopyDir))
                        {
                            Directory.CreateDirectory(backupCopyDir);
                        }

                        // Copy all files to backup directory
                        await Parallel.ForEachAsync(SelectedFiles.Value, options, (file, _) =>
                        {
                            var destFileName = Path.Combine(backupCopyDir, file.Name);
                            File.Copy(file.FullName, destFileName, false);
                            return ValueTask.CompletedTask;
                        });
                    }


                    Progress.Value = 0;
                    ProgressMaximum.Value = SelectedFiles.Value.Count;
                    ProcessedFiles.Value.Clear();

                    IsIndeterminate.Value = false;
                    await Parallel.ForEachAsync(SelectedFiles.Value, options, (file, _) =>
                    {
                        _cts.Token.ThrowIfCancellationRequested();

                        var sourceFileName = file.FullName;

                        var outputLogFileName = Path.Combine(file.Directory.Name, file.Name);
                        var oldSize = file.Length.GetReadableFileSize();

                        try
                        {
                            var magick = new MagickImage(sourceFileName);
                            string destinationFileName;
                            switch (Conversion.Value)
                            {
                                default:
                                case ConversionTarget.NoConversion:
                                    destinationFileName = Path.Combine(destinationDirectory, file.Name);
                                    break;
                                case ConversionTarget.Avif:
                                    destinationFileName = Path.Combine(destinationDirectory,
                                        Path.ChangeExtension(file.Name, ".avif"));
                                    magick.Format = MagickFormat.Avif;
                                    break;
                                case ConversionTarget.Heic:
                                    destinationFileName = Path.Combine(destinationDirectory,
                                        Path.ChangeExtension(file.Name, ".heic"));
                                    magick.Format = MagickFormat.Heic;
                                    break;
                                case ConversionTarget.Jpg:
                                    destinationFileName = Path.Combine(destinationDirectory,
                                        Path.ChangeExtension(file.Name, ".jpg"));
                                    magick.Format = MagickFormat.Jpg;
                                    break;
                                case ConversionTarget.Jxl:
                                    destinationFileName = Path.Combine(destinationDirectory,
                                        Path.ChangeExtension(file.Name, ".jxl"));
                                    magick.Format = MagickFormat.Jxl;
                                    break;
                                case ConversionTarget.Png:
                                    destinationFileName = Path.Combine(destinationDirectory,
                                        Path.ChangeExtension(file.Name, ".png"));
                                    magick.Format = MagickFormat.Png;
                                    break;
                                case ConversionTarget.Webp:
                                    destinationFileName = Path.Combine(destinationDirectory,
                                        Path.ChangeExtension(file.Name, ".webp"));
                                    magick.Format = MagickFormat.WebP;
                                    break;
                            }

                            if (IsPercentageResizing.Value)
                            {
                                if (PercentageValue.Value > 0)
                                {
                                    magick.Resize(new Percentage(PercentageValue.Value));
                                }
                            }
                            else if (IsWidthAndHeightResizing.Value)
                            {
                                var w = WidthValue.CurrentValue;
                                var h = HeightValue.CurrentValue;
                                if (w > 0 && h > 0)
                                {
                                    if (IsKeepingAspectRatio.Value)
                                    {
                                        magick.Resize(w, h);
                                    }
                                    else
                                    {
                                        var geometry = new MagickGeometry(w, h) { IgnoreAspectRatio = true };
                                        magick.Resize(geometry);
                                    }
                                }
                            }
                            else if (IsWidthResizing.Value)
                            {
                                magick.Resize(SingleWidthValue.Value, 0);
                            }
                            else if (IsHeightResizing.Value)
                            {
                                magick.Resize(0, SingleHeightValue.Value);
                            }

                            magick.Write(destinationFileName);
                            if (Compression.Value is CompressionMode.Lossless or CompressionMode.Lossy)
                            {
                                ImageOptimizer imageOptimizer = new()
                                {
                                    OptimalCompression = Compression.Value is CompressionMode.Lossless
                                };
                                if (imageOptimizer.IsSupported(destinationFileName))
                                {
                                    imageOptimizer.Compress(destinationFileName);
                                }
                            }

                            var newFile = new FileInfo(destinationFileName);

                            var newSize = newFile.Length.GetReadableFileSize();
                            var entry = new BatchLogEntry
                            {
                                FileName = outputLogFileName,
                                NewSize = newSize,
                                OldSize = oldSize
                            };
                            for (var i = 0; i < ThumbnailAmount; i++)
                            {
                                var thumb = Thumbs[i];
                                var thumbDir = Path.Combine(destinationDirectory, thumb.SaveDestination);
                                var thumbPath = Path.Combine(thumbDir, Path.GetFileName(destinationFileName));
                                if (thumb.Percentage is not null)
                                {
                                    magick.Resize(thumb.Percentage.Value);
                                }
                                else if (thumb.Width is not null)
                                {
                                    magick.Resize((uint)thumb.Width.Value, 0);
                                }
                                else if (thumb.Height is not null)
                                {
                                    magick.Resize(0, (uint)thumb.Height.Value);
                                }

                                if (!Directory.Exists(thumbDir))
                                {
                                    Directory.CreateDirectory(thumbDir);
                                }

                                magick.Write(thumbPath);
                                var thumbSize = new FileInfo(thumbPath).Length.GetReadableFileSize();
                                lock (_lock)
                                {
                                    ProcessedFiles.Value.Add(new BatchLogEntry
                                    {
                                        FileName =
                                            $"{TranslationManager.Translation.Thumbnail}: {Path.Combine(thumb.SaveDestination, outputLogFileName)}",
                                        NewSize = thumbSize,
                                        OldSize = oldSize
                                    });
                                }
                            }

                            // Update output and progress. Needs to lock to not overwhelm UI.
                            lock (_lock)
                            {
                                ProcessedFiles.Value.Add(entry);
                                Progress.Value += 1;
                            }
                        }
                        catch (Exception e)
                        {
                            DebugHelper.LogDebug(nameof(BatchResizeViewModel), nameof(StartBatchResizeAsync), e);
                            lock (_lock)
                            {
                                ProcessedFiles.Value.Add(new BatchLogEntry
                                {
                                    FileName = outputLogFileName + Environment.NewLine + e.Message,
                                    NewSize = "--",
                                    OldSize = oldSize
                                });

                                Progress.Value += 1;
                            }
                        }

                        return ValueTask.CompletedTask;
                    });
                    IsFinished.Value = true;
                }
                catch (OperationCanceledException)
                {
                    // canceled
                    DebugHelper.LogDebug(nameof(BatchResizeViewModel), nameof(StartBatchResizeAsync), "Cancelled");
                    Progress.Value = 0;
                }
                catch (Exception ex)
                {
                    DebugHelper.LogDebug(nameof(BatchResizeViewModel), nameof(StartBatchResizeAsync), ex);
                }
            }, cancellationToken);
        }
    }
}