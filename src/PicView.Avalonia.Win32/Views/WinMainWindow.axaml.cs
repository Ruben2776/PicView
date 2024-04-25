using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using ReactiveUI;
using System.Reactive.Concurrency;

namespace PicView.Avalonia.Win32.Views;

public partial class WinMainWindow : Window
{
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
                };
            });
        };
        PointerPressed += (_, e) => MoveWindow(e);
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

    private void MoveWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }
        if (!MainKeyboardShortcuts.ShiftDown) { return; }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        await WindowHelper.WindowClosingBehavior(this);
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