using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.ReactiveUI;
using Avalonia.Themes.Simple;
using PicView.Avalonia.MacOS;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.ViewModels;

namespace PicView.Tests;

public class AvaloniaTest
{
    [assembly: AvaloniaTestApplication(typeof(AvaloniaTest))]
    [AvaloniaFact]
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseReactiveUI()
        .LogToTrace()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());

    [AvaloniaFact]
    public void TestWindow()
    {
        // Create a window and set the view model as its data context:

        var window = new MacMainWindow
        {
            Styles = { new SimpleTheme(), new StyleInclude(new Uri("avares://PicView.Avalonia/DarkTheme/Main.axaml")) },
            DataContext = new MainViewModel()
        };

        window.Show();
    }
}