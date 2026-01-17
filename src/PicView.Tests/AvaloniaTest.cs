using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using PicView.Avalonia.MacOS;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.ViewModels;

namespace PicView.Tests;

public class AvaloniaTest
{
    [assembly: AvaloniaTestApplication(typeof(AvaloniaTest))]
    [AvaloniaFact]
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .LogToTrace()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());

    [AvaloniaFact]
    public void TestWindow()
    {
        // Create a window and set the view model as its data context:

        var window = new MacMainWindow
        {
            Styles = { new StyleInclude(new Uri("avares://PicView.Avalonia/PicViewTheme/AllControls.axaml")) },
            DataContext = new MainViewModel()
        };

        window.Show();
    }

    [AvaloniaFact]
    public async Task TestPreloader()
    {
        // await LoadSettingsAsync();
        // var vm = new MainViewModel();
        //await vm.StartUpTask();
    }
}