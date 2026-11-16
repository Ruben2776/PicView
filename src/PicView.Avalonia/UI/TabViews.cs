using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.CustomControls.Zoom;
using PicView.Avalonia.Views.UC;
using PicView.Core.IPlatform;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.UI;

/// <summary>
/// One per Main Window: makes and keeps the controls the window shows for its tabs' Tab Views (ADR-0022). Each tab
/// has a menu, and a navigable view holding its image viewer, made once and kept, a comparison viewer made per
/// comparison and shown in the same navigable view, and a crop control made per crop. Attaching may happen on any
/// thread; the controls are made on the UI thread.
/// </summary>
public sealed class TabViews(MainWindow window) : ITabViewFactory
{
    public ITabViews Attach(TabViewModel tab) => new Set(window, tab);

    private sealed class Set : ITabViews
    {
        private readonly TabViewModel _tab;
        private readonly BindableReactiveProperty<object?> _view = new();
        private readonly IDisposable _following;
        private ImageViewer? _viewer;
        private ComparisonViewer? _comparisonViewer;
        private NavigableView? _navigable;
        private StartUpMenu? _menu;
        private CropControl? _crop;

        // The model the tab showed when its crop opened
        private ImageModel? _croppedModel;
        private bool _disposed;

        public Set(MainWindow window, TabViewModel tab)
        {
            _tab = tab;
            _following = tab.ViewKind.AsObservable().Subscribe(_ => Show());
        }

        public IReadOnlyBindableReactiveProperty<object?> View => _view;

        // Inline on the UI thread, else posted, never invoked: the thread that changed the kind does not wait for the window
        private void Show()
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                Apply();
                return;
            }

            // Reads the kind as it is when it runs
            Dispatcher.UIThread.Post(Apply, DispatcherPriority.Send);
        }

        private void Apply()
        {
            if (_disposed)
            {
                return;
            }

            var kind = _tab.ViewKind.Value;
            if (kind != TabViewKind.Crop)
            {
                EndCrop();
            }

            switch (kind)
            {
                case TabViewKind.StartUpMenu:
                    // Unsized, so it stretches to fill the window; only App Start fits the first menu to its opening size
                    _menu ??= new StartUpMenu { DataContext = _tab };
                    _view.Value = _menu;
                    break;
                case TabViewKind.ImageViewer:
                    _viewer ??= new ImageViewer(_tab);
                    _navigable ??= new NavigableView(_tab);
                    ShowInSlot(_viewer);
                    // After the image viewer took the slot, so the comparison viewer's zoom preview left with it
                    DropComparisonViewer();
                    _view.Value = _navigable;
                    break;
                case TabViewKind.Comparison:
                    // The comparison may already be gone again, which a later change of the kind shows
                    if (_tab.Comparison is not { } comparison)
                    {
                        break;
                    }

                    // Reached from the image viewer, and straight from the start-up menu or a crop
                    _navigable ??= new NavigableView(_tab);
                    if (_comparisonViewer is null || !ReferenceEquals(_comparisonViewer.Comparison, comparison))
                    {
                        // A viewer of an older comparison leaves the slot before it is disposed
                        var old = _comparisonViewer;
                        _comparisonViewer = new ComparisonViewer(_tab, comparison);
                        ShowInSlot(_comparisonViewer);
                        old?.Dispose();
                    }
                    else
                    {
                        ShowInSlot(_comparisonViewer);
                    }

                    _view.Value = _navigable;
                    break;
                case TabViewKind.Crop:
                    if (_crop is null)
                    {
                        // The crop opens where the viewer looks (ADR-0027)
                        _croppedModel = _tab.Model;
                        _crop = new CropControl(_viewer?.GetZoomView() ?? ZoomView.Fit) { DataContext = _tab };
                    }

                    _view.Value = _crop;
                    break;
            }
        }

        /// <summary>
        /// Puts <paramref name="viewer"/> in the navigable view's slot. A viewer that takes the slot from the other one
        /// starts at the tab's Zoom Factor, so toggling comparison keeps the zoom (ADR-0032), and an incoming image
        /// viewer follows the motion photo of what the tab shows now.
        /// </summary>
        private void ShowInSlot(INavigableContent viewer)
        {
            var navigable = _navigable!;
            if (navigable.Viewer is { } shown && !ReferenceEquals(shown, viewer))
            {
                viewer.ShowFactor(_tab.Zoom.Factor);
                if (viewer is ImageViewer imageViewer)
                {
                    imageViewer.UpdateMotionPhoto(_tab);
                }
            }

            navigable.Show(viewer);
        }

        private void DropComparisonViewer()
        {
            _comparisonViewer?.Dispose();
            _comparisonViewer = null;
        }

        /// <summary>
        /// Drops the crop control. The crop's zoom is its own: the viewer comes back as it was left (ADR-0027),
        /// or unzoomed when a crop saved over the shown file shows a new image.
        /// </summary>
        private void EndCrop()
        {
            if (_crop is not null && _viewer is { } viewer)
            {
                // The viewer was not shown when the new image was, so the reset that follows a new image's fit
                // passed it by
                if (Settings.Zoom.ResetZoomOnChange || !ReferenceEquals(_tab.Model, _croppedModel))
                {
                    viewer.ResetZoomSlim();
                }

                // The crop wrote the tab's Zoom Factor while it was shown; the viewer writes it from here on. Written
                // after a reset too, which reports nothing when the viewer was not zoomed
                _tab.Zoom.SetFactor(viewer.ZoomPanControl.Scale);

                // Don't let the zoom previewer show when the crop is ended. A viewer not loaded yet has none
                viewer.ZoomPanControl.ZoomPreviewer?.IsVisible = false;
            }

            _crop = null;
            _croppedModel = null;
        }

        public void Dispose()
        {
            _disposed = true;
            _following.Dispose();
            _navigable?.Dispose();
            _navigable = null;
            _viewer?.Dispose();
            _viewer = null;
            DropComparisonViewer();
            _menu = null;
            _crop = null;
            _croppedModel = null;
            _view.Value = null;
        }
    }
}
