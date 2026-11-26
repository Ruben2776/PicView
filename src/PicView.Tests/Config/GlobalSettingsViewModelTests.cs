using PicView.Core.Config;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Config;

/// <summary>
/// The four toggled settings and the Opening Mode publish their changes (ADR-0020): each writes its stored value before any
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

    private GlobalSettingsViewModel New() => StartedSettings.New(CountingSave);

    private GlobalSettingsViewModel Unstarted() => new(CountingSave);

    public static TheoryData<string, Func<GlobalSettingsViewModel, BindableReactiveProperty<bool>>, Func<bool>> ActionSettings => new()
    {
        { "Subdirectories", s => s.IsIncludingSubdirectories, () => Settings.Sorting.IncludeSubDirectories },
        { "FileHistory", s => s.IsFileHistoryEnabled, () => Settings.Navigation.IsFileHistoryEnabled },
        { "FullPath", s => s.IsShowingFullPathInTitleBar, () => Settings.UIProperties.ShowFullPathInTitleBar },
        { "TaskbarProgress", s => s.IsShowingTaskbarProgress, () => Settings.UIProperties.IsTaskbarProgressEnabled },
    };

    public static TheoryData<string, Func<GlobalSettingsViewModel, BindableReactiveProperty<bool>>, Func<bool>> Published
    {
        get
        {
            var all = new TheoryData<string, Func<GlobalSettingsViewModel, BindableReactiveProperty<bool>>, Func<bool>>();
            foreach (var row in ActionSettings)
            {
                all.Add(row);
            }
            all.Add("FadeButtons", s => s.ShowFadeButtons, () => Settings.UIProperties.ShowAltInterfaceButtons);
            all.Add("SideBySideRightToLeft", s => s.IsSideBySideRightToLeft, () => Settings.ImageScaling.IsSideBySideRightToLeft);
            return all;
        }
    }

    [Theory, MemberData(nameof(Published))]
    public void Setting_ChangedBeforeInitialize_WritesAndSavesNothing(string _,
        Func<GlobalSettingsViewModel, BindableReactiveProperty<bool>> pick, Func<bool> stored)
    {
        var setting = pick(Unstarted());
        var before = stored();

        setting.Value = !before;

        Assert.Equal(before, stored());
        Assert.Equal(0, _saves);
    }

    [Theory, MemberData(nameof(ActionSettings))]
    public void InitializeActionSettings_ActionSettingChanged_WritesAndSavesOnce(string _,
        Func<GlobalSettingsViewModel, BindableReactiveProperty<bool>> pick, Func<bool> stored)
    {
        var settings = Unstarted();
        settings.InitializeActionSettings();
        var before = stored();

        pick(settings).Value = !before;

        Assert.Equal(!before, stored());
        Assert.Equal(1, _saves);
    }

    [Fact]
    public void OpeningModeIndex_Set2_WritesNewTabAndSaves()
    {
        var settings = Unstarted();
        settings.InitializeActionSettings();

        settings.OpeningModeIndex.Value = 2;

        Assert.Equal(OpeningMode.NewTab, Settings.UIProperties.OpeningMode);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public void OpeningModeIndex_SetMinus1_WritesNothing()
    {
        var settings = Unstarted();
        settings.InitializeActionSettings();

        settings.OpeningModeIndex.Value = -1;

        Assert.Equal(OpeningMode.NewWindow, Settings.UIProperties.OpeningMode);
    }

    [Fact]
    public void OpeningModeIndex_ChangedBeforeInitialize_WritesAndSavesNothing()
    {
        var settings = Unstarted();

        settings.OpeningModeIndex.Value = 2;

        Assert.Equal(OpeningMode.NewWindow, Settings.UIProperties.OpeningMode);
        Assert.Equal(0, _saves);
    }

    [Fact]
    public void OpeningModeIndex_StartsAtTheStoredMode()
    {
        Settings.UIProperties.OpeningMode = OpeningMode.SameWindow;

        Assert.Equal(1, Unstarted().OpeningModeIndex.Value);
    }

    [Fact]
    public void InitializeActionSettings_ExtraSettingsChanged_WriteNothingUntilInitializeExtraSettings()
    {
        var settings = Unstarted();
        settings.InitializeActionSettings();
        var fade = Settings.UIProperties.ShowAltInterfaceButtons;
        var wheel = Settings.Gallery.GalleryMouseWheelBehavior;

        settings.ShowFadeButtons.Value = !fade;
        settings.WindowMargin.Value = 42;
        settings.GalleryMouseWheelBehavior.Value = (int)GalleryMouseWheel.Navigate;

        Assert.Equal(fade, Settings.UIProperties.ShowAltInterfaceButtons);
        Assert.NotEqual(42, Settings.WindowProperties.Margin);
        Assert.Equal(wheel, Settings.Gallery.GalleryMouseWheelBehavior);
        Assert.Equal(0, _saves);

        settings.InitializeExtraSettings();
        settings.WindowMargin.Value = 43;
        Assert.Equal(43, Settings.WindowProperties.Margin);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public void Initializers_CalledTwice_SaveOncePerChange()
    {
        var settings = Unstarted();
        settings.InitializeActionSettings();
        settings.InitializeActionSettings();
        settings.InitializeExtraSettings();
        settings.InitializeExtraSettings();

        settings.IsFileHistoryEnabled.Value = !Settings.Navigation.IsFileHistoryEnabled;
        Assert.Equal(1, _saves);
        settings.WindowMargin.Value = 42;
        Assert.Equal(2, _saves);
    }

    [Fact]
    public void InitializeActionSettings_CalledFromManyThreads_SavesOncePerChange()
    {
        var settings = Unstarted();

        Parallel.For(0, 16, _ => settings.InitializeActionSettings());
        settings.IsFileHistoryEnabled.Value = !Settings.Navigation.IsFileHistoryEnabled;

        Assert.Equal(1, _saves);
    }

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
    public void IsSideBySideRightToLeft_Changed_WritesTheStoredValueAndSavesOnce()
    {
        var settings = New();
        Assert.False(settings.IsSideBySideRightToLeft.Value);

        settings.IsSideBySideRightToLeft.Value = true;

        Assert.True(Settings.ImageScaling.IsSideBySideRightToLeft);
        Assert.Equal(1, _saves);
    }

    [Fact]
    public void IsSideBySideRightToLeft_StartsAtTheStoredOrder()
    {
        Settings.ImageScaling.IsSideBySideRightToLeft = true;

        Assert.True(Unstarted().IsSideBySideRightToLeft.Value);
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
        Settings.Navigation.IsFileHistoryEnabled = true;
        var settings = New();
        Assert.True(settings.IsFileHistoryEnabled.Value);
        Assert.True(Settings.Navigation.IsFileHistoryEnabled);
        Assert.Equal(0, _saves);
    }

    [Fact]
    public void Setting_SetToTheValueItHas_SavesNothing()
    {
        var settings = New();
        settings.IsFileHistoryEnabled.Value = Settings.Navigation.IsFileHistoryEnabled;
        Assert.Equal(0, _saves);
    }

    [Fact]
    public async Task SaveSoon_TwoChangesDuringOneSave_SavesExactlyOnceMore()
    {
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var saves = 0;
        var settings = new GlobalSettingsViewModel(() =>
            Interlocked.Increment(ref saves) is 1 ? new ValueTask<bool>(gate.Task) : ValueTask.FromResult(true));
        settings.InitializeActionSettings();
        settings.InitializeExtraSettings();

        settings.IsFileHistoryEnabled.Value = !Settings.Navigation.IsFileHistoryEnabled; // save 1 starts and is held
        settings.IsShowingTaskbarProgress.Value = !Settings.UIProperties.IsTaskbarProgressEnabled; // two changes land while it runs
        settings.OpeningModeIndex.Value = (int)OpeningMode.NewTab;
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
        settings.InitializeActionSettings();
        settings.InitializeExtraSettings();

        settings.IsFileHistoryEnabled.Value = !Settings.Navigation.IsFileHistoryEnabled;
        await settings.Saving;
        settings.IsShowingTaskbarProgress.Value = !Settings.UIProperties.IsTaskbarProgressEnabled;
        await settings.Saving;

        Assert.Equal(2, saves);
    }

    public static TheoryData<string, Func<GlobalSettingsViewModel, BindableReactiveProperty<bool>>,
        Func<GlobalSettingsViewModel, IReadOnlyBindableReactiveProperty<string?>>, Func<LanguageModel, string?>,
        Func<LanguageModel, string?>> Texts => new()
    {
        { "FileHistory", s => s.IsFileHistoryEnabled, s => s.FileHistoryText, t => t.FileHistoryEnabled, t => t.FileHistoryDisabled },
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
    public void Subscriber_MadeAfterInitialize_ReadsTheStoredValueAlreadyWritten()
    {
        var settings = New();
        bool? storedWhenNotified = null;
        using var later = settings.IsFileHistoryEnabled.Skip(1).Subscribe(_ => storedWhenNotified = Settings.Navigation.IsFileHistoryEnabled);

        settings.IsFileHistoryEnabled.Value = !Settings.Navigation.IsFileHistoryEnabled;

        Assert.Equal(settings.IsFileHistoryEnabled.Value, storedWhenNotified);
    }

    [Fact]
    public void InitializeExtraSettings_GalleryWheelChanged_WritesTheStoredValue()
    {
        var settings = New();
        settings.InitializeExtraSettings();
        settings.GalleryMouseWheelBehavior.Value = (int)GalleryMouseWheel.Navigate;
        Assert.Equal(GalleryMouseWheel.Navigate, Settings.Gallery.GalleryMouseWheelBehavior);
    }

    [Fact]
    public void InitializeExtraSettings_GalleryWheelChanged_SavesOnce()
    {
        var settings = New();
        settings.InitializeExtraSettings();
        var before = _saves;

        settings.GalleryMouseWheelBehavior.Value = (int)GalleryMouseWheel.Navigate;

        Assert.Equal(before + 1, _saves);
    }

    [Fact]
    public void InitializeExtraSettings_GalleryWheelOutOfRange_StoresScroll()
    {
        var settings = New();
        settings.InitializeExtraSettings();
        settings.GalleryMouseWheelBehavior.Value = (int)GalleryMouseWheel.Navigate;

        settings.GalleryMouseWheelBehavior.Value = 7;

        Assert.Equal(GalleryMouseWheel.Scroll, Settings.Gallery.GalleryMouseWheelBehavior);
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
