using System.Globalization;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Saving;
using R3;

namespace PicView.Core.ViewModels;

public class ResizeImageViewModel : IDisposable
{
    private double _aspectRatio;
    private DisposableBag _disposables;
    private bool _isInternalChange;
    private string? _lastTempFilePath;

    public BindableReactiveProperty<uint> OriginalPixelWidth { get; } = new();
    public BindableReactiveProperty<uint> OriginalPixelHeight { get; } = new();
    public BindableReactiveProperty<string> OriginalFileSize { get; } = new();
    public BindableReactiveProperty<string> DesiredPixelWidth { get; } = new();
    public BindableReactiveProperty<string> DesiredPixelHeight { get; } = new();
    public BindableReactiveProperty<string> OutputFileSize { get; } = new();
    public BindableReactiveProperty<double> Quality { get; } = new(90);
    public BindableReactiveProperty<bool> IsQualityEnabled { get; } = new(true);
    public BindableReactiveProperty<int> SelectedConversionIndex { get; } = new(0);
    public BindableReactiveProperty<bool> IsKeepingAspectRatio { get; } = new(true);
    public BindableReactiveProperty<bool> IsLoading { get; } = new();
    public BindableReactiveProperty<bool> ShowReset { get; } = new();

    public Action? CloseAction { get; set; }
    public Func<string, string, ValueTask<string?>>? PickFileAction { get; set; }

    /// <summary>Shows the user a message, such as that the name a conversion needs is taken.</summary>
    public Action<string>? ShowMessageAction { get; set; }

    private MainWindowViewModel? _mainVm;

    public void Initialize(MainWindowViewModel mainVm)
    {
        _mainVm = mainVm;

        Observable.EveryValueChanged(_mainVm.WindowTabs.ActiveTab.Value, tab => tab.Model).Subscribe(UpdateFromImageChange, 
                DebugHelper.LogError(nameof(ResizeImageViewModel), nameof(AdjustAspectRatioCore)))
            .AddTo(ref _disposables);

        DesiredPixelWidth.Subscribe(_ =>
        {
            AdjustAspectRatio(isWidth: true);
        }, DebugHelper.LogError(nameof(ResizeImageViewModel), nameof(AdjustAspectRatioCore)))
        .AddTo(ref _disposables);

        DesiredPixelHeight.Subscribe(_ =>
        {
            AdjustAspectRatio(isWidth: false);
        }, DebugHelper.LogError(nameof(ResizeImageViewModel), nameof(AdjustAspectRatioCore)))
        .AddTo(ref _disposables);

        SelectedConversionIndex.Subscribe(_ =>
        {
            ReAdjustQualitySliderFromConversion();
        }, DebugHelper.LogError(nameof(ResizeImageViewModel), nameof(ReAdjustQualitySliderFromConversion)))
        .AddTo(ref _disposables);

        Quality.Subscribe(_ => ShowReset.Value = true,
                DebugHelper.LogError(nameof(ResizeImageViewModel), nameof(Quality)))
            .AddTo(ref _disposables);

        Observable.Merge(
            DesiredPixelWidth.Select(_ => Unit.Default),
            DesiredPixelHeight.Select(_ => Unit.Default),
            Quality.Select(_ => Unit.Default),
            SelectedConversionIndex.Select(_ => Unit.Default)
        )
        .Debounce(TimeSpan.FromMilliseconds(50))
        .SubscribeAwait(async (_, _) =>
        {
            await UpdateOutputFileSizeAsync().ConfigureAwait(false);
        }, DebugHelper.LogError(nameof(ResizeImageViewModel), nameof(UpdateOutputFileSizeAsync)))
        .AddTo(ref _disposables);
    }

    private void UpdateFromImageChange(ImageModel? model)
    {
        if (model is null)
        {
            return;
        }
        OriginalPixelWidth.Value = _mainVm.WindowTabs.ActiveTab.CurrentValue.Model.PixelWidth;
        OriginalPixelHeight.Value = _mainVm.WindowTabs.ActiveTab.CurrentValue.Model.PixelHeight;
        
        _aspectRatio = (double)OriginalPixelWidth.Value / OriginalPixelHeight.Value;
            
        DesiredPixelWidth.Value = model.PixelWidth.ToString();
        DesiredPixelHeight.Value = model.PixelHeight.ToString();

        var tab = _mainVm.WindowTabs.ActiveTab.CurrentValue;
        if (tab.FileInfo.CurrentValue != null)
        {
            UpdateQualitySliderState(tab.FileInfo.CurrentValue);
        }

        if (model.FileInfo != null && File.Exists(model.FileInfo.FullName))
        {
            OriginalFileSize.Value = model.FileInfo.Length.GetReadableFileSize();
        }
        else if (model.FileInfo != null)
        {
            try
            {
                OriginalFileSize.Value = model.FileInfo.Length.GetReadableFileSize();
            }
            catch
            {
                OriginalFileSize.Value = string.Empty;
            }
        }
        else
        {
            OriginalFileSize.Value = string.Empty;
        }

        _ = UpdateOutputFileSizeAsync();
    }

    private void ReAdjustQualitySliderFromConversion()
    {
        var tab = _mainVm.WindowTabs.ActiveTab.CurrentValue;
        if (tab?.FileInfo.CurrentValue != null)
        {
            UpdateQualitySliderState(tab.FileInfo.CurrentValue);
        }
        ShowReset.Value = true;
    }

    private void AdjustAspectRatio(bool isWidth)
    {
        if (_isInternalChange)
        {
            return;
        }
        if (IsKeepingAspectRatio.Value)
        {
            _isInternalChange = true;
            AdjustAspectRatioCore(isWidth);
            _isInternalChange = false;
        }
        ShowReset.Value = true;
    }

    private void AdjustAspectRatioCore(bool isWidth)
    {
        var text = isWidth ? DesiredPixelWidth.Value : DesiredPixelHeight.Value;
        var percentage = text.GetPercentage();

        var tab = _mainVm.WindowTabs.ActiveTab.CurrentValue;
        if (tab == null) return;

        var pixelWidth = tab.Model.PixelWidth;
        var pixelHeight = tab.Model.PixelHeight;

        if (percentage > 0)
        {
            var newWidth = (uint)Math.Clamp(pixelWidth * (percentage / 100), uint.MinValue, uint.MaxValue);
            var newHeight = (uint)Math.Clamp(pixelHeight * (percentage / 100), uint.MinValue, uint.MaxValue);

            DesiredPixelWidth.Value = newWidth.ToString();
            DesiredPixelHeight.Value = newHeight.ToString();
        }
        else
        {
            if (isWidth)
            {
                if (!uint.TryParse(DesiredPixelWidth.Value, out var width))
                {
                    return;
                }

                var newHeight = (uint)Math.Clamp(Math.Round(width / _aspectRatio, MidpointRounding.ToEven), uint.MinValue, uint.MaxValue);
                DesiredPixelHeight.Value = newHeight.ToString();
            }
            else
            {
                if (!uint.TryParse(DesiredPixelHeight.Value, out var height))
                {
                    return;
                }

                var newWidth = (uint)Math.Clamp(Math.Round(height * _aspectRatio, MidpointRounding.ToEven), uint.MinValue, uint.MaxValue);
                DesiredPixelWidth.Value = newWidth.ToString();
            }
        }
    }

    public void UpdateQualitySliderState(FileInfo fileInfo)
    {
        try
        {
            if (IsConversionToQualityFormat())
            {
                IsQualityEnabled.Value = true;
                Quality.Value = 75;
            }
            else if (IsOriginalFileQualityFormat(fileInfo.Extension))
            {
                IsQualityEnabled.Value = true;
                Quality.Value = ImageAnalyzer.GetCompressionQuality(fileInfo.FullName);
            }
            else
            {
                IsQualityEnabled.Value = false;
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ResizeImageViewModel), nameof(UpdateQualitySliderState), e);
        }
    }

    private bool IsConversionToQualityFormat() => SelectedConversionIndex.Value is 1 or 2;

    private static bool IsOriginalFileQualityFormat(string ext)
        => ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
           || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
           || ext.Equals(".png", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Saves the resized image over the tab's file, or in its place when another format is chosen; asks where to save
    /// an image that is not a file. A name the conversion needs that is taken is shown, and the dialog stays open.
    /// </summary>
    public async Task SaveImage()
    {
        // The tab the dialog resizes, kept for the whole save even if another tab becomes active meanwhile
        var tab = _mainVm.WindowTabs.ActiveTab.CurrentValue;
        if (!TryGetSize(out var w, out var h))
        {
            return;
        }

        var fileInfo = tab.FileInfo.CurrentValue;
        if (fileInfo is null)
        {
            // A Single Image has no file to write over
            await SaveImageAsCore(tab, w, h).ConfigureAwait(false);
            return;
        }

        var original = fileInfo.FullName;
        var path = original;
        var ext = GetSelectedFileExtension(fileInfo.Extension, ref path);
        WriteDestination destination = string.Equals(ext, fileInfo.Extension, StringComparison.OrdinalIgnoreCase)
            ? new WriteDestination.OverItself()
            : new WriteDestination.Replace(Path.ChangeExtension(original, ext));

        var result = await WriteAsync(tab, w, h, ext, path, destination).ConfigureAwait(false);
        if (result.Outcome is WriteOutcome.NameTaken && destination is WriteDestination.Replace replace)
        {
            ShowMessageAction?.Invoke(TranslationManager.Translation.FileAlreadyExists + Environment.NewLine +
                                      Path.GetFileName(replace.NewPath));
            return;
        }

        CloseAction?.Invoke();
    }

    /// <summary>
    /// Asks where to save the resized image, then saves a copy there; the Shown File itself when it is picked.
    /// </summary>
    public async Task SaveImageAs()
    {
        var tab = _mainVm.WindowTabs.ActiveTab.CurrentValue;
        if (TryGetSize(out var w, out var h))
        {
            await SaveImageAsCore(tab, w, h).ConfigureAwait(false);
        }
    }

    private async Task SaveImageAsCore(TabViewModel tab, uint w, uint h)
    {
        if (PickFileAction is null)
        {
            return;
        }

        var fileInfo = tab.FileInfo.CurrentValue;
        var suggested = fileInfo?.FullName ?? "PicView" + PngExtension;
        var ext = GetSelectedFileExtension(fileInfo?.Extension ?? PngExtension, ref suggested);

        var picked = await PickFileAction(suggested, ext).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(picked))
        {
            return;
        }

        var destination = Path.ChangeExtension(picked, ext);
        // A copy onto the Shown File is written over itself by the save module
        await WriteAsync(tab, w, h, ext, destination, new WriteDestination.Copy(destination)).ConfigureAwait(false);
        CloseAction?.Invoke();
    }

    // The size asked for; a side that is not a number is 0, which keeps the aspect from the other. False when neither
    // side has a size, as a resize to nothing would only encode the image again
    private bool TryGetSize(out uint w, out uint h)
    {
        w = uint.TryParse(DesiredPixelWidth.CurrentValue, CultureInfo.InvariantCulture, out var width) ? width : 0;
        h = uint.TryParse(DesiredPixelHeight.CurrentValue, CultureInfo.InvariantCulture, out var height) ? height : 0;
        return w > 0 || h > 0;
    }

    // Writes the tab's Save Source, turned and flipped as it shows, resized to w x h
    private async Task<WriteResult> WriteAsync(TabViewModel tab, uint w, uint h, string ext, string path,
        WriteDestination destination)
    {
        IsLoading.Value = true;
        try
        {
            var edits = new WriteEdits(Width: w, Height: h, Quality: GetQualityValue(ext, path));
            return await _mainVm.Core.Writer
                .WriteAsync(tab, new ShownFileWrite(WriteSource.SaveSource, edits, destination))
                .ConfigureAwait(false);
        }
        finally
        {
            IsLoading.Value = false;
        }
    }

    private const string PngExtension = ".png";

    private string GetSelectedFileExtension(FileInfo fileInfo, ref string destination) =>
        GetSelectedFileExtension(fileInfo.Extension, ref destination);

    private string GetSelectedFileExtension(string originalExtension, ref string destination)
    {
        var ext = originalExtension;
        if (SelectedConversionIndex.Value == 0)
        {
            return ext;
        }

        ext = GetExtensionFromSelectedItem() ?? ext;
        destination = Path.ChangeExtension(destination, ext);
        return ext;
    }

    private string? GetExtensionFromSelectedItem()
    {
        return SelectedConversionIndex.Value switch
        {
            1 => ".png",
            2 => ".jpg",
            3 => ".webp",
            4 => ".avif",
            5 => ".heic",
            6 => ".jxl",
            _ => null
        };
    }

    private uint? GetQualityValue(string ext, string destination)
    {
        if (IsQualityEnabled.Value && (
                ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(destination).Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(destination).Equals(".jpeg", StringComparison.OrdinalIgnoreCase)))
        {
            return (uint)Quality.Value;
        }

        return null;
    }

    public void ResetSettings()
    {
        var tab = _mainVm.WindowTabs.ActiveTab.CurrentValue;
        var fileInfo = tab.FileInfo.CurrentValue;

        _isInternalChange = true;
        DesiredPixelWidth.Value = OriginalPixelWidth.Value.ToString();
        DesiredPixelHeight.Value = OriginalPixelHeight.Value.ToString();
        _isInternalChange = false;

        if (fileInfo != null)
        {
            if (IsOriginalFileQualityFormat(fileInfo.Extension))
            {
                IsQualityEnabled.Value = true;
                Quality.Value = ImageAnalyzer.GetCompressionQuality(fileInfo.FullName);
            }
            else
            {
                IsQualityEnabled.Value = false;
            }
        }

        SelectedConversionIndex.Value = 0;
        IsKeepingAspectRatio.Value = true;
        ShowReset.Value = false;

        _ = UpdateOutputFileSizeAsync();
    }

    public async Task UpdateOutputFileSizeAsync()
    {
        var tab = _mainVm.WindowTabs.ActiveTab.CurrentValue;

        var fileInfo = tab.FileInfo.CurrentValue ?? tab.Model?.FileInfo;
        if (fileInfo is null || !File.Exists(fileInfo.FullName) ||
            !uint.TryParse(DesiredPixelWidth.Value, CultureInfo.InvariantCulture, out var w) || w == 0 ||
            !uint.TryParse(DesiredPixelHeight.Value, CultureInfo.InvariantCulture, out var h) || h == 0)
        {
            OutputFileSize.Value = string.Empty;
            return;
        }

        var orientation = tab.Orientation.Current.CurrentValue;
        var isFlipped = orientation.IsFlipped;
        var rotationAngle = orientation.Degrees;

        var dummyDest = fileInfo.FullName;
        var ext = GetSelectedFileExtension(fileInfo, ref dummyDest);

        var tempFilePath = TempFileManager.GetNewTempFilePath($"temp_resize_{Guid.NewGuid():N}{ext}");

        try
        {
            var quality = GetQualityValue(ext, tempFilePath);

            using var magickImage = new MagickImage(fileInfo);
            if (quality is not null)
            {
                magickImage.Quality = quality.Value;
            }

            // Turn, then mirror, as the view does
            if (rotationAngle != 0)
            {
                magickImage.Rotate(rotationAngle);
            }

            if (isFlipped)
            {
                magickImage.Flop();
            }

            magickImage.Resize(w, h);
            await magickImage.WriteAsync(tempFilePath).ConfigureAwait(false);

            var tempFileInfo = new FileInfo(tempFilePath);
            if (tempFileInfo.Exists)
            {
                OutputFileSize.Value = tempFileInfo.Length.GetReadableFileSize();
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ResizeImageViewModel), nameof(UpdateOutputFileSizeAsync), e);
        }
        finally
        {
            CleanupTempFile(_lastTempFilePath);
            _lastTempFilePath = tempFilePath;
        }
    }

    private static void CleanupTempFile(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
        catch
        {
            // Ignore cleanup exceptions
        }
    }

    public void ToggleAspectRatio()
    {
        IsKeepingAspectRatio.Value = !IsKeepingAspectRatio.Value;

        if (IsKeepingAspectRatio.Value)
        {
            _isInternalChange = true;
            AdjustAspectRatio(true);
            _isInternalChange = false;
        }
        else
        {
            ShowReset.Value = true;
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
        DesiredPixelWidth.Dispose();
        DesiredPixelHeight.Dispose();
        Quality.Dispose();
        IsQualityEnabled.Dispose();
        SelectedConversionIndex.Dispose();
        IsKeepingAspectRatio.Dispose();
        IsLoading.Dispose();
        ShowReset.Dispose();
        OutputFileSize.Dispose();
        OriginalFileSize.Dispose();
        OriginalPixelWidth.Dispose();
        OriginalPixelHeight.Dispose();

        CleanupTempFile(_lastTempFilePath);
        _lastTempFilePath = null;

        GC.SuppressFinalize(this);
    }
}
