using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.ViewModels;

[Collection("Sequential")]
public class TabOverviewViewModelTests
{
    private readonly CoreViewModel _core;

    public TabOverviewViewModelTests()
    {
        SetDefaults();
        TranslationManager.Init();
        _core = new CoreViewModel(null, file => new ImageModel { FileInfo = file });
    }

    [Fact]
    public void InsertTab_DoesNotChangeActiveTab_AndKeepsIndex()
    {
        var vm = new MainWindowViewModel(_core, _core.Translation, _core.GlobalSettings!, _core.GallerySettings);
        var tabs = vm.WindowTabs;
        tabs.SetViewFactory(new NoViews());
        var active = tabs.ActiveTab.Value;

        var before = tabs.InsertTab(0);
        var after = tabs.InsertTab(2);
        var clamped = tabs.InsertTab(99);

        Assert.Equal([before, active, after, clamped], tabs.Tabs.Value);
        Assert.Same(active, tabs.ActiveTab.Value);
        Assert.True(active.IsSelected);
        Assert.All([before, after, clamped], tab => Assert.False(tab.IsSelected));
        Assert.All([before, after, clamped], tab => Assert.NotNull(tab.Views));
    }

    private sealed class NoViews : ITabViewFactory
    {
        public ITabViews Attach(TabViewModel tab) => new Views();

        private sealed class Views : ITabViews
        {
            public IReadOnlyBindableReactiveProperty<object?> View { get; } = new BindableReactiveProperty<object?>();
            public void Dispose() { }
        }
    }
}
