using System.Collections.ObjectModel;
using PicView.Core.BatchResize;
using PicView.Core.DebugTools;
using PicView.Core.Localization;
using PicView.Core.Models;
using R3;
using RememberedSettings = PicView.Core.Config.BatchResizeWindowConfig.BatchResizeWindowProperties;
using RememberedThumbnail = PicView.Core.Config.BatchResizeWindowConfig.RememberedThumbnail;

namespace PicView.Core.ViewModels;

/// <summary>
/// The settings of the next Batch Resize Job as the user edits them, and the progress of the running one. There is one
/// for the whole app, like its window.
/// </summary>
public sealed class BatchResizeViewModel : IDisposable
{
    private readonly Func<FileInfo, List<FileInfo>> _getFiles;
    private readonly RememberedSettings _remembered;
    private readonly Lock _lock = new();
    private CancellationTokenSource? _cts;
    private DisposableBag _disposables;
    private double _aspectRatio;
    private bool _isLinkingSize;

    /// <param name="selectDirectory">Asks the user for a folder.</param>
    /// <param name="selectFile">Asks the user for a file.</param>
    /// <param name="getFiles">Lists the images in the folder of a file.</param>
    /// <param name="locateOnDisk">Shows a folder in the file manager.</param>
    /// <param name="remembered">The settings the last Batch Resize window left, which <see cref="RememberSettings"/>
    /// writes back to.</param>
    public BatchResizeViewModel(Func<Task<string>> selectDirectory, Func<Task<string?>> selectFile,
        Func<FileInfo, List<FileInfo>> getFiles, Action<string> locateOnDisk, RememberedSettings remembered)
    {
        _getFiles = getFiles;
        _remembered = remembered;

        ConversionChoices =
        [
            new(ConversionTarget.NoConversion, TranslationManager.Translation.NoConversion ?? string.Empty),
            new(ConversionTarget.Png, nameof(ConversionTarget.Png)),
            new(ConversionTarget.Jpg, nameof(ConversionTarget.Jpg)),
            new(ConversionTarget.Webp, nameof(ConversionTarget.Webp)),
            new(ConversionTarget.Avif, nameof(ConversionTarget.Avif)),
            new(ConversionTarget.Heic, nameof(ConversionTarget.Heic)),
            new(ConversionTarget.Jxl, nameof(ConversionTarget.Jxl))
        ];
        CompressionChoices =
        [
            new(CompressionMode.None, TranslationManager.Translation.None ?? string.Empty),
            new(CompressionMode.Lossless, TranslationManager.Translation.Lossless ?? string.Empty),
            new(CompressionMode.Lossy, TranslationManager.Translation.Lossy ?? string.Empty)
        ];
        ResizeChoices =
        [
            new(ResizeMode.None, TranslationManager.Translation.NoResize ?? string.Empty),
            new(ResizeMode.Percentage, TranslationManager.Translation.Percentage ?? string.Empty),
            new(ResizeMode.WidthAndHeight, TranslationManager.Translation.WidthAndHeight ?? string.Empty),
            new(ResizeMode.Width, TranslationManager.Translation.Width ?? string.Empty),
            new(ResizeMode.Height, TranslationManager.Translation.Height ?? string.Empty)
        ];
        ThumbnailSizeChoices =
        [
            new(ThumbnailSize.Percentage, TranslationManager.Translation.Percentage ?? string.Empty),
            new(ThumbnailSize.Width, TranslationManager.Translation.Width ?? string.Empty),
            new(ThumbnailSize.Height, TranslationManager.Translation.Height ?? string.Empty)
        ];
        ThumbnailAmountChoices =
        [
            new(0, TranslationManager.Translation.None ?? string.Empty),
            .. Enumerable.Range(1, BatchThumbnail.Max).Select(static amount =>
                new Choice<int>(amount, amount.ToString(System.Globalization.CultureInfo.InvariantCulture)))
        ];

        IsPercentageResizing = IsResizing(ResizeMode.Percentage);
        IsWidthAndHeightResizing = IsResizing(ResizeMode.WidthAndHeight);
        IsWidthResizing = IsResizing(ResizeMode.Width);
        IsHeightResizing = IsResizing(ResizeMode.Height);

        Restore();

        StartCommand = new ReactiveCommand(async (_, _) => await StartAsync().ConfigureAwait(false));
        CancelCommand = new ReactiveCommand(async (_, _) => await CancelAsync().ConfigureAwait(false));
        CloseProgressCommand = new ReactiveCommand(_ =>
        {
            IsFinished.Value = false;
            IsRunning.Value = false;
        });
        SelectAndAddFolderCommand = new ReactiveCommand(async (_, _) =>
        {
            var directory = await selectDirectory().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                foreach (var file in _getFiles(new FileInfo(directory)))
                {
                    SelectedFiles.Value.Add(file);
                }
            }
        });
        SelectAndAddFileCommand = new ReactiveCommand(async (_, _) =>
        {
            var file = await selectFile().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(file))
            {
                SelectedFiles.Value.Add(new FileInfo(file));
            }
        });
        PickOutputFolderCommand = new ReactiveCommand(async (_, _) =>
        {
            var directory = await selectDirectory().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                OutputFolder.Value = directory;
            }
        });
        ShowInFolderCommand = new ReactiveCommand(_ =>
        {
            if (!string.IsNullOrWhiteSpace(OutputFolder.Value))
            {
                locateOnDisk(OutputFolder.Value);
            }
        });
        ToggleAspectRatioCommand = new ReactiveCommand(_ => IsKeepingAspectRatio.Value = !IsKeepingAspectRatio.Value);
        ClearFilterCommand = new ReactiveCommand(_ => FilterText.Value = string.Empty);
        RemoveAllCommand = new ReactiveCommand(_ => SelectedFiles.Value.Clear());
        RemoveFileFromListCommand = new ReactiveCommand<FileInfo>(file => SelectedFiles.Value.Remove(file));
        foreach (var command in new IDisposable[]
                 {
                     StartCommand, CancelCommand, CloseProgressCommand, SelectAndAddFolderCommand,
                     SelectAndAddFileCommand, PickOutputFolderCommand, ShowInFolderCommand, ToggleAspectRatioCommand,
                     ClearFilterCommand, RemoveAllCommand, RemoveFileFromListCommand
                 })
        {
            _disposables.Add(command);
        }

        Width.Skip(1).Subscribe(width => LinkSize(fromWidth: true, width)).AddTo(ref _disposables);
        Height.Skip(1).Subscribe(height => LinkSize(fromWidth: false, height)).AddTo(ref _disposables);
        IsKeepingAspectRatio.Where(static isKeeping => isKeeping).Subscribe(_ =>
        {
            if (Width.Value > 0 && Height.Value > 0)
            {
                _aspectRatio = (double)Width.Value / Height.Value;
            }
        }).AddTo(ref _disposables);
        ThumbnailAmount.Skip(1).Subscribe(amount => SetThumbnails(BatchThumbnail.Defaults(amount)))
            .AddTo(ref _disposables);
        FilterText.Skip(1).Subscribe(_ => UpdateFilteredFiles(),
            DebugHelper.LogError(nameof(BatchResizeViewModel), nameof(UpdateFilteredFiles))).AddTo(ref _disposables);
    }

    public IReadOnlyList<Choice<ConversionTarget>> ConversionChoices { get; }
    public IReadOnlyList<Choice<CompressionMode>> CompressionChoices { get; }
    public IReadOnlyList<Choice<ResizeMode>> ResizeChoices { get; }
    public IReadOnlyList<Choice<ThumbnailSize>> ThumbnailSizeChoices { get; }
    public IReadOnlyList<Choice<int>> ThumbnailAmountChoices { get; }

    // Commands
    public ReactiveCommand StartCommand { get; }
    public ReactiveCommand CancelCommand { get; }
    public ReactiveCommand CloseProgressCommand { get; }
    public ReactiveCommand SelectAndAddFolderCommand { get; }
    public ReactiveCommand SelectAndAddFileCommand { get; }
    public ReactiveCommand PickOutputFolderCommand { get; }
    public ReactiveCommand ShowInFolderCommand { get; }
    public ReactiveCommand ToggleAspectRatioCommand { get; }
    public ReactiveCommand ClearFilterCommand { get; }
    public ReactiveCommand RemoveAllCommand { get; }
    public ReactiveCommand<FileInfo> RemoveFileFromListCommand { get; }

    // Files
    public BindableReactiveProperty<ObservableCollection<FileInfo>> SelectedFiles { get; } = new([]);
    public BindableReactiveProperty<ObservableCollection<FileInfo>> FilteredFiles { get; } = new([]);
    public BindableReactiveProperty<string?> FilterText { get; } = new(string.Empty, StringComparer.OrdinalIgnoreCase);
    public BindableReactiveProperty<bool> IsFiltering { get; } = new(false);
    public BindableReactiveProperty<string?> OutputFolder { get; } = new();

    // Settings
    public BindableReactiveProperty<ConversionTarget> Conversion { get; } = new();
    public BindableReactiveProperty<CompressionMode> Compression { get; } = new();
    public BindableReactiveProperty<bool> IsQualityEnabled { get; } = new();
    public BindableReactiveProperty<uint> Quality { get; } = new(75);
    public BindableReactiveProperty<ResizeMode> Resize { get; } = new();
    public BindableReactiveProperty<uint> Width { get; } = new();
    public BindableReactiveProperty<uint> Height { get; } = new();
    public BindableReactiveProperty<double> Percentage { get; } = new(100);
    public BindableReactiveProperty<bool> IsKeepingAspectRatio { get; } = new(true);
    public IReadOnlyBindableReactiveProperty<bool> IsPercentageResizing { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsWidthAndHeightResizing { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsWidthResizing { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsHeightResizing { get; }

    /// <summary>
    /// How many Batch Thumbnails the job makes. Changing it replaces <see cref="Thumbnails"/> with that many defaults.
    /// </summary>
    public BindableReactiveProperty<int> ThumbnailAmount { get; } = new();

    public ObservableCollection<BatchThumbnailRow> Thumbnails { get; } = [];

    // Progress
    public BindableReactiveProperty<bool> IsIndeterminate { get; } = new(true);

    /// <summary>
    /// Whether the progress of a run is shown, from when it starts until the user closes it.
    /// </summary>
    public BindableReactiveProperty<bool> IsRunning { get; } = new(false);
    public BindableReactiveProperty<bool> IsFinished { get; } = new(false);
    public BindableReactiveProperty<double> Progress { get; } = new();
    public BindableReactiveProperty<double> ProgressMaximum { get; } = new();
    public BindableReactiveProperty<ObservableCollection<BatchLogEntry>> ProcessedFiles { get; } = new([]);

    /// <summary>
    /// Fills the file list and output folder from the folder of a file, and the size from its image.
    /// </summary>
    public void Prefill(FileInfo? file, uint pixelWidth, uint pixelHeight)
    {
        SelectedFiles.Value = file is null ? [] : new ObservableCollection<FileInfo>(_getFiles(file));
        if (!string.IsNullOrWhiteSpace(file?.DirectoryName))
        {
            OutputFolder.Value = Path.Combine(file.DirectoryName, TranslationManager.Translation.BatchResize ?? "");
        }

        _isLinkingSize = true;
        Width.Value = pixelWidth;
        Height.Value = pixelHeight;
        _isLinkingSize = false;
        _aspectRatio = pixelWidth > 0 && pixelHeight > 0 ? (double)pixelWidth / pixelHeight : 0;
    }

    /// <summary>
    /// The Batch Resize Job the current settings make.
    /// </summary>
    public BatchResizeJob CreateJob() => new()
    {
        Files = [.. SelectedFiles.Value],
        OutputFolder = OutputFolder.Value ?? string.Empty,
        Conversion = Conversion.Value,
        Compression = Compression.Value,
        Quality = IsQualityEnabled.Value ? Quality.Value : null,
        Resize = Resize.Value,
        Width = Width.Value,
        Height = Height.Value,
        Percentage = Percentage.Value,
        KeepAspectRatio = IsKeepingAspectRatio.Value,
        Thumbnails = [.. Thumbnails.Select(static row => row.ToThumbnail())]
    };

    /// <summary>
    /// Writes the current settings to the remembered settings, for the next Batch Resize window.
    /// </summary>
    public void RememberSettings()
    {
        _remembered.Conversion = Conversion.Value.ToString();
        _remembered.Compression = Compression.Value.ToString();
        _remembered.Resize = Resize.Value.ToString();
        _remembered.IsQualityEnabled = IsQualityEnabled.Value;
        _remembered.Quality = Quality.Value;
        _remembered.KeepAspectRatio = IsKeepingAspectRatio.Value;
        _remembered.Thumbnails = [.. Thumbnails.Select(static row => new RememberedThumbnail
        {
            Subfolder = row.Subfolder.Value,
            Size = row.Size.Value.ToString(),
            Value = row.Value.Value
        })];
        _remembered.ConvertToIndex = null;
        _remembered.CompressionIndex = null;
        _remembered.ResizeIndex = null;
        _remembered.GenerateThumbnailsIndex = null;
    }

    private void Restore()
    {
        Conversion.Value = Parse(_remembered.Conversion, _remembered.ConvertToIndex, ConversionTarget.NoConversion);
        Compression.Value = Parse(_remembered.Compression, _remembered.CompressionIndex, CompressionMode.Lossless);
        Resize.Value = Parse(_remembered.Resize, _remembered.ResizeIndex, ResizeMode.None);
        IsQualityEnabled.Value = _remembered.IsQualityEnabled;
        Quality.Value = _remembered.Quality ?? 75;
        IsKeepingAspectRatio.Value = _remembered.KeepAspectRatio;

        var thumbnails = _remembered.Thumbnails?.Take(BatchThumbnail.Max).Select(static thumbnail =>
                new BatchThumbnail(thumbnail.Subfolder, Parse(thumbnail.Size, null, ThumbnailSize.Percentage),
                    thumbnail.Value)).ToList() ??
            BatchThumbnail.Defaults(Math.Clamp(_remembered.GenerateThumbnailsIndex ?? 0, 0, BatchThumbnail.Max));
        SetThumbnails(thumbnails);
        ThumbnailAmount.Value = thumbnails.Count;
    }

    private static T Parse<T>(string? name, int? index, T fallback) where T : struct, Enum
    {
        if (Enum.TryParse<T>(name, out var value))
        {
            return value;
        }

        // Remembered by index, in the order the drop-downs had, which is the order of the enums
        if (index is { } i && (T)(object)i is var byIndex && Enum.IsDefined(byIndex))
        {
            return byIndex;
        }

        return fallback;
    }

    private IReadOnlyBindableReactiveProperty<bool> IsResizing(ResizeMode mode)
    {
        var isResizing = Resize.Select(current => current == mode).ToReadOnlyBindableReactiveProperty();
        _disposables.Add(isResizing);
        return isResizing;
    }

    private void SetThumbnails(IEnumerable<BatchThumbnail> thumbnails)
    {
        foreach (var row in Thumbnails)
        {
            row.Dispose();
        }

        Thumbnails.Clear();
        foreach (var thumbnail in thumbnails)
        {
            Thumbnails.Add(new BatchThumbnailRow(Thumbnails.Count + 1, thumbnail, ThumbnailSizeChoices));
        }
    }

    /// <summary>
    /// Keeps the width and height in the image's proportions while the aspect ratio is kept.
    /// </summary>
    private void LinkSize(bool fromWidth, uint changed)
    {
        if (_isLinkingSize || !IsKeepingAspectRatio.Value || _aspectRatio <= 0 || changed == 0)
        {
            return;
        }

        _isLinkingSize = true;
        try
        {
            if (fromWidth)
            {
                Height.Value = (uint)Math.Max(1, Math.Round(changed / _aspectRatio));
            }
            else
            {
                Width.Value = (uint)Math.Max(1, Math.Round(changed * _aspectRatio));
            }
        }
        finally
        {
            _isLinkingSize = false;
        }
    }

    private void UpdateFilteredFiles()
    {
        if (string.IsNullOrWhiteSpace(FilterText.CurrentValue))
        {
            FilteredFiles.Value.Clear();
            IsFiltering.Value = false;
            return;
        }

        FilteredFiles.Value = new ObservableCollection<FileInfo>(SelectedFiles.CurrentValue
            .Where(file => file.Name.Contains(FilterText.CurrentValue, StringComparison.OrdinalIgnoreCase)));
        IsFiltering.Value = true;
    }

    private async ValueTask StartAsync()
    {
        if (IsRunning.Value || SelectedFiles.Value.Count == 0 || string.IsNullOrWhiteSpace(OutputFolder.Value))
        {
            return;
        }

        var job = CreateJob();
        var context = SynchronizationContext.Current;
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        ProcessedFiles.Value.Clear();
        Progress.Value = 0;
        ProgressMaximum.Value = job.Files.Count;
        IsIndeterminate.Value = false;
        IsFinished.Value = false;
        IsRunning.Value = true;

        try
        {
            await Task.Run(() => BatchResizer.RunAsync(job, entry => OnUiThread(context, () => Log(entry)), token),
                token).ConfigureAwait(false);
            OnUiThread(context, () => IsFinished.Value = true);
        }
        catch (OperationCanceledException)
        {
            OnUiThread(context, () =>
            {
                Progress.Value = 0;
                IsRunning.Value = false;
            });
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(BatchResizeViewModel), nameof(StartAsync), e);
            OnUiThread(context, () =>
            {
                Log(new BatchLogEntry { FileName = e.Message, OldSize = "--", NewSize = "--" });
                IsFinished.Value = true;
            });
        }
    }

    private void Log(BatchLogEntry entry)
    {
        lock (_lock)
        {
            ProcessedFiles.Value.Add(entry);
            if (!entry.IsThumbnail)
            {
                Progress.Value += 1;
            }
        }
    }

    /// <summary>
    /// Runs on the thread the run was started from, so the bound collection and progress are changed there.
    /// </summary>
    private static void OnUiThread(SynchronizationContext? context, Action action)
    {
        if (context is null)
        {
            action();
        }
        else
        {
            context.Post(static state => ((Action)state!)(), action);
        }
    }

    private async ValueTask CancelAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
        }

        IsFinished.Value = false;
        IsRunning.Value = false;
    }

    /// <summary>
    /// Cancels a run that is still going, as when the window closes or App Shutdown ends it.
    /// </summary>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        foreach (var row in Thumbnails)
        {
            row.Dispose();
        }

        _disposables.Dispose();
    }
}
