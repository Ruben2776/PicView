using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.SettingsManagement;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.SettingsManagement;

[Collection("Sequential")]
public class ToggleHelperTests
{
    public ToggleHelperTests() => SetDefaults();

    // Every Window Setting an Action toggles
    private static readonly Func<WindowSettings, BindableReactiveProperty<bool>>[] Toggled =
    [
        static s => s.IsSideBySide,
        static s => s.IsLooping,
        static s => s.IsZoomedToFit,
        static s => s.IsScrollingEnabled,
        static s => s.ShowInterface,
        static s => s.ShowBottomBar,
        static s => s.ShowHoverBar,
        static s => s.IsConstrainingBackgroundColor,
        static s => s.IsTopMost
    ];

    [Fact]
    public async Task Toggle_AWindowSettingOfAClosedWindow_DoesNothingAndThrowsNothing()
    {
        var settings = WindowSettings.FromRecord(GetDefaults());
        var before = Toggled.Select(pick => pick(settings).Value).ToArray();
        var backgroundBefore = settings.BackgroundChoice.Value;
        var mainWindows = new MainWindowOverviewViewModel();
        var window = new MainWindowViewModel(null!, null!, null!, null!, windowSettings: settings);
        mainWindows.ActiveWindow.Value = window;

        settings.Dispose();

        foreach (var pick in Toggled)
        {
            await ToggleHelper.Toggle(settings, pick);
        }

        BackgroundManager.ChangeBackground(settings);
        BackgroundManager.SetBackground(mainWindows, backgroundBefore + 1);

        Assert.Equal(before, Toggled.Select(pick => pick(settings).Value).ToArray());
        Assert.Equal(backgroundBefore, settings.BackgroundChoice.Value);
    }

    [Fact]
    public async Task Toggle_AWindowSettingOfAnOpenWindow_FlipsIt()
    {
        using var settings = WindowSettings.FromRecord(GetDefaults());
        foreach (var pick in Toggled)
        {
            var before = pick(settings).Value;

            await ToggleHelper.Toggle(settings, pick);

            Assert.Equal(!before, pick(settings).Value);
        }
    }
}
