using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Update;
using PicView.Core.ViewModels;

namespace PicView.Tests.Update;

[Collection("Sequential")]
public class AboutViewModelTests
{
    public AboutViewModelTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var mockUpdate = new MockPlatformSpecificUpdate();
        using var vm = new AboutViewModel(mockUpdate);

        Assert.False(vm.IsLoading.Value);
        Assert.True(vm.IsHitTestVisible.Value);
        Assert.Equal(1.0, vm.WindowOpacity.Value);
        Assert.True(vm.IsUpdateAvailable.Value);
        Assert.Null(vm.UpdateStatusText.Value);
        Assert.NotNull(vm.UpdateCommand);
    }

    [Fact]
    public void Constructor_SetsVersionNumber()
    {
        var mockUpdate = new MockPlatformSpecificUpdate();
        using var vm = new AboutViewModel(mockUpdate);

        Assert.NotNull(vm.UpdateVersionNumber.Value);
    }

    [Fact]
    public async Task UpdateCurrentVersion_SetsAndResetsLoadingState()
    {
        // UpdateManager.UpdateCurrentVersion will attempt network download,
        // fail, and return false. The finally block should reset loading state.
        var mockUpdate = new MockPlatformSpecificUpdate();
        using var vm = new AboutViewModel(mockUpdate);

        await vm.UpdateCurrentVersion();

        // After completion, loading state should be reset
        Assert.False(vm.IsLoading.Value);
        Assert.True(vm.IsHitTestVisible.Value);
        Assert.Equal(1.0, vm.WindowOpacity.Value);
    }

    [Fact]
    public async Task UpdateCurrentVersion_NoUpdateAvailable_SetsUpdateAvailableToFalse()
    {
        // When no update is found (download fails or version is current),
        // UpdateManager returns false and IsUpdateAvailable should be false
        var mockUpdate = new MockPlatformSpecificUpdate();
        using var vm = new AboutViewModel(mockUpdate);

        UpdateManager.ForceUpdate = false;
        await vm.UpdateCurrentVersion();

        Assert.False(vm.IsUpdateAvailable.Value);
        // UpdateStatusText is set to TranslationManager.Translation.NoUpdateFound
        Assert.Equal(TranslationManager.Translation.NoUpdateFound, vm.UpdateStatusText.Value);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var mockUpdate = new MockPlatformSpecificUpdate();
        var vm = new AboutViewModel(mockUpdate);

        var exception = Record.Exception(() => vm.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var mockUpdate = new MockPlatformSpecificUpdate();
        var vm = new AboutViewModel(mockUpdate);

        var exception = Record.Exception(() =>
        {
            vm.Dispose();
            vm.Dispose();
        });
        Assert.Null(exception);
    }

    [Fact]
    public void IsUpdateAvailable_DefaultIsTrue()
    {
        var mockUpdate = new MockPlatformSpecificUpdate();
        using var vm = new AboutViewModel(mockUpdate);

        Assert.True(vm.IsUpdateAvailable.Value);
    }

    [Fact]
    public void UpdateStatusText_DefaultIsNull()
    {
        var mockUpdate = new MockPlatformSpecificUpdate();
        using var vm = new AboutViewModel(mockUpdate);

        Assert.Null(vm.UpdateStatusText.Value);
    }

    private class MockPlatformSpecificUpdate : IPlatformSpecificUpdate
    {
        public Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath)
        {
            return Task.CompletedTask;
        }
    }
}
