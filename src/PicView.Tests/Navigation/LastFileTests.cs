using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class LastFileTests
{
    private readonly CoreViewModel _core;

    public LastFileTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        Settings.StartUp.LastFile = null;
        _core = new CoreViewModel(null, file => ValueTask.FromResult(new ImageModel { FileInfo = file }));
    }

    [Fact]
    public void Display_InTheActiveTabOfTheActiveMainWindow_WritesTheLastFile()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;

        tab.Display(Model("a.jpg"));

        Assert.Equal(PathOf("a.jpg"), Settings.StartUp.LastFile);
    }

    [Fact]
    public void Display_InATabThatIsNotActive_LeavesTheLastFile()
    {
        var window = OpenWindow();
        var first = window.WindowTabs.ActiveTab.Value;
        var second = window.WindowTabs.CreateTab();
        window.WindowTabs.SelectTab(first);

        second.Display(Model("b.jpg"));

        Assert.Null(Settings.StartUp.LastFile);
    }

    [Fact]
    public void Display_InAMainWindowThatIsNotActive_LeavesTheLastFile()
    {
        var background = OpenWindow();
        OpenWindow();

        background.WindowTabs.ActiveTab.Value.Display(Model("c.jpg"));

        Assert.Null(Settings.StartUp.LastFile);
    }

    [Fact]
    public void DisplaySingleImage_ClipboardImage_LeavesTheLastFile()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg"));

        tab.DisplaySingleImage(new ImageModel { Image = new object() }, SingleImageType.Clipboard, "Clipboard image");

        Assert.Equal(PathOf("a.jpg"), Settings.StartUp.LastFile);
    }

    [Fact]
    public void DisplaySingleImage_UrlDownload_WritesTheUrl()
    {
        const string url = "https://example.com/a.png";
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.SourceURL = url;

        tab.DisplaySingleImage(new ImageModel { Image = new object() }, SingleImageType.Url, url);

        Assert.Equal(url, Settings.StartUp.LastFile);
    }

    [Fact]
    public void Display_WithFileHistoryOff_StillWritesTheLastFile()
    {
        Settings.Navigation.IsFileHistoryEnabled = false;
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;

        tab.Display(Model("a.jpg"));

        Assert.Equal(PathOf("a.jpg"), Settings.StartUp.LastFile);
    }

    [Fact]
    public void Remember_TabOfAMainWindowThatIsNotActive_WritesWhatItShows()
    {
        var background = OpenWindow();
        var tab = background.WindowTabs.ActiveTab.Value;
        OpenWindow();
        tab.Display(Model("d.jpg"));

        // As closing its Main Window does
        LastFile.Remember(tab);

        Assert.Equal(PathOf("d.jpg"), Settings.StartUp.LastFile);
    }

    [Fact]
    public void Remember_TabShowingAClipboardImage_KeepsTheLastFile()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg"));
        tab.DisplaySingleImage(new ImageModel { Image = new object() }, SingleImageType.Clipboard, "Clipboard image");

        LastFile.Remember(tab);

        Assert.Equal(PathOf("a.jpg"), Settings.StartUp.LastFile);
    }

    private MainWindowViewModel OpenWindow()
    {
        var window = new MainWindowViewModel(_core, null!, null!, null!);
        _core.MainWindows.MainWindows.Add(window);
        _core.MainWindows.ActiveWindow.Value = window;
        return window;
    }

    private static string PathOf(string name) => Path.Combine(Path.GetTempPath(), "PicViewLastFile", name);

    private static ImageModel Model(string name) => new() { FileInfo = new FileInfo(PathOf(name)), Image = new object() };

    private class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
