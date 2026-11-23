using System.Globalization;
using PicView.Core.Config;
using PicView.Core.Conversion;
using PicView.Core.Exif;
using PicView.Core.Models;
using PicView.Core.Saving;
using PicView.Core.Sizing;
using PicView.Core.Titles;
using R3;

namespace PicView.Core.ViewModels;

public class ImageInfoWindowViewModel(MainWindowViewModel vm) : IDisposable
{
    public ImageInfoWindowConfig? ImageInfoWindowConfig { get; set; }
    
    public int TextWidth => 100;
    public int CopyButtonWidth => 37;
    
    private const int TextMaxWidth = 135;

    // The largest side a resize asks for; more is a typing slip, and most formats take no more
    private const double MaxSide = 65535;
    
    public BindableReactiveProperty<double> TextBoxMaxWidth { get; } = new(TextMaxWidth);
    public BindableReactiveProperty<double> ExtraTextBoxMaxWidth { get; } = new(TextMaxWidth);

    public BindableReactiveProperty<double> TextBoxWidth { get; } = new(180);
    public BindableReactiveProperty<double> TextBoxXlWidth { get; } = new(620);
    public BindableReactiveProperty<double> TextBoxXxlWidth { get; } = new(630);
    public BindableReactiveProperty<double> HalfLineWidth { get; } = new(395);

    public BindableReactiveProperty<bool> IsCopyButtonEnabled { get; } = new();
    public BindableReactiveProperty<bool> IsExtraButtonsEnabled { get; } = new();
    
    public BindableReactiveProperty<bool> IsLoading { get; } = new(true);

    public void ResponsiveResizeUpdate(double width, double scrollBarThickness)
    {
        const int firstBreakPoint = 600;
        const int secondBreakPoint = 915;
        const int thirdBreakPoint = 1150;
        const int fourthBreakPoint = 1450;

        var textWidth = TextWidth;
        var copyBtnWidth = CopyButtonWidth;

        IsCopyButtonEnabled.Value = width >= firstBreakPoint;
        IsExtraButtonsEnabled.Value = width >= secondBreakPoint;

        const int padding = 10;

        TextBoxMaxWidth.Value = width < thirdBreakPoint ? 0 : TextMaxWidth;
        ExtraTextBoxMaxWidth.Value = width < fourthBreakPoint ? 0 : TextMaxWidth;

        switch (width)
        {
            case <= firstBreakPoint:
                TextBoxWidth.Value = TextBoxXlWidth.Value = width - (textWidth + scrollBarThickness + padding);
                TextBoxXxlWidth.Value = width - (textWidth + scrollBarThickness);
                break;
            case >= firstBreakPoint and <= secondBreakPoint:
                TextBoxWidth.Value = TextBoxXlWidth.Value = TextBoxXxlWidth.Value =
                    width - (textWidth + scrollBarThickness + padding + copyBtnWidth);
                break;
            case >= secondBreakPoint and <= thirdBreakPoint:
                var thirdBreakWidth = width - width / 2 - (textWidth * 2 + scrollBarThickness + padding) +
                                      copyBtnWidth * 2;
                TextBoxWidth.Value = thirdBreakWidth;
                var newWidthBreakL =  thirdBreakWidth * 2 + textWidth * 2 - padding * 2;
                TextBoxXlWidth.Value = newWidthBreakL;
                TextBoxXxlWidth.Value = newWidthBreakL - padding;
                break;
            case >= thirdBreakPoint:
                var aboveThirdWidth = width / 2 - (textWidth * 2 + scrollBarThickness) +
                                      copyBtnWidth;
                TextBoxWidth.Value = aboveThirdWidth;
                var aboveThirdWidthL = aboveThirdWidth * 2 + textWidth * 2 - padding * 2;
                TextBoxXlWidth.Value = aboveThirdWidthL;
                TextBoxXxlWidth.Value = aboveThirdWidthL - padding;
                break;
        }

        if (width >= thirdBreakPoint)
        {
            HalfLineWidth.Value = width / 2 - (scrollBarThickness + padding + 15);
        }
        else
        {
            HalfLineWidth.Value = width / 2 - (scrollBarThickness + padding);
        }
    }

    public async ValueTask UpdateValuesAsync(ImageModel imageModel, CancellationToken cancellationToken)
    {
        if (vm.Exif == null) return;
        
        await Task.Run(() =>
        {
            vm.Exif.UpdateExifValues(imageModel);
        }, cancellationToken).ConfigureAwait(false);

        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.ShouldOptimizeImageBeEnabled.Value = ConversionHelper.DetermineIfOptimizeImageShouldBeEnabled(tab.Model.FileInfo);
        vm.Exif.IsExifAvailable.Value = vm.Exif.ImageFormat.CurrentValue.IsExifImage();
    }

    public async Task ResizeImageAsync(bool isWidth, string widthText, string heightText)
    {
        // The tab the window resizes, kept for the whole write even if another tab becomes active meanwhile
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        IsLoading.Value = true;
        try
        {
            var text = isWidth ? widthText : heightText;
            // double.TryParse reads "NaN" and "Infinity" too, which are no size
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var size) ||
                !double.IsFinite(size) || size < 1 || size > MaxSide)
            {
                return;
            }

            // The other side is 0, which keeps the aspect. A Single Image has no file to write over: nothing is written
            var side = (uint)size;
            var edits = new WriteEdits(Width: isWidth ? side : 0, Height: isWidth ? 0 : side);
            await vm.Core.Writer.WriteAsync(tab,
                new ShownFileWrite(WriteSource.SaveSource, edits, new WriteDestination.OverItself()))
                .ConfigureAwait(false);
        }
        finally
        {
            IsLoading.Value = false;
        }
    }

    public void UpdatePrintSizes(uint width, uint height)
    {
        if (vm.Exif == null) return;

        var printSizes = PrintSizing.GetPrintSizes(width, height, vm.Exif.DpiX.CurrentValue, vm.Exif.DpiY.CurrentValue);
        vm.Exif.PrintSizeInch.Value = printSizes.PrintSizeInch;
        vm.Exif.PrintSizeCm.Value = printSizes.PrintSizeCm;
        vm.Exif.SizeMp.Value = printSizes.SizeMp;

        var gcd = AspectRatioFormatter.GCD(width, height);
        vm.Exif.AspectRatio.Value = AspectRatioFormatter.GetFormattedAspectRatio(gcd, vm.WindowTabs.ActiveTab.Value.Model.PixelWidth,
                vm.WindowTabs.ActiveTab.Value.Model.PixelHeight);
    }

    public void Dispose()
    {
        Disposable.Dispose(
            TextBoxMaxWidth,
            TextBoxMaxWidth,
            TextBoxWidth,
            TextBoxXlWidth,
            TextBoxXxlWidth,
            HalfLineWidth,
            IsCopyButtonEnabled,
            IsExtraButtonsEnabled,
            IsLoading);
        
        GC.SuppressFinalize(this);
    }
}
