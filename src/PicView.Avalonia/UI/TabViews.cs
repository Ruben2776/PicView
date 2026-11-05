using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Core.IPlatform;
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
                _crop = null;
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
                    _crop ??= new CropControl { DataContext = _tab };
                    _view.Value = _crop;
                    break;
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _following.Dispose();
            _viewer?.Dispose();
            _viewer = null;
            _menu = null;
            _crop = null;
            _view.Value = null;
        }
    }
}
