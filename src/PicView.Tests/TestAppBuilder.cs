using Avalonia;
using Avalonia.Headless;
using PicView.Avalonia;

[assembly: AvaloniaTestApplication(typeof(PicView.Tests.TestAppBuilder))]

namespace PicView.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
