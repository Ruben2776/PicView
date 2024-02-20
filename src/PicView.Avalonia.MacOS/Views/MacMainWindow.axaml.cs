using Avalonia;
using Avalonia.Controls;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using ReactiveUI;
using System;
using System.Reactive.Concurrency;
using System.Reflection.PortableExecutable;

namespace PicView.Avalonia.MacOS.Views;

public partial class MacMainWindow : Window
{
    private ExifWindow? _exifWindow;

    public MacMainWindow()
    {
        InitializeComponent();

        Loaded += delegate
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                if (DataContext is null)
                {
                    return;
                }

                var wm = (MainViewModel)DataContext;

                // Keep window position when resizing
                ClientSizeProperty.Changed.Subscribe(size =>
                {
                    WindowHelper.HandleWindowResize(this, size);
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
            });
        };
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

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();

        await SettingsHelper.SaveSettingsAsync();
        FileDeletionHelper.DeleteTempFiles();
        base.OnClosing(e);

        Environment.Exit(0);
    }
}