using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Views.UC;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class HoverBarTests
{
    public HoverBarTests()
    {
        SetDefaults();
        TranslationManager.Init();
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
    }

    [AvaloniaFact]
    public void ApplyResponsiveResize_WhileHidden_LeavesIsVisibleToTheBinding()
    {
        var bar = new HoverBar { IsVisible = false };
        var window = new Window { Width = 900, Height = 600, Content = bar };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        bar.ApplyResponsiveResize(900);

        Assert.False(bar.IsVisible);
        Assert.True(bar.BottomBorder.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void ApplyResponsiveResize_TooNarrow_HidesTheBorderAndNotTheBar()
    {
        var bar = new HoverBar { IsVisible = true };
        var window = new Window { Width = 900, Height = 600, Content = bar };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        bar.ApplyResponsiveResize(100);
        Assert.True(bar.IsVisible);
        Assert.False(bar.BottomBorder.IsVisible);

        bar.ApplyResponsiveResize(900);
        Assert.True(bar.BottomBorder.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void IsHitTestVisible_FollowsTheFadeButtonsSetting()
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()),
            new GlobalSettingsViewModel(static () => ValueTask.FromResult(true)));
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings, core.GallerySettings);
        var bar = new HoverBar { DataContext = vm.WindowTabs.ActiveTab.Value };
        var window = new Window { Width = 900, Height = 600, Content = bar };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.True(bar.IsHitTestVisible);

        core.GlobalSettings.ShowFadeButtons.Value = false;
        Dispatcher.UIThread.RunJobs();
        Assert.False(bar.IsHitTestVisible);

        core.GlobalSettings.ShowFadeButtons.Value = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(bar.IsHitTestVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void ApplyDetectionArea_OverADockedGallery_Leaves45PxAboveTheBar()
    {
        var bar = new HoverBar();

        bar.ApplyDetectionArea(isDockedGalleryShown: true);

        Assert.Equal(45, bar.Height - bar.BottomBorder.Height - bar.BottomBorder.Margin.Bottom);
        Assert.Equal(6, bar.BottomBorder.Margin.Bottom);
    }

    [AvaloniaFact]
    public void ApplyDetectionArea_WithoutADockedGallery_KeepsTheBarWhereItWas()
    {
        var bar = new HoverBar();

        bar.ApplyDetectionArea(isDockedGalleryShown: false);

        Assert.Equal(160, bar.Height);
        Assert.Equal(61.5, bar.BottomBorder.Margin.Bottom);
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
