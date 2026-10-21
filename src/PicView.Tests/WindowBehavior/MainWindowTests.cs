using PicView.Avalonia.CustomControls;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Tests.WindowBehavior;

[Collection("Sequential")]
public class MainWindowTests
{
    public MainWindowTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaFact]
    public void MoveTab_ToAnotherWindow_RemovesItFromSourceAndSelectsItInTarget()
    {
        var core = CreateCore();
        var source = CreateWindowViewModel(core);
        var target = CreateWindowViewModel(core);
        var remaining = source.WindowTabs.ActiveTab.Value;
        var tab = new TabViewModel(source, null);
        source.WindowTabs.Tabs.Value.Add(tab);
        source.WindowTabs.SelectTab(tab);

        MainWindow.MoveTab(tab, source, target);

        Assert.DoesNotContain(tab, source.WindowTabs.Tabs.Value);
        Assert.Same(remaining, source.WindowTabs.ActiveTab.Value);
        Assert.Contains(tab, target.WindowTabs.Tabs.Value);
        Assert.Same(tab, target.WindowTabs.ActiveTab.Value);
        Assert.Same(target, tab.ParentWindowContext);
    }

    [AvaloniaFact]
    public void MoveTab_WhenDragPreviewAlreadyAddedIt_DoesNotDuplicateIt()
    {
        var core = CreateCore();
        var source = CreateWindowViewModel(core);
        var target = CreateWindowViewModel(core);
        var tab = new TabViewModel(source, null);
        source.WindowTabs.Tabs.Value.Add(tab);
        target.WindowTabs.Tabs.Value.Add(tab);

        MainWindow.MoveTab(tab, source, target);

        Assert.Single(target.WindowTabs.Tabs.Value, t => ReferenceEquals(t, tab));
        Assert.Same(tab, target.WindowTabs.ActiveTab.Value);
    }

    private static CoreViewModel CreateCore() =>
        new(null, _ => ValueTask.FromResult(new ImageModel()));

    private static MainWindowViewModel CreateWindowViewModel(CoreViewModel core) =>
        new(core, core.Translation, null!, core.GlobalSettings!, core.GallerySettings);
}
