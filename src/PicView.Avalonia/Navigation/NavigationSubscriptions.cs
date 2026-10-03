using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using PicView.Core.Gallery;
using R3;

namespace PicView.Avalonia.Navigation;

public static class NavigationSubscriptions
{
    public static void ModelSubscription(TabViewModel tabViewModel, MainWindowViewModel mainWindowViewModel, MainWindow mainWindow)
    {
        // Subscribing with AvaloniaRenderingFrameProvider is faster and fixes not being able to navigate while gallery is loading
        Dispatcher.UIThread.Invoke(() =>
        {
            Observable.EveryValueChanged(tabViewModel, tab => tab.Model.FileInfo, mainWindow.FrameProvider)
                .Skip(1)
                .Subscribe(file =>
                {
                    UpdateImage.UpdateFileInfo(tabViewModel, file);
                }, DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(UpdateImage)))
                .AddTo(tabViewModel.Disposables);
            Observable.EveryValueChanged(tabViewModel, tab => tab.Model.Image, mainWindow.FrameProvider)
                .Skip(1)
                .Subscribe(_ =>
                {
                    UpdateImage.ChangeImageWithTitle(mainWindow, tabViewModel, mainWindowViewModel);
                }, DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(UpdateImage)))
                .AddTo(tabViewModel.Disposables);

            Observable.EveryValueChanged(tabViewModel, tab => tab.Gallery.ActiveGalleryMode.Value, mainWindow.FrameProvider)
                .Skip(1)
                .SubscribeAwait(async (mode, _) =>
                {
                    if (mode is GalleryMode.Docked or GalleryMode.Expanded)
                    {
                        if (tabViewModel.Gallery.LoadingState is GalleryLoadingState.NotLoaded && tabViewModel is { GalleryCoordinator: not null, ImageIterator: not null })
                        {
                            await tabViewModel.GalleryCoordinator.LoadGalleryAsync(tabViewModel.ImageIterator.Files).ConfigureAwait(false);
                        }
                    }
                }, DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(tabViewModel.GalleryCoordinator.LoadGalleryAsync)))
                .AddTo(tabViewModel.Disposables);
            
            tabViewModel.Gallery.OpenSelectedItemCommand
                .Skip(1)
                .SubscribeAwait(async (index, _) =>
                {
                    if (tabViewModel is { GalleryCoordinator: not null })
                    {
                        await tabViewModel.GalleryCoordinator.ToggleGalleryAndLoadItem(index).ConfigureAwait(false);
                    }
                }, DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(tabViewModel.GalleryCoordinator.ToggleGalleryAndLoadItem)))
                .AddTo(tabViewModel.Disposables);
        });
    }
}