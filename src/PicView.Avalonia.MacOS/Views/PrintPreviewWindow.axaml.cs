using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.MacOS.Printing;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Printing;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.Main;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.Localization;
using PicView.Core.Printing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.MacOS.Views;

public partial class PrintPreviewWindow : Window
{
    private const float PreviewDpi = 96f;
    private object? _outputImage;

    public PrintPreviewWindow()
    {
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this,
            TranslationManager.Translation.Print + " - PicView");

        Loaded += (_, _) =>
        {
            // Keep window position when resizing
            ClientSizeProperty.Changed.ToObservable()
                .Subscribe(size =>
                {
                    WindowResizing.KeepWindowSize(this, size);
                });
        };
    }

    public void Initialize(string filePath)
    {
        var vm = Dispatcher.UIThread.Invoke(() => DataContext as MainViewModel);
        
        if (vm.PrintPreview == null)
        {
            return;
        }
        
        WindowFunctions.CenterWindowOnScreen(this);

        vm.PrintPreview.PrintSettings.Value.ImagePath.Value = filePath;
        var ps = vm.PrintPreview.PrintSettings.Value;

        // Printer change
        ps.PrinterName
            .AsObservable()
            .DistinctUntilChanged()
            .SubscribeAwait(async (_, _) =>{ await UpdatePreviewAsync(vm.PrintPreview); })
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
            .SubscribeAwait(async (_, _) =>{ await UpdatePreviewAsync(vm.PrintPreview); })
            .AddTo(vm.PrintPreview.Disposables);
    }

    // -----------------------------------------------------------
    //   Preview rendering 
    // -----------------------------------------------------------

    private async ValueTask UpdatePreviewAsync(PrintPreviewViewModel vm)
    {
        var settings = vm.PrintSettings.Value;
        var mainVm= Dispatcher.UIThread.Invoke(() => DataContext as MainViewModel);
        await using var fs = File.OpenRead(settings.ImagePath.Value);
        var avaloniaBmp = new Bitmap(fs);
        
        // Grayscale if needed
        if (settings.ColorMode.Value == (int)ColorModes.BlackAndWhite)
        {
            vm.GrayCache ??= PrintCore.ToGrayScale(avaloniaBmp, PreviewDpi);
            avaloniaBmp = (Bitmap)vm.GrayCache;
        }
        else
        {
            vm.GrayCache = null;
        }
        
        PrintLayout? printLayout;
        var paper = MacPrintEngine.ResolvePaper(settings.PaperSize.Value, settings.Orientation.Value);
        try
        {
            printLayout = PrintCore.ComputeLayout(
                paper.WidthMm,
                paper.HeightMm,
                settings,
                avaloniaBmp?.PixelSize.Width ?? 0,
                avaloniaBmp?.PixelSize.Height ?? 0,
                PreviewDpi);
        }
        catch (Exception e)
        {
            // Fix null exception by loading from cache
            DebugHelper.LogDebug(nameof(PrintPreviewView), nameof(UpdatePreviewAsync), e);
            var cached = await NavigationManager.GetPreLoadValueAsync(mainVm.PicViewer.FileInfo.Value);
            printLayout = PrintCore.ComputeLayout(
                paper.WidthMm,
                paper.HeightMm,
                settings,
                cached.ImageModel.PixelWidth,
                cached.ImageModel.PixelHeight,
                PreviewDpi);
            avaloniaBmp = cached.ImageModel.Image as Bitmap;
        }
        var layout = printLayout.Value;
        
        var rtb = new RenderTargetBitmap(
            new PixelSize((int)layout.PageWidthPx, (int)layout.PageHeightPx));
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var ctx = rtb.CreateDrawingContext();
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
        });
        
        if (vm.PreviewImage.Value is Bitmap old)
        {
            old.Dispose();
        }

        vm.PreviewImage.Value = rtb;
        vm.PageWidth.Value = layout.PageWidthPx;
        vm.PageHeight.Value = layout.PageHeightPx;
        
        _outputImage = avaloniaBmp;
    }

    // -----------------------------------------------------------
    //   Print command
    // -----------------------------------------------------------

    public async ValueTask RunPrintAsync(MainViewModel vm)
    {
        if (vm.PrintPreview == null)
        {
            return;
        }
        var preview = vm.PrintPreview;
        var settings = preview.PrintSettings.Value;

        preview.IsProcessing.Value = true;

        try
        {
            await MacPrintEngine.RunPrintJob(settings, _outputImage as Bitmap);
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

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is Window hostWindow)
        {
            hostWindow.BeginMoveDrag(e);
        }
    }
}