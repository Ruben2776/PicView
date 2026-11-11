using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Core.IPlatform;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.UI;

/// <summary>
/// One per Main Window: makes and keeps the controls the window shows for its tabs' Tab Views (ADR-0022). Each tab
/// has a menu and a viewer made once and kept, and a crop control made per crop. Attaching may happen on any thread;
/// the controls are made on the UI thread.
/// </summary>
public sealed class TabViews(MainWindow window) : ITabViewFactory
{
    public ITabViews Attach(TabViewModel tab) => new Set(window, tab);

    private sealed class Set : ITabViews
    {
        private readonly MainWindow _window;
        private readonly TabViewModel _tab;
        private readonly BindableReactiveProperty<object?> _view = new();
        private readonly IDisposable _following;
        private ImageViewer? _viewer;
        private StartUpMenu? _menu;
        private CropControl? _crop;

        // The model the tab showed when its crop opened
        private ImageModel? _croppedModel;
        private bool _disposed;

        public Set(MainWindow window, TabViewModel tab)
        {
            _window = window;
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
                    _view.Value = _viewer;
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
        /// Drops the crop control, handing its zoom back to the viewer when the tab still shows the image the crop
        /// opened on. A crop saved over the shown file shows a new image, whose zoom is the viewer's own.
        /// </summary>
        private void EndCrop()
        {
            if (_crop is { } crop && _viewer is { } viewer)
            {
                if (ReferenceEquals(_tab.Model, _croppedModel))
                {
                    viewer.SetZoomView(crop.GetZoomView());
                }
                else
                {
                    // The crop wrote the tab's Zoom Factor while it was shown; the viewer writes it from here on
                    _tab.Zoom.SetFactor(viewer.ZoomPanControl.Scale);
                }
            }

            _crop = null;
            _croppedModel = null;
        }

        public void Dispose()
        {
            _disposed = true;
            _following.Dispose();
            _viewer?.Dispose();
            _viewer = null;
            _menu = null;
            _crop = null;
            _croppedModel = null;
            _view.Value = null;
        }
    }
}
