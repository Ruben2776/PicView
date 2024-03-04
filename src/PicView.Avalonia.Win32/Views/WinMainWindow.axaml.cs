using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using ReactiveUI;
using System.Reactive.Concurrency;
using PicView.Avalonia.Helpers;
using PicView.Core.Calculations;

namespace PicView.Avalonia.Win32.Views;

public partial class WinMainWindow : Window
{
    private bool _nextButtonClicked;
    private bool _prevButtonClicked;
    private ExifWindow? _exifWindow;
    private SettingsWindow? _settingsWindow;

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
                var wm = (MainViewModel)DataContext;

                // Keep window position when resizing
                ClientSizeProperty.Changed.Subscribe(size =>
                {
                    WindowHelper.HandleWindowResize(this, size);
                });
                var nextButton = BottomBar.GetControl<Button>("NextButton");
                var prevButton = BottomBar.GetControl<Button>("PreviousButton");
                nextButton.Click += (_, _) => _nextButtonClicked = true;
                prevButton.Click += (_, _) => _prevButtonClicked = true;
                wm.ImageChanged += (s, e) =>
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

                // TODO figure out how to use KeyBindings via json config file
                KeyBindings.Add(new KeyBinding { Command = wm.NextCommand, Gesture = new KeyGesture(Key.D) });
                KeyBindings.Add(new KeyBinding { Command = wm.PreviousCommand, Gesture = new KeyGesture(Key.A) });
                KeyBindings.Add(new KeyBinding { Command = wm.ToggleUICommand, Gesture = new KeyGesture(Key.Z, KeyModifiers.Alt) });

                wm.ShowInFolderCommand = ReactiveCommand.Create(() =>
                {
                    Windows.FileHandling.FileExplorer.OpenFolderAndSelectFile(wm.FileInfo?.DirectoryName, wm.FileInfo?.Name);
                });

                wm.ShowExifWindowCommand = ReactiveCommand.Create(() =>
                {
                    if (_exifWindow is null)
                    {
                        _exifWindow = new ExifWindow
                        {
                            DataContext = wm,
                            WindowStartupLocation = WindowStartupLocation.Manual,
                            Position = new PixelPoint(Position.X, Position.Y + (int)Height / 3)
                        };
                        _exifWindow.Show();
                        _exifWindow.Closing += (s, e) => _exifWindow = null;
                    }
                    else
                    {
                        _exifWindow.Activate();
                    }
                    wm.CloseMenuCommand.Execute(null);
                });

                wm.ShowSettingsWindowCommand = ReactiveCommand.Create(() =>
                {
                    if (_settingsWindow is null)
                    {
                        _settingsWindow = new SettingsWindow
                        {
                            DataContext = wm,
                            WindowStartupLocation = WindowStartupLocation.Manual,
                            Position = new PixelPoint(Position.X, Position.Y + (int)Height / 3)
                        };
                        _settingsWindow.Show();
                        _settingsWindow.Closing += (s, e) => _settingsWindow = null;
                    }
                    else
                    {
                        _settingsWindow.Activate();
                    }
                    wm.CloseMenuCommand.Execute(null);
                });
            });
        };
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();

        await SettingsHelper.SaveSettingsAsync();
        FileDeletionHelper.DeleteTempFiles();
        base.OnClosing(e);

        Environment.Exit(0);
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
        wm.SetSize();
    }
}