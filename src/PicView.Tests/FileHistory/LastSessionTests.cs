using PicView.Core.FileHistory;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using R3;

namespace PicView.Tests.FileHistory;

[Collection("Sequential")]
public class LastSessionTests : IDisposable
{
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PicViewLastSession_" + Guid.NewGuid().ToString("N"));
    private readonly List<TabViewModel> _created = [];

    public LastSessionTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        Settings.Navigation.IsFileHistoryEnabled = true;
        Directory.CreateDirectory(_folder);
        FileHistoryManager.Initialize(Path.Combine(_folder, "history.json"));
    }

    public void Dispose()
    {
        foreach (var tab in _created)
        {
            tab.Archives.Cleanup();
        }

        Directory.Delete(_folder, true);
        _fileHistoryBackup.Dispose();
    }

    [Fact]
    public void Of_SkipsTabsWithoutPath_AndRemapsActive()
    {
        var a = Tab("a.jpg");
        var empty = Tab(null);
        var c = Tab("c.jpg");

        var session = LastSession.Of([a, empty, c], c);

        Assert.Equal([PathOf("a.jpg"), PathOf("c.jpg")], session.Sources);
        Assert.Equal(1, session.ActiveIndex);
    }

    [Fact]
    public void Of_ActiveTabWithoutPath_NextKeptTabBecomesActive()
    {
        var a = Tab("a.jpg");
        var empty = Tab(null);
        var c = Tab("c.jpg");

        var session = LastSession.Of([a, empty, c], empty);

        Assert.Equal(1, session.ActiveIndex);
    }

    [Fact]
    public void Of_LastActiveTabWithoutPath_PreviousKeptTabBecomesActive()
    {
        var a = Tab("a.jpg");
        var b = Tab("b.jpg");
        var empty = Tab(null);

        var session = LastSession.Of([a, b, empty], empty);

        Assert.Equal(1, session.ActiveIndex);
    }

    [Fact]
    public void Of_NoTabWithPath_IsEmpty()
    {
        var empty = Tab(null);

        var session = LastSession.Of([empty], empty);

        Assert.Empty(session.Sources);
        Assert.Equal(0, session.ActiveIndex);
    }

    private TabViewModel Tab(string? name)
    {
        var tab = new TabViewModel(null!,
            TestTabServices.For(new SharedImageCache(file => new ImageModel { FileInfo = file }), null));
        _created.Add(tab);
        if (name is not null)
        {
            tab.Display(new ImageModel { FileInfo = new FileInfo(PathOf(name)), Image = new object() });
        }

        return tab;
    }

    private string PathOf(string name) => Path.Combine(_folder, name);

    private class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
