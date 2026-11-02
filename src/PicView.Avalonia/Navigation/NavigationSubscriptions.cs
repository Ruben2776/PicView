using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;
using PicView.Core.Gallery;
using R3;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// What one Main Window does when a tab it shows changes: reacts to the tab's <see cref="TabViewModel.Shown"/> change
/// on the UI thread, and opens the gallery item the user picks. The gallery loads its items by itself (ADR-0010).
/// Attaching may happen on any thread, and attaching reacts to nothing by itself.
/// </summary>
public sealed class NavigationSubscriptions(MainWindow mainWindow, MainWindowViewModel mainWindowViewModel)
    : ITabHookups
{
    public IDisposable Attach(TabViewModel tabViewModel)
    {
        return Dispatcher.UIThread.Invoke(() => Disposable.Combine(
            // Posted, never invoked: the thread that showed the image does not wait for the window. At the highest
            // priority, so the reaction runs before anything posted after the image showed, as the Center at startup
            tabViewModel.Shown
                .Subscribe(change => Dispatcher.UIThread.Post(() =>
                        UpdateImage.ReactToShown(mainWindow, tabViewModel, mainWindowViewModel, change),
                    DispatcherPriority.Send),
                    DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(UpdateImage.ReactToShown))),
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
