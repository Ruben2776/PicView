using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Core.Gallery;
using R3;

namespace PicView.Avalonia.Navigation;

/// <summary>
/// What one Main Window does when a tab it shows changes: its image viewer follows each Fit of a new image, and the
/// taskbar progress follows the tab's <see cref="TabViewModel.Shown"/> change on the UI thread. The gallery loads its
/// items by itself (ADR-0010). Attaching may happen on any thread, and attaching reacts to nothing by itself.
/// </summary>
public sealed class NavigationSubscriptions : ITabHookups
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public NavigationSubscriptions(MainWindow mainWindow, MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        mainWindow.Disposables.Add(mainWindow.WindowFitter.Fits.Subscribe(FollowFit));
    }

    public IDisposable Attach(TabViewModel tabViewModel)
    {
        return Dispatcher.UIThread.Invoke(() => Disposable.Combine(
            // Posted, never invoked: the thread that showed the image does not wait for the window
            tabViewModel.Shown
                .Subscribe(_ => Dispatcher.UIThread.Post(() =>
                        UpdateImage.SetTaskbarProgress(_mainWindowViewModel, tabViewModel), DispatcherPriority.Send),
                    DebugHelper.LogError(nameof(NavigationSubscriptions), nameof(UpdateImage.SetTaskbarProgress)))));
    }

    // Fits publish on the UI thread. The tab reset its Zoom Factor when it published the image; the control's
    // transform follows here, after the Fit, so the scale it reports meets the new Fit Scale (ADR-0016)
    private void FollowFit(Fitted fitted)
    {
        var tab = _mainWindowViewModel.WindowTabs.ActiveTab.CurrentValue;
        if (fitted.Cause is not FitCause.NewImage || tab.CurrentView.Value is not ImageViewer viewer)
        {
            return;
        }

        if (Settings.Zoom.ResetZoomOnChange)
        {
            viewer.ResetZoomSlim();
        }

        viewer.UpdateMotionPhoto(tab);
    }
}
