using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.MacOS.Views;

public partial class ImageInfoWindow : GenericWindow, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ImageInfoWindowConfig _config;
    
    public ImageInfoWindow(MainWindowViewModel viewModel)
    {
        Debug.Assert(viewModel.InfoWindow.ImageInfoWindowConfig != null);
        _config = viewModel.InfoWindow.ImageInfoWindowConfig;
        DataContext = viewModel;
        InitializeComponent();
        if (Settings.Theme.GlassTheme)
        {
            WindowBorder.Background = Brushes.Transparent;
        }
        else if (!Settings.Theme.Dark)
        {
            XExifView.Background = UIHelper.GetMenuBackgroundColor();
        }
        GenericWindowHelper.GenericWindowInitialize(this, TranslationManager.Translation.ImageInfo + " - PicView");
        Loaded += delegate
        {
            ClientSizeProperty.Changed.ToObservable()
                .Debounce(TimeSpan.FromMilliseconds(10))
                .Subscribe(UpdateWindowSize)
                .AddTo(_disposables);
            PositionChanged += (_, __) => UpdateWindowPosition();
        };
    }
    
    private void UpdateWindowPosition()
    {
        _config.WindowProperties.Left = Position.X;
        _config.WindowProperties.Top = Position.Y;
    }
    
    private void UpdateWindowSize(AvaloniaPropertyChangedEventArgs<Size> size)
        => WindowFunctions.SetWindowSize(this, size, _config.WindowProperties);
    
    public void Dispose()
    {
        Disposable.Dispose(_disposables);
        GC.SuppressFinalize(this);
    }
}