using System.Windows.Input;
using PicView.Core.Config;
using PicView.Core.ISettings;
using PicView.Core.Localization;
using PicView.Core.SettingsSearch;
using PicView.Core.ViewModels;
using PicView.Tests.Config;
using R3;

namespace PicView.Tests;

[Collection("Sequential")]
public class SettingsViewModelTests
{
    public SettingsViewModelTests()
    {
        var frameProvider = new ManualFrameProvider();
        ObservableSystem.DefaultFrameProvider = frameProvider;
        SetDefaults();
        TranslationManager.Init();
    }

    [Fact]
    public void RestoreLastTab_Zero_SetsOverview()
    {
        using var vm = new SettingsViewModel(new TranslationViewModel(), NewSettings());
        vm.RestoreLastTab(0);
        Assert.True(vm.IsOverviewVisible.Value);
    }

    [Fact]
    public void RestoreLastTab_One_SetsGeneral()
    {
        using var vm = new SettingsViewModel(new TranslationViewModel(), NewSettings());
        vm.RestoreLastTab(1);
        Assert.False(vm.IsOverviewVisible.Value);
        Assert.Equal(SettingsCategory.General, vm.SelectedCategory.Value);
    }

    [Fact]
    public void Navigation_History_Works()
    {
        using var vm = new SettingsViewModel(new TranslationViewModel(), NewSettings());
        vm.RestoreLastTab(0); // Start at Overview
        
        // Go to General
        // Simulate what happens when user clicks or scrolls
        // The VM subscribes to changes.
        vm.SelectedCategory.Value = SettingsCategory.General;
        vm.IsOverviewVisible.Value = false;
        
        // Expect Back enabled
        // Wait, subscriptions are synchronous? R3 usually is.
        Assert.True(vm.IsBackButtonEnabled.Value, "Back button should be enabled after navigation");
        
        // Go Back
        // Assert.True(vm.GoBackCommand.CanExecute(Unit.Default)); // CanExecute might take different args or ICommand interface
        Assert.True(((ICommand)vm.GoBackCommand).CanExecute(null));
        vm.GoBackCommand.Execute(Unit.Default);
        
        Assert.True(vm.IsOverviewVisible.Value, "Should be back at Overview");
        Assert.False(vm.IsBackButtonEnabled.Value, "Back button should be disabled at start");
        Assert.True(vm.IsForwardButtonEnabled.Value, "Forward button should be enabled");
        
        // Go Forward
        vm.GoForwardCommand.Execute(Unit.Default);
        Assert.False(vm.IsOverviewVisible.Value, "Should be forward at General");
        Assert.Equal(SettingsCategory.General, vm.SelectedCategory.Value);
    }
    
    [Fact]
    public void Constructor_UnstartedSettings_StartsTheExtraSettingsOnly()
    {
        var saves = 0;
        var settings = new GlobalSettingsViewModel(() => { saves++; return ValueTask.FromResult(true); });
        using var vm = new SettingsViewModel(new TranslationViewModel(), settings);
        using var again = new SettingsViewModel(new TranslationViewModel(), settings);
        var history = Settings.Navigation.IsFileHistoryEnabled;

        vm.WindowMargin.Value = 42;
        settings.IsFileHistoryEnabled.Value = !history;

        Assert.Equal(42, Settings.WindowProperties.Margin);
        Assert.Equal(1, saves);
        Assert.Equal(history, Settings.Navigation.IsFileHistoryEnabled);
    }

    private static GlobalSettingsViewModel NewSettings() => StartedSettings.New();

    [Fact]
    public void StartUpIndex_StoredContinueSession_IsKeptOnSubscribe()
    {
        Settings.Navigation.IsFileHistoryEnabled = true;
        Settings.StartUp.Behavior = StartUpBehavior.ContinueSession;

        using var vm = Started(NewSettings());

        Assert.Equal(2, vm.StartUpIndex.Value);
        Assert.Equal(StartUpBehavior.ContinueSession, Settings.StartUp.Behavior);
    }

    [Fact]
    public void StartUpIndex_HistoryOff_ShowsNone_AndKeepsStoredBehavior()
    {
        Settings.Navigation.IsFileHistoryEnabled = true;
        Settings.StartUp.Behavior = StartUpBehavior.ContinueSession;
        var settings = NewSettings();
        using var vm = Started(settings);

        settings.IsFileHistoryEnabled.Value = false;

        Assert.Equal(0, vm.StartUpIndex.Value);
        Assert.Equal(StartUpBehavior.ContinueSession, Settings.StartUp.Behavior);
    }

    [Fact]
    public void StartUpIndex_HistoryOff_ChangeWritesNothing()
    {
        Settings.Navigation.IsFileHistoryEnabled = true;
        Settings.StartUp.Behavior = StartUpBehavior.OpenLastFile;
        var settings = NewSettings();
        using var vm = Started(settings);
        settings.IsFileHistoryEnabled.Value = false;

        vm.StartUpIndex.Value = 2;

        Assert.Equal(StartUpBehavior.OpenLastFile, Settings.StartUp.Behavior);
    }

    [Fact]
    public void StartUpIndex_HistoryBackOn_ShowsStoredBehavior()
    {
        Settings.Navigation.IsFileHistoryEnabled = true;
        Settings.StartUp.Behavior = StartUpBehavior.ContinueSession;
        var settings = NewSettings();
        using var vm = Started(settings);
        settings.IsFileHistoryEnabled.Value = false;

        settings.IsFileHistoryEnabled.Value = true;

        Assert.Equal(2, vm.StartUpIndex.Value);
        Assert.Equal(StartUpBehavior.ContinueSession, Settings.StartUp.Behavior);
    }

    [Fact]
    public void StartUpIndex_Set2_WritesContinueSession()
    {
        Settings.Navigation.IsFileHistoryEnabled = true;
        Settings.StartUp.Behavior = StartUpBehavior.None;
        using var vm = Started(NewSettings());

        vm.StartUpIndex.Value = 2;

        Assert.Equal(StartUpBehavior.ContinueSession, Settings.StartUp.Behavior);
    }

    [Fact]
    public void StartUpIndex_SetMinus1_WritesNothing()
    {
        Settings.Navigation.IsFileHistoryEnabled = true;
        Settings.StartUp.Behavior = StartUpBehavior.OpenLastFile;
        using var vm = Started(NewSettings());

        vm.StartUpIndex.Value = -1;

        Assert.Equal(StartUpBehavior.OpenLastFile, Settings.StartUp.Behavior);
    }

    [Fact]
    public void ToggleFadeButtons_WritesThePublishedSetting()
    {
        var settings = NewSettings();
        using var vm = Started(settings);
        var before = settings.ShowFadeButtons.Value;

        vm.ToggleFadeButtonsCommand.Execute(Unit.Default);

        Assert.Equal(!before, settings.ShowFadeButtons.Value);
        Assert.Equal(!before, Settings.UIProperties.ShowAltInterfaceButtons);
    }

    private static SettingsViewModel Started(GlobalSettingsViewModel settings)
    {
        var vm = new SettingsViewModel(new TranslationViewModel(), settings);
        vm.Initialize(new NoThemeService(), new NoLanguageService(), new NoImageSettingsService());
        return vm;
    }

    private sealed class NoThemeService : IThemeService
    {
        public void SetTheme(int themeIndex) { }
        public void SetColorTheme(int colorIndex) { }
        public void SetAcrylic(bool isEnabled) { }
        public void SetBackground(int backgroundIndex) { }
    }

    private sealed class NoLanguageService : ILanguageService
    {
        public ValueTask UpdateLanguageAsync(string languageCode) => ValueTask.CompletedTask;
        public IEnumerable<(string Code, string DisplayName)> GetAvailableLanguages() => [];
    }

    private sealed class NoImageSettingsService : IImageSettingsService
    {
        public void TriggerScalingModeUpdate(bool nearestNeighbor) { }
    }

    // A language where every text is its key and a suffix, so a test can tell which language a text is in.
    private static LanguageModel Language(string suffix)
    {
        var model = new LanguageModel();
        foreach (var property in typeof(LanguageModel).GetProperties())
        {
            if (property.PropertyType == typeof(string))
            {
                property.SetValue(model, $"{property.Name}-{suffix}");
            }
        }

        return model;
    }

    [Fact]
    public void LanguageChanges_SearchKeywords_AreInTheNewLanguage()
    {
        var previous = TranslationManager.Swap(Language("a"));
        try
        {
            var translation = new TranslationViewModel();
            using var vm = new SettingsViewModel(translation, NewSettings());
            vm.SearchQuery.Value = "ContinueWhereYouLeftOff-a";
            Assert.Equal(SettingKey.ApplicationStartup, Assert.Single(vm.Suggestions.Value).Key);

            TranslationManager.Swap(Language("b"));
            translation.UpdateLanguage();

            Assert.Empty(vm.Suggestions.Value);
            vm.SearchQuery.Value = "ContinueWhereYouLeftOff-b";
            Assert.Equal(SettingKey.ApplicationStartup, Assert.Single(vm.Suggestions.Value).Key);
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }

    [Fact]
    public void SetSettingsOnPage_ASettingNotOnThePage_IsNotSuggested()
    {
        var previous = TranslationManager.Swap(Language("a"));
        try
        {
            using var vm = new SettingsViewModel(new TranslationViewModel(), NewSettings());
            vm.SetSettingsOnPage(new HashSet<SettingKey> { SettingKey.Theme },
                new HashSet<SettingsCategory> { SettingsCategory.Appearance });

            vm.SearchQuery.Value = "Theme-a";
            Assert.Equal(SettingKey.Theme, Assert.Single(vm.Suggestions.Value).Key);

            vm.SearchQuery.Value = "BlurWindowBackground-a";
            Assert.Empty(vm.Suggestions.Value);
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }

    [Fact]
    public void SetSettingsOnPage_ACategoryNotOnThePage_IsNotSuggested()
    {
        var previous = TranslationManager.Swap(Language("a"));
        try
        {
            using var vm = new SettingsViewModel(new TranslationViewModel(), NewSettings());
            vm.SearchQuery.Value = "FileAssociations-a";
            Assert.Contains(vm.Suggestions.Value, item => item.IsCategory);

            vm.SetSettingsOnPage(new HashSet<SettingKey>(), new HashSet<SettingsCategory>());

            Assert.Empty(vm.Suggestions.Value);
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }

    [Fact]
    public void LanguageChanges_WithAQueryTyped_SuggestsInTheNewLanguage()
    {
        var previous = TranslationManager.Swap(Language("a"));
        try
        {
            var translation = new TranslationViewModel();
            using var vm = new SettingsViewModel(translation, NewSettings());
            vm.SearchQuery.Value = "OpenLastFile";
            Assert.NotEmpty(vm.Suggestions.Value);

            TranslationManager.Swap(Language("b"));
            translation.UpdateLanguage();

            Assert.Equal("OpenLastFile", vm.SearchQuery.Value);
            Assert.NotEmpty(vm.Suggestions.Value);
            Assert.All(vm.Suggestions.Value, item => Assert.EndsWith("-b", item.Name, StringComparison.Ordinal));
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }

    [Fact]
    public void LanguageChanges_WithNoQuery_SuggestsNothing()
    {
        var previous = TranslationManager.Swap(Language("a"));
        try
        {
            var translation = new TranslationViewModel();
            using var vm = new SettingsViewModel(translation, NewSettings());

            TranslationManager.Swap(Language("b"));
            translation.UpdateLanguage();

            Assert.Empty(vm.Suggestions.Value);
            Assert.False(vm.IsSearchVisible.Value);
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }

    [Fact]
    public void LanguageChanges_TextsAreMissing_DoesNotThrow()
    {
        var previous = TranslationManager.Swap(Language("a"));
        try
        {
            var translation = new TranslationViewModel();
            using var vm = new SettingsViewModel(translation, NewSettings());
            vm.SearchQuery.Value = "OpenLastFile";

            // A language file that has only the two texts UpdateLanguage cannot do without
            TranslationManager.Swap(new LanguageModel { File = "file", Files = "files" });
            var exception = Record.Exception(translation.UpdateLanguage);

            Assert.Null(exception);
            Assert.Empty(vm.Suggestions.Value);
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }

    [Fact]
    public void LanguageChanges_AfterDispose_DoesNotRebuild()
    {
        var previous = TranslationManager.Swap(Language("a"));
        try
        {
            var translation = new TranslationViewModel();
            var vm = new SettingsViewModel(translation, NewSettings());
            vm.SearchQuery.Value = "OpenLastFile";
            var before = vm.Suggestions.Value;
            vm.Dispose();

            TranslationManager.Swap(Language("b"));
            translation.UpdateLanguage();

            Assert.Same(before, vm.Suggestions.Value);
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }

    [Fact]
    public void LanguageChanges_DoubleClickBehavior_KeepsItsIndexAndItsSetting()
    {
        var previous = TranslationManager.Swap(Language("a"));
        try
        {
            Settings.UIProperties.DoubleClickBehavior = 2;
            var translation = new TranslationViewModel();
            using var vm = new SettingsViewModel(translation, NewSettings());

            TranslationManager.Swap(Language("b"));
            translation.UpdateLanguage();

            Assert.Equal(2, vm.MouseDoubleClickBehaviorIndex.Value);
            Assert.Equal(2, Settings.UIProperties.DoubleClickBehavior);
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }

    private class ManualFrameProvider : FrameProvider
    {
        private readonly List<IFrameRunnerWorkItem> _items = [];

        public override long GetFrameCount() => 0;

        public override void Register(IFrameRunnerWorkItem callback)
        {
            _items.Add(callback);
        }
    }
}
