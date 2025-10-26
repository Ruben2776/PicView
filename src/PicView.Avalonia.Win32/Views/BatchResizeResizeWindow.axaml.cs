using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.Core.WindowsNT.Taskbar;
using R3;

namespace PicView.Avalonia.Win32.Views;

public partial class BatchResizeWindow : Window, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly BatchResizeWindowConfig _config;
    public BatchResizeWindow(BatchResizeWindowConfig config)
    {
        _config = config;
        InitializeComponent();
        StartUp();
    }

    private void StartUp()
    {
        if (Settings.Theme.GlassTheme)
        {
            IconBorder.Background = Brushes.Transparent;
            IconBorder.BorderThickness = new Thickness(0);
            MinimizeButton.Background = Brushes.Transparent;
            MinimizeButton.BorderThickness = new Thickness(0);
            CloseButton.Background = Brushes.Transparent;
            CloseButton.BorderThickness = new Thickness(0);
            BorderRectangle.Height = 0;
            TitleText.Background = Brushes.Transparent;

            if (!Application.Current.TryGetResource("SecondaryTextColor",
                    Application.Current.RequestedThemeVariant, out var textColor))
            {
                return;
            }

            if (textColor is not Color color)
            {
                return;
            }

            TitleText.Foreground = new SolidColorBrush(color);
            MinimizeButton.Foreground = new SolidColorBrush(color);
            CloseButton.Foreground = new SolidColorBrush(color);
        }
        else if (!Settings.Theme.Dark)
        {
            if (!Application.Current.TryGetResource("MenuBackgroundColor",
                    Application.Current.RequestedThemeVariant, out var menuBackgroundColor))
            {
                return;
            }
            
            if (menuBackgroundColor is not Color color)
            {
                return;
            }
            ResizeView.Background = new SolidColorBrush(color);
        }

        GenericWindowHelper.GenericWindowInitialize(this, TranslationManager.Translation.BatchResize + " - PicView");
        Loaded += delegate
        {
            ClientSizeProperty.Changed.ToObservable()
                .ObserveOn(UIHelper.GetFrameProvider)
                .Subscribe(size =>
                {
                    WindowResizing.HandleWindowResize(this, size);
                    UpdateWindowSize(size);
                })
                .AddTo(_disposables);
            PositionChanged += (_, _) => UpdateWindowPosition();

            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            if (Settings.UIProperties.IsTaskbarProgressEnabled)
            {
                Observable.EveryValueChanged(vm.BatchResizeViewModel.Progress, x => x.CurrentValue)
                    .Skip(1)
                    .Subscribe(d =>
                    {
                        if (vm.BatchResizeViewModel?.Progress is not null &&
                            vm.BatchResizeViewModel?.ProgressMaximum?.Value is not null)
                        {
                            SetTaskbarProgress((ulong)d, (ulong)vm.BatchResizeViewModel.ProgressMaximum.CurrentValue);
                        }
                    });
            }
        };
        
        Closing += async delegate
        {
            Hide();
            if (VisualRoot is null)
            {
                return;
            }

            var hostWindow = (Window)VisualRoot;
            hostWindow?.BringIntoView();
            await _config.SaveAsync();
        };
    }

    private TaskbarProgress? _taskbarProgress;

    private void SetTaskbarProgress(ulong progress, ulong max)
    {
        if (_taskbarProgress is null)
        {
            var handle = TryGetPlatformHandle()?.Handle;

            // Ensure the handle is valid before proceeding
            if (handle == IntPtr.Zero || handle is null)
            {
                return;
            }

            _taskbarProgress = new TaskbarProgress(handle.Value);
        }

        if (progress == max)
        {
            _taskbarProgress.StopProgress();
        }
        else
        {
            _taskbarProgress.SetProgress(progress, max);
        }
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is null)
        {
            return;
        }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
    }

    private void Close(object? sender, RoutedEventArgs e) => Close();

    private void Minimize(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    
    private void UpdateWindowSize(AvaloniaPropertyChangedEventArgs<Size> size)
        => WindowFunctions.SetWindowSize(this, size, _config.WindowProperties);
    
    private void UpdateWindowPosition()
    {
        _config.WindowProperties.Left = Position.X;
        _config.WindowProperties.Top = Position.Y;
    }

    public void Dispose()
    {
       Disposable.Dispose(_disposables);
       _taskbarProgress = null;
       GC.SuppressFinalize(this);
    }
}