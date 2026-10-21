using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;
using PicView.Core.Gallery;
using R3;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// What one Main Window does when a tab it shows changes: shows the tab's new image, and loads and opens its gallery.
/// Attaching may happen on any thread.
/// </summary>
public sealed class NavigationSubscriptions(MainWindow mainWindow, MainWindowViewModel mainWindowViewModel,
    CoreViewModel core) : ITabHookups
{
    public IDisposable Attach(TabViewModel tabViewModel)
    {
        // Subscribing with AvaloniaRenderingFrameProvider is faster and fixes not being able to navigate while gallery is loading
        return Dispatcher.UIThread.Invoke(() => Disposable.Combine(
            Observable.EveryValueChanged(tabViewModel, tab => tab.Model.FileInfo, mainWindow.FrameProvider)
                .Skip(1)
                .Subscribe(file =>
                {
                    UpdateImage.UpdateFileInfo(tabViewModel, file);
                }, DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(UpdateImage))),
            Observable.EveryValueChanged(tabViewModel, tab => tab.Model.Image, mainWindow.FrameProvider)
                .Skip(1)
                .Subscribe(_ =>
                {
                    UpdateImage.ChangeImageWithTitle(mainWindow, tabViewModel, mainWindowViewModel);
                }, DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(UpdateImage))),
            Observable.EveryValueChanged(tabViewModel, tab => tab.Gallery.ActiveGalleryMode.Value, mainWindow.FrameProvider)
                .Skip(1)
                .SubscribeAwait(async (mode, ct) =>
                {
                    if (mode is GalleryMode.Docked or GalleryMode.Expanded)
                    {
                        if (tabViewModel.Gallery.LoadingState is GalleryLoadingState.NotLoaded && tabViewModel is { GalleryCoordinator: not null, ImageIterator: not null })
                        {
                            await Task.Run(() => tabViewModel.GalleryCoordinator.LoadGallery(tabViewModel.ImageIterator.Files, core), ct).ConfigureAwait(false);
                        }
                    }
                }, DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(tabViewModel.GalleryCoordinator.LoadGallery))),
            tabViewModel.Gallery.OpenSelectedItemCommand
                .Skip(1)
                .SubscribeAwait(async (index, _) =>
                {
                    if (tabViewModel is { GalleryCoordinator: not null })
                    {
                        await tabViewModel.GalleryCoordinator.ToggleGalleryAndLoadItem(index).ConfigureAwait(false);
                    }
                }, DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(tabViewModel.GalleryCoordinator.ToggleGalleryAndLoadItem)))));
    }
}
