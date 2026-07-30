using Avalonia;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using PicView.Core.WindowsNT.Taskbar;
using R3;

namespace PicView.Avalonia.Win32.Views;

public partial class BatchResizeWindow : GenericWindow, IDisposable
{
    private DisposableBag _disposables;
    public BatchResizeWindow(BatchResizeWindowConfig config)
    {
        InitializeComponent();
        StartUp(config);
    }

    private void StartUp(BatchResizeWindowConfig config)
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

        }

        GenericWindowHelper.GenericWindowInitialize(this, StringExtensions.CombineWithAppName(TranslationManager.Translation.BatchResize), false, config.WindowProperties);
        Loaded += delegate
        {
            if (DataContext is not CoreViewModel core)
            {
                return;
            }

            if (Settings.UIProperties.IsTaskbarProgressEnabled)
            {
                Observable.EveryValueChanged(core.BatchResize.Progress, x => x.CurrentValue)
                    .Skip(1)
                    .Subscribe(d =>
                    {
                        if (core.BatchResize?.Progress is not null &&
                            core.BatchResize.ProgressMaximum?.Value is not null)
                        {
                            SetTaskbarProgress((ulong)d, (ulong)core.BatchResize.ProgressMaximum.CurrentValue);
                        }
                    }, DebugHelper.LogError(nameof(BatchResizeWindow), nameof(core.BatchResize)))
                    .AddTo(ref _disposables);
            }
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

    public void Dispose()
    {
       Disposable.Dispose(_disposables);
       _taskbarProgress = null;
       GC.SuppressFinalize(this);
    }
}