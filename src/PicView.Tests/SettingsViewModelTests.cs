using PicView.Tests.Config;
using PicView.Core.ViewModels;
using PicView.Core.Config;
using PicView.Core.Localization;
using Xunit;
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
        Assert.True(((System.Windows.Input.ICommand)vm.GoBackCommand).CanExecute(null));
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
    public void ToggleFadeButtons_WritesThePublishedSetting()
    {
        var settings = NewSettings();
        using var vm = new SettingsViewModel(new TranslationViewModel(), settings);
        var before = settings.ShowFadeButtons.Value;

        vm.ToggleFadeButtonsCommand.Execute(Unit.Default);

        Assert.Equal(!before, settings.ShowFadeButtons.Value);
        Assert.Equal(!before, Settings.UIProperties.ShowAltInterfaceButtons);
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
    public void LanguageChanges_SearchTags_AreInTheNewLanguage()
    {
        var previous = TranslationManager.Swap(Language("a"));
        try
        {
            var translation = new TranslationViewModel();
            using var vm = new SettingsViewModel(translation, NewSettings());
            Assert.Contains("ApplicationStartup-a", vm.SearchData.Value.StartUpSearchTags, StringComparison.Ordinal);

            TranslationManager.Swap(Language("b"));
            translation.UpdateLanguage();

            Assert.Contains("ApplicationStartup-b", vm.SearchData.Value.StartUpSearchTags, StringComparison.Ordinal);
            Assert.DoesNotContain("-a", vm.SearchData.Value.StartUpSearchTags, StringComparison.Ordinal);
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
            var before = vm.SearchData.Value;

            // A language file that has only the two texts UpdateLanguage cannot do without
            TranslationManager.Swap(new LanguageModel { File = "file", Files = "files" });
            translation.UpdateLanguage();

            Assert.NotSame(before, vm.SearchData.Value);
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
            var before = vm.SearchData.Value;
            vm.Dispose();

            TranslationManager.Swap(Language("b"));
            translation.UpdateLanguage();

            Assert.Same(before, vm.SearchData.Value);
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
