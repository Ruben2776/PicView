using PicView.Avalonia.Input;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;

namespace PicView.Tests.Input;

public class ContextualActionHandlerTests
{
    private sealed class FakeCropService : ICropService
    {
        public List<string> Calls { get; } = [];

        public bool IsCropping => true;

        public Task StartCropControlAsync() => Task.CompletedTask;

        public Task SaveCropAsAsync()
        {
            Calls.Add(nameof(SaveCropAsAsync));
            return Task.CompletedTask;
        }

        public ValueTask CopyCroppedImageAsync()
        {
            Calls.Add(nameof(CopyCroppedImageAsync));
            return ValueTask.CompletedTask;
        }

        public void CloseCropControl() => Calls.Add(nameof(CloseCropControl));

        public object? GetCroppedImage() => null;
    }

    private static (ContextualActionHandler Handler, FakeCropService Crop) CreateHandler()
    {
        SetDefaults();
        var vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var crop = new FakeCropService();
        var tab = new TabViewModel(vm) { CropService = crop };
        vm.WindowTabs.ActiveTab.Value = tab;

        // The crop meanings only use the view model, not the window
        return (new ContextualActionHandler(null!, vm), crop);
    }

    [Theory]
    // Confirm asks where to save, as cropping from the context menu does, instead of overwriting the image
    [InlineData(ContextualActionHandler.Confirm, nameof(ICropService.SaveCropAsAsync))]
    [InlineData(ContextualActionHandler.Cancel, nameof(ICropService.CloseCropControl))]
    [InlineData(ContextualActionHandler.Copy, nameof(ICropService.CopyCroppedImageAsync))]
    public async Task Crop_ContextualAction_RunsItsCropOperation(string action, string expectedCall)
    {
        var (handler, crop) = CreateHandler();

        var claimed = await handler.TryRunAsync(InputMode.Crop, action);

        Assert.True(claimed);
        Assert.Equal([expectedCall], crop.Calls);
    }

    [Fact]
    public async Task Crop_PlayPause_IsNotClaimed()
    {
        var (handler, crop) = CreateHandler();

        Assert.False(await handler.TryRunAsync(InputMode.Crop, ContextualActionHandler.PlayPause));
        Assert.Empty(crop.Calls);
    }
}
