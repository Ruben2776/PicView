using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using ReactiveUI;
using System.Reactive.Concurrency;
using Avalonia.Input;
using Avalonia.Controls.ApplicationLifetimes;

namespace PicView.Avalonia.Win32.Views;

public partial class WinMainWindow : Window
{
    private bool _nextButtonClicked;
    private bool _prevButtonClicked;

    public WinMainWindow()
    {
        InitializeComponent();
        Loaded += delegate
        {
            if (DataContext == null)
            {
                return;
            }

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                var vm = (MainViewModel)DataContext;

                // Keep window position when resizing
                ClientSizeProperty.Changed.Subscribe(size =>
                {
                    WindowHelper.HandleWindowResize(this, size);
                });
                var nextButton = BottomBar.GetControl<Button>("NextButton");
                var prevButton = BottomBar.GetControl<Button>("PreviousButton");
                nextButton.Click += (_, _) => _nextButtonClicked = true;
                prevButton.Click += (_, _) => _prevButtonClicked = true;
                vm.ImageChanged += (s, e) =>
                {
                    if (SettingsHelper.Settings.UIProperties.IsTaskbarProgressEnabled)
                    {
                        // TODO: Add taskbar progress for Win32. using Microsoft.WindowsAPICodePack.Taskbar is not AOT compatible
                        // check if https://github.com/microsoft/CsWin32 AOT compatible

                        //TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, TryGetPlatformHandle().Handle);
                        //TaskbarManager.Instance.SetProgressValue(wm.GetIndex, wm.ImageIterator.Pics.Count, TryGetPlatformHandle().Handle);
                    }
                    // TODO: using NativeMethods.SetCursorPos(p.X, p.Y) to move cursor is not AOT compatible
                    PixelPoint p = default;
                    if (_nextButtonClicked)
                    {
                        if (Dispatcher.UIThread.CheckAccess())
                        {
                            p = nextButton.PointToScreen(new Point(50, 10));
                        }
                        else
                        {
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                p = nextButton.PointToScreen(new Point(50, 10));
                            }, DispatcherPriority.Normal).Wait();
                        }

                        Windows.NativeMethods.SetCursorPos(p.X, p.Y); // TODO check if https://github.com/microsoft/CsWin32 will work in AOT
                        _nextButtonClicked = false;
                    }
                    else if (_prevButtonClicked)
                    {
                        if (Dispatcher.UIThread.CheckAccess())
                        {
                            p = prevButton.PointToScreen(new Point(50, 10));
                        }
                        else
                        {
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                p = prevButton.PointToScreen(new Point(50, 10));
                            }, DispatcherPriority.Normal).Wait();
                        }

                        Windows.NativeMethods.SetCursorPos(p.X, p.Y);
                        _prevButtonClicked = false;
                    }
                };
            });
        };
        PointerPressed += async (_, e) => await MoveWindow(e);
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        desktop.ShutdownRequested += async (_, e) =>
        {
            await SettingsHelper.SaveSettingsAsync();
            FileDeletionHelper.DeleteTempFiles();
        };
    }

    private async Task MoveWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }
        if (!MainKeyboardShortcuts.ShiftDown) { return; }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
        await WindowHelper.UpdateWindowPosToSettings();
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        Hide();

        await SettingsHelper.SaveSettingsAsync();
        FileDeletionHelper.DeleteTempFiles();
        base.OnClosing(e);
    }

    private void Control_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext == null)
        {
            return;
        }

        if (e is { HeightChanged: false, WidthChanged: false })
        {
            return;
        }
        var wm = (MainViewModel)DataContext;
        WindowHelper.SetSize(wm);
    }
}