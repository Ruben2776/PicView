using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using ReactiveUI;
using System.Reactive.Concurrency;

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
                var wm = (MainViewModel)DataContext;

                // Keep window position when resizing
                ClientSizeProperty.Changed.Subscribe(size =>
                {
                    if (!SettingsHelper.Settings.WindowProperties.AutoFit)
                    {
                        return;
                    }

                    if (!size.OldValue.HasValue || !size.NewValue.HasValue)
                    {
                        return;
                    }

                    if (size.OldValue.Value.Width == 0 || size.OldValue.Value.Height == 0 ||
                        size.NewValue.Value.Width == 0 || size.NewValue.Value.Height == 0)
                    {
                        return;
                    }
                    var x = (size.OldValue.Value.Width - size.NewValue.Value.Width) / 2;
                    var y = (size.OldValue.Value.Height - size.NewValue.Value.Height) / 2;

                    Position = new PixelPoint(Position.X + (int)x, Position.Y + (int)y);
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

                        Windows.NativeMethods.SetCursorPos(p.X, p.Y);
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
                    var exifWindow = new ExifWindow
                    {
                        DataContext = wm,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        Position = new PixelPoint(Position.X, Position.Y + (int)Height / 3)
                    };
                    exifWindow.Show();
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