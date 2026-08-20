using System.Globalization;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
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

    public async Task SaveImage()
    {
        var tab = _mainVm.WindowTabs.ActiveTab.CurrentValue;

        var fileInfo = tab.FileInfo.CurrentValue;
        if (fileInfo is null)
        {
            return;
        }

        var destination = fileInfo.FullName;
        var isFlipped = tab.ScaleX.CurrentValue < 0;
        var rotationAngle = tab.RotationAngle.CurrentValue;

        await SaveImageInternal(fileInfo, destination, isFlipped, rotationAngle).ConfigureAwait(false);
        CloseAction?.Invoke();
    }

    public async Task SaveImageAs()
    {
        var tab = _mainVm.WindowTabs.ActiveTab.CurrentValue;

        var fileInfo = tab.FileInfo.CurrentValue;
        if (fileInfo is null || PickFileAction is null)
        {
            return;
        }

        var fileInfoFullName = fileInfo.FullName;
        var ext = GetSelectedFileExtension(fileInfo, ref fileInfoFullName);

        var destination = await PickFileAction(fileInfo.FullName, ext).ConfigureAwait(false);
        if (destination == null) return;

        var isFlipped = tab.ScaleX.CurrentValue < 0;
        var rotationAngle = tab.RotationAngle.CurrentValue;

        await SaveImageInternal(fileInfo, destination, isFlipped, rotationAngle).ConfigureAwait(false);
        CloseAction?.Invoke();
    }

    private async Task SaveImageInternal(FileInfo fileInfo, string destination, bool isFlipped, int rotationAngle)
    {
        IsLoading.Value = true;

        try
        {
            var ext = GetSelectedFileExtension(fileInfo, ref destination);
            destination = Path.ChangeExtension(destination, ext);
            var quality = GetQualityValue(ext, destination);

            using var magickImage = new MagickImage(fileInfo);
            if (quality != null)
            {
                magickImage.Quality = quality.Value;
            }

            if (isFlipped)
            {
                magickImage.Flop();
            }

            if (rotationAngle != 0)
            {
                magickImage.Rotate(rotationAngle);
            }

            var w = Convert.ToUInt32(DesiredPixelWidth.CurrentValue, CultureInfo.InvariantCulture);
            var h = Convert.ToUInt32(DesiredPixelHeight.CurrentValue, CultureInfo.InvariantCulture);

            magickImage.Resize(w, h);
            await magickImage.WriteAsync(destination).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ResizeImageViewModel), nameof(SaveImageInternal), e);
        }
        finally
        {
            IsLoading.Value = false;
        }
    }

    private string GetSelectedFileExtension(FileInfo fileInfo, ref string destination)
    {
        var ext = fileInfo.Extension;
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

        var isFlipped = tab.ScaleX.CurrentValue < 0;
        var rotationAngle = tab.RotationAngle.CurrentValue;

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

            if (isFlipped)
            {
                magickImage.Flop();
            }

            if (rotationAngle != 0)
            {
                magickImage.Rotate(rotationAngle);
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
