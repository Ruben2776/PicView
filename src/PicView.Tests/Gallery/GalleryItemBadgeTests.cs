using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.Views.Gallery;
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
        TranslationManager.Init();
        SetDefaults();
    }

    [AvaloniaFact]
    public void MotionPhotoBadge_Visibility_FollowsDetectionAndSetting()
    {
        RunWithDispatcher(Dispatcher.UIThread.InvokeAsync<bool>(() =>
        {
            _gallerySettings = new GallerySharedSettingsViewModel();
            var mainVm = new MainWindowViewModel(null!, new TranslationViewModel(),
                new GlobalSettingsViewModel(), _gallerySettings);
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
        Assert.False(_gallerySettings!.ShowMotionPhotoBadges.Value);
        Assert.False(_badge.IsVisible);

        // Badges are off by default, turning the setting on shows the badge of a motion photo
        RunWithDispatcher(Dispatcher.UIThread.InvokeAsync<bool>(() =>
        {
            _gallerySettings.ShowMotionPhotoBadges.Value = true;
            return true;
        }));
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
}
