using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

namespace PicView.Avalonia.Win32;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
#if DEBUG
            .LogToTrace()
#endif
            .UseReactiveUI()
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = 256_000_000,
                UseOpacitySaveLayer = true
            })
            .UseWin32()
            .UseSkia();
    }
}