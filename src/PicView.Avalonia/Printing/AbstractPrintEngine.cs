using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Core.DebugTools;
using PicView.Core.Printing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Printing;

public abstract class AbstractPrintEngine : IPrintEngine
{
    protected const float PreviewDpi = 96f;
    protected const float PrintDpi = PrintLayout.PrintDpi;

    public virtual async ValueTask UpdatePreviewAsync(TabViewModel tab, PrintPreviewViewModel previewVm)
    {
        try
        {
            var settings = previewVm.PrintSettings.Value;
            if (settings == null)
            {
                return;
            }

            var avaloniaBmp = GetBitmap(tab);
            if (avaloniaBmp is null)
            {
                return;
            }

            // Grayscale if needed
            if (settings.ColorMode.Value == (int)ColorModes.BlackAndWhite)
            {
                previewVm.GrayCache ??= PrintCore.ToGrayScale(avaloniaBmp, PreviewDpi);
                avaloniaBmp = (Bitmap)previewVm.GrayCache;
            }
            else
            {
                previewVm.GrayCache = null;
            }

            var layout = PrintLayout.Compute(
                settings,
                avaloniaBmp.PixelSize.Width,
                avaloniaBmp.PixelSize.Height,
                PreviewDpi);

            var rtb = new RenderTargetBitmap(
                new PixelSize((int)layout.PageWidthPx, (int)layout.PageHeightPx));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                using var ctx = rtb.CreateDrawingContext();
                ctx.FillRectangle(Brushes.White, new Rect(0, 0, layout.PageWidthPx, layout.PageHeightPx));
                ctx.DrawRectangle(null, new Pen(Brushes.LightGray),
                    new Rect(0.5, 0.5, layout.PageWidthPx - 1, layout.PageHeightPx - 1));

                var dashPen = new Pen(Brushes.Gray, 2)
                {
                    DashStyle = new DashStyle([4, 2], 0)
                };
                ctx.DrawRectangle(null, dashPen, layout.Content.ToRect());

                ctx.DrawImage(avaloniaBmp, layout.Source.ToRect(), layout.Destination.ToRect());
            });

            previewVm.PreviewImage.Value = rtb;
            previewVm.PageWidth.Value = layout.PageWidthPx;
            previewVm.PageHeight.Value = layout.PageHeightPx;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(GetType().Name, nameof(UpdatePreviewAsync), ex);
        }
        finally
        {
            previewVm.IsProcessing.Value = false;
        }
    }

    public async ValueTask RunPrintAsync(TabViewModel tab, PrintPreviewViewModel preview)
    {
        var settings = preview.PrintSettings.Value;
        if (settings == null)
        {
            return;
        }

        preview.IsProcessing.Value = true;

        try
        {
            await RunPrintJob(settings, GetBitmap(tab)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(GetType().Name, nameof(RunPrintAsync), ex);
        }
        finally
        {
            preview.IsProcessing.Value = false;
        }
    }

    protected abstract ValueTask RunPrintJob(PrintSettings settings, Bitmap avaloniaBmp);

    private static Bitmap? GetBitmap(TabViewModel tab)
    {
        return tab.Image.CurrentValue as Bitmap;
    }
}
