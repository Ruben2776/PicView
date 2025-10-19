using System.Collections.ObjectModel;
using System.Drawing.Printing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.Printing;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Win32.Printing;
using PicView.Core.DebugTools;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Win32.Views;

public partial class PrintPreviewWindow : Window
{
    private const float PreviewDpi = 96f;

    public PrintPreviewWindow()
    {
        InitializeComponent();

        GenericWindowHelper.GenericWindowInitialize(this, TranslationManager.Translation.Print + " - PicView");
        ThemeUpdates();
    }

    private void ThemeUpdates()
    {
        if (!Settings.Theme.Dark)
        {
            PrintPreviewView.Background = UIHelper.GetMenuBackgroundColor();
        }

        // Glass/Transparent theme support
        if (!Settings.Theme.GlassTheme)
        {
            return;
        }

        PrintPreviewView.Background = Brushes.Transparent;
        IconBorder.Background = Brushes.Transparent;
        IconBorder.BorderThickness = new Thickness(0);
        MinimizeButton.Background = Brushes.Transparent;
        MinimizeButton.BorderThickness = new Thickness(0);
        CloseButton.Background = Brushes.Transparent;
        CloseButton.BorderThickness = new Thickness(0);
        BorderRectangle.Height = 0;
        TitleText.Background = Brushes.Transparent;
        var brush = UIHelper.GetBrush("SecondaryTextColor");
        TitleText.Foreground = brush;
        MinimizeButton.Foreground = brush;
        CloseButton.Foreground = brush;
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is Window hostWindow)
        {
            hostWindow.BeginMoveDrag(e);
        }
    }

    private void Close(object? sender, RoutedEventArgs e) => Close();
    private void Minimize(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    public void Initialize()
    {
        var vm = Dispatcher.UIThread.Invoke(() => DataContext as MainViewModel);
        
        if (vm.PrintPreview == null)
        {
            return;
        }

        var ps = vm.PrintPreview.PrintSettings.Value;

        // Printer change
        ps.PrinterName
            .AsObservable()
            .DistinctUntilChanged()
            .Subscribe(_ => UpdatePrinter(vm.PrintPreview))
            .AddTo(vm.PrintPreview.Disposables);

        // Any setting change triggers preview update
        // ReSharper disable once InvokeAsExtensionMethod
        Observable.CombineLatest(
                ps.Orientation.AsObservable(),
                ps.MarginTop.AsObservable(),
                ps.MarginBottom.AsObservable(),
                ps.MarginLeft.AsObservable(),
                ps.MarginRight.AsObservable(),
                ps.ScaleMode.AsObservable(),
                ps.ColorMode.AsObservable(),
                ps.PaperSize.AsObservable(),
                (orientation, top, bottom, left, right, scale, color, paper)
                    => (orientation, top, bottom, left, right, scale, color, paper))
            .ThrottleLast(TimeSpan.FromMilliseconds(100))
            .Subscribe(_ => UpdatePreview(vm.PrintPreview))
            .AddTo(vm.PrintPreview.Disposables);

        // Initial render
        UpdatePrinter(vm.PrintPreview);
        UpdatePreview(vm.PrintPreview);
    }


    // -----------------------------------------------------------
    //   Printer setup
    // -----------------------------------------------------------

    public void UpdatePrinter(PrintPreviewViewModel vm)
    {
        var settings = vm.PrintSettings.Value;
        if (settings == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.PrinterName.Value))
        {
            return;
        }

        try
        {
            var ps = new PrinterSettings { PrinterName = settings.PrinterName.Value };
            var sizes = ps.PaperSizes.Cast<PaperSize>().Select(p => p.PaperName).ToList();

            var currentPaper = settings.PaperSize.Value ?? string.Empty;
            if (!sizes.Contains(currentPaper))
            {
                currentPaper =
                    sizes.FirstOrDefault(p =>
                        p.StartsWith(currentPaper, StringComparison.OrdinalIgnoreCase) ||
                        currentPaper.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    ?? sizes.FirstOrDefault()
                    ?? string.Empty;
            }

            vm.PaperSizes.Value = new ObservableCollection<string>(sizes);
            settings.PaperSize.Value = currentPaper;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(PrintPreviewView), nameof(UpdatePrinter),
                "[PrintPreview] Failed to reload paper sizes");
            DebugHelper.LogDebug(nameof(PrintPreviewView), nameof(UpdatePrinter), ex);
        }
    }


    // -----------------------------------------------------------
    //   Preview rendering (uses same layout as print)
    // -----------------------------------------------------------

    private void UpdatePreview(PrintPreviewViewModel vm)
    {
        try
        {
            var settings = vm.PrintSettings.Value;
            if (settings == null)
            {
                return;
            }

            if (DataContext is not MainViewModel mainVm)
            {
                return;
            }

            if (mainVm.PicViewer?.ImageSource.Value is not Bitmap avaloniaBmp)
            {
                return;
            }

            // Grayscale if needed
            if (settings.ColorMode.Value == (int)ColorModes.BlackAndWhite)
            {
                vm.GrayCache ??= PrintEngine.ToGrayScale(avaloniaBmp, PreviewDpi);
                avaloniaBmp = (Bitmap)vm.GrayCache;
            }
            else
            {
                vm.GrayCache = null;
            }

            // Resolve paper
            var paperInfo = PrintEngine.ResolvePaper(settings.PrinterName.Value, settings.PaperSize.Value,
                settings.Orientation.Value == (int)Orientations.Landscape);
            if (paperInfo is null)
            {
                return;
            }

            // Compute layout once
            var layout = PrintEngine.ComputeLayout(
                paperInfo.WidthMm, paperInfo.HeightMm, settings,
                avaloniaBmp.PixelSize.Width,
                avaloniaBmp.PixelSize.Height,
                PreviewDpi);

            var rtb = new RenderTargetBitmap(
                new PixelSize((int)layout.PageWidthPx, (int)layout.PageHeightPx));

            using (var ctx = rtb.CreateDrawingContext())
            {
                ctx.FillRectangle(Brushes.White, new Rect(0, 0, layout.PageWidthPx, layout.PageHeightPx));
                ctx.DrawRectangle(null, new Pen(Brushes.LightGray),
                    new Rect(0.5, 0.5, layout.PageWidthPx - 1, layout.PageHeightPx - 1));

                var destRect = new Rect(layout.DrawX, layout.DrawY, layout.DrawWidth, layout.DrawHeight);

                var dashPen = new Pen(Brushes.Gray, 2)
                {
                    DashStyle = new DashStyle([4, 2], 0)
                };
                ctx.DrawRectangle(null, dashPen,
                    new Rect(layout.ContentX, layout.ContentY, layout.ContentWidth, layout.ContentHeight));

                ctx.DrawImage(avaloniaBmp,
                    new Rect(0, 0, avaloniaBmp.PixelSize.Width, avaloniaBmp.PixelSize.Height),
                    destRect);
            }

            if (vm.PreviewImage.Value is Bitmap old)
            {
                old.Dispose();
            }

            vm.PreviewImage.Value = rtb;
            vm.PageWidth.Value = layout.PageWidthPx;
            vm.PageHeight.Value = layout.PageHeightPx;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(PrintPreviewView), nameof(UpdatePreview), ex);
        }
    }


    // -----------------------------------------------------------
    //   Print command
    // -----------------------------------------------------------

    public async Task RunPrintAsync(MainViewModel vm)
    {
        if (vm.PrintPreview == null)
        {
            return;
        }

        var preview = vm.PrintPreview;
        var settings = preview.PrintSettings.Value!;
        if (DataContext is not MainViewModel mainVm)
        {
            return;
        }

        if (mainVm.PicViewer?.ImageSource.Value is not Bitmap avaloniaBmp)
        {
            return;
        }

        preview.IsProcessing.Value = true;

        try
        {
            await Task.Run(() => PrintEngine.RunPrintJob(settings, avaloniaBmp));
            await Dispatcher.UIThread.InvokeAsync(Close);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(PrintPreviewView), nameof(RunPrintAsync), ex);
        }
        finally
        {
            preview.IsProcessing.Value = false;
        }
    }
}