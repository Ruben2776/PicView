using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using PicView.Core.Sizing;
using PicView.Tests.WindowBehavior;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class NavigationSubscriptionsTests
{
    public NavigationSubscriptionsTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaFact]
    public async Task Attach_FromABackgroundThread_AttachesTheTab()
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        var window = new TestMainWindow(new NoOpModeAdapter());
        var vm = new MainWindowViewModel(core, core.Translation, window, core.GlobalSettings!, core.GallerySettings);
        window.DataContext = vm;
        var hookups = new NavigationSubscriptions(window, vm);
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;

        // Opening the first source sets up a tab off the UI thread, as QuickLoad does
        using var attached = await Task.Run(() => hookups.Attach(tab), TestContext.Current.CancellationToken);

        Assert.NotNull(attached);
    }

    private sealed class NoOpModeAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state)
        {
        }
    }
}
