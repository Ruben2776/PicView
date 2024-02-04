using Avalonia.Controls;
using PicView.Core.Config;
using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.ViewModels;
using ReactiveUI;

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
            var wm = (MainViewModel)DataContext;
            wm.ShowInFolderCommand = ReactiveCommand.Create(() =>
            {
                Windows.FileHandling.FileExplorer.OpenFolderAndSelectFile(wm.FileInfo?.DirectoryName, wm.FileInfo?.Name);
            });
            
            // Keep window position when resizing
            ClientSizeProperty.Changed.Subscribe(size =>
            {
                if (!SettingsHelper.Settings.WindowProperties.AutoFit)
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
                if (_nextButtonClicked)
                {
                    PixelPoint p = default;
                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        p = nextButton.PointToScreen(new Point(50, 10)); //Points cursor to center of RightButton
                    }
                    else
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            p = nextButton.PointToScreen(new Point(50, 10)); //Points cursor to center of RightButton
                        }, DispatcherPriority.Normal).Wait();
                    }


                    Windows.NativeMethods.SetCursorPos(p.X, p.Y);
                    _nextButtonClicked = false;
                }
                if (_prevButtonClicked)
                {
                    PixelPoint p = default;
                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        p = prevButton.PointToScreen(new Point(50, 10)); //Points cursor to center of RightButton
                    }
                    else
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            p = prevButton.PointToScreen(new Point(50, 10)); //Points cursor to center of RightButton
                        }, DispatcherPriority.Normal).Wait();
                    }

                    Windows.NativeMethods.SetCursorPos(p.X, p.Y);
                    _prevButtonClicked = false;
                }
            };
            
        };
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();

        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
        Environment.Exit(0);
    }
}