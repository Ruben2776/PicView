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
                    UpdateImage.ChangeImage(mainWindow, tabViewModel, mainWindowViewModel);
                }, DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(UpdateImage)))
                .AddTo(tabViewModel.Disposables);

            Observable.EveryValueChanged(tabViewModel, tab => tab.Gallery.ActiveGalleryMode.Value, mainWindow.FrameProvider)
                .Skip(1)
                .SubscribeAwait(async (mode, _) =>
                {
                    if (Application.Current.DataContext is not CoreViewModel core)
                    {
                        return;
                    }
                    if (mode is GalleryMode.Docked or GalleryMode.Expanded)
                    {
                        if (tabViewModel.Gallery.LoadingState is GalleryLoadingState.NotLoaded)
                        {
                            await GalleryHelper.LoadGallery(core).ConfigureAwait(false);
                        }
                    }
                }, DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(GalleryLoader.LoadGalleryAsync)))
                .AddTo(tabViewModel.Disposables);
            
            tabViewModel.Gallery.OpenSelectedItemCommand
                .Skip(1)
                .SubscribeAwait(async (index, _) =>
                {
                    await GalleryLoader.ToggleGalleryAndLoadItem(tabViewModel, index).ConfigureAwait(false);
                }, DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(GalleryLoader.ToggleGalleryAndLoadItem)))
                .AddTo(tabViewModel.Disposables);
        });
    }
}