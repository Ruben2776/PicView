using Avalonia;
using Avalonia.Headless;
using PicView.Avalonia;

[assembly: AvaloniaTestApplication(typeof(PicView.Tests.TestAppBuilder))]

namespace PicView.Tests;

public class TestAppBuilder
{
    // Skia instead of headless drawing, so bitmaps can be encoded (saving a clipboard image)
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
