using PicView.Core.ViewModels;
using PicView.Core.Config;
using PicView.Core.Localization;
using Xunit;
using R3;

namespace PicView.Tests;

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
        using var vm = new SettingsViewModel(new TranslationViewModel());
        vm.RestoreLastTab(0);
        Assert.True(vm.IsOverviewVisible.Value);
    }

    [Fact]
    public void RestoreLastTab_One_SetsGeneral()
    {
        using var vm = new SettingsViewModel(new TranslationViewModel());
        vm.RestoreLastTab(1);
        Assert.False(vm.IsOverviewVisible.Value);
        Assert.Equal(SettingsCategory.General, vm.SelectedCategory.Value);
    }

    [Fact]
    public void Navigation_History_Works()
    {
        using var vm = new SettingsViewModel(new TranslationViewModel());
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
