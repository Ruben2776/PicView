using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia;
using PicView.Avalonia.Views.Gallery;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Tests.Gallery;

/// <summary>
/// Verifies that the motion photo badge on gallery thumbnails follows both the item's
/// detection state and the shared "show motion photo badges" setting. The badge visibility
/// is a MultiBinding that reaches the window's DataContext via $parent[Window], which this
/// exercises end to end.
/// </summary>
[Collection("Sequential")]
public class GalleryItemBadgeTests
{
    private Border? _badge;
    private GalleryItemViewModel? _itemVm;
    private GallerySharedSettingsViewModel? _gallerySettings;

    public GalleryItemBadgeTests()
    {
        try
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
        }
        catch (InvalidOperationException)
        {
            // Another headless test class has already configured the app in this process
        }

        TranslationManager.Init();
        SetDefaults();
    }

    [Fact]
    public void MotionPhotoBadge_Visibility_FollowsDetectionAndSetting()
    {
        RunWithDispatcher(Dispatcher.UIThread.InvokeAsync<bool>(() =>
        {
            _gallerySettings = new GallerySharedSettingsViewModel();
            var mainVm = new MainWindowViewModel(new TranslationViewModel(),
                new StubPlatformWindowService(), new GlobalSettingsViewModel(), _gallerySettings);
            _itemVm = new GalleryItemViewModel { IsMotionPhoto = { Value = true } };

            var window = new Window { DataContext = mainVm };
            var item = new GalleryItem { DataContext = _itemVm };
            window.Content = item;
            window.Show();

            _badge = item.GetVisualDescendants().OfType<Border>()
                .FirstOrDefault(b => !b.IsHitTestVisible && Math.Abs(b.Opacity - 0.9) < 0.01);
            return true;
        }));

        Assert.NotNull(_badge);
        Assert.True(_gallerySettings!.ShowMotionPhotoBadges.Value);
        Assert.True(_badge.IsVisible);

        // Turning the setting off hides the badge even though the item is a motion photo
        RunWithDispatcher(Dispatcher.UIThread.InvokeAsync<bool>(() =>
        {
            _gallerySettings.ShowMotionPhotoBadges.Value = false;
            return true;
        }));
        Assert.False(_badge.IsVisible);

        RunWithDispatcher(Dispatcher.UIThread.InvokeAsync<bool>(() =>
        {
            _gallerySettings.ShowMotionPhotoBadges.Value = true;
            return true;
        }));
        Assert.True(_badge.IsVisible);

        // A regular image never shows the badge
        RunWithDispatcher(Dispatcher.UIThread.InvokeAsync<bool>(() =>
        {
            _itemVm!.IsMotionPhoto.Value = false;
            return true;
        }));
        Assert.False(_badge.IsVisible);
    }

    private static void RunWithDispatcher(DispatcherOperation operation)
    {
        while (operation.Status is not DispatcherOperationStatus.Completed and not DispatcherOperationStatus.Aborted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        operation.GetAwaiter().GetResult();
    }

    private sealed class StubPlatformWindowService : IPlatformWindowService
    {
        public int CombinedTitleButtonsWidth { get; set; }
        public Task Maximize(bool saveSetting = true) => Task.CompletedTask;
        public void Minimize() { }
        public Task MaximizeRestore(bool saveSettings = true) => Task.CompletedTask;
        public Task Fullscreen(bool saveSetting = true) => Task.CompletedTask;
        public Task ToggleFullscreen(bool saveSettings = true) => Task.CompletedTask;
        public Task Restore() => Task.CompletedTask;
        public void ShowAboutWindow() { }
        public Task ShowImageInfoWindow() => Task.CompletedTask;
        public Task ShowKeybindingsWindow() => Task.CompletedTask;
        public ValueTask ShowSettingsWindow() => ValueTask.CompletedTask;
        public void ShowEffectsWindow() { }
        public void ShowSingleImageResizeWindow() { }
        public ValueTask ShowBatchResizeWindow() => ValueTask.CompletedTask;
        public void ShowFileAssociationsWindow() { }
        public void ShowConvertWindow() { }
        public Task ShowPrintWindow(string path) => Task.CompletedTask;
    }
}
