using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using R3;

namespace PicView.Avalonia.MacOS.Views;

public partial class KeybindingsWindow : Window, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly KeybindingWindowConfig _config;

    public KeybindingsWindow(KeybindingWindowConfig config)
    {
        _config = config;
        InitializeComponent();
        if (Settings.Theme.GlassTheme)
        {
            WindowBorder.Background = Brushes.Transparent;
            XKeybindingsView.Background = Brushes.Transparent;
        }
        else if (!Settings.Theme.Dark)
        {
            XKeybindingsView.Background = UIHelper.GetMenuBackgroundColor();
        }
        GenericWindowHelper.KeybindingsWindowInitialize(this);

        ClientSizeProperty.Changed.ToObservable()
            .ObserveOn(UIHelper.GetFrameProvider)
            .Subscribe(UpdateWindowSize)
            .AddTo(_disposables);
        PositionChanged += (_, _) => UpdateWindowPosition();
        
        Closing += async delegate
        {
            Hide();
            if (VisualRoot is null)
            {
                return;
            }

            var hostWindow = (Window)VisualRoot;
            hostWindow?.Focus();
            await _config.SaveAsync();
        };
    }

    private void UpdateWindowSize(AvaloniaPropertyChangedEventArgs<Size> size)
        => WindowFunctions.SetWindowSize(this, size, _config.WindowProperties);

    private void UpdateWindowPosition()
    {
        _config.WindowProperties.Left = Position.X;
        _config.WindowProperties.Top = Position.Y;
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}