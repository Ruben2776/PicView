using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Config;

/// <summary>
/// The fifteen toggled settings publish their changes (ADR-0020): each writes its stored value before any
/// subscriber hears of it, and a change saves the settings once, however many land during a save.
/// </summary>
[Collection("Sequential")]
public class GlobalSettingsViewModelTests
{
    private int _saves;

    public GlobalSettingsViewModelTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
    }

    private ValueTask<bool> CountingSave()
    {
        Interlocked.Increment(ref _saves);
        return ValueTask.FromResult(true);
    }

    private GlobalSettingsViewModel New() => new(CountingSave);

    public static TheoryData<string, Func<GlobalSettingsViewModel, BindableReactiveProperty<bool>>, Func<bool>> Published => new()
    {
        { "Looping", s => s.IsLooping, () => Settings.UIProperties.Looping },
        { "SideBySide", s => s.IsSideBySide, () => Settings.ImageScaling.ShowImageSideBySide },
        { "Comparison", s => s.IsComparing, () => Settings.ImageScaling.ShowComparison },
        { "Scrolling", s => s.IsScrollingEnabled, () => Settings.Zoom.ScrollEnabled },
        { "ZoomToFit", s => s.IsZoomedToFit, () => Settings.ImageScaling.ZoomToFit },
        { "Subdirectories", s => s.IsIncludingSubdirectories, () => Settings.Sorting.IncludeSubDirectories },
        { "FileHistory", s => s.IsFileHistoryEnabled, () => Settings.Navigation.IsFileHistoryEnabled },
        { "FullPath", s => s.IsShowingFullPathInTitleBar, () => Settings.UIProperties.ShowFullPathInTitleBar },
        { "TaskbarProgress", s => s.IsShowingTaskbarProgress, () => Settings.UIProperties.IsTaskbarProgressEnabled },
        { "OpenInSameWindow", s => s.IsOpeningInSameWindow, () => Settings.UIProperties.OpenInSameWindow },
        { "ConstrainBackground", s => s.IsConstrainingBackgroundColor, () => Settings.UIProperties.IsConstrainBackgroundColorEnabled },
        { "TopMost", s => s.IsTopMost, () => Settings.WindowProperties.TopMost },
        { "Interface", s => s.ShowInterface, () => Settings.UIProperties.ShowInterface },
        { "BottomBar", s => s.ShowBottomBar, () => Settings.UIProperties.ShowBottomNavBar },
        { "HoverBar", s => s.ShowHoverBar, () => Settings.UIProperties.ShowHoverNavigationBar },
        { "FadeButtons", s => s.ShowFadeButtons, () => Settings.UIProperties.ShowAltInterfaceButtons },
    };

    [Theory, MemberData(nameof(Published))]
    public void Setting_Changed_WritesItsStoredValueAndSavesOnce(string _,
        Func<GlobalSettingsViewModel, BindableReactiveProperty<bool>> pick, Func<bool> stored)
    {
        var setting = pick(New());
        var before = stored();
        Assert.Equal(before, setting.Value);

        setting.Value = !before;

        Assert.Equal(!before, stored());
        Assert.Equal(1, _saves);
    }

    [Fact]
    public void WindowMargin_Changed_WritesTheStoredValueAndSavesOnce()
    {
        var settings = New();
        Assert.Equal(Settings.WindowProperties.Margin, settings.WindowMargin.Value);

        settings.WindowMargin.Value = 42;

        Assert.Equal(42, Settings.WindowProperties.Margin);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public void WindowMargin_StoredNegative_PublishesTheSameMarginTheWindowStartsWith()
    {
        Settings.WindowProperties.Margin = -5;

        var settings = New();

        Assert.Equal(45, settings.WindowMargin.Value);
    }

    [Fact]
    public void Constructor_OverLoadedSettings_WritesAndSavesNothing()
    {
        Settings.UIProperties.Looping = true;
        var settings = New();
        Assert.True(settings.IsLooping.Value);
        Assert.True(Settings.UIProperties.Looping);
        Assert.Equal(0, _saves);
    }

    [Fact]
    public void IsComparing_TurnedOn_TurnsSideBySideOff()
    {
        var settings = New();
        settings.IsSideBySide.Value = true;

        settings.IsComparing.Value = true;

        Assert.False(settings.IsSideBySide.Value);
        Assert.False(Settings.ImageScaling.ShowImageSideBySide);
    }

    [Fact]
    public void IsSideBySide_TurnedOn_TurnsComparingOff()
    {
        var settings = New();
        settings.IsComparing.Value = true;

        settings.IsSideBySide.Value = true;

        Assert.False(settings.IsComparing.Value);
        Assert.False(Settings.ImageScaling.ShowComparison);
    }

    [Fact]
    public void IsShowingPair_EitherOn_IsTrue()
    {
        var settings = New();
        Assert.False(settings.IsShowingPair.CurrentValue);

        settings.IsSideBySide.Value = true;
        Assert.True(settings.IsShowingPair.CurrentValue);

        settings.IsComparing.Value = true;
        Assert.True(settings.IsShowingPair.CurrentValue);

        settings.IsComparing.Value = false;
        Assert.False(settings.IsShowingPair.CurrentValue);
    }

    [Fact]
    public void IsShowingPair_SwitchingSideBySideToComparing_NeverEmitsFalse()
    {
        var settings = New();
        settings.IsSideBySide.Value = true;
        var emitted = new List<bool>();
        using var _ = settings.IsShowingPair.Skip(1).Subscribe(emitted.Add);

        settings.IsComparing.Value = true;

        Assert.DoesNotContain(false, emitted);
        Assert.True(settings.IsShowingPair.CurrentValue);
    }

    [Fact]
    public void Constructor_BothStoredOn_ComparisonWins()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        Settings.ImageScaling.ShowComparison = true;

        var settings = New();

        Assert.True(settings.IsComparing.Value);
        Assert.False(settings.IsSideBySide.Value);
        Assert.False(Settings.ImageScaling.ShowImageSideBySide);
    }

    [Fact]
    public void Setting_SetToTheValueItHas_SavesNothing()
    {
        var settings = New();
        settings.IsLooping.Value = Settings.UIProperties.Looping;
        Assert.Equal(0, _saves);
    }

    [Fact]
    public async Task SaveSoon_TwoChangesDuringOneSave_SavesExactlyOnceMore()
    {
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var saves = 0;
        var settings = new GlobalSettingsViewModel(() =>
            Interlocked.Increment(ref saves) is 1 ? new ValueTask<bool>(gate.Task) : ValueTask.FromResult(true));

        settings.IsLooping.Value = true; // save 1 starts and is held
        settings.IsScrollingEnabled.Value = true; // two changes land while it runs
        settings.IsZoomedToFit.Value = true;
        Assert.Equal(1, saves);
        gate.SetResult(true);
        await settings.Saving;

        Assert.Equal(2, saves);
    }

    [Fact]
    public async Task SaveSoon_SaveThrows_LaterChangesStillSave()
    {
        var saves = 0;
        var settings = new GlobalSettingsViewModel(() =>
        {
            if (Interlocked.Increment(ref saves) is 1)
            {
                throw new IOException("disk");
            }
            return ValueTask.FromResult(true);
        });

        settings.IsLooping.Value = true;
        await settings.Saving;
        settings.IsScrollingEnabled.Value = true;
        await settings.Saving;

        Assert.Equal(2, saves);
    }

    public static TheoryData<string, Func<GlobalSettingsViewModel, BindableReactiveProperty<bool>>,
        Func<GlobalSettingsViewModel, IReadOnlyBindableReactiveProperty<string?>>, Func<LanguageModel, string?>,
        Func<LanguageModel, string?>> Texts => new()
    {
        { "Looping", s => s.IsLooping, s => s.LoopingText, t => t.LoopingEnabled, t => t.LoopingDisabled },
        { "Scrolling", s => s.IsScrollingEnabled, s => s.ScrollingText, t => t.ScrollingEnabled, t => t.ScrollingDisabled },
        { "FileHistory", s => s.IsFileHistoryEnabled, s => s.FileHistoryText, t => t.FileHistoryEnabled, t => t.FileHistoryDisabled },
        { "Interface", s => s.ShowInterface, s => s.InterfaceText, t => t.HideUI, t => t.ShowUI },
        { "BottomBar", s => s.ShowBottomBar, s => s.BottomBarText, t => t.HideBottomToolbar, t => t.ShowBottomToolbar },
        { "HoverBar", s => s.ShowHoverBar, s => s.HoverBarText, t => t.HideHoverNavigationBar, t => t.ShowHoverNavigationBar },
    };

    [Theory, MemberData(nameof(Texts))]
    public void Text_FollowsItsSetting_AndRephraseFollowsTheLanguage(string _,
        Func<GlobalSettingsViewModel, BindableReactiveProperty<bool>> pick,
        Func<GlobalSettingsViewModel, IReadOnlyBindableReactiveProperty<string?>> text,
        Func<LanguageModel, string?> on, Func<LanguageModel, string?> off)
    {
        TranslationManager.LoadLanguage("en");
        var settings = New();
        settings.Rephrase();
        var setting = pick(settings);
        setting.Value = false;
        Assert.Equal(off(TranslationManager.Translation), text(settings).Value);

        setting.Value = true;
        Assert.Equal(on(TranslationManager.Translation), text(settings).Value);

        TranslationManager.LoadLanguage("da");
        settings.Rephrase();
        Assert.Equal(on(TranslationManager.Translation), text(settings).Value);
        TranslationManager.LoadLanguage("en");
    }

    [Fact]
    public void Subscriber_MadeAfterConstruction_ReadsTheStoredValueAlreadyWritten()
    {
        var settings = New();
        bool? storedWhenNotified = null;
        using var later = settings.IsLooping.Skip(1).Subscribe(_ => storedWhenNotified = Settings.UIProperties.Looping);

        settings.IsLooping.Value = !Settings.UIProperties.Looping;

        Assert.Equal(settings.IsLooping.Value, storedWhenNotified);
    }

    [Fact]
    public void InitializeExtraSettings_GalleryWheelChanged_WritesTheStoredValue()
    {
        var settings = New();
        settings.InitializeExtraSettings();
        settings.GalleryMouseWheelBehavior.Value = (int)GalleryMouseWheel.Navigate;
        Assert.Equal(GalleryMouseWheel.Navigate, Settings.Gallery.GalleryMouseWheelBehavior);
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
