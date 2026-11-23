using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using PicView.Core.WindowsNT.Taskbar;
using R3;

namespace PicView.Avalonia.Win32.Views;

public partial class BatchResizeWindow : GenericWindow, IDisposable
{
    private DisposableBag _disposables;
    public BatchResizeWindow()
    {
        InitializeComponent();
        ThemeManager.ApplyAcrylic(this, ResizeView);
        StartUp();
    }

    private void StartUp()
    {
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